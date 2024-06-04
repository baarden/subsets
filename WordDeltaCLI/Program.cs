using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static WordDeltaCLI.Models;

namespace WordDeltaCLI;

class Program
{
    private static readonly HttpClient client = new();
    private const string ApiBaseUrl = "https://localhost:7186";

    static async Task Main()
    {
        client.BaseAddress = new Uri(ApiBaseUrl);
        while (true)
        {
            Status? status = await GetStatus();
            if (status == null)
            {
                Console.WriteLine("Unable to reach API, trying again.");
                await Task.Delay(5000);
                continue;
            }

            Console.WriteLine($"Clue: {status.ClueWord}");

            foreach (var guess in status.Guesses)
            {
                if (guess.State == GuessState.Unsolved && status.State == GuessState.Unsolved && guess.WordIndex < status.NextGuess!.WordIndex)
                {
                    continue;
                }
                PrintGuess(guess, status.Indent);
            }

            if (status.NextGuess != null)
            {
                PrintGuess(status.NextGuess, status.Indent);
                string prompt = status.NextGuess.WordIndex == 7 ?
                    "Guess the anagram matching the clue in the columns:" :
                    "Your next guess:";
                Console.WriteLine(prompt);
            }
            else if (status.State == GuessState.Solved)
            {
                Console.WriteLine("You've solved today's puzzle!");
                break;
            }
            else
            {
                Console.WriteLine("NextGuess is unexpectedly empty. Exiting.");
                break;
            }

            string? userGuess = Console.ReadLine();
            if (string.IsNullOrEmpty(userGuess)) { continue; }

            await SendGuess(userGuess);
        }
    }

    static async Task<Status?> GetStatus()
    {
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync("/game/status");
        } catch (Exception e) {
            Console.WriteLine($"API error: {e.Message}");
            return null;
        }
        if (response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Status>(content);
        }
        return null;
    }

    static async Task SendGuess(string guess)
    {
        var payload = new { Guess = guess };

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("/game/guess", payload);
        } catch (Exception e) {
            Console.WriteLine($"{e.Message}");
            return;
        }
        if (!response.IsSuccessStatusCode)
        {
            string errorMessage = await response.Content.ReadAsStringAsync();
            Console.WriteLine(errorMessage);
        }
    }

    static void PrintGuess(Guess guess, int maxIndent)
    {
        int indent = 3 * (maxIndent - guess.Offset);
        string line = new string(' ', indent);

        foreach (var clue in guess.Characters)
        {
            switch (clue.Type)
            {
                case ClueType.Incorrect:
                case ClueType.Empty:
                    line += $"[{clue.Letter}]";
                    break;
                case ClueType.CorrectLetter:
                    line += $"_{clue.Letter}_";
                    break;
                case ClueType.AllCorrect:
                    line += $"*{clue.Letter}*";
                    break;
            }
        }

        Console.WriteLine(line);
    }
}
