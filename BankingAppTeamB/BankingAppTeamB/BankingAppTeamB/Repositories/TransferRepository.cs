using System;
using System.Collections.Generic;
using BankingAppTeamB.Data;
using BankingAppTeamB.Models;
using Microsoft.Data.SqlClient;

namespace BankingAppTeamB.Repositories
{
    public class TransferRepository : ITransferRepository
    {
        /// <summary>Adds a transfer record and sets the generated ID on the transfer object.</summary>
        public void Add(Transfer transfer)
        {
            string sqlQuery = @"
                INSERT INTO Transfers
                    (UserId, SourceAccountId, TransactionId, RecipientName, RecipientIBAN,
                     RecipientBankName, Amount, Currency, ConvertedAmount, ExchangeRate,
                     Fee, Reference, Status, EstimatedArrival, CreatedAt)
                OUTPUT INSERTED.Id
                VALUES
                    (@UserId, @SourceAccountId, @TransactionId, @RecipientName, @RecipientIBAN,
                     @RecipientBankName, @Amount, @Currency, @ConvertedAmount, @ExchangeRate,
                     @Fee, @Reference, @Status, @EstimatedArrival, @CreatedAt)";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserId", transfer.UserId));
                    command.Parameters.Add(new SqlParameter("@SourceAccountId", transfer.SourceAccountId));
                    command.Parameters.Add(new SqlParameter("@TransactionId",
                        (object?)transfer.TransactionId ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@RecipientName", transfer.RecipientName));
                    command.Parameters.Add(new SqlParameter("@RecipientIBAN", transfer.RecipientIBAN));
                    command.Parameters.Add(new SqlParameter("@RecipientBankName",
                        (object?)transfer.RecipientBankName ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Amount", transfer.Amount));
                    command.Parameters.Add(new SqlParameter("@Currency", transfer.Currency));
                    command.Parameters.Add(new SqlParameter("@ConvertedAmount",
                        (object?)transfer.ConvertedAmount ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@ExchangeRate", (object?)transfer.ExchangeRate ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Fee", transfer.Fee));
                    command.Parameters.Add(new SqlParameter("@Reference", (object?)transfer.Reference ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Status", transfer.Status));
                    command.Parameters.Add(new SqlParameter("@EstimatedArrival",
                        (object?)transfer.EstimatedArrival ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@CreatedAt", transfer.CreatedAt));

                    transfer.Id = (int)command.ExecuteScalar();
                }
            }
        }

        /// <summary>Gets one transfer by ID or throws if it does not exist.</summary>
        public Transfer GetById(int transferId)
        {
            string sqlQuery = "SELECT * FROM Transfers WHERE Id = @Id";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new SqlParameter("@Id", transferId));

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapTransfer(reader);
                        }
                    }
                }
            }

            throw new KeyNotFoundException($"Transfer with ID {transferId} was not found.");
        }

        /// <summary>Gets all transfers for a user, newest first.</summary>
        public List<Transfer> GetByUserId(int userId)
        {
            string sqlQuery = "SELECT * FROM Transfers WHERE UserId = @UserId ORDER BY CreatedAt DESC";
            var transfers = new List<Transfer>();

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
                            transfers.Add(MapTransfer(reader));
                        }
                    }
                }
            }

            return transfers;
        }

        /// <summary>Updates the status text for a transfer.</summary>
        public void UpdateStatus(int transferId, string transferStatus)
        {
            string sqlQuery = "UPDATE Transfers SET Status = @Status WHERE Id = @Id";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new SqlParameter("@Status", transferStatus));
                    command.Parameters.Add(new SqlParameter("@Id", transferId));
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Builds a Transfer object from the current database row.</summary>
        private Transfer MapTransfer(SqlDataReader reader)
        {
            return new Transfer
            {
                Id = (int)reader["Id"],
                UserId = (int)reader["UserId"],
                SourceAccountId = (int)reader["SourceAccountId"],
                TransactionId = reader["TransactionId"] == DBNull.Value ? null : (int?)reader["TransactionId"],
                RecipientName = (string)reader["RecipientName"],
                RecipientIBAN = (string)reader["RecipientIBAN"],
                RecipientBankName = reader["RecipientBankName"] == DBNull.Value
                    ? null
                    : (string)reader["RecipientBankName"],
                Amount = (decimal)reader["Amount"],
                Currency = (string)reader["Currency"],
                ConvertedAmount =
                    reader["ConvertedAmount"] == DBNull.Value ? null : (decimal?)reader["ConvertedAmount"],
                ExchangeRate = reader["ExchangeRate"] == DBNull.Value ? null : (decimal?)reader["ExchangeRate"],
                Fee = (decimal)reader["Fee"],
                Reference = reader["Reference"] == DBNull.Value ? null : (string)reader["Reference"],
                Status = Enum.TryParse<TransferStatus>(
                    reader["Status"].ToString(),
                    out var status)
                    ? status
                    : throw new Exception("Invalid TransferStatus value"),
                EstimatedArrival = reader["EstimatedArrival"] == DBNull.Value
                    ? null
                    : (DateTime?)reader["EstimatedArrival"],
                CreatedAt = (DateTime)reader["CreatedAt"]
            };
        }
    }
}