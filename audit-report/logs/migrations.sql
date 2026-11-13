IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] int NOT NULL IDENTITY,
        [ProductName] nvarchar(100) NOT NULL,
        [SKU] nvarchar(50) NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE TABLE [RawMaterials] (
        [Id] int NOT NULL IDENTITY,
        [MaterialName] nvarchar(100) NOT NULL,
        [SupplierName] nvarchar(100) NOT NULL,
        [CurrentQuantity] decimal(18,2) NOT NULL,
        [PhysicalState] nvarchar(50) NOT NULL,
        [ExpiryDate] datetime2 NOT NULL,
        [IsQCApproved] bit NOT NULL,
        CONSTRAINT [PK_RawMaterials] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL IDENTITY,
        [RoleName] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(100) NOT NULL,
        [Username] nvarchar(50) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [RoleId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [Action] nvarchar(100) NOT NULL,
        [Details] nvarchar(max) NULL,
        [Timestamp] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE TABLE [ProductionBatches] (
        [Id] int NOT NULL IDENTITY,
        [BatchNumber] nvarchar(50) NOT NULL,
        [ProductId] int NOT NULL,
        [RawMaterialId] int NOT NULL,
        [IssuedMaterialQty] decimal(18,2) NOT NULL,
        [ProducedQty] decimal(18,2) NOT NULL,
        [ReturnedMaterialQty] decimal(18,2) NOT NULL,
        [ScrapQty] decimal(18,2) NOT NULL,
        [CurrentState] nvarchar(50) NOT NULL,
        [IsReworked] bit NOT NULL,
        [ExpiryDate] datetime2 NULL,
        [CreatedBy] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProductionBatches] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductionBatches_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductionBatches_RawMaterials_RawMaterialId] FOREIGN KEY ([RawMaterialId]) REFERENCES [RawMaterials] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProductionBatches_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE TABLE [ScrapAndQuarantines] (
        [Id] int NOT NULL IDENTITY,
        [SourceType] nvarchar(50) NOT NULL,
        [ReferenceId] int NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [ScrapReason] nvarchar(max) NOT NULL,
        [FinalDecision] nvarchar(50) NOT NULL,
        [DecisionBy] int NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ScrapAndQuarantines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ScrapAndQuarantines_Users_DecisionBy] FOREIGN KEY ([DecisionBy]) REFERENCES [Users] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE TABLE [LabResults] (
        [Id] int NOT NULL IDENTITY,
        [Type] nvarchar(50) NOT NULL,
        [BatchId] int NULL,
        [RawMaterialId] int NULL,
        [ResultDetails] nvarchar(max) NOT NULL,
        [IsPassed] bit NOT NULL,
        [Notes] nvarchar(max) NULL,
        [TestedBy] int NOT NULL,
        [TestedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_LabResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LabResults_ProductionBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductionBatches] ([Id]),
        CONSTRAINT [FK_LabResults_RawMaterials_RawMaterialId] FOREIGN KEY ([RawMaterialId]) REFERENCES [RawMaterials] ([Id]),
        CONSTRAINT [FK_LabResults_Users_TestedBy] FOREIGN KEY ([TestedBy]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE TABLE [ShippingLogs] (
        [Id] int NOT NULL IDENTITY,
        [BatchId] int NOT NULL,
        [ClientName] nvarchar(100) NOT NULL,
        [DriverName] nvarchar(100) NOT NULL,
        [TruckPlates] nvarchar(50) NOT NULL,
        [ShippedBy] int NOT NULL,
        [ShippingDate] datetime2 NOT NULL,
        CONSTRAINT [PK_ShippingLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShippingLogs_ProductionBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductionBatches] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ShippingLogs_Users_ShippedBy] FOREIGN KEY ([ShippedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LabResults_BatchId] ON [LabResults] ([BatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LabResults_RawMaterialId] ON [LabResults] ([RawMaterialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LabResults_TestedBy] ON [LabResults] ([TestedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductionBatches_CreatedBy] ON [ProductionBatches] ([CreatedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductionBatches_ProductId] ON [ProductionBatches] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ProductionBatches_RawMaterialId] ON [ProductionBatches] ([RawMaterialId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ScrapAndQuarantines_DecisionBy] ON [ScrapAndQuarantines] ([DecisionBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ShippingLogs_BatchId] ON [ShippingLogs] ([BatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ShippingLogs_ShippedBy] ON [ShippingLogs] ([ShippedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260730125948_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260730125948_InitialCreate', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731125452_FixCascadeIssue'
)
BEGIN
    ALTER TABLE [ShippingLogs] DROP CONSTRAINT [FK_ShippingLogs_Users_ShippedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731125452_FixCascadeIssue'
)
BEGIN
    ALTER TABLE [ShippingLogs] ADD CONSTRAINT [FK_ShippingLogs_Users_ShippedBy] FOREIGN KEY ([ShippedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260731125452_FixCascadeIssue'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260731125452_FixCascadeIssue', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [AuditLogs] DROP CONSTRAINT [FK_AuditLogs_Users_UserId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [LabResults] DROP CONSTRAINT [FK_LabResults_Users_TestedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [ProductionBatches] DROP CONSTRAINT [FK_ProductionBatches_Products_ProductId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [ProductionBatches] DROP CONSTRAINT [FK_ProductionBatches_RawMaterials_RawMaterialId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [ProductionBatches] DROP CONSTRAINT [FK_ProductionBatches_Users_CreatedBy];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    DROP INDEX [IX_ShippingLogs_BatchId] ON [ShippingLogs];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [RawMaterials] ADD [LotCode] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [RawMaterials] ADD [RowVersion] rowversion NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [RawMaterials] ADD [SupplierLot] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [RawMaterials] ADD [Unit] nvarchar(10) NOT NULL DEFAULT N'kg';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [ProductionBatches] ADD [IsHeld] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [ProductionBatches] ADD [RowVersion] rowversion NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [ProductionBatches] ADD [StartRequestId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    CREATE TABLE [MaterialIssues] (
        [Id] int NOT NULL IDENTITY,
        [RawMaterialId] int NOT NULL,
        [BatchId] int NULL,
        [RequestId] uniqueidentifier NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [IssuedBy] int NOT NULL,
        [IssuedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_MaterialIssues] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_MaterialIssues_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_MaterialIssues_ProductionBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductionBatches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MaterialIssues_RawMaterials_RawMaterialId] FOREIGN KEY ([RawMaterialId]) REFERENCES [RawMaterials] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MaterialIssues_Users_IssuedBy] FOREIGN KEY ([IssuedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    CREATE TABLE [PalletDispatches] (
        [Id] int NOT NULL IDENTITY,
        [RequestId] uniqueidentifier NOT NULL,
        [ClientName] nvarchar(100) NOT NULL,
        [TruckPlates] nvarchar(50) NOT NULL,
        [ShippedBy] int NOT NULL,
        [ShippedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PalletDispatches] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PalletDispatches_Users_ShippedBy] FOREIGN KEY ([ShippedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    CREATE TABLE [FinishedPallets] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [BatchId] int NOT NULL,
        [Quantity] decimal(18,2) NOT NULL,
        [DispatchId] int NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_FinishedPallets] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_FinishedPallets_Quantity] CHECK ([Quantity] > 0),
        CONSTRAINT [FK_FinishedPallets_PalletDispatches_DispatchId] FOREIGN KEY ([DispatchId]) REFERENCES [PalletDispatches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_FinishedPallets_ProductionBatches_BatchId] FOREIGN KEY ([BatchId]) REFERENCES [ProductionBatches] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    UPDATE [RawMaterials] SET [LotCode] = CONCAT('LEGACY-', [Id]) WHERE [LotCode] IS NULL
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShippingLogs_BatchId] ON [ShippingLogs] ([BatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_RawMaterials_LotCode] ON [RawMaterials] ([LotCode]) WHERE [LotCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProductionBatches_BatchNumber] ON [ProductionBatches] ([BatchNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ProductionBatches_StartRequestId] ON [ProductionBatches] ([StartRequestId]) WHERE [StartRequestId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    CREATE INDEX [IX_FinishedPallets_BatchId] ON [FinishedPallets] ([BatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FinishedPallets_Code] ON [FinishedPallets] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    CREATE INDEX [IX_FinishedPallets_DispatchId] ON [FinishedPallets] ([DispatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    CREATE INDEX [IX_MaterialIssues_BatchId] ON [MaterialIssues] ([BatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    CREATE INDEX [IX_MaterialIssues_IssuedBy] ON [MaterialIssues] ([IssuedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    CREATE INDEX [IX_MaterialIssues_RawMaterialId_BatchId] ON [MaterialIssues] ([RawMaterialId], [BatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_MaterialIssues_RequestId] ON [MaterialIssues] ([RequestId]) WHERE [RequestId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PalletDispatches_RequestId] ON [PalletDispatches] ([RequestId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    CREATE INDEX [IX_PalletDispatches_ShippedBy] ON [PalletDispatches] ([ShippedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [AuditLogs] ADD CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [LabResults] ADD CONSTRAINT [FK_LabResults_Users_TestedBy] FOREIGN KEY ([TestedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [ProductionBatches] ADD CONSTRAINT [FK_ProductionBatches_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [ProductionBatches] ADD CONSTRAINT [FK_ProductionBatches_RawMaterials_RawMaterialId] FOREIGN KEY ([RawMaterialId]) REFERENCES [RawMaterials] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    ALTER TABLE [ProductionBatches] ADD CONSTRAINT [FK_ProductionBatches_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113140520_AddBottlingTraceability'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261113140520_AddBottlingTraceability', N'10.0.10');
END;

COMMIT;
GO

