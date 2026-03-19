using Microsoft.Extensions.AI;
using Moq;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using AI.Woowoo.Tools;

namespace AI.Woowoo.Tests.Tools;

public class ConspiracyTheoryToolTests
{
    private readonly Mock<IAIChatService> _chatService = new();
    private readonly Mock<IAIProfileService> _profileService = new();

    // Fix 5: Shared helper to avoid duplicating profile construction across tests.
    private static (Guid profileId, AIProfile fakeProfile) CreateFakeProfile()
    {
        var profileId = Guid.NewGuid();
        var fakeProfile = new AIProfile
        {
            Alias = "test",
            Name = "Test Profile",
            ConnectionId = Guid.NewGuid(),
        };
        // Fix 1: AIProfile.Id has an internal setter (defined in Umbraco.AI.Core, a separate
        // assembly), so it cannot be assigned directly from test code. Reflection is the only
        // option short of a dedicated test factory inside that library.
        typeof(AIProfile).GetProperty("Id")!.SetValue(fakeProfile, profileId);
        return (profileId, fakeProfile);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoDefaultProfile_ReturnsError()
    {
        _profileService
            .Setup(x => x.GetDefaultProfileAsync(AICapability.Chat, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No default profile"));

        var tool = new ConspiracyTheoryTool(_chatService.Object, _profileService.Object);

        var result = await tool.TestExecuteAsync(
            new ConspiracyTheoryArgs("Some content about cheese"),
            CancellationToken.None);

        // Fix 4: Assert on the exact error message rather than a loose string contains.
        Assert.NotNull(result);
        dynamic error = result;
        Assert.Equal("No default chat profile is configured in Umbraco.AI.", (string)error.Error);

        // Fix 3: Chat service must never be called when there is no profile.
        _chatService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenChatSucceeds_ReturnsConspiracyTheory()
    {
        var (profileId, fakeProfile) = CreateFakeProfile();

        var fakeResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, "The cheese is watching you."));

        _profileService
            .Setup(x => x.GetDefaultProfileAsync(AICapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeProfile);

        _chatService
            .Setup(x => x.GetChatResponseAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeResponse);

        var tool = new ConspiracyTheoryTool(_chatService.Object, _profileService.Object);

        var result = await tool.TestExecuteAsync(
            new ConspiracyTheoryArgs("Some content about cheese"),
            CancellationToken.None);

        // Fix 2: Verify that the profile ID resolved from the profile service was forwarded to
        // the chat service call.
        _chatService.Verify(x => x.GetChatResponseAsync(
            profileId,
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(result);
        Assert.IsType<string>(result);
        Assert.Equal("The cheese is watching you.", (string)result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenResponseTextIsNullOrEmpty_ReturnsError()
    {
        var (_, fakeProfile) = CreateFakeProfile();

        var fakeResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, (string?)null));

        _profileService
            .Setup(x => x.GetDefaultProfileAsync(AICapability.Chat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeProfile);

        _chatService
            .Setup(x => x.GetChatResponseAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeResponse);

        var tool = new ConspiracyTheoryTool(_chatService.Object, _profileService.Object);

        var result = await tool.TestExecuteAsync(
            new ConspiracyTheoryArgs("Some content about cheese"),
            CancellationToken.None);

        // Fix 4: Assert on the exact error message rather than a loose string contains.
        Assert.NotNull(result);
        dynamic error = result;
        Assert.Equal("Could not generate conspiracy theory.", (string)error.Error);
    }
}
