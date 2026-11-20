USE MaturaarbeitenDb;
GO

SET NOCOUNT ON;

-- testdaten
DELETE FROM Buchung;
DELETE FROM G4Praesentation;
DELETE FROM Person;
DELETE FROM Raum;

-- testdaten
INSERT INTO Raum (bezeichnung) VALUES ('Testraum 101');
DECLARE @raum_id INT = SCOPE_IDENTITY();

INSERT INTO G4Praesentation (thema, datum, startZeit, endZeit, raum_id) 
VALUES ('KI in der Medizin', '2026-06-01', '10:00:00', '11:00:00', @raum_id);
DECLARE @praes_1 INT = SCOPE_IDENTITY();

INSERT INTO G4Praesentation (thema, datum, startZeit, endZeit, raum_id) 
VALUES ('Blockchain', '2026-06-01', '10:30:00', '11:30:00', @raum_id);
DECLARE @praes_2 INT = SCOPE_IDENTITY();

INSERT INTO G4Praesentation (thema, datum, startZeit, endZeit, raum_id) 
VALUES ('Erneuerbare Energien', '2026-06-01', '12:00:00', '13:00:00', @raum_id);
DECLARE @praes_3 INT = SCOPE_IDENTITY();

INSERT INTO G4Praesentation (thema, datum, startZeit, endZeit, raum_id) 
VALUES ('Quantencomputing', '2026-06-01', '14:00:00', '15:00:00', @raum_id);
DECLARE @praes_4 INT = SCOPE_IDENTITY();

DECLARE @schueler_1 UNIQUEIDENTIFIER = NEWID();
DECLARE @schueler_2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Person (person_id, vorname, nachname, email, passwort_hash) 
VALUES (@schueler_1, 'Max', 'Mustermann', 'max@test.ch', 'hash123');

INSERT INTO Person (person_id, vorname, nachname, email, passwort_hash) 
VALUES (@schueler_2, 'Anna', 'Muster', 'anna@test.ch', 'hash456');

-- testdaten
EXEC dbo.uspBuchungPraesentation @person_id = @schueler_1, @praesentation_id = @praes_1;
EXEC dbo.uspBuchungPraesentation @person_id = @schueler_1, @praesentation_id = @praes_3;

-- Abfrage
PRINT '--- Test 1: Max 2 Buchungen ---';
BEGIN TRY
    EXEC dbo.uspBuchungPraesentation @person_id = @schueler_1, @praesentation_id = @praes_4;
    PRINT 'FEHLER: Test fehlgeschlagen.';
END TRY
BEGIN CATCH
    PRINT 'BESTANDEN: ' + ERROR_MESSAGE();
END CATCH;

-- Abfrage
PRINT '--- Test 2: Zeitkonflikt ---';
BEGIN TRY
    EXEC dbo.uspBuchungPraesentation @person_id = @schueler_2, @praesentation_id = @praes_1;
    EXEC dbo.uspBuchungPraesentation @person_id = @schueler_2, @praesentation_id = @praes_2;
    PRINT 'FEHLER: Test fehlgeschlagen.';
END TRY
BEGIN CATCH
    PRINT 'BESTANDEN: ' + ERROR_MESSAGE();
END CATCH;

-- testdaten
DECLARE @i INT = 1;
WHILE @i <= 6
BEGIN
    DECLARE @temp_student UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Person (person_id, vorname, nachname, email, passwort_hash)
    VALUES (@temp_student, 'Schueler', CAST(@i AS VARCHAR), 's' + CAST(@i AS VARCHAR) + '@test.ch', 'hash');
    EXEC dbo.uspBuchungPraesentation @person_id = @temp_student, @praesentation_id = @praes_4;
    SET @i = @i + 1;
END;

-- Abfrage
PRINT '--- Test 3: Raum Voll (Max 6) ---';
BEGIN TRY
    DECLARE @student_7 UNIQUEIDENTIFIER = NEWID();
    INSERT INTO Person (person_id, vorname, nachname, email, passwort_hash)
    VALUES (@student_7, 'Siebter', 'Schueler', 's7@test.ch', 'hash');
    EXEC dbo.uspBuchungPraesentation @person_id = @student_7, @praesentation_id = @praes_4;
    PRINT 'FEHLER: Test fehlgeschlagen.';
END TRY
BEGIN CATCH
    PRINT 'BESTANDEN: ' + ERROR_MESSAGE();
END CATCH;

-- Abfrage
PRINT '--- Test 4: Doppelte Buchung derselben Präsentation ---';
BEGIN TRY
    -- Schüler 1 hat im oberen Bereich bereits Präsentation 1 gebucht und versucht es hier erneut
    EXEC dbo.uspBuchungPraesentation @person_id = @schueler_1, @praesentation_id = @praes_1;
    PRINT 'FEHLER: Test fehlgeschlagen.';
END TRY
BEGIN CATCH
    PRINT 'BESTANDEN: ' + ERROR_MESSAGE();
END CATCH;
GO