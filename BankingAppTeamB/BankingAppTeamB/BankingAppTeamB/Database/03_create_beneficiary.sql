IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Beneficiaries')
BEGIN
    CREATE TABLE Beneficiaries (
        Id                  INT             NOT NULL IDENTITY(1,1),
        UserId              INT             NOT NULL,
        Name                NVARCHAR(200)   NOT NULL,
        IBAN                NVARCHAR(50)    NOT NULL,
        BankName            NVARCHAR(200)   NULL,
        LastTransferDate    DATETIME2       NULL,
        TotalAmountSent     DECIMAL(18,2)   NOT NULL    DEFAULT 0,
        TransferCount       INT             NOT NULL    DEFAULT 0,
        CreatedAt           DATETIME2       NOT NULL    DEFAULT GETUTCDATE(),

        CONSTRAINT PK_Beneficiaries                 PRIMARY KEY (Id),
        CONSTRAINT UQ_Beneficiaries_UserIBAN        UNIQUE      (UserId, IBAN),
        CONSTRAINT CK_Beneficiaries_TotalAmount     CHECK       (TotalAmountSent >= 0),
        CONSTRAINT CK_Beneficiaries_TransferCount   CHECK       (TransferCount >= 0)
    );
END
