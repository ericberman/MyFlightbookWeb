ALTER TABLE `logbook`.`users` ADD COLUMN `UsesPerModelCurrency` TINYINT(1) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Do currency on a per-model basis' AFTER `UsesArmyCurrency`;

DROP TABLE IF EXISTS `logbook`.`customcurrency`;
CREATE TABLE  `logbook`.`customcurrency` (
  `idCurrency` int(10) unsigned NOT NULL AUTO_INCREMENT COMMENT 'Primary key',
  `username` varchar(255) NOT NULL COMMENT 'Owner of the custom currency',
  `name` varchar(45) NOT NULL COMMENT 'Name of the currency',
  `minEvents` int(10) unsigned NOT NULL COMMENT 'Number of events that must be counted',
  `timespan` int(10) unsigned NOT NULL COMMENT 'Timespan in which events must be found',
  `timespanType` int(10) unsigned NOT NULL COMMENT '0 = days, 1 = months',
  `eventType` int(10) unsigned NOT NULL COMMENT 'Type of event to count.',
  `modelRestriction` int(10) unsigned NOT NULL COMMENT 'If non-zero, only the modelID counts',
  `categoryRestriction` varchar(25) NOT NULL DEFAULT '""' COMMENT 'The category (e.g., "Airplane") to which this applies.',
  `catClassRestriction` int(10) unsigned NOT NULL COMMENT 'If non-zero, only the catclass matters',
  `aircraftRestriction` int(10) unsigned NOT NULL COMMENT 'If non-zero, only the aircraft matters',
  PRIMARY KEY (`idCurrency`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8 COMMENT='Custom currencies for a user';