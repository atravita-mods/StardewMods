## Event Commands

[<- back](../README.md)

AtraCore packs the following event commands. Note that event commands are case insensitive.

Command name | Example | Command effect 
-------------|---------|-------------
`atravita_AllowRepeatAfter <events> [days]` | <ul>`atravita_AllowRepeatAfter 7` to allow the current event to repeat after 7 days.<li>`atravita_AllowRepeatAfter myEventId myOtherEventId 7` would allow myEventId and myOtherEventId (but not the current event) to repeat after 7 days.</ul> | After the given number of days, removes the event from the player's eventsSeen hashset to allow the event to be re-seen.