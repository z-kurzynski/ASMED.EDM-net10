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
    public DbSet<EdmDocument> EdmDocuments => Set<EdmDocument>();
    public DbSet<P1SyncLog> P1SyncLogs => Set<P1SyncLog>();

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
        modelBuilder.Entity<Patient>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Visit>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Doctor>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<DoctorSchedule>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<MedicalRecord>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Prescription>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<EdmDocument>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Firma>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Faktura>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ListaBadan>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Badanie>().HasQueryFilter(e => !e.IsDeleted);

        // Konfiguracja relacji i indeksów
        ConfigurePatient(modelBuilder);
        ConfigureUser(modelBuilder);
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

        // Lub zastosuj konfiguracje z osobnych plików
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(AsmedDbContext).Assembly);
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
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nazwa).IsRequired().HasMaxLength(200);
            entity.Property(e => e.NIP).HasMaxLength(20);
            entity.Property(e => e.NazwaCennika).HasMaxLength(100);
            entity.Property(e => e.FkEmail).HasMaxLength(255);
            entity.Property(e => e.Kod).HasMaxLength(10);
            entity.Property(e => e.Miejscowosc).HasMaxLength(100);
            entity.Property(e => e.Ulica).HasMaxLength(200);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => e.Nazwa);
            entity.HasIndex(e => e.NIP);
        });
    }

    private void ConfigureCennik(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cennik>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nazwa).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CenaPodstawowa).HasColumnType("decimal(10,2)");
            entity.Property(e => e.CenaLaryngolog).HasColumnType("decimal(10,2)");
            entity.Property(e => e.CenaOkulista).HasColumnType("decimal(10,2)");
            entity.Property(e => e.CenaKsiazeczka).HasColumnType("decimal(10,2)");
            entity.Property(e => e.CenaLipidogram).HasColumnType("decimal(10,2)");
            entity.Property(e => e.CenaEKG).HasColumnType("decimal(10,2)");
            entity.Property(e => e.CenaPoradnia).HasColumnType("decimal(10,2)");
            entity.Property(e => e.CenaInne).HasColumnType("decimal(10,2)");

            entity.HasIndex(e => e.Nazwa).IsUnique();
        });
    }

    private void ConfigureFaktura(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Faktura>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NumerFaktury).HasMaxLength(50);
            entity.Property(e => e.KwotaBrutto).HasColumnType("decimal(12,2)");
            entity.Property(e => e.SumaBadan).HasColumnType("decimal(12,2)");
            entity.Property(e => e.Saldo).HasColumnType("decimal(12,2)");
            entity.Property(e => e.Uwagi).HasMaxLength(500);
            entity.Property(e => e.NazwaCennika).HasMaxLength(100);
            entity.Property(e => e.PdfPath).HasMaxLength(500);
            entity.Property(e => e.RowVersion).IsRowVersion();

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
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NumerListy).HasMaxLength(50);
            entity.Property(e => e.SumaRazem).HasColumnType("decimal(12,2)");
            entity.Property(e => e.Uwagi).HasMaxLength(500);
            entity.Property(e => e.RowVersion).IsRowVersion();

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
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TypBadania).HasMaxLength(100);
            entity.Property(e => e.Wynik).HasMaxLength(100);
            entity.Property(e => e.NrKsiegi).HasMaxLength(50);
            entity.Property(e => e.NazwaCennika).HasMaxLength(100);
            entity.Property(e => e.Razem).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Cena1Podstawowa).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Cena2Laryngolog).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Cena3Okulista).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Cena4Ksiazeczka).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Cena5Lipidogram).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Cena6EKG).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Cena7Poradnia).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Cena8Inne).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Cena9).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Cena10).HasColumnType("decimal(10,2)");
            entity.Property(e => e.RowVersion).IsRowVersion();

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
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IdentificationNumber).HasMaxLength(20);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.PostalCode).HasMaxLength(10);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.BloodType).HasMaxLength(5);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => e.IdentificationNumber).IsUnique();
            entity.HasIndex(e => e.Email);
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
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(e => e.Doctor)
                .WithOne(d => d.User)
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);
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
