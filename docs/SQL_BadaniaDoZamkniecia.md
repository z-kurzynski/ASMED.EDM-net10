# SQL Query dla listy badań do zamknięcia

## ✅ Poprawne Query (potwierdzone - zwraca 105 rekordów)

```sql
SELECT 
	B_Skierowania.B_ID,
	B_Skierowania.B_DataSkierowania,
	B_Skierowania.B_TypBadania,
	B_Skierowania.B_książeczka,
	B_Skierowania.B_Zaswiadczenie,
	P_Pacjent.P_ID,
	P_Pacjent.P_imie,
	P_Pacjent.P_nazwisko,
	P_Pacjent.P_pesel,
	P_Pacjent.P_zawód,
	P_Pacjent.P_Firma_id,
	Firma.Nazwa AS Firma_Nazwa,
	Firma.NIP AS Firma_NIP,
	Firma.Cennik AS Firma_Cennik,
	Badanie.Bad_Data,
	Badanie.Bad_ID,
	Badanie.Bad_Data_Do,
	Badanie.Bad_Wynik,
	Badanie.Bad_Nr_KS,
	Badanie.Bad_bn_cennik,
	Badanie.Bad_Razem,
	Badanie.Bad_Cena1,
	Badanie.Bad_Cena2,
	Badanie.Bad_Cena3,
	Badanie.Bad_Cena4,
	Badanie.Bad_Cena5,
	Badanie.Bad_Cena6,
	Badanie.Bad_Cena7,
	Badanie.Bad_Cena8
FROM 
	(
		(B_Skierowania 
		INNER JOIN P_Pacjent ON B_Skierowania.B_Pacjent_ID = P_Pacjent.P_ID)
		LEFT JOIN Firma ON P_Pacjent.P_Firma_id = Firma.id
	)
	LEFT JOIN Badanie ON B_Skierowania.B_ID = Badanie.Bad_S_ID
WHERE 
	B_Skierowania.B_Activ = True
	AND Badanie.Bad_Data IS NULL
ORDER BY 
	B_Skierowania.B_ID DESC;
```

## 🔑 Kluczowy warunek filtrowania

**POPRAWNY:**
```sql
WHERE Badanie.Bad_Data IS NULL
```

**BŁĘDNY (użyty wcześniej):**
```sql
WHERE Badanie.Bad_Data_Do IS NULL
```

## 📊 Wyjaśnienie

- **Bad_Data** = Data wykonania badania (gdy pacjent był na badaniu)
- **Bad_Data_Do** = Data ważności badania (do kiedy badanie jest ważne)

**Badanie do zamknięcia** = badanie które jeszcze **NIE MA daty wykonania** (`Bad_Data IS NULL`), czyli pacjent został skierowany ale jeszcze nie przyszedł na badanie.

## 🔄 Implementacja w Entity Framework Core (TODO)

Gdy będą gotowe encje w `src/ASMED.EDM.Core/Entities/`:

```csharp
public async Task<List<BadanieDto>> GetBadaniaDoZamknieciaAsync()
{
	using var context = new AsmedDbContext(_options);

	var query = from skier in context.Skierowania
				join pacjent in context.Pacjenci on skier.PacjentId equals pacjent.Id
				join firma in context.Firmy on pacjent.FirmaId equals firma.Id into firmaGroup
				from firma in firmaGroup.DefaultIfEmpty()
				join badanie in context.Badania on skier.Id equals badanie.SkierowanieId into badanieGroup
				from badanie in badanieGroup.DefaultIfEmpty()
				where skier.IsActive == true && badanie.DataWykonania == null
				orderby skier.Id descending
				select new BadanieDto
				{
					B_ID = skier.Id,
					B_DataSkierowania = skier.DataSkierowania,
					B_TypBadania = skier.TypBadania,
					P_imie = pacjent.Imie,
					P_nazwisko = pacjent.Nazwisko,
					P_pesel = pacjent.Pesel,
					Firma_Nazwa = firma.Nazwa,
					Bad_Data = badanie.DataWykonania,  // NULL dla badań do zamknięcia
					// ... inne pola
				};

	return await query.ToListAsync();
}
```

## 📝 Uwagi implementacyjne

1. **LEFT JOIN Badanie** - jest istotne, bo dla badań do zamknięcia może nie być jeszcze rekordu w tabeli `Badanie` (tylko w `B_Skierowania`)
2. **B_Activ = True** - filtruje tylko aktywne skierowania
3. **ORDER BY B_ID DESC** - najnowsze skierowania na górze
4. Liczba rekordów powinna wynosić **105** (zweryfikowane na bazie produkcyjnej)

## 🎯 Aktualny stan

Obecnie w `src/ASMED.EDM.UI/Services/BadaniaService.cs` używamy:
- Mock data (3 przykładowe rekordy)
- Warunek: `b.Bad_Data == null` ✅ POPRAWNY

Do zaimplementowania:
- Połączenie z bazą Access/MySQL
- Migracja na Entity Framework Core
- Pełny query z JOIN jak powyżej
