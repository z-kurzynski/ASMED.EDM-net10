using ASMED.EDM.UI.ViewModels;
using ASMED.EDM.UI.Views.Skierowania;
using ASMED.EDM.UI.Views.wizytyview;
using ASMED.EDM.UI.Views.badania;
using ASMED.EDM.UI.Views.Badanie;
using ASMED.EDM.UI.Views.Orzeczenie;
using ASMED.EDM.UI.Views.lista_do_faktur;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.IO;
using System.Text;

namespace ASMED.EDM.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // Słownik: ContentControl zakładki → factory odtwarzający domyślny widok
    private Dictionary<ContentControl, Func<UserControl>> _tabFactories = new();

    // Uchwyt monitora, na którym aktualnie znajduje się okno — do wykrywania przeniesienia na inny ekran
    private IntPtr _currentMonitor = IntPtr.Zero;

    public MainWindow(MainWindowViewModel viewModel)
    {
        Console.WriteLine("🏠 MainWindow: Constructor started");
        InitializeComponent();
        DataContext = viewModel;

        // Przekieruj Console.Out tak, aby każde Console.Write uruchamiało cykl paska postępu
        try
        {
            var originalOut = Console.Out;
            Console.SetOut(new ProgressConsoleWriter(originalOut, viewModel));
        }
        catch { /* ignoruj jeśli nie można przekierować konsoli */ }

        Console.WriteLine("📋 MainWindow: Creating tab views...");
        // Ustaw domyślne widoki wszystkich zakładek
        NowaKartaContent.Content      = new SkierListaPacjentowView();
        RejestracjaContent.Content    = new WizytyViewView();
        KartyBadanContent.Content     = new SkierowaniaView();

        Console.WriteLine("🔍 MainWindow: Creating BadaniaNewView...");
        ZakonczBadanieContent.Content = new BadaniaNewView();

        Console.WriteLine("📝 MainWindow: Creating BadaniaEditNewView...");
        EdycjaBadanContent.Content    = new BadaniaEditNewView();

        ListaDoFakturContent.Content  = new ListaFaktAddView();
        BadanieContent.Content        = new BadanieView();
        OrzeczenieContent.Content     = new OrzeczenieView();
        Console.WriteLine("✅ MainWindow: All tab views created");

        // Słownik factory — double-click na zakładce odtworzy domyślny widok
        _tabFactories = new Dictionary<ContentControl, Func<UserControl>>
        {
            { NowaKartaContent,       () => new SkierListaPacjentowView() },
            { RejestracjaContent,     () => new WizytyViewView() },
            { KartyBadanContent,      () => new SkierowaniaView() },
            { ZakonczBadanieContent,  () => new BadaniaNewView() },
            { EdycjaBadanContent,     () => new BadaniaEditNewView() },
            { ListaDoFakturContent,   () => new ListaFaktAddView() },
            { BadanieContent,         () => new BadanieView() },
            { OrzeczenieContent,      () => new OrzeczenieView() },
        };

        SourceInitialized += MainWindow_SourceInitialized;
        LocationChanged   += MainWindow_LocationChanged;

        // Lazy-load widoku EDM/P1 — tworzy się dopiero gdy ContentControl jest widoczny
        EdmP1TestContent.Loaded += EdmP1TestContent_Loaded;
        Console.WriteLine("✅ MainWindow: Constructor completed");
    }

    private void EdmP1TestContent_Loaded(object sender, RoutedEventArgs e)
    {
        if (EdmP1TestContent.Content is not null) return;
        try
        {
            var app = System.Windows.Application.Current as App
                      ?? throw new InvalidOperationException("App nie jest dostępna.");

            var vm   = app.Host.Services.GetRequiredService<ViewModels.P1.EdmP1TestViewModel>();
            var view = new Views.Settings.EdmP1TestView { DataContext = vm };

            EdmP1TestContent.DataContext = vm;
            EdmP1TestContent.Content     = view;
        }
        catch (Exception ex)
        {
            // Zbierz pełny łańcuch błędów do wyświetlenia
            var msg = new System.Text.StringBuilder();
            var e2  = ex;
            while (e2 is not null)
            {
                msg.AppendLine(e2.GetType().Name + ": " + e2.Message);
                e2 = e2.InnerException;
                if (e2 is not null) msg.AppendLine("  ↳ ");
            }

            // Zapisz do pliku — żeby móc odczytać pełny błąd
            try
            {
                var logDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ASMED.EDM.UI");
                System.IO.Directory.CreateDirectory(logDir);
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(logDir, "edm_load_error.log"),
                    "[" + DateTime.Now + "]\n" + ex.ToString());
            }
            catch { /* ignoruj błędy logowania */ }

            EdmP1TestContent.Content = new TextBlock
            {
                Text         = "❌ Błąd ładowania widoku EDM:\n\n" + msg,
                Foreground   = System.Windows.Media.Brushes.Red,
                Margin       = new Thickness(20),
                TextWrapping = TextWrapping.Wrap,
                FontSize     = 12
            };
        }
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _currentMonitor = GetCurrentMonitorHandle();
    }

    private void MainWindow_LocationChanged(object? sender, EventArgs e)
    {
        // Nie dopasowuj okna, dopóki nie zostanie w pełni załadowane i wyrenderowane
        // (np. podczas pierwszego pozycjonowania startowego ActualWidth/ActualHeight = 0).
        if (!IsLoaded || WindowState != WindowState.Normal || ActualWidth <= 0 || ActualHeight <= 0)
            return;

        var monitor = GetCurrentMonitorHandle();
        if (monitor == IntPtr.Zero || monitor == _currentMonitor)
            return;

        _currentMonitor = monitor;
        AdjustWindowToMonitor(monitor);
    }

    private IntPtr GetCurrentMonitorHandle()
    {
        var helper = new WindowInteropHelper(this);
        if (helper.Handle == IntPtr.Zero)
            return IntPtr.Zero;

        return MonitorFromWindow(helper.Handle, MONITOR_DEFAULTTONEAREST);
    }

    /// <summary>
    /// Dopasowuje pozycję i rozmiar okna do obszaru roboczego nowego monitora
    /// po przeniesieniu okna na inny ekran.
    /// </summary>
    private void AdjustWindowToMonitor(IntPtr monitor)
    {
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(monitor, ref info))
            return;

        var source = PresentationSource.FromVisual(this);
        var toDips = source?.CompositionTarget != null
            ? source.CompositionTarget.TransformFromDevice
            : System.Windows.Media.Matrix.Identity;

        var workAreaTopLeft = toDips.Transform(new Point(info.rcWork.Left, info.rcWork.Top));
        var workAreaSize = toDips.Transform(new Point(
            info.rcWork.Right - info.rcWork.Left,
            info.rcWork.Bottom - info.rcWork.Top));

        double availableWidth = workAreaSize.X;
        double availableHeight = workAreaSize.Y;

        double newWidth = Math.Min(ActualWidth, availableWidth);
        double newHeight = Math.Min(ActualHeight, availableHeight);

        if (!double.IsNaN(MinWidth) && MinWidth > 0)
            newWidth = Math.Max(newWidth, Math.Min(MinWidth, availableWidth));
        if (!double.IsNaN(MinHeight) && MinHeight > 0)
            newHeight = Math.Max(newHeight, Math.Min(MinHeight, availableHeight));

        if (newWidth <= 0 || newHeight <= 0)
            return;

        Width = newWidth;
        Height = newHeight;

        double newLeft = Left;
        double newTop = Top;

        if (newLeft < workAreaTopLeft.X)
            newLeft = workAreaTopLeft.X;
        if (newTop < workAreaTopLeft.Y)
            newTop = workAreaTopLeft.Y;
        if (newLeft + newWidth > workAreaTopLeft.X + availableWidth)
            newLeft = workAreaTopLeft.X + availableWidth - newWidth;
        if (newTop + newHeight > workAreaTopLeft.Y + availableHeight)
            newTop = workAreaTopLeft.Y + availableHeight - newHeight;

        Left = newLeft;
        Top = newTop;
    }

    private const int MONITOR_DEFAULTTONEAREST = 2;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    /// <summary>
    /// Podmienia zawartość zakładki "Nowa Karta" na dowolny UserControl.
    /// </summary>
    public void SetNowaKartaContent(UserControl control)
    {
        NowaKartaContent.Content = control;
    }

    /// <summary>
    /// Podmienia zawartość zakładki "Karty Badań" na dowolny UserControl
    /// (np. na SkierPacjentaView podczas edycji istniejącej karty).
    /// </summary>
    public void SetKartyBadanContent(UserControl control)
    {
        KartyBadanContent.Content = control;
    }

    /// <summary>Przełącza aktywną zakładkę na "Rejestracja".</summary>
    public void SelectRejestracjaTab() => MainTabControl.SelectedItem = tabRejestracja;

    /// <summary>Przełącza aktywną zakładkę na "Nowa Karta".</summary>
    public void SelectNowaKartaTab() => MainTabControl.SelectedItem = tabNowaKarta;

    /// <summary>Przełącza aktywną zakładkę na "Karty Badań".</summary>
    public void SelectKartyBadanTab() => MainTabControl.SelectedItem = tabKartyBadan;

    /// <summary>Przełącza aktywną zakładkę na "Zakończ Badanie".</summary>
    public void SelectZakonczBadanieTab() => MainTabControl.SelectedItem = tabZakonczBadanie;

    /// <summary>Przełącza aktywną zakładkę na "Lista Do Faktur".</summary>
    public void SelectListaDoFakturTab() => MainTabControl.SelectedItem = tabListaDoFaktur;

    private void TabControl_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var tc = sender as TabControl;
        if (tc == null) return;

        // Znajdź TabItem pod kursorem myszki
        var dep = (DependencyObject)e.OriginalSource;
        while (dep != null && dep is not TabItem)
            dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);

        if (dep is not TabItem tab) return;

        // Sprawdź, czy TabItem zawiera ContentControl z naszego słownika
        if (tab.Content is ContentControl cc && _tabFactories.TryGetValue(cc, out var factory))
        {
            cc.Content = factory();
            e.Handled = true;
        }
    }
}

internal class ProgressConsoleWriter : TextWriter
{
    private readonly TextWriter _inner;
    private readonly MainWindowViewModel _vm;

    public ProgressConsoleWriter(TextWriter inner, MainWindowViewModel vm)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _vm = vm ?? throw new ArgumentNullException(nameof(vm));
    }

    public override Encoding Encoding => _inner.Encoding ?? Encoding.UTF8;

    public override void Write(char value)
    {
        try { _inner.Write(value); } catch { }
        try { _vm.StartConsoleProgressCycle(); } catch { }
    }

    public override void Write(string? value)
    {
        try { _inner.Write(value); } catch { }
        try { _vm.StartConsoleProgressCycle(); } catch { }
    }

    public override void WriteLine(string? value)
    {
        try { _inner.WriteLine(value); } catch { }
        try { _vm.StartConsoleProgressCycle(); } catch { }
    }

    public override void Flush()
    {
        try { _inner.Flush(); } catch { }
    }
}
