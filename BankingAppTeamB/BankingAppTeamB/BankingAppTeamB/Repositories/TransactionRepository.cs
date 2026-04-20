using System;
using System.Collections.Generic;
using BankingAppTeamB.Data;
using BankingAppTeamB.Models;
using Microsoft.Data.SqlClient;

namespace BankingAppTeamB.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        public void Add(Transaction transaction)
        {
            string insertSql = @"
                INSERT INTO Transactions
                    (AccountId, CardId, TransactionRef, Type, Direction,
                     Amount, Currency, BalanceAfter, CounterpartyName, CounterpartyIBAN,
                     Fee, ExchangeRate, Status, RelatedEntityType, RelatedEntityId, CreatedAt)
                OUTPUT INSERTED.Id
                VALUES
                    (@AccountId, @CardId, @TransactionRef, @Type, @Direction,
                     @Amount, @Currency, @BalanceAfter, @CounterpartyName, @CounterpartyIBAN,
                     @Fee, @ExchangeRate, @Status, @RelatedEntityType, @RelatedEntityId, @CreatedAt)";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();

                using (var command = new SqlCommand(insertSql, connection))
                {
                    command.Parameters.Add(new SqlParameter("@AccountId", transaction.AccountId));
                    command.Parameters.Add(new SqlParameter("@CardId", (object?)transaction.CardId ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@TransactionRef", transaction.TransactionRef));
                    command.Parameters.Add(new SqlParameter("@Type", transaction.Type));
                    command.Parameters.Add(new SqlParameter("@Direction", transaction.Direction));
                    command.Parameters.Add(new SqlParameter("@Amount", transaction.Amount));
                    command.Parameters.Add(new SqlParameter("@Currency", transaction.Currency));
                    command.Parameters.Add(new SqlParameter("@BalanceAfter", transaction.BalanceAfter));
                    command.Parameters.Add(new SqlParameter("@CounterpartyName", transaction.CounterpartyName));
                    command.Parameters.Add(new SqlParameter("@CounterpartyIBAN", (object?)transaction.CounterpartyIBAN ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Fee", transaction.Fee));
                    command.Parameters.Add(new SqlParameter("@ExchangeRate", (object?)transaction.ExchangeRate ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Status", transaction.Status));
                    command.Parameters.Add(new SqlParameter("@RelatedEntityType", (object?)transaction.RelatedEntityType ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@RelatedEntityId", (object?)transaction.RelatedEntityId ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@CreatedAt", transaction.CreatedAt));

                    transaction.Id = (int)command.ExecuteScalar();
                }
            }
        }

        public Transaction GetById(int transactionId)
        {
            string sqlQuery = "SELECT * FROM Transactions WHERE Id = @Id";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new SqlParameter("@Id", transactionId));

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapTransaction(reader);
                        }
                    }
                }
            }
            throw new KeyNotFoundException($"Transaction with ID {transactionId} was not found.");
        }

        public List<Transaction> GetByUserId(int userId)
        {
            string sqlQuery = "SELECT * FROM Transactions WHERE AccountId = @UserId";
            var transactions = new List<Transaction>();

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserId", userId));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            transactions.Add(MapTransaction(reader));
                        }
                    }
                }
            }
            return transactions;
        }

        private Transaction MapTransaction(SqlDataReader reader)
        {
            return new Transaction
            {
                Id = (int)reader["Id"],
                AccountId = (int)reader["AccountId"],
                CardId = reader["CardId"] == DBNull.Value ? null : (int?)reader["CardId"],
                TransactionRef = (string)reader["TransactionRef"],
                Type = (string)reader["Type"],
                Direction = (string)reader["Direction"],
                Amount = (decimal)reader["Amount"],
                Currency = (string)reader["Currency"],
                BalanceAfter = (decimal)reader["BalanceAfter"],
                CounterpartyName = (string)reader["CounterpartyName"],
                CounterpartyIBAN = reader["CounterpartyIBAN"] == DBNull.Value ? null : (string)reader["CounterpartyIBAN"],
                Fee = (decimal)reader["Fee"],
                ExchangeRate = reader["ExchangeRate"] == DBNull.Value ? null : (decimal?)reader["ExchangeRate"],
                Status = (string)reader["Status"],
                RelatedEntityType = reader["RelatedEntityType"] == DBNull.Value ? null : (string)reader["RelatedEntityType"],
                RelatedEntityId = reader["RelatedEntityId"] == DBNull.Value ? null : (int?)reader["RelatedEntityId"],
                CreatedAt = (DateTime)reader["CreatedAt"]
            };
        }
    }
}
