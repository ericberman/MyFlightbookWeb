/* Image endorsements: confidence from the vision model, indexes for the absorbed-image join, and the scan-marker table */

ALTER TABLE `logbook`.`endorsements`
ADD COLUMN `Confidence` TINYINT UNSIGNED NULL DEFAULT NULL COMMENT 'For Kind=image with CategorySource=Model: model confidence 0-100.  NULL if not applicable.' AFTER `TemplateID`,
ADD INDEX `idxStudentKind` (`Student`, `Kind`, `DateDeleted`),
ADD INDEX `idxImageThumbStudent` (`Kind`, `StudentType`, `Student`, `ThumbFileName`),
ADD INDEX `idxImageThumbCFI` (`Kind`, `StudentType`, `CFI`, `ThumbFileName`);

/* Records images that were sent to the model and yielded nothing (or whose image endorsements were all deleted), so that
   "Categorize images" doesn't keep re-sending them.  Keyed like the images table. */
CREATE TABLE `logbook`.`endorsementimagescans` (
  `VirtPathID` INT NOT NULL COMMENT 'Image class: 2 = Endorsement, 4 = OfflineEndorsement',
  `ImageKey` VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT 'Owner username (student for endorsements, instructor for offline endorsements)',
  `ThumbFilename` VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `Status` TINYINT NOT NULL DEFAULT 1 COMMENT '1 = scanned, nothing found; 2 = image endorsements deleted by the user',
  `DateScanned` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`VirtPathID`, `ImageKey`, `ThumbFilename`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Scan markers for image endorsements';

/* Optional: model to use for reading endorsement images.  Empty = use AnthropicScanModel (or the built-in default).
   Present so that LocalConfig doesn't reload the table on every lookup of a missing key. */
INSERT IGNORE INTO `logbook`.`localconfig` (`keyName`, `keyValue`) VALUES ('AnthropicEndorsementModel', '');
