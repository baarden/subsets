using DbUp.Engine;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using Npgsql;
using CsvHelper;
using CsvHelper.Configuration;
using System.Linq;
using System.Text.RegularExpressions;


namespace CsvToPostgres.Migrations;

public partial class S006InsertSynsets : IScript
{
    [GeneratedRegex(@"^[a-zA-Z]+$")]
    private static partial Regex MyRegex();
    
    public string ProvideScript(Func<IDbCommand> dbCommandFactory)
    {
        Console.WriteLine("Location of thesaurus file:");

        string? csvFilePath = Console.ReadLine();
        if (string.IsNullOrEmpty(csvFilePath)) { throw new Exception("File path cannot be empty."); }

        var cmd = dbCommandFactory();
        var conn = (NpgsqlConnection)cmd.Connection!;

        using var reader = new StreamReader(csvFilePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
            NewLine = Environment.NewLine,
        });

        int lineCount = 0;
        while (csv.Read())
        {
            var synset = new List<string>();
            for (var i = 0; i < csv.Parser.Count; i++)
            {
                var field = csv.GetField<string>(i);
                if (IsAlphabetic(field))
                {
                    synset.Add(field);
                }
            }

            if (synset.Count != 0)
            {
                using var insertCmd = new NpgsqlCommand("INSERT INTO SynSet (SynSet) VALUES (@synset)", conn);
                insertCmd.Parameters.AddWithValue("synset", synset.ToArray());
                insertCmd.ExecuteNonQuery();
            }

            lineCount++;
            if (lineCount % 1000 == 0)
            {
                Console.WriteLine($"{lineCount} lines processed...");
            }
        }

        return string.Empty;
    }

    static bool IsAlphabetic(string input)
    {
        if (input == null) { return false; }
        return MyRegex().IsMatch(input);
    }

}
