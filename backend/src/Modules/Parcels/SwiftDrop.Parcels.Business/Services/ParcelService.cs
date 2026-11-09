using SwiftDrop.Parcels.Contracts.Dtos;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Repositories;
using SwiftDrop.Pricing.Contracts;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Parcels.Business.Services;

internal sealed class ParcelService(
    IParcelRepository repository,
    IPricingService pricingService) : IParcelService
{
    public async Task<ParcelCreatedResponse> CreateParcelAsync(Guid customerId, CreateParcelRequest req, CancellationToken ct)
    {
        // 1. Calculate fee using Part 10's Pricing Service
        var estimate = await pricingService.EstimateAsync(req.ZoneId, req.WeightKg, ServiceType.Standard, ct)
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
            req.PaymentMethod
        );

        // 4. Save to DB
        await repository.AddAsync(parcel, ct);
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

    private static string GenerateTrackingNumber()
    {
        var datePart = DateTime.UtcNow.ToString("yyMMdd");
        var randomPart = Convert.ToHexString(Guid.NewGuid().ToByteArray())[..6];
        return $"SD-{datePart}-{randomPart}";
    }
}