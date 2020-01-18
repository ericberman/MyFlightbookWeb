CREATE  TABLE `logbook`.`FAQ` (
  `idFAQ` INT NOT NULL AUTO_INCREMENT ,
  `Category` VARCHAR(45) NULL COMMENT 'Category for the FAQ (for grouping)' ,
  `Question` VARCHAR(80) NULL ,
  `Answer` TEXT NULL ,
  PRIMARY KEY (`idFAQ`) ,
  UNIQUE INDEX `idFAQ_UNIQUE` (`idFAQ` ASC) )
COMMENT = 'Table for FAQs';
