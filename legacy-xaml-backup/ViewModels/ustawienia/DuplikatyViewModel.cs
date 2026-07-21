using ASMED.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ASMED.WPF.ViewModels
{
    /// <summary>
    /// ViewModel obsługujący wyszukiwanie i scalanie duplikatów
    /// w tabelach: P_Pacjent, Firma, B_Skierowania.
    /// </summary>
    public class DuplikatyViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ── Typ tabeli ──
        public ObservableCollection<string> TabelaOptions { get; } = new()
        {
            "Pacjenci",
            "Firmy",
            "Skierowania",
            "Badania"
        };

        private string _selectedTabela = "Pacjenci";
        public string SelectedTabela
        {
            get => _selectedTabela;
            set
            {
                if (_selectedTabela == value) return;
                _selectedTabela = value;
                OnPropertyChanged();
                GrupyDuplikatow.Clear();
                WybranaGrupa = null;
            }
        }

        // ── Wyniki wyszukiwania ──
        public ObservableCollection<DuplikatGrupa> GrupyDuplikatow { get; } = new();

        private DuplikatGrupa? _wybranaGrupa;
        public DuplikatGrupa? WybranaGrupa
        {
            get => _wybranaGrupa;
            set
            {
                if (_wybranaGrupa == value) return;
                _wybranaGrupa = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CzyWybranoGrupe));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool CzyWybranoGrupe => WybranaGrupa != null;

        private string _statusText = "Wybierz tabelę i kliknij 'Szukaj duplikatów'";
        public string StatusText
        {
            get => _statusText;
            set { if (_statusText != value) { _statusText = value; OnPropertyChanged(); } }
        }

        // ── Komendy ──
        public ICommand SzukajDuplikatowCommand { get; }
        public ICommand ScalDuplikatyCommand { get; }

        public DuplikatyViewModel()
        {
            SzukajDuplikatowCommand = new RelayCommand(_ => SzukajDuplikatow());
            ScalDuplikatyCommand = new RelayCommand(_ => OtworzDialogScalania());
        }

        // ══════════════════════════════════════════════════════════
        //  Wyszukiwanie duplikatów
        // ══════════════════════════════════════════════════════════
        private void SzukajDuplikatow()
        {
            try
            {
                GrupyDuplikatow.Clear();
                WybranaGrupa = null;

                switch (SelectedTabela)
                {
                    case "Pacjenci": SzukajDuplikatowPacjentow(); break;
                    case "Firmy": SzukajDuplikatowFirm(); break;
                    case "Skierowania": SzukajDuplikatowSkierowan(); break;
                    case "Badania": SzukajDuplikatowBadan(); break;
                }

                StatusText = GrupyDuplikatow.Count > 0
                    ? $"Znaleziono {GrupyDuplikatow.Count} grup duplikatów w tabeli '{SelectedTabela}'"
                    : $"Brak duplikatów w tabeli '{SelectedTabela}' ✓";
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[DuplikatyVM] Błąd: {ex}");
                MessageBox.Show($"Błąd wyszukiwania duplikatów:\n{ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Pacjenci: duplikaty po TRIM(P_imie)+TRIM(P_nazwisko)+P_Firma_id ──
        // Ta sama osoba w różnych firmach NIE jest duplikatem.
        private void SzukajDuplikatowPacjentow()
        {
            var db = new AccessDbHelper();
            using var conn = db.GetConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT TRIM(P_imie) AS pi, TRIM(P_nazwisko) AS pn, P_Firma_id, COUNT(*) AS cnt
                FROM P_Pacjent
                GROUP BY TRIM(P_imie), TRIM(P_nazwisko), P_Firma_id
                HAVING COUNT(*) > 1
                ORDER BY COUNT(*) DESC";

            var grupy = new List<(string imie, string nazwisko, int? firmaId, int cnt)>();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    grupy.Add((
                        reader["pi"]?.ToString() ?? "",
                        reader["pn"]?.ToString() ?? "",
                        reader["P_Firma_id"] != DBNull.Value ? Convert.ToInt32(reader["P_Firma_id"]) : null,
                        Convert.ToInt32(reader["cnt"])
                    ));
                }
            }

            foreach (var (imie, nazwisko, firmaId, cnt) in grupy)
            {
                using var cmdDet = conn.CreateCommand();
                if (firmaId.HasValue)
                {
                    cmdDet.CommandText = @"
                        SELECT P_ID, P_imie, P_nazwisko, P_firma, P_Firma_id, P_Activ
                        FROM P_Pacjent
                        WHERE TRIM(P_imie) = ? AND TRIM(P_nazwisko) = ? AND P_Firma_id = ?
                        ORDER BY P_ID";
                    var p1 = cmdDet.CreateParameter(); p1.Value = imie; cmdDet.Parameters.Add(p1);
                    var p2 = cmdDet.CreateParameter(); p2.Value = nazwisko; cmdDet.Parameters.Add(p2);
                    var p3 = cmdDet.CreateParameter(); p3.Value = firmaId.Value; cmdDet.Parameters.Add(p3);
                }
                else
                {
                    cmdDet.CommandText = @"
                        SELECT P_ID, P_imie, P_nazwisko, P_firma, P_Firma_id, P_Activ
                        FROM P_Pacjent
                        WHERE TRIM(P_imie) = ? AND TRIM(P_nazwisko) = ? AND P_Firma_id IS NULL
                        ORDER BY P_ID";
                    var p1 = cmdDet.CreateParameter(); p1.Value = imie; cmdDet.Parameters.Add(p1);
                    var p2 = cmdDet.CreateParameter(); p2.Value = nazwisko; cmdDet.Parameters.Add(p2);
                }

                var firmaLabel = firmaId.HasValue ? $"Firma ID:{firmaId}" : "brak firmy";
                var grupa = new DuplikatGrupa
                {
                    Tabela = "Pacjenci",
                    KluczGrupy = $"{imie} {nazwisko} [{firmaLabel}]",
                    Liczba = cnt
                };

                using (var rd = cmdDet.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        grupa.Rekordy.Add(new DuplikatRekord
                        {
                            Id = Convert.ToInt32(rd["P_ID"]),
                            Kolumny = new Dictionary<string, string>
                            {
                                ["P_ID"] = rd["P_ID"]?.ToString() ?? "",
                                ["P_imie"] = rd["P_imie"]?.ToString() ?? "",
                                ["P_nazwisko"] = rd["P_nazwisko"]?.ToString() ?? "",
                                ["P_firma"] = rd["P_firma"]?.ToString() ?? "",
                                ["P_Firma_id"] = rd["P_Firma_id"]?.ToString() ?? "",
                                ["Aktywny"] = rd["P_Activ"] is bool b ? (b ? "Tak" : "Nie") : rd["P_Activ"]?.ToString() ?? ""
                            }
                        });
                    }
                }

                GrupyDuplikatow.Add(grupa);
            }
        }

        // ── Firmy: duplikaty po TRIM(Nazwa) ──
        private void SzukajDuplikatowFirm()
        {
            var db = new AccessDbHelper();
            using var conn = db.GetConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT TRIM(Nazwa) AS fn, COUNT(*) AS cnt
                FROM Firma
                GROUP BY TRIM(Nazwa)
                HAVING COUNT(*) > 1
                ORDER BY COUNT(*) DESC";

            var grupy = new List<(string nazwa, int cnt)>();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    grupy.Add((
                        reader["fn"]?.ToString() ?? "",
                        Convert.ToInt32(reader["cnt"])
                    ));
                }
            }

            foreach (var (nazwa, cnt) in grupy)
            {
                using var cmdDet = conn.CreateCommand();
                cmdDet.CommandText = @"
                    SELECT id, Nazwa, NIP, Miejscowosc, activ
                    FROM Firma
                    WHERE TRIM(Nazwa) = ?
                    ORDER BY id";
                var p1 = cmdDet.CreateParameter(); p1.Value = nazwa; cmdDet.Parameters.Add(p1);

                var grupa = new DuplikatGrupa
                {
                    Tabela = "Firmy",
                    KluczGrupy = nazwa,
                    Liczba = cnt
                };

                using (var rd = cmdDet.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        grupa.Rekordy.Add(new DuplikatRekord
                        {
                            Id = Convert.ToInt32(rd["id"]),
                            Kolumny = new Dictionary<string, string>
                            {
                                ["id"] = rd["id"]?.ToString() ?? "",
                                ["Nazwa"] = rd["Nazwa"]?.ToString() ?? "",
                                ["NIP"] = rd["NIP"]?.ToString() ?? "",
                                ["Miejscowosc"] = rd["Miejscowosc"]?.ToString() ?? "",
                                ["Aktywna"] = rd["activ"] is bool b ? (b ? "Tak" : "Nie") : rd["activ"]?.ToString() ?? ""
                            }
                        });
                    }
                }

                GrupyDuplikatow.Add(grupa);
            }
        }

        // ── Skierowania: duplikaty po B_Pacjent_ID + B_Firma_ID + B_DataSkierowania ──
        private void SzukajDuplikatowSkierowan()
        {
            var db = new AccessDbHelper();
            using var conn = db.GetConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT B_Pacjent_ID, B_Firma_ID, B_DataSkierowania, COUNT(*) AS cnt
                FROM B_Skierowania
                WHERE B_Pacjent_ID IS NOT NULL
                GROUP BY B_Pacjent_ID, B_Firma_ID, B_DataSkierowania
                HAVING COUNT(*) > 1
                ORDER BY COUNT(*) DESC";

            var grupy = new List<(int? pacjentId, int? firmaId, DateTime? data, int cnt)>();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    grupy.Add((
                        reader["B_Pacjent_ID"] != DBNull.Value ? Convert.ToInt32(reader["B_Pacjent_ID"]) : null,
                        reader["B_Firma_ID"] != DBNull.Value ? Convert.ToInt32(reader["B_Firma_ID"]) : null,
                        reader["B_DataSkierowania"] != DBNull.Value ? Convert.ToDateTime(reader["B_DataSkierowania"]) : null,
                        Convert.ToInt32(reader["cnt"])
                    ));
                }
            }

            foreach (var (pacjentId, firmaId, data, cnt) in grupy)
            {
                using var cmdDet = conn.CreateCommand();
                cmdDet.CommandText = @"
                    SELECT B_ID, B_Pacjent_ID, B_Firma_ID, B_Badanie_ID, B_DataSkierowania, B_TypBadania, B_Activ
                    FROM B_Skierowania
                    WHERE (B_Pacjent_ID = ? OR (B_Pacjent_ID IS NULL AND ? IS NULL))
                      AND (B_Firma_ID = ? OR (B_Firma_ID IS NULL AND ? IS NULL))
                      AND (B_DataSkierowania = ? OR (B_DataSkierowania IS NULL AND ? IS NULL))
                    ORDER BY B_ID";
                var pp1 = cmdDet.CreateParameter(); pp1.Value = (object?)pacjentId ?? DBNull.Value; cmdDet.Parameters.Add(pp1);
                var pp2 = cmdDet.CreateParameter(); pp2.Value = (object?)pacjentId ?? DBNull.Value; cmdDet.Parameters.Add(pp2);
                var pp3 = cmdDet.CreateParameter(); pp3.Value = (object?)firmaId ?? DBNull.Value; cmdDet.Parameters.Add(pp3);
                var pp4 = cmdDet.CreateParameter(); pp4.Value = (object?)firmaId ?? DBNull.Value; cmdDet.Parameters.Add(pp4);
                var pp5 = cmdDet.CreateParameter(); pp5.Value = (object?)data ?? DBNull.Value; cmdDet.Parameters.Add(pp5);
                var pp6 = cmdDet.CreateParameter(); pp6.Value = (object?)data ?? DBNull.Value; cmdDet.Parameters.Add(pp6);

                var grupa = new DuplikatGrupa
                {
                    Tabela = "Skierowania",
                    KluczGrupy = $"Pacjent:{pacjentId} Firma:{firmaId} Data:{data:dd.MM.yyyy}",
                    Liczba = cnt
                };

                using (var rd = cmdDet.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        grupa.Rekordy.Add(new DuplikatRekord
                        {
                            Id = Convert.ToInt32(rd["B_ID"]),
                            Kolumny = new Dictionary<string, string>
                            {
                                ["B_ID"] = rd["B_ID"]?.ToString() ?? "",
                                ["B_Pacjent_ID"] = rd["B_Pacjent_ID"]?.ToString() ?? "",
                                ["B_Firma_ID"] = rd["B_Firma_ID"]?.ToString() ?? "",
                                ["B_Badanie_ID"] = rd["B_Badanie_ID"]?.ToString() ?? "",
                                ["B_DataSkierowania"] = rd["B_DataSkierowania"] != DBNull.Value
                                    ? Convert.ToDateTime(rd["B_DataSkierowania"]).ToString("dd.MM.yyyy") : "",
                                ["B_TypBadania"] = rd["B_TypBadania"]?.ToString() ?? "",
                                ["Aktywne"] = rd["B_Activ"] is bool b2 ? (b2 ? "Tak" : "Nie") : rd["B_Activ"]?.ToString() ?? ""
                            }
                        });
                    }
                }

                GrupyDuplikatow.Add(grupa);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  Dialog scalania
        // ══════════════════════════════════════════════════════════
        private void OtworzDialogScalania()
        {
            if (WybranaGrupa == null) return;

            try
            {
                var dialog = new Views.DuplikatyScalDialog(WybranaGrupa);
                dialog.Owner = Application.Current.MainWindow;
                if (dialog.ShowDialog() == true && dialog.WybranyGlownyId.HasValue)
                {
                    ScalRekordy(WybranaGrupa, dialog.WybranyGlownyId.Value);
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[DuplikatyVM] OtworzDialogScalania error: {ex}");
                MessageBox.Show($"Błąd otwierania dialogu scalania:\n{ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  Scalanie rekordów
        // ══════════════════════════════════════════════════════════
        private void ScalRekordy(DuplikatGrupa grupa, int glownyId)
        {
            try
            {
                var doUsuniecia = grupa.Rekordy.Where(r => r.Id != glownyId).Select(r => r.Id).ToList();
                if (doUsuniecia.Count == 0) return;

                var db = new AccessDbHelper();
                using var conn = db.GetConnection();
                conn.Open();
                using var tx = conn.BeginTransaction();

                try
                {
                    switch (grupa.Tabela)
                    {
                        case "Pacjenci":
                            ScalPacjentow(conn, tx, glownyId, doUsuniecia);
                            break;
                        case "Firmy":
                            ScalFirmy(conn, tx, glownyId, doUsuniecia);
                            break;
                        case "Skierowania":
                            ScalSkierowania(conn, tx, glownyId, doUsuniecia);
                            break;
                        case "Badania":
                            ScalBadania(conn, tx, glownyId, doUsuniecia);
                            break;
                    }

                    tx.Commit();

                    MessageBox.Show(
                        $"Scalono {doUsuniecia.Count + 1} rekordów → ID {glownyId}.\n" +
                        $"Usunięto duplikaty: {string.Join(", ", doUsuniecia)}",
                        "Scalanie zakończone", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Odśwież listę
                    SzukajDuplikatow();
                }
                catch (Exception ex)
                {
                    try { tx.Rollback(); } catch { }
                    throw new InvalidOperationException($"Błąd transakcji scalania: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"[DuplikatyVM] ScalRekordy error: {ex}");
                MessageBox.Show($"Błąd scalania:\n{ex.Message}", "Błąd",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Scalanie pacjentów:
        /// 1. Przepnij B_Skierowania.B_Pacjent_ID → główny
        /// 2. Przepnij Badanie.Bad_P_ID → główny
        /// 3. Usuń duplikaty z P_Pacjent
        /// </summary>
        private void ScalPacjentow(IDbConnection conn, IDbTransaction tx, int glownyId, List<int> doUsuniecia)
        {
            foreach (var dupId in doUsuniecia)
            {
                // Przepnij skierowania
                ExecuteUpdate(conn, tx,
                    "UPDATE B_Skierowania SET B_Pacjent_ID = ? WHERE B_Pacjent_ID = ?",
                    glownyId, dupId);

                // Przepnij badania
                ExecuteUpdate(conn, tx,
                    "UPDATE Badanie SET Bad_P_ID = ? WHERE Bad_P_ID = ?",
                    glownyId, dupId);

                // Usuń duplikat pacjenta
                ExecuteUpdate(conn, tx,
                    "DELETE FROM P_Pacjent WHERE P_ID = ?",
                    dupId);
            }
        }

        /// <summary>
        /// Scalanie firm:
        /// 1. Przepnij P_Pacjent.P_Firma_id → główna
        /// 2. Przepnij B_Skierowania.B_Firma_ID → główna
        /// 3. Przepnij Badanie.Bad_F_ID → główna
        /// 4. Przepnij Faktura.FK_Firma_ID → główna
        /// 5. Przepnij ListyBadan.L_Firma_ID → główna
        /// 6. Usuń duplikaty z Firma
        /// </summary>
        private void ScalFirmy(IDbConnection conn, IDbTransaction tx, int glownyId, List<int> doUsuniecia)
        {
            foreach (var dupId in doUsuniecia)
            {
                ExecuteUpdate(conn, tx,
                    "UPDATE P_Pacjent SET P_Firma_id = ? WHERE P_Firma_id = ?",
                    glownyId, dupId);

                ExecuteUpdate(conn, tx,
                    "UPDATE B_Skierowania SET B_Firma_ID = ? WHERE B_Firma_ID = ?",
                    glownyId, dupId);

                ExecuteUpdate(conn, tx,
                    "UPDATE Badanie SET Bad_F_ID = ? WHERE Bad_F_ID = ?",
                    glownyId, dupId);

                ExecuteUpdate(conn, tx,
                    "UPDATE Faktura SET FK_Firma_ID = ? WHERE FK_Firma_ID = ?",
                    glownyId, dupId);

                ExecuteUpdate(conn, tx,
                    "UPDATE ListyBadan SET L_Firma_ID = ? WHERE L_Firma_ID = ?",
                    glownyId, dupId);

                ExecuteUpdate(conn, tx,
                    "DELETE FROM Firma WHERE id = ?",
                    dupId);
            }
        }

        /// <summary>
        /// Scalanie skierowań:
        /// 1. Przepnij Badanie.Bad_S_ID → główne
        /// 2. Usuń duplikaty z B_Skierowania
        /// </summary>
        private void ScalSkierowania(IDbConnection conn, IDbTransaction tx, int glownyId, List<int> doUsuniecia)
        {
            foreach (var dupId in doUsuniecia)
            {
                ExecuteUpdate(conn, tx,
                    "UPDATE Badanie SET Bad_S_ID = ? WHERE Bad_S_ID = ?",
                    glownyId, dupId);

                ExecuteUpdate(conn, tx,
                    "DELETE FROM B_Skierowania WHERE B_ID = ?",
                    dupId);
            }
        }

        // ── Badania: duplikaty po Bad_S_ID (to samo skierowanie = duplikat badania) ──
        private void SzukajDuplikatowBadan()
        {
            var db = new AccessDbHelper();
            using var conn = db.GetConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Bad_S_ID, COUNT(*) AS cnt
                FROM Badanie
                WHERE Bad_S_ID IS NOT NULL
                GROUP BY Bad_S_ID
                HAVING COUNT(*) > 1
                ORDER BY COUNT(*) DESC";

            var grupy = new List<(int skierowanieId, int cnt)>();
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    grupy.Add((
                        Convert.ToInt32(reader["Bad_S_ID"]),
                        Convert.ToInt32(reader["cnt"])
                    ));
                }
            }

            foreach (var (skierowanieId, cnt) in grupy)
            {
                using var cmdDet = conn.CreateCommand();
                cmdDet.CommandText = @"
                    SELECT Bad_ID, Bad_S_ID, Bad_P_ID, Bad_F_ID, Bad_L_ID, Bad_Typ, Bad_Data,
                           Bad_Razem, Bad_Fakt, Bad_Nr_KS
                    FROM Badanie
                    WHERE Bad_S_ID = ?
                    ORDER BY Bad_ID";
                var p1 = cmdDet.CreateParameter(); p1.Value = skierowanieId; cmdDet.Parameters.Add(p1);

                var grupa = new DuplikatGrupa
                {
                    Tabela = "Badania",
                    KluczGrupy = $"Skierowanie ID:{skierowanieId}",
                    Liczba = cnt
                };

                using (var rd = cmdDet.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        grupa.Rekordy.Add(new DuplikatRekord
                        {
                            Id = Convert.ToInt32(rd["Bad_ID"]),
                            Kolumny = new Dictionary<string, string>
                            {
                                ["Bad_ID"] = rd["Bad_ID"]?.ToString() ?? "",
                                ["Bad_S_ID"] = rd["Bad_S_ID"]?.ToString() ?? "",
                                ["Bad_P_ID"] = rd["Bad_P_ID"]?.ToString() ?? "",
                                ["Bad_F_ID"] = rd["Bad_F_ID"]?.ToString() ?? "",
                                ["Bad_L_ID"] = rd["Bad_L_ID"]?.ToString() ?? "",
                                ["Bad_Typ"] = rd["Bad_Typ"]?.ToString() ?? "",
                                ["Bad_Data"] = rd["Bad_Data"] != DBNull.Value
                                    ? Convert.ToDateTime(rd["Bad_Data"]).ToString("dd.MM.yyyy") : "",
                                ["Bad_Razem"] = rd["Bad_Razem"] != DBNull.Value
                                    ? Convert.ToDecimal(rd["Bad_Razem"]).ToString("N2") : "",
                                ["Bad_Fakt"] = rd["Bad_Fakt"]?.ToString() ?? "",
                                ["Bad_Nr_KS"] = rd["Bad_Nr_KS"]?.ToString() ?? ""
                            }
                        });
                    }
                }

                GrupyDuplikatow.Add(grupa);
            }
        }

        /// <summary>
        /// Scalanie badań (ten sam Bad_S_ID):
        /// 1. Przepnij Bad_L_ID — jeśli główne badanie nie ma listy, przejmij z duplikatu
        /// 2. Przepnij B_Skierowania.B_Badanie_ID → główne
        /// 3. Usuń duplikaty z Badanie
        /// </summary>
        private void ScalBadania(IDbConnection conn, IDbTransaction tx, int glownyId, List<int> doUsuniecia)
        {
            foreach (var dupId in doUsuniecia)
            {
                // Przepnij B_Badanie_ID w skierowaniach (jeśli wskazywało na duplikat)
                ExecuteUpdate(conn, tx,
                    "UPDATE B_Skierowania SET B_Badanie_ID = ? WHERE B_Badanie_ID = ?",
                    glownyId, dupId);

                // Usuń duplikat badania
                ExecuteUpdate(conn, tx,
                    "DELETE FROM Badanie WHERE Bad_ID = ?",
                    dupId);
            }
        }

        // ── Helper: wykonaj UPDATE/DELETE z parametrami ──
        private static void ExecuteUpdate(IDbConnection conn, IDbTransaction tx, string sql, params object[] values)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            foreach (var val in values)
            {
                var p = cmd.CreateParameter();
                p.Value = val ?? DBNull.Value;
                cmd.Parameters.Add(p);
            }
            cmd.ExecuteNonQuery();
        }
    }

    // ══════════════════════════════════════════════════════════
    //  Modele danych duplikatów
    // ══════════════════════════════════════════════════════════

    public class DuplikatGrupa
    {
        public string Tabela { get; set; } = "";
        public string KluczGrupy { get; set; } = "";
        public int Liczba { get; set; }
        public ObservableCollection<DuplikatRekord> Rekordy { get; } = new();

        public string Display => $"{KluczGrupy}  ({Liczba} szt.)";
    }

    public class DuplikatRekord
    {
        public int Id { get; set; }
        public Dictionary<string, string> Kolumny { get; set; } = new();

        public string Display
        {
            get
            {
                var parts = Kolumny.Select(kv => $"{kv.Key}: {kv.Value}");
                return string.Join(" | ", parts);
            }
        }
    }
}
