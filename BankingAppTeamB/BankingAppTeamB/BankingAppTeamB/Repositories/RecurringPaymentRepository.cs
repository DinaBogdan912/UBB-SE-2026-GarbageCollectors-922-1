using System;
using System.Collections.Generic;
using BankingAppTeamB.Data;
using BankingAppTeamB.Models;
using Microsoft.Data.SqlClient;

namespace BankingAppTeamB.Repositories
{
    public class RecurringPaymentRepository : IRecurringPaymentRepository
    {
        public RecurringPayment Add(RecurringPayment recurringPayment)
        {
            string sqlQuery = @"
                INSERT INTO RecurringPayment
                    (UserId, BillerId, SourceAccountId, Amount, IsPayInFull,
                     Frequency, StartDate, EndDate, NextExecutionDate, Status, CreatedAt)
                OUTPUT INSERTED.Id
                VALUES
                    (@UserId, @BillerId, @SourceAccountId, @Amount, @IsPayInFull,
                     @Frequency, @StartDate, @EndDate, @NextExecutionDate, @Status, @CreatedAt)";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserId", recurringPayment.UserId));
                    command.Parameters.Add(new SqlParameter("@BillerId", recurringPayment.BillerId));
                    command.Parameters.Add(new SqlParameter("@SourceAccountId", recurringPayment.SourceAccountId));
                    command.Parameters.Add(new SqlParameter("@Amount", recurringPayment.Amount));
                    command.Parameters.Add(new SqlParameter("@IsPayInFull", recurringPayment.IsPayInFull));
                    command.Parameters.Add(new SqlParameter("@Frequency", recurringPayment.Frequency.ToString()));
                    command.Parameters.Add(new SqlParameter("@StartDate", recurringPayment.StartDate));
                    command.Parameters.Add(new SqlParameter("@EndDate", (object?)recurringPayment.EndDate ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@NextExecutionDate", recurringPayment.NextExecutionDate));
                    command.Parameters.Add(new SqlParameter("@Status", recurringPayment.Status.ToString()));
                    command.Parameters.Add(new SqlParameter("@CreatedAt", recurringPayment.CreatedAt));

                    recurringPayment.Id = (int)command.ExecuteScalar();
                }
            }
            return recurringPayment;
        }

        public RecurringPayment? GetById(int recurringPaymentId)
        {
            string sqlQuery = "SELECT * FROM RecurringPayment WHERE Id = @Id";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new SqlParameter("@Id", recurringPaymentId));

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapRecurringPayment(reader);
                        }
                    }
                }
            }
            return null;
        }

        public List<RecurringPayment> GetByUserId(int userId)
        {
            string sqlQuery = @"
                SELECT * FROM RecurringPayment
                WHERE UserId = @UserId
                ORDER BY NextExecutionDate ASC";
            var recurringPayments = new List<RecurringPayment>();

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
                            recurringPayments.Add(MapRecurringPayment(reader));
                        }
                    }
                }
            }
            return recurringPayments;
        }

        public List<RecurringPayment> GetDueBefore(DateTime dueBeforeDateTime)
        {
            string sqlQuery = @"
                SELECT * FROM RecurringPayment
                WHERE NextExecutionDate <= @DateTime
                ORDER BY NextExecutionDate ASC";
            var dueRecurringPayments = new List<RecurringPayment>();

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new SqlParameter("@DateTime", dueBeforeDateTime));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dueRecurringPayments.Add(MapRecurringPayment(reader));
                        }
                    }
                }
            }
            return dueRecurringPayments;
        }

        public void Update(RecurringPayment recurringPayment)
        {
            string sqlQuery = @"
                UPDATE RecurringPayment SET
                    Amount              = @Amount,
                    IsPayInFull         = @IsPayInFull,
                    Frequency           = @Frequency,
                    StartDate           = @StartDate,
                    EndDate             = @EndDate,
                    NextExecutionDate   = @NextExecutionDate,
                    Status              = @Status
                WHERE Id = @Id";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new SqlParameter("@Amount", recurringPayment.Amount));
                    command.Parameters.Add(new SqlParameter("@IsPayInFull", recurringPayment.IsPayInFull));
                    command.Parameters.Add(new SqlParameter("@Frequency", recurringPayment.Frequency.ToString()));
                    command.Parameters.Add(new SqlParameter("@StartDate", recurringPayment.StartDate));
                    command.Parameters.Add(new SqlParameter("@EndDate", (object?)recurringPayment.EndDate ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@NextExecutionDate", recurringPayment.NextExecutionDate));
                    command.Parameters.Add(new SqlParameter("@Status", recurringPayment.Status.ToString()));
                    command.Parameters.Add(new SqlParameter("@Id", recurringPayment.Id));

                    command.ExecuteNonQuery();
                }
            }
        }

        public void Delete(int recurringPaymentId)
        {
            string sqlQuery = "DELETE FROM RecurringPayment WHERE Id = @Id";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new SqlParameter("@Id", recurringPaymentId));
                    command.ExecuteNonQuery();
                }
            }
        }

        private RecurringPayment MapRecurringPayment(SqlDataReader reader)
        {
            return new RecurringPayment
            {
                Id = (int)reader["Id"],
                UserId = (int)reader["UserId"],
                BillerId = (int)reader["BillerId"],
                SourceAccountId = (int)reader["SourceAccountId"],
                Amount = (decimal)reader["Amount"],
                IsPayInFull = (bool)reader["IsPayInFull"],
                Frequency = Enum.Parse<RecurringFrequency>((string)reader["Frequency"]),
                StartDate = (DateTime)reader["StartDate"],
                EndDate = reader["EndDate"] == DBNull.Value ? null : (DateTime?)reader["EndDate"],
                NextExecutionDate = (DateTime)reader["NextExecutionDate"],
                Status = Enum.Parse<PaymentStatus>((string)reader["Status"]),
                CreatedAt = (DateTime)reader["CreatedAt"]
            };
        }
    }
}