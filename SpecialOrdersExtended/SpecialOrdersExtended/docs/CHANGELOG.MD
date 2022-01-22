Changelog
#### Todo

1. Add condition to just randomly reduce the chance of a quest appearing. (May need integrate with that one mod that changes the day special orders appear?)
2. Add condition for total number of quests (both Qi and otherwise completed.)
3. Add dialogue condition for Recently Completed Order, for use with repeat orders....somehow.
3. Add tag for professions (and handle prestige mods as well?)
3. Alter `week_X` tag to handle extended months.

#### Stardew 1.6 todo

1. Register anything not included in Game State Queries as GSQ
2. Push people towards GSQ

##### In consideration

1. Somehow limit the kill objective to just monsters on a certain (few?) maps. (Perhaps a custom objective, perhaps a Special Rule).
2. De-hardcode skill levels and consider a way to support mod skills?
6. Implement a temporary saving mechanism for Save Anywhere (look into their API?)

#### Version 1.0.6

* Add verbosity config option. Intended mostly for mod authors.
* `week_X` now handles extended months (in theory.)
* Add `profession` tag. Makes `skilllevel` tag take into account Spacecore skills, if that's installed (in theory. Memo to self: test that).<!-- todo: document professions tag -->

#### Version 1.0.5

* In theory: fixes splitscreen.
* `Available` token now correctly returns no entries if the special orders board is not unlocked. Also, adds `_IsAvailable` as a possible dialogue option.
* Adds dialogue key `_RepeatOrder`, which is cleared seven days after the completion of the order.
* Check heart levels for dialogue conditions above 14.
* Added tags: `married`.
* (Internal) Use delegates instead of `PropertyInfo`s.

#### Version 1.0.4

* Added tokens (`Current`, `Available`, `Completed`, `RecentCompleted`). (Thanks, Lumina, for the suggestion!)
* Added token for the current special order rules (`CurrentRules`) (Thanks, shekurika, for the suggestion!)
* Added `achievement`, `walnutcount` to possible tags. De-hardcoded the stats section (and added a console command to list available stats). (Thanks, vert, for the suggestion!)
* Moved saving of previously-said dialogue lines to a custom data class instead of using mailflags.
* Alter dialogue conditions so `_Completed` is said for the rest of the day instead of being forgotten when the player dismisses the quest

#### Version 1.0.3

No real changes, just recompiled for NET 5.0/Stardew 1.5.5/SMAPI 3.13.0+

#### Version 1.0.2

Adds dialogue keys, adds the `dropboxRoom` tag.

#### Version 1.0.1 

Added tags for active conversation topics, total money earned, and crafting/cooking recipes.
