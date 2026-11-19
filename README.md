# Report Helper

[![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/Valiice/ReportHelper/total?style=flat-square&color=blue)](https://github.com/Valiice/ReportHelper/releases)
[![GitHub Release](https://img.shields.io/github/v/release/Valiice/ReportHelper?include_prereleases&style=flat-square&color=green)](https://github.com/Valiice/ReportHelper/releases/latest)
[![GitHub License](https://img.shields.io/github/license/Valiice/ReportHelper?style=flat-square&color=orange)](LICENSE.md)
[![GitHub Issues](https://img.shields.io/github/issues/Valiice/ReportHelper?style=flat-square)](https://github.com/Valiice/ReportHelper/issues)
[![Download Latest Zip](https://img.shields.io/badge/Download-Latest%20Zip-blue?style=flat-square)](https://github.com/Valiice/ReportHelper/releases/latest/download/ReportHelper.zip)

Write harassment reports without the hassle.

The game's report form is awkward to type in, and by the time you open it the chat you want to quote has scrolled away. Report Helper keeps the last 24 hours of who you played with and what they said, and gives you a normal text box to write the report in. You then copy the result and paste it into the game's form.

## Installation

1. Add my custom plugin repository: [https://raw.githubusercontent.com/Valiice/DalamudPluginRepo/master/repo.json](https://github.com/Valiice/DalamudPluginRepo)
2. Search for **Report Helper** in the Plugin Installer.
3. Install and Enable.

Not on the official repo. Alternatively, build with `dotnet build ReportHelper/ReportHelper.csproj -c Release` and load `ReportHelper/bin/Release/ReportHelper` as a dev plugin.

## Usage

1. Open a report form in game (Support Desk, Contact Us, Report Harassment). Report Helper opens next to it. `/reporthelper` opens it at any time.
2. Pick a player on the left. Players are grouped by the duty or zone you met them in and coloured by role. Ctrl+click adds more players from the same group to one report.
3. Tick the chat lines you want to quote. Each line carries its Server Time and channel.
4. Write what happened in the text box.
5. Press **Copy title** and paste it into the form's title with Ctrl+V, then do the same with **Copy report** for the body.
6. Check the form and submit it yourself.

Where and when are filled in from the place and the lines you ticked. Type over them if they are wrong.

## What is recorded

- Players in your party or alliance, with the duty or zone and their role.
- Chat from other players in party, alliance, tell, say, shout, yell and emotes. The **Settings** tab turns each channel on or off.

A player who later sends you a tell, or talks near you, has those lines listed with their duty chat, as long as the name and home world match.

## Templates

The default template follows the six prompts of the game's own harassment form. The **Templates** tab lets you edit it or add your own. A template is plain text with these placeholders:

`{players}` `{where}` `{when}` `{description}` `{quotes}` `{channels}`

The default template, in case you changed it and want it back:

Title:

```
Harassment by {players} in {where}
```

Body:

```
■ Character Name(s) and Home World (if known):
{players}

■ World and area/duty where the incident occurred:
{where}

■ Approximate date and time of the incident:
{when}

■ Detailed description of the incident:
{description}

■ Comments made by the player(s):
{quotes}

■ Chat mode used:
{channels}
```

## Limits of the game's form

- The title holds 200 characters.
- The body holds 255 lines. The game counts lines, not characters.

Report Helper shows both counters and disables the copy button when the text will not fit.

## Privacy

- Report Helper never writes into the game's form and never submits anything. You paste and send the report yourself.
- History is kept in a file in the plugin's config folder so it survives a restart. Entries are deleted after 24 hours, and **Clear history** (hold Ctrl) deletes everything at once.
- Nothing leaves your machine unless you copy it yourself.

Report Helper shows what was said. Whether it breaks the rules is for you and the GMs to decide.

## Limitations

- Your own chat lines are not recorded, only those of other players.
- A player who says nothing and is not in your party or alliance is not listed.
- The window opens for every Support Desk form, including bug reports and suggestions.

## License & Copyright

Copyright © 2026 Valiice

Report Helper is licensed under the GNU Affero General Public License v3.0; see [LICENSE](LICENSE.md) for details. You are free to use, modify, and redistribute the source code, provided derivative works keep the same license, credit this project with a link back to [https://github.com/Valiice/ReportHelper](https://github.com/Valiice/ReportHelper), and retain this copyright notice.
