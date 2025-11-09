using System;
using System.Data;
using DbUp.Engine;
using Npgsql;

namespace CsvToPostgres.Migrations;

public class S013bUpdateSubsetRankNums : IScript
{
    public string ProvideScript(Func<IDbCommand> dbCommandFactory)
    {
        var cmd = dbCommandFactory();
        var conn = (NpgsqlConnection)cmd.Connection!;

        // Process plusonemore table with target ranking 97.0
        Console.WriteLine("Processing plusonemore table...");
        ProcessTable(conn, "plusonemore", 97.0);

        // Process plusone table with target ranking 83.0
        Console.WriteLine("Processing plusone table...");
        ProcessTable(conn, "plusone", 83.0);

        return string.Empty;
    }

    private static void ProcessTable(NpgsqlConnection conn, string tableName, double targetRanking)
    {
        string queryTemplate = @"
            WITH ranked AS (
              SELECT
                ctid,
                row_number() OVER (ORDER BY abs(ranking - {1})) AS new_ranknum
              FROM {0}
            )
            UPDATE {0}
            SET ranknum = ranked.new_ranknum
            FROM ranked
            WHERE {0}.ctid = ranked.ctid;
        ";

        string query = string.Format(queryTemplate, tableName, targetRanking);
        int rowsAffected = ExecuteNonQuery(conn, query);
        Console.WriteLine($"Updated {rowsAffected} rows in {tableName}");

        string indexQuery = $"CREATE INDEX IF NOT EXISTS ix_{tableName}_ranknum ON {tableName} (ranknum);";
        Console.WriteLine($"Creating index on {tableName}.ranknum...");
        ExecuteNonQuery(conn, indexQuery);
        Console.WriteLine($"Completed processing {tableName}");
    }

    private static int ExecuteNonQuery(NpgsqlConnection connection, string query)
    {
        using var cmd = new NpgsqlCommand(query, connection)
        {
            CommandTimeout = 3600  // 60 minutes
        };
        return cmd.ExecuteNonQuery();
    }
}
