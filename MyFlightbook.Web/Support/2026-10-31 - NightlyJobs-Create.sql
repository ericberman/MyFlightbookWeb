-- Nightly run job queue (see NightlyJobs.cs).  One row per unit of work per run; makes the nightly run resumable.
CREATE TABLE `nightlyjobs` (
  `id` int NOT NULL AUTO_INCREMENT,
  `runKey` varchar(300) NOT NULL COMMENT 'Identifies the run: yyyy-MM-dd, plus task/user suffixes for partial or debug runs',
  `runDate` date NOT NULL COMMENT 'Date the run is for',
  `jobType` varchar(40) NOT NULL COMMENT 'NightlyJobType enum name',
  `username` varchar(255) NOT NULL DEFAULT '' COMMENT 'User the job is for, empty for run-wide jobs',
  `priority` int NOT NULL DEFAULT '0' COMMENT 'Lower runs first',
  `payload` varchar(255) DEFAULT NULL COMMENT 'Job-specific data captured at planning time',
  `status` enum('pending','running','done','failed') NOT NULL DEFAULT 'pending',
  `attempts` int NOT NULL DEFAULT '0' COMMENT 'Number of times the job has been claimed',
  `owner` char(36) DEFAULT NULL COMMENT 'GUID of the current claim',
  `leaseExpires` datetime DEFAULT NULL COMMENT 'A running job whose lease has expired is presumed dead and can be reclaimed',
  `dateCreated` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `dateCompleted` datetime DEFAULT NULL,
  `result` mediumtext COMMENT 'Success text for the report',
  `failures` mediumtext COMMENT 'Failure text for the report',
  PRIMARY KEY (`id`),
  UNIQUE KEY `runjobuser` (`runKey`,`jobType`,`username`),
  KEY `claim` (`runKey`,`status`,`priority`,`id`),
  KEY `rundate` (`runDate`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Work queue for the nightly run';

ALTER TABLE nightlyjobs MODIFY `owner` varchar(36) DEFAULT NULL COMMENT 'GUID of the current claim';