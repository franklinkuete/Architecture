CREATE TABLE IF NOT EXISTS `OutboxMessages` (
    `Id` CHAR(36) NOT NULL,
    `Type` VARCHAR(255) NOT NULL,
    `Content` LONGTEXT NOT NULL,
    `CreatedOnUtc` DATETIME(6) NOT NULL,
    `ProcessedOnUtc` DATETIME(6) NULL,
    `Error` LONGTEXT NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_OutboxMessages_ProcessedOnUtc` (`ProcessedOnUtc`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;