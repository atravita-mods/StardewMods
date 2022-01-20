Ginger-Island specific Dialogue Keys
=========================================

Characters who have island schedules and are not currently on `IslandSouth` can call upon vacation dialogue keys in the usual dialogue file for each character
`Characters/Dialogue/<NPCname>`. These dialogue keys are said at most once per day.

Vacation keys are inserted just after the location-specific dialogue keys and follow the general form `<season><base><remainder>`. Like the location keys, the season-specific variants are checked first.

The `<base>` is `Resort_Approach` before 1200. After 1800, the base is `Resort_Left` when the character is outside (ie, when they're traveling away from Ginger Island). When they're inside, the base is `Resort_Left_<location>` - ie `Resort_Left_Saloon`. Married NPCs will also check for base keys with `_marriage` appended to it, but will use non-marriage specific dialogue keys if a married variant is not available.

Next, you can limit the times the key will be used with the `<remainder>`. Remainders are checked in the following order:

1. `<base>_<ShortDayOfWeek>`. IE - `Resort_Left_Fri`
2. `<base><hearts>`, where `<hearts>` is any even number - the code will use the highest value it can find where the friendship requirement is satisfied. IE - `Resort_Left6`
3. `<base>`. IE Resort_Left

Examples of keys

* `winterResort_Left_Saloon_Fri` - said at the Saloon, in winter on Fridays, after coming back from the Resort.
* `winterResort_Left_Saloon_marriage_Fri` - said by married characters, at the Saloon, in winter on Fridays, after coming back from the Resort. (If this key is omitted, the married character will say the `winterResort_Left_Saloon_Fri` dialogue.
* `winterResort_Left_Fri` - said *outside only*, after coming back from the Resort, in winter on Fridays.
* `winterResort_Left` - said *outside only*, after coming back from the resort in winter.
* `Resort_Left` - always said *outside only* after coming back from the resort.
* `Resort_Approach` - said before heading to the resort in the morning, always, indoors or outdoors.

(Why is this not consistent? It's because characters in general do not do things before heading to Ginger Island, while `GIRemainder` schedule keys allow for characters to do things *after* returning.)