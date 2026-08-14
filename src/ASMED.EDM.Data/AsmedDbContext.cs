using ASMED.EDM.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ASMED.EDM.Data;

/// <summary>
/// Główny kontekst bazy danych ASMED.EDM
/// </summary>
public class AsmedDbContext : DbContext
{
    public AsmedDbContext(DbContextOptions<AsmedDbContext> options) 
        : base(options)
    {
    }

    // DbSets dla encji domenowych
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<EdmDocument> EdmDocuments => Set<EdmDocument>();
    public DbSet<P1SyncLog> P1SyncLogs => Set<P1SyncLog>();
    public DbSet<AppTab> AppTabs => Set<AppTab>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // ─── Moduł fakturowania ───────────────────────────────────────────────────
    public DbSet<Firma> Firmy => Set<Firma>();
    public DbSet<Cennik> Cenniki => Set<Cennik>();
    public DbSet<Faktura> Faktury => Set<Faktura>();
    public DbSet<ListaBadan> ListyBadan => Set<ListaBadan>();
    public DbSet<Badanie> Badania => Set<Badanie>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Konfiguracja globalnych query filters (soft delete)
        // UWAGA: !e.IsDeleted wyklucza NULL w MySQL. Używamy != true żeby NULL (migrowane rekordy) były widoczne.
        modelBuilder.Entity<Patient>().HasQueryFilter(e => e.IsDeleted != true);
        modelBuilder.Entity<Visit>().HasQueryFilter(e => e.IsDeleted != true);
        modelBuilder.Entity<User>().HasQueryFilter(e => e.IsDeleted != true);
        modelBuilder.Entity<Doctor>().HasQueryFilter(e => e.IsDeleted != true);
        modelBuilder.Entity<DoctorSchedule>().HasQueryFilter(e => e.IsDeleted != true);
        modelBuilder.Entity<MedicalRecord>().HasQueryFilter(e => e.IsDeleted != true);
        modelBuilder.Entity<Prescription>().HasQueryFilter(e => e.IsDeleted != true);
        modelBuilder.Entity<EdmDocument>().HasQueryFilter(e => e.IsDeleted != true);
        // Firma.IsDeleted = kolumna Del
        modelBuilder.Entity<Firma>().HasQueryFilter(e => e.IsDeleted != true);
        // Faktura, ListaBadan, Badanie nie mają kolumny IsDeleted — brak query filter
        // modelBuilder.Entity<Faktura>().HasQueryFilter(...);
        // modelBuilder.Entity<ListaBadan>().HasQueryFilter(...);
        // modelBuilder.Entity<Badanie>().HasQueryFilter(...);

        // Konfiguracja relacji i indeksów
        ConfigurePatient(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureLoginHistory(modelBuilder);
        ConfigureDoctor(modelBuilder);
        ConfigureVisit(modelBuilder);
        ConfigurePrescription(modelBuilder);
        ConfigureMedicalRecord(modelBuilder);
        ConfigureDoctorSchedule(modelBuilder);
        ConfigureAuditLog(modelBuilder);
        ConfigureEdmDocument(modelBuilder);
        ConfigureP1SyncLog(modelBuilder);
        ConfigureFirma(modelBuilder);
        ConfigureCennik(modelBuilder);
        ConfigureFaktura(modelBuilder);
        ConfigureListaBadan(modelBuilder);
        ConfigureBadanie(modelBuilder);
        ConfigureAppTab(modelBuilder);
        ConfigureRolePermission(modelBuilder);

        // Lub zastosuj konfiguracje z osobnych plików
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(AsmedDbContext).Assembly);
    }

    private void ConfigureAppTab(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppTab>(entity =>
        {
            entity.ToTable("AppTab");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ParentKey).HasMaxLength(100);

            entity.HasIndex(e => e.Key).IsUnique();
        });
    }

    private void ConfigureRolePermission(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermission");
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.AppTab)
                .WithMany(t => t.RolePermissions)
                .HasForeignKey(e => e.AppTabId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.Role, e.AppTabId }).IsUnique();
        });
    }

    private void ConfigureEdmDocument(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EdmDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DocumentOid).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentHash).IsRequired().HasMaxLength(64);
            entity.Property(e => e.P1IndexId).HasMaxLength(255);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => e.DocumentOid).IsUnique();
            entity.HasIndex(e => e.PatientId);

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Visit)
                .WithMany()
                .HasForeignKey(e => e.VisitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Doctor)
                .WithMany()
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureFirma(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Firma>(entity =>
        {
            entity.ToTable("Firma");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Activ).HasColumnName("activ");
            entity.Property(e => e.IsDeleted).HasColumnName("Del");
            entity.Property(e => e.Nazwa).HasColumnName("Nazwa").IsRequired().HasMaxLength(255);
            entity.Property(e => e.NIP).HasColumnName("NIP").HasMaxLength(255);
            entity.Property(e => e.NazwaCennika).HasColumnName("Cennik").HasMaxLength(255);
            entity.Property(e => e.FkEmail).HasColumnName("FKemail").HasMaxLength(255);
            entity.Property(e => e.Kod).HasColumnName("Kod").HasMaxLength(255);
            entity.Property(e => e.Miejscowosc).HasColumnName("Miejscowosc").HasMaxLength(100);
            entity.Property(e => e.Ulica).HasColumnName("Ulica").HasMaxLength(255);
            entity.Property(e => e.UmowaDo)
                .HasColumnName("umowa_do")
                .HasConversion(
                    v => v.HasValue ? v.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
                    v => string.IsNullOrWhiteSpace(v) ? (DateTime?)null : DateTime.Parse(v));
            entity.Property(e => e.CzasNieokreslon).HasColumnName("czas_nieokreslon");
            entity.Property(e => e.Osoba_kontaktowa).HasColumnName("Osoba_kontaktowa").HasMaxLength(255);
            entity.Property(e => e.Telefon).HasColumnName("Telefon").HasMaxLength(255);
            entity.Ignore(e => e.RowVersion);
            entity.Ignore(e => e.CreatedAt);
            entity.Ignore(e => e.CreatedById);
            entity.Ignore(e => e.ModifiedAt);
            entity.Ignore(e => e.ModifiedById);
            entity.Ignore(e => e.DeletedAt);
            entity.Ignore(e => e.DeletedById);

            entity.HasIndex(e => e.Nazwa);
            entity.HasIndex(e => e.NIP);
        });
    }

    private void ConfigureCennik(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cennik>(entity =>
        {
            entity.ToTable("BAD_Cennik");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Identyfikator");
            entity.Property(e => e.IsActiv).HasColumnName("b_activ");
            entity.Property(e => e.Nazwa).HasColumnName("b_Nazwa").IsRequired().HasMaxLength(255);
            entity.Property(e => e.CenaPodstawowa).HasColumnName("b_Cena").HasColumnType("decimal(18,4)");
            // Cennik Access ma tylko jedną kolumnę ceny b_Cena i b_Cennik (nazwa cennika)
            // pozostałe ceny są w Badanie.Bad_Cena1-10, nie w BAD_Cennik
            entity.Ignore(e => e.CenaLaryngolog);
            entity.Ignore(e => e.CenaOkulista);
            entity.Ignore(e => e.CenaKsiazeczka);
            entity.Ignore(e => e.CenaLipidogram);
            entity.Ignore(e => e.CenaEKG);
            entity.Ignore(e => e.CenaPoradnia);
            entity.Ignore(e => e.CenaInne);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.RowVersion);

            entity.HasIndex(e => e.Nazwa);
        });
    }

    private void ConfigureFaktura(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Faktura>(entity =>
        {
            entity.ToTable("Faktura");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("FK_ID");
            entity.Property(e => e.FirmaId).HasColumnName("FK_Firma_ID");
            entity.Property(e => e.ListaBadanId).HasColumnName("FK_Num_Listy");
            entity.Property(e => e.NumerFaktury).HasColumnName("FK_Numer").HasMaxLength(30);
            entity.Property(e => e.DataWystawienia).HasColumnName("FK_Data");
            entity.Property(e => e.KwotaBrutto).HasColumnName("FK_Kwota").HasColumnType("decimal(18,4)");
            entity.Property(e => e.SumaBadan).HasColumnName("FK_Suma_Bad").HasColumnType("decimal(18,4)");
            entity.Property(e => e.Saldo).HasColumnName("FK_Saldo").HasColumnType("decimal(18,4)");
            entity.Property(e => e.Uwagi).HasColumnName("FK_Uwagi").HasMaxLength(255);
            entity.Property(e => e.NazwaCennika).HasColumnName("FK_Cennik").HasMaxLength(255);
            entity.Property(e => e.Status).HasColumnName("FK_Status");
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.RowVersion);
            entity.Ignore(e => e.PdfPath);
            entity.Ignore(e => e.CreatedAt);
            entity.Ignore(e => e.CreatedById);
            entity.Ignore(e => e.ModifiedAt);
            entity.Ignore(e => e.ModifiedById);
            entity.Ignore(e => e.DeletedAt);
            entity.Ignore(e => e.DeletedById);

            entity.HasIndex(e => e.NumerFaktury);
            entity.HasIndex(e => e.FirmaId);
            entity.HasIndex(e => e.DataWystawienia);

            entity.HasOne(e => e.Firma)
                .WithMany(f => f.Faktury)
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ListaBadan)
                .WithOne(l => l.Faktura)
                .HasForeignKey<Faktura>(e => e.ListaBadanId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureListaBadan(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ListaBadan>(entity =>
        {
            entity.ToTable("ListyBadan");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Identyfikator");
            entity.Property(e => e.FirmaId).HasColumnName("L_Firma_ID");
            entity.Property(e => e.FakturaId).HasColumnName("L_FK_ID");
            entity.Property(e => e.DataUtworzenia).HasColumnName("L_Data");
            entity.Property(e => e.Uwagi).HasColumnName("L_Uwagi").HasMaxLength(255);
            entity.Property(e => e.NumerListy).HasColumnName("L_Nazwa").HasMaxLength(255);
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.RowVersion);
            entity.Ignore(e => e.SumaRazem); // brak tej kolumny w ListyBadan
            entity.Ignore(e => e.CreatedAt);
            entity.Ignore(e => e.CreatedById);
            entity.Ignore(e => e.ModifiedAt);
            entity.Ignore(e => e.ModifiedById);
            entity.Ignore(e => e.DeletedAt);
            entity.Ignore(e => e.DeletedById);

            entity.HasIndex(e => e.FirmaId);
            entity.HasIndex(e => e.DataUtworzenia);

            entity.HasOne(e => e.Firma)
                .WithMany(f => f.ListyBadan)
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureBadanie(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Badanie>(entity =>
        {
            entity.ToTable("Badanie");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Bad_ID");
            entity.Property(e => e.PatientId).HasColumnName("Bad_P_ID");
            entity.Property(e => e.ListaBadanId).HasColumnName("Bad_L_ID");
            entity.Property(e => e.FakturaId).HasColumnName("Bad_Fakt_ID");
            entity.Property(e => e.TypBadania).HasColumnName("Bad_Typ").HasMaxLength(50);
            entity.Property(e => e.DataBadania).HasColumnName("Bad_Data");
            entity.Property(e => e.DataWaznosci).HasColumnName("Bad_Data_Do");
            entity.Property(e => e.Wynik).HasColumnName("Bad_Wynik").HasMaxLength(100);
            entity.Property(e => e.NrKsiegi).HasColumnName("Bad_Nr_KS").HasMaxLength(255);
            entity.Property(e => e.IsZakonczone).HasColumnName("Bad_END");
            entity.Property(e => e.NazwaCennika).HasColumnName("Bad_bn_cennik").HasMaxLength(255);
            entity.Property(e => e.Razem).HasColumnName("Bad_Razem").HasColumnType("decimal(18,4)");
            entity.Property(e => e.Cena1Podstawowa).HasColumnName("Bad_Cena1").HasColumnType("decimal(18,4)");
            entity.Property(e => e.Cena2Laryngolog).HasColumnName("Bad_Cena2").HasColumnType("decimal(18,4)");
            entity.Property(e => e.Cena3Okulista).HasColumnName("Bad_Cena3").HasColumnType("decimal(18,4)");
            entity.Property(e => e.Cena4Ksiazeczka).HasColumnName("Bad_Cena4").HasColumnType("decimal(18,4)");
            entity.Property(e => e.Cena5Lipidogram).HasColumnName("Bad_Cena5").HasColumnType("decimal(18,4)");
            entity.Property(e => e.Cena6EKG).HasColumnName("Bad_Cena6").HasColumnType("decimal(18,4)");
            entity.Property(e => e.Cena7Poradnia).HasColumnName("Bad_Cena7").HasColumnType("decimal(18,4)");
            entity.Property(e => e.Cena8Inne).HasColumnName("Bad_Cena8").HasColumnType("decimal(18,4)");
            entity.Property(e => e.Cena9).HasColumnName("Bad_Cena9").HasColumnType("decimal(18,4)");
            entity.Property(e => e.Cena10).HasColumnName("Bad_Cena10").HasColumnType("decimal(18,4)");
            // brak IsDeleted/RowVersion/audyt w tabeli Badanie
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.RowVersion);
            entity.Ignore(e => e.CreatedAt);
            entity.Ignore(e => e.CreatedById);
            entity.Ignore(e => e.ModifiedAt);
            entity.Ignore(e => e.ModifiedById);
            entity.Ignore(e => e.DeletedAt);
            entity.Ignore(e => e.DeletedById);

            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.ListaBadanId);
            entity.HasIndex(e => e.FakturaId);
            entity.HasIndex(e => e.DataBadania);

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ListaBadan)
                .WithMany(l => l.Badania)
                .HasForeignKey(e => e.ListaBadanId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Faktura)
                .WithMany()
                .HasForeignKey(e => e.FakturaId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigureP1SyncLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<P1SyncLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OperationType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.P1Identifier).HasMaxLength(255);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => new { e.EntityName, e.EntityId });
            entity.HasIndex(e => e.Status);
        });
    }

    private void ConfigurePatient(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("P_Pacjent");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("P_ID");
            entity.Property(e => e.FirstName).HasColumnName("P_imie").IsRequired().HasMaxLength(255);
            entity.Property(e => e.LastName).HasColumnName("P_nazwisko").IsRequired().HasMaxLength(255);
            entity.Property(e => e.IdentificationNumber).HasColumnName("P_pesel").HasMaxLength(11);
            entity.Property(e => e.PhoneNumber).HasColumnName("P_telefon").HasMaxLength(255);
            entity.Property(e => e.Email).HasColumnName("P_email").HasMaxLength(255);
            entity.Property(e => e.PostalCode).HasColumnName("P_Ades_kod").HasMaxLength(255);
            entity.Property(e => e.FirmaId).HasColumnName("P_Firma_id");
            // brak IsDeleted / RowVersion w P_Pacjent — ignoruj
            entity.Ignore(e => e.IsDeleted);
            entity.Ignore(e => e.RowVersion);

            entity.HasIndex(e => e.IdentificationNumber);
            entity.HasIndex(e => e.FirmaId);
            entity.HasIndex(e => new { e.LastName, e.FirstName });

            entity.HasOne(e => e.Firma)
                .WithMany()
                .HasForeignKey(e => e.FirmaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Role)
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.Doctor)
                .WithOne(d => d.User)
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureLoginHistory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoginHistory>(entity =>
        {
            entity.ToTable("LoginHistory");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).HasMaxLength(255);
            entity.Property(e => e.ComputerName).HasMaxLength(255);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.FailureReason).HasMaxLength(255);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.LoginTime);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureDoctor(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MedicalLicenseNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Specialization).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(50);
            entity.Property(e => e.ConsultationFee).HasColumnType("decimal(18,2)");
            entity.Property(e => e.RowVersion).IsRowVersion();
            entity.Property(e => e.TotpSecretKeyEncrypted).HasMaxLength(500);
            entity.Property(e => e.DoctorCertificatePassword).HasMaxLength(255);

            entity.HasIndex(e => e.MedicalLicenseNumber).IsUnique();
            entity.HasIndex(e => e.UserId).IsUnique();
        });
    }

    private void ConfigureVisit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Visit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VisitType).HasMaxLength(100);
            entity.Property(e => e.VisitCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasOne(e => e.Patient)
                .WithMany(p => p.Visits)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Doctor)
                .WithMany(d => d.Visits)
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.DoctorId);
            entity.HasIndex(e => e.ScheduledDateTime);
            entity.HasIndex(e => e.Status);
        });
    }

    private void ConfigurePrescription(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MedicationName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Dosage).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Frequency).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ReimbursementPercentage).HasColumnType("decimal(5,2)");
            entity.Property(e => e.PharmacyName).HasMaxLength(200);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasOne(e => e.Visit)
                .WithMany()
                .HasForeignKey(e => e.VisitId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Doctor)
                .WithMany()
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.VisitId);
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.PrescriptionDate);
        });
    }

    private void ConfigureMedicalRecord(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RecordType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.IcdCode).HasMaxLength(20);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasOne(e => e.Patient)
                .WithMany()
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Visit)
                .WithMany()
                .HasForeignKey(e => e.VisitId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.VisitId);
            entity.HasIndex(e => e.RecordDate);
        });
    }

    private void ConfigureDoctorSchedule(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DoctorSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasOne(e => e.Doctor)
                .WithMany()
                .HasForeignKey(e => e.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.DoctorId, e.DayOfWeek });
        });
    }

    private void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OperationType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EntityName).HasMaxLength(100);
            entity.Property(e => e.EntityId).HasMaxLength(50);
            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.IpAddress).HasMaxLength(50);

            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.EntityName, e.EntityId });
        });
    }
}
