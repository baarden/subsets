using DbUp.Engine;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using Npgsql;
using CsvHelper;
using CsvHelper.Configuration;

namespace CsvToPostgres.Migrations;

public partial class S002bInsertWordFrequency : IScript
{
    readonly int MinWordLength = 2;
    readonly int MaxWordLength = 8;

    public string ProvideScript(Func<IDbCommand> dbCommandFactory)
    {
        Console.WriteLine("Location of word frequency file:");

        string? csvFilePath = Console.ReadLine();
        if (string.IsNullOrEmpty(csvFilePath)) { throw new Exception("File path cannot be empty."); }

        var cmd = dbCommandFactory();
        var conn = (NpgsqlConnection)cmd.Connection!;

        using var reader = new StreamReader(csvFilePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null,
        });

        csv.Read();
        csv.ReadHeader();

        var records = new List<WordFrequencyRecord>();
        int batchSize = 1000;
        int batchIdx = 1;
        while (csv.Read())
        {
            var word = csv.GetField<string>("word");
            var count = csv.GetField<long>("count");
            var length = word.Length;
            double freq = Math.Log(count);

            if (length > MaxWordLength || length < MinWordLength) { continue; }

            records.Add(new WordFrequencyRecord(
                Word: word,
                Frequency: freq
            ));

            if (records.Count >= batchSize)
            {
                InsertIntoDatabase(records, conn);
                records.Clear();
                Console.WriteLine($"Inserted batch {batchIdx}");
                batchIdx++;
            }
        }

        // Insert any remaining records
        if (records.Count > 0)
        {
            InsertIntoDatabase(records, conn);
        }

        return string.Empty;
    }

    private static void InsertIntoDatabase(List<WordFrequencyRecord> words, NpgsqlConnection conn)
    {

        string insertQuery = @"
                INSERT INTO WordFreq (Word, Frequency) 
                VALUES (@word, @frequency);
            ";

        using var transaction = conn.BeginTransaction();
        foreach (var word in words)
        {
            using var cmd = new NpgsqlCommand(insertQuery, conn);
            cmd.Parameters.AddWithValue("word", word.Word);
            cmd.Parameters.AddWithValue("frequency", word.Frequency);
            cmd.ExecuteNonQuery();
        }
        transaction.Commit();
    }
}

record WordFrequencyRecord(
    string Word,
    double Frequency
);
