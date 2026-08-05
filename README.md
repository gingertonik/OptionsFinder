# OptionsFinder

`https://raw.githubusercontent.com/gingertonik/OptionsFinder/master/repo.json`

A Dalamud plugin for FINAL FANTASY XIV that adds a searchable window over the
options in the game's native **Character Configuration** and **System
Configuration** menus. Type part of an option's name, see its current value,
and edit it inline.

Values are read and written through Dalamud's `IGameConfig` service so editing
here has the same effects and limits as changing the setting in the game
menu. Numeric options are clamped to the min/max the game itself reports for
that option; string options don't have a bound exposed through this API, and
are flagged as such in the UI.

Only options that are actually present in the native menus are shown.

## Usage

`/ofinder` toggles the search window in-game.
`/pfind` also toggles the search window in-game.
 
