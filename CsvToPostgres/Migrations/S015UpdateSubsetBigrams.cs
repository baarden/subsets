using System;
using System.Data;
using DbUp.Engine;
using Npgsql;

namespace CsvToPostgres.Migrations;

public class S015UpdateSubsetBigrams : IScript
{
    public string ProvideScript(Func<IDbCommand> dbCommandFactory)
    {
        var cmd = dbCommandFactory();
        var conn = (NpgsqlConnection)cmd.Connection!;

        string query = @"
            WITH subsetWords AS (
                SELECT d.id, di.wordId, di.subsetseq
                FROM {0} d
                CROSS JOIN unnest(d.wordIds) WITH ORDINALITY AS di(wordId, subsetseq)
                WHERE deleted = false
                    AND d.id > {1} AND d.id <= {2}
            ),
            word_pairs AS (
                SELECT sw1.id, sw1.wordId AS wordid, sw2.wordId AS wordid2
                FROM subsetWords sw1
                JOIN subsetWords sw2 ON sw1.id = sw2.id AND sw2.subsetseq = sw1.subsetseq + 1
            ),
            bigrams AS (
				select l.id, array_agg(b.id) bigrams
				from word_pairs l
					join Bigrams b on b.bigram = ARRAY[l.wordid, l.wordid2]
				where wordid2 is not null
				group by l.id
			)			
            UPDATE {0} d
            SET bigrams = b.bigrams
            FROM bigrams b
            WHERE d.id = b.id;
        ";

        // Process plusonemore table
        Console.WriteLine("Processing plusonemore table...");
        ProcessTable(conn, "plusonemore", query, batchSize: 10000);
        Console.WriteLine("Creating index on plusonemore.bigrams...");
        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS ix_plusonemore_bigrams ON plusonemore USING GIN (bigrams);");

        // Process plusone table
        Console.WriteLine("Processing plusone table...");
        ProcessTable(conn, "plusone", query, batchSize: 10000);
        Console.WriteLine("Creating index on plusone.bigrams...");
        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS ix_plusone_bigrams ON plusone USING GIN (bigrams);");

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
            ExecuteNonQuery(conn, query);

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
