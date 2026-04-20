using System;
using System.Collections.Generic;
using BankingAppTeamB.Configuration;
using BankingAppTeamB.Models;
using Microsoft.Data.SqlClient;

namespace BankingAppTeamB.Repositories;

public class ExchangeRepository : IExchangeRepository
{
    private const bool TriggeredAlertState = true;

    /// <summary>Inserts a new exchange transaction row, writes the generated identity back, and returns the updated entity.</summary>
    public ExchangeTransaction Add(ExchangeTransaction exchangeTransaction)
    {
        using var connection = new SqlConnection(ConnectionConfigHelper.GetConnectionString());
        connection.Open();

        var insertExchangeTransactionSqlQuery = @"
                INSERT INTO ExchangeTransaction
                (UserId, SourceAccountId, TargetAccountId, SourceCurrency, TargetCurrency,
                 SourceAmount, TargetAmount, ExchangeRate, Commission, Status, CreatedAt)
                OUTPUT INSERTED.Id
                VALUES
                (@UserId, @SourceAccountId, @TargetAccountId, @SourceCurrency, @TargetCurrency,
                 @SourceAmount, @TargetAmount, @Rate, @Commission, @Status, @CreatedAt)";

        using var command = new SqlCommand(insertExchangeTransactionSqlQuery, connection);

        command.Parameters.AddWithValue("@UserId", exchangeTransaction.UserId);
        command.Parameters.AddWithValue("@SourceAccountId", exchangeTransaction.SourceAccountId);
        command.Parameters.AddWithValue("@TargetAccountId", exchangeTransaction.TargetAccountId);
        command.Parameters.AddWithValue("@SourceCurrency", exchangeTransaction.SourceCurrency);
        command.Parameters.AddWithValue("@TargetCurrency", exchangeTransaction.TargetCurrency);
        command.Parameters.AddWithValue("@SourceAmount", exchangeTransaction.SourceAmount);
        command.Parameters.AddWithValue("@TargetAmount", exchangeTransaction.TargetAmount);
        command.Parameters.AddWithValue("@Rate", exchangeTransaction.ExchangeRate);
        command.Parameters.AddWithValue("@Commission", exchangeTransaction.Commission);
        command.Parameters.AddWithValue("@Status", exchangeTransaction.Status);
        command.Parameters.AddWithValue("@CreatedAt", exchangeTransaction.CreatedAt);

        exchangeTransaction.Id = (int)command.ExecuteScalar();

        return exchangeTransaction;
    }

    /// <summary>Returns all exchange transactions for userId, ordered by creation date descending.</summary>
    public List<ExchangeTransaction> GetByUserId(int userId)
    {
        var exchangeTransactions = new List<ExchangeTransaction>();

        using var connection = new SqlConnection(ConnectionConfigHelper.GetConnectionString());
        connection.Open();

        var selectExchangeTransactionsByUserSqlQuery = @"
        SELECT * FROM ExchangeTransaction
        WHERE UserId = @UserId
        ORDER BY CreatedAt DESC";

        using var command = new SqlCommand(selectExchangeTransactionsByUserSqlQuery, connection);
        command.Parameters.AddWithValue("@UserId", userId);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            exchangeTransactions.Add(new ExchangeTransaction
            {
                Id = (int)reader["Id"],
                UserId = (int)reader["UserId"],
                SourceCurrency = reader["SourceCurrency"].ToString(),
                TargetCurrency = reader["TargetCurrency"].ToString(),
                SourceAmount = (decimal)reader["SourceAmount"],
                TargetAmount = (decimal)reader["TargetAmount"],
                ExchangeRate = (decimal)reader["ExchangeRate"],
                Commission = (decimal)reader["Commission"],
                Status = Enum.Parse<TransferStatus>(reader["Status"].ToString()),
                CreatedAt = (DateTime)reader["CreatedAt"]
            });
        }

        return exchangeTransactions;
    }

    /// <summary>Returns rate alerts for userId, optionally filtered by triggered state.</summary>
    public List<RateAlert> GetAlertsByUser(int userId, bool? isTriggered = null)
    {
        var rateAlerts = new List<RateAlert>();
        using var connection = new SqlConnection(ConnectionConfigHelper.GetConnectionString());
        connection.Open();

        var selectRateAlertsByUserSqlQuery = @"
        SELECT * FROM RateAlert
        WHERE UserId = @UserId
        AND (@IsTriggered IS NULL OR IsTriggered = @IsTriggered)";

        using var command = new SqlCommand(selectRateAlertsByUserSqlQuery, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@IsTriggered", (object?)isTriggered ?? DBNull.Value);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            rateAlerts.Add(MapRateAlert(reader));
        }

        return rateAlerts;
    }

    /// <summary>Hydrates a RateAlert from the current row of reader.</summary>
    private RateAlert MapRateAlert(SqlDataReader reader)
    {
        return new RateAlert
        {
            Id = (int)reader["Id"],
            UserId = (int)reader["UserId"],
            BaseCurrency = reader["BaseCurrency"].ToString(),
            TargetCurrency = reader["TargetCurrency"].ToString(),
            TargetRate = (decimal)reader["TargetRate"],
            IsTriggered = (bool)reader["IsTriggered"],
            IsBuyAlert = (bool)reader["IsBuyAlert"],
            CreatedAt = (DateTime)reader["CreatedAt"]
        };
    }

    /// <summary>Returns all rate alerts across all users, optionally filtered by triggered state.</summary>
    public List<RateAlert> GetAllAlerts(bool? isTriggered = null)
    {
        var rateAlerts = new List<RateAlert>();
        using var connection = new SqlConnection(ConnectionConfigHelper.GetConnectionString());
        connection.Open();

        var selectAllRateAlertsSqlQuery = "SELECT * FROM RateAlert WHERE @IsTriggered IS NULL OR IsTriggered = @IsTriggered";

        using var command = new SqlCommand(selectAllRateAlertsSqlQuery, connection);
        command.Parameters.AddWithValue("@IsTriggered", (object?)isTriggered ?? DBNull.Value);
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            rateAlerts.Add(MapRateAlert(reader));
        }

        return rateAlerts;
    }

    /// <summary>Inserts a new rate alert row, writes the generated identity back, and returns the updated entity.</summary>
    public RateAlert AddAlert(RateAlert rateAlert)
    {
        using var connection = new SqlConnection(ConnectionConfigHelper.GetConnectionString());
        connection.Open();

        var insertRateAlertSqlQuery = @"
        INSERT INTO RateAlert
        (UserId, BaseCurrency, TargetCurrency, TargetRate, isTriggered, isBuyAlert, CreatedAt)
        OUTPUT INSERTED.Id
        VALUES (@UserId, @Base, @Target, @Rate, @IsTriggered, @IsBuyAlert, @CreatedAt)";

        using var command = new SqlCommand(insertRateAlertSqlQuery, connection);

        command.Parameters.AddWithValue("@UserId", rateAlert.UserId);
        command.Parameters.AddWithValue("@Base", rateAlert.BaseCurrency);
        command.Parameters.AddWithValue("@Target", rateAlert.TargetCurrency);
        command.Parameters.AddWithValue("@Rate", rateAlert.TargetRate);
        command.Parameters.AddWithValue("@IsTriggered", rateAlert.IsTriggered);
        command.Parameters.AddWithValue("@CreatedAt", rateAlert.CreatedAt);
        command.Parameters.AddWithValue("@IsBuyAlert", rateAlert.IsBuyAlert);

        rateAlert.Id = (int)command.ExecuteScalar();

        return rateAlert;
    }

    /// <summary>Permanently removes the rate alert row with the given rateAlertId.</summary>
    public void DeleteAlert(int rateAlertId)
    {
        using var connection = new SqlConnection(ConnectionConfigHelper.GetConnectionString());
        connection.Open();

        using var command = new SqlCommand("DELETE FROM RateAlert WHERE Id = @Id", connection);

        command.Parameters.AddWithValue("@Id", rateAlertId);
        command.ExecuteNonQuery();
    }

    /// <summary>Sets IsTriggered = true for the rate alert identified by rateAlertId.</summary>
    public void MarkAlertTriggered(int rateAlertId)
    {
        using var connection = new SqlConnection(ConnectionConfigHelper.GetConnectionString());
        connection.Open();

        using var command = new SqlCommand(
            "UPDATE RateAlert SET IsTriggered = @IsTriggered WHERE Id = @Id", connection);

        command.Parameters.AddWithValue("@Id", rateAlertId);
        command.Parameters.AddWithValue("@IsTriggered", TriggeredAlertState);

        command.ExecuteNonQuery();
    }
}