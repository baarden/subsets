using System;
using System.Reflection;
using Npgsql;

class Program
{
    private static string connectionString = "Host=localhost;Username=admin;Password=T9Bt4M7tSB!r;Database=delta3";


    static void Main()
    {
        while (true)
        {
            var (index, words) = GetBestDelta();
            if (words == null)
            {
                Console.WriteLine("No delta available.");
                continue;
            }

            DisplayWords(index, words);
            int option = GetUserOption();

            switch (option)
            {
                case 1:
                    ProcessDelta(index);
                    break;
                case 2:
                    DeleteWord();
                    break;
                case 3:
                    DeleteWordPair();
                    break;
                case 4:
                    DeleteDelta(index);
                    break;
                default:
                    Console.WriteLine("Invalid option, try again.");
                    break;
            }
        }
    }

    private static (int, string[]?) GetBestDelta()
    {
        using (var conn = new NpgsqlConnection(connectionString))
        {
            conn.Open();
            var query = @"
                SELECT id, deltawords
                FROM deltas
                WHERE batch IS NULL
                ORDER BY ranking DESC
                LIMIT 1";
            using var cmd = new NpgsqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                int index = reader.GetInt32(0);
                var words = reader.GetFieldValue<string[]>(1);
                return (index, words);
            }
        }
        return (0, null);
    }

    private static void DisplayWords(int index, string[] words)
    {
        Console.WriteLine($"Next delta ({index}):");
        foreach (var word in words)
        {
            Console.WriteLine(word);
        }
    }

    private static int GetUserOption()
    {
        Console.WriteLine("Choose an option:");
        Console.WriteLine("1 - Accept delta");
        Console.WriteLine("2 - Delete word");
        Console.WriteLine("3 - Delete word pair");
        Console.WriteLine("4 - Delete delta");
        return int.TryParse(Console.ReadLine(), out int option) ? option : 0;
    }

    private static void ProcessDelta(int index)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        using var transaction = conn.BeginTransaction();
        try
        {
            UpdateDelta(index, conn);
            DeleteDeltaOverlaps(index, conn);
            MoveFirstLastOverlapsToNextBatch(index, conn);
            AddAnagram(index, conn);
            AddAnagramClue(index, conn);

            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine("An error occurred: " + ex.Message);
        }
    }

    private static void UpdateDelta(int index, NpgsqlConnection conn)
    {
        var queryUpdate = @"
                update deltas d
                set batch = 1
                where id = @id;";

        using var cmdUpdate = new NpgsqlCommand(queryUpdate, conn);
        cmdUpdate.Parameters.AddWithValue("@id", index);
        cmdUpdate.ExecuteNonQuery();
        Console.WriteLine("Delta updated.");
    }

    private static void DeleteDeltaOverlaps(int index, NpgsqlConnection conn)
    {
        var queryDeleteDeltaOverlaps = @"
                with bestdelta as (
	                select id, bigrams
	                from deltas
	                where id = @id
                ), deletions as (
	                select d.id cte_id
	                from deltas d
		                cross join bestdelta bd
	                where d.id != bd.id and cardinality(d.bigrams & bd.bigrams) > 1
                )
                delete from deltas
                where id in (select cte_id from deletions);";

        using var cmdDeleteDeltas = new NpgsqlCommand(queryDeleteDeltaOverlaps, conn);
        cmdDeleteDeltas.Parameters.AddWithValue("@id", index);
        cmdDeleteDeltas.ExecuteNonQuery();
        Console.WriteLine("Deltas with 2+ bigram overlaps deleted.");
    }

    private static void MoveFirstLastOverlapsToNextBatch(int index, NpgsqlConnection conn)
    {
        var queryMoveOverlaps = @"
            with bestdelta as (
	            select id, bigrams, deltaWords[1] startWord, deltaWords[6] endWord
	            from deltas
	            where id = @id
            ), updates as (
	            select d.id update_id
	            from deltas d
		            cross join bestdelta bd
	            where d.id != bd.id
		            and d.batch is null
		            and (cardinality(d.bigrams & bd.bigrams) = 1 
		            or d.deltaWords[1] = bd.startWord
		            or d.deltaWords[6] = endWord)
            )
            update deltas
            set batch = 2
            where id in (select update_id from updates);";
        
        using var cmdMoveOverlaps = new NpgsqlCommand(queryMoveOverlaps, conn);
        cmdMoveOverlaps.Parameters.AddWithValue("@id", index);
        cmdMoveOverlaps.ExecuteNonQuery();
        Console.WriteLine("Delta with first/last word overlap or bigram overlap moved to next batch.");
    }

    private static void AddAnagram(int index, NpgsqlConnection conn)
    {
        var queryAddAnagram = @"
            with sources as (
	            select d.id deltaId,
                    row_number() over (order by w.Length) idx,
                    d.deltaIds,
                    w.chars,
                    w.SynSets,
                    w.Length,
                    w.trigrams
	            from deltas d
		            cross join unnest(d.deltawords) dw(word)
		            join Words w on w.Word = dw.word
	            where d.id = @id
            ), combos as (
	            select row_number() over () idx, 
		            a.chr||b.chr||c.chr||d.chr||e.chr||f.chr src,
		            ARRAY[a.chr, b.chr, c.chr, d.chr, e.chr, f.chr] srcArray
	            from
		            (select idx, unnest(chars) chr from sources where idx = 1) a
		            cross join (select idx, unnest(chars) chr from sources where idx = 2) b
		            cross join (select idx, unnest(chars) chr from sources where idx = 3) c
		            cross join (select idx, unnest(chars) chr from sources where idx = 4) d
		            cross join (select idx, unnest(chars) chr from sources where idx = 5) e
		            cross join (select idx, unnest(chars) chr from sources where idx = 6) f
            ), aggregates as (
	            select c.idx,
		            string_agg(t.chr, '' order by t.chr) sortedChars,
		            array_agg(t.wordIdx order by t.chr) sortedWordIdx	
	            from combos c
		            cross join unnest(c.srcArray) WITH ORDINALITY AS t(chr, wordIdx)
	            group by c.idx
            ), anagrams as (
	            select s.deltaId, a.idx, w.Word,
                    cardinality(w.SynSets & s.SynSets) synOverlap,
                    w.frequency, w.Sorted, a.sortedWordIdx, s.deltaIds
	            from aggregates a
		            join Words w on w.Sorted = a.sortedChars
		            cross join sources s 
                    left join Deltas d on d.anagram = w.word
	            where s.Length = 6 and d.anagram is null
	            group by a.idx, w.Word, w.SynSets, s.SynSets, w.frequency,
                    w.sorted, a.sortedWordIdx, s.deltaIds, s.deltaId
	            having sum((w.trigrams && s.trigrams)::INT) = 0
            ), bestAnagram as (
	            select deltaId, Word, Sorted, sortedWordIdx, deltaIds, synoverlap, frequency
	            from anagrams
	            where synOverlap is not null
	            order by synOverlap desc, Frequency desc
	            limit 1
            ), characterSources as (
	            SELECT t.wordIdx,
	                substring(a.sorted from t.idx::INT for 1) AS character,
		            w.Id wordId, w.Word
	            FROM bestAnagram a
	                cross join UNNEST(a.sortedWordIdx) WITH ORDINALITY AS t(wordIdx, idx)
		            join Words w on w.id = a.deltaIds[t.wordIdx]
            ), offsets as (
	            select array_agg(position(cs.character in cs.word) order by cs.wordidx) charOffsets
	            from characterSources cs
            )
            update deltas d
            set anagram = ba.Word, anagramOffsets = o.charOffsets
            from bestAnagram ba
	            cross join offsets o
            where d.id = ba.deltaId;";

        using var cmdAddAnagram = new NpgsqlCommand(queryAddAnagram, conn);
        cmdAddAnagram.Parameters.AddWithValue("@id", index);
        cmdAddAnagram.ExecuteNonQuery();
        Console.WriteLine("Delta updated with anagram.");
    }

    private static void AddAnagramClue(int index, NpgsqlConnection conn)
    {
        var queryAnagramClue = @"
            update deltas d
            set clueword = sq.synonym
            from (
	            select dd.id, t.synonym
	            from deltas dd
		            join Words w on w.word = dd.Anagram
		            cross join unnest(w.SynSets) s(id) 
		            join synset ss on ss.id = s.id
		            cross join unnest(ss.synset) t(synonym)
		            join Words sw on sw.Word = t.synonym
	            where dd.id = @id
		            and not (sw.Trigrams && w.Trigrams)
	            group by dd.id, t.synonym, sw.Frequency
	            order by count(*) desc, sw.Frequency desc
	            limit 1
            ) sq
            where d.id = sq.id";

        using var cmdAnagramClue = new NpgsqlCommand(queryAnagramClue, conn);
        cmdAnagramClue.Parameters.AddWithValue("@id", index);
        cmdAnagramClue.ExecuteNonQuery();
        Console.WriteLine("Delta updated with anagram clue.");
    }

    private static void DeleteWord()
    {
        Console.WriteLine("Which word to delete?");
        string? wordToDelete = Console.ReadLine();
        if (string.IsNullOrEmpty(wordToDelete)) { return; }

        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        using var transaction = conn.BeginTransaction();
        try
        {
            using var cmdInsert = new NpgsqlCommand("INSERT INTO NonWords (nonword) VALUES (@word);", conn);
            cmdInsert.Parameters.AddWithValue("@word", wordToDelete);
            cmdInsert.ExecuteNonQuery();

            var queryDeleteDeltas = @"
                DELETE FROM deltas d
                WHERE @word = ANY(deltawords);";

            using var cmdDeleteDeltas = new NpgsqlCommand(queryDeleteDeltas, conn);
            cmdDeleteDeltas.Parameters.AddWithValue("@word", wordToDelete);
            cmdDeleteDeltas.ExecuteNonQuery();

            transaction.Commit();
            Console.WriteLine("Word deleted and relevant deltas updated.");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine("An error occurred: " + ex.Message);
        }
    }

    private static void DeleteWordPair()
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

        int? wordPairId = InsertBadWordPair(words);
        if (wordPairId == null) { return; }

        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        var queryDelete = @"
            delete from deltas d
            using badwordpairs bwp 
            where bwp.id = @wordPairId
	            and cardinality(d.deltaids & bwp.wordPair) = 2
        ";

        using var cmdDelete = new NpgsqlCommand(queryDelete, conn);
        cmdDelete.Parameters.AddWithValue("@wordPairId", wordPairId);
        cmdDelete.ExecuteNonQuery();
        Console.WriteLine("Deleted deltas containing word pair");
    }

    static int? InsertBadWordPair(string[] words)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

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

    private static void DeleteDelta(int index)
    {
        using var conn = new NpgsqlConnection(connectionString);
        try
        {
            conn.Open();
            using var cmd = new NpgsqlCommand("delete from deltas where id=@id;", conn);
            cmd.Parameters.AddWithValue("@id", index);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
        Console.WriteLine("Delta deleted.");
    }
}
