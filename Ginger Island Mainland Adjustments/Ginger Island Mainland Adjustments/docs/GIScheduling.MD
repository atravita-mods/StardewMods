Ginger-Island Scheduling
=======================

[← back to readme](../../README.md) [← back to main guide](../README.md)

If the setting `UseThisScheduler` is enabled, this mod will try to schedule NPCs at Ginger Island using the below data:

### Custom Groups

Vanilla NPCs may go to Ginger Island in groups. They're typically family groups, although all the bachelors and all the bachelorettes are a possible group.

#### Explorer Groups

### Specific roles

#### Bartenders

Gus, whenever he visits, will tend bar. When he does so, other characters (if they are adults) will crowd around him and drink. You can assign another NPC the bartender role by patching `Mods/atravita_Ginger_Island_Adjustments_bartenders` and using the internal name of the NPC as the key. (The value is ignored and can be anything).

'''js
{
	"Format": "1.24.0",
	"Changes": [
		{
			"Action": "EditData",
			"Target": "Mods/atravita_Ginger_Island_Adjustments_bartenders",
			"Entries": {
				"NPCname": null,
			}
		}
	]
}
'''

(Note: the Resort shop still requires Gus's presence. You can use Shop Tile Framework if you'd like your bartender to have a shop of their own, I suspect.)

#### Musicians

