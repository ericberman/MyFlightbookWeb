CREATE  TABLE `logbook`.`LocText` (
  `idLocText` INT NOT NULL AUTO_INCREMENT COMMENT 'Id of the row' ,
  `idTableID` INT NOT NULL COMMENT 'ID of the table for which the translations are held (just to avoid collisions' ,
  `idItemID` INT NOT NULL COMMENT 'ID of the item within the table for which this holds a translation' ,
  `LangID` VARCHAR(2) NOT NULL DEFAULT 'en' COMMENT 'The language that this text is in' ,
  `Text` VARCHAR(255) NULL COMMENT 'The translated text' ,
  PRIMARY KEY (`idLocText`, `idTableID`, `idItemID`, `LangID`) )
COMMENT = 'Localized text for strings in the database';

