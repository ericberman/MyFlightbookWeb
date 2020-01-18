ALTER TABLE `logbook`.`manufacturers` ADD COLUMN `DefaultSim` BOOLEAN NOT NULL DEFAULT 0 COMMENT 'If true, then models created with this manufacturer default to sim-only' AFTER `manufacturer`;
ALTER TABLE `logbook`.`models` ADD COLUMN `fSimOnly` BOOLEAN NOT NULL DEFAULT 0 COMMENT 'If true, all aircraft with this make/model MUST NOT be real-aircraft' AFTER `ArmyMissionDesignSeries`;

ALTER TABLE `logbook`.`manufacturers` CHANGE COLUMN `DefaultSim` `DefaultSim` INT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'default sim mode: 0 = any, 1 = sim only, 2= sim or generic'  ;
ALTER TABLE `logbook`.`models` CHANGE COLUMN `fSimOnly` `fSimOnly` INT NOT NULL DEFAULT '0' COMMENT 'If 1, all aircraft with this make/model MUST NOT be real-aircraft.  If 2, can also be generic but real. '  ;

