using System;
using System.Collections.Generic;
using BankingAppTeamB.Data;
using BankingAppTeamB.Models;
using Microsoft.Data.SqlClient;

namespace BankingAppTeamB.Repositories
{
    public class BeneficiaryRepository : IBeneficiaryRepository
    {
        /// <summary>Adds a new beneficiary row to the database and sets the new ID on the object.</summary>
        public void Add(Beneficiary beneficiary)
        {
            string sqlQuery = @"
                INSERT INTO Beneficiaries
                    (UserId, Name, IBAN, BankName, LastTransferDate,
                     TotalAmountSent, TransferCount, CreatedAt)
                OUTPUT INSERTED.Id
                VALUES
                    (@UserId, @Name, @IBAN, @BankName, @LastTransferDate,
                     @TotalAmountSent, @TransferCount, @CreatedAt)";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserId", beneficiary.UserId));
                    command.Parameters.Add(new SqlParameter("@Name", beneficiary.Name));
                    command.Parameters.Add(new SqlParameter("@IBAN", beneficiary.IBAN));
                    command.Parameters.Add(new SqlParameter("@BankName", (object?)beneficiary.BankName ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@LastTransferDate", (object?)beneficiary.LastTransferDate ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@TotalAmountSent", beneficiary.TotalAmountSent));
                    command.Parameters.Add(new SqlParameter("@TransferCount", beneficiary.TransferCount));
                    command.Parameters.Add(new SqlParameter("@CreatedAt", beneficiary.CreatedAt));

                    beneficiary.Id = (int)command.ExecuteScalar();
                }
            }
        }

        /// <summary>Gets one beneficiary by ID or throws if it is not found.</summary>
        public Beneficiary GetById(int beneficiaryId)
        {
            string sqlQuery = "SELECT * FROM Beneficiaries WHERE Id = @Id";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new SqlParameter("@Id", beneficiaryId));

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapBeneficiary(reader);
                        }
                    }
                }
            }
            throw new KeyNotFoundException($"Beneficiary with ID {beneficiaryId} was not found.");
        }

        /// <summary>Gets all beneficiaries for one user, sorted by name.</summary>
        public List<Beneficiary> GetByUserId(int userId)
        {
            string sqlQuery = "SELECT * FROM Beneficiaries WHERE UserId = @UserId ORDER BY Name ASC";
            var beneficiaries = new List<Beneficiary>();

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
                            beneficiaries.Add(MapBeneficiary(reader));
                        }
                    }
                }
            }
            return beneficiaries;
        }

        /// <summary>Updates an existing beneficiary's editable info and transfer stats.</summary>
        public void Update(Beneficiary beneficiary)
        {
            string sqlQuery = @"
                UPDATE Beneficiaries SET
                    Name                = @Name,
                    IBAN                = @IBAN,
                    BankName            = @BankName,
                    LastTransferDate    = @LastTransferDate,
                    TotalAmountSent     = @TotalAmountSent,
                    TransferCount       = @TransferCount
                WHERE Id = @Id";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new SqlParameter("@Name", beneficiary.Name));
                    command.Parameters.Add(new SqlParameter("@IBAN", beneficiary.IBAN));
                    command.Parameters.Add(new SqlParameter("@BankName", (object?)beneficiary.BankName ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@LastTransferDate", (object?)beneficiary.LastTransferDate ?? DBNull.Value));
                    command.Parameters.Add(new SqlParameter("@TotalAmountSent", beneficiary.TotalAmountSent));
                    command.Parameters.Add(new SqlParameter("@TransferCount", beneficiary.TransferCount));
                    command.Parameters.Add(new SqlParameter("@Id", beneficiary.Id));
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Deletes a beneficiary by ID.</summary>
        public void Delete(int beneficiaryId)
        {
            string sqlQuery = "DELETE FROM Beneficiaries WHERE Id = @Id";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new SqlParameter("@Id", beneficiaryId));
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Checks if this user already has a beneficiary with the same IBAN.</summary>
        public bool ExistsByIBAN(int userId, string iban)
        {
            const int noRecordsFound = 0;
            string sqlQuery = "SELECT COUNT(1) FROM Beneficiaries WHERE UserId = @UserId AND IBAN = @IBAN";

            using (var connection = AppDatabase.GetConnection())
            {
                connection.Open();
                using (var command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.Add(new SqlParameter("@UserId", userId));
                    command.Parameters.Add(new SqlParameter("@IBAN", iban));
                    return (int)command.ExecuteScalar() > noRecordsFound;
                }
            }
        }

        /// <summary>Builds a Beneficiary object from the current database row.</summary>
        private Beneficiary MapBeneficiary(SqlDataReader reader)
        {
            return new Beneficiary
            {
                Id = (int)reader["Id"],
                UserId = (int)reader["UserId"],
                Name = (string)reader["Name"],
                IBAN = (string)reader["IBAN"],
                BankName = reader["BankName"] == DBNull.Value ? null : (string)reader["BankName"],
                LastTransferDate = reader["LastTransferDate"] == DBNull.Value ? null : (DateTime?)reader["LastTransferDate"],
                TotalAmountSent = (decimal)reader["TotalAmountSent"],
                TransferCount = (int)reader["TransferCount"],
                CreatedAt = (DateTime)reader["CreatedAt"]
            };
        }
    }
}
