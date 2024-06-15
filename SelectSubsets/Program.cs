using System;
using System.Reflection;
using Npgsql;

class Program
{
    private static string connectionString = "Host=localhost;Username=admin;Password=T9Bt4M7tSB!r;Database=subsets2";
    private static int _batch = 3;

    static void Main()
    {
        while (true)
        {
            var (index, words) = GetBestSubsets();
            if (words == null)
            {
                Console.WriteLine("No subset available.");
                continue;
            }

            DisplayWords(index, words);
            int option = GetUserOption();

            switch (option)
            {
                case 1:
                    ProcessSubset(index);
                    break;
                case 2:
                    DeleteWord();
                    break;
                case 3:
                    DeleteWordPair();
                    break;
                case 4:
                    DeleteSubset(index);
                    break;
                default:
                    Console.WriteLine("Invalid option, try again.");
                    break;
            }
        }
    }

    private static (int, string[]?) GetBestSubsets()
    {
        const double targetRanking = 97.0;  // The median of a complete run of subsets
        using (var conn = new NpgsqlConnection(connectionString))
        {
            conn.Open();
            var query = @"
                SELECT id, words
                FROM subsets
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
        }
        return (0, null);
    }

    private static void DisplayWords(int index, string[] words)
    {
        Console.WriteLine($"\nNext subset ({index}):");
        foreach (var word in words)
        {
            Console.WriteLine(word);
        }
    }

    private static int GetUserOption()
    {
        Console.WriteLine("Choose an option:");
        Console.WriteLine("1 - Accept subset");
        Console.WriteLine("2 - Delete word");
        Console.WriteLine("3 - Delete word pair");
        Console.WriteLine("4 - Delete subset");
        return int.TryParse(Console.ReadLine(), out int option) ? option : 0;
    }

    private static void ProcessSubset(int index)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        using var transaction = conn.BeginTransaction();
        try
        {
            UpdateSubset(index, conn);
            DeleteSubsetOverlaps(index, conn);
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

    private static void UpdateSubset(int index, NpgsqlConnection conn)
    {
        var queryUpdate = @"
                update subsets d
                set batch = @batch, batchPool = null
                where id = @id;";

        using var cmdUpdate = new NpgsqlCommand(queryUpdate, conn);
        cmdUpdate.Parameters.AddWithValue("@batch", _batch);
        cmdUpdate.Parameters.AddWithValue("@id", index);
        cmdUpdate.ExecuteNonQuery();
        Console.WriteLine("Subset updated.");
    }

    private static void DeleteSubsetOverlaps(int index, NpgsqlConnection conn)
    {
        var queryDeleteSubsetOverlaps = @"
                with bestsubset as (
	                select id, bigrams
	                from subsets
	                where id = @id
                ), deletions as (
	                select d.id cte_id
	                from subsets d
		                cross join bestsubset bd
	                where d.id != bd.id and cardinality(d.bigrams & bd.bigrams) > 1
                )
                delete from subsets
                where id in (select cte_id from deletions);";

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
	            from subsets
	            where id = @id
            ), updates as (
	            select d.id update_id
	            from subsets d
		            cross join bestsubsets bd
	            where d.id != bd.id
		            and d.batchPool = @batch
                    and cardinality(d.wordids & bd.wordids) > 0
            )
            update subsets
            set batchPool = (@batch + 1)
            where id in (select update_id from updates);";
        
        using var cmdMoveOverlaps = new NpgsqlCommand(queryMoveOverlaps, conn);
        cmdMoveOverlaps.Parameters.AddWithValue("@batch", _batch);
        cmdMoveOverlaps.Parameters.AddWithValue("@id", index);
        cmdMoveOverlaps.CommandTimeout = 120;
        cmdMoveOverlaps.ExecuteNonQuery();
        Console.WriteLine("Subsets with word overlap moved to next batch.");
    }

    private static void AddAnagram(int index, NpgsqlConnection conn)
    {
        var queryAddAnagram = @"
            create temporary table if not exists aggregates (comboId INT, sortedChars TEXT, sortedWordIdx INT[]);
            truncate table aggregates;

	        with sources as (
		        select id subsetId, wordIds
		        from subsets d
		        where d.id = @id
	        ), sourceWords as (
	            select dw.idx, w.chars
	            from sources d
	                cross join unnest(d.wordIds) WITH ORDINALITY dw(wordId, idx)
	                join Words w on w.id = dw.wordId
	        ), combos as (
	            select row_number() over () idx, 
	                a.chr||b.chr||c.chr||d.chr||e.chr||f.chr src,
	                ARRAY[a.chr, b.chr, c.chr, d.chr, e.chr, f.chr] srcArray
	            from
	                (select idx, unnest(chars) chr from sourceWords where idx = 1) a
	                cross join (select idx, unnest(chars) chr from sourceWords where idx = 2) b
	                cross join (select idx, unnest(chars) chr from sourceWords where idx = 3) c
	                cross join (select idx, unnest(chars) chr from sourceWords where idx = 4) d
	                cross join (select idx, unnest(chars) chr from sourceWords where idx = 5) e
	                cross join (select idx, unnest(chars) chr from sourceWords where idx = 6) f
	        )
	        insert into aggregates
		        select c.idx,
			        string_agg(t.chr, '' order by t.chr) sortedChars,
			        array_agg(t.wordIdx order by t.chr) sortedWordIdx	
		        from combos c
			        cross join unnest(c.srcArray) WITH ORDINALITY AS t(chr, wordIdx)
		        group by c.idx;

	        with sources as (
		        select id subsetId, wordIds
		        from subsets d
		        where d.id = @id
	        ), sourceWords as (
	            select d.subsetId, d.wordIds, w.SynSets, w.trigrams
	            from sources d
	                cross join unnest(d.wordIds) WITH ORDINALITY dw(wordId, idx)
	                join Words w on w.id = dw.wordId
	        ), syns as (
		        select d.subsetId, unnest(d.SynSets) syn
		        from sourceWords d
		        where d.synsets is not null
	        ), aggSyn as (
		        select subsetId, array_agg(DISTINCT syn) AS aggSynSet
		        from syns d
		        group by subsetId
	        ), trigrams as (
		        select subsetId, unnest(trigrams) trigram
		        from sourceWords d
	        ), aggTrigrams as (
		        select subsetId, array_agg(DISTINCT trigram) as aggTrigrams
		        from trigrams
		        group by subsetId
	        ), bestAnagram as (
	            select s.subsetId, s.wordIds, w.Word, w.Sorted, a.sortedWordIdx
	            from aggregates a
	                join Words w on w.Sorted = a.sortedChars
	                cross join sources s
		            left outer join aggsyn sy on 1=1
			        cross join aggTrigrams tri
			        left join subsets ss on ss.anagram = w.word
		        where ss.anagram is null
                    and w.synsets is not null
                    and w.frequency > 13
		        order by
			        cardinality(array(SELECT unnest(w.trigrams) INTERSECT SELECT unnest(tri.aggTrigrams))),
			        cardinality(w.synsets & sy.aggSynSet) desc,
        			w.frequency desc
	            limit 1
	        ), characterSources as (
	            SELECT t.wordIdx,
	                substring(a.sorted from t.idx::INT for 1) AS character,
	                w.Id wordId, w.Word
	            FROM bestAnagram a
	                cross join UNNEST(a.sortedWordIdx) WITH ORDINALITY AS t(wordIdx, idx)
	                join Words w on w.id = a.wordIds[t.wordIdx]
	        ), offsets as (
	            select array_agg(position(cs.character in cs.word) order by cs.wordidx) charOffsets
	            from characterSources cs
	        )
	        update subsets d
	        set anagram = ba.Word, anagramOffsets = o.charOffsets
	        from bestAnagram ba
	            cross join offsets o
	        where d.id = ba.subsetId;
        ";

        using var cmdAddAnagram = new NpgsqlCommand(queryAddAnagram, conn);
        cmdAddAnagram.Parameters.AddWithValue("@id", index);
        cmdAddAnagram.ExecuteNonQuery();
        Console.WriteLine("Subset updated with anagram.");
    }

    private static void AddAnagramClue(int index, NpgsqlConnection conn)
    {
        var queryAnagramClue = @"
            update subsets d
            set clueword = sq.synonym
            from (
                SELECT dd.id, t.synonym
                FROM subsets dd
	                JOIN Words w ON w.word = dd.Anagram
	                CROSS JOIN LATERAL unnest(w.SynSets) AS s(id) 
	                JOIN synset ss ON ss.id = s.id
	                CROSS JOIN LATERAL unnest(ss.synset) AS t(synonym)
	                JOIN Words sw ON sw.Word = t.synonym
		            left join nonwords nw on nw.nonword = sw.word
	            WHERE dd.id = @id
                    AND sw.frequency > 12
                    AND NOT (sw.Trigrams && w.Trigrams)
                    and nw.nonword is null
                GROUP BY dd.id, t.synonym, sw.Frequency
                ORDER BY count(*) desc, sw.Frequency DESC
                LIMIT 1
            ) sq
            WHERE d.id = sq.id;
        ";

        using var cmdAnagramClue = new NpgsqlCommand(queryAnagramClue, conn);
        cmdAnagramClue.Parameters.AddWithValue("@id", index);
        cmdAnagramClue.ExecuteNonQuery();
        Console.WriteLine("Subset updated with anagram clue.");
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

            var queryDeleteSubsets = @"
                delete from subsets s
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
            delete from subsets d
            using badwordpairs bwp 
            where bwp.id = @wordPairId
	            and cardinality(d.wordids & bwp.wordPair) = 2
        ";

        using var cmdDelete = new NpgsqlCommand(queryDelete, conn);
        cmdDelete.Parameters.AddWithValue("@wordPairId", wordPairId);
        cmdDelete.ExecuteNonQuery();
        Console.WriteLine("Deleted subsets containing word pair");
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

    private static void DeleteSubset(int index)
    {
        using var conn = new NpgsqlConnection(connectionString);
        try
        {
            conn.Open();
            using var cmd = new NpgsqlCommand("delete from subsets where id=@id;", conn);
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
