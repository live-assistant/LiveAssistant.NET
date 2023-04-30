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
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Storage.Pickers;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Linearstar.Windows.RawInput;
using LiveAssistant.Common;
using LiveAssistant.Common.Messages;
using LiveAssistant.Common.Types;
using LiveAssistant.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using WinRT;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;
using WinUIEx.Messaging;

namespace LiveAssistant.Pages;

internal sealed partial class MainWindow
{
    private IntPtr WindowHandle => WinRT.Interop.WindowNative.GetWindowHandle(this);
    private WindowManager Manager => WindowManager.Get(this);
    private AppWindowTitleBar TitleBar => AppWindow.TitleBar;

    public MainWindow()
    {
        InitializeComponent();

        // Setup window
        Setup();
        TrySetMicaBackdrop();
        SetupNavigationView();

        // Register handlers
        // Initialize picker with window
        WeakReferenceMessenger.Default.Register<MainWindowInitializeFolderPickerMessage>(this, (_, m) =>
        {
            WinRT.Interop.InitializeWithWindow.Initialize(m.Value, WindowHandle);
        });

        // Show content dialog
        WeakReferenceMessenger.Default.Register<ShowContentDialogMessage>(this, (_, m) =>
        {
            var dialog = m.Value;
            dialog.XamlRoot = Content.XamlRoot;
            _ = dialog.ShowAsync();
        });

        // Show InfoBar
        WeakReferenceMessenger.Default.Register<ShowInfoBarMessage>(this, (_, message) =>
        {
            var bar = message.Value;
            bar.IsOpen = true;
            bar.IsClosable = true;
            bar.Closed += delegate
            {
                InfoBarsPanel.Children.Remove(bar);
            };
            InfoBarsPanel.Children.Add(bar);
        });

        // Raw input
        WeakReferenceMessenger.Default.Register<MainWindowAddRawInputHandlerMessage>(this, (_, m) =>
        {
            RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse, RawInputDeviceFlags.InputSink, WindowHandle);
            RawInputDevice.RegisterDevice(HidUsageAndPage.Keyboard, RawInputDeviceFlags.InputSink, WindowHandle);
            RawInputDevice.RegisterDevice(HidUsageAndPage.GamePad, RawInputDeviceFlags.InputSink, WindowHandle);
            Manager.WindowMessageReceived += m.Value;
        });
        WeakReferenceMessenger.Default.Register<MainWindowRemoveRawInputHandlerMessage>(this, (_, m) =>
        {
            RawInputDevice.UnregisterDevice(HidUsageAndPage.Mouse);
            RawInputDevice.UnregisterDevice(HidUsageAndPage.Keyboard);
            RawInputDevice.UnregisterDevice(HidUsageAndPage.GamePad);
            Manager.WindowMessageReceived -= m.Value;
        });

        // Open overlay explorer if a new provider/package added
        WeakReferenceMessenger.Default.Register<ShouldAddNewOverlayProviderMessage>(this, delegate
        {
            NavigationView.SelectedItem = NavigationView.MenuItems.FirstOrDefault(item =>
                item.As<NavigationViewItem>().Tag.As<ComboBoxItemValueSet<Type>>().Value ==
                typeof(OverlayPage));
        });
        WeakReferenceMessenger.Default.Register<ShouldAddNewOverlayPackageMessage>(this, delegate
        {
            NavigationView.SelectedItem = NavigationView.MenuItems.FirstOrDefault(item =>
                item.As<NavigationViewItem>().Tag.As<ComboBoxItemValueSet<Type>>().Value ==
                typeof(OverlayPage));
        });

        Closed += OnClosed;

        Activate();
    }

    // Setup Window
    private void Setup()
    {
        // Set window title
        AppWindow.Title = "AppDisplayName".Localize();

        // Customize title bar
        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        TitleBar.ButtonBackgroundColor = Colors.Transparent;
        TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        MinWidth = 1280;
        MinHeight = 720;

        // Set icon for the preview popup
        Helpers.SetWindowIcon(WindowHandle, "Images/Package/WindowIcon.ico");
    }

    // Theme
    private WindowsSystemDispatcherQueueHelper? _dispatcherQueueHelper;
    private MicaController? _micaController;
    private SystemBackdropConfiguration? _configurationSource;

    /// <summary>
    /// Try to setup Mica background for the window.
    /// </summary>
    private void TrySetMicaBackdrop()
    {
        if (!MicaController.IsSupported())
        {
            return;
        }

        _dispatcherQueueHelper = new WindowsSystemDispatcherQueueHelper();
        _dispatcherQueueHelper.EnsureWindowsSystemDispatcherQueueController();

        // Hooking up the policy object
        _configurationSource = new SystemBackdropConfiguration();
        Activated += Window_Activated;
        Closed += Window_Closed;
        ((FrameworkElement)Content).ActualThemeChanged += Window_ThemeChanged;

        // Initial configuration state.
        _configurationSource.IsInputActive = true;
        SetConfigurationSourceTheme();

        _micaController = new MicaController();

        // Enable the system backdrop.
        // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
        _micaController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
        _micaController.SetSystemBackdropConfiguration(_configurationSource);
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_configurationSource is null) return;
        _configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
        // use this closed window.
        if (_micaController != null)
        {
            _micaController.Dispose();
            _micaController = null;
        }
        Activated -= Window_Activated;
        _configurationSource = null;
    }

    private void Window_ThemeChanged(FrameworkElement sender, object args)
    {
        if (_configurationSource != null)
        {
            SetConfigurationSourceTheme();
        }
    }

    private void SetConfigurationSourceTheme()
    {
        if (_configurationSource is null) return;
        _configurationSource.Theme = ((FrameworkElement)Content).ActualTheme switch
        {
            ElementTheme.Dark => SystemBackdropTheme.Dark,
            ElementTheme.Light => SystemBackdropTheme.Light,
            ElementTheme.Default => SystemBackdropTheme.Default,
            _ => _configurationSource.Theme
        };
    }

    // Draggable title bar area
    private void DraggableArea_OnLoaded(object sender, RoutedEventArgs e)
    {
        SetDragArea();
    }

    private void DraggableArea_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        SetDragArea();
    }

    private void SetDragArea()
    {
        var scaleAdjustment = Helpers.GetScaleAdjustment(WindowHandle);

        var barTransform = DraggableArea.TransformToVisual(Content);
        var barPosition = barTransform.TransformPoint(new Point(0, 0));
        var barArea = new RectInt32
        {
            X = (int)(barPosition.X * scaleAdjustment),
            Y = 0,
            Width = (int)(DraggableArea.ActualWidth * scaleAdjustment),
            Height = (int)(48 * scaleAdjustment),
        };

        var iconTransform = Icon.TransformToVisual(Content);
        var iconPosition = iconTransform.TransformPoint(new Point(0, 0));
        var iconArea = new RectInt32
        {
            X = 0,
            Y = 0,
            Width = (int)((iconPosition.X + Icon.ActualWidth) * scaleAdjustment),
            Height = (int)(48 * scaleAdjustment),
        };

        TitleBar.SetDragRectangles(new[]
        {
            barArea,
            iconArea,
        });
    }

    // Navigation
    private void SetupNavigationView()
    {
        foreach (var set in _tabs)
        {
            NavigationView.MenuItems.Add(new NavigationViewItem
            {
                Content = set.Name,
                Tag = set,
            });
        }

        NavigationView.SelectedItem = NavigationView.MenuItems.FirstOrDefault();
    }

    private readonly ObservableCollection<ComboBoxItemValueSet<Type>> _tabs = new()
    {
        new ComboBoxItemValueSet<Type>
        {
            Name = "MainWindowTabNameRecorder".Localize(),
            Value = typeof(RecorderPage),
        },
        new ComboBoxItemValueSet<Type>
        {
            Name = "MainWindowTabNameHistory".Localize(),
            Value = typeof(HistoryPage),
        },
        new ComboBoxItemValueSet<Type>
        {
            Name = "MainWindowTabNameOverlay".Localize(),
            Value = typeof(OverlayPage),
        },
        new ComboBoxItemValueSet<Type>
        {
            Name = "MainWindowTabNameSettings".Localize(),
            Value = typeof(SettingsPage),
        },
    };

    private int _previousTabIndex;
    private void OnNavigate(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var set = (ComboBoxItemValueSet<Type>)args.SelectedItem.As<NavigationViewItem>().Tag;
        var index = _tabs.IndexOf(set);

        MainFrame.NavigateToType(
            set.Value,
            null,
            new FrameNavigationOptions
            {
                TransitionInfoOverride = new SlideNavigationTransitionInfo
                {
                    Effect = index > _previousTabIndex ? SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft,
                },
            });

        _previousTabIndex = index;
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        WeakReferenceMessenger.Default.Send(new MainWindowClosedMessage());
    }
}

public class MainWindowInitializeFolderPickerMessage : ValueChangedMessage<FolderPicker>
{
    public MainWindowInitializeFolderPickerMessage(FolderPicker value) : base(value) { }
}

public class MainWindowAddRawInputHandlerMessage : ValueChangedMessage<EventHandler<WindowMessageEventArgs>>
{
    public MainWindowAddRawInputHandlerMessage(EventHandler<WindowMessageEventArgs> value) : base(value) { }
}

public class MainWindowRemoveRawInputHandlerMessage : ValueChangedMessage<EventHandler<WindowMessageEventArgs>>
{
    public MainWindowRemoveRawInputHandlerMessage(EventHandler<WindowMessageEventArgs> value) : base(value) { }
}

public class MainWindowClosedMessage : ValueChangedMessage<bool>
{
    public MainWindowClosedMessage() : base(true) { }
}
