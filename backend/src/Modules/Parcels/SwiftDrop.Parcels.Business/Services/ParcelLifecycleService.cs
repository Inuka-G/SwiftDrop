using SwiftDrop.Parcels.Business.Lifecycle;
using SwiftDrop.Parcels.Contracts;
using SwiftDrop.Parcels.Contracts.Dtos;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Repositories;
using SwiftDrop.Parcels.Data.Storage;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Parcels.Business.Services;

internal sealed class ParcelLifecycleService(
    IParcelRepository repository,
    IProofPhotoStorage photoStorage,
    TimeProvider clock) : IParcelLifecycleService
{
    public async Task<IReadOnlyList<CourierDeliveryDto>> GetCourierDeliveriesAsync(Guid courierId,
        DateTime closedSinceUtc, CancellationToken ct)
    {
        var parcels = await repository.GetForCourierAsync(courierId, closedSinceUtc, ct);
        return parcels.Select(ToCourierDto).ToList();
    }

    public async Task<CourierDeliveryDto> GetCourierDeliveryAsync(Guid courierId, Guid parcelId, CancellationToken ct) =>
        ToCourierDto(await LoadAssignedAsync(courierId, parcelId, ct));

    public async Task<CourierDeliveryDto> UpdateStatusAsync(Guid courierId, Guid parcelId,
        UpdateDeliveryStatusRequest request, CancellationToken ct)
    {
        // Delivered and Failed need proof or a reason, so they have their own endpoints.
        if (request.Status is ParcelStatuses.Delivered or ParcelStatuses.Failed)
            throw new BadRequestException("Use the deliver or fail endpoint to close a delivery.");

        var parcel = await LoadAssignedAsync(courierId, parcelId, ct);
        ApplyTransition(parcel, request.Status, Normalize(request.Note), courierId, failureReason: null);
        await repository.SaveChangesAsync(ct);
        return ToCourierDto(parcel);
    }

    public async Task<CourierDeliveryDto> ConfirmDeliveryAsync(Guid courierId, Guid parcelId,
        ConfirmDeliveryRequest request, ProofPhotoUpload? photo, CancellationToken ct)
    {
        var recipientName = Normalize(request.RecipientName);
        if (recipientName is null && photo is null)
            throw new BadRequestException("Proof of delivery needs the recipient's name or a photo.");

        var parcel = await LoadAssignedAsync(courierId, parcelId, ct);
        var note = Normalize(request.Note);
        ApplyTransition(parcel, ParcelStatuses.Delivered, note, courierId, failureReason: null);

        // The photo is checked and stored only after the transition is known to be valid.
        var photoKey = await StorePhotoAsync(photo, ct);
        repository.AddProof(new DeliveryProof(parcel.Id, courierId, recipientName, note, photoKey,
            ContentTypeOf(photoKey), parcel.UpdatedAtUtc));
        await SaveWithPhotoAsync(photoKey, ct);
        return ToCourierDto(parcel);
    }

    public async Task<CourierDeliveryDto> ReportFailureAsync(Guid courierId, Guid parcelId,
        ReportFailureRequest request, ProofPhotoUpload? photo, CancellationToken ct)
    {
        var parcel = await LoadAssignedAsync(courierId, parcelId, ct);
        var note = Normalize(request.Note);
        ApplyTransition(parcel, ParcelStatuses.Failed, note, courierId, request.Reason);

        var photoKey = await StorePhotoAsync(photo, ct);
        repository.AddFailureReport(new FailureReport(parcel.Id, courierId, request.Reason, note, photoKey,
            ContentTypeOf(photoKey), parcel.UpdatedAtUtc));
        await SaveWithPhotoAsync(photoKey, ct);
        return ToCourierDto(parcel);
    }

    public async Task<IReadOnlyList<StatusHistoryEntryDto>> GetHistoryAsync(ParcelActor actor, Guid parcelId, CancellationToken ct)
    {
        await LoadVisibleAsync(actor, parcelId, ct);
        var history = await repository.GetHistoryAsync(parcelId, ct);
        return history.Select(h => new StatusHistoryEntryDto(h.FromStatus, h.ToStatus, h.Note, h.ChangedByRole, h.CreatedAtUtc))
            .ToList();
    }

    public async Task<DeliveryProofDto?> GetProofAsync(ParcelActor actor, Guid parcelId, CancellationToken ct)
    {
        await LoadVisibleAsync(actor, parcelId, ct);
        var proof = await repository.GetProofAsync(parcelId, ct);
        return proof is null ? null : new DeliveryProofDto(proof.RecipientName, proof.Note, proof.PhotoKey is not null, proof.CreatedAtUtc);
    }

    public async Task<ProofPhoto?> OpenProofPhotoAsync(ParcelActor actor, Guid parcelId, CancellationToken ct)
    {
        await LoadVisibleAsync(actor, parcelId, ct);
        var proof = await repository.GetProofAsync(parcelId, ct);
        return OpenPhoto(proof?.PhotoKey, proof?.PhotoContentType);
    }

    public async Task<FailureReportDto?> GetFailureReportAsync(ParcelActor actor, Guid parcelId, CancellationToken ct)
    {
        await LoadVisibleAsync(actor, parcelId, ct);
        var report = await repository.GetLatestFailureAsync(parcelId, ct);
        return report is null
            ? null
            : new FailureReportDto(report.Reason, report.Note, report.PhotoKey is not null, report.CreatedAtUtc);
    }

    public async Task<ProofPhoto?> OpenFailurePhotoAsync(ParcelActor actor, Guid parcelId, CancellationToken ct)
    {
        await LoadVisibleAsync(actor, parcelId, ct);
        var report = await repository.GetLatestFailureAsync(parcelId, ct);
        return OpenPhoto(report?.PhotoKey, report?.PhotoContentType);
    }

    private async Task<string?> StorePhotoAsync(ProofPhotoUpload? photo, CancellationToken ct)
    {
        if (photo is null) return null;
        var (content, extension) = await ProofPhotoRules.ReadAsync(photo, ct);
        await using (content)
            return await photoStorage.SaveAsync(content, extension, ct);
    }

    private async Task SaveWithPhotoAsync(string? photoKey, CancellationToken ct)
    {
        try
        {
            await repository.SaveChangesAsync(ct);
        }
        catch
        {
            if (photoKey is not null) photoStorage.Delete(photoKey);   // don't leave an orphaned file
            throw;
        }
    }

    private ProofPhoto? OpenPhoto(string? key, string? contentType)
    {
        var stream = key is null ? null : photoStorage.OpenRead(key);
        return stream is null ? null : new ProofPhoto(stream, contentType ?? "application/octet-stream");
    }

    private static string? ContentTypeOf(string? photoKey) =>
        photoKey is null ? null : ProofPhotoRules.ContentTypeFor(photoKey);

    /// The one place a status changes: checks the state machine, then stages the new status,
    /// a history row and the integration event. The caller saves them in one transaction.
    private void ApplyTransition(Parcel parcel, string newStatus, string? note, Guid courierId, string? failureReason)
    {
        var previous = parcel.CurrentStatus;
        if (!ParcelStatusTransitions.CanMove(previous, newStatus))
            throw new ConflictException($"A parcel in status {previous} cannot move to {newStatus}.");

        var now = clock.GetUtcNow().UtcDateTime;
        parcel.ChangeStatus(newStatus, now);
        repository.AddHistory(new ParcelStatusHistory(parcel.Id, previous, newStatus, note, courierId, Roles.Courier, now));

        var eventId = Guid.NewGuid();
        repository.StageEvent(eventId, ParcelEventRoutingKeys.StatusChanged,
            new ParcelStatusChangedIntegrationEvent(eventId, parcel.Id, parcel.TrackingNumber, parcel.CustomerId,
                parcel.CourierId, previous, newStatus, failureReason, now),
            now);
    }

    private async Task<Parcel> LoadAssignedAsync(Guid courierId, Guid parcelId, CancellationToken ct)
    {
        var parcel = await repository.GetByIdAsync(parcelId, ct) ?? throw new NotFoundException("Delivery not found.");
        if (parcel.CourierId != courierId) throw new ForbiddenException("You are not assigned to this delivery.");
        return parcel;
    }

    private async Task<Parcel> LoadVisibleAsync(ParcelActor actor, Guid parcelId, CancellationToken ct)
    {
        var parcel = await repository.GetByIdAsync(parcelId, ct) ?? throw new NotFoundException("Parcel not found.");
        var allowed = actor.Role switch
        {
            Roles.Admin or Roles.Dispatcher => true,
            Roles.Courier => parcel.CourierId == actor.UserId,
            Roles.Customer => parcel.CustomerId == actor.UserId,
            _ => false,
        };
        return allowed ? parcel : throw new ForbiddenException("You do not have access to this parcel.");
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CourierDeliveryDto ToCourierDto(Parcel p) => new(
        p.Id,
        p.TrackingNumber,
        p.CurrentStatus,
        p.PickupAddress,
        p.PickupContactName,
        p.PickupContactPhone,
        p.DropoffAddress,
        p.RecipientName,
        p.RecipientPhone,
        p.WeightKg,
        p.SizeCategory,
        p.PaymentMethod,
        p.PaymentMethod.Equals("COD", StringComparison.OrdinalIgnoreCase) ? p.FinalFee ?? p.EstimatedFee : 0m,
        p.AssignedAtUtc,
        p.UpdatedAtUtc,
        ParcelStatusTransitions.NextFrom(p.CurrentStatus));
}
