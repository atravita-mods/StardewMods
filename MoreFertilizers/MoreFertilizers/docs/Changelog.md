Changelog
=============

#### Version 0.3.0 TODO items
* Bush fertilizers
    - Handle adding to shops and other acquisition methods.
* Beverage fertilizer
    - Fruit trees (DGA, Automate?)
* Acquisition: maybe have fertilizer appear in big slimes outside the mines?
* Items and all the implementation for the prismatic, radioactive, and everlasting fertilizers (don't forget that JA and Winter Star also transpile in this space)
* Some fertilizer for trees? Tree tapper fertilizer?
* Something for mushroom boxen? (1.6 - reduce to one day?)
* Make good draw code for all of the fertilizers. (don't forget domesticated fish - maybe some more jumping...)
* PR Pathos to make CJB pass in the correct fertilizer (b/c Everlasting)
* Read from JA's files to get the old IDs if I don't have them.

#### Version 0.3.0
* Added new fertilizers:
    - Bountiful Bush Fertilizer: increases the production periods of tea and berry bushes.
    - Fertilizer of Miraculous Beverages: makes beverages appear.
    - Rapid Bush Fertilizer: makes tea tree bushes grow 20% faster.
    - Secret Joja Fertilizer: decreases growth time, has a chance of decreasing regrowth time, but forces the crop to be base quality.
    - Wisdom Fertilizer: increases farming XP by 10%.
    - Seedy Fertilizer: drops the seed! (doesn't work for DGA crops yet).
* Adjusted previous fertilizers:
    - Waterlogged now increases the growth speed of paddy crops a little.
* Added German translation (thanks to CrisTortion!)
* Fixes the lucky fertilizer.
* Fixes integration with Prismatic/Radioactive tools.
* Fixes organic fertilizer with beer, pale ale, green tea, mead, and coffee.
* Internal improvements.
* Deshuffle code fixes.

#### Version 0.2.0
* Move to using AtraCore, which should have improved loading times significantly.
* Added Chinese translation (thanks to Puffeeydii!)
* Add integration with SolidFoundations.

#### Version 0.1.7
* Some efficiency improvements
* Drop IDs when returning to title, because JA does also.
* A bit of rebalancing - fish food now slightly reduces time to bite, and the drop rates of fertilizers on the farm have been adjusted.
* Bone mill now produces fertilizers when Automate is installed.
* Volcano dungeon chests may now contain fertilizers.

#### Version 0.1.6
* Organic seeds now apply the organic fertilizer.
* Fixed deshuffling code. Again.

#### Version 0.1.5
* Joja crops no longer can become organic.
* Mill preserves organic
* Fixed errors in deshuffling code.
* Add compat for PFMAutomate.

#### Version 0.1.4

* Fix organic artisan goods not stacking when they should stack.

#### Version 0.1.3
* Fix issue for bigcraftables that don't set their held item?

#### Version 0.1.2
* Adjusted radius check for specially-placed fertilizers to allow for more distance.

#### Version 0.1.1

* Update to SMAPI 3.14.0
* Adjust level-based acquisition of fertilizer to account for WoL's extending levels.
* Added French translation (thanks Schneitizel!)
* Added compat for Automate, DGA and MultiYieldCrops.
* Fix Lucky Fertilizer not actually showing up in Pierre's shop....

#### Version 0.1.0

Initial upload.
