using SwiftDrop.Parcels.Business.Lifecycle;
using SwiftDrop.Parcels.Contracts;
using SwiftDrop.Parcels.Contracts.Dtos;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Repositories;
using SwiftDrop.Pricing.Contracts;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Parcels.Business.Services;

internal sealed class ParcelService(
    IParcelRepository repository,
    IPricingService pricingService,
    IEventPublisher eventPublisher) : IParcelService, IParcelDispatchDirectory
{
    public Task<IReadOnlyList<DispatchableParcelSnapshot>> GetDispatchSnapshotAsync(CancellationToken ct) =>
        repository.GetDispatchSnapshotAsync(ct);
    public Task<ParcelCreatedResponse> CreateParcelAsync(Guid customerId, CreateParcelRequest req, CancellationToken ct) =>
        CreateParcelAsync(customerId, null, req, ct);

    public Task<ParcelCreatedResponse> CreateMerchantParcelAsync(Guid merchantId, CreateParcelRequest req, CancellationToken ct) =>
        CreateParcelAsync(null, merchantId, req, ct);

    public async Task<MerchantOrdersPage> ListMerchantOrdersAsync(
        Guid merchantId, string? status, int page, int pageSize, CancellationToken ct)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            throw new BadRequestException("Use a positive page and a pageSize between 1 and 100 within the supported offset range.");

        var canonicalStatus = ParcelStatuses.All.FirstOrDefault(s =>
            string.Equals(s, status?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (status is not null && canonicalStatus is null)
            throw new BadRequestException("Unknown delivery status.");

        var (items, total) = await repository.ListMerchantOrdersAsync(
            merchantId, canonicalStatus, (page - 1) * pageSize, pageSize, ct);
        return new MerchantOrdersPage(items.Select(ToMerchantOrder).ToArray(), page, pageSize, total);
    }

    public async Task<MerchantOrderDto?> GetMerchantOrderAsync(Guid merchantId, Guid parcelId, CancellationToken ct)
    {
        var parcel = await repository.GetMerchantOrderAsync(merchantId, parcelId, ct);
        return parcel is null ? null : ToMerchantOrder(parcel);
    }

    private static MerchantOrderDto ToMerchantOrder(Parcel parcel) =>
        new(parcel.Id, parcel.TrackingNumber, parcel.RecipientName, parcel.RecipientPhone,
            parcel.DropoffAddress, parcel.CurrentStatus, parcel.EstimatedFee, parcel.CreatedAtUtc);

    private async Task<ParcelCreatedResponse> CreateParcelAsync(Guid? customerId, Guid? merchantId,
        CreateParcelRequest req, CancellationToken ct)
    {
        if (!Enum.IsDefined(req.ServiceType))
            throw new BadRequestException("Service type must be Standard (1) or Express (2).");

        // 1. Calculate fee using Part 10's Pricing Service
        var estimate = await pricingService.EstimateAsync(req.ZoneId, req.WeightKg, req.ServiceType, ct)
            ?? throw new BadRequestException("Invalid zone or pricing could not be calculated.");

        // 2. Generate unique tracking number: e.g. SD-261108-A9B2
        var trackingNumber = GenerateTrackingNumber();

        // 3. Create Entity
        var parcel = new Parcel(
            customerId,
            trackingNumber,
            req.ZoneId,
            req.PickupAddress,
            req.PickupContactName,
            req.PickupContactPhone,
            req.DropoffAddress,
            req.RecipientName,
            req.RecipientPhone,
            req.WeightKg,
            req.SizeCategory,
            estimate.Total,
            req.PaymentMethod,
            merchantId
        );

        // 4. Save to DB, with the first status history row and the ParcelCreated event
        //    (Dispatch consumes it to list the parcel as unassigned)
        await repository.AddAsync(parcel, ct);
        repository.AddHistory(new ParcelStatusHistory(parcel.Id, null, parcel.CurrentStatus, null,
            merchantId ?? customerId!.Value, merchantId.HasValue ? Roles.Merchant : Roles.Customer, parcel.CreatedAtUtc,
            merchantId.HasValue ? ParcelStatusActorTypes.Merchant : ParcelStatusActorTypes.User));
        var eventId = Guid.NewGuid();
        repository.StageEvent(eventId, ParcelEventRoutingKeys.Created,
            new ParcelCreatedIntegrationEvent(eventId, parcel.Id, parcel.TrackingNumber, customerId, parcel.ZoneId,
                parcel.CreatedAtUtc, merchantId),
            parcel.CreatedAtUtc);

        if (merchantId.HasValue)
        {
            var statusEventId = Guid.NewGuid();
            repository.StageEvent(statusEventId, ParcelEventRoutingKeys.StatusChanged,
                new ParcelStatusChangedIntegrationEvent(statusEventId, parcel.Id, parcel.TrackingNumber, null,
                    null, "None", parcel.CurrentStatus, null, parcel.CreatedAtUtc, merchantId),
                parcel.CreatedAtUtc);
        }

        if (customerId.HasValue)
        {
            var statusEvent = new ParcelStatusChanged(
                parcel.Id, parcel.TrackingNumber, "None", parcel.CurrentStatus, "Parcel created.",
                customerId, null, null)
            {
                OccurredOnUtc = parcel.CreatedAtUtc
            };
            repository.StageEvent(statusEvent.EventId, "parcel.status.changed", statusEvent, parcel.CreatedAtUtc);
        }

        await repository.SaveChangesAsync(ct);

        return new ParcelCreatedResponse(parcel.Id, parcel.TrackingNumber, parcel.EstimatedFee, parcel.CurrentStatus, parcel.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<ParcelSummaryDto>> GetCustomerHistoryAsync(Guid customerId, CancellationToken ct)
    {
        var parcels = await repository.GetByCustomerIdAsync(customerId, ct);
        return parcels.Select(p => new ParcelSummaryDto(
            p.Id,
            p.TrackingNumber,
            p.RecipientName,
            p.DropoffAddress,
            p.WeightKg,
            p.EstimatedFee,
            p.CurrentStatus,
            p.CreatedAtUtc
        )).ToList();
    }

    public async Task<PublicTrackingDto?> GetPublicTrackingAsync(string trackingNumber, CancellationToken ct)
    {
        var parcel = await repository.GetByTrackingNumberAsync(trackingNumber, ct);
        if (parcel is null) return null;

        // Mask address for public privacy: take just the city or first 20 chars
        var cityOrArea = parcel.DropoffAddress.Split(',').LastOrDefault()?.Trim() ?? "Destination";

        return new PublicTrackingDto(
            parcel.TrackingNumber,
            parcel.CurrentStatus,
            parcel.CreatedAtUtc,
            parcel.UpdatedAtUtc,
            cityOrArea
        );
    }

    public async Task<ParcelDetailsDto?> GetReceiptDetailsAsync(Guid parcelId, Guid customerId, CancellationToken ct)
    {
        var parcel = await repository.GetByIdAsync(parcelId, ct);
        if (parcel is null || parcel.CustomerId != customerId) return null;

        return new ParcelDetailsDto(
            parcel.Id,
            parcel.TrackingNumber,
            parcel.PickupAddress,
            parcel.DropoffAddress,
            parcel.RecipientName,
            parcel.WeightKg,
            parcel.EstimatedFee,
            parcel.CurrentStatus,
            parcel.CreatedAtUtc
        );
    }

    public async Task<bool> UpdateStatusAsync(Guid parcelId, string newStatus, string? note = null, Guid? courierId = null, CancellationToken ct = default)
    {
        var parcel = await repository.GetByIdAsync(parcelId, ct);
        if (parcel is null) return false;

        var oldStatus = parcel.CurrentStatus;
        parcel.UpdateStatus(newStatus);
        await repository.SaveChangesAsync(ct);

        await eventPublisher.PublishAsync(new ParcelStatusChanged(
            parcel.Id,
            parcel.TrackingNumber,
            OldStatus: oldStatus,
            NewStatus: newStatus,
            Note: note,
            CustomerId: parcel.CustomerId,
            MerchantId: parcel.MerchantId,
            CourierId: courierId
        ), ct);

        return true;
    }

    private static string GenerateTrackingNumber()
    {
        var datePart = DateTime.UtcNow.ToString("yyMMdd");
        var randomPart = Convert.ToHexString(Guid.NewGuid().ToByteArray())[..6];
        return $"SD-{datePart}-{randomPart}";
    }
}
