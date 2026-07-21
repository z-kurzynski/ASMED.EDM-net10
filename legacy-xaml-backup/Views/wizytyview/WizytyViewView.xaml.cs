using ASMED.WPF.ViewModels;
using System.ComponentModel;
using System.Windows.Controls;
using Syncfusion.UI.Xaml.Scheduler;
using System.Windows;
using System.Linq;
using ASMED.WPF.Helpers;
using System;
using System.Windows.Media;
using System.Windows.Input;

namespace ASMED.WPF.Views
{
    public partial class WizytyViewView : UserControl
    {
        public WizytyViewView()
        {
            InitializeComponent();

            // Nie ustawiamy DataContext w trybie projektanta — zapobiega błędom wdesignerze.
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                DataContext = new WizytyViewViewModel();
            }

            this.Loaded += WizytyViewView_Loaded;

            // ✅ NOWE: Obsługa klawisza F2 (focus na wyszukiwaniu)
            this.PreviewKeyDown += WizytyViewView_PreviewKeyDown;
        }

        /// <summary>
        /// ✅ NOWA METODA: Przełącza na zakładkę "Karta Badań" (Skierowania)
        /// </summary>
        private void KartaBadan_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("KartaBadan_Click: Przełączanie na zakładkę Karta Badań...");

                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null)
                {
                    // System.Diagnostics.Debug.WriteLine("KartaBadan_Click: ❌ Nie znaleziono MainWindow");
                    MessageBox.Show("Nie można odnaleźć głównego okna aplikacji.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ✅ Znajdź zakładkę "Skierowania" (Karta Badań)
                var skierowaniaTab = mainWindow.FindName("Skierowania") as Syncfusion.Windows.Tools.Controls.TabItemExt;
                if (skierowaniaTab != null)
                {
                    skierowaniaTab.IsSelected = true;
                    // System.Diagnostics.Debug.WriteLine("KartaBadan_Click: ✅ Przełączono na zakładkę Karta Badań");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("KartaBadan_Click: ❌ Nie znaleziono zakładki Skierowania");
                    MessageBox.Show("Nie można odnaleźć zakładki 'Karta Badań'.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"KartaBadan_Click ERROR: {ex.Message}");
                MessageBox.Show($"Błąd podczas przełączania zakładki:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Przełącza na zakładkę "Edycja Badań" (BadaniaNew)
        /// </summary>
        private void EdycjaBadan_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("EdycjaBadan_Click: Przełączanie na zakładkę Edycja Badań...");

                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null)
                {
                    // System.Diagnostics.Debug.WriteLine("EdycjaBadan_Click: ❌ Nie znaleziono MainWindow");
                    MessageBox.Show("Nie można odnaleźć głównego okna aplikacji.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // ✅ Znajdź zakładkę "BadaniaNew" (Edycja Badań)
                var badaniaNewTab = mainWindow.FindName("BadaniaNew") as Syncfusion.Windows.Tools.Controls.TabItemExt;
                if (badaniaNewTab != null)
                {
                    badaniaNewTab.IsSelected = true;
                    // System.Diagnostics.Debug.WriteLine("EdycjaBadan_Click: ✅ Przełączono na zakładkę Edycja Badań");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("EdycjaBadan_Click: ❌ Nie znaleziono zakładki BadaniaNew");
                    MessageBox.Show("Nie można odnaleźć zakładki 'Edycja Badań'.",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"EdycjaBadan_Click ERROR: {ex.Message}");
                MessageBox.Show($"Błąd podczas przełączania zakładki:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void WizytyViewView_Loaded(object? sender, RoutedEventArgs e)
        {
            if (this.FindName("WizytyScheduler") is SfScheduler scheduler)
            {
                scheduler.AppointmentEditorClosing += Schedule_AppointmentEditorClosing;
                scheduler.AppointmentEditorOpening += Scheduler_AppointmentEditorOpening;
                scheduler.PreviewMouseDoubleClick += Scheduler_PreviewMouseDoubleClick;
                scheduler.SelectionChanged += WizytyScheduler_SelectionChanged;
                scheduler.SelectedDate = DateTime.Today; // domyślna data to dzisiaj
            }

            // Refresh appointments when the view becomes visible (e.g., switching tabs)
            this.IsVisibleChanged -= WizytyViewView_IsVisibleChanged;
            this.IsVisibleChanged += WizytyViewView_IsVisibleChanged;
        }

        /// <summary>
        /// ✅ NOWE: Obsługa zmiany wybranej daty w kalendarzu
        /// </summary>
        private void WizytyScheduler_SelectionChanged(object? sender, Syncfusion.UI.Xaml.Scheduler.SelectionChangedEventArgs e)
        {
            if (sender is SfScheduler scheduler && this.DataContext is WizytyViewViewModel vm)
            {
                // Pobierz wybraną datę z kalendarza
                if (scheduler.SelectedDate != null)
                {
                    vm.SelectedDate = scheduler.SelectedDate;
                    // System.Diagnostics.Debug.WriteLine($"📅 Wybrano datę w kalendarzu: {scheduler.SelectedDate.Value:yyyy-MM-dd}");
                }
            }
        }

        private void WizytyViewView_IsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (this.IsVisible)
                {
                    if (this.DataContext is WizytyViewViewModel vm)
                    {
                        // ✅ KLUCZOWE: Sprawdź checkbox domyślnej daty PRZED odświeżeniem
                        // System.Diagnostics.Debug.WriteLine("WizytyViewView: Zakładka stała się widoczna - sprawdzam checkbox domyślnej daty");

                        if (vm.UseCustomDefaultDateCalendar)
                        {
                            // Checkbox zaznaczony → użyj CustomDefaultDateCalendar
                            vm.SelectedDate = vm.CustomDefaultDateCalendar;
                            // System.Diagnostics.Debug.WriteLine($"WizytyViewView: ✅ Checkbox zaznaczony - SelectedDate = {vm.CustomDefaultDateCalendar:dd-MM-yyyy}");
                        }
                        else
                        {
                            // Checkbox odznaczony → użyj DateTime.Now
                            vm.SelectedDate = DateTime.Now;
                            // System.Diagnostics.Debug.WriteLine($"WizytyViewView: ❌ Checkbox odznaczony - SelectedDate = {DateTime.Now:dd-MM-yyyy}");
                        }

                        // POTEM odśwież kalendarz
                        vm.RefreshFromDb();
                    }
                    else
                    {
                        // fallback: try to find viewmodel on MainWindow if not set
                        if (Application.Current.MainWindow?.DataContext is MainWindowViewModel mainVm)
                        {
                            // if there's a property exposing Wizyty VM, try to use it (best-effort)
                            var prop = mainVm.GetType().GetProperty("WizytyViewModel");
                            if (prop != null)
                            {
                                if (prop.GetValue(mainVm) is WizytyViewViewModel mv)
                                    mv.RefreshFromDb();
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void Scheduler_AppointmentEditorOpening(object? sender, AppointmentEditorOpeningEventArgs e)
        {
            // Optionally cancel in Month view to force day view editing
            if (sender is SfScheduler scheduler && scheduler.ViewType == SchedulerViewType.Month)
            {
                e.Cancel = false; // allow opening; change to true to block
            }
        }

        private void Scheduler_PreviewMouseDoubleClick(object? sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var scheduler = sender as SfScheduler;
            if (scheduler == null) return;
            if (scheduler.ViewType == SchedulerViewType.Month)
            {
                var date = scheduler.SelectedDate;
                if (date != null)
                {
                    scheduler.ViewType = SchedulerViewType.Day;
                    scheduler.SelectedDate = date;
                    e.Handled = true;
                }
            }
        }

        private void Schedule_AppointmentEditorClosing(object? sender, AppointmentEditorClosingEventArgs e)
        {
            if (!(e.Action.HasFlag(AppointmentEditorAction.Add) || e.Action.HasFlag(AppointmentEditorAction.Edit) || e.Action.HasFlag(AppointmentEditorAction.Delete))
                || e.Appointment is not ScheduleAppointment sched)
            {
                return;
            }

            var vm = this.DataContext as WizytyViewViewModel;

            // determine referral id from Notes or Subject if present, else 0
            int? referralId = null;
            string sample = sched.Notes ?? sched.Subject ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(sample))
            {
                // look for explicit tokens like R_S_ID:123 or S_ID:123
                var idxS = sample.IndexOf("R_S_ID:", StringComparison.OrdinalIgnoreCase);
                if (idxS < 0) idxS = sample.IndexOf("S_ID:", StringComparison.OrdinalIgnoreCase);
                if (idxS >= 0)
                {
                    var tail = sample.Substring(idxS);
                    var digits = new string(tail.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());
                    if (int.TryParse(digits, out var sid) && sid > 0) referralId = sid;
                }
            }

            // try to extract explicit appointment R_ID from Notes (token R_ID:123 or R_ID=123)
            int? appointmentRId = null;
            if (!string.IsNullOrWhiteSpace(sample))
            {
                var idx = sample.IndexOf("R_ID", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var tail = sample.Substring(idx);
                    var digits = new string(tail.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());
                    if (int.TryParse(digits, out var parsedRid)) appointmentRId = parsedRid;
                }
            }

            // default time range 11:00-15:00 is handled by DaysViewSettings in XAML

            var rec = new AccessDbContext.RejestracjaRecord
            {
                R_Data = sched.StartTime.Date,
                RStatus = "Wizyta",
                R_S_ID = referralId ?? 0,
                R_GG_MM = sched.StartTime,
                R_Subject = sched.Subject,
                R_Uwagi = sched.Notes,
                R_P_ID = 0
            };

            var db = new AccessDbContext();

            try
            {
                var all = db.GetRejestracje();

                // find existing: prefer appointmentRId if present
                var existing = (appointmentRId.HasValue) ? all.FirstOrDefault(r => r.R_ID == appointmentRId.Value) : null;
                if (existing == null && referralId.HasValue)
                {
                    existing = all.FirstOrDefault(r => r.R_S_ID == referralId.Value && r.R_GG_MM.HasValue && r.R_GG_MM.Value == rec.R_GG_MM);
                }
                // As last resort for Edit/Delete, match by exact datetime only (avoid for Add)
                if (existing == null && (e.Action.HasFlag(AppointmentEditorAction.Edit) || e.Action.HasFlag(AppointmentEditorAction.Delete)))
                {
                    existing = all.FirstOrDefault(r => r.R_GG_MM.HasValue && r.R_GG_MM.Value == rec.R_GG_MM);
                }

                // Decide action by exact e.Action value to avoid overlapping flags
                // System.Diagnostics.Debug.WriteLine($"AppointmentEditorClosing Action={e.Action}");
                bool isAdd = e.Action.HasFlag(AppointmentEditorAction.Add);
                bool isEdit = e.Action.HasFlag(AppointmentEditorAction.Edit);
                bool isDelete = e.Action.HasFlag(AppointmentEditorAction.Delete);

                // System.Diagnostics.Debug.WriteLine($"AppointmentEditorClosing Action={e.Action}, isAdd={isAdd}, isEdit={isEdit}, isDelete={isDelete}");

                // Prefer Delete > Add > Edit when multiple flags present
                if (e.Action.HasFlag(AppointmentEditorAction.Delete))
                {
                    //System.Diagnostics.Debug.WriteLine("Wizyty: handling Delete");
                    if (appointmentRId.HasValue)
                        db.DeleteRejestracja(appointmentRId.Value);
                    else if (existing != null && existing.R_ID.HasValue)
                        db.DeleteRejestracja(existing.R_ID.Value);
                }
                else if (e.Action.HasFlag(AppointmentEditorAction.Add))
                {
                    // System.Diagnostics.Debug.WriteLine("Wizyty: handling Add");
                    try { sched.AppointmentBackground = new SolidColorBrush(Colors.LightGreen); } catch { }
                    int newRid = db.AddRejestracjaReturnId(rec);
                    // System.Diagnostics.Debug.WriteLine($"Wizyty: Add returned newRid={newRid}");
                    if (newRid > 0)
                    {
                        //  var notes = (sched.Notes ?? string.Empty).Trim();
                        //  if (!notes.Contains("R_ID:") && !notes.Contains("R_ID="))
                        //      sched.Notes = string.IsNullOrWhiteSpace(notes) ? $"R_ID:{newRid}" : notes + " R_ID:" + newRid.ToString();
                    }
                }
                else if (e.Action.HasFlag(AppointmentEditorAction.Edit))
                {
                    // System.Diagnostics.Debug.WriteLine("Wizyty: handling Edit");
                    if (appointmentRId.HasValue)
                    {
                        db.UpdateRejestracja(appointmentRId.Value, rec);
                    }
                    else if (existing != null && existing.R_ID.HasValue)
                    {
                        db.UpdateRejestracja(existing.R_ID.Value, rec);
                    }
                    else
                    {
                        int rid = db.AddRejestracjaReturnId(rec);
                        if (rid > 0)
                        {
                            // var notes = (sched.Notes ?? string.Empty).Trim();
                            // if (!notes.Contains("R_ID:") && !notes.Contains("R_ID="))
                            //     sched.Notes = string.IsNullOrWhiteSpace(notes) ? $"R_ID:{rid}" : notes + " R_ID:" + rid.ToString();
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                // MessageBox.Show($"Błąd operacji rejestracji: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                // System.Diagnostics.Debug.WriteLine($"Błąd operacji rejestracji: {ex.Message}");
            }

            vm?.RefreshFromDb();
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
        /// ✅ NOWA: Obsługa kliknięcia w komórkę kalendarza zmiany terminu
        /// </summary>
        private void WizytyZmianScheduler_CellTapped(object? sender, CellTappedEventArgs e)
        {
            if (DataContext is WizytyViewViewModel viewModel)
            {
                try
                {
                    // Ustaw nową datę i godzinę na podstawie klikniętej komórki
                    viewModel.NewAppointmentDate = e.DateTime;
                    // System.Diagnostics.Debug.WriteLine($"📅 CellTapped - Wybrano nowy termin: {e.DateTime:yyyy-MM-dd HH:mm}");
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"❌ Błąd CellTapped: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// ✅ Obsługa klawisza F2 - focus na pole wyszukiwania
        /// </summary>
        private void WizytyViewView_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                try
                {
                    // 1️⃣ Wyczyść filtr w ViewModelu
                    if (DataContext is WizytyViewViewModel vm)
                    {
                        vm.FilterTextNazwisko = string.Empty;
                        // System.Diagnostics.Debug.WriteLine("🔍 F2: Wyczyszczono filtr");
                    }

                    // 2️⃣ Znajdź TextBox wyszukiwania i ustaw focus
                    var searchBox = FindSearchTextBox(this);
                    if (searchBox != null)
                    {
                        searchBox.Focus();
                        searchBox.SelectAll(); // Zaznacz całą zawartość (jeśli coś było)
                        // System.Diagnostics.Debug.WriteLine("🔍 F2: Ustawiono focus na pole wyszukiwania");
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine("⚠️ F2: Nie znaleziono TextBox wyszukiwania");
                    }

                    e.Handled = true; // Zapobiegnij dalszemu przetwarzaniu F2
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"❌ F2 Error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Pomocnicza metoda do znalezienia TextBox wyszukiwania w drzewie wizualnym
        /// </summary>
        private TextBox? FindSearchTextBox(DependencyObject parent)
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // Sprawdź czy to nasz TextBox (bindowany do FilterTextNazwisko)
                if (child is TextBox textBox)
                {
                    var binding = textBox.GetBindingExpression(TextBox.TextProperty);
                    if (binding?.ParentBinding?.Path?.Path == "FilterTextNazwisko")
                    {
                        return textBox;
                    }
                }

                // Rekurencyjnie szukaj w dzieciach
                var result = FindSearchTextBox(child);
                if (result != null) return result;
            }

            return null;
        }

        // Provide a runtime InitializeComponent if the generated partial method is not available.
        // This loads the XAML manually. If the project generator creates InitializeComponent, there will be no conflict.
    }
}
