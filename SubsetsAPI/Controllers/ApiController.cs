using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using SubsetsAPI.Models;
using System.Text.RegularExpressions;

namespace SubsetsAPI.Controllers;

[ApiController]
[Route("api")]
public partial class ApiController : ControllerBase
{
    // RegularExpressions turns the class into a partial
    [GeneratedRegex(@"^ *[a-zA-Z]{3,7} *$")]
    private static partial Regex PlusOneGuessRegex();

    [GeneratedRegex(@"^ *[a-zA-Z]{3,8} *$")]
    private static partial Regex PlusOneMoreGuessRegex();


    private readonly GameService _gameService;
    private readonly ILogger<ApiController> _logger;

    public ApiController(GameService gameService, ILogger<ApiController> logger)
    {
        _gameService = gameService;
        _logger = logger;
    }

    [HttpGet("status")]
    [HttpGet("more/status")]
    public ActionResult<Status> GetStatus()
    {
        bool isMore = HttpContext.Request.Path.StartsWithSegments("/api/more");

        int? userId = GetUser();
        if (userId == null) { return Unauthorized("User not found."); }

        DateOnly today = GetDate();

        Status status;
        try {
            (status, _) = _gameService.GetStatus((int)userId, today, isMore);
            return Ok(status);
        } catch (Exception e) {
            Console.WriteLine(e.ToString());
            return BadRequest(e.Message);
        }
    }

    [HttpGet("stats")]
    [HttpGet("more/stats")]
    public ActionResult<Statistics> GetStats()
    {
        bool isMore = HttpContext.Request.Path.StartsWithSegments("/api/more");

        int? userId = GetUser();
        if (userId == null) { return Unauthorized("User not found."); }

        DateOnly today = GetDate();

        Statistics stats;
        try {
            stats = _gameService.GetStatistics((int)userId, today, isMore);
            return Ok(stats);
        } catch (Exception e) {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("guess")]
    [HttpPost("more/guess")]
    public ActionResult<GuessResponse> PostGuess([FromBody] GuessRequest guessRequest)
    {
        bool isMore = HttpContext.Request.Path.StartsWithSegments("/api/more");

        if (guessRequest == null || string.IsNullOrWhiteSpace(guessRequest.Guess))
        {
            return UnprocessableEntity("Empty guess submitted.");
        }

        DateOnly today = GetDate();
        if (guessRequest.Date.CompareTo(today) != 0) {
            return Conflict("Date mismatch");
        }

        int? userId = GetUser();
        if (userId == null) { return Unauthorized("User not found."); }

        Status status;
        string? refWord;
        (status, refWord) = _gameService.GetStatus((int)userId, today, isMore);
        if (status == null) { return StatusCode(500, "Unable to retrieve status."); }

        string guess = guessRequest.Guess.ToLower(CultureInfo.InvariantCulture);
        string? validationError = ValidateGuess(guess, today, refWord, status, isMore);
        if (validationError != null) {
            return BadRequest(validationError);
        }

        int guessNumber = status.Guesses.Count + 1;
        (bool inserted, string? errorMessage) = _gameService.AddGuess(
            (int)userId,
            today,
            guessNumber,
            status.NextGuess!.WordIndex,
            guess,
            isMore);
        if (!inserted)
        {
            if (errorMessage != null) { return BadRequest(errorMessage); }
            return StatusCode(500, "Failed to insert guess.");
        }

        return Ok();
    }

    private static string? ValidateGuess(string guess, DateOnly today, string? refWord, Status status, bool isMore)
    {
        Regex regex = isMore ? PlusOneMoreGuessRegex() : PlusOneGuessRegex();

        if (status.NextGuess == null 
            || refWord == null 
            || guess.Length != refWord.Length
            || !regex.IsMatch(guess)
            )
        {
            return "Invalid guess length";
        }

        if (status.Guesses.Any(g => (
            g.GuessWord.Trim() == guess.Trim()
            && g.WordIndex == status.NextGuess.WordIndex
            )))
        {
            return "Already guessed";
        }
        return null;
    }

    private int? GetUser()
    {
        string? sessionId = HttpContext.Session.GetString("SessionId");
        if (string.IsNullOrEmpty(sessionId))
        {
            sessionId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("SessionId", sessionId);
        }

        int? userId = _gameService.GetUser(sessionId);
        return userId;
    }

    private static DateOnly GetDate()
    {
        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")
            );
        return DateOnly.FromDateTime(now);
    }
}
