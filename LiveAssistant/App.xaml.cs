//    Copyright (C) 2023  Live Assistant official Windows app Authors
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Web;
using Windows.ApplicationModel.Activation;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using LiveAssistant.Common;
using LiveAssistant.Common.Messages;
using LiveAssistant.Database;
using Microsoft.UI.Xaml;
using LiveAssistant.Pages;
using LiveAssistant.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Sentry;
using Sentry.Protocol;
using WinRT;
using static PInvoke.User32;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;

namespace LiveAssistant;

public partial class App
{
    public App()
    {
        if (_appSettings.SendDiagnosticData)
        {
            SentrySdk.Init(options =>
            {
                options.Dsn = Env.SentryDsn;

#if DEBUG
                options.Debug = true;
                options.TracesSampleRate = 1.0;
                options.DiagnosticLevel = SentryLevel.Debug;

#else
                options.Debug = false;
                options.TracesSampleRate = 0.2;
                options.DiagnosticLevel = SentryLevel.Warning;
#endif
            });

            Current.UnhandledException += OnUnhandledException;
        }

        MainQueue = DispatcherQueue.GetForCurrentThread();
        Services = ConfigureServices();

        InitializeComponent();
    }

    private readonly AppSettings _appSettings = AppSettings.Get();

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var appArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        var mainInstance = AppInstance.FindOrRegisterForKey("main");

        if (!mainInstance.IsCurrent)
        {
            await mainInstance.RedirectActivationToAsync(appArgs);
            Process.GetCurrentProcess().Kill();
            return;
        }

        AppInstance.GetCurrent().Activated += OnAppActivated;
        _mainWindow = new MainWindow();

        HandleActivation(appArgs);
    }

    /// <summary>
    /// Handle activation.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnAppActivated(object? sender, AppActivationArguments args)
    {
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(_mainWindow);
        ShowWindow(handle, WindowShowStyle.SW_RESTORE);
        SetForegroundWindow(handle);
        HandleActivation(args);
    }

    private Window? _mainWindow;

    /// <summary>
    /// Handle the activation args.
    /// </summary>
    /// <param name="args"></param>
    private void HandleActivation(AppActivationArguments args)
    {
        switch (args.Kind)
        {
            case ExtendedActivationKind.Protocol:
                var protocolArgs = args.Data.As<IProtocolActivatedEventArgs>();
                var uri = protocolArgs.Uri;

                switch (uri.Host)
                {
                    case "twitch":
                        WeakReferenceMessenger.Default.Send(new CompleteTwitchOAuthMessage(uri.Fragment));
                        break;
                    case "overlay-provider":
                        try
                        {
                            var queries = HttpUtility.ParseQueryString(uri.Query);
                            var url = queries.Get("configUrl");
                            if (string.IsNullOrEmpty(url)) return;

                            Current.MainQueue.TryEnqueue(delegate
                            {
                                WeakReferenceMessenger.Default.Send(new ShouldAddNewOverlayProviderMessage(url));
                            });
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                            WeakReferenceMessenger.Default.Send(new ShowInfoBarMessage(Helpers.GetExceptionInfoBar(e)));
                        }
                        break;
                }
                break;

            case ExtendedActivationKind.File:
                var fileArgs = args.Data.As<IFileActivatedEventArgs>();
                var file = fileArgs.Files[0];
                var path = file.Path;
                if (!path.EndsWith(".lapack")) return;

                MainQueue.TryEnqueue(delegate
                {
                    WeakReferenceMessenger.Default.Send(new ShouldAddNewOverlayPackageMessage(file));
                });
                break;
        }
    }

    /// <summary>
    /// Gets the current <see cref="App"/> instance in use.
    /// </summary>
    public new static App Current => (App)Application.Current;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
    /// </summary>
    internal IServiceProvider Services { get; }

    public readonly DispatcherQueue MainQueue;

    /// <summary>
    /// Configures the services for the application.
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var serviceProvider = new ServiceCollection()
            .AddSingleton<SocketServerViewModel>()
            .AddSingleton(new AppSettingsViewModel())
            .AddSingleton(new DataProcessorViewModel())
            .AddSingleton(new TwitchOAuthViewModel())
            .AddSingleton<SessionViewModel>()
            .AddSingleton<HistoryViewModel>()
            .AddSingleton(new OverlayViewModel())
            .AddSingleton<TutorialViewModel>()
            .BuildServiceProvider();

        Ioc.Default.ConfigureServices(serviceProvider);

        return serviceProvider;
    }

    /// <summary>
    /// Catch unhandled exceptions
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        var exception = e.Exception;
        if (exception == null) return;

        exception.Data[Mechanism.HandledKey] = false;
        exception.Data[Mechanism.MechanismKey] = "Application.UnhandledException";
        SentrySdk.CaptureException(exception);

        SentrySdk.FlushAsync(TimeSpan.FromSeconds(3)).Wait();
    }
}
