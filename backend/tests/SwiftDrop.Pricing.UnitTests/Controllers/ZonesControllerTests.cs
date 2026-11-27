using Microsoft.AspNetCore.Mvc;
using Moq;
using SwiftDrop.Pricing.Api.Controllers;
using SwiftDrop.Pricing.Business.Services;
using SwiftDrop.Pricing.Contracts;

namespace SwiftDrop.Pricing.UnitTests.Controllers;

public sealed class ZonesControllerTests
{
    private const string InvalidWeightMessage = "Parcel weight must be greater than zero and no more than 50 kg.";

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(50.01)]
    public async Task Estimate_RejectsWeightsOutsideAllowedRange(decimal weightKg)
    {
        var pricing = new Mock<IPricingService>(MockBehavior.Strict);
        var controller = new ZonesController(Mock.Of<IZoneService>(), pricing.Object);

        var result = await controller.Estimate(Guid.NewGuid(), weightKg, ServiceType.Standard, default);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(InvalidWeightMessage, badRequest.Value);
        pricing.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public async Task Estimate_AllowsWeightsWithinAllowedRange(decimal weightKg)
    {
        var zoneId = Guid.NewGuid();
        var estimate = new FeeEstimateDto(zoneId, "Colombo", 500m, 0m, 500m);
        var pricing = new Mock<IPricingService>(MockBehavior.Strict);
        pricing.Setup(service => service.EstimateAsync(zoneId, weightKg, ServiceType.Standard, It.IsAny<CancellationToken>()))
            .ReturnsAsync(estimate);
        var controller = new ZonesController(Mock.Of<IZoneService>(), pricing.Object);

        var result = await controller.Estimate(zoneId, weightKg, ServiceType.Standard, default);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(estimate, ok.Value);
        pricing.VerifyAll();
    }
}
