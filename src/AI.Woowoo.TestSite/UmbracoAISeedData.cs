// UmbracoAISeedData.cs
//
// Drop this file into an Umbraco site project that has Umbraco.AI packages installed.
// It seeds demo data (connection, profile, context, prompts, agents) on first startup.
//
// Prerequisites:
//   - Umbraco.AI, Umbraco.AI.OpenAI, Umbraco.AI.Prompt, Umbraco.AI.Agent packages installed
//   - OpenAI API key configured in connection settings
//
// The seeder is idempotent — it checks for existing data by alias and skips if already seeded.

using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.OpenAI;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Demo;

public class UmbracoAISeedDataComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, UmbracoAISeedDataHandler>();
}

public class UmbracoAISeedDataHandler(
    IAIConnectionService connectionService,
    IAIProfileService profileService,
    IAIContextService contextService,
    IAIPromptService promptService,
    IAIAgentService agentService,
    IAISettingsService settingsService,
    AIToolScopeCollection toolScopes,
    ILogger<UmbracoAISeedDataHandler> logger)
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken ct)
    {
        // Skip if already seeded (check for our connection alias)
        if (await connectionService.GetConnectionByAliasAsync("openai-demo", ct) is not null)
        {
            logger.LogInformation("Demo data already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding Umbraco.AI demo data...");

        // Resolve all available tool scope IDs for full agent permissions
        var allToolScopeIds = toolScopes.Select(s => s.Id).ToList();

        // 1. Context
        var context = await contextService.SaveContextAsync(new AIContext
        {
            Alias = "brand-voice",
            Name = "Brand Voice",
            Resources =
            [
                new AIContextResource
                {
                    ResourceTypeId = "brand-voice",
                    Name = "Brand Guidelines",
                    Description = "Core brand voice and tone guidelines",
                    SortOrder = 0,
                    Data = new
                    {
                        ToneDescription = "We are friendly, professional, and approachable. Use clear, simple language. Avoid jargon. Speak directly to the reader using 'you'. Keep sentences short and paragraphs focused.",
                        TargetAudience = "Web developers and content editors using Umbraco CMS. Our audience ranges from technical developers building sites to non-technical content editors managing day-to-day content. Write so both groups can understand.",
                        StyleGuidelines = "Use active voice. Lead with the benefit or outcome, not the feature. Use sentence case for headings. Prefer short paragraphs (2-3 sentences). Use bullet points for lists of three or more items. Write at a secondary school reading level.",
                        AvoidPatterns = "Marketing buzzwords (leverage, synergy, cutting-edge). Exclamation marks. Overly casual language (gonna, wanna). Passive voice where active is possible. Filler phrases (in order to, it is important to note that). Starting sentences with 'So' or 'Basically'."
                    },
                    InjectionMode = AIContextResourceInjectionMode.Always
                }
            ]
        }, ct);

        // 2. Connection (API key resolved from IConfiguration via $-prefix)
        var connection = await connectionService.SaveConnectionAsync(new AIConnection
        {
            Alias = "openai-demo",
            Name = "OpenAI",
            ProviderId = "openai",
            Settings = new OpenAIProviderSettings { ApiKey = "YOUR_OPENAI_API_KEY" },
            IsActive = true
        }, ct);

        // 3. Profile
        var profile = await profileService.SaveProfileAsync(new AIProfile
        {
            Alias = "default-chat",
            Name = "Default Chat",
            Capability = AICapability.Chat,
            ConnectionId = connection.Id,
            Model = new AIModelRef("openai", "gpt-4o"),
            Settings = new AIChatProfileSettings
            {
                Temperature = 0.7f,
                ContextIds = [context.Id]
            }
        }, ct);

        // 4. Set profile as default in settings
        var settings = await settingsService.GetSettingsAsync(ct);
        settings.DefaultChatProfileId = profile.Id;
        await settingsService.SaveSettingsAsync(settings, ct);

        // 5. Prompts
        await promptService.SavePromptAsync(new AIPrompt
        {
            Alias = "summarize",
            Name = "Summarize",
            Description = "Summarize the current content into a concise paragraph",
            Instructions = "Summarize the following content in a single, concise paragraph that captures the key points:\n\n{{currentValue}}\n\nReturn just the result.",
            ProfileId = profile.Id,
            IsActive = true,
            IncludeEntityContext = false,
            OptionCount = 3,
            Scope = new AIPromptScope
            {
                AllowRules = [new AIPromptScopeRule { PropertyEditorUiAliases = ["Umb.PropertyEditorUi.TextArea", "Umb.PropertyEditorUi.TextBox"] }]
            }
        }, ct);

        await promptService.SavePromptAsync(new AIPrompt
        {
            Alias = "seo-description",
            Name = "SEO Description",
            Description = "Generate an SEO-friendly meta description",
            Instructions = "Write an SEO-optimized meta description (150-160 characters) for this content. Include relevant keywords naturally. Return just the result.",
            ProfileId = profile.Id,
            IsActive = true,
            IncludeEntityContext = true,
            OptionCount = 1,
            Scope = new AIPromptScope
            {
                AllowRules = [new AIPromptScopeRule { PropertyEditorUiAliases = ["Umb.PropertyEditorUi.TextArea", "Umb.PropertyEditorUi.TextBox"] }]
            }
        }, ct);

        // 6. Agents
        // NOTE: In Umbraco.AI.Agent 1.5.0, ContextIds/Instructions/AllowedToolScopeIds live on
        // AIStandardAgentConfig (the Config property), not directly on AIAgent.
        // This was changed in a later version of the package.
        await agentService.SaveAgentAsync(new AIAgent
        {
            Alias = "content-assistant",
            Name = "Content Assistant",
            Description = "Helps create and edit content across the site",
            ProfileId = profile.Id,
            SurfaceIds = ["copilot"],
            IsActive = true,
            Config = new AIStandardAgentConfig
            {
                ContextIds = [context.Id],
                Instructions = "You are a helpful content assistant for an Umbraco CMS website. Help users create, edit, and improve their content. Be concise and practical.",
                AllowedToolScopeIds = allToolScopeIds
            },
            Scope = new AIAgentScope
            {
                AllowRules = [new AIAgentScopeRule { Sections = ["content"] }]
            }
        }, ct);

        await agentService.SaveAgentAsync(new AIAgent
        {
            Alias = "media-assistant",
            Name = "Media Assistant",
            Description = "Helps manage and describe media assets",
            ProfileId = profile.Id,
            SurfaceIds = ["copilot"],
            IsActive = true,
            Config = new AIStandardAgentConfig
            {
                ContextIds = [context.Id],
                Instructions = "You are a media assistant for an Umbraco CMS website. Help users write alt text, captions, and descriptions for their media assets. Focus on accessibility and SEO.",
                AllowedToolScopeIds = allToolScopeIds
            },
            Scope = new AIAgentScope
            {
                AllowRules = [new AIAgentScopeRule { Sections = ["media"] }]
            }
        }, ct);

        await agentService.SaveAgentAsync(new AIAgent
        {
            Alias = "legal-specialist",
            Name = "Legal Specialist",
            Description = "Helps draft and review legal content like terms and conditions, privacy policies, and disclaimers",
            ProfileId = profile.Id,
            SurfaceIds = ["copilot"],
            IsActive = true,
            Config = new AIStandardAgentConfig
            {
                ContextIds = [context.Id],
                Instructions = "You are a legal content specialist for an Umbraco CMS website. Help users draft and review legal content such as terms and conditions, privacy policies, cookie policies, and disclaimers. Write in clear, plain language that is accessible to non-lawyers while remaining legally sound. Always recommend professional legal review for final versions.",
                AllowedToolScopeIds = allToolScopeIds
            },
            Scope = new AIAgentScope
            {
                AllowRules = [new AIAgentScopeRule { Sections = ["content"] }]
            }
        }, ct);

        logger.LogInformation("Umbraco.AI demo data seeded successfully.");
    }
}
