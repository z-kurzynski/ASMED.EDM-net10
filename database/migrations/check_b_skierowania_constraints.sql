-- ============================================
-- SPRAWDZENIE CONSTRAINTS DLA B_Skierowania
-- ============================================

USE asmed2026_krone;

-- 1. WSZYSTKIE FOREIGN KEYS dla B_Skierowania
SELECT 
	CONSTRAINT_NAME,
	COLUMN_NAME,
	REFERENCED_TABLE_NAME,
	REFERENCED_COLUMN_NAME,
	DELETE_RULE,
	UPDATE_RULE
FROM 
	INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE 
	TABLE_SCHEMA = 'asmed2026_krone'
	AND TABLE_NAME = 'B_Skierowania'
	AND REFERENCED_TABLE_NAME IS NOT NULL
ORDER BY 
	CONSTRAINT_NAME;

-- 2. SPRAWDZENIE WARTOŚCI B_Badanie_ID
SELECT 
	'Total rows' AS info,
	COUNT(*) AS count
FROM B_Skierowania
UNION ALL
SELECT 
	'B_Badanie_ID NOT NULL' AS info,
	COUNT(*) AS count
FROM B_Skierowania
WHERE B_Badanie_ID IS NOT NULL
UNION ALL
SELECT 
	'B_Badanie_ID NULL' AS info,
	COUNT(*) AS count
FROM B_Skierowania
WHERE B_Badanie_ID IS NULL;

-- 3. PRZYKŁADOWE WARTOŚCI B_Badanie_ID (pierwsze 10)
SELECT 
	B_ID,
	B_Badanie_ID,
	B_Pacjent_ID,
	B_DataSkierowania
FROM B_Skierowania
WHERE B_Badanie_ID IS NOT NULL
LIMIT 10;

-- 4. SPRAWDZENIE CZY WARTOŚCI B_Badanie_ID PASUJĄ DO Badanie.Bad_ID
SELECT 
	'Values matching Badanie.Bad_ID' AS info,
	COUNT(*) AS count
FROM B_Skierowania bs
WHERE bs.B_Badanie_ID IS NOT NULL
  AND EXISTS (
	  SELECT 1 FROM Badanie b WHERE b.Bad_ID = bs.B_Badanie_ID
  );

-- 5. SPRAWDZENIE CZY WARTOŚCI B_Badanie_ID PASUJĄ DO BAD_Lista.Identyfikator
SELECT 
	'Values matching BAD_Lista.Identyfikator' AS info,
	COUNT(*) AS count
FROM B_Skierowania bs
WHERE bs.B_Badanie_ID IS NOT NULL
  AND EXISTS (
	  SELECT 1 FROM BAD_Lista bl WHERE bl.Identyfikator = bs.B_Badanie_ID
  );
