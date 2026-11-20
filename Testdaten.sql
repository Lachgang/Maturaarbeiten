USE MaturaarbeitenDb;
GO

DELETE FROM mat_buchung;
DELETE FROM mat_g4_praesentation;
DELETE FROM mat_person;
DELETE FROM mat_raum;
GO

INSERT INTO mat_raum (id, bezeichnung)
VALUES
    ('R01', 'Zimmer R01'),
    ('R02', 'Zimmer R02'),
    ('R03', 'Zimmer R03'),
    ('I01', 'Informatikraum I01');
GO

DECLARE @AdminId UNIQUEIDENTIFIER    = '11111111-1111-1111-1111-111111111111';
DECLARE @Schueler1Id UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @Schueler2Id UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
DECLARE @Schueler3Id UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444444';
DECLARE @TestPwdHash VARCHAR(256)     = '$2a$12$D9A90ROxBa3mqOKDe9bEz.nxUXmhkoM9ApGhPgJChOxqCu9g5JUOa';

INSERT INTO mat_person (id, vorname, nachname, email, passwort_hash, ist_Admin)
VALUES
    (@AdminId,     'Damian', 'Wyss',    'admin@schule.ch',        @TestPwdHash, 1),
    (@Schueler1Id, 'Lukas',  'Meier',   'lukas.meier@schule.ch',  @TestPwdHash, 0),
    (@Schueler2Id, 'Sara',   'Keller',  'sara.keller@schule.ch',  @TestPwdHash, 0),
    (@Schueler3Id, 'Noah',   'Brunner', 'noah.brunner@schule.ch', @TestPwdHash, 0);
GO

INSERT INTO mat_g4_praesentation (thema, datum, startZeit, endZeit, raum_id)
VALUES
    ('Künstliche Intelligenz im Alltag',         '2026-12-02', '08:15:00', '08:45:00', 'R01'),
    ('Nachhaltige Energie in der Schweiz',       '2026-12-02', '08:15:00', '08:45:00', 'R02'),
    ('Digitale Privatsphäre bei Jugendlichen',   '2026-12-02', '09:00:00', '09:30:00', 'I01'),
    ('Mikroplastik in Schweizer Flüssen',        '2026-12-02', '09:45:00', '10:15:00', 'R01'),
    ('Einfluss von Social Media auf den Schlaf', '2026-12-02', '10:30:00', '11:00:00', 'R03');
GO

DECLARE @Schueler1Id UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @Schueler2Id UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';
DECLARE @Pr1Id INT = (SELECT id FROM mat_g4_praesentation WHERE thema = 'Künstliche Intelligenz im Alltag');
DECLARE @Pr3Id INT = (SELECT id FROM mat_g4_praesentation WHERE thema = 'Digitale Privatsphäre bei Jugendlichen');
DECLARE @Pr4Id INT = (SELECT id FROM mat_g4_praesentation WHERE thema = 'Mikroplastik in Schweizer Flüssen');

INSERT INTO mat_buchung (person_id, praesentation_id)
VALUES
    (@Schueler1Id, @Pr1Id),
    (@Schueler1Id, @Pr3Id),
    (@Schueler2Id, @Pr4Id);
GO