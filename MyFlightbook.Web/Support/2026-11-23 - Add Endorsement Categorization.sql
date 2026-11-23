ALTER TABLE `logbook`.`endorsementtemplates` 
ADD COLUMN `DefaultCategory` INT NOT NULL DEFAULT 0 COMMENT 'The default category/enum for this template' AFTER `Text`;

ALTER TABLE `logbook`.`endorsements` 
ADD COLUMN `Category` INT NOT NULL DEFAULT 0 COMMENT 'The category for the endorsement' AFTER `DigitizedSignature`,
ADD COLUMN `CategorySource` TINYINT NOT NULL DEFAULT 0 COMMENT '0 = None, 1 = Template, 2 = rule, 3 = Model, 4 = User' AFTER `Category`,
ADD COLUMN `Kind` TINYINT NOT NULL DEFAULT 0 COMMENT '0 = regular, 1 = image' AFTER `CategorySource`,
ADD COLUMN `ThumbFileName` VARCHAR(255) NULL DEFAULT NULL COMMENT 'For kind=image, has the thumbnail file to use.  Could be an offlineendorsement image or an endorsement image, depending on studenttype' AFTER `Kind`,
ADD COLUMN `PageNumber` SMALLINT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'For image endorsements from a PDF - the page the endorsement came from' AFTER `ThumbFileName`,
ADD COLUMN `RawText` TEXT NULL COMMENT 'For type=image, contains the raw OCR output' AFTER `PageNumber`,
ADD COLUMN `TemplateID` INT UNSIGNED NOT NULL DEFAULT 0 COMMENT '0 = unknown, else ID of the original template' AFTER `RawText`;

/* Update the template table with default categories */
UPDATE endorsementtemplates SET DefaultCategory = CASE id
  WHEN 1  THEN 0    -- Custom: classified later by rules
  WHEN 2  THEN 401  -- FlightReview
  WHEN 3  THEN 201  -- KnowledgeTestPrep (61.125)
  WHEN 4  THEN 101  -- PresoloKnowledge
  WHEN 5  THEN 102  -- PresoloFlight
  WHEN 6  THEN 103  -- Solo (additional 90 days)
  WHEN 7  THEN 104  -- SoloLocalAndAirspace (25nm)
  WHEN 8  THEN 105  -- SoloCrossCountry (planning)
  WHEN 9  THEN 105  -- SoloCrossCountry (repeated <50nm)
  WHEN 10 THEN 104  -- SoloLocalAndAirspace (Class B)
  WHEN 11 THEN 104  -- SoloLocalAndAirspace (airport in Class B)
  WHEN 12 THEN 201  -- KnowledgeTestPrep (PP aero knowledge)
  WHEN 13 THEN 203  -- PracticalTestPrep (PP flight proficiency)
  WHEN 14 THEN 303  -- Tailwheel
  WHEN 15 THEN 301  -- Complex
  WHEN 16 THEN 302  -- HighPerformance
  WHEN 17 THEN 308  -- LightSport (sport B/C/D)
  WHEN 18 THEN 308  -- LightSport (VH > 87)
  WHEN 19 THEN 308  -- LightSport (VH <= 87)
  WHEN 20 THEN 201  -- KnowledgeTestPrep
  WHEN 21 THEN 103  -- Solo (first 90 days)
  WHEN 22 THEN 104  -- SoloLocalAndAirspace (61.94)
  WHEN 23 THEN 203  -- PracticalTestPrep (PP)
  WHEN 24 THEN 203  -- PracticalTestPrep (sport add'l cat/class)
  WHEN 25 THEN 205  -- TestResult (sport passed add'l)
  WHEN 26 THEN 203  -- PracticalTestPrep (sport)
  WHEN 27 THEN 205  -- TestResult (sport passed)
  WHEN 28 THEN 501  -- WingsActivity
  WHEN 29 THEN 502  -- WingsPhase
  WHEN 30 THEN 402  -- InstrumentProficiencyCheck
  WHEN 31 THEN 304  -- HighAltitude (pressurized ground)
  WHEN 32 THEN 304  -- HighAltitude (pressurized flight)
  WHEN 33 THEN 201  -- KnowledgeTestPrep (instrument ground)
  WHEN 34 THEN 203  -- PracticalTestPrep (61.39)
  WHEN 35 THEN 203  -- PracticalTestPrep (instrument flight instruction)
  WHEN 36 THEN 203  -- PracticalTestPrep (commercial proficiency)
  WHEN 37 THEN 601  -- SpinTraining
  WHEN 38 THEN 701  -- Citizenship
  WHEN 39 THEN 204  -- Retest
  WHEN 40 THEN 203  -- PracticalTestPrep (add'l ratings)
  WHEN 41 THEN 310  -- TypeAndDifferences (SFAR 73 ground)
  WHEN 42 THEN 103  -- Solo (R22)
  WHEN 43 THEN 103  -- Solo (R44)
  WHEN 44 THEN 310  -- TypeAndDifferences (R22 PIC)
  WHEN 45 THEN 310  -- TypeAndDifferences (R44 PIC)
  WHEN 46 THEN 602  -- InstructorQualification (R22/R44 instructor)
  WHEN 47 THEN 602  -- InstructorQualification (NVG instructor)
  WHEN 48 THEN 307  -- NightVisionGoggles (ground)
  WHEN 49 THEN 307  -- NightVisionGoggles (flight)
  WHEN 50 THEN 403  -- ProficiencyCheck (NVG)
  WHEN 51 THEN 103  -- Solo (night)
  WHEN 52 THEN 203  -- PracticalTestPrep (commercial)
  WHEN 53 THEN 203  -- PracticalTestPrep (instrument)
  WHEN 54 THEN 202  -- KnowledgeTestDeficiencies
  WHEN 55 THEN 103  -- Solo (additional 90 days)
  WHEN 56 THEN 105  -- SoloCrossCountry (training)
  WHEN 57 THEN 203  -- PracticalTestPrep (recreational)
  WHEN 58 THEN 104  -- SoloLocalAndAirspace (rec within 50nm)
  WHEN 59 THEN 105  -- SoloCrossCountry (rec beyond 50nm)
  WHEN 60 THEN 106  -- RecreationalPrivileges (180-day)
  WHEN 61 THEN 106  -- RecreationalPrivileges (solo add'l rating)
  WHEN 62 THEN 104  -- SoloLocalAndAirspace (rec B/C/D)
  WHEN 63 THEN 206  -- CertificationTrainingProgram (R-ATP academic)
  WHEN 64 THEN 206  -- CertificationTrainingProgram (ATP CTP)
  WHEN 65 THEN 201  -- KnowledgeTestPrep (FOI)
  WHEN 66 THEN 201  -- KnowledgeTestPrep (CFI knowledge)
  WHEN 67 THEN 203  -- PracticalTestPrep (CFI)
  WHEN 68 THEN 203  -- PracticalTestPrep (CFII)
  WHEN 69 THEN 201  -- KnowledgeTestPrep (sport CFI FOI)
  WHEN 70 THEN 203  -- PracticalTestPrep (sport CFI add'l)
  WHEN 71 THEN 205  -- TestResult (sport CFI add'l passed)
  WHEN 72 THEN 203  -- PracticalTestPrep (sport CFI)
  WHEN 73 THEN 205  -- TestResult (sport CFI passed)
  WHEN 74 THEN 601  -- SpinTraining (sport CFI)
  WHEN 75 THEN 603  -- GroundInstructorRecency
  WHEN 76 THEN 305  -- WithoutRating (61.31(d)(2))
  WHEN 77 THEN 203  -- PracticalTestPrep (type + cat/class)
  WHEN 78 THEN 203  -- PracticalTestPrep (type only, ATP)
  WHEN 79 THEN 203  -- PracticalTestPrep (type + cat/class, ATP)
  WHEN 80 THEN 306  -- GliderAndTowing (launch)
  WHEN 81 THEN 201  -- KnowledgeTestPrep (home study)
  WHEN 82 THEN 203  -- PracticalTestPrep (experimental 61.63(h))
  WHEN 83 THEN 203  -- PracticalTestPrep (experimental 61.165(f))
  WHEN 84 THEN 312  -- OperationalAuthorization (ultralight credit)
  WHEN 85 THEN 312  -- OperationalAuthorization (90-day)
  WHEN 86 THEN 202  -- KnowledgeTestDeficiencies (instrument)
  WHEN 87 THEN 702  -- SecurityAwareness
  WHEN 88 THEN 702  -- SecurityAwareness (independent CFI)
  WHEN 89 THEN 702  -- SecurityAwareness (school employee)
  WHEN 90 THEN 306  -- GliderAndTowing (towing experience)
  WHEN 91 THEN 306  -- GliderAndTowing (towing training)
  WHEN 92 THEN 205  -- TestResult (record of test result)
  WHEN 93 THEN 309  -- SecondInCommand (61.159(c))
  WHEN 94 THEN 309  -- SecondInCommand (61.55)
  WHEN 95 THEN 312  -- OperationalAuthorization (oceanic RNP)
  WHEN 96 THEN 105  -- SoloCrossCountry (instructor review)
  WHEN 97 THEN 203  -- PracticalTestPrep (PP Part 141)
  WHEN 98 THEN 308  -- LightSport (sport night)
  ELSE DefaultCategory END
WHERE id BETWEEN 1 AND 98;

-- Workbench safe-update mode will block these; either SET SQL_SAFE_UPDATES = 0;
-- or keep the "e.id > 0" predicates below.

-- Preview: how many rows each pass would touch
SELECT COUNT(*) FROM endorsements e
  JOIN endorsementtemplates t ON t.Title = e.Title AND t.FARRef = e.FARRef
 WHERE e.CategorySource = 0 AND t.id <> 1;

-- Pass 1: exact title + FARRef
UPDATE endorsements e
  JOIN endorsementtemplates t ON t.Title = e.Title AND t.FARRef = e.FARRef
   SET e.Category = t.DefaultCategory, e.CategorySource = 1, e.TemplateID = t.id
 WHERE e.id > 0 AND e.CategorySource = 0 AND t.id <> 1;

-- Pass 2: title only (template titles are unique), also stripping
-- the ": FAR ..." suffix some clients appended
UPDATE endorsements e
  JOIN endorsementtemplates t
    ON t.Title = TRIM(SUBSTRING_INDEX(e.Title, ': FAR ', 1))
   SET e.Category = t.DefaultCategory, e.CategorySource = 2, e.TemplateID = t.id
 WHERE e.id > 0 AND e.CategorySource = 0 AND t.id <> 1;

-- Pass 3: templates that were renamed at some point
UPDATE endorsements e
  JOIN (SELECT 'Flight Instruction before Test' AS OldTitle, 34 AS TemplateID
        UNION ALL SELECT 'Review of deficiencies identified on airman knowledge test:', 54
        UNION ALL SELECT 'SFAR 73 Awareness Training', 41
        UNION ALL SELECT 'Flight Proficiency required for Practical Test', 23
        UNION ALL SELECT 'WINGS Tasks Completed', 28) a
    ON a.OldTitle = TRIM(SUBSTRING_INDEX(e.Title, ': FAR ', 1))
  JOIN endorsementtemplates t ON t.id = a.TemplateID
   SET e.Category = t.DefaultCategory, e.CategorySource = 2, e.TemplateID = t.id
 WHERE e.id > 0 AND e.CategorySource = 0;