-- Skrypt SQL do utworzenia tabeli Umowy_Firm w bazie MS Access

CREATE TABLE Umowy_Firm (
    Id COUNTER PRIMARY KEY,
    Firma_ID LONG NOT NULL,
    Data_Umowy DATETIME NOT NULL,
    Ilosc_Miesiecy LONG DEFAULT 0,
    Czy_Terminowa YESNO DEFAULT Yes,
    Status TEXT(50) DEFAULT 'Aktywna',
    Budzet CURRENCY DEFAULT 0,
    Data_Koncowa DATETIME
);

-- Jeśli używasz relacji w Access, możesz również dodać klucz obcy:
-- CONSTRAINT FK_Umowy_Firma FOREIGN KEY (Firma_ID) REFERENCES Firma(id)

-- Indeks dla szybszego wyszukiwania umów firmy
CREATE INDEX idx_firma_id ON Umowy_Firm (Firma_ID);

-- Indeks dla statusu
CREATE INDEX idx_status ON Umowy_Firm (Status);

-- Dodanie pola Umowa_ID do tabeli Lista_badania_do_faktury
-- (Jeśli tabela już istnieje, użyj ALTER TABLE)
-- ALTER TABLE Lista_badania_do_faktury ADD COLUMN Umowa_ID LONG;

-- Indeks dla Umowa_ID w tabeli Lista_badania_do_faktury
-- CREATE INDEX idx_umowa_id ON Lista_badania_do_faktury (Umowa_ID);
