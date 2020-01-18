ALTER TABLE `logbook`.`users` ADD COLUMN `OverwriteDropbox` TINYINT(1) UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Overwrite dropbox backup or add new one each time?'  AFTER `DropboxAccesstoken` ;
