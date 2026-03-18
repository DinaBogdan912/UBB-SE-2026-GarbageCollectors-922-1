IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Transfers')
BEGIN
    CREATE TABLE Transfers (
        Id                  INT             NOT NULL IDENTITY(1,1),
        UserId              INT             NOT NULL,
        SourceAccountId     INT             NOT NULL,
        TransactionId       INT             NULL,
        RecipientName       NVARCHAR(200)   NOT NULL,
        RecipientIBAN       NVARCHAR(50)    NOT NULL,
        RecipientBankName   NVARCHAR(200)   NULL,
        Amount              DECIMAL(18,2)   NOT NULL,
        Currency            NVARCHAR(10)    NOT NULL,
        ConvertedAmount     DECIMAL(18,2)   NULL,
        ExchangeRate        DECIMAL(18,6)   NULL,
        Fee                 DECIMAL(18,2)   NOT NULL    DEFAULT 0,
        Reference           NVARCHAR(200)   NULL,
        Status              NVARCHAR(50)    NOT NULL,
        EstimatedArrival    DATETIME2       NULL,
        CreatedAt           DATETIME2       NOT NULL    DEFAULT GETUTCDATE(),

        CONSTRAINT PK_Transfers             PRIMARY KEY (Id),
        CONSTRAINT FK_Transfers_Transaction FOREIGN KEY (TransactionId) REFERENCES Transactions(Id),
        CONSTRAINT CK_Transfers_Amount      CHECK       (Amount > 0),
        CONSTRAINT CK_Transfers_Fee         CHECK       (Fee >= 0)
    );
END
