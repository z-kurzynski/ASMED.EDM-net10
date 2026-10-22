-- =====================================================================
-- ASMED EDM - przygotowanie lokalnego serwera MySQL
--
-- PRZED URUCHOMIENIEM:
--   1. Zamien hasla ZMIEN_haslo_aplikacji / ZMIEN_haslo_backupu.
--   2. Podsiec gabinetu: 192.168.100.x (serwer 192.168.100.166) - popraw '192.168.100.%' jesli sie zmieni.
--
-- URUCHOMIENIE (PowerShell, jako root):
--   cmd /c '"C:\Program Files\MySQL\MySQL Server 26.10\bin\mysql.exe" -u root -p --default-character-set=utf8mb4 < "A:\scripts\mysql-local\01-setup-local-db.sql"'
--
-- Skrypt mozna uruchomic ponownie - nie nadpisuje istniejacych obiektow.
-- =====================================================================

SELECT VERSION() AS wersja,
	   @@datadir AS katalog_danych,
	   IF(@@lower_case_table_names = 2,
		  'OK (2 = Preserve Given Case)',
		  'UWAGA: lower_case_table_names <> 2 - przeinstaluj serwer z opcja Preserve Given Case') AS wielkosc_liter_tabel;

CREATE DATABASE IF NOT EXISTS asmed_local
	CHARACTER SET utf8mb4
	COLLATE utf8mb4_0900_ai_ci;

-- Uzytkownik programu: wszystkie stanowiska w sieci lokalnej + sam serwer.
-- Pelne prawa tylko do bazy asmed_local (program tworzy tabele przy starcie).
CREATE USER IF NOT EXISTS 'asmed_app'@'192.168.100.%' IDENTIFIED BY 'ZMIEN_haslo_aplikacji';
CREATE USER IF NOT EXISTS 'asmed_app'@'localhost'     IDENTIFIED BY 'ZMIEN_haslo_aplikacji';
GRANT ALL PRIVILEGES ON asmed_local.* TO 'asmed_app'@'192.168.100.%';
GRANT ALL PRIVILEGES ON asmed_local.* TO 'asmed_app'@'localhost';

-- Uzytkownik kopii zapasowych: tylko odczyt, tylko z tego komputera.
CREATE USER IF NOT EXISTS 'asmed_backup'@'localhost' IDENTIFIED BY 'ZMIEN_haslo_backupu';
CREATE USER IF NOT EXISTS 'asmed_backup'@'127.0.0.1' IDENTIFIED BY 'ZMIEN_haslo_backupu';
GRANT SELECT, SHOW VIEW, TRIGGER, LOCK TABLES, EVENT ON asmed_local.* TO 'asmed_backup'@'localhost';
GRANT SELECT, SHOW VIEW, TRIGGER, LOCK TABLES, EVENT ON asmed_local.* TO 'asmed_backup'@'127.0.0.1';
GRANT SHOW_ROUTINE ON *.* TO 'asmed_backup'@'localhost';
GRANT SHOW_ROUTINE ON *.* TO 'asmed_backup'@'127.0.0.1';

SELECT user, host, plugin FROM mysql.user WHERE user LIKE 'asmed%' ORDER BY user, host;
