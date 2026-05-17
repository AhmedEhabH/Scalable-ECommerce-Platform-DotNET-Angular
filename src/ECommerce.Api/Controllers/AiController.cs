using ECommerce.Api.Models;
using ECommerce.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.Api.Controllers;

/// <summary>
/// AI Customer Assistant endpoints for shopping assistance
/// </summary>
[EnableRateLimiting("global")]
public class AiController : BaseApiController
{
    private readonly IAiService _aiService;

    public AiController(IAiService aiService)
    {
        _aiService = aiService;
    }

    /// <summary>
    /// Send a message to the AI shopping assistant
    /// </summary>
    /// <param name="request">Chat request containing user message and optional history</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI assistant response</returns>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ApiResponse<ChatResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return HandleBadRequest("Message is required");

        string response;

        if (request.History != null && request.History.Count > 0)
        {
            var messages = request.History
                .Select(h => new ChatMessage(h.Role, h.Content))
                .ToList();

            response = await _aiService.GetChatResponseWithHistoryAsync(messages, cancellationToken);
        }
        else
        {
            response = await _aiService.GetChatResponseAsync(request.Message, cancellationToken);
        }

        return HandleSuccess(new ChatResponse(response));
    }
}

public record ChatRequest(string Message, List<ChatHistoryItem>? History);

public record ChatHistoryItem(string Role, string Content);

public record ChatResponse(string Reply);
