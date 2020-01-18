DELIMITER $$

DROP PROCEDURE IF EXISTS `logbook`.`GetUserName` $$
CREATE PROCEDURE `logbook`.`GetUserName` (in szUser varchar(255), out givenname varchar(32), out surname varchar(32))
BEGIN
  select FirstName from users where Users.username = szUser into givenname;
  select LastName from users where users.username = szUser into surname;
END $$

DELIMITER ;