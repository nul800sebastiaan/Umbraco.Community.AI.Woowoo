This guide walks through setting up a new Umbraco package project called `Umbraco.Community.AI.Woowoo` that extends [Umbraco.AI](https://umbraco.com/marketplace/category/ai/). Rather than starting from scratch with a blank solution, the goal is a proper package structure from day one: the right project layout, a frontend build pipeline, uSync, GitHub Actions for NuGet publishing, the works.

That's where [Lotte's Opinionated Package Starter Template](https://github.com/LottePitcher/opinionated-package-starter) comes in. Lotte has done the hard work of figuring out what a well-structured Umbraco package solution should look like, and turned it into a `dotnet new` template. Pair that with Matt Brailsford's [Umbraco.AI Kitchen Sink install guide](https://mattbrailsford.dev/umbraco-ai-kitchen-sink-install), and you have everything you need to go from zero to a fully wired-up Umbraco.AI development environment.

This post walks through the full setup, including the Linux-specific gotcha to watch out for.

---

## Prerequisites

Before starting, make sure you have:

- **.NET 10 SDK** -- Umbraco 17 targets .NET 10
- **Node.js and npm** -- the package project uses Vite and TypeScript for its frontend
- **A GitHub account** -- the template wires up a GitHub remote and includes GitHub Actions for NuGet publishing

---

## Step 1: Install the template

Install the package starter template from NuGet:

```bash
dotnet new install Umbraco.Community.Templates.PackageStarter
```

You should see output ending with something like:

```plaintext
Success: Umbraco.Community.Templates.PackageStarter::X.X.X installed the following templates:
  umbracopackagestarter
```

**What does this template give you?**

When you scaffold from it, you get a complete two-project solution:

- `src/AI.Woowoo/` -- the package project, with Vite + TypeScript frontend (via the `umbraco-extension` template) and a NuGet-configured `.csproj`
- `src/AI.Woowoo.TestSite/` -- an Umbraco 17 test site with uSync pre-configured
- `src/AI.Woowoo.sln` -- the solution file, with the TestSite already referencing the package project
- `.github/workflows/release.yml` -- a GitHub Action that publishes to NuGet when you push a version tag
- `umbraco-marketplace.json`, README stubs, issue templates, and an MIT `LICENSE`

That's a solid foundation for package development, covering all the scaffolding that would otherwise take hours to set up from scratch.

---

## Step 2: Scaffold the solution

```bash
dotnet new umbracopackagestarter -n AI.Woowoo -an "Your Name" -gu YourGitHubUsername -gr Umbraco.Community.AI.Woowoo --allow-scripts yes
```

What each parameter does:

| Parameter | What it does |
|-----------|-------------|
| `-n AI.Woowoo` | The base name for the solution. The template prepends `Umbraco.Community.`, so the NuGet package ID becomes `Umbraco.Community.AI.Woowoo` |
| `-an "Your Name"` | Author name, used in the `.csproj` and marketplace metadata |
| `-gu YourGitHubUsername` | Your GitHub username, used to construct the repo URL |
| `-gr Umbraco.Community.AI.Woowoo` | The GitHub repository name |
| `--allow-scripts yes` | Bypasses the security prompt for the post-creation script (`setup.cmd`), which runs `npm install` as part of the `umbraco-extension` template setup |

This will take a few minutes -- `npm install` runs as part of the setup. Wait for the "All done!" message.

---

## Step 3: The Linux/macOS gotcha -- `setup.cmd` won't run

> **If you're on Windows, you can skip this section.** The template's `setup.cmd` script will have run fine and your solution is ready. Jump straight to [Step 4](#step-4-build-the-frontend--verify-the-base-site).

On Linux and macOS, the `setup.cmd` post-creation script fails with "Permission denied" because it's a Windows batch file. The template *files* are all created correctly -- the script just doesn't run, so the git repository, the frontend scaffolding, the solution wiring, and a few other things need to be done manually.

Here's everything the script would have done, translated into commands you can run:

```bash
# Initialise the git repository
cd AI.Woowoo
git init
git branch -M main
git remote add origin https://github.com/YourGitHubUsername/Umbraco.Community.AI.Woowoo.git

# Make sure you have the latest Umbraco dotnet templates
dotnet new install Umbraco.Templates --force

# Scaffold the package project inside src/
cd src
dotnet new umbraco-extension -n "AI.Woowoo" --site-domain "https://localhost:44361" --include-example

# The template creates two .csproj files -- swap them so the NuGet-configured one is active
cd AI.Woowoo
rm AI.Woowoo.csproj
mv AI.Woowoo_nuget.csproj AI.Woowoo.csproj

# Wire everything up in the solution
cd ..
dotnet sln add "AI.Woowoo"
dotnet add "AI.Woowoo.TestSite/AI.Woowoo.TestSite.csproj" reference "AI.Woowoo/AI.Woowoo.csproj"
```

> **Tip:** The repo root also contains a `setup.mjs` file -- a Node.js replacement for `setup.cmd` that works cross-platform. If it's present, running `node setup.mjs` handles all the steps above.

---

## Step 4: Build the frontend & verify the base site

Before layering anything on top, confirm the scaffolded solution actually works. Build the frontend assets first:

```bash
cd src/AI.Woowoo/Client
npm install
npm run build
```

You should see Vite output with files written to `dist/`. Now start the TestSite:

```bash
cd ../../AI.Woowoo.TestSite
dotnet run
```

The `launchSettings.json` included by the template automatically sets `ASPNETCORE_ENVIRONMENT=Development`, so you don't need to set it yourself. On first run, Umbraco installs itself unattended -- watch the terminal output for the URL (typically `https://localhost:44331` or similar).

Log in with the pre-configured credentials from `appsettings.json`:

- **Email:** `admin@example.com`
- **Password:** `1234567890`

Check two things:

1. Navigate to the **Content** section -- an "Example Dashboard" should be there (this is the example added by the `--include-example` flag in the `umbraco-extension` template)
2. Navigate to `/umbraco/swagger` -- the **Umbraco Management API** document should appear in the dropdown

Once everything looks good, hit `Ctrl+C` to stop the site. You now have a clean, working base to build on.

---

## Step 5: Add the Umbraco.AI packages

All the Umbraco.AI packages go into the TestSite project, not the package project. The package project stays clean -- it will only reference `Umbraco.AI` (or specific Umbraco.AI interfaces) when actually needed for package code. The TestSite is the development host.

Run these from the repo root (`AI.Woowoo/`):

```bash
# The Clean starter kit adds a demo content structure to the site
dotnet add "src/AI.Woowoo.TestSite" package Clean

# The core Umbraco.AI integration layer
dotnet add "src/AI.Woowoo.TestSite" package Umbraco.AI

# Addons
dotnet add "src/AI.Woowoo.TestSite" package Umbraco.AI.Prompt
dotnet add "src/AI.Woowoo.TestSite" package Umbraco.AI.Agent
dotnet add "src/AI.Woowoo.TestSite" package Umbraco.AI.Agent.Copilot --prerelease

# AI providers
dotnet add "src/AI.Woowoo.TestSite" package Umbraco.AI.Amazon
dotnet add "src/AI.Woowoo.TestSite" package Umbraco.AI.Anthropic
dotnet add "src/AI.Woowoo.TestSite" package Umbraco.AI.Google
dotnet add "src/AI.Woowoo.TestSite" package Umbraco.AI.MicrosoftFoundry
dotnet add "src/AI.Woowoo.TestSite" package Umbraco.AI.OpenAI
```

A couple of notes:

- `Umbraco.AI.Agent.Copilot` requires `--prerelease` because it's still in alpha. The other packages are stable.
- All five provider packages (Amazon Bedrock, Anthropic, Google, Microsoft Foundry, OpenAI) are installed so the environment is ready for any AI provider you or contributors want to test against. The seed data in the next step only activates the OpenAI connection, but the others are configured and waiting.

Check [Matt's install guide](https://mattbrailsford.dev/umbraco-ai-kitchen-sink-install) if you're following along later -- version numbers may have moved on. The [Umbraco.AI documentation](https://github.com/umbraco/Umbraco.AI/tree/main/docs/public) covers all the extensibility points in detail if you want to go further.

---

## Step 6: Add the seed demo data

Matt has a helpful GitHub Gist that seeds a full Umbraco.AI demo configuration: a connection, profile, context, prompts, and agents. It's a self-registering C# file -- drop it in the project and it registers itself via Umbraco's component system and seeds the data on first startup. It's also idempotent, so safe to leave in place.

```bash
curl -o src/AI.Woowoo.TestSite/UmbracoAISeedData.cs \
  "https://gist.githubusercontent.com/mattbrailsford/199d0b45e926ffb122fa96467039bd90/raw/UmbracoAISeedData.cs?v=2"
```

### API key (optional)

The seed data installs an OpenAI connection with a dummy key. The site runs fine without a real key -- all the AI configuration gets seeded into the backoffice, the connection just won't be functional. Replace the key later through the Umbraco.AI section in the backoffice once everything is up and running.

Don't have an OpenAI key? Google hands out free Gemini API keys via [Google AI Studio](https://aistudio.google.com/app/apikey) -- no credit card required. Hit "Create API key" in the top right, or "Get API key" in the left sidebar. The free tier is generous enough for development. If you go this route, use the `Umbraco.AI.Google` provider and configure the connection in the backoffice accordingly.

---

## Step 7: Run and verify

```bash
cd src/AI.Woowoo.TestSite
dotnet run
```

Once it's running, log in (`admin@example.com` / `1234567890`) and navigate to the **Umbraco.AI** section in the backoffice. You should see:

- A seeded OpenAI connection
- A profile
- A context
- Several prompts
- Several agents

If you added a real API key, try running a prompt or agent to confirm the connection is live.

---

## Step 8: Make the initial commit

Before staging everything, do a quick sanity check: open `UmbracoAISeedData.cs` and confirm there's no real API key in there.

```bash
git add .
git commit -m "chore: initial scaffold from Lotte's Opinionated Package Starter Template"
git push -u origin main
```

> **Prerequisite:** before pushing, go to GitHub and create a new empty repository named `Umbraco.Community.AI.Woowoo` (skip the readme, license, and gitignore options -- the template already created those locally). The template already configured the remote, so `git push` goes straight to the right place.

---

## What's next

The environment is now ready:

- `src/AI.Woowoo/` -- the package project, clean and ready for package code
- `src/AI.Woowoo.TestSite/` -- a full Umbraco 17 + Umbraco.AI host to develop and test against, with demo data seeded
- GitHub Actions configured for NuGet publishing on version tags
- All five AI providers installed and ready to configure

When it comes to implementing the package itself, the first thing to add to `src/AI.Woowoo/AI.Woowoo.csproj` is a reference to `Umbraco.AI.Core` -- the package that contains all the extension points (custom tools, middleware, guardrail evaluators, etc.):

```xml
<PackageReference Include="Umbraco.AI.Core" Version="1.6.0" />
```

Note that `Umbraco.AI.Core` 1.6.0 requires Umbraco.Cms 17.1.0 or higher. The template scaffolds the package project against 17.0.0, so bump the four Umbraco.Cms references in the same file to 17.1.0 when you add this.

The next step is implementing the `Umbraco.Community.AI.Woowoo` package itself -- but that's a story for another post.
