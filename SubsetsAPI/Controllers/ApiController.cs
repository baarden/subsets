using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using SubsetsAPI.Models.ValueObjects;
using System.Text.RegularExpressions;

namespace SubsetsAPI.Controllers;

[ApiController]
[Route("api")]
public partial class ApiController : ControllerBase
{
    // RegularExpressions turns the class into a partial
    [GeneratedRegex(@"^ *[a-zA-Z]{3,7} *$")]
    private static partial Regex GuessRegex();

    private readonly GameService _gameService;
    private readonly ILogger<ApiController> _logger;

    public ApiController(GameService gameService, ILogger<ApiController> logger)
    {
        _gameService = gameService;
        _logger = logger;
    }

    [HttpGet("status")]
    public ActionResult<Status> GetStatus()
    {
        int? userId = GetUser();
        if (userId == null) { return Unauthorized("User not found."); }

        Status status;
        try {
            (status, _) = _gameService.GetStatus((int)userId);
            return Ok(status);
        } catch (Exception e) {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("guess")]
    public ActionResult PostGuess([FromBody] GuessPayload payload)
    {
        if (payload == null || string.IsNullOrWhiteSpace(payload.Guess))
        {
            return UnprocessableEntity("Empty guess submitted.");
        }
        string guess = payload.Guess.ToLower(CultureInfo.InvariantCulture);

        int? userId = GetUser();
        if (userId == null) { return Unauthorized("User not found."); }

        Status status;
        string? refWord;
        (status, refWord) = _gameService.GetStatus((int)userId);
        if (status == null) { return StatusCode(500, "Unable to retrieve status."); }
        if (status.NextGuess == null || refWord == null || guess.Length != refWord.Length || !GuessRegex().IsMatch(guess))
        {
            return BadRequest("Invalid guess length");
        }

        if (status.Guesses.Any(g => (
            g.GuessWord.Trim() == guess.Trim()
            && g.WordIndex == status.NextGuess.WordIndex
            )))
        {
            return BadRequest("Already guessed");
        }

        int guessNumber = status.Guesses.Count + 1;

        bool inserted = _gameService.AddGuess((int)userId, status.Today, guessNumber, status.NextGuess.WordIndex, guess, out string errorMessage);
        if (!inserted)
        {
            if (errorMessage != null) { return BadRequest(errorMessage); }
            return StatusCode(500, "Failed to insert guess.");
        }

        return Ok();
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
}
