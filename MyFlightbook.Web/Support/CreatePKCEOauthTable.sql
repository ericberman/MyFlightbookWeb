CREATE TABLE `pkceauthorization` (
  `State` varchar(64) NOT NULL,
  `PublicCode` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `LegacyCode` varchar(1024) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ClientId` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `RedirectUri` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `scopes` varchar(255) NOT NULL,
  `CodeChallenge` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CodeChallengeMethod` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ExpiresUtc` datetime NOT NULL,
  `Used` bit(1) NOT NULL DEFAULT b'0',
  PRIMARY KEY (`State`),
  KEY `Code` (`PublicCode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

/* Need a "publicClientProxy that is a confidential client.  Fill in appropriate client secret, callback, scopes, and owning user */
INSERT INTO `allowedoauthclients` (`ClientID`,`ClientSecret`,`ClientName`,`Callback`,`ClientType`,`Scopes`,`owningUserName`) VALUES ('publicClientProxy','...','Proxy for PKCE','...',0,'...','...');
