using System;
using System.Collections.Generic;
using BankingAppTeamB.Data;
using BankingAppTeamB.Models;
using Microsoft.Data.SqlClient;

namespace BankingAppTeamB.Repositories
{
    public class BillPaymentRepository : IBillPaymentRepository
    {
        public BillPayment Add(BillPayment billPayment)
        {
            string sql = @"
                INSERT INTO BillPayment
                    (UserId, SourceAccountId, BillerId, TransactionId, BillerReference,
                     Amount, Fee, ReceiptNumber, Status, CreatedAt)
                OUTPUT INSERTED.Id
                VALUES
                    (@UserId, @SourceAccountId, @BillerId, @TransactionId, @BillerReference,
                     @Amount, @Fee, @ReceiptNumber, @Status, @CreatedAt)";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserId", billPayment.UserId));
                    command.Parameters.Add(new SqlParameter("@SourceAccountId", billPayment.SourceAccountId));
                    command.Parameters.Add(new SqlParameter("@BillerId", billPayment.BillerId));
                    command.Parameters.Add(new SqlParameter("@TransactionId", (object?)billPayment.TransactionId ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@BillerReference", billPayment.BillerReference));
                    command.Parameters.Add(new SqlParameter("@Amount", billPayment.Amount));
                    command.Parameters.Add(new SqlParameter("@Fee", billPayment.Fee));
                    command.Parameters.Add(new SqlParameter("@ReceiptNumber", billPayment.ReceiptNumber));
                    command.Parameters.Add(new SqlParameter("@Status", billPayment.Status.ToString()));
                    command.Parameters.Add(new SqlParameter("@CreatedAt", billPayment.CreatedAt));

                    billPayment.Id = (int)command.ExecuteScalar();
                }
            }
            return billPayment;
        }

        public List<BillPayment> GetByUserId(int userId)
        {
            string sql = @"
                SELECT * FROM BillPayment
                WHERE UserId = @UserId
                ORDER BY CreatedAt DESC";
            var results = new List<BillPayment>();

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserId", userId));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(MapBillPayment(reader));
                        }
                    }
                }
            }
            return results;
        }

        public List<Biller> GetAllBillers(bool? isActive = null)
        {
            string sql = @"
                SELECT * FROM Biller
                WHERE (@IsActive IS NULL OR IsActive = @IsActive)
                ORDER BY Category ASC, Name ASC";
            var results = new List<Biller>();

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(new SqlParameter("@IsActive", (object?)isActive ?? DBNull.Value));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(MapBiller(reader));
                        }
                    }
                }
            }
            return results;
        }

        public List<Biller> SearchBillers(string query, string? category, bool? isActive = null)
        {
            string sql = @"
                SELECT * FROM Biller
                WHERE (@IsActive IS NULL OR IsActive = @IsActive)
                AND (@Query = '' OR Name LIKE '%' + @Query + '%')
                AND (@Category IS NULL OR Category = @Category)
                ORDER BY Category ASC, Name ASC";
            var results = new List<Biller>();

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(new SqlParameter("@IsActive", (object?)isActive ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@Query", query));
                    command.Parameters.Add(new SqlParameter("@Category", (object?)category ?? DBNull.Value));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(MapBiller(reader));
                        }
                    }
                }
            }
            return results;
        }

        public Biller? GetBillerById(int id)
        {
            string sql = "SELECT * FROM Biller WHERE Id = @Id";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(new SqlParameter("@Id", id));

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapBiller(reader);
                        }
                    }
                }
            }
            throw new KeyNotFoundException($"Biller with ID {id} was not found.");
        }

        public List<SavedBiller> GetSavedBillers(int userId)
        {
            string sql = @"
                SELECT sb.Id, sb.UserId, sb.BillerId, sb.Nickname,
                       sb.DefaultReference, sb.CreatedAt,
                       b.Id AS B_Id, b.Name AS B_Name, b.Category AS B_Category,
                       b.LogoUrl AS B_LogoUrl, b.IsActive AS B_IsActive
                FROM SavedBiller sb
                INNER JOIN Biller b ON sb.BillerId = b.Id
                WHERE sb.UserId = @UserId";
            var results = new List<SavedBiller>();

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserId", userId));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var savedBiller = MapSavedBiller(reader);
                            savedBiller.Biller = new Biller
                            {
                                Id = (int)reader["B_Id"],
                                Name = (string)reader["B_Name"],
                                Category = (string)reader["B_Category"],
                                LogoUrl = reader["B_LogoUrl"] == DBNull.Value ? null : (string)reader["B_LogoUrl"],
                                IsActive = (bool)reader["B_IsActive"]
                            };
                            results.Add(savedBiller);
                        }
                    }
                }
            }
            return results;
        }

        public void SaveBiller(SavedBiller savedBiller)
        {
            string sql = @"
                INSERT INTO SavedBiller
                    (UserId, BillerId, Nickname, DefaultReference, CreatedAt)
                VALUES
                    (@UserId, @BillerId, @Nickname, @DefaultReference, @CreatedAt)";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserId", savedBiller.UserId));
                    command.Parameters.Add(new SqlParameter("@BillerId", savedBiller.BillerId));
                    command.Parameters.Add(new SqlParameter("@Nickname", (object?)savedBiller.Nickname ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@DefaultReference", (object?)savedBiller.DefaultReference ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@CreatedAt", savedBiller.CreatedAt));

                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteSavedBiller(int id)
        {
            string sql = "DELETE FROM SavedBiller WHERE Id = @Id";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(new SqlParameter("@Id", id));
                    command.ExecuteNonQuery();
                }
            }
        }

        private BillPayment MapBillPayment(SqlDataReader reader)
        {
            return new BillPayment
            {
                Id = (int)reader["Id"],
                UserId = (int)reader["UserId"],
                SourceAccountId = (int)reader["SourceAccountId"],
                BillerId = (int)reader["BillerId"],
                TransactionId = reader["TransactionId"] == DBNull.Value ? null : (int?)reader["TransactionId"],
                BillerReference = (string)reader["BillerReference"],
                Amount = (decimal)reader["Amount"],
                Fee = (decimal)reader["Fee"],
                ReceiptNumber = (string)reader["ReceiptNumber"],
                Status = Enum.Parse<PaymentStatus>((string)reader["Status"]),
                CreatedAt = (DateTime)reader["CreatedAt"]
            };
        }

        private Biller MapBiller(SqlDataReader reader)
        {
            return new Biller
            {
                Id = (int)reader["Id"],
                Name = (string)reader["Name"],
                Category = (string)reader["Category"],
                LogoUrl = reader["LogoUrl"] == DBNull.Value ? null : (string)reader["LogoUrl"],
                IsActive = (bool)reader["IsActive"]
            };
        }

        private SavedBiller MapSavedBiller(SqlDataReader reader)
        {
            return new SavedBiller
            {
                Id = (int)reader["Id"],
                UserId = (int)reader["UserId"],
                BillerId = (int)reader["BillerId"],
                Nickname = reader["Nickname"] == DBNull.Value ? null : (string)reader["Nickname"],
                DefaultReference = reader["DefaultReference"] == DBNull.Value ? null : (string)reader["DefaultReference"],
                CreatedAt = (DateTime)reader["CreatedAt"]
            };
        }
    }
}