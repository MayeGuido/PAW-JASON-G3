-- Base de datos de Jason. Correr en SQL Server (SSMS, Azure Data Studio o sqlcmd).
-- Es seguro correrlo más de una vez: solo crea lo que no existe.

IF DB_ID('JasonDb') IS NULL
    CREATE DATABASE JasonDb;
GO

USE JasonDb;
GO

-- Sources y SourceItems: tal como las define el profesor.
-- NO cambiar ni quitar columnas: el JSON que se descarga tiene que poder cargarse en las apps de los otros grupos.

IF OBJECT_ID('dbo.Sources', 'U') IS NULL
CREATE TABLE Sources (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Url NVARCHAR(500) NOT NULL,                  -- URL fuente
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    ComponentType NVARCHAR(100) NOT NULL,        -- 'widget', 'api', 'feed'
    RequiresSecret BIT NOT NULL DEFAULT 0        -- 0 = no, 1 = sí
);
GO

IF OBJECT_ID('dbo.SourceItems', 'U') IS NULL
CREATE TABLE SourceItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SourceId INT,                                -- Sources
    Json NVARCHAR(MAX),                          -- el item completo en el formato común
    CreatedAt DATETIME                           -- fecha en que se guardó
);
GO

-- Relación entre las dos tablas (el script del profesor no la trae, pero ayuda a mantener los datos limpios)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SourceItems_Sources')
ALTER TABLE SourceItems
    ADD CONSTRAINT FK_SourceItems_Sources FOREIGN KEY (SourceId) REFERENCES Sources(Id) ON DELETE CASCADE;
GO

-- Settings: secrets y ajustes. Los valores se guardan cifrados (nunca en texto plano).
IF OBJECT_ID('dbo.Settings', 'U') IS NULL
CREATE TABLE Settings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    [Key] NVARCHAR(200) NOT NULL UNIQUE,
    ValueEncrypted NVARCHAR(MAX) NOT NULL,
    SourceId INT NULL,                           -- si el secret es de una fuente en particular
    UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Las tablas de usuarios y roles (ASP.NET Identity) están en 03_identity_tables.sql.
