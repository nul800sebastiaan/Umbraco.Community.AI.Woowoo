@ECHO OFF
:: This file can now be deleted!
:: It was used when setting up the package solution (using https://github.com/LottePitcher/opinionated-package-starter)

:: set up git
git init
git branch -M main
git remote add origin https://github.com/nul800sebastiaan/Umbraco.Community.AI.Woowoo.git

:: ensure latest Umbraco templates used
dotnet new install Umbraco.Templates --force

:: use the umbraco-extension dotnet template to add the package project
cd src
dotnet new umbraco-extension -n "AI.Woowoo" --site-domain "https://localhost:44361" --include-example

:: replace package .csproj with the one from the template so has the extra information needed for publishing to nuget
cd AI.Woowoo
del AI.Woowoo.csproj
ren AI.Woowoo_nuget.csproj AI.Woowoo.csproj

:: add project to solution
cd..
dotnet sln add "AI.Woowoo"

:: add reference to project from test site
dotnet add "AI.Woowoo.TestSite/AI.Woowoo.TestSite.csproj" reference "AI.Woowoo/AI.Woowoo.csproj"
