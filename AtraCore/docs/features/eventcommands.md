## Event Commands

[<- back](../README.md)

AtraCore packs the following event commands. Note that event commands are case insensitive.

Command name | Example | Command effect 
-------------|---------|-------------
`atravita_AllowRepeatAfter <events> [days]` | <ul><li>`atravita_AllowRepeatAfter 7` to allow the current event to repeat after 7 days.<li>`atravita_AllowRepeatAfter myEventId myOtherEventId 7` would allow myEventId and myOtherEventId (but not the current event) to repeat after 7 days.</ul> | After the given number of days, removes the event from the player's eventsSeen hashset to allow the event to be re-seen.
`atravita_BranchIf <branch> <GSQ>` | Switches the event to the named branch if the GSQ resolves to true. | `atravita_BranchIf event_branch PLAYER_HAS_MAIL Current WhateverMail Received`
`atravita_SetInvisible <name> [days]` | Sets an NPC invisible for the number of days given. Note that unlike the `Data/TriggerActions` way, this **works even if called from a farmhand.** | `atravita_SetInvisible Pam 14`