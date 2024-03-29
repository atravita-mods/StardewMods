﻿Changelog
===================

[← back to readme](../../README.md)

#### Todo

2. Nonreplacing dialogue for the Resort keys? (flatten the ones that already exist, but add a way to add more that's less likely to clobber).
3. Handle roommates, like actually.
4. See if NPCs can go *into* Professor Snail's Tent?
5. Add in tokens for islanders/current bartender/musicians. You won't be able to use these on day start, but may prevent clobbering on the resort shop tile? <!-- does this matter when 1.6 will fix the issue for good?-->
7. Handle children better. Should they go with the spouse?
<!-- Move this mod's scheduler earlier so I can add in CP tokens. (so OnDayStarted or before?). Sadly, this is not feasible because CustomNPCExclusions expects the island schedules to be generated *after* CP is done updating tokens, and I would need to move it *before*. Would be a compat nightmare. see: https://github.com/Esca-MMC/CustomNPCExclusions/blob/master/CustomNPCExclusions/HarmonyPatch_IslandVisit.cs -->
<!-- Finish the locations console command: https://docs.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences to add bold -->
<!-- Write a function to get the villager's schedule for today, that takes into account that if a location replacement is needed, the villager's daySchedule.Value will be X_Replacement -->
<!-- More schedule debugging tools: get arbitrary schedule from X day? -->
<!-- Figure out why Emily dances *in* the changing room? -->
<!-- AntiSocial lines for George/Evelyn/Willy-->
<!-- Get spouses into Island Farmhouse -->
<!-- Get Willy to change in his bedroom? -->
<!-- Make sure the GI schedule keys are right.... -->
<!-- Document animations -->
<!-- Warp NPCs into, say, Town or Saloon, if they need to walk a long distance? -->
<!-- Document new tokens -->

##### Known Issues

1. NPCs may vanish if they go to `IslandSouthEast`. They reappear the next day. Therefore, that's been temporarily removed until I figure out why they disappear from `IslandSouthEast`. `IslandNorth` is fine.
2. If you pause time, NPCs will tend to get stuck at schedule points. Unfortunately for Ginger Island, this usually ends with NPCs trapped in the changing room. If you go to Ginger Island and see no one there, try unpausing time. Or just leave them trapped in the changing room....(or I guess, disable changing)
3. The debugging console commands basically only work for the host in multiplayer.

### Version 1.1.8
* Fix assumption in scheduling code that schedules will have at least two points.
* GIMA will now try to re-add the `default` or `spring` schedule if a mod tries removing that from a vanilla character. This is because the game expects one of those two schedules to exist and uses it as a default, and removing both may cause a crash.
* Add Chinese (thanks Herienseo!)

### Version 1.1.7
* Remove no longer necessary compat code.
* Add the `ScheduleStrictness` property, which I promise to document sometime....
* adds an option to prevent villagers with no Resort dialogue from going to the Resort.

### Version 1.1.6
* Various bugfixes

### Version 1.1.5

* Update to SMAPI 3.15.0
* Add `_groupname` suffix to GI `Resort` keys (ie `Resort_groupname`). <!-- test this? -->
* Updates to use AtraCore and associated efficiency improvements.
* Some bugfixes.

### Version 1.1.4

* Fix error for shops.

### Version 1.1.3
 
* Integration with Child2NPC.
* After a certain heart event, you'll can now call Pam, who will tell you if she's headed to Ginger Island. Assuming, that is, you didn't decide to insult her.
* You can now toggle beach outfits.
* Adds `GIReturn_Talked_<NPCname>` key, so your spouse can acknowledge that you've talked to them.

Fixes:
* Schedules are now selected correctly.
* Fix GILeave/Return dialogue not happening for farmhands. Spouses now have default GILeave/Return lines, although I would recommend still giving your favorite spouse a line or two.
* `bed` is supported for `GIRemainder` schedules.
* Fixes for splitscreen.
* Fixed fishing on Ginger Island.
* Internal optimizations.

### Version 1.1.2

* Adds Willy as a possible resort attendee.
* Adds fishing as a possible resort activity. (Updated content pack - Pam should now be able to fish).
* Removed children going to the resort when not with a group. Added back in the Penny/Vincent/Jas group I accidentally left out....
* Add in a way to exclude characters from visiting the island alone: they'll be able to go as a group or not at all. (Thanks for the suggestion, tiakall#4802!)
* Fix issue where spouses were not using the married schedules.

Internal
* Fixed up the console command for listing `NPC.routesFromLocationToLocation`.

### Version 1.1.1

* Fix issue where spouses would say their `GILeave` lines a day after they were supposed to.

### Version 1.1.0

* Initial upload.
