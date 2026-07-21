using ASMED.WPF.Helpers;
using ASMED.WPF.ViewModels;
using ASMED.WPF.ViewModels.ListaDoFaktur;
using ASMED.WPF.Views.lista_do_faktur;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;

namespace ASMED.WPF.Views
{
    public partial class ListaDoFaktur_DetailView : UserControl
    {
        public ListaDoFaktur_DetailView()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"Failed to load ListaDoFaktur_DetailView XAML: {ex}");
                MessageBox.Show($"Błąd ładowania widoku ListaDoFaktur_DetailView:\n{ex.Message}", "Błąd XAML", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // do not set DataContext here - parent view should provide it
            // avoid running runtime-only logic at design time
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                this.Loaded += (s, e) => { try { /* optional initialization */ } catch { } };
            }
        }

        // Handler: zamienia zawartość zakładki "Lista do Faktur" na widok edycji (ListaFaktAddView)
        // i ładuje w nim wybraną listę (SelectedLista z bieżącego DataContext).
        // Przycisk w XAML powinien mieć Click="Edytuj_Liste_Click".
        private void Edytuj_Liste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1) pobierz VM, z którego pobierzemy SelectedLista
                var srcVm = this.DataContext as ListaDoFakturViewModel;
                if (srcVm == null)
                {
                    MessageBox.Show("Brak kontekstu widoku (ListaDoFakturViewModel). Nie można przejść do edycji.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var lista = srcVm.SelectedLista;
                if (lista == null)
                {
                    MessageBox.Show("Nie wybrano żadnej listy do edycji.", "Brak wyboru", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 2) Znajdź zakładkę "Lista do Faktur" w MainWindow (tak jak w innych miejscach projektu)
                var main = Application.Current.MainWindow as MainWindow;
                if (main == null)
                {
                    MessageBox.Show("Nie znaleziono MainWindow.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                TabItem? targetTab = main.FindName("ListaDoFaktur") as TabItem;
                if (targetTab == null)
                {
                    // fallback: przeszukaj TabControly w MainWindow
                    foreach (var child in LogicalTreeHelper.GetChildren(main))
                    {
                        if (child is TabControl tc)
                        {
                            foreach (var item in tc.Items)
                            {
                                if (item is TabItem ti &&
                                    ti.Header?.ToString()?.IndexOf("Lista do Faktur", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    targetTab = ti;
                                    break;
                                }
                            }
                        }
                        if (targetTab != null) break;
                    }
                }

                if (targetTab == null)
                {
                    MessageBox.Show("Nie znaleziono zakładki 'Lista do Faktur' w MainWindow.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 3) Utwórz widok edycji i VM, przypisz SelectedLista do nowego VM
                var editView = new ListaFaktAddView();
                var editVm = new ListaFaktAddViewModel();

                // ustaw SelectedLista bez kopiowania (przekazujemy obiekt DTO - VM edytora oczekuje tego typu)
                editVm.SelectedLista = lista;

                // Spróbuj ustawić dodatkowe pola formularza (numer faktury, data, uwagi, firma) jeśli dostępne w dto
                // używamy bezpiecznego odczytu przez refleksję, aby obsłużyć różne nazwy pól
                object TryGetProp(object src, string[] names)
                {
                    foreach (var n in names)
                    {
                        var p = src.GetType().GetProperty(n, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                        if (p != null)
                        {
                            try
                            {
                                var v = p.GetValue(src);
                                if (v != null) return v;
                            }
                            catch { }
                        }
                    }
                    return null!;
                }

                // Numer faktury (może być FK_Numer, L_Numer, Numer)
                try
                {
                    var num = TryGetProp(lista, new[] { "FK_Numer", "L_Numer", "Numer", "FKNumer" }) as string;
                    if (!string.IsNullOrWhiteSpace(num))
                        editVm.NumerFaktury = num;
                }
                catch { }

                // Data wystawienia / data faktury (FK_Data, L_Data, Data)
                try
                {
                    var d = TryGetProp(lista, new[] { "FK_Data", "L_Data", "Data", "FKData" });
                    if (d is DateTime dt) editVm.DataWystawienia = dt;
                    else if (d != null && DateTime.TryParse(d.ToString(), out var dt2)) editVm.DataWystawienia = dt2;
                }
                catch { }

                // Uwagi
                try
                {
                    var u = TryGetProp(lista, new[] { "L_Uwagi", "Uwagi", "FK_Uwagi", "L_Uwaga" }) as string;
                    if (!string.IsNullOrWhiteSpace(u))
                        editVm.Uwagi = u;
                }
                catch { }

                // Firma: spróbuj odczytać id i nazwę firmy z pola L_Firma_ID, L_Firma, FirmaId, Nazwa
                try
                {
                    int? firmaId = null;
                    string? firmaName = null;

                    // ✅ PRIORYTET: Pobierz L_Firma_ID (najpierw bezpośrednio)
                    var propFirmaId = lista.GetType().GetProperty("L_Firma_ID",
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.IgnoreCase);

                    if (propFirmaId != null)
                    {
                        var val = propFirmaId.GetValue(lista);
                        if (val != null && int.TryParse(val.ToString(), out var parsed))
                        {
                            firmaId = parsed;
                            //System.Diagnostics.Debug.WriteLine($"✅ Edytuj_Liste_Click: Pobrano L_Firma_ID = {firmaId}");
                            //MessageBox.Show($"Pobrano L_Firma_ID = {firmaId}", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    // ✅ FALLBACK: Jeśli L_Firma_ID jest null, spróbuj innych nazw pól
                    if (!firmaId.HasValue)
                    {
                        foreach (var n in new[] { "L_FirmaID", "FirmaId", "Firma_ID" })
                        {
                            var p = lista.GetType().GetProperty(n, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                            if (p == null) continue;
                            try
                            {
                                var v = p.GetValue(lista);
                                if (v == null) continue;
                                if (v is int iv) { firmaId = iv; break; }
                                if (int.TryParse(v.ToString(), out var parsed2)) { firmaId = parsed2; break; }
                            }
                            catch { }
                        }
                    }
                    //MessageBox.Show($"Po próbie pobrania FirmaId = {firmaId}", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);

                    // ✅ Pobierz nazwę firmy (częściej pole "Nazwa" lub "FirmaNazwa")
                    foreach (var n in new[] { "Nazwa", "FirmaNazwa", "L_Nazwa", "Name" })
                    {
                        var p = lista.GetType().GetProperty(n, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                        if (p == null) continue;
                        try
                        {
                            var v = p.GetValue(lista);
                            if (v != null) { firmaName = v.ToString(); break; }
                        }
                        catch { }
                    }

                    // ✅ JEDNO WYWOŁANIE: Ustaw firmę w edytorze
                    if (firmaId.HasValue || !string.IsNullOrWhiteSpace(firmaName))
                    {
                        //System.Diagnostics.Debug.WriteLine($"✅ Edytuj_Liste_Click: Ustawiam firmę: ID={firmaId}, Nazwa={firmaName}");
                        // MessageBox.Show($"Ustawianie firmy w edycji:\nID: {firmaId}\nNazwa: {firmaName}", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                        editVm.SetSelectedFirmaByValues(firmaId, firmaName);
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"⚠️ Edytuj_Liste_Click: Brak danych firmy do przekazania");
                    }
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"❌ Edytuj_Liste_Click: Błąd pobierania firmy: {ex.Message}");
                }

                // 4) przypisz DataContext i zamień zawartość zakładki
                editView.DataContext = editVm;
                targetTab.Content = editView;

                // 5) ustaw aktywną zakładkę (jeśli tab jest wewnątrz TabControl)
                try
                {
                    if (targetTab.Parent is TabControl parentTabControl)
                        parentTabControl.SelectedItem = targetTab;
                }
                catch { }

                // optional: wymuś layout/update
                try { targetTab.InvalidateMeasure(); targetTab.InvalidateArrange(); targetTab.UpdateLayout(); } catch { }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"Edytuj_Liste_Click error: {ex}");
                MessageBox.Show($"Błąd podczas otwierania edycji listy: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- DODANO: Handler usuwania wybranej listy ---
        private void Usun_Liste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Pobierz ViewModel z DataContext
                var vm = this.DataContext as ListaDoFakturViewModel;
                if (vm == null)
                {
                    MessageBox.Show("Brak kontekstu widoku (ListaDoFakturViewModel). Nie można usunąć listy.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Pobierz wybraną listę
                var dto = vm.SelectedLista;
                if (dto == null)
                {
                    MessageBox.Show("Nie wybrano żadnej listy do usunięcia.", "Brak wyboru", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!dto.Identyfikator.HasValue)
                {
                    MessageBox.Show("Wybrany rekord nie ma identyfikatora. Nie można usunąć.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Potwierdzenie usunięcia
                var confirm = MessageBox.Show(
                    $"Czy na pewno usunąć listę:\n{dto.Nazwa}\n\nID listy: {dto.Identyfikator}\nNumer faktury: {dto.FK_Numer ?? "(brak)"}\n\nTa operacja odłączy wszystkie badania i usunie listę z bazy danych.",
                    "Potwierdź usunięcie",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes) return;

                var db = new AccessDbContext();
                int listId = dto.Identyfikator.Value;

                // 1) Pobierz przypisane badania i dla każdego wykonaj unassign
                var badania = db.GetBadaniaForLista(listId);
                int failedUnassign = 0;
                foreach (var b in badania)
                {
                    try
                    {
                        if (b.Bad_ID.HasValue)
                        {
                            var ok = db.UnassignBadanieFromLista(b.Bad_ID.Value, "DeleteList");
                            if (!ok) failedUnassign++;
                        }
                    }
                    catch
                    {
                        failedUnassign++;
                    }
                }

                // 2) Jeżeli lista powiązana z fakturą -> ustaw FK_Num_Listy = 0
                var fakturaId = db.GetFakturaIdForList(listId);
                if (fakturaId.HasValue)
                {
                    try
                    {
                        db.ClearFakturaNumListByFakturaId(fakturaId.Value);
                    }
                    catch { /* ignore */ }
                }

                // 3) Usuń rekord ListyBadan
                var deleted = db.DeleteListyBadan(listId);
                if (!deleted)
                {
                    MessageBox.Show("Nie udało się usunąć rekordu listy z tabeli ListyBadan.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    // Odśwież listę w VM
                    try
                    {
                        vm?.RefreshFromDb();
                        vm?.RefreshAssignedForSelected();
                    }
                    catch (Exception refreshEx)
                    {
                        // System.Diagnostics.Debug.WriteLine($"Błąd odświeżania po usunięciu: {refreshEx.Message}");
                    }

                    var msg = "Usunięto listę pomyślnie.";
                    if (failedUnassign > 0) msg += $"\n\nUwaga: Niektóre powiązania badań nie zostały odłączone ({failedUnassign}).";
                    MessageBox.Show(msg, "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"Usun_Liste_Click error: {ex}");
                MessageBox.Show($"Błąd podczas usuwania listy: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- DODANO: polecenie wysyłki email ---
        private RelayCommand<object>? _sendEmailCommand;
        public ICommand SendEmailCommand => _sendEmailCommand ??= new RelayCommand<object>(_ => SendEmail());

        // --- NOWA METODA: otwarcie Outlook + załączniki jeśli istnieją ---
        private void SendEmail()
        {
            try
            {
                // Najpierw spróbuj odczytać numer faktury bezpośrednio z widoku (TextBlock o nazwie faktura_Nr)
                string invoiceNumberRaw = string.Empty;
                try { invoiceNumberRaw = faktura_Nr?.Text?.Trim() ?? string.Empty; } catch { invoiceNumberRaw = string.Empty; }

                // Jeśli puste - spróbuj od DataContext (ListaDoFakturViewModel.SelectedLista lub inne właściwości)
                var vm = this.DataContext as ListaDoFakturViewModel;
                if (string.IsNullOrWhiteSpace(invoiceNumberRaw))
                {
                    try
                    {
                        if (vm?.SelectedLista != null)
                        {
                            invoiceNumberRaw = vm.SelectedLista.FK_Numer ?? vm.SelectedLista.Numer ?? string.Empty;
                        }
                        else
                        {
                            var prop = vm?.GetType().GetProperty("NumerFaktury", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                            if (prop != null) invoiceNumberRaw = prop.GetValue(vm) as string ?? string.Empty;
                        }
                    }
                    catch { }
                }

                if (string.IsNullOrWhiteSpace(invoiceNumberRaw))
                {
                    NotificationHelper.ShowWarning("Brak numeru faktury. Wprowadź numer przed wysłaniem e-maila.");
                    return;
                }

                // Normalizacja numeru do nazwy pliku: zamień '/' na '_' (zgodnie z wcześniejszą konwencją)
                string invoiceNumber = invoiceNumberRaw.Replace("/", "_").Trim();

                MessageBox.Show($"Przygotowanie e-maila dla faktury nr: {invoiceNumberRaw} - {invoiceNumber}", "Wysyłka e-mail", MessageBoxButton.OK, MessageBoxImage.Information);

                // Nazwa firmy (najpierw TextBlock 'firma', potem VM)
                string companyName = string.Empty;
                try { companyName = firma?.Text?.Trim() ?? string.Empty; } catch { }
                if (string.IsNullOrWhiteSpace(companyName))
                {
                    try { companyName = vm?.SelectedLista?.Nazwa ?? vm?.WybranaFirmaName ?? string.Empty; } catch { }
                }

                // Adres e-mail firmy: próbujemy kolejno: SelectedFirmaDto.fkemail, WybranaFirma.FKemail/Email, pola w SelectedLista
                string mailTo = string.Empty;
                try
                {
                    // 1) SelectedFirmaDto on VM (if exists)
                    var sfProp = vm?.GetType().GetProperty("SelectedFirmaDto", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (sfProp != null)
                    {
                        var sfObj = sfProp.GetValue(vm);
                        if (sfObj != null)
                        {
                            var emailProp = sfObj.GetType().GetProperty("fkemail", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                                            ?? sfObj.GetType().GetProperty("FKemail", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                                            ?? sfObj.GetType().GetProperty("Email", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                            if (emailProp != null)
                            {
                                mailTo = (emailProp.GetValue(sfObj) as string ?? string.Empty).Trim();
                            }
                        }
                    }

                    // 2) WybranaFirma on VM
                    if (string.IsNullOrWhiteSpace(mailTo))
                    {
                        var wFirmaProp = vm?.GetType().GetProperty("WybranaFirma", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                        var wFirmaObj = wFirmaProp?.GetValue(vm);
                        if (wFirmaObj != null)
                        {
                            var emailProp = wFirmaObj.GetType().GetProperty("FKemail", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                                            ?? wFirmaObj.GetType().GetProperty("Email", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                            if (emailProp != null) mailTo = (emailProp.GetValue(wFirmaObj) as string ?? string.Empty).Trim();
                        }
                    }

                    // 3) pola na SelectedLista (różne nazwy możliwe)
                    if (string.IsNullOrWhiteSpace(mailTo) && vm?.SelectedLista != null)
                    {
                        foreach (var n in new[] { "fkemail", "FKemail", "email", "E_mail", "Firma_Email" })
                        {
                            var p = vm.SelectedLista.GetType().GetProperty(n, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                            if (p == null) continue;
                            try
                            {
                                var v = p.GetValue(vm.SelectedLista);
                                if (v != null && !string.IsNullOrWhiteSpace(v.ToString())) { mailTo = v.ToString().Trim(); break; }
                            }
                            catch { }
                        }
                    }
                }
                catch { }

                if (string.IsNullOrWhiteSpace(mailTo))
                {
                    // fallback: stały adres lub pusty
                    mailTo = "adres@domena.pl";
                }

                if (string.IsNullOrWhiteSpace(mailTo))
                {
                    NotificationHelper.ShowWarning("Brak adresu e-mail dla wybranej firmy. Uzupełnij adres w danych firmy przed wysyłką.");
                    return;
                }

                // Ścieżka z której pobieramy pliki (możesz zmienić na konfigurację)
                string exportPath = @"A:\Email\OUT";

                // Plik faktury: "FS {invoiceNumber}.pdf"
                string fakturaFile = Path.Combine(exportPath, $"FS {invoiceNumber}.pdf");

                // Szukanie pliku listy zawierającego numer faktury (różne warianty podziałów/nakładek)
                string? listaFile = null;
                if (Directory.Exists(exportPath))
                {
                    // najpierw proste wyszukiwanie po tokenie invoiceNumber
                    var files = Directory.GetFiles(exportPath, $"lista_do_{invoiceNumber}*.pdf", SearchOption.TopDirectoryOnly);
                    if (files.Length == 0)
                    {
                        // spróbuj wariantu z zamianą '_' na spację
                        var invoiceWithSpaces = invoiceNumber.Replace("_", " ");
                        files = Directory.GetFiles(exportPath, $"lista_do_{invoiceWithSpaces}*.pdf", SearchOption.TopDirectoryOnly);
                    }
                    if (files.Length > 0)
                        listaFile = files[0];
                }

                // Przygotuj temat i treść wiadomości
                string subject = "Medycyna Pracy";
                string body = $"Dzień Dobry\r\nW załączeniu przesyłamy Fakturę nr {invoiceNumberRaw}\r\noraz załączoną listę osób.\r\n\r\nSerdecznie pozdrawiam\r\nNZOZ ASMED\r\nNIP: 113 03 31 776\r\nAl. Stanów Zjednoczonych 51 pok 204\r\n22 871 44 02";

                // Debug/diagnostyka - (możesz usunąć)
                // System.Diagnostics.Debug.WriteLine($"SendEmail: faktura='{fakturaFile}', lista='{listaFile}', to='{mailTo}'");

                // Spróbuj Outlook interop (jeśli Outlook zainstalowany)
                try
                {
                    var outlookType = Type.GetTypeFromProgID("Outlook.Application");
                    if (outlookType != null)
                    {
                        dynamic? app = Activator.CreateInstance(outlookType);
                        dynamic mail = app?.CreateItem(0); // 0 = olMailItem
                        mail.To = mailTo;
                        mail.Subject = subject;
                        mail.Body = body;
                        if (File.Exists(fakturaFile)) mail.Attachments.Add(fakturaFile);
                        if (!string.IsNullOrWhiteSpace(listaFile) && File.Exists(listaFile)) mail.Attachments.Add(listaFile);
                        mail.Display(false); // otwiera okno edycji maila w Outlook
                        return;
                    }
                }
                catch (Exception exOutlook)
                {
                    // System.Diagnostics.Debug.WriteLine($"Outlook interop failed: {exOutlook}");
                    // nie przerywamy — pójdziemy do fallbacku mailto
                }

                // Fallback: otwarcie domyślnego klienta poczty bez załączników (mailto)
                string mailtoUrl = $"mailto:{mailTo}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
                Process.Start(new ProcessStartInfo(mailtoUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"SendEmail error: {ex}");
                NotificationHelper.ShowError($"Błąd podczas otwierania e-maila: {ex.Message}");
            }
        }

    }

}
