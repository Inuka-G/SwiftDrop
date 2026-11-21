using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace SwiftDrop.SharedKernel.Messaging;

public sealed class RabbitMqConnection(IOptions<RabbitMqOptions> options) : IAsyncDisposable
{
    private readonly RabbitMqOptions _o = options.Value;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqOptions Options => _o;

    public async Task<IConnection> GetAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true }) return _connection;

        await _lock.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true }) return _connection;

            if (_connection is not null)
            {
                try { await _connection.DisposeAsync(); }
                catch { /* ignore cleanup error */ }
                _connection = null;
            }

            var factory = new ConnectionFactory
            {
                HostName = _o.Host,
                Port = _o.Port,
                UserName = _o.Username,
                Password = _o.Password,
                AutomaticRecoveryEnabled = true
            };
            _connection = await factory.CreateConnectionAsync("swiftdrop", ct);
            return _connection;
        }
        finally { _lock.Release(); }
    }

    /// Declares the main topic exchange and the dead letter exchange (idempotent).
    public async Task DeclareExchangesAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(_o.Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        await channel.ExchangeDeclareAsync(_o.DeadLetterExchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null) await _connection.DisposeAsync();
    }
}