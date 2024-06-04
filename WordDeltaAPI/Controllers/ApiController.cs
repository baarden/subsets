using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using WordDeltaAPI.Models.ValueObjects;

namespace WordDeltaAPI.Controllers;

[ApiController]
[Route("api")]
public class ApiController : ControllerBase
{
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
        (status, _) = _gameService.GetStatus((int)userId);
        return Ok(status);
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
        if (status.NextGuess == null || refWord == null || guess.Length != refWord.Length)
        {
            return BadRequest("Invalid guess length");
        }

        if (status.Guesses.Any(g => g.GuessWord == guess))
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
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("SessionId")))
        {
            var sessionId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("SessionId", sessionId);
        }
        string? currentSessionId = HttpContext.Session.GetString("SessionId");
        if (string.IsNullOrEmpty(currentSessionId)) { return null; }

        int? userId = _gameService.GetUser(currentSessionId);
        return userId;
    }    
}
