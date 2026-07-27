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
                P_Nazwisko          VARCHAR(255),
                P_Imie              VARCHAR(255),
                P_Pesel             VARCHAR(11),
                P_DataUrodzenia     DATETIME,
                P_Plec              VARCHAR(10),
                P_Telefon           VARCHAR(50),
                P_Email             VARCHAR(255),
                P_Adres_Ulica       VARCHAR(255),
                P_Adres_Miasto      VARCHAR(255),
                P_Adres_Kod         VARCHAR(10),
                P_Adres_Wojewodztwo VARCHAR(100),
                P_Adres_Gmina       VARCHAR(100),
                P_Adres_Poczta      VARCHAR(100),
                P_Nr_Domu           VARCHAR(20),
                P_Nr_Mieszkania     VARCHAR(20),
                P_Wyksztalcenie     VARCHAR(100),
                P_Stan_Cywilny      VARCHAR(50),
                P_Obywatelstwo      VARCHAR(100),
                P_Miejsce_Urodzenia VARCHAR(255),
                P_Imie_Ojca         VARCHAR(255),
                P_Imie_Matki        VARCHAR(255),
                P_Nazwisko_Rodowe_Matki VARCHAR(255),
                P_Nr_Dowodu         VARCHAR(50),
                P_Seria_Dowodu      VARCHAR(10),
                P_Data_Wydania_Dowodu DATETIME,
                P_Wydany_Przez      VARCHAR(255),
                P_Comments          TEXT,
                P_Active            TINYINT(1) DEFAULT 1,
                P_RegistrationDate  DATETIME,
                P_LastModifiedDate  DATETIME,
                P_CreatedBy         VARCHAR(100),
                P_ModifiedBy        VARCHAR(100),

                INDEX idx_pesel (P_Pesel),
                INDEX idx_nazwisko (P_Nazwisko),
                INDEX idx_active (P_Active)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: Firma (Firmy/Kontrahenci)
        // ══════════════════════════════════════════════════════════════════════════
        ("Firma", """
            CREATE TABLE IF NOT EXISTS `Firma` (
                F_ID            INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                F_Nazwa         VARCHAR(255) NOT NULL,
                F_NIP           VARCHAR(20),
                F_REGON         VARCHAR(20),
                F_KRS           VARCHAR(20),
                F_Adres_Ulica   VARCHAR(255),
                F_Adres_Miasto  VARCHAR(255),
                F_Adres_Kod     VARCHAR(10),
                F_Telefon       VARCHAR(50),
                F_Email         VARCHAR(255),
                F_WWW           VARCHAR(255),
                F_Osoba_Kontakt VARCHAR(255),
                F_Comments      TEXT,
                F_Active        TINYINT(1) DEFAULT 1,
                F_Data_Zalozenia DATETIME,
                F_RegistrationDate DATETIME,

                INDEX idx_nip (F_NIP),
                INDEX idx_nazwa (F_Nazwa),
                INDEX idx_active (F_Active)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: Umowy_Firm (Umowy z firmami)
        // ══════════════════════════════════════════════════════════════════════════
        ("Umowy_Firm", """
            CREATE TABLE IF NOT EXISTS `Umowy_Firm` (
                U_ID            INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                U_Firma_ID      INT,
                U_Nr_Umowy      VARCHAR(100),
                U_Data_Zawarcia DATETIME,
                U_Data_Od       DATETIME,
                U_Data_Do       DATETIME,
                U_Rodzaj_Umowy  VARCHAR(255),
                U_Cena_Jednostkowa DECIMAL(18,4),
                U_Waluta        VARCHAR(10),
                U_Comments      TEXT,
                U_Active        TINYINT(1) DEFAULT 1,
                U_RegistrationDate DATETIME,

                INDEX idx_firma (U_Firma_ID),
                INDEX idx_active (U_Active),
                FOREIGN KEY (U_Firma_ID) REFERENCES Firma(F_ID) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: BAD_Lista (Lista badań)
        // ══════════════════════════════════════════════════════════════════════════
        ("BAD_Lista", """
            CREATE TABLE IF NOT EXISTS `BAD_Lista` (
                BL_ID           INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                BL_Nazwa        VARCHAR(255) NOT NULL,
                BL_Kod          VARCHAR(50),
                BL_Opis         TEXT,
                BL_Kategoria    VARCHAR(100),
                BL_Cena_Bazowa  DECIMAL(18,4),
                BL_Active       TINYINT(1) DEFAULT 1,
                BL_RegistrationDate DATETIME,

                INDEX idx_kod (BL_Kod),
                INDEX idx_nazwa (BL_Nazwa),
                INDEX idx_active (BL_Active)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: BAD_Cennik (Cennik badań)
        // ══════════════════════════════════════════════════════════════════════════
        ("BAD_Cennik", """
            CREATE TABLE IF NOT EXISTS `BAD_Cennik` (
                BC_ID           INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                BC_Badanie_ID   INT,
                BC_Firma_ID     INT,
                BC_Cena         DECIMAL(18,4),
                BC_Data_Od      DATETIME,
                BC_Data_Do      DATETIME,
                BC_Active       TINYINT(1) DEFAULT 1,
                BC_RegistrationDate DATETIME,

                INDEX idx_badanie (BC_Badanie_ID),
                INDEX idx_firma (BC_Firma_ID),
                INDEX idx_active (BC_Active),
                FOREIGN KEY (BC_Badanie_ID) REFERENCES BAD_Lista(BL_ID) ON DELETE CASCADE,
                FOREIGN KEY (BC_Firma_ID) REFERENCES Firma(F_ID) ON DELETE CASCADE
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
                FOREIGN KEY (B_Firma_ID) REFERENCES Firma(F_ID) ON DELETE SET NULL,
                FOREIGN KEY (B_Badanie_ID) REFERENCES BAD_Lista(BL_ID) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: Badanie (Wyniki badań)
        // ══════════════════════════════════════════════════════════════════════════
        ("Badanie", """
            CREATE TABLE IF NOT EXISTS `Badanie` (
                Bad_ID          INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                Bad_Pacjent_ID  INT,
                Bad_Skierowanie_ID INT,
                Bad_Data_Wyk    DATETIME,
                Bad_Lekarz      VARCHAR(255),
                Bad_Wynik       TEXT,
                Bad_Rozpoznanie TEXT,
                Bad_Comments    TEXT,
                Bad_Status      VARCHAR(50),
                Bad_RegistrationDate DATETIME,

                INDEX idx_pacjent (Bad_Pacjent_ID),
                INDEX idx_skierowanie (Bad_Skierowanie_ID),
                INDEX idx_data (Bad_Data_Wyk),
                FOREIGN KEY (Bad_Pacjent_ID) REFERENCES P_Pacjent(P_ID) ON DELETE CASCADE,
                FOREIGN KEY (Bad_Skierowanie_ID) REFERENCES B_Skierowania(B_ID) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: Faktura (Faktury)
        // ══════════════════════════════════════════════════════════════════════════
        ("Faktura", """
            CREATE TABLE IF NOT EXISTS `Faktura` (
                Fak_ID          INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                Fak_Nr          VARCHAR(100),
                Fak_Firma_ID    INT,
                Fak_Data_Wyst   DATETIME,
                Fak_Data_Sprzed DATETIME,
                Fak_Termin_Plat DATETIME,
                Fak_Kwota_Netto DECIMAL(18,4),
                Fak_Kwota_VAT   DECIMAL(18,4),
                Fak_Kwota_Brutto DECIMAL(18,4),
                Fak_Waluta      VARCHAR(10),
                Fak_Status      VARCHAR(50),
                Fak_Comments    TEXT,
                Fak_RegistrationDate DATETIME,

                INDEX idx_nr (Fak_Nr),
                INDEX idx_firma (Fak_Firma_ID),
                INDEX idx_data (Fak_Data_Wyst),
                INDEX idx_status (Fak_Status),
                FOREIGN KEY (Fak_Firma_ID) REFERENCES Firma(F_ID) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: Rejestracja (Wizyty/Rejestracja)
        // ══════════════════════════════════════════════════════════════════════════
        ("Rejestracja", """
            CREATE TABLE IF NOT EXISTS `Rejestracja` (
                R_ID            INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                R_Pacjent_ID    INT,
                R_Data_Wizyty   DATETIME,
                R_Godzina_Od    VARCHAR(10),
                R_Godzina_Do    VARCHAR(10),
                R_Lekarz        VARCHAR(255),
                R_Gabinet       VARCHAR(100),
                R_Typ_Wizyty    VARCHAR(100),
                R_Status        VARCHAR(50),
                R_Comments      TEXT,
                R_RegistrationDate DATETIME,

                INDEX idx_pacjent (R_Pacjent_ID),
                INDEX idx_data (R_Data_Wizyty),
                INDEX idx_lekarz (R_Lekarz),
                INDEX idx_status (R_Status),
                FOREIGN KEY (R_Pacjent_ID) REFERENCES P_Pacjent(P_ID) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: Users (Użytkownicy systemu)
        // ══════════════════════════════════════════════════════════════════════════
        ("Users", """
            CREATE TABLE IF NOT EXISTS `Users` (
                U_ID            INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                U_Login         VARCHAR(100) NOT NULL UNIQUE,
                U_Password      VARCHAR(255),
                U_Imie          VARCHAR(255),
                U_Nazwisko      VARCHAR(255),
                U_Email         VARCHAR(255),
                U_Rola          VARCHAR(50),
                U_Active        TINYINT(1) DEFAULT 1,
                U_LastLogin     DATETIME,
                U_RegistrationDate DATETIME,

                INDEX idx_login (U_Login),
                INDEX idx_active (U_Active)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: LoginHistory (Historia logowań)
        // ══════════════════════════════════════════════════════════════════════════
        ("LoginHistory", """
            CREATE TABLE IF NOT EXISTS `LoginHistory` (
                LH_ID           INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                LH_User_ID      INT,
                LH_LoginTime    DATETIME,
                LH_LogoutTime   DATETIME,
                LH_IP           VARCHAR(50),
                LH_ComputerName VARCHAR(255),
                LH_Success      TINYINT(1),

                INDEX idx_user (LH_User_ID),
                INDEX idx_time (LH_LoginTime),
                FOREIGN KEY (LH_User_ID) REFERENCES Users(U_ID) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELE SŁOWNIKOWE (S_*)
        // ══════════════════════════════════════════════════════════════════════════
        ("S_Imiona", """
            CREATE TABLE IF NOT EXISTS `S_Imiona` (
                SI_ID   INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                SI_Imie VARCHAR(255) NOT NULL,
                SI_Plec VARCHAR(1)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        ("S_Nazwisko", """
            CREATE TABLE IF NOT EXISTS `S_Nazwisko` (
                SN_ID       INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                SN_Nazwisko VARCHAR(255) NOT NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        ("S__Ulice", """
            CREATE TABLE IF NOT EXISTS `S__Ulice` (
                SU_ID       INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                SU_Miasto   VARCHAR(255),
                SU_Ulica    VARCHAR(255),
                SU_Kod      VARCHAR(10)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        ("Gminy", """
            CREATE TABLE IF NOT EXISTS `Gminy` (
                G_ID            INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                G_Wojewodztwo   VARCHAR(100),
                G_Powiat        VARCHAR(100),
                G_Gmina         VARCHAR(100),
                G_Typ           VARCHAR(50)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        ("FormatowanieTekstu", """
            CREATE TABLE IF NOT EXISTS `FormatowanieTekstu` (
                FT_ID       INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                FT_Nazwa    VARCHAR(255),
                FT_Wartosc  TEXT
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        ("S_hints", """
            CREATE TABLE IF NOT EXISTS `S_hints` (
                SH_ID       INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                SH_Kategoria VARCHAR(100),
                SH_Klucz    VARCHAR(255),
                SH_Wartosc  TEXT
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
                FOREIGN KEY (L_Firma_ID) REFERENCES Firma(F_ID) ON DELETE SET NULL
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
