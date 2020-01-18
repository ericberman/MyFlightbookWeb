CREATE TABLE `logbook`.`AircraftInstanceTypes` (
  `ID` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Instance type ID',
  `Description` VARCHAR(45) NOT NULL DEFAULT '' COMMENT 'Human-readable name',
  `IsCertifiedIFR` BOOLEAN NOT NULL DEFAULT 0 COMMENT 'Certified for IFR approaches?',
  `IsCertifiedLanding` BOOLEAN NOT NULL DEFAULT 0 COMMENT 'Certified for landings',
  `IsFullMotion` BOOLEAN NOT NULL DEFAULT 0 COMMENT 'Full Motion Sim?',
  `FAASimLevel` CHAR ASCII NOT NULL DEFAULT '' COMMENT 'A, B, C, or D',
  PRIMARY KEY(`ID`)
)
ENGINE = InnoDB
COMMENT = 'Types of airplanes (real, simulator)';

insert into aircraftinstancetypes set Description='Real Airplane', IsCertifiedIFR=1, IsCertifiedLanding=1, IsFullMotion=1, FAASimLevel='';
insert into aircraftinstancetypes set Description='Simulator - Uncertified', IsCertifiedIFR=0, IsCertifiedLanding=0, IsFullMotion=0, FAASimLevel='';
insert into aircraftinstancetypes set Description='Simulator - Can log IFR approaches', IsCertifiedIFR=1, IsCertifiedLanding=0, IsFullMotion=0, FAASimLevel='';
insert into aircraftinstancetypes set Description='Simulator - Can log landings and approaches', IsCertifiedIFR=1, IsCertifiedLanding=1, IsFullMotion=1, FAASimLevel='D';

ALTER TABLE `logbook`.`aircraft` ADD COLUMN `InstanceType` INTEGER UNSIGNED NOT NULL DEFAULT 1 COMMENT 'InstanceType of this airplane (real vs. simulator)' AFTER `idmodel`;

delete from categoryclass where idCatClass=6

-- (Now: need to alter aircraft 8, 45, 46, and 67 to reference the appropriate instancetypes)

-- Add new ATD types:
ALTER TABLE `logbook`.`aircraftinstancetypes` ADD COLUMN `IsATD` TINYINT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Is this an ATD?' AFTER `FAASimLevel`;
ALTER TABLE `logbook`.`aircraftinstancetypes` MODIFY COLUMN `IsATD` TINYINT(1) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Is this an ATD?',
 ADD COLUMN `IsFTD` TINYINT(1) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Is this an FTD?' AFTER `IsATD`;

insert into aircraftinstancetypes set Description='Aviation Training Device (ATD)', IsCertifiedIFR=0, IsCertifiedLanding=0, IsFullMotion=0, FAASimLevel='', IsATD=1;
update aircraftinstancetypes set IsFTD=1 where ID=3 OR ID=4
update aircraftinstancetypes set IsCertifiedIFR=0, Description='Simulator/FTD - Can log IFR approaches' where ID=3
update aircraftinstancetypes set IsCertifiedIFR=0, Description='Simulator/FTD - Can log landings & approaches' where ID=4

update CustomPropertyTypes set Flags=4096 where Title like '%Approach%' and idPropType <> 25 AND idPropType<>31
