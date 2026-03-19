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

        // Error is returned as an anonymous object; ToString() produces "{ Error = ... }"
        Assert.NotNull(result);
        Assert.Contains("Error", result.ToString()!);
    }

    [Fact]
    public async Task ExecuteAsync_WhenChatSucceeds_ReturnsConspiracyTheory()
    {
        var profileId = Guid.NewGuid();
        var fakeProfile = new AIProfile
        {
            Alias = "test",
            Name = "Test Profile",
            ConnectionId = Guid.NewGuid(),
        };
        // Set Id via reflection since it has internal setter
        typeof(AIProfile).GetProperty("Id")!.SetValue(fakeProfile, profileId);

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

        Assert.NotNull(result);
        Assert.IsType<string>(result);
        Assert.Equal("The cheese is watching you.", (string)result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenResponseTextIsNullOrEmpty_ReturnsError()
    {
        var profileId = Guid.NewGuid();
        var fakeProfile = new AIProfile
        {
            Alias = "test",
            Name = "Test Profile",
            ConnectionId = Guid.NewGuid(),
        };
        typeof(AIProfile).GetProperty("Id")!.SetValue(fakeProfile, profileId);

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

        Assert.NotNull(result);
        Assert.Contains("Error", result.ToString()!);
    }
}
