INSERT INTO `logbook`.`localconfig` (`keyName`, `keyValue`) VALUES ('TachTimeClientID', 'xxx');
INSERT INTO `logbook`.`localconfig` (`keyName`, `keyValue`) VALUES ('TachTimeClientSecret', 'yyy');
INSERT INTO `logbook`.`localconfig` (`keyName`, `keyValue`) VALUES ('TachTimeClientIDSandbox', 'yyy');
INSERT INTO `logbook`.`localconfig` (`keyName`, `keyValue`) VALUES ('TachTimeClientSecretSandbox', 'zzz');

CREATE TABLE `externalmaintenance` (
  `idaircraft` int unsigned NOT NULL COMMENT 'The id (from the aircraft table) of the aircraft for which this maintenance record applies.',
  `username` varchar(255) CHARACTER SET utf8mb3 COLLATE utf8mb3_general_ci NOT NULL COMMENT 'The username of the owning user',
  `sourceID` int NOT NULL COMMENT 'The ID of the external source; e.g., 1 for tachtime',
  `jsonData` json NOT NULL,
  `highWaterTach` decimal(10,2) DEFAULT '0.00' COMMENT 'highWaterTach, if known, at the time that this record was retrieved',
  `highWaterHobbs` decimal(10,2) DEFAULT '0.00' COMMENT 'High watermark hobbs, if known, at the time this record was retrieved',
  `lastUpdated` datetime NOT NULL COMMENT 'Timestamp that this record was last updated',
  PRIMARY KEY (`idaircraft`, `username`, `sourceID`),
  KEY `aircraft_idx` (`idaircraft`) /*!80000 INVISIBLE */,
  KEY `user_idx` (`username`),
  CONSTRAINT `fkAircraftMaintID` FOREIGN KEY (`idaircraft`) REFERENCES `aircraft` (`idaircraft`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `fkUsernameMaintID` FOREIGN KEY (`username`) REFERENCES `users` (`Username`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Record of maintenance from external aircraft logbook sources';

