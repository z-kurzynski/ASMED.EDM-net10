# Proces formalny podłączenia NZOZ do systemu P1 (e-zdrowie)

Dokument opisuje kroki formalno-administracyjne wymagane, aby placówka typu **NZOZ (niepubliczny zakład opieki zdrowotnej)** uzyskała dostęp do systemu P1 (Elektroniczna Platforma Gromadzenia, Analizy i Udostępniania Zasobów Cyfrowych o Zdarzeniach Medycznych) w zakresie:

- rejestracji **Zdarzeń Medycznych (ZM)** — wizyty w przychodni,
- **weryfikacji pacjenta po PESEL**,
- **indeksowania dokumentów EDM** (m.in. orzeczenie o zdolności do pracy).

## 1. Wymagania wstępne

| Wymaganie | Opis |
|---|---|
| Wpis do RPWDL | Placówka musi być zarejestrowana w Rejestrze Podmiotów Wykonujących Działalność Leczniczą (https://rpwdl.ezdrowie.gov.pl). Z wpisu wynikają identyfikatory: **numer księgi rejestrowej**, **REGON (14-znakowy)**, kody resortowe (cz. I, V, VII). |
| Konto administratora | Osoba reprezentująca podmiot musi posiadać konto na https://rpwdl.ezdrowie.gov.pl z uprawnieniami do składania wniosków (podpis: profil zaufany / podpis kwalifikowany). |
| Oprogramowanie gabinetowe | System usługodawcy (ASMED.EDM) komunikujący się z P1 przez API (FHIR R4 / usługi P1). |

## 2. Wniosek o dostęp do P1

1. Zalogować się do systemu RPWDL i przejść do sekcji **„Jak podłączyć się do systemu P1"** / wniosek o wydanie certyfikatów P1.
2. Wypełnić **wniosek o złożenie certyfikatów do P1** — wskazać:
   - dane podmiotu (numer księgi rejestrowej RPWDL, REGON),
   - administratora danych usługodawcy (imię, nazwisko, PESEL, e-mail).
3. Podpisać wniosek profilem zaufanym lub podpisem kwalifikowanym.
4. Po pozytywnej weryfikacji CeZ generuje dla podmiotu **identyfikator usługodawcy (OID)** w węźle `2.16.840.1.113883.3.4424.2.3.1` oraz umożliwia pobranie plików do generacji certyfikatów.

## 3. Certyfikaty WSS i TLS

P1 wymaga dwóch certyfikatów X.509 wystawianych przez Centrum e-Zdrowia (CeZ):

- **Certyfikat TLS** — uwierzytelnienie kanału transmisji (mutual TLS),
- **Certyfikat WSS** — podpisywanie komunikatów / autoryzacja systemu usługodawcy.

Procedura:

1. Po akceptacji wniosku pobrać z RPWDL **Generator plików CSR** (aplikacja CeZ) lub wygenerować CSR ręcznie (klucz RSA min. 2048 bit):

   ```powershell
   # przykład z użyciem OpenSSL — osobne pary kluczy dla TLS i WSS
   openssl req -new -newkey rsa:2048 -nodes -keyout wss.key -out wss.csr
   openssl req -new -newkey rsa:2048 -nodes -keyout tls.key -out tls.csr
   ```

2. Przesłać pliki CSR przez portal RPWDL (wniosek certyfikacyjny).
3. Pobrać wystawione certyfikaty (`.pem`/`.cer`) i zbudować magazyny **PKCS#12 (.p12/.pfx)** zawierające klucz prywatny + certyfikat:

   ```powershell
   openssl pkcs12 -export -in wss.cer -inkey wss.key -out wss.p12
   openssl pkcs12 -export -in tls.cer -inkey tls.key -out tls.p12
   ```

4. Pliki `.p12` umieścić w bezpiecznej lokalizacji wskazanej w konfiguracji aplikacji (`P1Settings:TlsCertificatePath`, `P1Settings:WssCertificatePath`). **Nie commitować do repozytorium.** Hasła przechowywać w bezpiecznym magazynie (np. Windows DPAPI / zmienne środowiskowe).

Certyfikaty ważne są ok. 2 lata — zaplanować proces odnowienia.

## 4. Środowisko integracyjne (INT)

Przed produkcją należy przeprowadzić testy na środowisku integracyjnym P1:

- Wniosek o dostęp do środowiska INT składa się analogicznie (odrębne certyfikaty testowe).
- Adresy usług INT i PROD różnią się — w aplikacji konfigurowane w `P1Settings:BaseUrl`.
- Dokumentacja techniczna API ZM (FHIR R4), przykłady komunikatów i reguły walidacyjne: https://ezdrowie.gov.pl (sekcja *Strefa dostawcy / Zdarzenia Medyczne*).
- Do czasu uzyskania certyfikatów aplikacja działa w trybie **stub** (`P1Settings:UseStub = true`).

## 5. Identyfikatory wymagane w komunikacji (konfiguracja `P1Settings`)

| Klucz | Opis | Przykładowy węzeł OID |
|---|---|---|
| `SubjectOid` | OID usługodawcy nadany przez P1 | `2.16.840.1.113883.3.4424.2.3.1.<id>` |
| `RpwdlBookNumber` | Numer księgi rejestrowej RPWDL | — |
| `Regon14` | 14-znakowy REGON zakładu leczniczego | — |
| `OrganizationalUnitCode` | Kod resortowy cz. V/VII jednostki/komórki | — |
| OID PESEL (stały) | Identyfikacja pacjenta po PESEL | `2.16.840.1.113883.3.4424.1.1.616` |
| OID NPWZ (stały) | Numer prawa wykonywania zawodu lekarza | `2.16.840.1.113883.3.4424.1.6.2` |

## 6. Zakres etapu 1 (niniejsza integracja)

1. **Zdarzenia Medyczne** — rejestracja/aktualizacja/anulowanie ZM (wizyta w przychodni) w P1 po zakończeniu wizyty.
2. **Weryfikacja pacjenta po PESEL** — wyszukanie zasobu `Patient` w P1 w celu potwierdzenia danych pacjenta.
3. **Orzeczenie o zdolności do pracy** — dokument HL7 CDA (PIK HL7 CDA), podpis elektroniczny (pieczęć placówki), zapis w lokalnym repozytorium EDM (baza + plik XML); indeksacja dokumentu w P1 przygotowana interfejsowo (etap 2).

## 7. Wymogi prawne (skrót)

- Rozporządzenie MZ w sprawie rodzajów, zakresu i wzorów dokumentacji medycznej oraz sposobu jej przetwarzania.
- Obowiązek raportowania Zdarzeń Medycznych do P1 (ustawa o systemie informacji w ochronie zdrowia, art. 56).
- EDM podpisywane: podpisem kwalifikowanym, profilem zaufanym, podpisem osobistym albo z wykorzystaniem sposobu potwierdzania pochodzenia oraz integralności danych dostępnego w systemie teleinformatycznym udostępnionym bezpłatnie przez ZUS. W etapie 1 stosujemy podpis certyfikatem placówki (pieczęć) — do weryfikacji z wymogami dla danego typu dokumentu przed produkcją.
- RODO: minimalizacja logowania danych (PESEL nie trafia do logów), audyt operacji przez `IAuditService`.
