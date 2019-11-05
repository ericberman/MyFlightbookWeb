ALTER TABLE `logbook`.`users` 
ADD COLUMN `timezone` VARCHAR(45) NULL COMMENT 'ID for the preferred timezone for the user' AFTER `PropertyBlackList`;
