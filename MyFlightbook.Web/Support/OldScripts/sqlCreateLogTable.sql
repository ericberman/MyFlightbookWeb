CREATE TABLE `logbook`.`WSEvents` (
  `eventID` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Date` DATETIME NOT NULL,
  `eventType` INTEGER UNSIGNED NOT NULL DEFAULT 0,
  `user` VARCHAR(255) NOT NULL,
  `description` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`eventID`)
)
ENGINE = InnoDB
COMMENT = 'Log of actions taken with web service';
delimiter $$

CREATE TABLE `eventcounts` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `WSCommittedFlights` int(11) DEFAULT NULL,
  `ImportedFlights` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8 COMMENT='Table of scalar stats for the site'


Insert into logbook.eventcounts set WSCommittedFlights=0, ImportedFlights=0, id=1;
