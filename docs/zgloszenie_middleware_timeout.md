# Zgłoszenie do dostawcy API — timeout / pusta odpowiedź na `medycynapracy/pacjent/wyszukaj`

**Temat:** Błąd API — `POST /medycynapracy/pacjent/wyszukaj` nie zwraca odpowiedzi / zwraca pustą treść (status 200, `Content-Type: text/html`, `Content-Length: 0`)

**Środowisko:** `https://telsa.e-recepty.online/`
**Konto testowe:** `telsa`
**Data/czas testu:** 2026-09-11, ok. 20:02–20:13 UTC

---

## Opis problemu

Logowanie (`POST /login?username=...&password=...`) działa poprawnie — token JWT jest zwracany w ~0,6–0,7s.

Natomiast wywołanie `POST /medycynapracy/pacjent/wyszukaj` z tym samym, poprawnym tokenem albo:
- **nie odpowiada w rozsądnym czasie** (obserwowane oczekiwanie ~11 sekund), po czym
- zwraca **`HTTP 200 OK`** z nagłówkami `Content-Type: text/html; charset=UTF-8` oraz `Content-Length: 0` — czyli pustą treść zamiast oczekiwanego JSON-a.

Request jest wysyłany dokładnie zgodnie z przykładami z udostępnionej kolekcji Postman (`Medycyna pracy przykłady dla klienta.postman_collection.json`), w tym z użyciem minimalnego obiektu `author` (`mp_author_min`), tak jak w tej kolekcji dla operacji na pacjencie.

## Szczegóły żądania

```
POST https://telsa.e-recepty.online/medycynapracy/pacjent/wyszukaj
Authorization: Bearer <ważny token JWT z /login>
Accept: application/json
Content-Type: application/x-www-form-urlencoded

author={"code":"LEK","authorType":"1","authorTypeDetail":"1","pwz":"9296328","clinicKsr":"000000927609"}
pacjent={"identyfikator":"70032816894","imie":"Anna","nazwisko":"Kasina"}
```

## Odpowiedź serwera

```
HTTP/1.1 200 OK
Server: nginx
Cache-Control: no-cache, private
Vary: Authorization, User-Agent
Content-Type: text/html; charset=UTF-8
Content-Length: 0

(pusta treść)
```

Czas odpowiedzi: ~11 sekund (znacznie dłuższy niż dla `/login`, ~0,6s).

## Dodatkowe obserwacje

- Zjawisko jest **powtarzalne** — wystąpiło przy kilku niezależnych próbach, z różnymi tokenami (świeżo wygenerowanymi).
- Token jest ważny (świeżo pobrany, `expires_in: 3600`), więc nie jest to problem z autoryzacją.
- Dane `pwz` i `clinicKsr` w `author` odpowiadają danym testowym udostępnionym przez Państwa zespół (PWZ `9296328`, KSR `000000927609`).
- Wcześniej, przy innym zestawie danych testowych, otrzymaliśmy prawidłową odpowiedź walidacyjną: `{"isError":true,"pacjent":null,"messages":{"severity":"error","diagnostics":"Podmiot uprawniony do uwierzytelnienia musi posiadać aktywne konto w Systemie P1","location":null}}` — co sugeruje, że endpoint w pewnych warunkach poprawnie się komunikuje z systemem P1, ale w innych przypadkach (jak opisany powyżej) prawdopodobnie napotyka wewnętrzny błąd/timeout przy tej komunikacji, który nie jest poprawnie obsłużony (brak zwrócenia kodu błędu / treści JSON).

## Pytania do dostawcy

1. Czy konto testowe `telsa` z danymi `pwz=9296328` / `clinicKsr=000000927609` ma aktywne konto w systemie P1 wymagane do wykonania tej operacji?
2. Czy endpoint `medycynapracy/pacjent/wyszukaj` w środowisku testowym komunikuje się z zewnętrznym systemem P1/CEZ, i czy mogło dojść do timeoutu/awarii tego połączenia po Państwa stronie?
3. Czy możliwe jest, aby w przypadku takiego wewnętrznego błędu serwer zwracał poprawny kod błędu (np. 500/504) z treścią JSON zamiast `200 OK` z pustym `text/html`? Obecne zachowanie uniemożliwia odróżnienie sukcesu od błędu po stronie klienta.

Proszę o informację zwrotną lub potwierdzenie, czy jest to znany, tymczasowy problem środowiska testowego.
