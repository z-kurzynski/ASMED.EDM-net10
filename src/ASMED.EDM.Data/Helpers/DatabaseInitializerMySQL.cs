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
                P_CWUB_ID           VARCHAR(255),
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
                B_BadanieWstepne    TINYINT(1),
                B_BadanieOkresowe   TINYINT(1),
                B_BadanieKontrolne  TINYINT(1),
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
                B_Urlop             TINYINT(1),
                B_Ankieta           TINYINT(1),
                B_Activ             TINYINT(1),
                B_wywiad            TINYINT(1),
                B_orzeczenie        TINYINT(1),
                B_laryngolog        TINYINT(1),
                B_okulista          TINYINT(1),
                B_EKG               TINYINT(1),
                B_Lipidogram        TINYINT(1),
                B_orzeczenie_ID     INT,
                B_uwagi             TEXT,
                B_EDM               TINYINT(1),
                B_edm_document_id   INT,

                INDEX idx_pacjent (B_Pacjent_ID),
                INDEX idx_firma (B_Firma_ID),
                INDEX idx_badanie (B_Badanie_ID),
                INDEX idx_faktura (B_Faktura_ID),
                INDEX idx_data_skierowania (B_DataSkierowania),
                FOREIGN KEY (B_Pacjent_ID) REFERENCES P_Pacjent(P_ID) ON DELETE CASCADE,
                FOREIGN KEY (B_Firma_ID) REFERENCES Firma(id) ON DELETE SET NULL,
                FOREIGN KEY (B_Badanie_ID) REFERENCES Badanie(Bad_ID) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ═══════════════════════════════════════════════════════════════════
        // TABELA: Badanie_Laryngologiczne (Badanie laryngologiczne — 1:1 ze skierowaniem)
        // ═══════════════════════════════════════════════════════════════════
        ("Badanie_Laryngologiczne", """
            CREATE TABLE IF NOT EXISTS `Badanie_Laryngologiczne` (
                BL_ID                           INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                BL_B_ID                         INT NOT NULL,
                BL_P_ID                         INT,
                BL_Data                         DATETIME,
                BL_Status_Wykonano              TINYINT(1) DEFAULT 1,
                BL_VHI_Wypelniona               TINYINT(1) DEFAULT 1,
                BL_VHI_Opis                     VARCHAR(100),
                BL_Krtan_Prawidlowa             TINYINT(1) DEFAULT 1,
                BL_Nosogardlo_Prawidlowe        TINYINT(1) DEFAULT 1,
                BL_Sluch_Prawidlowy             TINYINT(1) DEFAULT 1,
                BL_BlonyBebenkowe_Prawidlowe    TINYINT(1) DEFAULT 1,
                BL_Rownowaga_Prawidlowa         TINYINT(1) DEFAULT 1,
                BL_PrzeciwwskazaniaGlos         TINYINT(1) DEFAULT 0,
                BL_WynikOgolny_Prawidlowy       TINYINT(1) DEFAULT 1,
                BL_Uwagi                        TEXT,
                BL_CreatedAt                    DATETIME,
                BL_UpdatedAt                    DATETIME,

                UNIQUE INDEX idx_BL_B_ID (BL_B_ID),
                INDEX idx_BL_P_ID (BL_P_ID),
                FOREIGN KEY (BL_B_ID) REFERENCES B_Skierowania(B_ID) ON DELETE CASCADE,
                FOREIGN KEY (BL_P_ID) REFERENCES P_Pacjent(P_ID) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ═══════════════════════════════════════════════════════════════════
        // TABELA: Badanie_Okulistyczne (Badanie okulistyczne — 1:1 ze skierowaniem)
        // ═══════════════════════════════════════════════════════════════════
        ("Badanie_Okulistyczne", """
            CREATE TABLE IF NOT EXISTS `Badanie_Okulistyczne` (
                BO_ID                           INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                BO_B_ID                         INT NOT NULL,
                BO_P_ID                         INT,
                BO_Data                         DATETIME,
                BO_Status_Wykonano              TINYINT(1) DEFAULT 1,
                BO_WidzenieBarw_Prawidlowe      TINYINT(1) DEFAULT 1,
                BO_PoleWidzenia_Prawidlowe      TINYINT(1) DEFAULT 1,
                BO_WidzenieObuoczne_Prawidlowe  TINYINT(1) DEFAULT 1,
                BO_WynikOgolny_Prawidlowy       TINYINT(1) DEFAULT 1,
                BO_PrzeciwwskazaniaMonitor      TINYINT(1) DEFAULT 0,
                BO_UzywaOkularow                TINYINT(1) DEFAULT 0,
                BO_Uwagi                        TEXT,
                BO_CreatedAt                    DATETIME,
                BO_UpdatedAt                    DATETIME,

                UNIQUE INDEX idx_BO_B_ID (BO_B_ID),
                INDEX idx_BO_P_ID (BO_P_ID),
                FOREIGN KEY (BO_B_ID) REFERENCES B_Skierowania(B_ID) ON DELETE CASCADE,
                FOREIGN KEY (BO_P_ID) REFERENCES P_Pacjent(P_ID) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ═══════════════════════════════════════════════════════════════════
        // TABELA: Badanie_Sanitarne (Badanie sanitarno-epidemiologiczne — 1:1 ze skierowaniem)
        // ═══════════════════════════════════════════════════════════════════
        ("Badanie_Sanitarne", """
            CREATE TABLE IF NOT EXISTS `Badanie_Sanitarne` (
                BS_ID                           INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                BS_B_ID                         INT NOT NULL,
                BS_P_ID                         INT,
                BS_Data                         DATETIME,
                BS_Wynik                        VARCHAR(30) DEFAULT 'Pozytywny',
                BS_Uwagi                        TEXT,
                BS_CreatedAt                    DATETIME,
                BS_UpdatedAt                    DATETIME,

                UNIQUE INDEX idx_BS_B_ID (BS_B_ID),
                INDEX idx_BS_P_ID (BS_P_ID),
                FOREIGN KEY (BS_B_ID) REFERENCES B_Skierowania(B_ID) ON DELETE CASCADE,
                FOREIGN KEY (BS_P_ID) REFERENCES P_Pacjent(P_ID) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ═══════════════════════════════════════════════════════════════════
        // TABELA: Badanie_Urlop (Urlop dla poratowania zdrowia — 1:1 ze skierowaniem)
        // ═══════════════════════════════════════════════════════════════════
        ("Badanie_Urlop", """
            CREATE TABLE IF NOT EXISTS `Badanie_Urlop` (
                BU_ID                           INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                BU_B_ID                         INT NOT NULL,
                BU_P_ID                         INT,
                BU_DataOd                       DATETIME,
                BU_DataDo                       DATETIME,
                BU_Uzasadnienie                 TEXT,
                BU_CreatedAt                    DATETIME,
                BU_UpdatedAt                    DATETIME,

                UNIQUE INDEX idx_BU_B_ID (BU_B_ID),
                INDEX idx_BU_P_ID (BU_P_ID),
                FOREIGN KEY (BU_B_ID) REFERENCES B_Skierowania(B_ID) ON DELETE CASCADE,
                FOREIGN KEY (BU_P_ID) REFERENCES P_Pacjent(P_ID) ON DELETE SET NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: Badanie (Wyniki/Wycena badań)(status badań, wyniki, ceny, faktury)
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
                Id                          INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                Username                    VARCHAR(50) NOT NULL,
                PasswordHash                VARCHAR(255) NOT NULL,
                FirstName                   VARCHAR(100) NOT NULL DEFAULT '',
                LastName                    VARCHAR(100) NOT NULL DEFAULT '',
                Email                       VARCHAR(255) NULL,
                PhoneNumber                 VARCHAR(20),
                Role                        VARCHAR(50) NOT NULL DEFAULT 'Receptionist',
                IsActive                    TINYINT(1) NOT NULL DEFAULT 1,
                LastLoginAt                 DATETIME NULL,
                LastFailedLoginAt           DATETIME NULL,
                FailedLoginAttempts         INT NOT NULL DEFAULT 0,
                IsLocked                    TINYINT(1) NOT NULL DEFAULT 0,
                LockedUntil                 DATETIME NULL,
                LockedOutUntil              DATETIME NULL,
                PasswordResetToken          VARCHAR(255) NULL,
                PasswordResetTokenExpiry    DATETIME NULL,

                CreatedAt                   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                CreatedById                 INT NULL,
                ModifiedAt                  DATETIME NULL,
                ModifiedById                INT NULL,
                IsDeleted                   TINYINT(1) NOT NULL DEFAULT 0,
                DeletedAt                   DATETIME NULL,
                DeletedById                 INT NULL,
                RowVersion                  TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),

                UNIQUE INDEX idx_Username (Username),
                UNIQUE INDEX idx_Email (Email)
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
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: EdmDocuments (lokalne repozytorium dokumentów EDM — HL7 CDA)
        // ══════════════════════════════════════════════════════════════════════════
        ("EdmDocuments", """
            CREATE TABLE IF NOT EXISTS `EdmDocuments` (
                Id              INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                DocumentType    INT NOT NULL,
                PatientId       INT NOT NULL,
                VisitId         INT NULL,
                DoctorId        INT NOT NULL,
                DocumentOid     VARCHAR(255) NOT NULL,
                Title           VARCHAR(255) NOT NULL,
                FilePath        VARCHAR(500) NOT NULL,
                ContentHash     VARCHAR(64) NOT NULL,
                SignatureStatus INT NOT NULL DEFAULT 0,
                SignedAt        DATETIME NULL,
                P1IndexStatus   INT NOT NULL DEFAULT 0,
                P1IndexId       VARCHAR(255) NULL,
                CreatedAt       DATETIME NOT NULL,
                CreatedById     INT NULL,
                ModifiedAt      DATETIME NULL,
                ModifiedById    INT NULL,
                IsDeleted       TINYINT(1) NOT NULL DEFAULT 0,
                DeletedAt       DATETIME NULL,
                DeletedById     INT NULL,
                RowVersion      TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),

                UNIQUE INDEX idx_edm_oid (DocumentOid),
                INDEX idx_edm_patient (PatientId),
                INDEX idx_edm_visit (VisitId)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════
        // TABELA: P1SyncLogs (log synchronizacji z systemem P1)
        // ══════════════════════════════════════════════════════════════════════════
        ("P1SyncLogs", """
            CREATE TABLE IF NOT EXISTS `P1SyncLogs` (
                Id              INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                OperationType   VARCHAR(50) NOT NULL,
                EntityName      VARCHAR(50) NOT NULL,
                EntityId        INT NOT NULL,
                Status          INT NOT NULL,
                P1Identifier    VARCHAR(255) NULL,
                ErrorMessage    VARCHAR(2000) NULL,
                AttemptCount    INT NOT NULL DEFAULT 1,
                LastAttemptAt   DATETIME NOT NULL,
                CreatedAt       DATETIME NOT NULL,
                CreatedById     INT NULL,
                ModifiedAt      DATETIME NULL,
                ModifiedById    INT NULL,
                IsDeleted       TINYINT(1) NOT NULL DEFAULT 0,
                DeletedAt       DATETIME NULL,
                DeletedById     INT NULL,
                RowVersion      TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),

                INDEX idx_p1sync_entity (EntityName, EntityId),
                INDEX idx_p1sync_status (Status)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════════
        // Tabela Rejestr Orzeczeń (Rejestracja Orzeczeń Lekarskich) — nowa tabela wprowadzona w 2026
        // ══════════════════════════════════════════════════════════════════════════════
        ("RejestrOrzeczen", """
            CREATE TABLE IF NOT EXISTS `RejestrOrzeczen` (
                Id              INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                PacjentId       INT NOT NULL,
                FimaId          INT NOT NULL,
                OrzeczenieId    INT NOT NULL,
                RodzajOrzeczenia VARCHAR(255) NOT NULL,
                DataWystawienia DATETIME NOT NULL,
                DataWaznosci    DATETIME NOT NULL,
                Wystawil        VARCHAR(255) NOT NULL,
                Uwagi           TEXT,
                INDEX idx_rejestr_pacjent (PacjentId),
                INDEX idx_rejestr_orzeczenie (OrzeczenieId),
                FOREIGN KEY (PacjentId) REFERENCES P_Pacjent(P_ID) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════════════════════
        // Tabela druków medycznych (formularze, zaświadczenia, skierowania) — nowa tabela wprowadzona w 2026
        // ══════════════════════════════════════════════════════════════════════════════

        ("MedicalForms", """
            CREATE TABLE IF NOT EXISTS `MedicalForms` (
                Id              INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                FormName        VARCHAR(255) NOT NULL,
                FormType        VARCHAR(100) NOT NULL,
                FolderPath      VARCHAR(500) NOT NULL,
                Content         TEXT NOT NULL,
                CreatedAt       DATETIME NOT NULL,
                CreatedById     INT NULL,
                ModifiedAt      DATETIME NULL,
                ModifiedById    INT NULL,
                IsDeleted       TINYINT(1) NOT NULL DEFAULT 0,
                DeletedAt       DATETIME NULL,
                DeletedById     INT NULL,
                RowVersion      TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
                INDEX idx_medicalforms_name (FormName),
                INDEX idx_medicalforms_type (FormType)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════
        // TABELA: AppTab (Zakładki aplikacji - lista definiowana programistycznie)
        // ══════════════════════════════════════════════════════════════
        ("AppTab", """
            CREATE TABLE IF NOT EXISTS `AppTab` (
                Id              INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                `Key`           VARCHAR(100) NOT NULL,
                DisplayName     VARCHAR(200) NOT NULL,
                ParentKey       VARCHAR(100) NULL,
                SortOrder       INT NOT NULL DEFAULT 0,

                UNIQUE INDEX idx_apptab_key (`Key`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """),

        // ══════════════════════════════════════════════════════════════
        // TABELA: RolePermission (Przypisanie r\u00f3l do zak\u0142adek)
        // ══════════════════════════════════════════════════════════════
        ("RolePermission", """
            CREATE TABLE IF NOT EXISTS `RolePermission` (
                Id              INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
                Role            INT NOT NULL,
                AppTabId        INT NOT NULL,

                UNIQUE INDEX idx_roleperm_role_tab (Role, AppTabId),
                FOREIGN KEY (AppTabId) REFERENCES AppTab(Id) ON DELETE CASCADE
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
            """)

    ];
    // ══════════════════════════════════════════════════════════════════════════════
    // Seed Data (dane startowe)
    // ══════════════════════════════════════════════════════════════════════════════
    private static readonly (string Table, string Sql)[] SeedData =
    [
        ("Users", """
            INSERT INTO `Users` (Username, PasswordHash, FirstName, LastName, Email, Role, IsActive, CreatedAt)
            VALUES ('admin', TO_BASE64(UNHEX(SHA2('admin123', 256))), 'Administrator', 'Systemu', 'admin@asmed.pl', 'Administrator', 1, NOW())
            ON DUPLICATE KEY UPDATE Username = Username
            """),

        ("AppTab", """
            INSERT INTO `AppTab` (`Key`, DisplayName, ParentKey, SortOrder) VALUES
                ('Rejestracja', 'Rejestracja', NULL, 10),
                ('Pacjenci', 'Pacjenci', NULL, 20),
                ('BadKarty', 'Bad. Karty', NULL, 30),
                ('BadWywiad', 'Bad. Wywiad', NULL, 40),
                ('BadOrzeczenie', 'Bad. Orzeczenie', NULL, 50),
                ('BadZakonczenie', 'Bad. Zako\u0144czenie', NULL, 60),
                ('BadEdycja', 'Bad. Edycja', NULL, 70),
                ('ListaDoFaktur', 'Lista Do Faktur', NULL, 80),
                ('Ustawienia', 'Ustawienia', NULL, 90),
                ('Ustawienia.Konfiguracja', 'Konfiguracja', 'Ustawienia', 10),
                ('Ustawienia.Cenniki', 'Cenniki', 'Ustawienia', 20),
                ('Ustawienia.DanePlacowki', 'Dane Placówki', 'Ustawienia', 25),
                ('Ustawienia.Faktura', 'Faktura', 'Ustawienia', 30),
                ('Ustawienia.Firma', 'Firma', 'Ustawienia', 40),
                ('Ustawienia.Raporty', 'Raporty', 'Ustawienia', 50),
                ('Ustawienia.Migracja', 'Migracja', 'Ustawienia', 60),
                ('Ustawienia.EDM', 'EDM', 'Ustawienia', 70),
                ('Ustawienia.Uzytkownicy', 'U\u017cytkownicy', 'Ustawienia', 80),
                ('Ustawienia.RoleUprawnienia', 'Role i uprawnienia', 'Ustawienia', 90)
            ON DUPLICATE KEY UPDATE `Key` = `Key`
            """)
    ];

    // ══════════════════════════════════════════════════════════════════════════════
    // Migrations (dodawanie nowych kolumn do istniejących tabel)
    // ══════════════════════════════════════════════════════════════════════════════
    private static readonly (string Table, string Column, string TypeDef)[] Migrations =
    [
        // Integracja P1 (e-zdrowie) — status Zdarzenia Medycznego na wizycie
        ("Visits", "P1EventId", "VARCHAR(255) NULL"),
        ("Visits", "P1SyncStatus", "INT NOT NULL DEFAULT 0"),

        // Pacjenci — identyfikator w Rejestrze Pacjentów P1 (CWUB)
        ("P_Pacjent", "P_CWUB_ID", "VARCHAR(255) NULL"),

        // Doctors — autoryzacja podpisu lekarza (TOTP) i dane certyfikatu P1 per-lekarz
        ("Doctors", "TotpSecretKeyEncrypted", "VARCHAR(500) NULL"),
        ("Doctors", "TotpEnabled", "TINYINT(1) NOT NULL DEFAULT 0"),
        ("Doctors", "TotpEnrolledAt", "DATETIME NULL"),
        ("Doctors", "DoctorCertificatePassword", "VARCHAR(255) NULL"),

        // Users — dostosowanie do nowego modelu (BaseEntity + rozdzielone imię/nazwisko)
        // Stare kolumny (FullName, CreatedDate, LastLogin, EndLogin) pozostają w bazie
        // do czasu migracji danych (patrz DataMigrations), a następnie mogą zostać usunięte ręcznie.
        ("Users", "FirstName", "VARCHAR(100) NOT NULL DEFAULT ''"),
        ("Users", "LastName", "VARCHAR(100) NOT NULL DEFAULT ''"),
        ("Users", "PhoneNumber", "VARCHAR(20) NULL"),
        ("Users", "IsActive", "TINYINT(1) NOT NULL DEFAULT 1"),
        ("Users", "LastLoginAt", "DATETIME NULL"),
        ("Users", "LastFailedLoginAt", "DATETIME NULL"),
        ("Users", "FailedLoginAttempts", "INT NOT NULL DEFAULT 0"),
        ("Users", "IsLocked", "TINYINT(1) NOT NULL DEFAULT 0"),
        ("Users", "LockedUntil", "DATETIME NULL"),
        ("Users", "LockedOutUntil", "DATETIME NULL"),
        ("Users", "PasswordResetToken", "VARCHAR(255) NULL"),
        ("Users", "PasswordResetTokenExpiry", "DATETIME NULL"),
        ("Users", "CreatedAt", "DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP"),
        ("Users", "CreatedById", "INT NULL"),
        ("Users", "ModifiedAt", "DATETIME NULL"),
        ("Users", "ModifiedById", "INT NULL"),
        ("Users", "IsDeleted", "TINYINT(1) NOT NULL DEFAULT 0"),
        ("Users", "DeletedAt", "DATETIME NULL"),
        ("Users", "DeletedById", "INT NULL")
    ];

    // ══════════════════════════════════════════════════════════════════
    // DataMigrations (przepisywanie danych ze starych kolumn do nowych)
    // Uruchamiane po fazie ADD COLUMN, przed seedem. Idempotentne — używają
    // WHERE, które po pierwszym przebiegu nie znajdzie już nic do zrobienia.
    // ══════════════════════════════════════════════════════════════════
    private static readonly (string Description, string Sql)[] DataMigrations =
    [
        // Users.FullName ("Imię Nazwisko") -> FirstName / LastName
        ("Users.FullName -> FirstName/LastName", """
            UPDATE `Users`
            SET
                FirstName = CASE
                    WHEN FullName IS NULL OR FullName = '' THEN FirstName
                    WHEN LOCATE(' ', FullName) > 0 THEN SUBSTRING_INDEX(FullName, ' ', 1)
                    ELSE FullName
                END,
                LastName = CASE
                    WHEN FullName IS NULL OR FullName = '' THEN LastName
                    WHEN LOCATE(' ', FullName) > 0 THEN TRIM(SUBSTRING(FullName, LOCATE(' ', FullName) + 1))
                    ELSE LastName
                END
            WHERE (FirstName = '' OR FirstName IS NULL)
              AND (LastName = '' OR LastName IS NULL)
              AND FullName IS NOT NULL AND FullName <> ''
            """),

        // Users.CreatedDate -> CreatedAt
        ("Users.CreatedDate -> CreatedAt", """
            UPDATE `Users`
            SET CreatedAt = CreatedDate
            WHERE CreatedDate IS NOT NULL
              AND (CreatedAt IS NULL OR CreatedAt = '0000-00-00 00:00:00')
            """),

        // Users.LastLogin -> LastLoginAt
        ("Users.LastLogin -> LastLoginAt", """
            UPDATE `Users`
            SET LastLoginAt = LastLogin
            WHERE LastLogin IS NOT NULL AND LastLoginAt IS NULL
            """),

        // Users.Role — starsze wartości ("Admin") -> nowy enum ("Administrator")
        ("Users.Role legacy value normalization", """
            UPDATE `Users`
            SET Role = CASE Role
                WHEN 'Admin' THEN 'Administrator'
                WHEN 'Lekarz' THEN 'Doctor'
                WHEN 'Pielegniarka' THEN 'Nurse'
                WHEN 'Pielęgniarka' THEN 'Nurse'
                WHEN 'Recepcja' THEN 'Receptionist'
                ELSE Role
            END
            WHERE Role IN ('Admin', 'Lekarz', 'Pielegniarka', 'Pielęgniarka', 'Recepcja')
            """),

        // Users.Email - puste stringi zamieniamy na NULL, aby unikalny indeks nie traktował ich jako duplikatów
        ("Users.Email empty string -> NULL", """
            UPDATE `Users`
            SET Email = NULL
            WHERE Email = ''
            """),

        // Users.Email - kolumna nie może już wymuszać NOT NULL (email jest opcjonalny)
        ("Users.Email column made nullable", """
            ALTER TABLE `Users` MODIFY COLUMN Email VARCHAR(255) NULL
            """)
    ];

    // ══════════════════════════════════════════════════════════════════════════════════
    // Migracje FK (naprawa błędnych kluczy obcych z migracji Access→MySQL)
    // Każdy wpis: (NazwaConstraint, SQL do wykonania)
    // ══════════════════════════════════════════════════════════════════════════════════
    private static readonly (string ConstraintName, string[] Sqls)[] FkMigrations =
    [
        // B_Skierowania.B_Badanie_ID wskazywało na BAD_Lista — powinno wskazywać na Badanie
        ("B_Skierowania_ibfk_3_fixed", new[]
        {
            // Krok 1: usuń stary FK (ignoruj jeśli nie istnieje)
            @"ALTER TABLE `B_Skierowania` DROP FOREIGN KEY `B_Skierowania_ibfk_3`",
            // Krok 2: wyczyść wisące wartości (nie pasują do Badanie.Bad_ID)
            @"UPDATE `B_Skierowania` SET `B_Badanie_ID` = NULL
              WHERE `B_Badanie_ID` IS NOT NULL
                AND `B_Badanie_ID` NOT IN (SELECT `Bad_ID` FROM `Badanie`)",
            // Krok 3: dodaj poprawny FK
            @"ALTER TABLE `B_Skierowania`
              ADD CONSTRAINT `B_Skierowania_ibfk_3`
              FOREIGN KEY (`B_Badanie_ID`) REFERENCES `Badanie` (`Bad_ID`)
              ON DELETE SET NULL"
        })
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
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
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

            // Faza 2 — migracje (ADD COLUMN, jeśli kolumna jeszcze nie istnieje)
            foreach (var (table, column, typeDef) in Migrations)
            {
                try
                {
                    bool columnExists;
                    using (var checkCmd = new MySqlCommand(
                        "SELECT COUNT(*) FROM information_schema.columns " +
                        "WHERE table_schema = @schema AND table_name = @table AND column_name = @column", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@schema", conn.Database);
                        checkCmd.Parameters.AddWithValue("@table", table);
                        checkCmd.Parameters.AddWithValue("@column", column);
                        columnExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                    }

                    if (!columnExists)
                    {
                        using var cmd = new MySqlCommand(
                            $"ALTER TABLE `{table}` ADD COLUMN `{column}` {typeDef}", conn);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Migration {table}.{column}: {ex.Message}");
                }
            }

            // Faza 2b — migracja danych (przepisanie wartości ze starych kolumn do nowych)
            foreach (var (description, sql) in DataMigrations)
            {
                try
                {
                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"DataMigration '{description}': {ex.Message}");
                }
            }

            // Faza 3 — migracje FK (naprawa kluczy obcych)
            // Sprawdzamy po REFERENCED_TABLE_NAME — czy constraint wskazuje na złą tabelę
            foreach (var (constraintName, sqls) in FkMigrations)
            {
                // Marker "..._fixed" — szukamy jego obecności: jeśli już istnieje, skip
                bool alreadyFixed = false;
                try
                {
                    using var checkCmd = new MySqlCommand(
                        $"SELECT COUNT(*) FROM information_schema.REFERENTIAL_CONSTRAINTS " +
                        $"WHERE CONSTRAINT_SCHEMA = '{conn.Database}' " +
                        $"AND CONSTRAINT_NAME = '{constraintName}'", conn);
                    alreadyFixed = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                }
                catch { }

                if (!alreadyFixed)
                {
                    foreach (var sql in sqls)
                    {
                        try
                        {
                            using var cmd = new MySqlCommand(sql, conn);
                            cmd.ExecuteNonQuery();
                        }
                        catch { /* ignoruj — np. DROP gdy constraint nie istnieje */ }
                    }
                }
            }

            // Faza 4 — seed danych startowych
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
