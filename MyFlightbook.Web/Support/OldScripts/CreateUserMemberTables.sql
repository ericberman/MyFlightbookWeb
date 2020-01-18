(see http://www.codeproject.com/aspnet/MySQLMembershipProvider.asp)

CREATE TABLE Roles
(
  Rolename Varchar (255) NOT NULL,
  ApplicationName varchar (255) NOT NULL
)

CREATE TABLE UsersInRoles
(
  Username Varchar (255) NOT NULL,
  Rolename Varchar (255) NOT NULL,
  ApplicationName Text (255) NOT NULL
)
ALTER TABLE 'usersinroles' 
      ADD INDEX ( 'Username', 'Rolename', 'ApplicationName') ;
ALTER TABLE 'roles' ADD INDEX ( 'Rolename' , 'ApplicationName' ) ;

CREATE TABLE users (
  PKID varchar(36) collate latin1_general_ci NOT NULL default '',
  Username varchar(255) collate latin1_general_ci NOT NULL default '',
  ApplicationName varchar(100) 
                    collate latin1_general_ci NOT NULL default '',
  Email varchar(100) collate latin1_general_ci NOT NULL default '',
  Comment varchar(255) collate latin1_general_ci default NULL,
  FirstName varchar(32) collate latin1_general_ci default NULL,
  LastName varchar(32) collate latin1_general_ci default NULL,
  Password varchar(128) collate latin1_general_ci NOT NULL default '',
  PasswordQuestion varchar(255) collate latin1_general_ci default NULL,
  PasswordAnswer varchar(255) collate latin1_general_ci default NULL,
  IsApproved tinyint(1) default NULL,
  LastActivityDate datetime default NULL,
  LastLoginDate datetime default NULL,
  LastPasswordChangedDate datetime default NULL,
  CreationDate datetime default NULL,
  IsOnLine tinyint(1) default NULL,
  IsLockedOut tinyint(1) default NULL,
  LastLockedOutDate datetime default NULL,
  FailedPasswordAttemptCount int(11) default NULL,
  FailedPasswordAttemptWindowStart datetime default NULL,
  FailedPasswordAnswerAttemptCount int(11) default NULL,
  FailedPasswordAnswerAttemptWindowStart datetime default NULL,
  PRIMARY KEY  (PKID)
) ENGINE=MyISAM DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;