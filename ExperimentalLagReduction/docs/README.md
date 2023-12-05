Contributions README
====================================

Most users should download the mod from the [Nexus page]<!--(https://www.nexusmods.com/stardewvalley/mods/13049)-->. That said, if you'd like to contribute:

### Precaching:

The scheduler will pre-cache paths from certain locations. This is primarily used for locations where a lot of NPCs path through, like town centers.

For larger expansion mods, it may help to add your town center to this. The asset to target is `Mods/atravita/Rescheduler_Populate`, and the format is a `string->string` map between internal format and the search radius.

For example, this patch would add `Town`:

```js
{
    "Action": "EditData",
    "Target": "Mods/atravita/Rescheduler_Populate",
    "Entries": {
        "Town": "3"
    }
}
```

Note that the highest radius allowed is 4, and values of 2 or 3 work best. 

### Translations:

This mod uses SMAPI's i18n feature for translations. I'd love to get translations! Please see the wiki's guide [here](https://stardewvalleywiki.com/Modding:Translations), and feel free to message me, contact me on Discord (@atravita) or send me a pull request!

### Compiling from source:

3. Fork [this repository](https://github.com/atravita-mods/StardewMods).
4. Make sure you have [dotnet-8.0-sdk](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed. I do use features from langversion 12, which requires this sdk, minimum.
5. If your copy of the game is not in the standard STEAM or Gog install locations, you may need to edit the csproj to point at it. [Instructions here](https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/mod-package.md#available-properties).

This project uses Pathos' multiplatform nuget, so it **should** build on any platform, although admittedly I've only tried it with Windows.