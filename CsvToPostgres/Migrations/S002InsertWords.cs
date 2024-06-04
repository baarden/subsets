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

public partial class S002InsertWords : IScript
{
    public string ProvideScript(Func<IDbCommand> dbCommandFactory)
    {
        Console.WriteLine("Location of dictionary file:");

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

        var records = new List<WordRecord>();
        int batchSize = 1000;
        int batchIdx = 1;
        while (csv.Read())
        {
            var word = csv.GetField<string>("word");
            var count = csv.GetField<long>("count");
            double freq = Math.Log(count);
            int length = word.Length;

            if (length > 8 || length < 3) { continue; }

            records.Add(new WordRecord(
                Word: word,
                Length: length,
                Frequency: freq,
                Sorted: new string(word.OrderBy(c => c).ToArray()),
                Subsets: GetSubsets(word),
                //Subsets: GetSortedSubsets(word),
                Trigrams: GetTrigrams(word),
                Chars: GetChars(word)
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

    private static void InsertIntoDatabase(List<WordRecord> words, NpgsqlConnection conn)
    {

        string insertQuery = @"
                INSERT INTO Words (Word, Length, Frequency, Sorted, Subsets, Trigrams, Chars) 
                VALUES (@word, @length, @frequency, @sorted, @subsets, @trigrams, @chars);
            ";

        using var transaction = conn.BeginTransaction();
        foreach (var word in words)
        {
            using var cmd = new NpgsqlCommand(insertQuery, conn);
            cmd.Parameters.AddWithValue("word", word.Word);
            cmd.Parameters.AddWithValue("length", word.Length);
            cmd.Parameters.AddWithValue("frequency", word.Frequency);
            cmd.Parameters.AddWithValue("sorted", word.Sorted);
            cmd.Parameters.AddWithValue("subsets", word.Subsets);
            cmd.Parameters.AddWithValue("trigrams", word.Trigrams);
            cmd.Parameters.AddWithValue("chars", word.Chars);
            cmd.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    static string[] GetSortedSubsets(string word)
    {
        var sortedWord = new string(word.OrderBy(c => c).ToArray());
        var subsets = new HashSet<string>();

        for (int i = 0; i < sortedWord.Length; i++)
        {
            var subset = sortedWord.Remove(i, 1);
            subsets.Add(subset);
        }

        return [.. subsets];
    }

    static string[] GetSubsets(string word)
    {
        var subsets = new HashSet<string>();

        for (int i = 0; i < word.Length; i++)
        {
            var subset = word.Remove(i, 1);
            subsets.Add(subset);
        }

        return [.. subsets];
    }

    static string[] GetTrigrams(string word)
    {
        var trigrams = new HashSet<string>();

        for (int i = 0; i < word.Length - 2; i++)
        {
            trigrams.Add(word.Substring(i, 3));
        }

        return [.. trigrams];
    }

    static string[] GetChars(string word)
    {
        string[] distinctChars = word
                    .ToCharArray()
                    .Distinct()
                    .Select(c => c.ToString())
                    .ToArray();
        return distinctChars;
    }
}

record WordRecord(
    string Word,
    int Length,
    double Frequency,
    string Sorted,
    string[] Subsets,
    string[] Trigrams,
    string[] Chars
);