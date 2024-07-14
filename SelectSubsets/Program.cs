using System;
using System.Reflection;
using Npgsql;

/*
Run this query afterward to set playdates for the output:

with playdates as (
	select s.id,
		pd.lastDate + (row_number() over (order by random()) || ' days')::interval playdate
	from subsets2 s
	  cross join (select max(playdate) lastDate from subsets2) pd
	where batch is not null and playdate is null
)
update subsets2 s
set playdate = p.playdate
from playdates p
where s.id = p.id
;

*/

class Program
{
    private static string connectionString = "Host=localhost;Username=admin;Password=T9Bt4M7tSB!r;Database=subsets2";
    private static int _batch = 2;

    static void Main()
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        while (true)
        {
            var (index, words) = GetBestSubsets(conn);            
            if (words == null)
            {
                Console.WriteLine("No subset available.");
                continue;
            }
            var (anagram, anagramSourceChars) = GetAnagram(index, conn);
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

    private static (int, string[]?) GetBestSubsets(NpgsqlConnection conn)
    {
        const double targetRanking = 97.0;  // The median of a complete run of subsets

        var query = @"
            SELECT id, words
            FROM subsets2
            WHERE batchPool = @batch
            ORDER BY ABS(ranking - @targetRanking)
            LIMIT 1";
        using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@batch", _batch);
        cmd.Parameters.AddWithValue("@targetRanking", targetRanking);
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
        Console.WriteLine("1 - Accept subset");
        Console.WriteLine("2 - Delete word");
        Console.WriteLine("3 - Delete word pair");
        Console.WriteLine("4 - Delete subset");
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
            Console.WriteLine("Cannot accept a subset with no anagram or clue");
            return;
        }

        using var transaction = conn.BeginTransaction();
        try
        {
            UpdateSubset(index, anagram, anagramSourceChars, clue, conn);
            DeleteSubsetOverlaps(index, conn);
            MoveFirstLastOverlapsToNextBatch(index, conn);
            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine("An error occurred: " + ex.Message);
        }
    }

    private static void UpdateSubset(
        int index,
        string anagram,
        string[] anagramSourceChars,
        string clue,
        NpgsqlConnection conn)
    {
        var queryUpdate = @"
            update subsets2 d
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
        Console.WriteLine("Subset updated.");
    }

    private static void DeleteSubsetOverlaps(int index, NpgsqlConnection conn)
    {
        var queryDeleteSubsetOverlaps = @"
            with bestsubset as (
                select id, bigrams
                from subsets2
                where id = @id
            ), deletions as (
                select d.id cte_id
                from subsets2 d
                    cross join bestsubset bd
                where d.id != bd.id and cardinality(d.bigrams & bd.bigrams) > 1
            )
            delete from subsets2
            where id in (select cte_id from deletions);
        ";

        using var cmdDeleteSubsets = new NpgsqlCommand(queryDeleteSubsetOverlaps, conn);
        cmdDeleteSubsets.Parameters.AddWithValue("@id", index);
        cmdDeleteSubsets.CommandTimeout = 120;
        cmdDeleteSubsets.ExecuteNonQuery();
        Console.WriteLine("Subsets with 2+ bigram overlaps deleted.");
    }

    private static void MoveFirstLastOverlapsToNextBatch(int index, NpgsqlConnection conn)
    {
        var queryMoveOverlaps = @"
            with bestsubsets as (
	            select id, wordids
	            from subsets2
	            where id = @id
            ), updates as (
	            select d.id update_id
	            from subsets2 d
		            cross join bestsubsets bd
	            where d.id != bd.id
		            and d.batchPool = @batch
                    and cardinality(d.wordids & bd.wordids) > 0
            )
            update subsets2
            set batchPool = (@batch + 1)
            where id in (select update_id from updates);
        ";
        
        using var cmdMoveOverlaps = new NpgsqlCommand(queryMoveOverlaps, conn);
        cmdMoveOverlaps.Parameters.AddWithValue("@batch", _batch);
        cmdMoveOverlaps.Parameters.AddWithValue("@id", index);
        cmdMoveOverlaps.CommandTimeout = 120;
        cmdMoveOverlaps.ExecuteNonQuery();
        Console.WriteLine("Subsets with word overlap moved to next batch.");
    }

    private static (string?, string[]?) GetAnagram(int index, NpgsqlConnection conn)
    {
        var queryGetAnagram = @"
            with subsetWords as (
                select s.id subsetId, s.words, w.widx, w.word
                from subsets2 s
                    cross join unnest(s.words) with ordinality w(word, widx)
                where s.id = @sId
            ), trigrams as (
                select sw.subsetId, sw.words, array_agg(distinct t.trigram) aggTrigrams
                from subsetWords sw
                    join words w on w.word = sw.word 
                    cross join unnest(w.Trigrams) t(trigram)
                group by sw.subsetId, sw.words
            ), startWordChars as (
                select w.subsetId, c.idx, cast(c.chr as char) chr
                from subsetWords w
                    cross join unnest(string_to_array(w.word, NULL)) with ordinality c(chr, idx)
                where w.widx = 6
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
            ), extras as (
                select subsetId, widx, chr
                from leadChars
                group by subsetId, widx, chr
                having count(*) % 2 = 1
            ), subsetChars as (
                select e.subsetid, widx, v.idx, e.chr
                    from extras e
                        cross join (values (1), (2), (3)) v(idx)
                union all
                select subsetId, 6, idx, chr
                    from startWordChars 
            ), subsetsSorted as (
                select subsetid,
                    string_agg(chr, '' order by chr) sorted,
                    array_agg(chr order by widx) sourceChars
                from subsetChars
                group by subsetid, idx
            )
            select
                w.word anagram,
                array(
                    select unnest(ss.sourceChars)
                    union all
                    (select unnest(w.chars) except select unnest(ss.sourceChars))
                ) sourcechars
            from subsetsSorted ss
                join trigrams t on t.subsetid = ss.subsetid
                join words w on ss.sorted = any(w.subsets)
                    and not (t.aggTrigrams && w.trigrams)
                    and w.synsets is not null
                left join subsets2 s on s.anagram = w.word and s.batch = @batch
            where s.anagram is null
            order by coalesce(w.frequency, 0) desc, w.word
            limit 1;
        ";

        using var cmd = new NpgsqlCommand(queryGetAnagram, conn);
        cmd.Parameters.AddWithValue("@sId", index);
        cmd.Parameters.AddWithValue("@batch", _batch);
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
        
        var queryAnagramClue = @"
            SELECT t.synonym
            FROM Words w
                CROSS JOIN LATERAL unnest(w.SynSets) AS s(id) 
                JOIN synset ss ON ss.id = s.id
                CROSS JOIN LATERAL unnest(ss.synset) AS t(synonym)
                JOIN Words sw ON sw.Word = t.synonym
                left join nonwords nw on nw.nonword = sw.word
            WHERE w.word = @anagram
                AND sw.frequency > 12
                AND NOT (sw.Trigrams && w.Trigrams)
                and nw.nonword is null
            GROUP BY t.synonym, sw.Frequency
            ORDER BY count(*) desc, sw.Frequency DESC
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
        try
        {
            using var cmdInsert = new NpgsqlCommand("INSERT INTO NonWords (nonword) VALUES (@word);", conn);
            cmdInsert.Parameters.AddWithValue("@word", wordToDelete);
            cmdInsert.ExecuteNonQuery();

            var queryDeleteSubsets = @"
                delete from subsets2 s
                using words w
                where w.word = @word
	                and w.id = any(s.wordids);
             ";

            using var cmdDeleteSubsets = new NpgsqlCommand(queryDeleteSubsets, conn);
            cmdDeleteSubsets.Parameters.AddWithValue("@word", wordToDelete);
            cmdDeleteSubsets.ExecuteNonQuery();

            transaction.Commit();
            Console.WriteLine("Word and affected subsets deleted.");
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

        string[] words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 2)
        {
            Console.WriteLine("Please enter exactly two words separated by a space.");
            return;
        }

        int? wordPairId = InsertBadWordPair(words, conn);
        if (wordPairId == null) { return; }

        var queryDelete = @"
            delete from subsets2 d
            using badwordpairs bwp 
            where bwp.id = @wordPairId
	            and cardinality(d.wordids & bwp.wordPair) = 2
        ";

        using var cmdDelete = new NpgsqlCommand(queryDelete, conn);
        cmdDelete.Parameters.AddWithValue("@wordPairId", wordPairId);
        cmdDelete.ExecuteNonQuery();
        Console.WriteLine("Deleted subsets containing word pair");
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
        try
        {
            using var cmd = new NpgsqlCommand("delete from subsets2 where id=@id;", conn);
            cmd.Parameters.AddWithValue("@id", index);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
        Console.WriteLine("Subset deleted.");
    }
}
