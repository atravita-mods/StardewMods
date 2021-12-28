Changelog
============

#### Todo

1. Alter dialogue conditions so `_Completed` is said for the rest of the day instead of being forgotten when the player dismisses the quest
2. Add condition for total number of quests (both Qi and otherwise completed.)
3. Document tokens.
4. Document dialogue keys, their storage, and how to use console commands to check that.

#### Version 1.0.4 (upcoming)

* Added tokens (Current, Available, Completed). (Thanks, Lumina, for the suggestion!)
* Added `achievement`, `walnutcount` to possible tags. De-hardcoded the stats section (and added a console command to list available stats).
* Moved saving of previously-said dialogue lines to a custom data class instead of using mailflags.

#### Version 1.0.3

No real changes, just recompiled for NET 5.0/Stardew 1.5.5/SMAPI 3.13.0+

#### Version 1.0.2

Adds dialogue keys, adds the `dropboxRoom` tag.

#### Version 1.0.1 

Added tags for active conversation topics, total money earned, and crafting/cooking recipes.
