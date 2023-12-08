Screenshots
=================================

This mod takes screenshots, automatically.

## Install

1. Install the latest version of [SMAPI](https://smapi.io).
2. Download and install [AtraCore](https://www.nexusmods.com/stardewvalley/mods/12932).
2. Download this mod and unzip it into `Stardew Valley/Mods`.
3. Run the game using SMAPI.

## Configuration

Bear with me a bit, the configuration is a little complex, and not all of it works through GMCM. Like every other mod, it's a json file that's generated the first time you run the game with the mod installed.

### Basics
Let's start with the basic screenshot settings. These are global, affecting every screenshot.
* **Notification**: whether or not to show a small toast notification when a screenshot is taken.
* **ScreenFlash**: whether or not to cause the screen to flash white when a screenshot is taken.
* **AudioCue**: whether or not to play an audio cue when a screenshot is taken.

### Keybind
* **KeyBind**: Binds map screenshots to a keybind. See [SMAPI documentation](https://stardewvalleywiki.com/Modding:Player_Guide/Key_Bindings). Default is the `*` key.
* **KeyBindScale**: The scale used with screenshots taken by keybind. `0.25` by default.
* **KeyBindFileName**: The tokenized path (see below) where screenshots taken by keybind should be saved.


### Tokenized paths
*Default value: `"{{Default}}/{{Save}}/{{Location}}/{{Date}}.png"`*

This mod uses a token system to generate file paths. The `{{` and `}}` indicate tokens where information will be filled in to construct the final path. Tokens are case-insensitive.

These are the valid tokens:

| Token     | Usage
| -----------|-----------------------------------------------
| `Default` | The default screenshot directory of the game. Note that if you don't start the path with `{{Default}}` you should probably give it an absolute path.
| `Location`| The unique name of a location.
| `Context` | The name of this location's location context.
| `Save` | The farm's name, followed by the unique ID for the save. This acts as a distinct identifier for the save.
| `Farm` | The name of the farm.
| `Name`| The current player's name.
| `Date`| The in-game date, in the form `Year-Month-Day` for easier sorting.
| `Weather` | The in-game weather conditions.
| `Time`| The current game time (in military time.)
| `Timestamp`| The current real-life time.
| `Rule` | The name of the rule.

*Any other values will be left unchanged. Paths MUST end with .png, or else that will be added for you.*

### Rules
The automated part of the mod works with a list of rules. They are listed in the config file as Name->Rule pairs.

For **each game location, only one screenshot can be taken per day**, no matter how many valid rules match. The first match will be used.

Each rule has the following:

| Option | Usage | Example
| -------|-------|------------------------------
| `Maps` | A list of maps this rule is valid for, by internal name. <br>Special cases:<ul><li>`*` refers to **every** non-generated map.<li>You may also use the name of the location context (ie `"Island"` refers to every Ginger Island map.)<li>Use `"Mines"` to refer to the mines, `"SkullCavern"` to refer to the skull cavern, `"Quarry"` to refer to the quarry mines, and `"Volcano"` to refer to the inside of the volcano.<li>Building interiors use the non-unique name, ie (`"Barn"`).</ul>Use the mod [Debug Mode](https://www.nexusmods.com/stardewvalley/mods/679) to find the internal names.| <ul><li>`["FarmHouse", "IslandFarmHouse"]` would go off in either the vanilla farm house or the ginger island farmhouse.<li>`["*"]` refers to **every** map.</ul> Note that only one screenshot can happen per location per day.
| `Path` | The tokenized path to save this particular rule at. See above for options. | `"{{Default}}/{{Save}}/{{Location}}/{{Date}}.png"`
| `Scale` | The scale to save the screenshot at. | `0.25`
| `DuringEvents` | If true, the rule can fire during an event. If false, it will wait for the event to be over. | `true`
| `Triggers` | A list of triggers that may activate this rule. A rule will activate the first time in a day where ANY trigger is valid. | See below.

### Triggers.
A trigger represents a state where a rule can activate. Each rule has a **list** of triggers, and a single trigger becoming valid will activate the rule.

Triggers are ONLY checked at the start of the day and when you warp from map to map.

Valid trigger options

| Option | Usage | Example
| -------|-------|------------------------
| `Cooldown`| The number of days after the last screenshot to this location that need to pass before this trigger can fire. | `1`
| `Seasons` | A list of valid seasons, or `"Any"` for any season. Case insensitive. | `["Spring", "Summer"]` to only take pictures in Spring or Summer.
| `Days` | The valid day *ranges* for this trigger. This may be formated as: <ol><li>An exact day (like `"5"`).<li>A range (ie `"1-5"` for days 1- 5). May be left half open (ie `"-6"` for days 1-6).<li>A day of the week (case insensitive, ie `"Monday"`)<li>The exact word `"Any"` for any day.</ol> | <ul><li> `["Any"]` for any day. <li> `["Monday", "Tuesday"]` for Mondays and Tuesdays. <li> `["1-7"]` for the first seven days of a month.</ul>
| `Time` | A list of time ranges (both inclusive), in the format `"start-end"`, in military time. | <ul><li>```[ "0600-2600" ]``` for the entire day. <li>```[ "0600-1000", "2200-2600" ]``` for mornings from 6AM to 10AM, then evenings from 10PM to 2AM.</ul>
| `Weather`| The current weather conditions. May be one of these values: `"Sunny"`, `"Rainy"`, or `"Any"`. Does NOT use the game's internal weather names. Note that this checks ONLY if the current weather can be considered a rainy weather, so, basically, windy is sunny and storming is rainy. | `"Any"` 
| `Condition` (specialized) | A `GameStateQuery` that applies to this trigger. May be used for more detailed control. See [format](https://stardewvalleywiki.com/Modding:Game_state_queries).<br><br> This is a specialized setting that can be useful for more detailed control, but most people will not use it.| `null` for not applicable. `"PLAYER_HAS_FLAG Current qiCave"`  would check if the current player had the specific mail flag `"qiCave"`.

If you're confused, check the default config! There's one entry automatically populated, for the Farm. I **strongly** recommend using VSCode to edit this!

### Example

A full example of a rule is as follows

```js
    "Farm": { // the name of the rule, will be shown on a toaster pop up. Must be unique, but you can make it whatever you want.
      "Maps": [
        "Farm" // The valid locations, in this case, farm.
      ],
      "Triggers": [
        // Remember, multiple triggers are allowed, and the rule will fire the FIRST time ANY trigger is valid in a day!
        // We have two here.
        {
          "Cooldown": 1, // allow a screenshot per day.
          "Seasons": [
            "Any" // the valid season.
          ],
          "Days": [
            "Monday" // this trigger is valid only on Mondays.
          ],
          "Time": [
            "0600-2600" // This trigger is valid between the hours of 6AM and 2AM the next day.
          ],
          "Weather": "Sunny" // Only sunny weather is allowed!
        },

        // okay, but what if it was raining on Monday? Let's do a catchup trigger.
        // this one will will fire on any day if there hasn't been a screenshot for an entire week.
        {
          "Cooldown": 7, // seven full days must pass before this trigger will fire.
          "Seasons": [
            "Any"
          ],
          "Days": [
            "Any" // but this trigger is valid on any day.
          ],
          "Time": [
            "0600-2600",
          ],
          "Weather": "Sunny"
        }
      ],

      // The path to save to is tokenized (see above!)
      // In this case, we want to save in the default screenshot directory, in a directory for this save,
      // then a directory per location, then name each file based on the in-game date.
      "Path": "{{Default}}\\{{Save}}\\{{Location}}\\{{Date}}.png",

      // The scale to use for this rule.
      "Scale": 0.25,

      // This trigger is allowed to go off during an event.
      "DuringEvents": true
    },
```

## Optimizations

*Hey Atra, there's like two other screenshot mods. Why did you do this?*

Well, fundamentally, this mod started because I had a beef with the game's screenshot method. In that it was kinda slow.

The way the screenshot method of the game works is roughly this:
* Render upon a render buffer the entire map.
* Re-render it for resizing/cropping reasons.
* Transfer the data out of the render buffer into an array.
* Transfer the data out of that array into a SkiaSharp bitmap.
* Transfer the data from the SkiaSharp bitmap to a SkiaSharp canvas.
* Save the SkiaSharp canvas to disk.

Which worked fine! Except, slow, and I was getting tired of the game locking up when walking out to my farm first thing in the morning. Now, normally in game, you don't notice, because for the game, the only way you can take a screenshot is in a menu. Sure, your splitscreen partner might be annoyed, but you don't care.

This is different if there's a mod calling that function. In which case, well. It suddenly appears like the game is locking up. So let's see what we can do about that.

### Low hanging fruit.

First things first: there's no reason to do file system operations on the main thread. So saving to disk gets kicked to a task. Immediate win!

Also, because C# is a GC'ed language, the runtime has to keep track of everything we allocate, so it's usually a win to reuse as much as possible. I call this "buffer hoisting", I'm not quite sure the actual technical term, but the tl;dr is don't allocate in a loop if possible. There is no need to re-declare the original render texture for example, it never changes size.

Finally, we don't necessarily have to re-render, so skipping that step when unnecessary saves time.

### Let's go deeper.

All the render operations have to happen on the main thread, and also, grabbing data out of the render buffers. This is a monogame limitation. Additionally, you can only interact with a Skia canvas on one thread. But there's no reason for these to be the same thread. We can do a coroutine.

Fundamentally, in a coroutine, one thread pushes work to a queue for the other to do. (Technical people might notice that my queue doesn't actually guarantee any sort of ordering. It's not necessary here.)

Roughly:

```

    +-------------------+         +------------------------+
    |    UI thread      |         |       Skia thread      |
    +-------------------+         +------------------------+
              |                               |
              v                               |
    +-------------------+                     |
    |   Initial setup   |                     |
    +-------------------+                     |
              |                               |
              v                               |
    +-------------------+                     |
 |->| Render to texture |                     v
 |  |   Copy to skia    |         +------------------------+
 |  | send skia buffer  | ---->   | Take buffer from queue | <-|
 |  +-------------------+         | Write buffer to canvas |   |
 |            |                   +------------------------+   |
 |------------|                               |                |
                                              |----------------|
                                              v
                                  +------------------------+
                                  |       write file       |
                                  +------------------------+
```

Now, everything that can be off the main thread is off the main thread.

### The danger zone.

The last bit of optimization I did centers around getting the data out of the texture and into skia. Monogame provides a method for this (`Texture2d.GetData`) whose implementation is interesting. Roughly:

* Take the texture, and copy to a (newly declared) array.
* Take the data from that array, and copy it to an array the user passed in.

That's two copies (and one newly declared array), and then *I have to copy to skia.* But the initial function, the function that grabs the data from the texture in the first place just takes...a pointer. And I can get a pointer to the skia buffer. And I know the data format, the way each represents a Color, is actually the same (well, if you declare the skia buffer correctly) - four bytes, ABGR. So I should be able to transfer it directly, right?

Unfortunately, that is...`internal` to monogame. It's not like I can call that function...except we totally can, nothing a [little publicizer](https://www.nuget.org/packages/Krafs.Publicizer) won't fix.

With this last little trick, I can skip the intermediate transfer buffers entirely and directly copy the data out of the texture straight into a skiasharp bitmap. This is about 30% faster.

### That said.

I still don't recommend trying to take full scale (`scale = 1`) pictures of large maps, especially in multiplayer. On my computer, the final total time for a screenshot of `Town` (one of the biggest maps) at full scale is about 300-500 ms, with occasional random spikes. (The original time was closer to 1-2 seconds). At quarter scale, it's about 150 ms or so, so still a stutter you can feel if you try to take a screenshot during gameplay, but hides well enough in a warp otherwise.

### What else takes time?

Currently, the longest bits of time are:
* The remaining time in getting data out of the render textures, which is in native code and thus something I'm not touching.
* On-demand loading of textures and assets before rendering, which would be best optimized in SMAPI.

## FAQ:
* **Why is there (some random other mod's UI element) repeated over my map?** Because they forgot to disable drawing that during screenshots. Nothing I can do, tell them to not draw if `Game1.game1.takingMapScreenshot` is true.
* **Huh, I can see stitching lines in the screenshot.** I recommend setting the scale to `0.25`, `0.50`, `0.75`, or `1`. Any other value may cause stitching artifacts.
* **Why not reuse the SKBitmaps?** Because it doesn't actually matter. Skia will make a defensive copy of the `SKBitmap` when copying to the `SKCanvas` unless I specify it's immutable, in which case I can't reuse it.
* **What next?** I'd love to override the game's ambiance to get more consistent screenshots, but so far haven't figured that one out. And I'd like to have a more intuitive gui-based config editor. Stay tuned.

## Acknowledgements

This was, of course, inspired by [DailyScreenshot by CompSciLauren](https://github.com/CompSciLauren/stardew-valley-daily-screenshot-mod).

## Compatibility

* Works with Stardew Valley 1.6 on Linux/macOS/Windows.
* Works in single player, multiplayer, and split-screen mode. Can be installed by a single player in multiplayer.
* Should be compatible with most other mods. 

## See also

* [Changelog](docs/Changelog.md)
