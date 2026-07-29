# AssignedBadanieDto — pola użyte w `ListaFaktAddView.xaml`

Plik źródłowy klasy: `Helpers/AccessDbContext.cs`  
Widok: `Views/lista_do_faktur/ListaFaktAddView.xaml`

Poniżej tabela właściwości DTO i informacja, gdzie są używane w XAML (bez pośrednich użyć w ViewModel).

| Właściwość | Typ | Użycie w XAML |
|---|---:|---|
| `Lp` | `int` | DataGrid — kolumna "Lp" (Binding `{Binding Lp}`) |

| `Bad_Data` | `DateTime?` | ListView (GridView) — wyświetlana w wierszu; DataGrid — kolumna "Data Bad" |

| `Bad_Data_Do` | `DateTime?` | NIE używane bezpośrednio w tym XAML |

| `Bad_Typ` | `string?` | DataGrid — kolumna "Typ" |
| `P_imie` | `string?` | NIE używane bezpośrednio (używane pośrednio przez `PacjentDisplay`) |
| `P_nazwisko` | `string?` | NIE używane bezpośrednio (używane pośrednio przez `PacjentDisplay`) |

| `PacjentDisplay` | `string` | ListView — nagłówek/tekst; DataGrid — kolumna "Pacjent" |

| `P_zawod` | `string?` | NIE używane bezpośrednio w tym XAML |
| `FirmaNazwa` | `string?` | ListView — wiersz (firma) |
| `Bad_Razem` | `decimal?` | ListView — wiersz; DataGrid — kolumna "Razem" (teksty + DataTrigger) |
| `Bad_Cena1` | `decimal?` | DataGrid — kolumna "Lek.Med" |
| `Bad_Cena2` | `decimal?` | DataGrid — kolumna "Laryng." |
| `Bad_Cena3` | `decimal?` | DataGrid — kolumna "Okul." |
| `Bad_Cena4` | `decimal?` | DataGrid — kolumna "Książ." |
| `Bad_Cena5` | `decimal?` | DataGrid — kolumna "Lipid." |
| `Bad_Cena6` | `decimal?` | DataGrid — kolumna "EKG" |
| `Bad_Cena7` | `decimal?` | DataGrid — kolumna "Urlop" |
| `Bad_Cena8` | `decimal?` | DataGrid — kolumna "Inne" |
| `Bad_Cena9` | `decimal?` | NIE używane bezpośrednio w tym XAML |
| `Bad_Cena10` | `decimal?` | NIE używane bezpośrednio w tym XAML |
| `Bad_Nr_KS` | `string?` | NIE używane bezpośrednio w tym XAML |

| `Bad_Wynik` | `string?` | NIE używane bezpośrednio w tym XAML |

| `Bad_END` | `bool?` | NIE używane bezpośrednio w tym XAML |

| `Bad_ID` | `int?` | NIE używane w widoku, ale DTO przekazywane w CommandParameter (przekaz całego obiektu) |

| `Bad_L_ID` | `int?` | NIE używane bezpośrednio |

| `Bad_S_ID`, `Bad_P_ID`, `Bad_F_ID` | `int?` | NIE używane bezpośrednio w XAML |

| `FirmaCennik`, `Bad_bn_cennik` | `string?` | NIE używane bezpośrednio w XAML |

| `DataSkierDate`, `DataSkierDisplay` | aliasy / read-only | NIE używane bezpośrednio w tym XAML (obecne w DTO) |

Dodatkowe uwagi:
- W ListView przycisk "Dodaj" przekazuje `CommandParameter="{Binding Dto}"` — przycisk otrzymuje cały obiekt DTO (przez wrapperę).  
- W DataGrid przycisk "Usuń" przekazuje `CommandParameter="{Binding}"` (czyli obiekt `AssignedBadanieDto`).  
- Pola wykorzystywane tylko pośrednio (np. `P_imie`/`P_nazwisko`) są widoczne w UI przez `PacjentDisplay` (computed property) — nie wymagają dodatkowego bindowania.
- Jeśli chcesz TwoWay‑binding dla właściwości widocznych w ListView (np. chcesz edytować `Bad_Data` bez wrappera), lepiej dodać proxy-settery w `AssignedBadanieWrapper` lub wiązać do `Dto.Bad_Data` bezpośrednio.

Chcesz, żebym wygenerował:
- wersję tego raportu z odnośnikami (linia/kolumna) do konkretnego miejsca w `ListaFaktAddView.xaml`,  
- czy dodać proxy‑settery dla dodatkowych pól w `AssignedBadanieWrapper`?


Klasa: `AccessDbContext.AssignedBadanieDto`

Poniżej pełna lista właściwości, ich typy i dostępność (mutowalność). Przydatne przy tworzeniu bindingów XAML lub wrapperów.

| Właściwość | Typ | Dostępność | Uwagi |
|---|---:|---|---|
| `Bad_ID` | `int?` | get / set | PK rekordu (DB) |

| `Bad_L_ID` | `int?` | get / set | identyfikator listy (jeśli przypisane) |

| `Bad_Data` | `DateTime?` | get / set | data badania |
| `Bad_Data_Do` | `DateTime?` | get / set | data ważności |
| `Bad_Typ` | `string?` | get / set | typ badania |
| `P_imie` | `string?` | get / set | imię pacjenta |
| `P_nazwisko` | `string?` | get / set | nazwisko pacjenta |
| `P_zawod` | `string?` | get / set | zawód pacjenta |
| `FirmaNazwa` | `string?` | get / set | nazwa firmy |
| `Bad_Razem` | `decimal?` | get / set | suma/brutto (razem) |
| `Bad_Cena1` | `decimal?` | get / set | cena 1 (Lek.Med) |
| `Bad_Cena2` | `decimal?` | get / set | cena 2 (Laryng.) |
| `Bad_Cena3` | `decimal?` | get / set | cena 3 (Okul.) |
| `Bad_Cena4` | `decimal?` | get / set | cena 4 (Książ.) |
| `Bad_Cena5` | `decimal?` | get / set | cena 5 (Lipid.) |
| `Bad_Cena6` | `decimal?` | get / set | cena 6 (EKG) |
| `Bad_Cena7` | `decimal?` | get / set | cena 7 (Urlop) |
| `Bad_Cena8` | `decimal?` | get / set | cena 8 (Inne) |
| `Bad_Nr_KS` | `string?` | get / set | numer książeczki |
| `Bad_END` | `bool?` | get / set | flaga zakończenia |
| `Lp` | `int` | get / set | numer wiersza (1-based, UI) |
| `PacjentDisplay` | `string` | get only | złożone: `P_imie + " " + P_nazwisko` |
| `DataSkierDate` | `DateTime?` | get only | alias do `Bad_Data` |
| `DataSkierDisplay` | `string` | get only | sformatowana `Bad_Data` (`dd.MM.yyyy`) |
| `Bad_Wynik` | `string?` | get / set | wynik badania |
| `FirmaCennik` | `string?` | get / internal set | ustawialne wewnątrz assembly |
| `Bad_Cena9` | `decimal?` | get / internal set | dodatkowa cena (9) |
| `Bad_Cena10` | `decimal?` | get / internal set | dodatkowa cena (10) |


| `Bad_S_ID` | `int?` | get / internal set | wewnętrzne id |

| `Bad_P_ID` | `int?` | get / internal set | wewnętrzne id |

| `Bad_F_ID` | `int?` | get / internal set | id firmy (wewnętrznie ustawiane) |

| `Bad_bn_cennik` | `string?` | get / internal set | wewnętrzny identyfikator cennika |




