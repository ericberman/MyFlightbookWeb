ALTER TABLE `logbook`.`categoryclass` ADD COLUMN `AltCatClass` INTEGER UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Secondary category/class for airplanes that are part-time on floats or amphib' AFTER `Type`;

update categoryclass set altcatclass=3 where idCatClass=1;
update categoryclass set altcatclass=1 where idCatClass=3;
update categoryclass set altcatclass=2 where idCatClass=4;
update categoryclass set altcatclass=4 where idCatClass=2;
update categoryclass set altcatclass=14 where idCatClass=13;
update categoryclass set altcatclass=13 where idCatClass=14;
update categoryclass set altcatclass=16 where idCatClass=15;
update categoryclass set altcatclass=15 where idCatClass=16;

ALTER TABLE `logbook`.`flights` ADD COLUMN `idCatClassOverride` INTEGER UNSIGNED NOT NULL DEFAULT 0 COMMENT 'ID of overriding category/class for the flight.';

ALTER TABLE `logbook`.`categoryclass` DROP COLUMN `Type` ;
INSERT INTO `logbook`.`categoryclass` (`idCatClass`, `CatClass`, `Category`, `Class`, `AltCatClass`) 
VALUES (17, 'Unmanned Aerial System', 'Unmanned Aerial System', '', 0);
