Screenshots
=================================

This mod takes screenshots, automatically.

## Install

1. Install the latest version of [SMAPI](https://smapi.io).
2. Download and install [AtraCore](https://www.nexusmods.com/stardewvalley/mods/12932).
2. Download this mod and unzip it into `Stardew Valley/Mods`.
3. Run the game using SMAPI.

## Configuration

## Optimizations

*Hey Atra, there's like two other screenshot mods. Why did you do this?*

Well, fundamentally, this mod started because I had a beef with the game's screenshot method. In that, it was kinda slow.

The way the screenshot method of the game works is roughly this:
* Render upon a render buffer the entire map.
* Re-render it for resizing/cropping reasons.
* Transfer the data out of the render buffer into an array.
* Transfer the data out of that array into a SkiaSharp bitmap.
* Transfer the data from the SkiaSharp bitmap to a SkiaSharp canvas.
* Save the SkiaSharp canvas to disk.

Which worked fine! Except, it's kinda slow. Now, normally in game, you don't notice, because for the game, the only way you can take a screenshot is in a menu. Sure, your splitscreen partner might be annoyed, but you don't care.

This is different if there's a mod calling that function. In which case, well. It suddenly appears like the game is locking up.

## Compatibility

* Works with Stardew Valley 1.6 on Linux/macOS/Windows.
* Works in single player, multiplayer, and split-screen mode.
* Should be compatible with most other mods. 

## See also

* [Changelog](docs/Changelog.md)
