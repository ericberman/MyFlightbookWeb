CREATE TABLE `propertytemplate` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(45) NOT NULL COMMENT 'Name for the property template',
  `description` varchar(255) DEFAULT NULL COMMENT 'Description of this template',
  `owner` varchar(255) DEFAULT NULL COMMENT 'Owner for the template, blank for public',
  `originalowner` varchar(255) DEFAULT NULL COMMENT 'Original owner (publisher) of the template, if it''s public',
  `templategroup` int(11) NOT NULL COMMENT 'Identifier for a template group',
  `properties` text NOT NULL COMMENT 'comma separated list of property id''s.',
  `public` tinyint(1) NOT NULL COMMENT 'Is this visible to other users?',
  PRIMARY KEY (`id`),
  KEY `templateowner_idx` (`owner`),
  CONSTRAINT `templateowner` FOREIGN KEY (`owner`) REFERENCES `users` (`Username`) ON DELETE CASCADE ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8 COMMENT='Contains a named list of properties that are a template for a given kind of flight';

ALTER TABLE `logbook`.`propertytemplate` 
ADD COLUMN `isdefault` TINYINT(1) NOT NULL COMMENT 'Indicates if this template should be used by default when others are not specified' AFTER `public`;

ALTER TABLE `logbook`.`useraircraft` 
ADD COLUMN `TemplateIDs` TEXT NULL COMMENT 'JSONified array of ID\'s of templates to use for the aircraft' AFTER `DefaultImage`;

ALTER TABLE `logbook`.`useraircraft` 
DROP COLUMN `id`,
DROP PRIMARY KEY,
ADD PRIMARY KEY USING BTREE (`userName`, `idAircraft`);
;
