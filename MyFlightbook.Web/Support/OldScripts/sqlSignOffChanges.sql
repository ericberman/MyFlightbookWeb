CREATE TABLE `logbook`.`Students` (
  `id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `StudentName` VARCHAR(45) NOT NULL COMMENT 'Username of the student',
  `CFIName` VARCHAR(45) NOT NULL COMMENT 'Username of the Instructor',
  PRIMARY KEY (`id`)
)
ENGINE = InnoDB
COMMENT = 'Mapping of students to CFIs';


ALTER TABLE `logbook`.`users` ADD COLUMN `CertificateNumber` VARCHAR(90) NOT NULL COMMENT 'Pilot certificate number (for instructors)' AFTER `FacebookAccessToken`;
ALTER TABLE `logbook`.`users` ADD COLUMN `CFIExpiration` DATETIME COMMENT 'Expiration date of certificate' AFTER `CertificateNumber`;

CREATE TABLE  `logbook`.`endorsementtemplates` (
  `id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `Title` varchar(90) NOT NULL COMMENT 'Title of the endorsement',
  `FARRef` varchar(45) NOT NULL COMMENT 'FAR which is referenced',
  `Text` text NOT NULL COMMENT 'Body of the template, with placeholders',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

CREATE TABLE  `logbook`.`endorsements` (
  `id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `CFI` varchar(255) NOT NULL COMMENT 'Username of the CFI',
  `Student` varchar(255) NOT NULL COMMENT 'Username of the student',
  `Date` datetime NOT NULL COMMENT 'Date of the endorsement',
  `CFINum` varchar(45) NOT NULL COMMENT 'CFI Certificate Number',
  `CFIExpiration` datetime NOT NULL COMMENT 'Date of CFI Expiration',
  `Endorsement` text NOT NULL COMMENT 'The text of the endorsement',
  `Title` varchar(90) NOT NULL COMMENT 'The title of the endorsement',
  `FARRef` varchar(45) NOT NULL COMMENT 'The FAR which references the endorsement',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COMMENT='Contains endorsements of students by CFIs';

INSERT INTO `endorsementtemplates` (`id`,`Title`,`FARRef`,`Text`) VALUES 
 (1,'Custom Endorsement','','{FreeForm}'),
 (2,'Flight Review','61.56','Mr./Ms. {Student}, holder of pilot certificate # {Certificate} has satisfactorily completed the flight review required by FAR 61.56 on {Date}'),
 (3,'Commercial Airplane Test Preparation','61.125','I certify that {Student} has received the required training of 61.125.  I have determined that {he/she} is prepared for the Commercial Airplane.'),
 (4,'Presolo Aeronautical Knowledge','61.87(b)','{Student} has satisfactorily completed a presolo written examination demonstrating knowledge of the portions of FAR parts 61 and 91 applicable to student pilots, and the flight characteristics and operational limitations for a {Make and Model}'),
 (5,'Presolo Flight Training','61.87(c)','I have given {Student} the flight instruction required by FAR 61.87(c) in a {Make and model}.  {He/She} has demonstrated proficiency in the applicable maneuvers and procedures listed in FAR 61.87(d) through (j) and is competent to make safe solo flights in a {Make and model}.  Limitations - FAR 61.89(a)(8):{Limitations}.'),
 (6,'Solo (each additional 90-day period)','61.87(m)','I have given {Student} the instruction required by FAR 61.87(m).  {He/She} has met the requirements of FAR 61.87(m) and is competent to make safe solo flights in a {Make and model}.  Limitations - FAR 61.89(a)(8):{Limitations}.'),
 (7,'Solo landings and takeoffs at another airport within 25nm','61.93(b)','I have flown with {Student} and find {him/her} competent and proficient to practice landings and takeoffs at {Airport Name}.  Landings and takeoffs at {Airport Name} are authroized, subject to the following conditions: {Limitations}.'),
 (8,'Solo Cross-Country Flight','61.93(d)(2)(i)','I have reviewed the preflight planning and preparations of {Student} and attest that {he/she} is prepared to make the solo flight safely under the known circumstances from {Departure Airport} to {Destination Airport} via {Intermediate Airport} (\'None\', if none) in a {Make and model} on {Date}.  Limitations - FAR 61.89(a)(8): {Limitations}.'),
 (9,'Repeated cross-country flights not more than 50nm from the point of departure','FAR 61.93(d)(2)(ii)','I have given {Student} flight instruction in both directions over the route between {Departure Airport} and {Destination Airport}, including takeoffs and landings at the airports to be used, and find {him/her} to be competent to conduct repeated solo cross-country flights over that route, subject to the following conditions: {Allowable weather conditions}.'),
 (10,'Solo flight in Class B airspace','61.95(a)','I have given {Student} the ground and flight instruction required by FAR 61.95(a)(1) and find {him/her} competent to conduct solo flight in a {Make and model} in {Class B location} Class B airspace area.'),
 (11,'Solo flight to, from, or at an airport located within Class B Airspace','61.96(b) and 91.131(b)(1)(ii)','I have given {Student} the ground and flight instruction required by FAR 61.95(b)(1), and find {him/her} competent to conduct solo flight operations at {Airport Name}, subject to weather conditions that are {Weather Minimums}.'),
 (12,'Private Pilot aeronautical Knowledge','61.35(a)(1)','I certify that I have given {Student} the ground instruction required by FAR 61.105(a)(1) through (6).'),
 (13,'Private Pilot Flight Proficiency','61.107(a)','I certify that I have given {Student} the flight instruction required by FAR 61.107(a)(1) through (10) and find {him/her} competent to perform each pilot operation safely as a private pilot.'),
 (14,'Tailwheel Airplane','61.31(d)(2)','I have given {Student}, holder of pilot certificate #{Certificate #}, flight instruction in normal and crosswind takeoffs and landings, wheel landings (if appropriate), and go-around procedures in a tailwheel airplane and find {him/her} competent to act as PIC in tailwheel airplanes.'),
 (15,'Complex Aircraft','61.31(e)','I certify that {Student}, holder of pilot certificate #{Certificate} has received the required training of 61.31(e) in a {Make and model}.  I have determined that {he/she} is proficient in the operation and system of a complex airplane.'),
 (16,'High-performance Aircraft','61.31(f)','I certify that {Student}, holder of pilot certificate #{Certificate} has received the required training of 61.31(f) in a {Make and model}.  I have determined that {he/she} is proficient in the operation and system of a high-performance airplane.');
/*!40000 ALTER TABLE `endorsementtemplates` ENABLE KEYS */;

ALTER TABLE `logbook`.`endorsements` ADD COLUMN `DateCreated` DATETIME NULL COMMENT 'Timestamp of when the endorsement was created, which may be different from the date of the endorsement'  AFTER `Date` ;
update endorsements set DateCreated=Date where DateCreated is null;