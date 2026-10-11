# Contributing to SoundModes

Thanks for helping! Bug fixes, better presets, accessibility improvements and new features are all welcome.

## Found a bug or have an idea?

Open an **Issue** first and describe it, especially for anything bigger than a small fix, so we can agree on the
approach before you spend time on code. For bugs, include:

- What you did, what you expected, and what happened
- Windows version, and what you listen on (laptop speakers, headphones, AUX speakers)
- The relevant lines from `%APPDATA%\SoundModes\log.txt`, if any

## Making a change (pull requests)

1. **Fork** this repository and clone your fork.
2. Create a branch from `main`, named for what it does:
   `feature/short-name`, `fix/short-name`, `docs/short-name` or `chore/short-name`.
3. Set up and build: see [DEVELOPMENT.md](docs/DEVELOPMENT.md). You need Windows and the .NET 8 SDK.
4. Make your change, and keep it focused on one thing.
5. Check it:
   - `dotnet build` has no warnings
   - `dotnet test` passes, and new logic has a test (a bug fix comes with a test that would have caught the bug)
   - You ran the app and tried your change
6. Update the docs:
   - Add an entry at the top of [CHANGELOG.md](CHANGELOG.md): what changed, and why.
   - If users will notice the change, update [README.md](README.md) in plain, non-technical language.
7. Commit using `type: description`, for example `fix: popup opens off-screen on vertical monitors`
   or `feat: add mute hotkey`.
8. Push to your fork and open a **Pull Request** against `main`. Fill in the PR template.

All PRs are reviewed before merging. Small PRs get reviewed fastest.

## Code guidelines

- Match the style of the surrounding code; keep functions short and names clear.
- Every function gets a short comment saying what it does.
- User-facing text goes in `src/SoundModes/Strings.cs`, not inline in code or XAML.
- No new dependencies unless they're clearly worth it. Explain why in the PR.
- No network calls, telemetry or data collection. SoundModes is offline by design.
- Keep it light: check memory use stays around 25–30 MB in Task Manager.
- Keyboard and screen-reader accessibility matters: new controls need `AutomationProperties.Name`.

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE) of this project.
