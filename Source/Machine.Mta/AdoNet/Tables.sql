﻿CREATE TABLE future_publish
(
  Id INTEGER NOT NULL IDENTITY(1, 1) PRIMARY KEY,
  GroupName NVARCHAR(32) NOT NULL,
  PublishId UNIQUEIDENTIFIER NOT NULL,
  CreatedAt DATETIME NOT NULL,
  PublishAt DATETIME NOT NULL,
  ReturnAddress NVARCHAR(32),
  MessagePayload VARBINARY(MAX) NOT NULL,
  SagaIds NVARCHAR(1024) NOT NULL,
);

CREATE TABLE saga
(
  SagaId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
  StartedAt DATETIME NOT NULL,
  LastUpdatedAt DATETIME NOT NULL,
  SagaState VARBINARY(MAX) NOT NULL
);