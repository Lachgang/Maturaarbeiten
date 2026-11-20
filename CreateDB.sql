USE master;
GO

IF DB_ID(N'MaturaarbeitenDb') IS NOT NULL
BEGIN
    ALTER DATABASE MaturaarbeitenDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE MaturaarbeitenDb;
END;
GO

CREATE DATABASE MaturaarbeitenDb;
GO

USE MaturaarbeitenDb;
GO

CREATE TABLE mat_raum (
    id CHAR(3) NOT NULL,
    bezeichnung VARCHAR(50) NOT NULL,
    CONSTRAINT pk_id_mat_raum PRIMARY KEY (id)
);

CREATE TABLE mat_person (
    id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    vorname NVARCHAR(20) NOT NULL,
    nachname NVARCHAR(20) NOT NULL,
    email VARCHAR(50) NOT NULL,
    passwort_hash VARCHAR(256) NOT NULL,
    ist_Admin BIT NOT NULL DEFAULT 0,
    CONSTRAINT pk_id_mat_person PRIMARY KEY (id),
    CONSTRAINT uq_mat_email UNIQUE (email)
);

CREATE TABLE mat_g4_praesentation (
    id INT IDENTITY(1,1) NOT NULL,
    thema VARCHAR(200) NOT NULL,
    datum DATE NOT NULL,
    startZeit TIME NOT NULL,
    endZeit TIME NOT NULL,
    raum_id CHAR(3) NOT NULL,
    CONSTRAINT pk_id_mat_g4_praesentation PRIMARY KEY (id),
    CONSTRAINT fk_id_mat_raum FOREIGN KEY (raum_id) REFERENCES mat_raum(id),
    CONSTRAINT uq_mat_thema UNIQUE (thema)
);

CREATE TABLE mat_buchung (
    id INT IDENTITY(1,1) NOT NULL,
    person_id UNIQUEIDENTIFIER NOT NULL,
    praesentation_id INT NOT NULL,
    startzeit DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT pk_id_mat_buchung PRIMARY KEY (id),
    CONSTRAINT fk_id_mat_person FOREIGN KEY (person_id) REFERENCES mat_person(id),
    CONSTRAINT fk_id_mat_praesentation FOREIGN KEY (praesentation_id) REFERENCES mat_g4_praesentation(id),
    CONSTRAINT uq_mat_person_praesentation UNIQUE (person_id, praesentation_id)
);
GO

CREATE OR ALTER TRIGGER dbo.TR_mat_Buchung_Validierung ON mat_buchung
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE (
            SELECT COUNT(b.id)
            FROM mat_buchung b
            WHERE b.person_id = i.person_id
        ) + (
            SELECT COUNT(i2.person_id)
            FROM inserted i2
            WHERE i2.person_id = i.person_id
        ) > 2
    )
    BEGIN
        RAISERROR('Fehler, zu viele Buchungen', 16, 1);
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        WHERE (
            SELECT COUNT(b.id)
            FROM mat_buchung b
            WHERE b.praesentation_id = i.praesentation_id
        ) + (
            SELECT COUNT(i2.praesentation_id)
            FROM inserted i2
            WHERE i2.praesentation_id = i.praesentation_id
        ) > 6
    )
    BEGIN
        RAISERROR('Raum ist Voll', 16, 1);
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM mat_buchung b
        JOIN mat_g4_praesentation p_alt ON b.praesentation_id = p_alt.id
        JOIN inserted i ON b.person_id = i.person_id
        JOIN mat_g4_praesentation p_neu ON p_neu.id = i.praesentation_id
        WHERE p_neu.datum = p_alt.datum
          AND p_neu.startZeit < p_alt.endZeit
          AND p_neu.endZeit > p_alt.startZeit
    )
    BEGIN
        RAISERROR('2 Präsentationen zur gleichen Zeit', 16, 1);
        RETURN;
    END;

    INSERT INTO mat_buchung (person_id, praesentation_id, startzeit)
    SELECT person_id, praesentation_id, ISNULL(startzeit, GETDATE())
    FROM inserted;
END;
GO