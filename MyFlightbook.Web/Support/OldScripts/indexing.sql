ALTER TABLE `logbook`.`aircraft` DROP PRIMARY KEY,
 ADD PRIMARY KEY  USING BTREE(`idaircraft`, `tailnumber`, `idmodel`);

ALTER TABLE `logbook`.`aircraft` DROP PRIMARY KEY,
 ADD PRIMARY KEY  USING BTREE(`idaircraft`, `tailnumber`, `idmodel`, `InstanceType`);

ALTER TABLE `logbook`.`flightproperties` DROP PRIMARY KEY,
 ADD PRIMARY KEY  USING BTREE(`idProp`, `idFlight`, `idPropType`);

ALTER TABLE `logbook`.`flights` DROP PRIMARY KEY,
 ADD PRIMARY KEY  USING BTREE(`idFlight`, `date`, `idaircraft`, `username`);

ALTER TABLE `logbook`.`maintenancelog` DROP PRIMARY KEY,
 ADD PRIMARY KEY  USING BTREE(`id`, `idAircraft`, `User`);

ALTER TABLE `logbook`.`models` DROP PRIMARY KEY,
 ADD PRIMARY KEY  USING BTREE(`idmodel`, `idcategoryclass`, `idmanufacturer`);

ALTER TABLE `logbook`.`useraircraft` DROP PRIMARY KEY,
 ADD PRIMARY KEY  USING BTREE(`id`, `userName`, `idAircraft`);

ALTER TABLE `logbook`.`users` DROP PRIMARY KEY,
 ADD PRIMARY KEY  USING BTREE(`PKID`, `Username`, `Email`);
