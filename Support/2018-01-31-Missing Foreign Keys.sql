﻿ALTER TABLE `logbook`.`aircraft` 
DROP FOREIGN KEY `model`,
DROP FOREIGN KEY `instance`;
ALTER TABLE `logbook`.`aircraft` 
ADD CONSTRAINT `fkaircraftmodel`
  FOREIGN KEY (`idmodel`)
  REFERENCES `logbook`.`models` (`idmodel`)
  ON DELETE RESTRICT
  ON UPDATE NO ACTION,
ADD CONSTRAINT `fkaircraftinstance`
  FOREIGN KEY (`InstanceType`)
  REFERENCES `logbook`.`aircraftinstancetypes` (`ID`)
  ON DELETE RESTRICT
  ON UPDATE NO ACTION;

  ALTER TABLE `logbook`.`aircrafttombstones` 
CHANGE COLUMN `idMappedAircraft` `idMappedAircraft` INT(10) UNSIGNED NOT NULL COMMENT 'The ID of the aircraft with which it was merged' ;

ALTER TABLE `logbook`.`aircrafttombstones` 
ADD CONSTRAINT `mappedaircraft`
  FOREIGN KEY (`idMappedAircraft`)
  REFERENCES `logbook`.`aircraft` (`idaircraft`)
  ON DELETE NO ACTION
  ON UPDATE NO ACTION;

ALTER TABLE `logbook`.`badges` 
ADD INDEX `Users_idx` (`Username` ASC);
ALTER TABLE `logbook`.`badges` 
ADD CONSTRAINT `fkBadgesUsers`
  FOREIGN KEY (`Username`)
  REFERENCES `logbook`.`users` (`Username`)
  ON DELETE CASCADE
  ON UPDATE NO ACTION;

ALTER TABLE `logbook`.`clubmembers` 
ADD CONSTRAINT `fkuser`
  FOREIGN KEY (`username`)
  REFERENCES `logbook`.`users` (`Username`)
  ON DELETE CASCADE
  ON UPDATE NO ACTION;

ALTER TABLE `logbook`.`customcurrency` 
ADD CONSTRAINT `fkcustomcurrencyuser`
  FOREIGN KEY (`username`)
  REFERENCES `logbook`.`users` (`Username`)
  ON DELETE CASCADE
  ON UPDATE NO ACTION;

ALTER TABLE `logbook`.`deadlines` 
ADD CONSTRAINT `fkdeadlineuser`
  FOREIGN KEY (`username`)
  REFERENCES `logbook`.`users` (`Username`)
  ON DELETE CASCADE
  ON UPDATE NO ACTION;

ALTER TABLE `logbook`.`earnedgratuities` 
ADD CONSTRAINT `fkearnedgratuityuser`
  FOREIGN KEY (`username`)
  REFERENCES `logbook`.`users` (`Username`)
  ON DELETE CASCADE
  ON UPDATE NO ACTION;

ALTER TABLE `logbook`.`flightproperties` 
ADD CONSTRAINT `fkPropType`
  FOREIGN KEY (`idPropType`)
  REFERENCES `logbook`.`custompropertytypes` (`idPropType`)
  ON DELETE RESTRICT
  ON UPDATE NO ACTION;

ALTER TABLE `logbook`.`flights` 
ADD CONSTRAINT `fkFlightAircraft`
  FOREIGN KEY (`idaircraft`)
  REFERENCES `logbook`.`aircraft` (`idaircraft`)
  ON DELETE RESTRICT
  ON UPDATE NO ACTION;

ALTER TABLE `logbook`.`models` 
ADD CONSTRAINT `fkmodelsMan`
  FOREIGN KEY (`idmanufacturer`)
  REFERENCES `logbook`.`manufacturers` (`idManufacturer`)
  ON DELETE RESTRICT
  ON UPDATE NO ACTION,
ADD CONSTRAINT `fkmodelscatclass`
  FOREIGN KEY (`idcategoryclass`)
  REFERENCES `logbook`.`categoryclass` (`idCatClass`)
  ON DELETE NO ACTION
  ON UPDATE NO ACTION;

ALTER TABLE `logbook`.`students` 
ADD INDEX `fkCFIName_idx` (`CFIName` ASC),
ADD INDEX `fkStudenName_idx` (`StudentName` ASC);
ALTER TABLE `logbook`.`students` 
ADD CONSTRAINT `fkStudenName`
  FOREIGN KEY (`StudentName`)
  REFERENCES `logbook`.`users` (`Username`)
  ON DELETE CASCADE
  ON UPDATE NO ACTION,
ADD CONSTRAINT `fkCFIName`
  FOREIGN KEY (`CFIName`)
  REFERENCES `logbook`.`users` (`Username`)
  ON DELETE CASCADE
  ON UPDATE NO ACTION;

ALTER TABLE `logbook`.`useraircraft` 
ADD INDEX `fkuseraircraftUName_idx` (`userName` ASC);
ALTER TABLE `logbook`.`useraircraft` 
ADD CONSTRAINT `fkuseraircraftUName`
  FOREIGN KEY (`userName`)
  REFERENCES `logbook`.`users` (`Username`)
  ON DELETE CASCADE
  ON UPDATE NO ACTION,
ADD CONSTRAINT `fkuseraircraftAc`
  FOREIGN KEY (`idAircraft`)
  REFERENCES `logbook`.`aircraft` (`idaircraft`)
  ON DELETE RESTRICT
  ON UPDATE NO ACTION;

ALTER TABLE `logbook`.`usersinroles` 
ADD CONSTRAINT `fkusersinrolesUsername`
  FOREIGN KEY (`Username`)
  REFERENCES `logbook`.`users` (`Username`)
  ON DELETE CASCADE
  ON UPDATE NO ACTION;


ALTER TABLE `logbook`.`flights` 
ADD CONSTRAINT `fkFlightUser`
  FOREIGN KEY (`username`)
  REFERENCES `logbook`.`users` (`Username`)
  ON DELETE CASCADE
  ON UPDATE NO ACTION;

  ALTER TABLE `logbook`.`roles` 
ADD INDEX `rolenameidx` (`Rolename` ASC);

ALTER TABLE `logbook`.`usersinroles` 
ADD CONSTRAINT `fkusersinrolesrole`
  FOREIGN KEY (`Rolename`)
  REFERENCES `logbook`.`roles` (`Rolename`)
  ON DELETE RESTRICT
  ON UPDATE NO ACTION;

ALTER TABLE `logbook`.`passwordresetrequests` 
ADD CONSTRAINT `fkprrUsername`
  FOREIGN KEY (`username`)
  REFERENCES `logbook`.`users` (`Username`)
  ON DELETE CASCADE
  ON UPDATE NO ACTION;
