using System;
using System.Collections.Generic;
using BankingAppTeamB.Data;
using BankingAppTeamB.Models;
using Microsoft.Data.SqlClient;

namespace BankingAppTeamB.Repositories
{
    public class RecurringPaymentRepository : IRecurringPaymentRepository
    {
        /// <summary>Inserts a new recurring payment row, writes the generated identity back, and returns the updated entity.</summary>
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

        /// <summary>Returns the recurring payment with the given <paramref name="recurringPaymentId"/>; throws <see cref="KeyNotFoundException"/> if not found.</summary>
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
            throw new KeyNotFoundException($"Recurring payment with ID {recurringPaymentId} was not found.");
        }

        /// <summary>Returns all recurring payments belonging to <paramref name="userId"/>, ordered by next execution date ascending.</summary>
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

        /// <summary>Returns all recurring payments whose <c>NextExecutionDate</c> is on or before <paramref name="dueBeforeDateTime"/>, ordered ascending.</summary>
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

        /// <summary>Updates mutable fields (amount, frequency, dates, status) of an existing recurring payment row.</summary>
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

        /// <summary>Permanently removes the recurring payment row with the given <paramref name="recurringPaymentId"/>.</summary>
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

        /// <summary>Hydrates a <see cref="RecurringPayment"/> from the current row of <paramref name="reader"/>.</summary>
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