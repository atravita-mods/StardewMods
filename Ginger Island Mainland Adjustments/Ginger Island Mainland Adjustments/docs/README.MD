Documentation
=============

[← back to readme](../../README.md)

### Introduction

Ginger Island Mainland Adjustments adjusts schedules of villagers after they visit the Resort on the Island, adds some dialogue keys for villagers travelling to and from the Island, and optionally overrides the entire vanilla resort scheduler to allow for more visitors at the resort at once.

Certain behaviors of the mod can be altered by using Content Patcher to patch into specific targets this mod monitors. For example, this would add a custom NPC group:

```js
{
	"Format": "1.24.0",
	"Changes": [
		{
			"Action": "EditData",
			"Target": "Mods/atravita_Ginger_Island_Adjustments_groups",
			"Entries": {
				"GoldenTrio": "Harry, Hermione, Ron"
			}
		}
	]
}
```

The following data is what this mod will look for:

#### Specific GIRemainder schedules located in the NPC's usual schedule file

```js
{
	"Format": "1.24.0",
	"Changes": [
		{
			"Action": "EditData",
			"Target": "Characters/schedules/Leah",
			"Entries": {
				"GIRemainder": "1800 LeahHouse 8 5 0/2200 LeahHouse 13 10 2/2400 LeahHouse 2 4 3 leah_sleep"
			}
		}
	]
}
```

causes Leah to wander around in her house before she turns in for the night. The code will find the schedule, parse it, and append it to the end of Leah's schedule if she goes to Ginger Island.

For a full list of possible schedule keys, please see the [schedule documentation](../GIRemainderSchedules.md). There are also examples in the [example pack](https://github.com/atravita-mods/Ginger-Island-Mainland-Adjustments/blob/master/%5BCP%5D%20Ginger%20Island%20Mainland%20Adjustments/schedules.json).

#### Specific dialogue lines in the NPC's usual dialogue file

```js
{
	"Format": "1.24.0",
	"Changes": [
		{
			"Action": "EditData",
			"Target": "Characters/schedules/Leah",
			"Entries": {
				"GIRemainder": "1800 LeahHouse 8 5 0/2200 LeahHouse 13 10 2/2400 LeahHouse 2 4 3 leah_sleep"
			}
		}
	]
}
```

(For more examples, please see [the example pack!](https://github.com/atravita-mods/Ginger-Island-Mainland-Adjustments/tree/master/%5BCP%5D%20Ginger%20Island%20Mainland%20Adjustments)). 

See also: [Content Patcher documentation](https://github.com/Pathoschild/StardewMods/blob/develop/ContentPatcher/docs/author-guide.md).