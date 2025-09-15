using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using LaciSynchroni.Services.Mediator;
using LaciSynchroni.SyncConfiguration;
using LaciSynchroni.SyncConfiguration.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationType = LaciSynchroni.SyncConfiguration.Models.NotificationType;

namespace LaciSynchroni.Services;

public class NotificationService : DisposableMediatorSubscriberBase, IHostedService
{
    private readonly DalamudUtilService _dalamudUtilService;
    private readonly INotificationManager _notificationManager;
    private readonly IChatGui _chatGui;
    private readonly SyncConfigService _configurationService;

    public NotificationService(
        ILogger<NotificationService> logger,
        SyncMediator mediator,
        DalamudUtilService dalamudUtilService,
        INotificationManager notificationManager,
        IChatGui chatGui,
        SyncConfigService configurationService
    )
        : base(logger, mediator)
    {
        _dalamudUtilService = dalamudUtilService;
        _notificationManager = notificationManager;
        _chatGui = chatGui;
        _configurationService = configurationService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Mediator.Subscribe<NotificationMessage>(this, ShowNotification);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void PrintErrorChat(string? message)
    {
        SeStringBuilder se = new SeStringBuilder().AddText("[Laci Synchroni] Error: " + message);
        _chatGui.PrintError(se.BuiltString);
    }

    private void PrintInfoChat(string? message)
    {
        SeStringBuilder se = new SeStringBuilder()
            .AddText("[Laci Synchroni] Info: ")
            .AddItalics(message ?? string.Empty);
        _chatGui.Print(se.BuiltString);
    }

    private void PrintWarnChat(string? message)
    {
        SeStringBuilder se = new SeStringBuilder()
            .AddText("[Laci Synchroni] ")
            .AddUiForeground("Warning: " + (message ?? string.Empty), 31)
            .AddUiForegroundOff();
        _chatGui.Print(se.BuiltString);
    }

    private void ShowChat(NotificationMessage msg)
    {
        switch (msg.Type)
        {
            case SyncConfiguration.Models.NotificationType.Info:
                PrintInfoChat(msg.Message);
                break;

            case SyncConfiguration.Models.NotificationType.Warning:
                PrintWarnChat(msg.Message);
                break;

            case SyncConfiguration.Models.NotificationType.Error:
                PrintErrorChat(msg.Message);
                break;
        }
    }

    private void ShowNotification(NotificationMessage msg)
    {
        Logger.LogInformation("{msg}", msg.ToString());

        if (!_dalamudUtilService.IsLoggedIn)
            return;

        switch (msg.Type)
        {
            case SyncConfiguration.Models.NotificationType.Info:
                ShowNotificationLocationBased(msg, _configurationService.Current.InfoNotification);
                break;

            case SyncConfiguration.Models.NotificationType.Warning:
                ShowNotificationLocationBased(
                    msg,
                    _configurationService.Current.WarningNotification
                );
                break;

            case SyncConfiguration.Models.NotificationType.Error:
                ShowNotificationLocationBased(msg, _configurationService.Current.ErrorNotification);
                break;
        }
    }

    private void ShowNotificationLocationBased(
        NotificationMessage msg,
        NotificationLocation location
    )
    {
        switch (location)
        {
            case NotificationLocation.Toast:
                ShowToast(msg);
                break;

            case NotificationLocation.Chat:
                ShowChat(msg);
                break;

            case NotificationLocation.Both:
                ShowToast(msg);
                ShowChat(msg);
                break;

            case NotificationLocation.Nowhere:
                break;
        }
    }

    private void ShowToast(NotificationMessage msg)
    {
        var dalamudType = msg.Type switch
        {
            NotificationType.Error => Dalamud.Interface.ImGuiNotification.NotificationType.Error,
            NotificationType.Warning => Dalamud
                .Interface
                .ImGuiNotification
                .NotificationType
                .Warning,
            _ => Dalamud.Interface.ImGuiNotification.NotificationType.Info,
        };

        _notificationManager.AddNotification(
            new Notification()
            {
                Content = msg.Message ?? string.Empty,
                Title = msg.Title,
                Type = dalamudType,
                Minimized = false,
                InitialDuration = msg.TimeShownOnScreen ?? TimeSpan.FromSeconds(3),
            }
        );
    }
}