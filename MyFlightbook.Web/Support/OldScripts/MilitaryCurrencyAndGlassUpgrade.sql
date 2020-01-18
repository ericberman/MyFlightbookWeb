ALTER TABLE `logbook`.`aircraft` ADD COLUMN `HasGlassUpgrade` BOOLEAN UNSIGNED NOT NULL DEFAULT 0  COMMENT 'If true, the aircraft has glass regardless of underlying model default' AFTER `LastEngine`;

ALTER TABLE `logbook`.`users` ADD COLUMN `UsesArmyCurrency` BOOLEAN UNSIGNED NOT NULL DEFAULT 0  AFTER `CFIExpiration`;

ALTER TABLE `logbook`.`models` ADD COLUMN `ArmyMissionDesignSeries` VARCHAR(45) AFTER `fTAA`
, ROW_FORMAT = DYNAMIC;

update custompropertytypes set Flags=32768 WHERE idproptype=68;
update custompropertytypes set Flags=32768 WHERE idproptype=26;


/* Set up for glass cockpit defaults, fix up comments/etc. */
ALTER TABLE `logbook`.`models` 
ADD COLUMN `fGlassOnly` TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'True if ALL aircraft of this type are glass cockpit'  AFTER `fSimOnly` , 
CHANGE COLUMN `fHighPerf` `fHighPerf` TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'True for high performance'  , 
CHANGE COLUMN `fTailwheel` `fTailwheel` TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'True for tailwheel'  , 
CHANGE COLUMN `fConstantProp` `fConstantProp` TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'True for constant speed prop airplanes'  , 
CHANGE COLUMN `fTurbine` `fTurbine` TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'True for turbine'  , 
CHANGE COLUMN `fRetract` `fRetract` TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'True for retract'  , 
CHANGE COLUMN `fCowlFlaps` `fCowlFlaps` TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Bad name - true if the aircraft has FLAPS, not COWL FLAPS'  , 
CHANGE COLUMN `fTAA` `fTAA` TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'OBSOLETE - Technically Advanced Aircraft'  ;

/* Do this AFTER TAA has been removed from the code */
ALTER TABLE `logbook`.`models` DROP COLUMN `fTAA` ;


ALTER TABLE `logbook`.`models` ADD COLUMN `fMotorGlider` TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'True if this is a motor glider (only applies to gliders)'  AFTER `fGlassOnly` ;

ALTER TABLE `logbook`.`models` CHANGE COLUMN `fTurbine` `fTurbine` TINYINT(2) NOT NULL DEFAULT 0 COMMENT 'Non-zero for turbine; 1=Turboprop, 2=Jet'  ;

update models set fturbine = 3 WHERE fturbine <> 0;

ALTER TABLE `logbook`.`models` ADD COLUMN `fMultiHelicopter` TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'For helicopters, is this a multi-engine helicopter?'  AFTER `fMotorGlider` ;
