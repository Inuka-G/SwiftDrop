using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using SwiftDrop.Identity.Contracts;
using SwiftDrop.Merchants.Contracts;
using SwiftDrop.Notifications.Business;
using SwiftDrop.Notifications.Business.Email;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Notifications.UnitTests;

public sealed class ParcelStatusNotificationTests : IDisposable
{
    private readonly DirectoryInfo folder = Directory.CreateTempSubdirectory("swiftdrop-email-tests-");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sends_once_to_customer_or_approved_merchant_contact(bool merchantOrder)
    {
        var userId = Guid.NewGuid();
        var evt = Event(merchantOrder ? null : userId, merchantOrder ? Guid.NewGuid() : null);
        var users = new Mock<IUserEmailDirectory>();
        users.Setup(x => x.GetActiveEmailAsync(userId, default)).ReturnsAsync("recipient@example.test");
        var merchants = new Mock<IMerchantContactDirectory>();
        if (evt.MerchantId is { } merchantId)
            merchants.Setup(x => x.GetApprovedContactUserIdAsync(merchantId, default)).ReturnsAsync(userId);
        var sender = new RecordingSender();
        await using var services = CreateServices(users.Object, merchants.Object, sender);
        await services.InitializeNotificationsAsync();
        await DeliverAsync(services, evt);
        await DeliverAsync(services, evt);

        var sent = Assert.Single(sender.Messages);
        Assert.Equal("recipient@example.test", sent.Recipient);
        Assert.Contains(evt.TrackingNumber, sent.Body);
        Assert.Contains(evt.NewStatus, sent.Body);
        Assert.Contains("https://example.test/track/SD-TEST", sent.Body);
    }

    [Fact]
    public async Task Missing_recipient_completes_without_sending()
    {
        var sender = new RecordingSender();
        await using var services = CreateServices(Mock.Of<IUserEmailDirectory>(),
            Mock.Of<IMerchantContactDirectory>(), sender);
        await services.InitializeNotificationsAsync();
        await DeliverAsync(services, Event(null, Guid.NewGuid()));
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public async Task Failed_send_can_be_retried_and_deduplicated_after_success()
    {
        var userId = Guid.NewGuid();
        var users = new Mock<IUserEmailDirectory>();
        users.Setup(x => x.GetActiveEmailAsync(userId, default)).ReturnsAsync("recipient@example.test");
        var sender = new RecordingSender { Fail = true };
        await using var services = CreateServices(users.Object, Mock.Of<IMerchantContactDirectory>(), sender);
        await services.InitializeNotificationsAsync();
        var evt = Event(userId, null);

        await Assert.ThrowsAsync<SmtpException>(() => DeliverAsync(services, evt));
        sender.Fail = false;
        await DeliverAsync(services, evt);
        await DeliverAsync(services, evt);
        Assert.Single(sender.Messages);
    }

    private ServiceProvider CreateServices(IUserEmailDirectory users, IMerchantContactDirectory merchants,
        RecordingSender sender)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Notifications"] = $"Data Source={Path.Combine(folder.FullName, "notifications.db")};Pooling=False",
            ["Notifications:Email:Enabled"] = "true",
            ["Notifications:Email:Host"] = "localhost",
            ["Notifications:Email:FromAddress"] = "sender@example.test",
            ["Notifications:Email:TrackingBaseUrl"] = "https://example.test/"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNotificationsBusiness(config);
        services.AddSingleton(users);
        services.AddSingleton(merchants);
        services.Replace(ServiceDescriptor.Singleton<INotificationEmailSender>(sender));
        return services.BuildServiceProvider();
    }

    private static async Task DeliverAsync(IServiceProvider services, ParcelStatusChangedIntegrationEvent evt)
    {
        using var scope = services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<ParcelStatusChangedIntegrationEvent>>();
        await handler.HandleAsync(evt, default);
    }

    private static ParcelStatusChangedIntegrationEvent Event(Guid? customer, Guid? merchant) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "SD-TEST", customer, null, "InTransit", "Delivered", null,
            DateTime.UtcNow, merchant);

    public void Dispose() => folder.Delete(recursive: true);

    private sealed class RecordingSender : INotificationEmailSender
    {
        public bool Fail { get; set; }
        public List<(string Recipient, string Body)> Messages { get; } = [];

        public Task SendAsync(MailAddress recipient, string subject, string body, string messageId, CancellationToken ct)
        {
            if (Fail)
                throw new SmtpException("Simulated failure.");
            Messages.Add((recipient.Address, body));
            return Task.CompletedTask;
        }
    }
}
