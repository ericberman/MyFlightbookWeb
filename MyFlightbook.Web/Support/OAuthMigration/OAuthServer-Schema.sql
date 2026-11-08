-- MyFlightbook OAuth server (post-DotNetOpenAuth) schema.  Safe to run before cutover: nothing reads these tables yet.
-- MySQL 8.4.

-- Tokens: hashes only (hex SHA-256 of the full token string).  Access tokens expire; refresh tokens don't (until revoked).
CREATE TABLE IF NOT EXISTS `oauthtokens` (
  `TokenHash` char(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `TokenType` enum('access','refresh') NOT NULL,
  `ClientId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL COMMENT 'The real client - never publicClientProxy.  May be a Client ID Metadata Document URL.',
  `UserId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Scope` varchar(255) NOT NULL DEFAULT '',
  `Resource` varchar(255) DEFAULT NULL COMMENT 'RFC 8707 audience; NULL = the MyFlightbook API',
  `FamilyId` char(32) CHARACTER SET ascii COLLATE ascii_bin DEFAULT NULL COMMENT 'All tokens descending from one code / legacy refresh token',
  `IssuedUtc` datetime NOT NULL,
  `ExpiresUtc` datetime DEFAULT NULL,
  `RotatedUtc` datetime DEFAULT NULL COMMENT 'Refresh tokens: when exchanged for a successor',
  PRIMARY KEY (`TokenHash`),
  KEY `UserClient` (`UserId`,`ClientId`),
  KEY `Family` (`FamilyId`),
  KEY `Expires` (`ExpiresUtc`),
  KEY `Rotated` (`RotatedUtc`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='OAuth tokens issued by MyFlightbook (hashes only)';

-- Authorization codes for ALL clients (replaces pkceauthorization once cut over).  Hashes only.
CREATE TABLE IF NOT EXISTS `oauthauthcodes` (
  `CodeHash` char(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `ClientId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `UserId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `RedirectUri` varchar(1024) NOT NULL,
  `Scope` varchar(255) NOT NULL DEFAULT '',
  `Resource` varchar(255) DEFAULT NULL,
  `CodeChallenge` varchar(128) CHARACTER SET ascii COLLATE ascii_bin DEFAULT NULL,
  `CodeChallengeMethod` varchar(10) DEFAULT NULL,
  `ExpiresUtc` datetime NOT NULL,
  `FamilyId` char(32) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `Used` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`CodeHash`),
  KEY `Expires` (`ExpiresUtc`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='OAuth authorization codes (hashes only; minutes-lived)';

-- Display names for Client ID Metadata Document clients (e.g., Claude), so they show up on the user's "Authorized apps" page.
-- Upserted by MetadataDocumentOptions.OnResolved.  GrantedAuthsForUser must LEFT JOIN this alongside allowedoauthclients.
CREATE TABLE IF NOT EXISTS `oauthmetadataclients` (
  `ClientId` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ClientName` varchar(100) NOT NULL,
  `LastResolvedUtc` datetime NOT NULL,
  PRIMARY KEY (`ClientId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Client ID Metadata Document clients seen by the OAuth server';

-- HasActiveAuthorization runs on every refresh; today oauthclientauthorization has only its PK.
-- (Run once; MySQL has no CREATE INDEX IF NOT EXISTS.)
ALTER TABLE `oauthclientauthorization` ADD INDEX `ClientUser` (`ClientId`, `UserId`);

-- NOT changed, deliberately:
--   cryptokeys: rows with bucket='https://localhost/dnoa/oauth_refresh_token' decode every legacy refresh token.  Never delete them.
--   pkceauthorization, nonce: still used by the DNOA server until cutover; drop afterwards.
