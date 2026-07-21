using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ASMED.WPF.Helpers;
using ASMED.WPF.Models;
using ASMED.WPF.Services;

namespace ASMED.WPF.ViewModels
{
    public class FirmaEditViewModel : INotifyPropertyChanged
    {
        private readonly FirmaApiService _apiService = new FirmaApiService();
        private readonly RegonApiService _regonService = new RegonApiService();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event Action ?RequestClose;

        // ✅ NOWE: Cache wyjątków formatowania (załadowanych z bazy)
        private static Dictionary<string, string>? _formatExceptions = null;
        private static object _cacheLock = new object();

        // Właściwości Firmy
        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        private string?_cennik;
        public string?Cennik
        {
            get => _cennik;
            set { _cennik = value; OnPropertyChanged(); }
        }

        private string?_nazwa;
        public string?Nazwa
        {
            get => _nazwa;
            set
            {
                _nazwa = FormatText(value);
                OnPropertyChanged();
            }
        }

        private string?_miejscowosc;
        public string?Miejscowosc
        {
            get => _miejscowosc;
            set
            {
                _miejscowosc = FormatText(value);
                OnPropertyChanged();
            }
        }

        private string?_ulica;
        public string?Ulica
        {
            get => _ulica;
            set
            {
                _ulica = FormatText(value);
                OnPropertyChanged();
            }
        }

        private string?_nip;
        public string?NIP
        {
            get => _nip;
            set { _nip = value; OnPropertyChanged(); }
        }

        private string?_regon;
        public string?REGON
        {
            get => _regon;
            set { _regon = value; OnPropertyChanged(); }
        }

        private string?_kodPocztowy;
        public string?KodPocztowy
        {
            get => _kodPocztowy;
            set { _kodPocztowy = value; OnPropertyChanged(); }
        }

        private string?_osoba_kontaktowa;
        public string?Osoba_kontaktowa
        {
            get => _osoba_kontaktowa;
            set
            {
                _osoba_kontaktowa = FormatText(value);
                OnPropertyChanged();
            }
        }

        private string?_telefon;
        public string?Telefon
        {
            get => _telefon;
            set { _telefon = value; OnPropertyChanged(); }
        }

        private string?_email;
        public string?Email
        {
            get => _email;
            set
            {
                _email = value?.ToLower(); // Email zawsze małe litery
                OnPropertyChanged();
            }
        }

        private string?_fkemail;
        public string?FKemail
        {
            get => _fkemail;
            set
            {
                _fkemail = value?.ToLower(); // Email zawsze małe litery
                OnPropertyChanged();
            }
        }

        // Pomocnicze właściwości
        private string?_nipDoWyszukania;
        public string?NipDoWyszukania
        {
            get => _nipDoWyszukania;
            set { _nipDoWyszukania = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public bool IsEditMode => Id > 0;
        public string WindowTitle => IsEditMode ? $"Edycja firmy (ID: {Id})" : "Dodaj nową firmę";

        // Lista cenników z bazy danych
        public ObservableCollection<string> CennikiOptions { get; } = new ObservableCollection<string>();

        // ✅ NOWE: Przechowuje zapisaną firmę (dla przekazania do SkierPacjentaEditViewModel)
        public Firma? SavedFirma { get; private set; }

        // Komendy
        public ICommand PobierzDanePoNIPCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public FirmaEditViewModel()
        {
            PobierzDanePoNIPCommand = new RelayCommand(async _ => await PobierzDanePoNIP());
            SaveCommand = new RelayCommand(_ => SaveFirma());
            CancelCommand = new RelayCommand(_ => Cancel());

            // Załaduj cenniki z bazy
            LoadCennikiFromDb();
        }

        // Konstruktor dla edycji istniejącej firmy
        public FirmaEditViewModel(Firma firma) : this()
        {
            if (firma != null)
            {
                Id = firma.id;
                Cennik = firma.Cennik;

                // Zastosuj formatowanie do istniejących danych
                _nazwa = FormatText(firma.Nazwa);
                _miejscowosc = FormatText(firma.Miejscowosc);
                _ulica = FormatText(firma.Ulica);
                _osoba_kontaktowa = FormatText(firma.Osoba_kontaktowa);

                NIP = firma.NIP;
                Telefon = firma.Telefon;

                _email = firma.Email?.ToLower();
                _fkemail = firma.FKemail?.ToLower();

                // Wywołaj OnPropertyChanged dla wszystkich
                OnPropertyChanged(nameof(Nazwa));
                OnPropertyChanged(nameof(Miejscowosc));
                OnPropertyChanged(nameof(Ulica));
                OnPropertyChanged(nameof(Osoba_kontaktowa));
                OnPropertyChanged(nameof(Email));
                OnPropertyChanged(nameof(FKemail));
            }
        }

        /// <summary>
        /// Formatuje tekst: pierwsza litera wyrazu wielka, reszta małe, usuwa " i .
        /// Cyfry pozostają bez zmian
        /// Liczby rzymskie pozostają w formacie UPPERCASE (XIV, XVI, itp.)
        /// Wyjątki: przyimki, spójniki, skróty z kodu + nazwy własne z bazy (PKP, TVP, ZUS, P.H.U., S.A.)
        /// Przykład: "LITEWSKA 12" -> "Litewska 12", "PKP PLK S.A." -> "PKP PLK S.A."
        /// </summary>
        private string FormatText(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            try
            {
                // ✅ Zachowaj oryginalny tekst do wykrywania skrótów
                var originalInput = input;

                // Usuń cudzysłowy
                var text = input.Replace("\"", "");

                // Konwertuj na małe litery
                text = text.ToLower(CultureInfo.GetCultureInfo("pl-PL"));

                // Podziel na wyrazy
                var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                // ✅ NOWE: Połącz wyjątki z kodu i bazy w jeden słownik
                var allExceptions = GetCombinedFormatExceptions();

                // Sformatuj każdy wyraz
                var formattedWords = words.Select((word, index) =>
                {
                    if (word.Length == 0)
                        return word;

                    // ✅ Pobierz oryginalny wyraz (przed toLowerCase)
                    var originalWord = GetOriginalWord(originalInput, word);

                    // ✅ NOWE: Obsługa słów z kropkami (np. "P.H.U.AMELKA" lub "S.A.")
                    if (word.Contains("."))
                    {
                        return FormatWordWithDots(word, originalWord, allExceptions);
                    }

                    // ✅ PRIORYTET 1: Sprawdź w połączonym słowniku (baza + kod)
                    if (allExceptions.TryGetValue(word, out var formatType))
                    {
                        // Specjalna obsługa lowercase - nie stosuj dla pierwszego słowa
                        if (formatType == "lowercase" && index == 0)
                        {
                            // Pierwsze słowo zawsze z wielkiej litery (ignoruj lowercase)
                            var capitalized = char.ToUpper(word[0], CultureInfo.GetCultureInfo("pl-PL")) + 
                                            (word.Length > 1 ? word.Substring(1) : "");
                            // System.Diagnostics.Debug.WriteLine($"[FormatText] Pierwsze słowo '{word}' → '{capitalized}' (ignoruję lowercase)");
                            return capitalized;
                        }

                        var formatted = ApplyFormat(word, formatType);
                        // System.Diagnostics.Debug.WriteLine($"[FormatText] Wyjątek '{word}' ({formatType}) → '{formatted}'");
                        return formatted;
                    }

                    // ✅ PRIORYTET 2: Heurystyka - wykryj skróty (2-5 wielkich liter w oryginalnym tekście)
                    if (originalWord.Length >= 2 && 
                        originalWord.Length <= 5 && 
                        originalWord.All(char.IsUpper))
                    {
                        // System.Diagnostics.Debug.WriteLine($"[FormatText] Heurystyka (skrót UPPERCASE): '{word}' → '{originalWord}'");
                        return originalWord;
                    }

                    // ✅ PRIORYTET 3: Liczby rzymskie
                    if (IsRomanNumeral(word))
                    {
                        var romanUpper = word.ToUpper(CultureInfo.GetCultureInfo("pl-PL"));
                        // System.Diagnostics.Debug.WriteLine($"[FormatText] Liczba rzymska: '{word}' → '{romanUpper}'");
                        return romanUpper;
                    }

                    // ✅ PRIORYTET 4: Standardowe formatowanie (pierwsza wielka, reszta małe)
                    var firstChar = char.ToUpper(word[0], CultureInfo.GetCultureInfo("pl-PL"));
                    var rest = word.Length > 1 ? word.Substring(1) : "";

                    return firstChar + rest;
                });

                // Połącz wyrazy spacjami
                var result = string.Join(" ", formattedWords);

                // System.Diagnostics.Debug.WriteLine($"[FormatText] '{input}' → '{result}'");

                return result;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"[FormatText] Błąd: {ex.Message}");
                return input; // W przypadku błędu zwróć oryginalny tekst
            }
        }

        /// <parameter name="formatExceptions)
        /// Dzieli po kropkach i formatuje każdą część osobno, zachowując kropki.
        /// </summary>
        private string FormatWordWithDots(string word, string originalWord, Dictionary<string, string> formatExceptions)
        {
            try
            {
                // ✅ PRIORYTET 1: Sprawdź czy CAŁE SŁOWO (z kropkami) jest w bazie
                if (formatExceptions.TryGetValue(word, out var wholeWordFormat))
                {
                    var formatted = ApplyFormat(word, wholeWordFormat);
                    // System.Diagnostics.Debug.WriteLine($"[FormatWordWithDots] Całe słowo '{word}' z bazy → '{formatted}'");
                    return formatted;
                }

                // ✅ PRIORYTET 2: Podziel po kropkach i formatuj każdą część
                var parts = word.Split('.');
                var formattedParts = new List<string>();

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];

                    if (string.IsNullOrEmpty(part))
                    {
                        // Pusta część (np. po kropce na końcu) - dodaj tylko kropkę
                        if (i < parts.Length - 1)
                            formattedParts.Add("");
                        continue;
                    }

                    // Sprawdź czy ta część jest w bazie (z kropką i bez)
                    var partWithDot = part + ".";
                    string formatted;

                    if (formatExceptions.TryGetValue(partWithDot, out var formatType))
                    {
                        // Znaleziono w bazie z kropką (np. "p.h.u." → "P.H.U.")
                        formatted = ApplyFormat(partWithDot, formatType);
                        // System.Diagnostics.Debug.WriteLine($"[FormatWordWithDots] Część '{partWithDot}' z bazy → '{formatted}'");
                    }
                    else if (part.Length == 1 && char.IsLetter(part[0]))
                    {
                        // ✅ Pojedyncza litera (np. "P" w "P.H.U.") - zawsze wielka
                        // (IGNORUJ wyjątki dla pojedynczych liter - np. "u" jako przyimek)
                        formatted = part.ToUpper(CultureInfo.GetCultureInfo("pl-PL"));
                        // System.Diagnostics.Debug.WriteLine($"[FormatWordWithDots] Pojedyncza litera '{part}' → '{formatted}'");
                    }
                    else if (formatExceptions.TryGetValue(part, out formatType))
                    {
                        // Znaleziono w bazie bez kropki (np. "amelka" → "AMELKA")
                        formatted = ApplyFormat(part, formatType);
                        // System.Diagnostics.Debug.WriteLine($"[FormatWordWithDots] Część '{part}' z bazy → '{formatted}'");
                    }
                    else
                    {
                        // Standardowe formatowanie (pierwsza wielka, reszta małe)
                        formatted = char.ToUpper(part[0], CultureInfo.GetCultureInfo("pl-PL")) + 
                                   (part.Length > 1 ? part.Substring(1) : "");
                    }

                    formattedParts.Add(formatted);
                }

                // Połącz z kropkami
                var result = string.Join(".", formattedParts);
                // System.Diagnostics.Debug.WriteLine($"[FormatWordWithDots] '{word}' → '{result}'");
                return result;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"[FormatWordWithDots] Błąd: {ex.Message}");
                return word;
            }
        }

        /// <summary>
        /// Aplikuje format z bazy danych (UPPERCASE, lowercase, Capitalize)
        /// </summary>
        private string ApplyFormat(string text, string formatType)
        {
            switch (formatType)
            {
                case "UPPERCASE":
                    return text.ToUpper(CultureInfo.GetCultureInfo("pl-PL"));
                case "lowercase":
                    return text; // Już lowercase z ToLower()
                case "Capitalize":
                    return char.ToUpper(text[0], CultureInfo.GetCultureInfo("pl-PL")) + 
                           (text.Length > 1 ? text.Substring(1) : "");
                default:
                    return text;
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Łączy wyjątki z kodu i bazy w jeden słownik (z cache)
        /// Priorytet: baza danych > wyjątki hardcoded
        /// </summary>
        private Dictionary<string, string> GetCombinedFormatExceptions()
        {
            // Jeśli cache już istnieje, zwróć go
            if (_formatExceptions != null)
                return _formatExceptions;

            lock (_cacheLock)
            {
                // Double-check po wejściu w lock
                if (_formatExceptions != null)
                    return _formatExceptions;

                _formatExceptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                // ═══════════════════════════════════════════════════════════
                // KROK 1: Dodaj wyjątki hardcoded (domyślne/fallback)
                // ═══════════════════════════════════════════════════════════
                var defaultExceptions = new Dictionary<string, string>
                {
                    // Przyimki i spójniki (lowercase - nie na początku zdania)
                    { "a", "lowercase" },
                    { "w", "lowercase" },
                    { "i", "lowercase" },
                    { "z", "lowercase" },
                    { "o", "lowercase" },
                    { "u", "lowercase" },
                    { "na", "lowercase" },
                    { "do", "lowercase" },
                    { "po", "lowercase" },
                    { "od", "lowercase" },
                    { "za", "lowercase" },
                    { "we", "lowercase" },
                    { "też", "lowercase" },
                    { "ale", "lowercase" },
                    { "lub", "lowercase" },
                    { "ani", "lowercase" },
                    { "przy", "lowercase" },

                    // Popularne skróty (lowercase)
                    { "nr", "lowercase" },
                    { "im", "lowercase" },
                    { "dla", "lowercase" },
                    { "itp", "lowercase" },
                    { "itd", "lowercase" },
                    { "ul", "lowercase" },
                    { "al", "lowercase" },
                    { "pl", "lowercase" },
                    { "os", "lowercase" },

                    // Dodatkowe skróty (lowercase)
                    { "m", "lowercase" },
                    { "km", "lowercase" },
                    { "cm", "lowercase" },
                    { "mm", "lowercase" },
                    { "kg", "lowercase" },
                    { "g", "lowercase" },
                    { "l", "lowercase" },
                    { "ml", "lowercase" },
                    { "szt", "lowercase" },
                    { "tab", "lowercase" },
                    { "pkt", "lowercase" },
                    { "min", "lowercase" },
                    { "max", "lowercase" },
                    { "avg", "lowercase" },
                    { "temp", "lowercase" },
                    { "godz", "lowercase" },
                    { "dni", "lowercase" },
                    { "tyg", "lowercase" },
                    { "mies", "lowercase" },
                    { "rok", "lowercase" },
                };

                // Dodaj do słownika
                foreach (var kvp in defaultExceptions)
                {
                    _formatExceptions[kvp.Key] = kvp.Value;
                }

                // ═══════════════════════════════════════════════════════════
                // KROK 2: Załaduj wyjątki z bazy (nadpisują domyślne)
                // ═══════════════════════════════════════════════════════════
                var dbLoadResult = new StringBuilder();
                dbLoadResult.AppendLine("=== ŁADOWANIE Z BAZY ===\n");

                try
                {
                    var db = new AccessDbHelper();
                    using (var conn = db.GetConnection())
                    {
                        conn.Open();
                        dbLoadResult.AppendLine($"✅ Połączono z bazą danych");

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = "SELECT Slowo, FormatTyp FROM FormatowanieTekstu";
                            dbLoadResult.AppendLine($"SQL: {cmd.CommandText}\n");

                            using (var reader = cmd.ExecuteReader())
                            {
                                int dbCount = 0;
                                while (reader.Read())
                                {
                                    // ✅ TRIM! Baza Access może mieć spacje na końcu (CHAR vs VARCHAR)
                                    var slowo = reader["Slowo"]?.ToString()?.Trim();
                                    var formatTyp = reader["FormatTyp"]?.ToString()?.Trim();

                                    if (!string.IsNullOrWhiteSpace(slowo) && !string.IsNullOrWhiteSpace(formatTyp))
                                    {
                                        // Nadpisz wyjątek z bazy (priorytet wyższy niż domyślne)
                                        _formatExceptions[slowo] = formatTyp;
                                        dbCount++;

                                        // Pokaż pierwsze 10 wpisów
                                        if (dbCount <= 10)
                                        {
                                            dbLoadResult.AppendLine($"  {dbCount}. '{slowo}' → {formatTyp}");
                                        }
                                    }
                                }

                                dbLoadResult.AppendLine($"\n✅ Załadowano {dbCount} wpisów z bazy");
                                // System.Diagnostics.Debug.WriteLine($"[FormatText] Załadowano {dbCount} wyjątków z bazy + {defaultExceptions.Count} domyślnych = {_formatExceptions.Count} razem");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    dbLoadResult.AppendLine($"\n❌ BŁĄD: {ex.Message}");
                    dbLoadResult.AppendLine($"Stack trace: {ex.StackTrace}");
                    // System.Diagnostics.Debug.WriteLine($"[FormatText] Błąd ładowania wyjątków z bazy: {ex.Message}");
                    // Jeśli błąd - używamy tylko domyślnych wyjątków
                }

                // ✅ Logowanie do Debug (zamiast MessageBox)
                // System.Diagnostics.Debug.WriteLine(dbLoadResult.ToString());
                // System.Diagnostics.Debug.WriteLine($"[FormatText] UPPERCASE: {_formatExceptions.Count(x => x.Value == "UPPERCASE")}");
                // System.Diagnostics.Debug.WriteLine($"[FormatText] lowercase: {_formatExceptions.Count(x => x.Value == "lowercase")}");
                // System.Diagnostics.Debug.WriteLine($"[FormatText] Capitalize: {_formatExceptions.Count(x => x.Value == "Capitalize")}");

                return _formatExceptions;
            }
        }

        /// <summary>
        /// ✅ NOWA METODA: Znajduje oryginalny wyraz w tekście wejściowym (przed toLowerCase)
        /// Używane do wykrywania skrótów pisanych WIELKIMI LITERAMI
        /// </summary>
        private string GetOriginalWord(string input, string lowercaseWord)
        {
            try
            {
                // Podziel oryginalny tekst na wyrazy
                var originalWords = input.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                // Znajdź wyraz który pasuje (case-insensitive)
                var match = originalWords.FirstOrDefault(w => w.Equals(lowercaseWord, StringComparison.OrdinalIgnoreCase));

                return match ?? lowercaseWord;
            }
            catch
            {
                return lowercaseWord;
            }
        }

        /// <summary>
        /// Sprawdza czy podany tekst jest liczbą rzymską
        /// Obsługuje liczby od I do MMMCMXCIX (1-3999)
        /// </summary>
        private bool IsRomanNumeral(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // Konwertuj na wielkie litery dla porównania
            var upper = text.ToUpper();

            // Sprawdź czy składa się tylko z dozwolonych znaków rzymskich
            if (!System.Text.RegularExpressions.Regex.IsMatch(upper, "^[IVXLCDM]+$"))
                return false;

            // Lista popularnych liczb rzymskich (1-100 + większe wartości)
            var commonRomanNumerals = new[]
            {
                "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X",
                "XI", "XII", "XIII", "XIV", "XV", "XVI", "XVII", "XVIII", "XIX", "XX",
                "XXI", "XXII", "XXIII", "XXIV", "XXV", "XXVI", "XXVII", "XXVIII", "XXIX", "XXX",
                "XXXI", "XXXII", "XXXIII", "XXXIV", "XXXV", "XXXVI", "XXXVII", "XXXVIII", "XXXIX", "XL",
                "XLI", "XLII", "XLIII", "XLIV", "XLV", "XLVI", "XLVII", "XLVIII", "XLIX", "L",
                "LI", "LII", "LIII", "LIV", "LV", "LVI", "LVII", "LVIII", "LIX", "LX",
                "LXI", "LXII", "LXIII", "LXIV", "LXV", "LXVI", "LXVII", "LXVIII", "LXIX", "LXX",
                "LXXI", "LXXII", "LXXIII", "LXXIV", "LXXV", "LXXVI", "LXXVII", "LXXVIII", "LXXIX", "LXXX",
                "LXXXI", "LXXXII", "LXXXIII", "LXXXIV", "LXXXV", "LXXXVI", "LXXXVII", "LXXXVIII", "LXXXIX", "XC",
                "XCI", "XCII", "XCIII", "XCIV", "XCV", "XCVI", "XCVII", "XCVIII", "XCIX", "C",
                "CC", "CCC", "CD", "D", "DC", "DCC", "DCCC", "CM", "M", "MM", "MMM"
            };

            // Sprawdź czy jest na liście popularnych
            if (commonRomanNumerals.Contains(upper))
                return true;

            // Dodatkowa walidacja: spróbuj przekonwertować na liczbę arabską
            // Jeśli konwersja się powiedzie, to jest poprawną liczbą rzymską
            try
            {
                var arabicValue = RomanToArabic(upper);
                // Sprawdź czy konwersja zwrotna daje ten sam wynik (eliminuje niepoprawne kombinacje)
                return arabicValue > 0 && arabicValue <= 3999;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Konwertuje liczbę rzymską na arabską (do walidacji)
        /// </summary>
        private int RomanToArabic(string roman)
        {
            if (string.IsNullOrWhiteSpace(roman))
                return 0;

            var romanValues = new Dictionary<char, int>
            {
                {'I', 1},
                {'V', 5},
                {'X', 10},
                {'L', 50},
                {'C', 100},
                {'D', 500},
                {'M', 1000}
            };

            int result = 0;
            int prevValue = 0;

            // Iteruj od końca do początku
            for (int i = roman.Length - 1; i >= 0; i--)
            {
                if (!romanValues.TryGetValue(roman[i], out int currentValue))
                    throw new ArgumentException("Niepoprawny znak w liczbie rzymskiej");

                // Jeśli obecna wartość jest mniejsza niż poprzednia, odejmij (np. IV = 5-1 = 4)
                if (currentValue < prevValue)
                    result -= currentValue;
                else
                    result += currentValue;

                prevValue = currentValue;
            }

            return result;
        }

        private void LoadCennikiFromDb()
        {
            try
            {
                var db = new AccessDbHelper();
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT DISTINCT bn_cennik FROM BAD_Lista WHERE bn_cennik IS NOT NULL ORDER BY bn_cennik";

                        using (var reader = cmd.ExecuteReader())
                        {
                            CennikiOptions.Clear();

                            while (reader.Read())
                            {
                                var cennik = reader["bn_cennik"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(cennik) && !CennikiOptions.Contains(cennik))
                                {
                                    CennikiOptions.Add(cennik);
                                }
                            }
                        }
                    }
                }

                // System.Diagnostics.Debug.WriteLine($"[FirmaEdit] Załadowano {CennikiOptions.Count} cenników z bazy danych");

                // Jeśli lista jest pusta, dodaj domyślne wartości
                if (CennikiOptions.Count == 0)
                {
                    CennikiOptions.Add("Podstawowy");
                    CennikiOptions.Add("Firmowy");
                    CennikiOptions.Add("Szkoły");
                    // System.Diagnostics.Debug.WriteLine("[FirmaEdit] Brak cenników w bazie, użyto wartości domyślnych");
                }

                // Ustaw domyślny cennik jeśli nie został ustawiony
                if (string.IsNullOrWhiteSpace(Cennik) && CennikiOptions.Count > 0)
                {
                    Cennik = CennikiOptions[0];
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[FirmaEdit] Błąd ładowania cenników: {ex.Message}");

                // W przypadku błędu, dodaj domyślne wartości
                CennikiOptions.Add("Podstawowy");
                CennikiOptions.Add("Firmowy");
                CennikiOptions.Add("Szkoły");

                MessageBox.Show($"Nie można załadować cenników z bazy danych. Użyto wartości domyślnych.\n\nBłąd: {ex.Message}",
                    "Ostrzeżenie", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task PobierzDanePoNIP()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NipDoWyszukania))
                {
                    MessageBox.Show("Wpisz numer NIP do wyszukania",
                        "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsLoading = true;

                // ══════════════════════════════════════════════════
                // KASKADA: 1) GUS REGON  →  2) Biała Lista VAT
                // ══════════════════════════════════════════════════

                FirmaDaneDto? dane = null;
                string zrodlo = "";

                // 1) GUS REGON API — zawiera WSZYSTKIE podmioty + nazwa skrócona
                try
                {
                    // System.Diagnostics.Debug.WriteLine($"[FirmaEdit] ═══ KASKADA START ═══");
                    // System.Diagnostics.Debug.WriteLine($"[FirmaEdit] Próba GUS REGON dla NIP={NipDoWyszukania}");
                    dane = await _regonService.PobierzDaneFirmyPoNIP(NipDoWyszukania);
                    if (dane != null)
                    {
                        zrodlo = "GUS REGON (produkcja)";
                        // System.Diagnostics.Debug.WriteLine($"[FirmaEdit] ✅ GUS REGON zwrócił dane: {dane.Nazwa}");
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"[FirmaEdit] GUS REGON: null (nie znaleziono lub błąd)");
                    }
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"[FirmaEdit] ❌ GUS REGON exception: {ex.Message}");
                }

                // 2) Biała Lista VAT — fallback (tylko podatnicy VAT)
                if (dane == null)
                {
                    try
                    {
                        // System.Diagnostics.Debug.WriteLine($"[FirmaEdit] Fallback: Biała Lista VAT dla NIP={NipDoWyszukania}");
                        dane = await _apiService.PobierzDaneFirmyPoNIP(NipDoWyszukania);
                        if (dane != null)
                        {
                            zrodlo = "Biała Lista VAT";
                            // System.Diagnostics.Debug.WriteLine($"[FirmaEdit] ✅ Biała Lista VAT zwróciła dane: {dane.Nazwa}");
                        }
                        else
                        {
                            // System.Diagnostics.Debug.WriteLine($"[FirmaEdit] Biała Lista VAT: null");
                        }
                    }
                    catch (Exception)
                    {
                        // System.Diagnostics.Debug.WriteLine($"[FirmaEdit] ❌ Biała Lista VAT exception: {ex.Message}");
                    }
                }

                if (dane == null)
                {
                    MessageBox.Show($"Nie znaleziono firmy o NIP: {NipDoWyszukania}\n\n" +
                        "Sprawdzono: GUS REGON oraz Biała Lista VAT.\n" +
                        "Wpisz dane ręcznie.",
                        "Nie znaleziono", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!dane.CzyAktywna)
                {
                    var result = MessageBox.Show(
                        $"Firma jest NIEAKTYWNA (status: {dane.Status})\n\n" +
                        "Czy chcesz wypełnić formularz danymi tej firmy?",
                        "Firma nieaktywna", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                        return;
                }

                // Wypełnienie formularza z formatowaniem
                Nazwa = dane.Nazwa;
                NIP = dane.NIP;
                REGON = dane.REGON;
                Miejscowosc = dane.Miejscowosc;
                KodPocztowy = dane.KodPocztowy;

                var ulicaZNumerem = !string.IsNullOrWhiteSpace(dane.NumerDomu)
                    ? $"{dane.Ulica} {dane.NumerDomu}{(!string.IsNullOrWhiteSpace(dane.NumerLokalu) ? "/" + dane.NumerLokalu : "")}"
                    : dane.Ulica;

                Ulica = ulicaZNumerem;

                MessageBox.Show(
                    $"Pobrano dane firmy (źródło: {zrodlo}):\n\n" +
                    $"{Nazwa}\n{Ulica}, {KodPocztowy} {Miejscowosc}\nREGON: {REGON}",
                    "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd pobierania danych:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SaveFirma()
        {
            try
            {
                // ══════════════════════════════════════════════════
                // KROK 1: Walidacja wymaganych pól
                // ══════════════════════════════════════════════════
                if (string.IsNullOrWhiteSpace(Nazwa))
                {
                    MessageBox.Show("Nazwa firmy jest wymagana.",
                        "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ══════════════════════════════════════════════════
                // KROK 2: Sprawdzenie duplikatu
                // ══════════════════════════════════════════════════
                var duplikat = SzukajDuplikatuFirmy(Nazwa.Trim(), NIP, IsEditMode ? Id : (int?)null);

                if (duplikat.HasValue)
                {
                    var result = MessageBox.Show(
                        $"Znaleziono istniejącą firmę (ID: {duplikat.Value.id}):\n" +
                        $"{duplikat.Value.nazwa}" +
                        (string.IsNullOrWhiteSpace(duplikat.Value.nip) ? "" : $"\nNIP: {duplikat.Value.nip}") +
                        "\n\nCzy nadpisać dane istniejącej firmy?",
                        "Duplikat firmy",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Nadpisz istniejący rekord zamiast tworzyć nowy
                        UpdateFirmaById(duplikat.Value.id);
                        Id = duplikat.Value.id;
                        RequestClose?.Invoke();
                    }
                    return;
                }

                // ══════════════════════════════════════════════════
                // KROK 3: Zapis (nowy lub edycja)
                // ══════════════════════════════════════════════════
                var db = new AccessDbHelper();
                using (var conn = db.GetConnection())
                {
                    conn.Open();

                    if (IsEditMode)
                    {
                        var cmd = conn.CreateCommand();
                        cmd.CommandText = @"UPDATE Firma SET
                            Cennik = ?,
                            Nazwa = ?,
                            Miejscowosc = ?,
                            Ulica = ?,
                            NIP = ?,
                            Osoba_kontaktowa = ?,
                            Telefon = ?,
                            Email = ?,
                            FKemail = ?
                        WHERE id = ?";

                        cmd.Parameters.AddWithValue("@Cennik", Cennik ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Nazwa", Nazwa ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Miejscowosc", Miejscowosc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Ulica", Ulica ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@NIP", NIP ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Osoba_kontaktowa", Osoba_kontaktowa ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Telefon", Telefon ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", Email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FKemail", FKemail ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@id", Id);

                        cmd.ExecuteNonQuery();

                        MessageBox.Show("Firma została zaktualizowana pomyślnie",
                            "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        var cmd = conn.CreateCommand();
                        cmd.CommandText = @"INSERT INTO Firma 
                            (Cennik, Nazwa, Miejscowosc, Ulica, NIP, Osoba_kontaktowa, Telefon, Email, FKemail, Activ, Kod)
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, True, ?)";

                        cmd.Parameters.AddWithValue("@Cennik", Cennik ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Nazwa", Nazwa ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Miejscowosc", Miejscowosc ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Ulica", Ulica ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@NIP", NIP ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Osoba_kontaktowa", Osoba_kontaktowa ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Telefon", Telefon ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", Email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@FKemail", FKemail ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Kod", KodPocztowy ?? (object)DBNull.Value);

                        cmd.ExecuteNonQuery();

                        MessageBox.Show("Firma została dodana pomyślnie",
                            "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Szuka duplikatu firmy w bazie.
        /// - Jeśli nowa firma MA NIP → duplikatem jest: ten sam NIP LUB (ta sama nazwa + brak NIP w bazie)
        /// - Jeśli nowa firma NIE MA NIP → duplikatem jest: ta sama nazwa
        /// Przy edycji (excludeId != null) pomija własny rekord.
        /// </summary>
        private (int id, string nazwa, string nip)? SzukajDuplikatuFirmy(string nazwa, string nip, int? excludeId)
        {
            try
            {
                var db = new AccessDbHelper();
                using var conn = db.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();

                if (!string.IsNullOrWhiteSpace(nip))
                {
                    // Nowa firma MA NIP → szukaj po NIP lub (nazwa + brak NIP)
                    cmd.CommandText = @"
                        SELECT id, Nazwa, NIP FROM Firma
                        WHERE activ = True
                          AND (
                            TRIM(NIP) = TRIM(?)
                            OR (TRIM(UCASE(Nazwa)) = TRIM(UCASE(?))
                                AND (NIP IS NULL OR TRIM(NIP) = ''))
                          )";
                    var p1 = cmd.CreateParameter(); p1.Value = nip.Trim(); cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter(); p2.Value = nazwa; cmd.Parameters.Add(p2);
                }
                else
                {
                    // Nowa firma NIE MA NIP → szukaj po nazwie
                    cmd.CommandText = @"
                        SELECT id, Nazwa, NIP FROM Firma
                        WHERE activ = True
                          AND TRIM(UCASE(Nazwa)) = TRIM(UCASE(?))";
                    var p1 = cmd.CreateParameter(); p1.Value = nazwa; cmd.Parameters.Add(p1);
                }

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var foundId = Convert.ToInt32(reader["id"]);
                    if (excludeId.HasValue && foundId == excludeId.Value)
                        continue;
                    return (
                        foundId,
                        reader["Nazwa"]?.ToString() ?? "",
                        reader["NIP"]?.ToString() ?? ""
                    );
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"[SzukajDuplikatuFirmy] Błąd: {ex}");
            }
            return null;
        }

        /// <summary>
        /// Nadpisuje dane istniejącej firmy (duplikat → użytkownik wybrał "Tak").
        /// </summary>
        private void UpdateFirmaById(int firmaId)
        {
            var db = new AccessDbHelper();
            using var conn = db.GetConnection();
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE Firma SET
                Cennik = ?, Nazwa = ?, Miejscowosc = ?, Ulica = ?,
                NIP = ?, Osoba_kontaktowa = ?, Telefon = ?,
                Email = ?, FKemail = ?, Kod = ?
            WHERE id = ?";

            cmd.Parameters.AddWithValue("@Cennik", Cennik ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Nazwa", Nazwa ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Miejscowosc", Miejscowosc ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Ulica", Ulica ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@NIP", NIP ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Osoba_kontaktowa", Osoba_kontaktowa ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Telefon", Telefon ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Email", Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@FKemail", FKemail ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Kod", KodPocztowy ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@id", firmaId);

            cmd.ExecuteNonQuery();

            MessageBox.Show($"Dane firmy (ID: {firmaId}) zostały nadpisane.",
                "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Cancel()
        {
            RequestClose?.Invoke();
        }
    }
}
