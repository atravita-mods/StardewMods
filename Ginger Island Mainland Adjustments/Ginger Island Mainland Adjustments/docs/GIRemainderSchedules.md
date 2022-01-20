GIRemainder schedules work very similarly to how normal schedule keys work and should be placed in the usual schedule file `Characters/schedules/<NPCname>`.

All GIRemainder keys must start with `GIRemainder`. If an NPC is married, they'll use `GIRemainder_marriage` as their base key instead, and the normal keys are entirely ignored.

Next, keys are checked in the following order

1. `<base>_<season>_<day>` (ie `GIRemainder_winter_19`. Note that NPCs can't go to GI on their annual checkup day. They also can't go during the Night Market)
2. `<base>_<day>_<hearts>` (ie `GIRemainder_19_8`)
3. `<base>_<day>` (ie `GIRemainder_19`)
4. `<base>_rain` (for rainy days)
5. `<base>_<season>_<short day of week><hearts>` (ie `GIRemainder_winter_Fri6`)
6. `<base>_<season>_<short day of week>` (ie `GIRemainder_winter_Fri`)
7. `<base>_<short day of week><hearts>` (ie `GIRemainder_Fri6`)
8. `<base>_<short day of week>` (ie `GIRemainder_Fri`)
9. `<base>` (`GIRemainder`)

`<hearts>` are limited to even numbers.

GI schedules are constructed similarly to normal schedules. You can use `MAIL`/`GOTO`/`NOT` friendship basically the same as vanilla keys; however, if the key is rejected due to error or whatever, GI schedules go to the next valid schedule and not just direct to `spring`. The construction of each element is also the same, `<time> <location> <x> <y> <facing direction> <animation> <dialogue>`.

For GI keys, the first time *has* to be 1800. In general, it takes quite a while for NPCs to cross the map after getting home from GI, so I wouldn't recommend putting the next key any closer than 2100.