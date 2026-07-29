# Podsumowanie projektu ASMED_3

1. **Technologia**: Projekt ASMED_3 to aplikacja WPF oparta na .NET 8.
2. **Migracja**: Trwa migracja funkcjonalności z wersji ASMED_2_5 do nowej, uproszczonej i modularnej architektury.
3. **UI**: Interfejs użytkownika podzielony jest na tematyczne bloki, prezentowane jako zakładki. W obrębie zakładki widoki XAML mają być dynamicznie zmieniane, a dane przekazywane pomiędzy nimi.
4. **Baza danych**: Obsługa bazy Access znajduje się w folderze Helpers. Baza ma stałą lokalizację poza projektem, a jej struktura została uproszczona. Powiązania między danymi realizowane są przez ID dla poszczególnych bloków programu.
5. **Biblioteki**: Wykorzystywane są nowoczesne kontrolki Syncfusion (licencja społecznościowa), a także iTextSharp, PdfiumViewer, Notifications.Wpf.
6. **Cel aplikacji**: Zarządzanie danymi pacjentów oraz operacje na bazie Access, z naciskiem na modularność, dynamiczne widoki i uproszczone powiązania danych.
7. Syncfusion w obsłudze UI jako główny pakiet
1. 0. **Struktura projektu**: Projekt podzielony jest na foldery takie jak Views, ViewModels, Models, Helpers, co ułatwia zarządzanie kodem i jego rozwój.
	1. **Modularność**: Każda funkcjonalność jest realizowana w osobnym module, co pozwala na łatwe dodawanie nowych funkcji i utrzymanie kodu.

---

Funkcjonalności do zaimplementowania:
- [x] Strona główna z podsumowaniem i szybkim dostępem do funkcji
- [X] Zarządzanie pacjentami (dodawanie, edytowanie, usuwanie) --  brak usuwania - Helpers/PatientHelper.cs
- [X] Wyszukiwanie i filtrowanie pacjentów -- Helpers/PatientHelper.cs
- [ ] Zarządzanie wizytami i terminarzem -- brak wizyt
- [ ] Przeglądanie i zarządzanie historią medyczną pacjentów -- brak historii
- [ ] Generowanie raportów i dokumentów PDF - (iTextSharp, PdfiumViewer-wycofane) - Aktualne Syncfusion.Pdf - Helpers/PdfHelper.cs
- [X] Integracja z bazą danych Access (CRUD) - Helpers/DatabaseHelper.cs
- [ ] Dynamiczne widoki XAML w obrębie zakładek  - UserControl
- [ ] Powiadomienia i alerty dla użytkowników - Notifications.Wpf
- [ ] Testy jednostkowe i integracyjne - brak testów
- [ ] Optymalizacja wydajności aplikacji - brak optymalizacji
- [ ] Dokumentacja techniczna i użytkownika - brak dokumentacji
- [ ] Szkolenie użytkowników końcowych - brak szkolenia
- [ ] Wdrożenie i monitorowanie aplikacji w środowisku produkcyjnym - brak wdrożenia
- [ ] Zbieranie feedbacku od użytkowników i iteracyjne ulepszanie aplikacji - brak feedbacku
- [ ] Planowanie przyszłych aktualizacji i nowych funkcji - brak planowania
- [X] Zapewnienie wsparcia technicznego i utrzymania aplikacji - wsparcie techniczne
- [ ] Analiza bezpieczeństwa i zgodności z regulacjami prawnymi - brak analizy
- [ ] Implementacja funkcji eksportu i importu danych - brak importu/eksportu - Helpers/DatabaseHelper.cs - ExportPatientsToCsv i ImportPatientsFromCsv 
- [ ] Integracja z innymi systemami medycznymi - brak integracji
- [ ] Personalizacja interfejsu użytkownika - brak personalizacji - Settings - Helpers/SettingsHelper.cs
- [ ] Implementacja funkcji wyszukiwania i filtrowania danych - brak zaawansowanego filtrowania
- [ ] Zarządzanie uprawnieniami użytkowników i rolami - brak ról
- [ ] Backup i odzyskiwanie danych - brak backupu
- [ ] Monitorowanie i logowanie zdarzeń w aplikacji - brak logowania
- [ ] Aktualizacja bibliotek i zależności do najnowszych wersji - częściowo zaktualizowane
- [ ] Optymalizacja zużycia pamięci i zasobów systemowych - brak optymalizacji
- [ ] Implementacja funkcji eksportu danych do różnych formatów (CSV, XML, JSON) - brak eksportu do różnych formatów - Helpers/DatabaseHelper.cs - ExportPatientsToCsv
- [ ] Integracja z usługami chmurowymi (np. Azure, AWS) - brak integracji
- [ ] Implementacja funkcji synchronizacji danych między urządzeniami - brak synchronizacji
- [	] Zarządzanie wersjami i aktualizacjami aplikacji - brak zarządzania wersjami - ręczna
- [ ] Implementacja funkcji analizy danych i raportowania - brak analizy danych
- [ ] 

W ramach zakłądek "Pacjenci" zaimplementowano:
- Dodawanie pacjentów
- Edycję pacjentów
- Wyszukiwanie pacjentów

W ramach zakładki "Ustawienia" zaimplementowano:

W ramach zakładki Skierowania zaimplementowano:
- Dodawanie skierowań
- Edycję skierowań
- Wyszukiwanie skierowań
- Generowanie PDF skierowań
- Drukowanie PDF skierowań
- Usuwanie skierowań - do realizacji w przyszłości
- Eksport skierowań do CSV - do realizacji w przyszłości
- Import skierowań z CSV - do realizacji w przyszłości
- Zaawansowane filtrowanie skierowań
- Sortowanie skierowań
- Paginację skierowań

W ramach zakłdki Badania zaimplementowano:
- Dodawanie badań
- Edycję badań
- Wyszukiwanie badań
- Generowanie PDF badań - Drukowanie PDF badań - do realizacji w przyszłości
- Usuwanie badań 
- Eksport badań do CSV - do realizacji w przyszłości
- Import badań z CSV - do realizacji w przyszłości
- Zaawansowane filtrowanie badań
- Sortowanie badań
- Paginację badań
- Filtrowanie badań po pacjencie
- Filtrowanie badań po skierowaniu
- Filtrowanie badań po dacie
- Filtrowanie badań po typie badania

W ramach zakładki Faktury zaimplementowano:

W ramach zakładki Lista Do Faktur 
- listy badań do Faktur zaimplementowano:
	wyświetlanie listy badań do faktur
	edycję pozycji na liscie badań do faktur
-	enerowanie PDF listy badań do faktur
	drukowanie PDF listy badań do faktur
	zaawansowane filtrowanie listy badań do faktur
	sortowanie listy badań do faktur

Do realizacji:
- dodawanie badań do faktury
- usuwanie badań z faktury
- generowanie faktur PDF
- drukowanie faktur PDF
- eksport faktur do CSV
- import faktur z CSV
- paginacja faktur
- filtrowanie listy do faktur
- sortowanie listy do faktur
- edycja listy do faktur
- usuwanie listy do faktur


