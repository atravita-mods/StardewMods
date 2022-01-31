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




(For more examples, please see [the example pack!](https://github.com/atravita-mods/Ginger-Island-Mainland-Adjustments/tree/master/%5BCP%5D%20Ginger%20Island%20Mainland%20Adjustments)). 

See also: [Content Patcher documentation](https://github.com/Pathoschild/StardewMods/blob/develop/ContentPatcher/docs/author-guide.md).