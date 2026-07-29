# Podsumowanie projektu ASMED_3 (stan na dzi�)

## Og�lne informacje
- Projekt: **ASMED_3** (ASMED.WPF)
- Lokalizacja: `src/ASMED_3/`
- Typ: Aplikacja WPF (.NET 8)
- Wykorzystuje Syncfusion (licencja spo�eczno�ciowa)
- Baza danych: Access, obs�uga w folderze Helpers

**Cel aplikacji**: Zarządzanie danymi pacjentów oraz operacje na bazie Access, z naciskiem na modularność, dynamiczne widoki i uproszczone powiązania danych.
## Architektura
- **MVVM**: Model-View-ViewModel
- **Modularno��**: Widoki podzielone na zak��adki, dynamiczne zmiany w obr�bie zak��adek
- **Baza danych**: Uproszczona struktura, powi�zania przez ID
- **Kontrolki**: Syncfusion, iTextSharp, PdfiumViewer, Notifications.Wpf    

## Kontekst migracji
- Projekt jest modyfikacj� wcze�niejszego **ASMED_2_5** (WPF, .NET Framework)
- W ASMED_2_5 istnia�o wiele gotowych procedur i formularzy
- W ASMED_3 zmodyfikowano baz� danych (wi�cej powi�za� tabel)
- Celem jest przenoszenie/refaktoryzacja funkcjonalno�ci z ASMED_2_5 do nowej architektury

## Struktura projektu (ASMED_3)
- **Views**: Widoki XAML i code-behind (np. pacjent, skierowania, faktury, badania, rejestracja, ustawienia)
- **ViewModels**: Klasy ViewModel zgodnie z MVVM (np. PacjentSkierowanieViewModel, SkierowaniaViewModel)
- **Models**: Klasy danych (np. Pacjent, Firma)
- **Helpers/Converters**: Klasy pomocnicze i konwertery do XAML
- **G��wne pliki**: App.xaml, MainWindow.xaml

## Przyk�ad widoku
- `Views/pacjent/PacjentSkierowanieView.xaml` � formularz z polami pacjenta, dynamicznym prawym panelem i paskami przycisk�w

## Dalsze kroki
- Mo�liwo�� analizy i migracji wybranych funkcjonalno�ci z ASMED_2_5 (po podaniu �cie�ki do projektu)
- Refaktoryzacja pod k�tem nowych powi�za� w bazie i mo�liwo�ci Syncfusion/.NET 8

## opisane funkcjonalno�ci
1. **Pacjent**: Dodawanie, edycja, usuwanie pacjent�w, wyszukiwanie, filtrowanie
2. **Skierowania**: Zarz�dzanie skierowaniami, powi�zania z pacjentami
3. **Faktury**: Tworzenie i zarz�dzanie faktur     
4. **Badania**: Rejestrowanie i przegl�danie bada�w
5. **Rejestracja**: Zarz�dzanie wizytami pacjent�w
6. **Ustawienia**: Konfiguracja aplikacji, zarz�dzanie u��ytkownikami
7. **Raporty i eksport**: Generowanie raport�w, eksport do PDF (iTextSharp, PdfiumViewer)
8. **Powiadomienia**: System powiadomie� (Notifications.Wpf)
9. **Baza danych**: Operacje CRUD na bazie Access, uproszczona struktura, powi�zania przez ID
10. **Interfejs u��ytkownika**: Dynamiczne widoki, zak��adki, panele boczne
11. **Kontrolki Syncfusion**: Zaawansowane kontrolki UI dla lepszego do��wiadczenia u��ytkownika
12. **Modularno��**: Ka��da funkcjonalno���� jako oddzielny modu��, u��atwiaj�cy rozwijanie i utrzymanie aplikacji
13. **MVVM**: Oddzielenie logiki biznesowej od interfejsu u��ytkownika, u��atwiaj�ce testowanie i rozwijanie aplikacji
14. **Obs�uga b��d��w**: Mechanizmy obs�ugi b��d��w i walidacji danych
15. **Dokumentacja**: Komentarze w kodzie, dokumentacja techniczna dla przysz�ych programist�w
16. **Testowanie**: Podstawowe testy jednostkowe dla kluczowych funkcji
17. **Wydajno����**: Optymalizacja zapyta� do bazy danych i operacji na du��ych zbiorach danych
18. **Bezpiecze��stwo**: Podstawowe mechanizmy zabezpieczaj�ce dane pacjent�w i operacje w aplikacji
19. **Aktualizacje**: Mechanizm aktualizacji aplikacji i bazy danych
20. **Responsywność UI**: Zapewnienie płynnego działania interfejsu użytkownika, nawet przy dużej ilości danych
21. **Wsparcie dla różnych rozdzielczości ekranu**: Zapewnienie poprawnego wyświetlania aplikacji na różnych urządzeniach i rozdzielczościach ekranu


---

*Podsumowanie mo�na aktualizowa� na koniec ka�dej sesji i wykorzystywa� do kontynuacji prac.*
