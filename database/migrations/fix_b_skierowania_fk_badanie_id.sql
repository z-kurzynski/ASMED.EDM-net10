-- ============================================
-- FIX: B_Skierowania.B_Badanie_ID Foreign Key
-- ============================================
-- Błąd: B_Badanie_ID wskazuje na BAD_Lista.Identyfikator
-- Poprawka: B_Badanie_ID powinno wskazywać na Badanie.Bad_ID

USE asmed2026_krone;

-- KROK 1: Usuń błędny constraint
ALTER TABLE `B_Skierowania` 
DROP FOREIGN KEY `B_Skierowania_ibfk_3`;

-- KROK 1b: Wyczyść wisące wartości B_Badanie_ID (nie mają odpowiednika w tabeli Badanie)
UPDATE `B_Skierowania`
SET `B_Badanie_ID` = NULL
WHERE `B_Badanie_ID` IS NOT NULL
  AND `B_Badanie_ID` NOT IN (SELECT `Bad_ID` FROM `Badanie`);

-- KROK 2: Dodaj poprawny constraint
-- (opcjonalnie z ON DELETE SET NULL jeśli chcesz, żeby przy usunięciu badania znikał link)
ALTER TABLE `B_Skierowania` 
ADD CONSTRAINT `B_Skierowania_ibfk_badanie` 
FOREIGN KEY (`B_Badanie_ID`) 
REFERENCES `Badanie` (`Bad_ID`) 
ON DELETE SET NULL 
ON UPDATE CASCADE;

-- WERYFIKACJA:
SELECT 
	CONSTRAINT_NAME,
	COLUMN_NAME,
	REFERENCED_TABLE_NAME,
	REFERENCED_COLUMN_NAME
FROM 
	INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE 
	TABLE_SCHEMA = 'asmed2026_krone'
	AND TABLE_NAME = 'B_Skierowania'
	AND COLUMN_NAME = 'B_Badanie_ID';

-- Oczekiwany wynik:
-- CONSTRAINT_NAME: B_Skierowania_ibfk_badanie
-- COLUMN_NAME: B_Badanie_ID
-- REFERENCED_TABLE_NAME: Badanie
-- REFERENCED_COLUMN_NAME: Bad_ID
