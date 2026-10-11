# Third-Party Notices

The published `SoundModes.exe` bundles the following components. Each is used under its own license.

| Component | Used for | License |
|---|---|---|
| [.NET 8 runtime and WPF](https://github.com/dotnet/runtime) © .NET Foundation and Contributors | Runs the app (bundled in the self-contained exe) | [MIT](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) |
| [NAudio.Wasapi 2.2.1](https://github.com/naudio/NAudio) © Mark Heath and contributors | Reading and setting Windows volume | [MIT](https://github.com/naudio/NAudio/blob/master/license.txt) |

Not bundled: **[Equalizer APO](https://sourceforge.net/projects/equalizerapo/)** does the sound processing.
It is a separate program that users download and install themselves, under its own license (GPL v2).
SoundModes only writes its configuration file and does not include any of its code.

Development-only (used for tests, not shipped): xUnit (Apache 2.0), Microsoft.NET.Test.Sdk (MIT).
