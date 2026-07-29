using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ASMED.EDM.UI.Services;

/// <summary>
/// Typ powiadomienia (wpływa na kolor/ikonę).
/// </summary>
public enum ToastType
{
    Information,
    Success,
    Warning,
    Error
}

/// <summary>
/// Pozycja powiadomienia w oknie.
/// </summary>
public enum NotificationPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

/// <summary>
/// Interfejs serwisu powiadomień toast.
/// </summary>
public interface INotificationService
{
    void ShowToast(
        string message,
        ToastType type = ToastType.Information,
        int durationMs = 3000,
        NotificationPosition position = NotificationPosition.BottomRight);
}

/// <summary>
/// Prosta implementacja powiadomień toast w WPF (popup w rogu okna).
/// </summary>
public class NotificationService : INotificationService
{
    private readonly Window? _owner;

    public NotificationService(Window? owner = null)
    {
        _owner = owner;
    }

    public void ShowToast(
        string message,
        ToastType type = ToastType.Information,
        int durationMs = 3000,
        NotificationPosition position = NotificationPosition.BottomRight)
    {
        var owner = _owner ?? Application.Current?.MainWindow;
        if (owner == null) return;

        owner.Dispatcher.Invoke(() =>
        {
            var popup = new System.Windows.Controls.Primitives.Popup
            {
                AllowsTransparency = true,
                PlacementTarget = owner,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Relative,
                StaysOpen = true,
                IsOpen = false
            };

            var border = new Border
            {
                Background = GetBrushForType(type),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(16, 10, 16, 10),
                Margin = new Thickness(10),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 10,
                    ShadowDepth = 2,
                    Opacity = 0.3
                }
            };

            var textBlock = new TextBlock
            {
                Text = $"{GetIconForType(type)} {message}",
                Foreground = Brushes.White,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 350
            };

            border.Child = textBlock;
            popup.Child = border;

            // Oblicz pozycję
            owner.UpdateLayout();
            popup.IsOpen = true;
            popup.UpdateLayout();

            double offsetX = 0, offsetY = 0;
            double popupWidth = border.ActualWidth > 0 ? border.ActualWidth : 200;
            double popupHeight = border.ActualHeight > 0 ? border.ActualHeight : 50;

            switch (position)
            {
                case NotificationPosition.TopLeft:
                    offsetX = 10;
                    offsetY = 10;
                    break;
                case NotificationPosition.TopRight:
                    offsetX = owner.ActualWidth - popupWidth - 20;
                    offsetY = 10;
                    break;
                case NotificationPosition.BottomLeft:
                    offsetX = 10;
                    offsetY = owner.ActualHeight - popupHeight - 40;
                    break;
                case NotificationPosition.BottomRight:
                default:
                    offsetX = owner.ActualWidth - popupWidth - 20;
                    offsetY = owner.ActualHeight - popupHeight - 40;
                    break;
            }

            popup.HorizontalOffset = offsetX;
            popup.VerticalOffset = offsetY;

            // Auto-zamknij po czasie
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                popup.IsOpen = false;
            };
            timer.Start();
        });
    }

    private static Brush GetBrushForType(ToastType type) => type switch
    {
        ToastType.Success => new SolidColorBrush(Color.FromRgb(34, 139, 34)),   // zielony
        ToastType.Warning => new SolidColorBrush(Color.FromRgb(255, 165, 0)),   // pomarańczowy
        ToastType.Error => new SolidColorBrush(Color.FromRgb(220, 53, 69)),     // czerwony
        _ => new SolidColorBrush(Color.FromRgb(0, 120, 215))                     // niebieski (info)
    };

    private static string GetIconForType(ToastType type) => type switch
    {
        ToastType.Success => "✅",
        ToastType.Warning => "⚠️",
        ToastType.Error => "❌",
        _ => "ℹ️"
    };
}