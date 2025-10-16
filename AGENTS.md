# Repository Guidelines

## Project Structure & Module Organization
The WPF app is defined by LottoNumber.sln/LottoNumber.csproj. UI views live under App.xaml and MainWindow.xaml with their *.xaml.cs code-behind. Domain logic and reusable conditions reside in ConditionClass/ (for example BaseCondition.cs, Conditions.cs). Auto-generated assets are stored in Properties/ (resources, settings, assembly metadata). Build outputs land in in/<Configuration>/ and intermediates in obj/<Configuration>/; keep both out of source control.

## Build, Test, and Development Commands
- msbuild LottoNumber.sln /t:Build /p:Configuration=Debug: compile the solution.
- msbuild LottoNumber.sln /t:Clean: remove build outputs.
- .\bin\Debug\LottoNumber.exe: launch the app locally after a Debug build.
Run these from a Developer Command Prompt targeting .NET Framework 4.8.

## Coding Style & Naming Conventions
Use C# with 4-space indentation and Allman braces. Classes, methods, and properties follow PascalCase; locals and parameters use camelCase; private fields use _camelCase. Favor string interpolation, early returns, and explicit access modifiers. Match XAML event handlers to On<Event> naming. Store UI-facing strings in Properties/Resources.resx.

## Testing Guidelines
There is no test project yet; prefer MSTest v2 or NUnit targeting net48 when adding tests. Name tests ClassName_Method_Scenario_Expected and focus on logic inside ConditionClass to keep the UI thin. Run tests via Visual Studio Test Explorer or stest.console.exe.

## Commit & Pull Request Guidelines
Follow Conventional Commits such as eat(condition): add range filter; keep subjects imperative and under 72 characters. PRs should describe the change, link related work items, and include screenshots or GIFs for UI updates. Exclude in/ and obj/ artifacts from commits.

## Security & Configuration Tips
Avoid hardcoded secrets in App.config; prefer user-scoped settings. Validate all condition inputs and add defensive checks in new BaseCondition implementations to prevent invalid combinations.
