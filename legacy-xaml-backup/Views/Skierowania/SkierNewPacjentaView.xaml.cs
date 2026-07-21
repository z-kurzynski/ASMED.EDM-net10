using ASMED.WPF.ViewModels;
using System.Windows.Controls;
using ASMED.WPF.ViewModels.Skierowania;
using Syncfusion.UI.Xaml.Scheduler;
using System;
using System.Windows;
using System.Windows.Input;
using ScheduleAppointment = Syncfusion.UI.Xaml.Scheduler.ScheduleAppointment;
using ASMED.WPF.Helpers;


namespace ASMED.WPF.Views
{
    public partial class SkierNewPacjentaView : UserControl
    {
        public SkierNewPacjentaView()
        {
            InitializeComponent();
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("pl-PL");

            // Do not override DataContext at runtime when using DataTemplate.
            // Only provide a design-time DataContext for the XAML designer.
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                this.DataContext = new SkierNewPacjentaViewModel();
                SfScheduler schedule = new SfScheduler();
                this.Content = schedule;
            }
            this.Loaded += SkierNewPacjentaView_Loaded;
        }


        private void Scheduler_PreviewMouseDoubleClick(object? sender, MouseButtonEventArgs e)
        {
            var scheduler = sender as SfScheduler;
            if (scheduler == null)
                return;

            if (scheduler.ViewType == SchedulerViewType.Month)
            {
                // Pobierz aktualną SelectedDate
                var date = scheduler.SelectedDate;
                if (date != null)
                {
                    // Przełącz widok na dzień
                    scheduler.ViewType = SchedulerViewType.Day;
                    scheduler.SelectedDate = date;
                    e.Handled = true;
                }
                else
                {
                    MessageBox.Show("Nie udało się ustalić daty. Spróbuj kliknąć raz, a potem podwójnie.", "DEBUG");
                }
            }
        }


        private void SkierNewPacjentaView_Loaded(object? sender, RoutedEventArgs e)
        {
            if (this.FindName("Schedule") is SfScheduler scheduler)
            {
                scheduler.AppointmentEditorClosing += Schedule_AppointmentEditorClosing;
                scheduler.AppointmentEditorOpening += Scheduler_AppointmentEditorOpening;
                scheduler.PreviewMouseDoubleClick += Scheduler_PreviewMouseDoubleClick;
            }

            // Refresh appointments when the view becomes visible (e.g., when switching tabs)
            this.IsVisibleChanged -= SkierNewPacjentaView_IsVisibleChanged;
            this.IsVisibleChanged += SkierNewPacjentaView_IsVisibleChanged;
        }

        private void SkierNewPacjentaView_IsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (this.IsVisible)
                {
                    if (this.DataContext is SkierNewPacjentaViewModel vm)
                    {
                        vm.RefreshAppointmentsFromDb();
                    }
                    else
                    {
                        // try to get from MainWindow DataContext if not set directly
                        if (Application.Current.MainWindow?.DataContext is MainWindowViewModel mainVm)
                        {
                            if (mainVm.NowaKartaBadanWidok is SkierNewPacjentaViewModel mv)
                                mv.RefreshAppointmentsFromDb();
                        }
                    }
                }
            }
            catch { }
        }

        private void Scheduler_AppointmentEditorOpening(object? sender, AppointmentEditorOpeningEventArgs e)
        {
            // Zablokuj domyślny edytor w widoku miesiąca
            var scheduler = sender as SfScheduler;
            if (scheduler != null && scheduler.ViewType == SchedulerViewType.Month)
                e.Cancel = true;
        }

        private void Schedule_AppointmentEditorClosing(object? sender, AppointmentEditorClosingEventArgs e)
        {
            if (!(e.Action.HasFlag(AppointmentEditorAction.Add) || e.Action.HasFlag(AppointmentEditorAction.Edit) || e.Action.HasFlag(AppointmentEditorAction.Delete))
                || e.Appointment is not ScheduleAppointment sched)
            {
                return;
            }

            var vm = this.DataContext as SkierNewPacjentaViewModel;

            // Spróbuj określić identyfikator skierowania (R_S_ID) z ViewModel, w przeciwnym razie spróbuj przeanalizować go z notatek/tematu spotkania
            int referralId = vm?.PatientSkierowanieId ?? 0;
            if (referralId == 0)
            {
                // try parse first integer in Notes or Subject
                string sample = sched.Notes ?? sched.Subject ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(sample))
                {
                    var parts = sample.Split(new[] { ' ', ',', ';', '(', ')', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var p in parts)
                    {
                        if (int.TryParse(p, out var val) && val > 0)
                        {
                            referralId = val;
                            break;
                        }
                    }
                }
            }

            // Avoid modal debug popups here – use Debug output if needed
            // System.Diagnostics.Debug.WriteLine($"Schedule_AppointmentEditorClosing Action={e.Action} ReferralId={referralId} Start={sched.StartTime} Notes='{sched.Notes}'");

            if (referralId == 0)
            {
                MessageBox.Show("Nie ustalono ID skierowania (R_S_ID). Dodawanie/edycja/usuwanie rejestracji wymaga powiązanego skierowania.", "Brak danych", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // default time range 11:00-15:00 is handled by DaysViewSettings in XAML

            var rec = new AccessDbContext.RejestracjaRecord
            {
                R_Data = sched.StartTime.Date,
                RStatus = "Wizyta",
                R_S_ID = referralId,
                R_GG_MM = sched.StartTime,
                R_Subject = sched.Subject,
                R_Uwagi = sched.Notes,
                R_P_ID = 0
            };

            var db = new ASMED.WPF.Helpers.AccessDbContext();

            try
            {
                // Get registrations as non-generic enumerable to avoid compile-time nested type resolution
                var allEnumerable = (System.Collections.IEnumerable)db.GetRejestracje();

                // Match by R_ID if appointment has explicit R_ID token (e.g. "R_ID:123"), otherwise by referralId + time
                int? appointmentRId = null;
                if (!string.IsNullOrWhiteSpace(sched.Notes))
                {
                    var notes = sched.Notes;
                    var idx = notes.IndexOf("R_ID", StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0)
                    {
                        var tail = notes.Substring(idx);
                        var digits = new string(tail.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());
                        if (int.TryParse(digits, out var parsedRid)) appointmentRId = parsedRid;
                    }
                }

                dynamic? existing = null;
                if (appointmentRId.HasValue)
                {
                    foreach (var it in allEnumerable)
                    {
                        dynamic r = it;
                        try { if (r.R_ID == appointmentRId.Value) { existing = r; break; } } catch { }
                    }
                }
                if (existing == null)
                {
                    foreach (var it in allEnumerable)
                    {
                        dynamic r = it;
                        try { if (r.R_S_ID == rec.R_S_ID && r.R_GG_MM != null && r.R_GG_MM == rec.R_GG_MM) { existing = r; break; } } catch { }
                    }
                }
                if (existing == null)
                {
                    foreach (var it in allEnumerable)
                    {
                        dynamic r = it;
                        try { if (r.R_S_ID == rec.R_S_ID) { existing = r; break; } } catch { }
                    }
                }
                // Priority: Delete -> Add -> Edit
                if (e.Action.HasFlag(AppointmentEditorAction.Delete))
                {
                    if (existing != null)
                    {
                        try { db.DeleteRejestracja((int)existing.R_ID); }
                        catch { /* log if needed */ }
                    }
                }
                else if (e.Action.HasFlag(AppointmentEditorAction.Add))
                {
                    // Always add new record for Add (do not treat as update)
                    db.AddRejestracjaReturnId(rec);
                }
                else if (e.Action.HasFlag(AppointmentEditorAction.Edit))
                {
                    if (existing != null)
                    {
                        try { db.UpdateRejestracja((int)existing.R_ID, rec); }
                        catch { db.AddRejestracja(rec); }
                    }
                    else
                    {
                        db.AddRejestracja(rec);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd operacji rejestracji: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            vm?.RefreshAppointmentsFromDb();
        }



        private void RadioButton_Checked(object? sender, RoutedEventArgs e)
        {

        }

        private void CheckBox_Checked(object? sender, RoutedEventArgs e)
        {

        }

        private void CheckBox_Checked_1(object? sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// ✅ Przełącza na zakładkę "Rejestracja"
        /// </summary>
        private void Rejestracja_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("SkierNewPacjentaView.Rejestracja_Click: Przełączanie na zakładkę Rejestracja...");

                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null)
                {
                    // System.Diagnostics.Debug.WriteLine("Rejestracja_Click: ❌ Nie znaleziono MainWindow");
                    MessageBox.Show("Nie można odnaleźć głównego okna aplikacji.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var rejestracjaTab = mainWindow.FindName("Rejestracja") as Syncfusion.Windows.Tools.Controls.TabItemExt;
                if (rejestracjaTab != null)
                {
                    rejestracjaTab.IsSelected = true;
                    // System.Diagnostics.Debug.WriteLine("Rejestracja_Click: ✅ Przełączono na zakładkę Rejestracja");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("Rejestracja_Click: ❌ Nie znaleziono zakładki Rejestracja");
                    MessageBox.Show("Nie można odnaleźć zakładki 'Rejestracja'.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"Rejestracja_Click ERROR: {ex.Message}");
                MessageBox.Show($"Błąd podczas przełączania zakładki:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ✅ Przełącza na zakładkę "Karty Badań" (SkierowaniaView)
        /// </summary>
        private void KartyBadan_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("KartyBadan_Click: Przełączanie na zakładkę Karty Badań...");

                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null)
                {
                    // System.Diagnostics.Debug.WriteLine("KartyBadan_Click: ❌ Nie znaleziono MainWindow");
                    MessageBox.Show("Nie można odnaleźć głównego okna aplikacji.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ✅ Znajdź zakładkę "Skierowania" (lista kart badań)
                var skierowaniaTab = mainWindow.FindName("Skierowania") as Syncfusion.Windows.Tools.Controls.TabItemExt;
                if (skierowaniaTab != null)
                {
                    skierowaniaTab.IsSelected = true;
                    // System.Diagnostics.Debug.WriteLine("KartyBadan_Click: ✅ Przełączono na zakładkę Karty Badań");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("KartyBadan_Click: ❌ Nie znaleziono zakładki Skierowania");
                    MessageBox.Show("Nie można odnaleźć zakładki 'Karty Badań'.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"KartyBadan_Click ERROR: {ex.Message}");
                MessageBox.Show($"Błąd podczas przełączania zakładki:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ✅ Przełącza na zakładkę "Zakończ Badanie" (BadaniaNew)
        /// </summary>
        private void ZakonczBadanie_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("ZakonczBadanie_Click: Przełączanie na zakładkę Zakończ Badanie...");

                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null)
                {
                    // System.Diagnostics.Debug.WriteLine("ZakonczBadanie_Click: ❌ Nie znaleziono MainWindow");
                    MessageBox.Show("Nie można odnaleźć głównego okna aplikacji.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var badaniaNewTab = mainWindow.FindName("BadaniaNew") as Syncfusion.Windows.Tools.Controls.TabItemExt;
                if (badaniaNewTab != null)
                {
                    badaniaNewTab.IsSelected = true;
                    // System.Diagnostics.Debug.WriteLine("ZakonczBadanie_Click: ✅ Przełączono na zakładkę Zakończ Badanie");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("ZakonczBadanie_Click: ❌ Nie znaleziono zakładki BadaniaNew");
                    MessageBox.Show("Nie można odnaleźć zakładki 'Zakończ Badanie'.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"ZakonczBadanie_Click ERROR: {ex.Message}");
                MessageBox.Show($"Błąd podczas przełączania zakładki:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ✅ Przełącza na zakładkę "Lista do Faktur"
        /// </summary>
        private void ListaDoFaktur_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("ListaDoFaktur_Click: Przełączanie na zakładkę Lista do Faktur...");

                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null)
                {
                    // System.Diagnostics.Debug.WriteLine("ListaDoFaktur_Click: ❌ Nie znaleziono MainWindow");
                    MessageBox.Show("Nie można odnaleźć głównego okna aplikacji.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var listaDoFakturTab = mainWindow.FindName("ListaDoFaktur") as Syncfusion.Windows.Tools.Controls.TabItemExt;
                if (listaDoFakturTab != null)
                {
                    listaDoFakturTab.IsSelected = true;
                    // System.Diagnostics.Debug.WriteLine("ListaDoFaktur_Click: ✅ Przełączono na zakładkę Lista do Faktur");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("ListaDoFaktur_Click: ❌ Nie znaleziono zakładki ListaDoFaktur");
                    MessageBox.Show("Nie można odnaleźć zakładki 'Lista do Faktur'.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"ListaDoFaktur_Click ERROR: {ex.Message}");
                MessageBox.Show($"Błąd podczas przełączania zakładki:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }


}
