-- Script para crear la tabla ResetPasswordToken usando el patrón selector + token-hash
-- Ejecutar en tu base de datos SQL Server (ej. en SSMS)

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ResetPasswordToken') AND type in (N'U'))
BEGIN
    CREATE TABLE dbo.ResetPasswordToken
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Selector VARCHAR(64) NOT NULL,
        TokenHash VARCHAR(256) NOT NULL,
        Utilizado BIT NOT NULL CONSTRAINT DF_ResetPasswordToken_Utilizado DEFAULT(0),
        IdUsuarioSolicita INT NOT NULL,
        IdUsuarioCrea INT NULL,
        IdUsuarioModifica INT NULL,
        Estatus BIT NOT NULL CONSTRAINT DF_ResetPasswordToken_Estatus DEFAULT(1),
        FechaModificacion DATETIME NULL,
        FechaCreacion DATETIME NOT NULL CONSTRAINT DF_ResetPasswordToken_FechaCreacion DEFAULT(GETDATE()),
        FechaCaducidad DATETIME NOT NULL
    );

    -- FK: ajusta el nombre del esquema/tabla Usuario si es diferente
    ALTER TABLE dbo.ResetPasswordToken
    ADD CONSTRAINT FK_ResetPasswordToken_UsuarioSolicita FOREIGN KEY (IdUsuarioSolicita)
        REFERENCES dbo.Usuario (Id);

    ALTER TABLE dbo.ResetPasswordToken
    ADD CONSTRAINT FK_ResetPasswordToken_UsuarioCrea FOREIGN KEY (IdUsuarioCrea)
        REFERENCES dbo.Usuario (Id);

    ALTER TABLE dbo.ResetPasswordToken
    ADD CONSTRAINT FK_ResetPasswordToken_UsuarioModifica FOREIGN KEY (IdUsuarioModifica)
        REFERENCES dbo.Usuario (Id);

    -- Índices recomendados
    CREATE UNIQUE INDEX UX_ResetPasswordToken_Selector ON dbo.ResetPasswordToken(Selector);
    CREATE INDEX IX_ResetPasswordToken_IdUsuarioSolicita ON dbo.ResetPasswordToken(IdUsuarioSolicita);
    CREATE INDEX IX_ResetPasswordToken_FechaCaducidad ON dbo.ResetPasswordToken(FechaCaducidad);
END
ELSE
BEGIN
    PRINT 'La tabla dbo.ResetPasswordToken ya existe. No se realizaron cambios.';
END
