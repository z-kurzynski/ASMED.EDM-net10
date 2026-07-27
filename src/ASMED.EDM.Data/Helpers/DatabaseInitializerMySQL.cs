using MySqlConnector;
using ASMED.EDM.Data.Services;

namespace ASMED.EDM.Data.Helpers;

/// <summary>
/// Inicjalizator schematu bazy MySQL — odpowiednik DatabaseInitializer dla Access.
/// Pattern z TelsaTelecomBiling: CREATE TABLE IF NOT EXISTS + seed data.
/// Tworzy wszystkie tabele jeśli nie istnieją i wstawia dane startowe.
/// Bezpieczne do wielokrotnego wywołania.
/// </summary>
public static class DatabaseInitializerMySQL
{
    // ── Konwersja typów Access → MySQL ──────────────────────────────────────────
    // Liczba całkowita długa (Long)  → INT
    // Krótki tekst (Short Text)       → VARCHAR(n)
    // Długi tekst (Memo)              → TEXT
    // Tak/Nie (Boolean)               → TINYINT(1)
    // Data i godzina                  → DATETIME
    // Waluta                          → DECIMAL(18,4)
    // ────────────────────────────────────────────────────────────────────────────

    private static readonly (string Name, string Sql)[] Tables =
    [
        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: P_Pacjent (Pacjenci)
        // ══════════════════════════════════════════════════════════════════════════
        ("P_Pacjent", """
            CREATE TABLE IF NOT EXISTS `P_Pacjent` (
                P_ID                INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                P_pesel             VARCHAR(11),
                P_plec              VARCHAR(255),
                P_imie              VARCHAR(255),
                P_nazwisko          VARCHAR(255),
                P_Ades_kod          VARCHAR(255),
                P_Adres_ulica_numer VARCHAR(255),
                P_Ades_miasto       VARCHAR(255),
                P_zawod             VARCHAR(255),
                P_firma             VARCHAR(255),
                P_Adres_kraj        VARCHAR(255),
                P_data_urodzenia    DATETIME,
                P_obywatelstwo      VARCHAR(255),
                P_telefon           VARCHAR(255),
                P_email             VARCHAR(255),
                P_Firma_id          INT,
                P_Uwagi             TEXT,

                INDEX idx_pesel (P_pesel),
                INDEX idx_nazwisko (P_nazwisko),
                INDEX idx_firma (P_Firma_id)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: Firma (Firmy/Kontrahenci)
        // ══════════════════════════════════════════════════════════════════════════
        ("Firma", """
            CREATE TABLE IF NOT EXISTS `Firma` (
                id                          INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                activ                       TINYINT(1) DEFAULT 1,
                Del                         TINYINT(1) DEFAULT 0,
                Cennik                      VARCHAR(255),
                Nazwa                       VARCHAR(255),
                Ulica                       VARCHAR(255),
                Kod                         VARCHAR(255),
                Miejscowosc                 VARCHAR(255),
                NIP                         VARCHAR(255),
                brak_nip                    TINYINT(1) DEFAULT 0,
                Regon                       VARCHAR(255),
                Kraj                        VARCHAR(255),
                umowa_do                    VARCHAR(255),
                czas_nieokreslon            TINYINT(1) DEFAULT 0,
                Osoba_kontaktowa            VARCHAR(255),
                Telefon                     VARCHAR(255),
                Email                       VARCHAR(255),
                Metoda_platnosci            VARCHAR(255),
                Termin_platnosci            VARCHAR(255),
                Nabywca_platnik             VARCHAR(255),
                Sposob_przeslania_faktury   TINYINT(1) DEFAULT 0,
                FKemail                     VARCHAR(255),

                INDEX idx_nip (NIP),
                INDEX idx_nazwa (Nazwa),
                INDEX idx_activ (activ)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: Umowy_Firm (Umowy z firmami)
        // ══════════════════════════════════════════════════════════════════════════
        ("Umowy_Firm", """
            CREATE TABLE IF NOT EXISTS `Umowy_Firm` (
                Id                  INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                Firma_ID            INT,
                Data_Umowy          DATETIME,
                Ilosc_Miesiecy      INT,
                Status              VARCHAR(255),
                Budzet              DECIMAL(18,4),
                Data_Koncowa        DATETIME,
                nr_umowy            VARCHAR(255),

                INDEX idx_firma (Firma_ID),
                FOREIGN KEY (Firma_ID) REFERENCES Firma(id) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: BAD_Lista (Lista badań)
        // ══════════════════════════════════════════════════════════════════════════
        ("BAD_Lista", """
            CREATE TABLE IF NOT EXISTS `BAD_Lista` (
                Identyfikator   INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                bn_nazwa        VARCHAR(255),
                bn_activ        TINYINT(1) DEFAULT 1,
                bn_cennik       VARCHAR(255),
                bn_Cen_activ    TINYINT(1) DEFAULT 1,

                INDEX idx_bn_nazwa (bn_nazwa)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: BAD_Cennik (Cennik badań)
        // ══════════════════════════════════════════════════════════════════════════
        ("BAD_Cennik", """
            CREATE TABLE IF NOT EXISTS `BAD_Cennik` (
                Identyfikator   INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                b_Nazwa         VARCHAR(255),
                b_Cena          DECIMAL(18,4),
                b_Vat           INT,
                b_Cennik        VARCHAR(255),
                b_activ         TINYINT(1) DEFAULT 1,

                INDEX idx_b_Cennik (b_Cennik)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: B_Skierowania (Skierowania na badania)
        // ══════════════════════════════════════════════════════════════════════════
        ("B_Skierowania", """
            CREATE TABLE IF NOT EXISTS `B_Skierowania` (
                B_ID                INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                B_Pacjent_ID        INT,
                B_ID_OLD            INT,
                B_ID_pacjenta       INT,
                B_Firma_ID          INT,
                B_Badanie_ID        INT,
                B_Faktura_ID        INT,
                B_DataSkierowania   DATETIME,
                B_TypBadania        VARCHAR(255),
                B_Stanowisko        VARCHAR(255),
                B_RegistrationDate  DATETIME,
                B_czynnik_fizyczny  TINYINT(1),
                B_czynnik_fizyczny_opis VARCHAR(255),
                B_czynnik_pylowy    TINYINT(1),
                B_czynnik_pylowy_opis VARCHAR(255),
                B_czynnik_chemiczny TINYINT(1),
                B_czynnik_chemiczny_opis VARCHAR(255),
                B_czynnik_biologiczny TINYINT(1),
                B_czynnik_biologiczny_opis VARCHAR(255),
                B_czynnik_inny      TINYINT(1),
                B_czynnik_inny_opis VARCHAR(255),
                B_czynnik_sanepid   TINYINT(1),
                B_czynnik_sanepid_opis VARCHAR(255),
                B_Comments          VARCHAR(255),
                B_Scan              TINYINT(1),
                B_Zaswiadczenie     TINYINT(1),
                B_ksiazeczka        TINYINT(1),
                B_Ankieta           TINYINT(1),

                INDEX idx_pacjent (B_Pacjent_ID),
                INDEX idx_firma (B_Firma_ID),
                INDEX idx_badanie (B_Badanie_ID),
                INDEX idx_faktura (B_Faktura_ID),
                INDEX idx_data_skierowania (B_DataSkierowania),
                FOREIGN KEY (B_Pacjent_ID) REFERENCES P_Pacjent(P_ID) ON DELETE CASCADE,
                FOREIGN KEY (B_Firma_ID) REFERENCES Firma(id) ON DELETE SET NULL,
                FOREIGN KEY (B_Badanie_ID) REFERENCES BAD_Lista(Identyfikator) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: Badanie (Wyniki badań)
        // ══════════════════════════════════════════════════════════════════════════
        ("Badanie", """
            CREATE TABLE IF NOT EXISTS `Badanie` (
                Bad_ID          INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                Bad_R_ID        INT,
                Bad_S_ID        INT,
                Bad_P_ID        INT,
                Bad_L_ID        INT,
                Bad_F_ID        INT,
                Bad_Fakt_ID     INT,
                Bad_Fakt        VARCHAR(255),
                Bad_bn_cennik   VARCHAR(255),
                Bad_Typ         VARCHAR(50),
                Bad_Data        DATETIME,
                Bad_Data_Do     DATETIME,
                Bad_Wynik       TEXT,
                Bad_Cena1       DECIMAL(18,4),
                Bad_Cena2       DECIMAL(18,4),
                Bad_Cena3       DECIMAL(18,4),
                Bad_Cena4       DECIMAL(18,4),
                Bad_Cena5       DECIMAL(18,4),
                Bad_Cena6       DECIMAL(18,4),
                Bad_Cena7       DECIMAL(18,4),
                Bad_Cena8       DECIMAL(18,4),
                Bad_Cena9       DECIMAL(18,4),
                Bad_Cena10      DECIMAL(18,4),
                Bad_Razem       DECIMAL(18,4),
                Bad_Nr_KS       VARCHAR(255),
                Bad_ID_Numer    VARCHAR(255),
                Bad_END         TINYINT(1) DEFAULT 0,

                INDEX idx_Bad_P_ID (Bad_P_ID),
                INDEX idx_Bad_S_ID (Bad_S_ID),
                INDEX idx_Bad_Fakt_ID (Bad_Fakt_ID),
                INDEX idx_Bad_L_ID (Bad_L_ID),
                INDEX idx_Bad_F_ID (Bad_F_ID)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: Faktura (Faktury)
        // ══════════════════════════════════════════════════════════════════════════
        ("Faktura", """
            CREATE TABLE IF NOT EXISTS `Faktura` (
                FK_ID           INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                FK_Firma_ID     INT,
                FK_Numer        VARCHAR(30),
                FK_Data         DATETIME,
                FK_Kwota        DECIMAL(18,4),
                FK_Uwagi        VARCHAR(255),
                FK_Cennik       VARCHAR(255),
                FK_Suma_Bad     DECIMAL(18,4),
                FK_Saldo        DECIMAL(18,4),
                FK_Status       VARCHAR(20),
                FK_PDF          TINYINT(1) DEFAULT 0,
                FK_Num_Listy    INT,

                INDEX idx_FK_Firma_ID (FK_Firma_ID),
                INDEX idx_FK_Data (FK_Data),
                FOREIGN KEY (FK_Firma_ID) REFERENCES Firma(id) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: Rejestracja (Wizyty/Rejestracja)
        // ══════════════════════════════════════════════════════════════════════════
        ("Rejestracja", """
            CREATE TABLE IF NOT EXISTS `Rejestracja` (
                R_ID            INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                R_B_ID          INT,
                R_Data          DATETIME,
                R_Status        VARCHAR(255),
                R_Employee_ID   INT,
                R_S_ID          INT,
                R_P_ID          INT,
                R_GG_MM         VARCHAR(255),
                R_Uwagi         VARCHAR(255),
                R_Subject       VARCHAR(255),

                INDEX idx_R_P_ID (R_P_ID),
                INDEX idx_R_Data (R_Data),
                INDEX idx_R_B_ID (R_B_ID)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: Users (Użytkownicy systemu)
        // ══════════════════════════════════════════════════════════════════════════
        ("Users", """
            CREATE TABLE IF NOT EXISTS `Users` (
                Id              INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                Username        VARCHAR(255) NOT NULL UNIQUE,
                PasswordHash    VARCHAR(255),
                Email           VARCHAR(255),
                FullName        VARCHAR(255),
                Role            VARCHAR(255),
                CreatedDate     DATETIME,
                LastLogin       DATETIME,
                EndLogin        DATETIME,

                INDEX idx_Username (Username)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: LoginHistory (Historia logowań)
        // ══════════════════════════════════════════════════════════════════════════
        ("LoginHistory", """
            CREATE TABLE IF NOT EXISTS `LoginHistory` (
                Id              INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                UserId          INT,
                Username        VARCHAR(255),
                LoginTime       DATETIME,
                LogoutTime      DATETIME,
                ComputerName    VARCHAR(255),
                IpAddress       VARCHAR(50),
                FailureReason   VARCHAR(255),

                INDEX idx_UserId (UserId),
                INDEX idx_LoginTime (LoginTime),
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELE SŁOWNIKOWE (S_*)
        // ══════════════════════════════════════════════════════════════════════════
        ("S_Imiona", """
            CREATE TABLE IF NOT EXISTS `S_Imiona` (
                Identyfikator   INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                S_imie          VARCHAR(255),
                S_ile           INT,
                S_plec          VARCHAR(255)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        ("S_Nazwisko", """
            CREATE TABLE IF NOT EXISTS `S_Nazwisko` (
                Identyfikator   INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                S_Nazwisko      VARCHAR(255)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        ("S__Ulice", """
            CREATE TABLE IF NOT EXISTS `S__Ulice` (
                Identyfikator   INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                S_Ulica         VARCHAR(255)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        ("Gminy", """
            CREATE TABLE IF NOT EXISTS `Gminy` (
                autonumer       INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                Gmina           VARCHAR(255),
                Miasto          VARCHAR(255),
                Opis            VARCHAR(255),
                Kod             VARCHAR(255),
                MiastoUp        VARCHAR(255),
                idgB            INT
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        ("FormatowanieTekstu", """
            CREATE TABLE IF NOT EXISTS `FormatowanieTekstu` (
                ID          INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                Slowo       VARCHAR(100),
                FormatTyp   VARCHAR(255),
                Kategoria   VARCHAR(255)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        ("S_hints", """
            CREATE TABLE IF NOT EXISTS `S_hints` (
                S_hints_ID          INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                S_hints_jobTitle    VARCHAR(255),
                S_hints_name        VARCHAR(255),
                S_hints_last_name   VARCHAR(255)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELE POMOCNICZE / IMPORT
        // ══════════════════════════════════════════════════════════════════════════

        ("Daj_Bad", """
            CREATE TABLE IF NOT EXISTS `Daj_Bad` (
                Lx_ID_Skierowania INT,
                Max_B_ID        INT,
                B_ID_pacjenta   INT,
                Identyfikator   INT NOT NULL AUTO_INCREMENT PRIMARY KEY,

                INDEX idx_skierowania (Lx_ID_Skierowania),
                INDEX idx_pacjenta (B_ID_pacjenta)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        ("ListyBadan", """
            CREATE TABLE IF NOT EXISTS `ListyBadan` (
                Identyfikator   INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                L_FK_ID         INT,
                L_Firma_ID      INT,
                L_Data          DATETIME,
                L_Uwagi         VARCHAR(255),
                L_Email         TINYINT(1),
                L_Email_Adres   VARCHAR(255),
                L_Email_data    DATETIME,
                L_End           TINYINT(1),
                L_Wydruk_Typ    VARCHAR(255),
                L_Nazwa         VARCHAR(255),

                INDEX idx_firma (L_Firma_ID),
                FOREIGN KEY (L_Firma_ID) REFERENCES Firma(id) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        ("PES_Import_GOV", """
            CREATE TABLE IF NOT EXISTS `PES_Import_GOV` (
                Identyfikator       INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                PE_ID_Pacjent       INT,
                PE_Pesel            VARCHAR(11),
                PE_plec             VARCHAR(255),
                PE_imie             VARCHAR(255),
                PE_nazwisko         VARCHAR(255),
                PE_data_urodzenia   DATETIME,
                PE_Ades_kod         VARCHAR(255),
                PE_Ades_miasto      VARCHAR(255),
                PE_Adres_ulica_numer VARCHAR(255),

                INDEX idx_pesel (PE_Pesel),
                INDEX idx_pacjent (PE_ID_Pacjent),
                FOREIGN KEY (PE_ID_Pacjent) REFERENCES P_Pacjent(P_ID) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """)
    ];

    // ══════════════════════════════════════════════════════════════════════════════
    // Seed Data (dane startowe)
    // ══════════════════════════════════════════════════════════════════════════════
    private static readonly (string Table, string Sql)[] SeedData =
    [
        ("Users", """
            INSERT INTO `Users` (U_Login, U_Password, U_Imie, U_Nazwisko, U_Email, U_Rola, U_Active, U_RegistrationDate)
            VALUES ('admin', 'admin', 'Administrator', 'Systemu', 'admin@asmed.pl', 'Admin', 1, NOW())
            ON DUPLICATE KEY UPDATE U_Login = U_Login
            """)
    ];

    // ══════════════════════════════════════════════════════════════════════════════
    // Migrations (dodawanie nowych kolumn do istniejących tabel)
    // ══════════════════════════════════════════════════════════════════════════════
    private static readonly (string Table, string Column, string TypeDef)[] Migrations =
    [
        // Placeholder - miejsce na przyszłe migracje
        // Przykład: ("P_Pacjent", "P_PESEL_2", "VARCHAR(11)")
    ];

    // ══════════════════════════════════════════════════════════════════════════════
    // Metoda główna RunAsync
    // ══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Zwraca listę nazw wszystkich tabel zdefiniowanych w schemacie MySQL.
    /// Używana przez MigrationService do budowania listy dostępnych tabel.
    /// </summary>
    public static IReadOnlyList<string> GetTableNames()
        => Tables.Select(t => t.Name).ToList();

    /// <summary>
    /// Uruchamia inicjalizację bazy danych MySQL (używa DbConnectionFactory).
    /// </summary>
    public static Task<InitResult> RunAsync(DbConnectionFactory factory)
        => RunAsync(factory.ActiveConnectionString);

    /// <summary>
    /// Uruchamia inicjalizację bazy danych MySQL z podanym connection stringiem.
    /// </summary>
    public static async Task<InitResult> RunAsync(string connectionString)
    {
        var result = new InitResult();

        await Task.Run(() =>
        {
            using var conn = new MySqlConnection(connectionString);
            conn.Open();

            // Pobierz nazwę bazy z connection stringa
            result.DatabaseName = conn.Database;
            result.ServerName = conn.DataSource;

            // Faza 1 — tworzenie tabel
            foreach (var (name, sql) in Tables)
            {
                try
                {
                    // Sprawdź czy tabela już istnieje
                    bool tableExists = false;
                    using (var checkCmd = new MySqlCommand(
                        $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{conn.Database}' AND table_name = '{name}'", conn))
                    {
                        tableExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                    }

                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();

                    if (tableExists)
                    {
                        result.AlreadyExisted.Add(name);
                    }
                    else
                    {
                        result.Created.Add(name);
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{name}: {ex.Message}");
                }
            }

            // Faza 2 — migracje (ADD COLUMN IF NOT EXISTS)
            foreach (var (table, column, typeDef) in Migrations)
            {
                try
                {
                    using var cmd = new MySqlCommand(
                        $"ALTER TABLE `{table}` ADD COLUMN IF NOT EXISTS `{column}` {typeDef}", conn);
                    cmd.ExecuteNonQuery();
                }
                catch { /* kolumna już istnieje lub tabela nie istnieje — pomijamy */ }
            }

            // Faza 3 — seed danych startowych
            foreach (var (table, sql) in SeedData)
            {
                try
                {
                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                }
                catch { /* seed opcjonalny */ }
            }
        });

        return result;
    }
}

/// <summary>
/// Wynik inicjalizacji bazy danych.
/// </summary>
public class InitResult
{
    public string DatabaseName { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public List<string> Created { get; } = [];
    public List<string> AlreadyExisted { get; } = [];
    public List<string> Errors { get; } = [];
    public bool HasErrors => Errors.Count > 0;
    public int TotalTables => Created.Count + AlreadyExisted.Count;
}
