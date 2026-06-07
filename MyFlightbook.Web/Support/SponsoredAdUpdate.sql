ALTER TABLE `logbook`.`sponsoredads` 
ADD COLUMN `promoTextMarkup` VARCHAR(1024) NULL COMMENT 'Any promotional text, using markup' AFTER `ClickCount`,
ADD COLUMN `promoHeader` VARCHAR(128) NULL COMMENT 'Optional Header for a promo' AFTER `promoTextMarkup`,
ADD COLUMN `preferredImgWidth` INT NULL DEFAULT 0 AFTER `promoHeader`,
ADD COLUMN `preferredImgHeight` INT NULL DEFAULT 0 AFTER `preferredImgWidth`;
