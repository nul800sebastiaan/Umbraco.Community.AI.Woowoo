# Design: ConspiracyTheoryTool

**Date:** 2026-03-19
**Package:** Umbraco.Community.AI.Woowoo
**Feature:** Custom Umbraco.AI Tool -- Conspiracy Theory Generator

---

## Overview

A custom Umbraco.AI Tool that takes a piece of text and returns a fabricated, humorous conspiracy theory about it. The tool plugs into the Umbraco.AI agent system: users attach it to any agent in the backoffice and can then prompt that agent to generate a conspiracy theory from content.

---

## Architecture

### Extension point

Umbraco.AI exposes a Custom Tools API via `AIToolBase<TArgs>` (in `Umbraco.AI.Core`). Tools are auto-discovered at startup via the `[AITool]` attribute -- no Composer registration required.

### `[AITool]` attribute signature (confirmed from source)

```csharp
[AITool(
    id: string,           // unique identifier
    name: string,         // display name
    ScopeId: string,      // defaults to "general"
    IsDestructive: bool,  // defaults to false
    Tags: string[]        // optional -- no Category parameter exists
)]
```

### Components

**`ConspiracyTheoryArgs`** (record, same file as tool)
- Single property: `string Content` -- the text to generate a conspiracy theory from
- `[property: Description("...")]` from `System.ComponentModel`

**`ConspiracyTheoryTool`**
- `[AITool("conspiracy_theory", "Conspiracy Theory Generator", Tags = new[] { "woowoo" })]`
- Extends `AIToolBase<ConspiracyTheoryArgs>`
- `ExecuteAsync` returns `Task<object>` (base class signature) -- anonymous error objects are valid returns
- Constructor injects `IAIChatService` and `IAIProfileService`

### `ExecuteAsync` logic

1. Call `IAIProfileService.GetDefaultProfileAsync(AICapability.Chat)` to get the default chat profile. Wrap in try/catch -- `GetDefaultProfileAsync` throws `InvalidOperationException` if no default profile is configured.
2. Build the message list:
   - `ChatMessage(ChatRole.System, <system prompt>)` -- instructs the AI to produce a fictional conspiracy theory
   - `ChatMessage(ChatRole.User, args.Content)` -- the text to conspire about
3. Call `IAIChatService.GetChatResponseAsync(b => b.WithAlias("conspiracy-theory").WithProfile(profile.Id), messages, cancellationToken)`
4. Return `response.Text` as the result (`ChatResponse.Text` is a convenience property from `Microsoft.Extensions.AI`)

### System prompt (draft)

> You are a conspiracy theory generator. Given any piece of text, you fabricate an absurd, clearly fictional, and humorous conspiracy theory about it. Your theories should be creative and entertaining -- not plausible, not harmful, just delightfully unhinged. Keep it to a short paragraph.

### Error handling

- No default chat profile configured: return `new { Error = "No default chat profile is configured in Umbraco.AI." }`
- AI call fails or returns empty: return `new { Error = "Could not generate conspiracy theory." }`

### File layout

```
src/AI.Woowoo/
└── Tools/
    └── ConspiracyTheoryTool.cs   (args record + tool class)
```

### csproj change

Add to `src/AI.Woowoo/AI.Woowoo.csproj`:

```xml
<PackageReference Include="Umbraco.AI.Core" Version="1.6.0" />
```

Version pinned to `1.6.0` to match the `Umbraco.AI` version installed in the TestSite. This gives access to `AIToolBase`, `[AITool]`, `IAIChatService`, `IAIProfileService`, and `AICapability`.

---

## Data flow

1. User configures an Umbraco.AI agent in the backoffice and adds the "Conspiracy Theory Generator" tool to it
2. User asks the agent: "Generate a conspiracy theory about this page" (or similar)
3. Agent invokes the tool, passing the relevant content text as `Content`
4. Tool calls `IAIProfileService.GetDefaultProfileAsync(AICapability.Chat)` to resolve the profile
5. Tool calls `IAIChatService.GetChatResponseAsync(...)` with a system prompt and the content as the user message
6. Tool returns the AI-generated conspiracy theory string (or an error object)
7. Agent incorporates the result into its response

---

## What is NOT in scope

- No frontend / backoffice UI -- the tool surfaces through the existing Umbraco.AI agent panel
- No persistence -- results are ephemeral
- No configuration UI for the tool
- No additional tools (nutritional facts, woowoo-ify) -- future work
