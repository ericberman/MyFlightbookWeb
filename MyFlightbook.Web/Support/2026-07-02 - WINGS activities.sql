CREATE TABLE `wingsactivities` (
  `AccreditedActivityID` int NOT NULL,
  `ActivityNumber` varchar(255) NOT NULL,
  `ActivityName` varchar(1024) NOT NULL,
  `SyllabiNumbersString` varchar(1024) NOT NULL,
  PRIMARY KEY (`AccreditedActivityID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
