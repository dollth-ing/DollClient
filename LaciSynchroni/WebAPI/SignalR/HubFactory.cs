using LaciSynchroni.Common.SignalR;
using LaciSynchroni.Services;
using LaciSynchroni.Services.Mediator;
using LaciSynchroni.Services.ServerConfiguration;
using LaciSynchroni.WebAPI.SignalR.Utils;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LaciSynchroni.WebAPI.SignalR;

public class HubFactory : MediatorSubscriberBase
{
    private readonly ILoggerProvider _loggingProvider;
    private readonly ServerConfigurationManager _serverConfigurationManager;
    private readonly TokenProvider _tokenProvider;
    private HubConnection? _instance;
    private bool _isDisposed = false;
    private readonly bool _isWine = false;

    public HubFactory(
        ILogger<HubFactory> logger,
        SyncMediator mediator,
        ServerConfigurationManager serverConfigurationManager,
        TokenProvider tokenProvider,
        ILoggerProvider pluginLog,
        DalamudUtilService dalamudUtilService
    )
        : base(logger, mediator)
    {
        _serverConfigurationManager = serverConfigurationManager;
        _tokenProvider = tokenProvider;
        _loggingProvider = pluginLog;
        _isWine = dalamudUtilService.IsWine;
    }

    public async Task DisposeHubAsync()
    {
        if (_instance == null || _isDisposed)
            return;

        Logger.LogDebug("Disposing current HubConnection");

        _isDisposed = true;

        _instance.Closed -= HubOnClosed;
        _instance.Reconnecting -= HubOnReconnecting;
        _instance.Reconnected -= HubOnReconnected;

        await _instance.StopAsync().ConfigureAwait(false);
        await _instance.DisposeAsync().ConfigureAwait(false);

        _instance = null;

        Logger.LogDebug("Current HubConnection disposed");
    }

    public HubConnection GetOrCreate(CancellationToken ct, string? hubPathOverride = null)
    {
        if (!_isDisposed && _instance != null)
            return _instance;

        var transportType = _serverConfigurationManager.GetTransport() switch
        {
            HttpTransportType.None => HttpTransportType.WebSockets
                | HttpTransportType.ServerSentEvents
                | HttpTransportType.LongPolling,
            HttpTransportType.WebSockets => HttpTransportType.WebSockets
                | HttpTransportType.ServerSentEvents
                | HttpTransportType.LongPolling,
            HttpTransportType.ServerSentEvents => HttpTransportType.ServerSentEvents
                | HttpTransportType.LongPolling,
            HttpTransportType.LongPolling => HttpTransportType.LongPolling,
            _ => HttpTransportType.WebSockets
                | HttpTransportType.ServerSentEvents
                | HttpTransportType.LongPolling,
        };

        if (
            _isWine
            && !_serverConfigurationManager.CurrentServer.ForceWebSockets
            && transportType.HasFlag(HttpTransportType.WebSockets)
        )
        {
            Logger.LogDebug("Wine detected, falling back to ServerSentEvents / LongPolling");
            transportType = HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
        }

        var cs = _serverConfigurationManager.CurrentServer;

        var useAdvancedUris = cs.UseAdvancedUris;
        var serverHubUri = cs.ServerHubUri;

        var hasCustomHubPath = useAdvancedUris && !string.IsNullOrEmpty(serverHubUri) && string.IsNullOrEmpty(hubPathOverride);
        var hubUrl = hasCustomHubPath ? serverHubUri : cs.ServerUri + (hubPathOverride ?? IServerHub.Path);

        Logger.LogDebug("Building new HubConnection using transport {Transport}", transportType);

        _instance = new HubConnectionBuilder()
            .WithUrl(
                hubUrl,
                options =>
                {
                    options.AccessTokenProvider = () => _tokenProvider.GetOrUpdateToken(ct);
                    options.Transports = transportType;
                }
            )
            .AddMessagePackProtocol(opt =>
            {
                var resolver = CompositeResolver.Create(
                    StandardResolverAllowPrivate.Instance,
                    BuiltinResolver.Instance,
                    AttributeFormatterResolver.Instance,
                    // replace enum resolver
                    DynamicEnumAsStringResolver.Instance,
                    DynamicGenericResolver.Instance,
                    DynamicUnionResolver.Instance,
                    DynamicObjectResolver.Instance,
                    PrimitiveObjectResolver.Instance,
                    // final fallback(last priority)
                    StandardResolver.Instance
                );

                opt.SerializerOptions = MessagePackSerializerOptions
                    .Standard.WithCompression(MessagePackCompression.Lz4Block)
                    .WithResolver(resolver);
            })
            .WithAutomaticReconnect(new ForeverRetryPolicy(Mediator))
            .ConfigureLogging(a =>
            {
                a.ClearProviders().AddProvider(_loggingProvider);
                a.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        _instance.Closed += HubOnClosed;
        _instance.Reconnecting += HubOnReconnecting;
        _instance.Reconnected += HubOnReconnected;

        _isDisposed = false;

        return _instance;
    }

    private Task HubOnClosed(Exception? arg)
    {
        Mediator.Publish(new HubClosedMessage(arg));
        return Task.CompletedTask;
    }

    private Task HubOnReconnected(string? arg)
    {
        Mediator.Publish(new HubReconnectedMessage(arg));
        return Task.CompletedTask;
    }

    private Task HubOnReconnecting(Exception? arg)
    {
        Mediator.Publish(new HubReconnectingMessage(arg));
        return Task.CompletedTask;
    }
}