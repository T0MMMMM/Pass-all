using Avalonia.Controls;
using Avalonia.Input;

namespace Passall.Controls;

public partial class AppTitleBar : UserControl
{
    private Window? ParentWindow => TopLevel.GetTopLevel(this) as Window;

    public AppTitleBar()
    {
        InitializeComponent();
    }

    private void TitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            ParentWindow?.BeginMoveDrag(e);
    }

    private void MinimizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ParentWindow!.WindowState = WindowState.Minimized;

    private void MaximizeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ParentWindow!.WindowState = ParentWindow.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void CloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ParentWindow?.Close();
}
