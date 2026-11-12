using SwiftDrop.Parcels.Business.Lifecycle;
using SwiftDrop.Parcels.Contracts.Dtos;

namespace SwiftDrop.Parcels.Business.Services;

/// Status changes made by the assigned courier, plus read access to a parcel's
/// history, proof and failure report for everyone allowed to see that parcel.
public interface IParcelLifecycleService
{
    Task<IReadOnlyList<CourierDeliveryDto>> GetCourierDeliveriesAsync(Guid courierId, DateTime closedSinceUtc, CancellationToken ct);
    Task<CourierDeliveryDto> GetCourierDeliveryAsync(Guid courierId, Guid parcelId, CancellationToken ct);

    Task<CourierDeliveryDto> UpdateStatusAsync(Guid courierId, Guid parcelId, UpdateDeliveryStatusRequest request, CancellationToken ct);
    Task<CourierDeliveryDto> ConfirmDeliveryAsync(Guid courierId, Guid parcelId, ConfirmDeliveryRequest request,
        ProofPhotoUpload? photo, CancellationToken ct);
    Task<CourierDeliveryDto> ReportFailureAsync(Guid courierId, Guid parcelId, ReportFailureRequest request,
        ProofPhotoUpload? photo, CancellationToken ct);

    Task<IReadOnlyList<StatusHistoryEntryDto>> GetHistoryAsync(ParcelActor actor, Guid parcelId, CancellationToken ct);
    Task<DeliveryProofDto?> GetProofAsync(ParcelActor actor, Guid parcelId, CancellationToken ct);
    Task<ProofPhoto?> OpenProofPhotoAsync(ParcelActor actor, Guid parcelId, CancellationToken ct);
    Task<FailureReportDto?> GetFailureReportAsync(ParcelActor actor, Guid parcelId, CancellationToken ct);
    Task<ProofPhoto?> OpenFailurePhotoAsync(ParcelActor actor, Guid parcelId, CancellationToken ct);
}
