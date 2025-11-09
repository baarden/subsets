using System;
using System.Data;
using DbUp.Engine;
using Npgsql;

namespace CsvToPostgres.Migrations;

public class S012DeleteBadSubsets : IScript
{
    public string ProvideScript(Func<IDbCommand> dbCommandFactory)
    {
        var cmd = dbCommandFactory();
        var conn = (NpgsqlConnection)cmd.Connection!;

        string query = @"
            WITH s_words AS (
                SELECT id
                FROM words w
                WHERE w.word LIKE '%s'
            ),
            s_subwords AS (
                SELECT p.id, w.id AS word_ord, w.id + 1 AS word_ord_next
                FROM {0} p
                    CROSS JOIN unnest(wordids) WITH ORDINALITY w(wordidx, id)
                    JOIN s_words sw ON sw.id = w.wordidx
                WHERE p.id > {1} AND p.id <= {2}
            ),
            bad_sets AS (
                SELECT DISTINCT w1.id
                FROM s_subwords w1
                    JOIN s_subwords w2 ON w1.id = w2.id AND w2.word_ord = w1.word_ord_next
            )
            UPDATE {0} p
            SET deleted = True
            FROM bad_sets b
            WHERE p.id = b.id;
        ";

        // Process plusonemore table
        Console.WriteLine("Processing plusonemore table...");
        ProcessTable(conn, "plusonemore", query, batchSize: 10000);
        Console.WriteLine("Creating index on plusonemore.deleted...");
        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS idx_plusonemore_deleted ON plusonemore (deleted);");

        // Process plusone table
        Console.WriteLine("Processing plusone table...");
        ProcessTable(conn, "plusone", query, batchSize: 10000);
        Console.WriteLine("Creating index on plusone.deleted...");
        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS idx_plusone_deleted ON plusone (deleted);");

        return string.Empty;
    }

    private static void ProcessTable(NpgsqlConnection conn, string tableName, string queryTemplate, int batchSize)
    {
        int maxId = GetMaxId(conn, tableName);
        Console.WriteLine($"{tableName}: Processing up to id {maxId}");

        int batchNumber = 0;
        for (int offset = 0; offset < maxId; offset += batchSize)
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
