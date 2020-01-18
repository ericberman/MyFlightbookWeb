ALTER TABLE `logbook`.`models` 
ADD COLUMN `fTAA` TINYINT(1) NOT NULL COMMENT 'Indicates that this model is exclusively a Technically Advanced Aircraft per 61.129(j)' AFTER `fGlassOnly`;
ALTER TABLE `logbook`.`aircraft` 
ADD COLUMN `HasTAAUpgrade` TINYINT(1) UNSIGNED NOT NULL COMMENT 'Indicates that this specific aircraft has been upgraded to be TAA per 61.129(j)' AFTER `GlassUpgradeDate`;

update models set fTAA=1 where fTurbine=2 and fGlassonly<>0 and idmodel > 0;
update models set fTAA=1 where typename <> '' and fGlassOnly<>0 and ftaa=0 and idmodel > 0;