using System;
using System.Data;
using DbUp.Engine;
using Npgsql;

namespace CsvToPostgres.Migrations;

public class S013UpdateSubsetRankings : IScript
{
    public string ProvideScript(Func<IDbCommand> dbCommandFactory)
    {
        var cmd = dbCommandFactory();
        var conn = (NpgsqlConnection)cmd.Connection!;

        string query = @"
            UPDATE {0} d
            SET ranking = (
                SELECT SUM(w.frequency)
                FROM unnest(d.WordIds) di(id)
                JOIN Words w ON w.id = di.id
            )
            WHERE d.deleted = false
                AND d.id > {1} AND d.id <= {2};
        ";

        // Process plusonemore table
        Console.WriteLine("Processing plusonemore table...");
        ProcessTable(conn, "plusonemore", query, batchSize: 10000);

        // Process plusone table
        Console.WriteLine("Processing plusone table...");
        ProcessTable(conn, "plusone", query, batchSize: 10000);

        return string.Empty;
    }

    private static void ProcessTable(NpgsqlConnection conn, string tableName, string queryTemplate, int batchSize)
    {
        int maxId = GetMaxId(conn, tableName);
        Console.WriteLine($"{tableName}: Processing up to id {maxId}");

        int batchNumber = 0;
        for (int offset = 0; offset <= maxId; offset += batchSize)
        {
            batchNumber++;
            string query = string.Format(queryTemplate, tableName, offset, offset + batchSize);
            int rowsAffected = ExecuteNonQuery(conn, query);

            if (batchNumber % 100 == 0)
            {
                Console.WriteLine($"Batch {batchNumber}");
            }
        }

        Console.WriteLine($"Completed processing {tableName} after {batchNumber} batches");
    }

    private static int GetMaxId(NpgsqlConnection connection, string tableName)
    {
        string maxIdQuery = $"SELECT COALESCE(MAX(id), 0) FROM {tableName}";
        using var cmd = new NpgsqlCommand(maxIdQuery, connection);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int ExecuteNonQuery(NpgsqlConnection connection, string query)
    {
        using var cmd = new NpgsqlCommand(query, connection)
        {
            CommandTimeout = 600  // 10 minutes per batch
        };
        return cmd.ExecuteNonQuery();
    }
}
