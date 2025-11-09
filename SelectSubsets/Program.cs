using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Npgsql;
using SelectSubsets;

/*
Run this query afterward to set playdates for the output:

with playdates as (
	select s.id,
		pd.lastDate + (row_number() over (order by random()) || ' days')::interval playdate
	from plusonemore s
	  cross join (
        select coalesce(max(playdate), (now() + '-1 day'::interval)::date) lastDate from plusonemore
      ) pd
	where batch is not null and playdate is null
)
update plusonemore s
set playdate = p.playdate
from playdates p
where s.id = p.id
;
*/

// PlusOne
// Initial: 12794 sets
// After batch 1 (86): 7979
// After batch 2 (): 

class Program
{
    private static readonly AppSettings PlusOneSettings = new AppSettings(
        SetTable: "plusone",
        NonWordTable: "nonwordplusone",
        StartWordIndex: 5,
        TargetRanking: 83.0
    );
    private static readonly AppSettings PlusOneMoreSettings = new AppSettings(
        SetTable: "plusonemore",
        NonWordTable: "nonwordplusonemore",
        StartWordIndex: 6,
        TargetRanking: 97.0
    );
    private static readonly string connectionString = GetConnectionString();
    private static readonly int _batch = 1;
    internal static readonly char[] separator = [' '];
    private static AppSettings _config = PlusOneSettings;

    static void Main(string[] args)
    {
        bool isMore = false;
        if (args.Length > 0 && Array.Exists(args, element => element.Equals("more", StringComparison.OrdinalIgnoreCase)))
        {
            isMore = true;
        }
        _config = isMore ? PlusOneMoreSettings : PlusOneSettings;

        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        while (true)
        {
            var (index, words, anagram, anagramSourceChars) = GetValidSet(conn);            
            if (words == null)
            {
                Console.WriteLine("No set available.");
                break;
            }
            string? clue = GetAnagramClue(anagram, conn);

            DisplayWords(index, words, anagram, clue);
            int option = GetUserOption();

            switch (option)
            {
                case 1:
                    ProcessSubset(index, anagram, anagramSourceChars, clue, conn);
                    break;
                case 2:
                    DeleteWord(conn);
                    break;
                case 3:
                    DeleteWordPair(conn);
                    break;
                case 4:
                    DeleteSubset(index, conn);
                    Console.WriteLine("Set deleted.");
                    break;
                case 5:
                    clue = GetAlternateClue();
                    ProcessSubset(index, anagram, anagramSourceChars, clue, conn);
                    break;
                default:
                    Console.WriteLine("Invalid option, try again.");
                    break;
            }
        }
    }

    private static string GetConnectionString()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
            .Build();

        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private static (int, string[]?, string?, string[]?) GetValidSet(NpgsqlConnection conn)
    {
        int deletedSets = 0;
        while (true)
        {
            (int index, string[]? words) = GetBestSubsets(conn);
            if (words == null) { return (0, null, null, null); }

            (string? anagram, string[]? anagramSourceChars) = GetAnagram(index, conn);
            if (anagram == null)
            {
                deletedSets++;
                DeleteSubset(index, conn);
                continue;
            }
            if (deletedSets > 0)
            {
                Console.WriteLine($"Deleted {deletedSets} sets without anagrams.");
            }
            return (index, words, anagram, anagramSourceChars);
        }
    }

    private static (int, string[]?) GetBestSubsets(NpgsqlConnection conn)
    {
        var query = @$"
            SELECT id, words
            FROM {_config.SetTable}
            WHERE batchPool = @batch
                and deleted = false
            ORDER BY ranknum
            LIMIT 1";
        using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@batch", _batch);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            int index = reader.GetInt32(0);
            var words = reader.GetFieldValue<string[]>(1);
            return (index, words);
        }
        return (0, null);
    }

    private static void DisplayWords(int index, string[] words, string? anagram, string? clue)
    {
        Console.WriteLine($"\nNext subset ({index}):");
        foreach (var word in words.Reverse())
        {
            Console.WriteLine(word);
        }
        Console.WriteLine($"Anagram: {anagram}");
        Console.WriteLine($"Clue: {clue}");
    }

    private static int GetUserOption()
    {
        Console.WriteLine("Choose an option:");
        Console.WriteLine("1 - Accept set");
        Console.WriteLine("2 - Delete word");
        Console.WriteLine("3 - Delete word pair");
        Console.WriteLine("4 - Delete set");
        Console.WriteLine("5 - Alternate clue");
        return int.TryParse(Console.ReadLine(), out int option) ? option : 0;
    }

    private static string? GetAlternateClue()
    {
        Console.WriteLine("Enter an alternate 1-word clue:");
        string? clue = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(clue) || ! clue.All(char.IsLetter)) {
            clue = null;
        }
        return clue;
    }

    private static void ProcessSubset(
        int index,
        string? anagram,
        string[]? anagramSourceChars,
        string? clue,
        NpgsqlConnection conn)
    {
        if (anagram == null || anagramSourceChars == null || clue == null) {
            Console.WriteLine("Cannot accept a set with no anagram or clue");
            return;
        }

        using var transaction = conn.BeginTransaction();
        try
        {
            UpdateSubset(index, anagram, anagramSourceChars, clue, conn);
            DeleteSubsetOverlaps(index, conn);
            MoveWordOverlapsToNextBatch(index, conn);
            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine("An error occurred: " + ex.ToString());
        }
    }

    private static void UpdateSubset(
        int index,
        string anagram,
        string[] anagramSourceChars,
        string clue,
        NpgsqlConnection conn)
    {
        var queryUpdate = @$"
            update {_config.SetTable} d
            set batch = @batch,
                batchPool = null,
                anagram = @anagram,
                clueword = @clue,
                anagramSources = @sourceChars
            where id = @id;
        ";

        using var cmdUpdate = new NpgsqlCommand(queryUpdate, conn);
        cmdUpdate.Parameters.AddWithValue("@batch", _batch);
        cmdUpdate.Parameters.AddWithValue("@anagram", anagram);
        cmdUpdate.Parameters.AddWithValue("@clue", clue);
        cmdUpdate.Parameters.AddWithValue("@sourceChars", anagramSourceChars);
        cmdUpdate.Parameters.AddWithValue("@id", index);
        cmdUpdate.ExecuteNonQuery();
        Console.WriteLine("Set updated.");
    }

    private static void DeleteSubsetOverlaps(int index, NpgsqlConnection conn)
    {
        var queryDeleteSubsetOverlaps = @$"
            WITH target AS (
                SELECT id, bigrams
                FROM {_config.SetTable}
                WHERE id = @id
            )
            SELECT d.id AS set_id
            INTO TEMP TABLE deletions
            FROM {_config.SetTable} d
                CROSS JOIN target t
            WHERE d.id != t.id
                AND d.deleted = false
                AND d.bigrams && t.bigrams
                AND cardinality(d.bigrams & t.bigrams) > 1;

            update {_config.SetTable}
            set deleted = true
            where id in (select set_id from deletions);

            drop table deletions;
        ";

        using var cmdDeleteSubsets = new NpgsqlCommand(queryDeleteSubsetOverlaps, conn);
        cmdDeleteSubsets.Parameters.AddWithValue("@id", index);
        cmdDeleteSubsets.CommandTimeout = 120;
        cmdDeleteSubsets.ExecuteNonQuery();
        Console.WriteLine("Sets with 2+ bigram overlaps deleted.");
    }

    private static void MoveWordOverlapsToNextBatch(int index, NpgsqlConnection conn)
    {
        int batchSize = 5000;
        var queryMoveOverlaps = @$"
            WITH target AS (
                SELECT id, wordids, batchpool
                FROM {_config.SetTable}
                WHERE id = @id
            ),
            to_update AS (
                SELECT d.id
                FROM {_config.SetTable} d
                CROSS JOIN target t
                WHERE d.id != t.id
                    AND d.batchpool = @batch
                    AND d.wordids && t.wordids
                    AND d.deleted = false
                LIMIT @batchSize
            )
            UPDATE {_config.SetTable} p
            SET batchpool = (@batch + 1)
            FROM to_update u
            WHERE p.id = u.id;
        ";

        // Query without LIMIT to get accurate total estimate
        var explainQuery = @$"
            EXPLAIN (FORMAT JSON)
            WITH target AS (
                SELECT id, wordids, batchpool
                FROM {_config.SetTable}
                WHERE id = @id
            )
            SELECT d.id
            FROM {_config.SetTable} d
            CROSS JOIN target t
            WHERE d.id != t.id
                AND d.batchpool = @batch
                AND d.wordids && t.wordids
                AND d.deleted = false;
        ";

        // Get estimate of affected rows and calculate estimated batches
        int estimatedBatches = 1;
        using (var cmdExplain = new NpgsqlCommand(explainQuery, conn))
        {
            cmdExplain.CommandTimeout = 120;
            cmdExplain.Parameters.AddWithValue("@batch", _batch);
            cmdExplain.Parameters.AddWithValue("@id", index);

            using var reader = cmdExplain.ExecuteReader();
            if (reader.Read())
            {
                var jsonPlan = reader.GetString(0);
                var estimatedRows = ExtractEstimatedRows(jsonPlan);
                estimatedBatches = (int)Math.Ceiling(1.0 * estimatedRows / batchSize);
            }
        }

        int batchNumber = 0;
        int rowsAffected;
        do
        {
            using var cmdMoveOverlaps = new NpgsqlCommand(queryMoveOverlaps, conn);
            cmdMoveOverlaps.CommandTimeout = 120;
            cmdMoveOverlaps.Parameters.AddWithValue("@batch", _batch);
            cmdMoveOverlaps.Parameters.AddWithValue("@id", index);
            cmdMoveOverlaps.Parameters.AddWithValue("@batchSize", batchSize);
            rowsAffected = cmdMoveOverlaps.ExecuteNonQuery();

            if (rowsAffected > 0)
            {
                batchNumber++;
                Console.WriteLine($"Moved batch {batchNumber} (avg {estimatedBatches})");
            }
        } while (rowsAffected > 0);

    }

    private static (string?, string[]?) GetAnagram(int index, NpgsqlConnection conn)
    {
        var queryGetAnagram = @$"
            with subsetWords as (
                select s.id subsetId, s.words, w.widx, w.word
                from {_config.SetTable} s
                    cross join unnest(s.words) with ordinality w(word, widx)
                where s.id = @sId
            ), trigrams as (
                select sw.subsetId, sw.words, array_agg(distinct t.trigram) aggTrigrams
                from subsetWords sw
                    join words w on w.word = sw.word 
                    cross join unnest(w.Trigrams) t(trigram)
                group by sw.subsetId, sw.words
            ), startWordChars as (
                -- any one of the start word chars could be the plus one char
                -- set them aside so we can decide later
                select w.subsetId, c.idx, cast(c.chr as char) chr
                from subsetWords w
                    cross join unnest(string_to_array(w.word, NULL)) with ordinality c(chr, idx)
                where w.widx = @startIndex
            ), leadWords as (
                select
                    subsetId,
                    widx,
                    word,
                    lead(word, 1) over (partition by subsetId order by widx) as leadWord
                from subsetWords
            ), leadChars as (
                select lw.subsetId, lw.widx, lw.word, cast(c.chr as char) chr
                    from leadWords lw
                        cross join unnest(string_to_array(lw.word, NULL)) with ordinality c(chr, cidx)
                    where lw.leadWord is not null
                union all
                select lw.subsetId, lw.widx, lw.leadword, cast(c.chr as char) chr
                    from leadWords lw
                        cross join unnest(string_to_array(lw.leadWord, NULL)) with ordinality c(chr, cidx)
                    where lw.leadWord is not null
            ), plusones as (
                -- for subsequent pairs of words, the plus one character 
                -- will be the only one that occurs an odd number of times
                select subsetId, widx, chr
                from leadChars
                group by subsetId, widx, chr
                having count(*) % 2 = 1
            ), subsetChars as (
                -- pair each startword char with its own set of plusone chars
                select e.subsetid, widx, v.idx, e.chr
                    from plusones e
                        cross join (values (1), (2), (3)) v(idx)
                union all
                select subsetId, @startIndex, idx, chr
                    from startWordChars 
            ), subsetsSorted as (
                select subsetid,
                    string_agg(chr, '' order by chr) sorted,
                    array_agg(chr order by widx) sourceChars
                from subsetChars
                group by subsetid, idx
            ), bestAnagram as (
                -- find the most frequently used anagram word with no shared trigrams 
                -- that has synonym data and hasn't been used yet
	            select
	                w.word anagram,
	                ss.sourceChars,
					string_to_array(w.word, NULL) anagramChars
	            from subsetsSorted ss
	                join trigrams t on t.subsetid = ss.subsetid
	                join words w on ss.sorted = any(w.subsets)
	                    and not (t.aggTrigrams && w.trigrams)
	                    and w.synsets is not null
                    left join {_config.NonWordTable} nw on nw.nonword = w.word
	                left join {_config.SetTable} s on s.anagram = w.word and s.batch = @batch
	            where nw.nonword is null and s.anagram is null
	            order by coalesce(w.frequency, 0) desc, w.word
	            limit 1
			), anagramChars as (
			    SELECT distinct unnest(ba.anagramChars) AS char, 
					count(*) OVER (PARTITION BY unnest(ba.anagramChars)) AS charCount
			    FROM bestAnagram ba
			), sourceChars AS (
			    SELECT distinct unnest(ba.sourceChars) AS char,
					count(*) OVER (PARTITION BY unnest(ba.sourceChars)) AS charCount
			    FROM bestAnagram ba
			), charCounts AS (
			    SELECT
			        a.char,
			        a.charCount,
			        COALESCE(s.charCount, 0) AS sourceCharCount
			    FROM anagramChars a
			    	LEFT JOIN sourceChars s ON s.char = a.char
			), finalPlusOne as (
                -- find the one extra char in the anagram
				SELECT char
				FROM charCounts
				WHERE charCount > sourceCharCount
			)
			select
				ba.anagram,
				ba.sourceChars || po.char sourceChars
			from bestAnagram ba
				cross join finalPlusOne po
			;
        ";

        using var cmd = new NpgsqlCommand(queryGetAnagram, conn);
        cmd.Parameters.AddWithValue("@sId", index);
        cmd.Parameters.AddWithValue("@batch", _batch);
        cmd.Parameters.AddWithValue("@startIndex", _config.StartWordIndex);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var anagram = reader.GetString(0);
            var sourceChars = reader.GetFieldValue<string[]>(1);
            return (anagram, sourceChars);
        }
        return (null, null);
    }

    private static string? GetAnagramClue(string? anagram, NpgsqlConnection conn)
    {
        if (anagram == null) {
            return null;
        }
        
        var queryAnagramClue = @$"
            SELECT t.synonym
            FROM Words w
                CROSS JOIN LATERAL unnest(w.SynSets) AS s(id) 
                JOIN synset ss ON ss.id = s.id
                CROSS JOIN LATERAL unnest(ss.synset) AS t(synonym)
                JOIN Words sw ON sw.Word = t.synonym
                left join {_config.NonWordTable} nw on nw.nonword = sw.word
            WHERE w.word = @anagram
                AND sw.frequency > 12
                AND NOT (sw.Trigrams && w.Trigrams)
                and nw.nonword is null
            GROUP BY t.synonym, sw.Frequency
            ORDER BY
                case when t.synonym in ('author', 'artist') then 1 else 0 end, 
                count(*) desc, sw.Frequency DESC
            LIMIT 1;
        ";

        using var cmd = new NpgsqlCommand(queryAnagramClue, conn);
        cmd.Parameters.AddWithValue("@anagram", anagram);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return reader.GetString(0);
        }
        return null;
    }

    private static void DeleteWord(NpgsqlConnection conn)
    {
        Console.WriteLine("Which word to delete?");
        string? wordToDelete = Console.ReadLine();
        if (string.IsNullOrEmpty(wordToDelete)) { return; }

        using var transaction = conn.BeginTransaction();
        string insertQuery = $"INSERT INTO {_config.NonWordTable} (nonword) VALUES (@word);";
        try
        {
            using var cmdInsert = new NpgsqlCommand(insertQuery, conn);
            cmdInsert.Parameters.AddWithValue("@word", wordToDelete);
            cmdInsert.ExecuteNonQuery();
            int batchSize = 5000;

            var queryDeleteSubsets = @$"
                WITH to_delete AS (
                    SELECT s.id
                    FROM {_config.SetTable} s
                    WHERE s.wordids @> ARRAY[(SELECT id FROM words WHERE word = @word LIMIT 1)]
                        AND s.deleted = false
                    LIMIT @batchSize
                )
                UPDATE {_config.SetTable} s
                SET deleted = true
                FROM to_delete d
                WHERE d.id = s.id;
            ";

            // Query without LIMIT to get accurate total estimate
            var explainQuery = @$"
                EXPLAIN (FORMAT JSON)
                SELECT s.id
                FROM {_config.SetTable} s
                WHERE s.wordids @> ARRAY[(SELECT id FROM words WHERE word = @word LIMIT 1)]
                    AND s.deleted = false;
            ";

            // Get estimate of affected rows and calculate estimated batches
            int estimatedBatches = 1;
            using (var cmdExplain = new NpgsqlCommand(explainQuery, conn))
            {
                cmdExplain.CommandTimeout = 120;
                cmdExplain.Parameters.AddWithValue("@word", wordToDelete);

                using var reader = cmdExplain.ExecuteReader();
                if (reader.Read())
                {
                    var jsonPlan = reader.GetString(0);
                    var estimatedRows = ExtractEstimatedRows(jsonPlan);
                    estimatedBatches = (int)Math.Ceiling(1.0 * estimatedRows / batchSize);
                }
            }

            int batchNumber = 0;
            int rowsAffected;
            do
            {
                using var cmdDeleteSubsets = new NpgsqlCommand(queryDeleteSubsets, conn);
                cmdDeleteSubsets.CommandTimeout = 120;
                cmdDeleteSubsets.Parameters.AddWithValue("@word", wordToDelete);
                cmdDeleteSubsets.Parameters.AddWithValue("@batchSize", batchSize);
                rowsAffected = cmdDeleteSubsets.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    batchNumber++;
                    Console.WriteLine($"Deleted batch {batchNumber} (average {estimatedBatches})");
                }
            } while (rowsAffected > 0);

            transaction.Commit();
            Console.WriteLine("Word and affected sets deleted.");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine("An error occurred: " + ex.Message);
        }
    }

    private static void DeleteWordPair(NpgsqlConnection conn)
    {
        Console.WriteLine("Enter a pair of words separated by a space (e.g., 'post poster'):");
        string? input = Console.ReadLine();
        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine("No input provided.");
            return;
        }

        string[] words = input.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 2)
        {
            Console.WriteLine("Please enter exactly two words separated by a space.");
            return;
        }

        int? wordPairId = InsertBadWordPair(words, conn);
        if (wordPairId == null) { return; }

        var queryDelete = @$"
            update {_config.SetTable} d
            set deleted = true
            from badwordpairs bwp 
            where bwp.id = @wordPairId
	            and cardinality(d.wordids & bwp.wordPair) = 2
        ";

        using var cmdDelete = new NpgsqlCommand(queryDelete, conn);
        cmdDelete.Parameters.AddWithValue("@wordPairId", wordPairId);
        cmdDelete.ExecuteNonQuery();
        Console.WriteLine("Deleted sets containing word pair");
    }

    static int? InsertBadWordPair(string[] words, NpgsqlConnection conn)
    {
        string query = @"
            INSERT INTO BadWordPairs (WordPair)
            SELECT array_agg(w.id) AS wordPair
            FROM Words w
            WHERE w.Word = ANY(@words)
            RETURNING Id;
        ";

        using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.Add(new NpgsqlParameter("@words", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Text)
        {
            Value = words
        });
        object? result = cmd.ExecuteScalar();

        if (result != null)
        {
            return (int)result;
        }
        else
        {
            Console.WriteLine("No matching words found in the Words table or insertion failed.");
            return null;
        }
    }

    private static void DeleteSubset(int index, NpgsqlConnection conn)
    {
        string query = @$"
            update {_config.SetTable}
            set Deleted = true
            where id=@id
            ;
        ";
        try
        {
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", index);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
    }

    private static long ExtractEstimatedRows(string jsonPlan)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonPlan);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                var plan = root[0].GetProperty("Plan");
                if (plan.TryGetProperty("Plan Rows", out var planRows))
                {
                    long rowEst = planRows.GetInt64();
                    return rowEst;
                }
            }
            return -1;
        }
        catch
        {
            return -1;
        }
    }
}
