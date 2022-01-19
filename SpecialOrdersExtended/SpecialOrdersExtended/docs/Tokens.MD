Tokens
=======

New in 1.0.4

Tokens for Content Patcher have been added that reveal the current active quests, the current available quests, all completed quests, and the current active special orders rules.

Each token can be used without any input to get a list of all orders. For example,  `{{atravita.SpecialOrdersExtended/Current}}` will get you a list of all current orders. The single input `|contains=` will cause the token to produce either `true` or `false`. For example, `{{atravita.SpecialOrdersExtended/Current|contains=questkey}}` will return `true` if `questkey` is currently active, and `false` otherwise.

#### A note on custom dropboxes.

Custom dropboxes are quite easy to add with map edits. Just add `action Dropbox Dropboxname` to a tile. That said: it's fairly easy to accidentally overwrite someone else's custom dropbox. 