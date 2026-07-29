-- ============================================
-- SPRAWDZENIE PROBLEMATYCZNYCH WIERSZY
-- ============================================

USE asmed2026_krone;

-- 1. Pokaż 4 problematyczne wiersze
SELECT 
	bs.B_ID,
	bs.B_Badanie_ID AS "B_Badanie_ID (niepasujące)",
	bs.B_Pacjent_ID,
	bs.B_DataSkierowania,
	bs.B_TypBadania
FROM B_Skierowania bs
WHERE bs.B_Badanie_ID IS NOT NULL
  AND NOT EXISTS (
	  SELECT 1 FROM Badanie b WHERE b.Bad_ID = bs.B_Badanie_ID
  )
ORDER BY bs.B_ID;

-- 2. Sprawdź zakres wartości w Badanie.Bad_ID
SELECT 
	MIN(Bad_ID) AS min_Bad_ID,
	MAX(Bad_ID) AS max_Bad_ID,
	COUNT(*) AS total_badania
FROM Badanie;

-- 3. Sprawdź zakres wartości w BAD_Lista.Identyfikator
SELECT 
	MIN(Identyfikator) AS min_Identyfikator,
	MAX(Identyfikator) AS max_Identyfikator,
	COUNT(*) AS total_lista
FROM BAD_Lista;

-- 4. Sprawdź czy te 4 wartości pasują do BAD_Lista.Identyfikator
SELECT 
	bs.B_ID,
	bs.B_Badanie_ID,
	bl.Identyfikator AS "Pasuje_do_BAD_Lista",
	bl.bn_cennik
FROM B_Skierowania bs
LEFT JOIN BAD_Lista bl ON bs.B_Badanie_ID = bl.Identyfikator
WHERE bs.B_Badanie_ID IS NOT NULL
  AND NOT EXISTS (
	  SELECT 1 FROM Badanie b WHERE b.Bad_ID = bs.B_Badanie_ID
  )
ORDER BY bs.B_ID;
