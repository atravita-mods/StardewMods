Camera Pan
===========================
<!--![Header image](docs/pig.png)-->

Allows you to freely pan the camera.

## Install

1. Install the latest version of [SMAPI](https://smapi.io).
2. Download and install [AtraCore](https://www.nexusmods.com/stardewvalley/mods/12932).
2. Download this mod and unzip it into `Stardew Valley/Mods`.
3. Run the game using SMAPI.

## Uninstall
Simply delete from your Mods directory.

## Configuration.
Run SMAPI at least once with this mod installed to generate the `config.json`, or use [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) to configure.

#### General Options
* `ToggleBehavior`: Sets how the camera should be toggled on and off. Available options: `Always` (panning always enabled), `Never` (panning never enabled), `Toggle` (a toggle key enables or disables the panning), or `Camera` (holding the camera object enables the camera. Check Robin for it!).
* `UseMouseToPan`: If enabled, moving the mouse to the edges of the screen will cause the camera to pan, *if camera panning is enabled*.
* `ResetWhenDamageTaken`: If enabled, the camera will pan back towards the player if the player takes damage.
* `Speed`: Controls how fast the camera pans.

#### Panning boundaries
* `XRange`: Controls how far the camera can get away from the player (in the x-direction).
* `YRange`: Controls how far the camera can get away from the player (in the y-direction).
* `KeepPlayerOnScreen`: If enabled, the camera will not pan sufficiently far to move the player off-screen.
* `ShowArrowsToOtherPlayer`: If enabled, other players on the same map will be pointed out by a small arrow.
* `SelfColor`: The color of the arrow pointing to the current player.
* `FriendColor`: The color of the arrow pointing to any other player on the map.

#### Keybinds
Set any of the keybinds to null to disable.

* `ToggleButton`: The button used to toggle the camera, if `ToggleBehavior` is set to `Toggle`.
* `ResetButton`: The button that resets the camera over the player.
* `UpButton`, `DownButton`, `LeftButton`, `RightButton` pan the camera.

#### Map-specific camera behavior.
These control the default map behavior. The four options are:

* `Vanilla`: Completely vanilla camera. No panning, no locking. The camera will in general stay in-bounds.
* `Offset`: Panning is enabled, but the camera is not locked.
* `Locked`: The default vanilla camera will try to stay in-bounds on the map. This locks the camera to the player, including allowing it to leave the map boundaries.
* `Both`: This combines `Offset` and `Locked`. The camera will lock to the panned position.

Maps are generally categorized as indoor and outdoors and you can set those separately, or you can set a per-map override.

Special notes: the vanilla map property `ViewportFollowPlayer` automatically engages `Vanilla` -> `Locked` and `Offset` -> `Both`, and this is enabled normally in the mines. Additionally, the map property `atravita.PanningForbidden` allows map modders to disable panning on their map.

## Compatibility

* Works with Stardew Valley 1.5.6 on Linux/macOS/Windows.
* Works in single player, multiplayer, and split-screen mode. Should be fine if installed for only one player in multiplayer.
* Should be compatible with most other mods. A note on Critter Rings: `Locked` and `Both` will effectively disable the Frog Ring's panning.

## See also

[Changelog](docs/changelog.md)
