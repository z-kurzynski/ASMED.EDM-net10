# ASMED.EDM - Modele Domenowe - ETAP 1 (część 3)

## ✅ Zrealizowano

### 1. **Struktura folderów**
```
ASMED.EDM.Core/
├── Entities/          # Klasy reprezentujące tabele bazy danych
├── ValueObjects/      # Obiekty wartości (gotowe do użycia)
└── Enums/             # Wyliczenia (statusy, typy)
```

### 2. **Klasa bazowa dla encji**
**Plik**: `BaseEntity.cs`

Wspólne properties dla wszystkich encji:
- `Id` - klucz główny
- `CreatedAt`, `CreatedById` - audyt tworzenia
- `ModifiedAt`, `ModifiedById` - audyt modyfikacji
- `IsDeleted`, `DeletedAt`, `DeletedById` - soft delete
- `RowVersion` - optimistic concurrency control

### 3. **Enumy systemowe**

#### **VisitStatus** - status wizyty
- `Scheduled` - zaplanowana
- `CheckedIn` - pacjent przyszedł
- `InProgress` - w trakcie
- `Completed` - zakończona
- `Cancelled` - odwołana
- `NoShow` - nie pojawił się

#### **Gender** - płeć
- `Unspecified`, `Male`, `Female`, `Other`

#### **UserRole** - rola użytkownika
- `Administrator`, `Doctor`, `Nurse`, `Receptionist`, `ReadOnly`

### 4. **Encje domenowe**

#### **Patient** (Pacjent)
Właściwości:
- Dane osobowe: imię, nazwisko, PESEL, data urodzenia, płeć
- Kontakt: telefon, email, adres, miasto, kod pocztowy
- Medyczne: grupa krwi, alergie, przewlekłe choroby, notatki
- Kontakt awaryjny: imię, telefon
- Navigation: `ICollection<Visit> Visits`

#### **User** (Użytkownik systemu)
Właściwości:
- Dane logowania: username, passwordHash
- Dane osobowe: imię, nazwisko, email, telefon
- Bezpieczeństwo: rola, aktywny, last login, failed attempts, lockout
- Reset hasła: token, expiry
- Navigation: `Doctor?`, `ICollection<Visit> CreatedVisits`
- Computed: `FullName`

#### **Doctor** (Lekarz)
Właściwości:
- Powiązanie: `UserId` → `User`
- Licencja: PWZ (numer prawa wykonywania zawodu)
- Kwalifikacje: specjalizacja, tytuł naukowy, dodatkowe certyfikaty
- Praktyka: czy przyjmuje nowych, stawka za wizytę
- Navigation: `User`, `ICollection<Visit> Visits`
- Computed: `FullNameWithTitle`

#### **Visit** (Wizyta)
Właściwości:
- Powiązania: `PatientId`, `DoctorId`
- Harmonogram: `ScheduledDateTime`, `ActualStartTime`, `ActualEndTime`, `DurationMinutes`
- Status: `VisitStatus`, typ wizyty
- Medyczne: powód, objawy, diagnoza, leczenie, zalecenia, notatki lekarza
- Finansowe: koszt, czy opłacona, metoda płatności, data płatności
- Follow-up: czy potrzebna kolejna wizyta, sugerowana data
- Administracyjne: powód odwołania, notatki recepcji
- Navigation: `Patient`, `Doctor`

#### **DoctorSchedule** (Grafik lekarza)
Właściwości:
- Powiązanie: `DoctorId`
- Dzień tygodnia: `DayOfWeek` (System.DayOfWeek)
- Godziny: `StartTime`, `EndTime` (TimeSpan)
- Dostępność: `IsAvailable`
- Lokalizacja, notatki
- Navigation: `Doctor`

#### **MedicalRecord** (Dokumentacja medyczna)
Właściwości:
- Powiązania: `PatientId`, `VisitId?` (opcjonalne)
- Metadata: data, typ dokumentu, tytuł
- Treść: content, attachment path
- ICD-10: kod rozpoznania
- Navigation: `Patient`, `Visit?`

#### **Prescription** (Recepta)
Właściwości:
- Powiązania: `VisitId`, `PatientId`, `DoctorId`
- Data: wystawienia, wygaśnięcia
- Lek: nazwa, dawkowanie, częstotliwość, czas kuracji, ilość opakowań
- Refundacja: czy refundowane, procent
- Realizacja: czy zrealizowana, data, nazwa apteki
- Instrukcje
- Navigation: `Visit`, `Patient`, `Doctor`

#### **AuditLog** (Log audytowy)
Właściwości:
- Kto: `UserId`, `Username`
- Co: `OperationType`, `EntityName`, `EntityId`
- Kiedy: `Timestamp`
- Zmiany: `OldValues` (JSON), `NewValues` (JSON)
- Kontekst: IP address, User Agent
- Wynik: `IsSuccess`, `ErrorMessage`

### 5. **AsmedDbContext - kompletna konfiguracja**

**DbSets**:
```csharp
public DbSet<Patient> Patients => Set<Patient>();
public DbSet<Visit> Visits => Set<Visit>();
public DbSet<User> Users => Set<User>();
public DbSet<Doctor> Doctors => Set<Doctor>();
public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
public DbSet<Prescription> Prescriptions => Set<Prescription>();
public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
```

**Global Query Filters** (soft delete):
- Wszystkie encje z `IsDeleted` mają automatyczny filter `.HasQueryFilter(e => !e.IsDeleted)`

**Fluent API Konfiguracja**:
- ✅ **Patient**: Unique index na `IdentificationNumber`, index na `Email`, composite index na `LastName+FirstName`
- ✅ **User**: Unique index na `Username` i `Email`, powiązanie One-to-One z `Doctor`
- ✅ **Doctor**: Unique index na `MedicalLicenseNumber` i `UserId`
- ✅ **Visit**: Foreign keys + indexes na `PatientId`, `DoctorId`, `ScheduledDateTime`, `Status`
- ✅ **Prescription**: Foreign keys do `Visit`, `Patient`, `Doctor`, indexes
- ✅ **MedicalRecord**: Foreign keys z optional `Visit`, indexes
- ✅ **DoctorSchedule**: Foreign key + composite index `DoctorId+DayOfWeek`
- ✅ **AuditLog**: Indexes na `Timestamp`, `UserId`, `EntityName+EntityId`

**Delete Behaviors**:
- `Restrict` - relacje główne (Patient-Visit, Doctor-Visit, User-Doctor)
- `SetNull` - opcjonalne relacje (MedicalRecord.VisitId)
- `Cascade` - zależności (DoctorSchedule od Doctor)

### 6. **AsmedDbContextFactory**
**Plik**: `AsmedDbContextFactory.cs`

Factory dla EF Core Design-Time Tools (migracje, scaffolding):
- Implementuje `IDesignTimeDbContextFactory<AsmedDbContext>`
- Używa hardcoded connection string dla migracji
- Uproszczona konfiguracja (bez retry policy) dla szybszego działania tools

---

## ⚠️ ZNANY PROBLEM: Migracje EF Core

### Problem
```
Unable to create a 'DbContext' of type 'AsmedDbContext'. 
The exception 'Method not found: System.String 
Microsoft.EntityFrameworkCore.Diagnostics.AbstractionsStrings.ArgumentIsEmpty(System.Object)'
```

### Przyczyna
Konflikt wersji między:
- **Pomelo.EntityFrameworkCore.MySql 9.0.0** (najnowsza dostępna)
- **Microsoft.EntityFrameworkCore 10.0.10** (projekt NET 10)

Pomelo 9.0 wymaga `EntityFrameworkCore.Relational >= 9.0.0 && <= 9.0.999`, ale projekt ma wersję 10.0.10.

### Rozwiązania

**Opcja 1: Czekać na Pomelo 10.x** ✅ ZALECANE
- Pomelo.EntityFrameworkCore.MySql wersja 10.x jeszcze nie istnieje
- Prawdopodobnie pojawi się w najbliższych miesiącach
- Wtedy wystarczy: `dotnet add package Pomelo.EntityFrameworkCore.MySql --version 10.*`

**Opcja 2: Downgrade EF Core do 9.0**
- Zmienić wszystkie pakiety `Microsoft.EntityFrameworkCore.*` na wersję `9.0.*`
- Projekt musiałby wrócić do NET 8 (`net8.0`)
- ❌ NIE ZALECANE - tracimy NET 10

**Opcja 3: Użyć MySQL Connector/NET** (oficjalny provider Oracle)
- Zainstalować `MySql.EntityFrameworkCore` zamiast Pomelo
- ⚠️ Ma mniejsze wsparcie społeczności niż Pomelo

**Opcja 4: Tymczasowo stworzyć migracje w projekcie NET 9**
- Utworzyć pomocniczy projekt Data.Migrations w NET 9
- Wygenerować tam migracje
- Skopiować pliki migracji do głównego projektu NET 10
- ⚠️ Workaround, ale działający

### Co jest gotowe mimo braku migracji?

✅ **Wszystkie modele są kompletne i działają**
✅ **DbContext jest skonfigurowany**
✅ **Connection management działa**
✅ **Aplikacja kompiluje się**
✅ **Testy połączenia z MySQL działają**

❌ **Brakuje tylko utworzenia tabel w bazie** (nie ma plików migracji)

### Jak dokończyć gdy Pomelo 10 będzie dostępne

1. Zaktualizować Pomelo:
   ```bash
   cd D:\Visual\Asmed_EDM\src\ASMED.EDM.Data
   dotnet add package Pomelo.EntityFrameworkCore.MySql --version 10.*
   ```

2. Utworzyć migrację:
   ```bash
   dotnet ef migrations add InitialCreate
   ```

3. Zastosować do bazy:
   ```bash
   dotnet ef database update
   ```

4. Gotowe! 🎉

---

## 📊 Podsumowanie statystyk

**Encje**: 8 (Patient, User, Doctor, Visit, DoctorSchedule, MedicalRecord, Prescription, AuditLog)  
**Enumy**: 3 (VisitStatus, Gender, UserRole)  
**Value Objects**: 0 (gotowe do dodania w przyszłości)  
**Relationships**:
- One-to-One: User ↔ Doctor
- One-to-Many: Patient → Visits, Doctor → Visits, Visit → Prescriptions, Patient → MedicalRecords
- Many-to-One: Visit → Patient, Visit → Doctor, Prescription → Visit/Patient/Doctor

**Indeksy**: 15+ (uniczne, pojedyncze, composite)  
**Soft Delete**: Tak (wszystkie encje oprócz AuditLog)  
**Concurrency Control**: Tak (RowVersion na wszystkich encjach)  
**Audyt**: Pełny (Created/Modified/Deleted z userId i timestamp)

---

## 🎯 Następne kroki

### Repository Pattern (Data)
- [ ] `IRepository<T>` - generyczny interfejs
- [ ] `Repository<T>` - implementacja bazowa
- [ ] `IPatientRepository`, `IVisitRepository` - interfejsy specjalistyczne
- [ ] `IUnitOfWork` - transakcje i SaveChanges

### Seedy danych testowych
- [ ] Użytkownicy (admin, lekarz, recepcjonista)
- [ ] Lekarze (2-3 przykładowych)
- [ ] Pacjenci (10-20 przykładowych)
- [ ] Wizyty (historyczne i przyszłe)

### ViewModels i Services (UI)
- [ ] `PatientListViewModel`
- [ ] `VisitScheduleViewModel`
- [ ] `IPatientService`, `IVisitService`

### Okna WPF
- [ ] Patient List
- [ ] Patient Details/Edit
- [ ] Visit Schedule/Calendar
- [ ] Visit Details

### Registry Configuration Service
- [ ] Odczyt/zapis connection strings z Registry
- [ ] Edytor ustawień bazy danych w UI

---

**Data utworzenia**: 21.07.2026  
**Status**: ✅ **Modele domenowe w 100% gotowe**  
**Blokada**: ⚠️ Migracje EF Core zablokowane przez brak Pomelo 10.x (tymczasowe)
