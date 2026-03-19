using System.ComponentModel;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Tools;

namespace AI.Woowoo.Tools;

public record ConspiracyTheoryError(string Error);

public record ConspiracyTheoryArgs(
    [property: Description("The text content to generate a conspiracy theory about")] string Content);

[AITool("conspiracy_theory", "Conspiracy Theory Generator", Tags = new[] { "woowoo" })]
public class ConspiracyTheoryTool : AIToolBase<ConspiracyTheoryArgs>
{
    private const string SystemPrompt =
        "You are a conspiracy theory generator. Given any piece of text, you fabricate an absurd, " +
        "clearly fictional, and humorous conspiracy theory about it. Your theories should be creative " +
        "and entertaining -- not plausible, not harmful, just delightfully unhinged. Keep it to a short paragraph.";

    private readonly IAIChatService _chatService;
    private readonly IAIProfileService _profileService;

    public ConspiracyTheoryTool(IAIChatService chatService, IAIProfileService profileService)
    {
        _chatService = chatService;
        _profileService = profileService;
    }

    public override string Description =>
        "Generates a fictional, humorous conspiracy theory from a given piece of text. " +
        "Use this when asked to conspire about content, find hidden meanings, or generate a conspiracy theory.";

    protected override async Task<object> ExecuteAsync(
        ConspiracyTheoryArgs args,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var profile = await _profileService.GetDefaultProfileAsync(AICapability.Chat, cancellationToken);

            IEnumerable<ChatMessage> messages =
            [
                new ChatMessage(ChatRole.System, SystemPrompt),
                new ChatMessage(ChatRole.User, args.Content),
            ];

            var response = await _chatService.GetChatResponseAsync(
                profile.Id,
                messages,
                null,
                cancellationToken);

            var text = response?.Text;

            if (string.IsNullOrWhiteSpace(text))
                return new ConspiracyTheoryError("Could not generate conspiracy theory.");

            return text;
        }
        catch (InvalidOperationException)
        {
            return new ConspiracyTheoryError("No default chat profile is configured in Umbraco.AI.");
        }
    }

    // Exposes protected ExecuteAsync for unit tests
    public Task<object> TestExecuteAsync(ConspiracyTheoryArgs args, CancellationToken cancellationToken)
        => ExecuteAsync(args, cancellationToken);
}
