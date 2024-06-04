using DbUp.Engine;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using Npgsql;
using System.Linq;
using System.Text.RegularExpressions;


namespace CsvToPostgres.Migrations;

public partial class S008UpdateWordSynsets : IScript
{
    public string ProvideScript(Func<IDbCommand> dbCommandFactory)
    {
        var cmd = dbCommandFactory();
        var conn = (NpgsqlConnection)cmd.Connection!;

        int totalRows = GetTotalRowCount(conn);

        int batchSize = 500;
        for (int offset = 0; offset < totalRows; offset += batchSize)
        {
            int startId = offset + 1;
            int endId = offset + batchSize;

            string updateQuery = $@"
                UPDATE Words w
                SET Synsets = sq.synset_ids
                FROM (
                    SELECT
                        w.Id,
                        ARRAY_AGG(s.Id) AS synset_ids
                    FROM Words w
                    JOIN SynSet s ON w.Word = ANY(s.SynSet)
                    WHERE w.id BETWEEN {startId} AND {endId}
                    GROUP BY w.Id
                ) sq
                WHERE sq.id = w.id;
            ";

            var updateCmd = new NpgsqlCommand(updateQuery, conn)
            {
                CommandTimeout = 300  // seconds
            };
            updateCmd.ExecuteNonQuery();
            Console.WriteLine($"Updated Words.Synsets through row {endId}");
        }
        return string.Empty;
    }

    static int GetTotalRowCount(NpgsqlConnection connection)
    {
        string rowCountQuery = "SELECT COUNT(*) FROM Words";
        using var cmd = new NpgsqlCommand(rowCountQuery, connection);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
