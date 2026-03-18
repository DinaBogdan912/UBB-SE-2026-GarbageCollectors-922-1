IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Transactions')
BEGIN
    CREATE TABLE Transactions (
        Id                  INT             NOT NULL IDENTITY(1,1),
        AccountId           INT             NOT NULL,
        CardId              INT             NULL,
        TransactionRef      NVARCHAR(100)   NOT NULL,
        Type                NVARCHAR(50)    NOT NULL,
        Direction           NVARCHAR(10)    NOT NULL,   -- 'Debit' or 'Credit'
        Amount              DECIMAL(18,2)   NOT NULL,
        Currency            NVARCHAR(10)    NOT NULL,
        BalanceAfter        DECIMAL(18,2)   NOT NULL,
        CounterpartyName    NVARCHAR(200)   NOT NULL,
        CounterpartyIBAN    NVARCHAR(50)    NULL,
        Fee                 DECIMAL(18,2)   NOT NULL    DEFAULT 0,
        ExchangeRate        DECIMAL(18,6)   NULL,
        Status              NVARCHAR(50)    NOT NULL,
        RelatedEntityType   NVARCHAR(100)   NULL,
        RelatedEntityId     INT             NULL,
        CreatedAt           DATETIME2       NOT NULL    DEFAULT GETUTCDATE(),

        CONSTRAINT PK_Transactions          PRIMARY KEY (Id),
        CONSTRAINT UQ_Transactions_Ref      UNIQUE      (TransactionRef),
        CONSTRAINT CK_Transactions_Dir      CHECK       (Direction IN ('Debit', 'Credit')),
        CONSTRAINT CK_Transactions_Amount   CHECK       (Amount >= 0),
        CONSTRAINT CK_Transactions_Fee      CHECK       (Fee >= 0)
    );
END
