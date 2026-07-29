# Plan Integracji z Systemem P1 – Orzeczenia Lekarskie Medycyny Pracy

> **Podstawa prawna:** Ustawa o medycynie pracy (nowelizacja obowiązująca od **17 kwietnia 2026**)
> **Cel:** Elektroniczne przesyłanie orzeczeń lekarskich do systemu P1 (e-Zdrowie) przez platformę **Gabinet.gov.pl**

---

## 1. Kontekst prawny

Od **17 kwietnia 2026** lekarze medycyny pracy są zobowiązani do:
- Elektronicznego wystawiania orzeczeń lekarskich
- Przesyłania orzeczeń do systemu **P1** jako **Zdarzenia Medyczne (ZM)**
- Wysłania orzeczenia **w dniu jego wystawienia**
- Udostępnienia orzeczenia pacjentowi przez **Internetowe Konto Pacjenta (IKP)**

---

## 2. Architektura integracji

```
Aplikacja ASMED (WPF .NET 8)
         │
         │  OAuth2 (client_id / client_secret)
         ▼
  Gabinet.gov.pl API (REST/JSON)
         │
         │  HL7 FHIR / Zdarzenie Medyczne
         ▼
  System P1 – Centrum e-Zdrowia (CeZ)
         │
         ▼
  IKP – Internetowe Konto Pacjenta
```

---

## 3. Wymagania techniczne

### 3.1 Autoryzacja
| Parametr | Opis |
|----------|------|
| `client_id` | Identyfikator aplikacji z portalu Gabinet.gov.pl |
| `client_secret` | Klucz tajny aplikacji |
| `grant_type` | `client_credentials` |
| `scope` | `zdarzenia-medyczne:write skierowania:read` |
| Token | **Bearer JWT** (ważność: 3600s) |
| Środowisko INT | `https://int.gabinet.gov.pl/oauth/token` |
| Środowisko PROD | `https://api.gabinet.gov.pl/oauth/token` |

### 3.2 Endpoint wysyłania orzeczenia
```
POST https://api.gabinet.gov.pl/zdarzenia-medyczne/orzeczenia
Authorization: Bearer {token}
Content-Type: application/json
```

---

## 4. Struktura plików do utworzenia

```
Services/
└── P1/
    ├── P1Settings.cs                  ← konfiguracja (URL, client_id, secret, env)
    ├── GabinetAuthService.cs          ← OAuth2 – pobieranie i odświeżanie tokenu
    ├── OrzeczenieP1Service.cs         ← wysyłanie ZM po zatwierdzeniu badania
    ├── SkierowaniaP1Service.cs        ← (przyszłość) odczyt e-Skierowań
    └── Models/
        ├── TokenResponse.cs           ← model odpowiedzi OAuth2
        ├── OrzeczenieZMDto.cs         ← struktura Zdarzenia Medycznego
        ├── PacjentP1Dto.cs            ← dane pacjenta dla P1
        ├── WynikOrzeczenieDto.cs      ← wynik: zdolny / niezdolny / warunkowo zdolny
        └── P1ApiResponse.cs           ← model odpowiedzi z API
```

---

## 5. Model danych orzeczenia (OrzeczenieZMDto)

Pola wymagane przez P1 dla orzeczenia medycyny pracy:

| Pole | Źródło w ASMED | Opis |
|------|---------------|------|
| `pesel` | `P_pesel` | PESEL pacjenta |
| `imie` | `P_imie` | Imię pacjenta |
| `nazwisko` | `P_nazwisko` | Nazwisko pacjenta |
| `dataUrodzenia` | obliczona z PESEL | Data urodzenia |
| `dataOrzeczenia` | `Bad_Data` | Data wystawienia |
| `dataWaznosci` | `Bad_Data_Do` | Data ważności orzeczenia |
| `wynikOrzeczenia` | `Bad_Wynik` | Zdolny / Niezdolny / Warunkowo zdolny |
| `nrKsiegiGlownej` | `Bad_Nr_KS` | Nr w księdze głównej |
| `nipPracodawcy` | `Firma_NIP` | NIP firmy kierującej |
| `nazwaPracodawcy` | `Firma_Nazwa` | Nazwa firmy |
| `stanowiskoPracy` | `P_zawod` | Stanowisko pracownika |
| `rodzajBadania` | `B_TypBadania` | Wstępne / Okresowe / Kontrolne |
| `npwzLekarza` | konfiguracja | Nr prawa wykonywania zawodu |
| `idPlacowki` | konfiguracja | ID placówki w systemie P1 |

---

## 6. Mapowanie wyników orzeczenia

| Wartość w ASMED (`Bad_Wynik`) | Kod P1 | Opis |
|-------------------------------|--------|------|
| `Zdolny` | `ZDOLNY` | Brak przeciwwskazań |
| `Niezdolny` | `NIEZDOLNY` | Istnieją przeciwwskazania |
| `Warunkowo zdolny` | `WARUNKOWO_ZDOLNY` | Zdolny z ograniczeniami |

---

## 7. Punkt integracji w aplikacji

Integracja zostanie podpięta w `BadaniaEditNewViewModel.cs` przy zatwierdzaniu badania:

```
[Przycisk: Zatwierdź badanie]
        │
        ▼
BadaniaEditNewViewModel.ZatwierdzBadanieCommand
        │
        ├── Zapis do lokalnej bazy (AccessDbContext) ← już istnieje
        │
        └── OrzeczenieP1Service.WyslijOrzeczenieAsync(BadanieRecordEdit)
                    │
                    ├── Pobierz token (GabinetAuthService)
                    ├── Zbuduj OrzeczenieZMDto
                    ├── POST → api.gabinet.gov.pl
                    └── Zapisz status wysyłki w bazie (Bad_P1_Status)
```

---

## 8. Obsługa błędów i statusy

| Status | Opis | Akcja |
|--------|------|-------|
| `WYSŁANO` | Orzeczenie przyjęte przez P1 | Zielony znacznik w UI |
| `BŁĄD_API` | Problem z połączeniem / token | Retry + powiadomienie |
| `BŁĄD_WALIDACJI` | Brakujące/błędne dane | Komunikat dla użytkownika |
| `OCZEKUJE` | Kolejka wysyłki (offline) | Wysyłka przy następnym połączeniu |
| `NIE_WYSŁANO` | Wysyłka wyłączona w konfiguracji | Brak akcji |

---

## 9. Zmiany w bazie danych

Tabela `Badania` – nowe kolumny do dodania:

```sql
ALTER TABLE Badania ADD Bad_P1_Status     NVARCHAR(20)  NULL; -- status wysyłki
ALTER TABLE Badania ADD Bad_P1_ID         NVARCHAR(100) NULL; -- ID orzeczenia w P1
ALTER TABLE Badania ADD Bad_P1_DataWysylki DATETIME     NULL; -- data wysyłki
ALTER TABLE Badania ADD Bad_P1_Komunikat  NVARCHAR(500) NULL; -- komunikat błędu
```

---

## 10. Konfiguracja (appsettings / szyfrowany plik lokalny)

```json
{
  "P1": {
    "Enabled": false,
    "Environment": "INT",
    "ClientId": "",
    "ClientSecret": "",
    "NpwzLekarza": "",
    "IdPlacowki": "",
    "UrlInt": "https://int.gabinet.gov.pl",
    "UrlProd": "https://api.gabinet.gov.pl"
  }
}
```

---

## 11. Harmonogram implementacji

| Etap | Zakres | Status |
|------|--------|--------|
| **1** | Uzyskanie `client_id` / `client_secret` z Gabinet.gov.pl | ⏳ W toku |
| **2** | `P1Settings.cs` + `GabinetAuthService.cs` (OAuth2) | 🔲 Do zrobienia |
| **3** | `OrzeczenieZMDto.cs` + `OrzeczenieP1Service.cs` | 🔲 Do zrobienia |
| **4** | Podpięcie do `BadaniaEditNewViewModel` | 🔲 Do zrobienia |
| **5** | Zmiany w schemacie bazy danych | 🔲 Do zrobienia |
| **6** | Testy na środowisku **INT** | 🔲 Do zrobienia |
| **7** | Testy akceptacyjne z CeZ | 🔲 Do zrobienia |
| **8** | Wdrożenie na środowisko **PROD** | 🔲 Do zrobienia |

---

## 12. Notatki

- Przed wdrożeniem PROD wymagane **testy akceptacyjne** zatwierdzone przez CeZ
- Certyfikat TLS może być wymagany dla środowiska PROD
- Lekarz musi mieć aktywne konto w Gabinet.gov.pl z numerem NPWZ
- Orzeczenia wysłane do P1 są **nieodwołalne** – wymagana walidacja przed wysyłką
- Dokumentacja API: [https://api.gabinet.gov.pl/docs](https://api.gabinet.gov.pl/docs)
- Kontakt CeZ: [https://ezdrowie.gov.pl/kontakt](https://ezdrowie.gov.pl/kontakt)
