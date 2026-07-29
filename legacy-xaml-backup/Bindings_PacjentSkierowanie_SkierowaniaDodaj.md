# Binding Fields in XAML

## PacjentSkierowanieView.xaml

- PatientFirstName -  P_Pacjent.P_imie as string
- PatientLastName -  P_Pacjent.P_nazwisko as string
- PatientPesel -  P_Pacjent.P_pesel as string
- PatientGender -  P_Pacjent.P_pleæ as string
- PatientBirthDate -  P_Pacjent.P_data_urodzenia as date
- PatientJobTitle -  P_Pacjent.P_zawód as string 
- PatientPostalCode  -  P_Pacjent.P_kod_pocztowy as string
- PatientCity -  P_Pacjent.P_miasto as string
- PatientStreet - _Pacjent.P_Adres_ulica_numer as string
- PatientId -  P_Pacjent.P_id as INT
- Uwagi -  P_Pacjent.P_uwagi as string
- CompanyId -  P_Pacjent.P_Firma_id as INT
- CompanyName  - Firma.Nazwa as string
- CompanyPostalCode - Firma.Kod as string
- CompanyCity -  Firma.Miejscowosc as string
- CompanyStreet -  Firma.Ulica as string

## SkierowaniaPacjentDodajView.xaml

- RegistrationDate
- IsGroupW - nie ma w bazie
- IsGroupO - nie ma w bazie
- IsGroupK - nie ma w bazie
- TestType - B_Skierowania.B_TypBadania as string
- JobTitle - B_Skierowania.B_Stanowisko as string
- IsPhysical - B_Skierowania.B_czynnik_fizyczny as boolean
- PhysicalDescription - B_Skierowania.B_czynnik_fizyczny_opis as string
- IsDust - B_Skierowania.B_czynnik_py³owy as boolean
- DustDescription - B_Skierowania.B_czynnik_py³owy_opis as string
- IsChemical - B_Skierowania.B_czynnik_chemiczny as boolean
- ChemicalDescription - B_Skierowania.B_czynnik_chemiczny_opis as string
- IsBiological - B_Skierowania.B_czynnik_biologiczny as boolean
- BiologicalDescription - B_Skierowania.B_czynnik_biologiczny_opis as string
- IsOther - B_Skierowania.B_czynnik_inny as boolean
- OtherDescription - B_Skierowania.B_czynnik_inny_opis as string
- IsSanepid - B_Skierowania.B_czynnik_sanepid as boolean
- SanepidDescription - B_Skierowania.B_czynnik_sanepid_opis as string
- IsbookletSanepid - B_Skierowania.B_ksi¹¿eczka_sanepid as boolean
- IsCertificate - B_Skierowania.B_zaœwiadczenie as boolean

---

Dopisz poni¿ej nazwê tabeli i odpowiadaj¹ce jej pola w SQL, aby przygotowaæ mapowanie.
## Tabela: P_Pacjent
- P_imie as string
- P_nazwisko as string
- P_pesel as string
- P_pleæ as string
- P_data_urodzenia as date
- P_zawód as string
- P_kod_pocztowy as string
- P_miasto as string
- P_Adres_ulica_numer as string
- P_id as INT
- P_uwagi as string
- P_Firma_id as INT
- Firma.Nazwa as string
- Firma.Kod as string
- Firma.Miejscowosc as string
- Firma.Ulica as string

## Tabela: B_Skierowania
- B_TypBadania as string
- B_Stanowisko as string
- B_czynnik_fizyczny as boolean
- B_czynnik_fizyczny_opis as string
- B_czynnik_py³owy as boolean
- B_czynnik_py³owy_opis as string
- B_czynnik_chemiczny as boolean
- B_czynnik_chemiczny_opis as string
- B_czynnik_biologiczny as boolean
- B_czynnik_biologiczny_opis as string
- B_czynnik_inny as boolean
- B_czynnik_inny_opis as string
- B_czynnik_sanepid as boolean
- B_czynnik_sanepid_opis as string
- B_ksi¹¿eczka_sanepid as boolean
- B_zaœwiadczenie as boolean
- RegistrationDate - as date


table B-Badania