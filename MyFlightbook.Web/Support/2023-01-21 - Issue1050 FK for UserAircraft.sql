ALTER TABLE `logbook`.`flights` 
ADD CONSTRAINT `fkuseraircraft`
  FOREIGN KEY (`idaircraft` , `username`)
  REFERENCES `logbook`.`useraircraft` (`idAircraft` , `userName`)
  ON DELETE RESTRICT
  ON UPDATE CASCADE;
