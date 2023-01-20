# Compass2

## Features

### Compasses!
* Magnetic Compass
  * Points north.
  * Crafted with 1 fired clay bowl, 1 stick, and either 1 magnetite nugget or 1 metal scraps.
* Origin Compass
  * Points to the world's default spawn _at time of its creation_.
    * If world spawn is later changed, the compass will not point at the new location.
    * If world spawn has a radius, it points to the center of it.
  * Crafted with 1 Magnetic Compass and 2 Temporal Gears.
* Relative Compass
  * Points to the location where it was created.
  * Crafted with 1 Origin Compass and 2 Temporal Gears.
* **\*NEW\*** Player Compass
  * Points to the player who first picks it up (usually the one who crafted it).
  * Does not track an offline player.
  * No crafting recipe yet. Feel free to add your own!

### **\*NEW\*** Storage
* Compasses can be held in your offhand.
* Compasses can be placed on the ground with other ground-storables.
* Compasses can be placed on shelves.
* Compasses can be placed in display cases.

### **\*NEW\*** Admin Commands
* Requires "commandplayer" permissions.
* Only function on the compass held in the active hotbar slot
* `/compass show`
  * Displays the compass's internal data: creator and target position.
* `/compass set target <X> <Y> <Z>`
  * Changes the compass's target position to the provided position.
  * Supports flexible pos inputs, i.e. `~10 ~0 ~-15` denotes a position relative to the player, `=512000 =120 =512000` denotes an absolute position, and `0 120 0` denotes a map coordinate.
* `/compass set craftedBy [<player_name>|<player_uid>|<any_string>]`
  * Changes who the compass was created by (currently only matters for the Player Compass).
  * Can be a player's Name, a player's UID, or an unrelated string.
* `/compass reset`
  * Causes the compass to act as though it had just been crafted.
* `/compass remove target`
  * Deletes the compass's saved target position.
* `/compass remove craftedBy`
  * Deletes the compass's saved creator.
  * Currently, this will cause the compass to act as though it had just been crafted
* `/compass for [<player_name>|<player_uid>] [show|set|reset]`
  * Performs the given command on behalf of the specified player, as if that player had run the command themselves.
  * Example: ServerAdmin runs the command `/compass for OtherPlayer set target 10000 120 10000`. If OtherPlayer is holding a compass in their active hotbar slot, its target will be set to 10000, 120, 10000.

### Configuration
The below configuration options are applied before Vintage Story's JSON patching system. Changes made through JSON patching take priority over the changes made in the provided configuration file and should be safe from conflicts.

<details><summary>Sample configuration file with default settings</summary>

```json
{
  "EnableMagneticRecipeDesc": "Enable crafting a Magnetic Compass with a Magnetite Nugget.",
  "EnableMagneticRecipe": true,
  "EnableScrapRecipeDesc": "Enable additional recipe for the Magnetic Compass. Uses Metal Scraps instead of Magnetite.",
  "EnableScrapRecipe": true,
  "EnableOriginRecipeDesc": "Allow the Origin Compass to be crafted. <REQUIRED TO CRAFT THE RELATIVE COMPASS>",
  "EnableOriginRecipe": true,
  "EnableRelativeRecipeDesc": "Allow the Relative Compass to be crafted.",
  "EnableRelativeRecipe": true,
  "OriginCompassGearsDesc": "Number of Temporal Gears required to craft the Origin Compass. Min: 1, Max: 8",
  "OriginCompassGears": 3,
  "RelativeCompassGearsDesc": "Number of Temporal Gears required to craft the Relative Compass. Min: 1, Max: 8",
  "RelativeCompassGears": 4,
  "AllowCompassesInOffhandDesc": "Allow a player to place a compass in their offhand slot.",
  "AllowCompassesInOffhand": true
}
```

</details>

### **\*NEW\*** Modding the mod
There are many details of the compasses that can be modified via JSON. Below is a sample asset file with only the relevant properties. Custom properties are all further explained below. Additional information is provided when relevant.

NOTE: Compasses *MUST* be blocks, not items, due to differences in how Vintage Story handles block and item rendering, and how compass animations were enabled where Vintage Story does not support them.

<details><summary>Sample Asset JSON</summary>

```json
{
  "textures": { "shell": { "base": "game:block/clay/ceramic-dark" } },
  "texturesByType": {
    "*-magnetic": { "needle": { "base": "game:item/resource/nugget/magnetite" } },
    "*-relative": { "needle": { "base": "game:block/metal/plate/gold" } },
    "*-origin": { "needle": { "base": "game:block/fire-blue" } },
    "*-player": { "needle": { "base": "game:item/resource/nugget/malachite" } }
  },
  "shape": { "base": "block/compass/shell" },
  "shapeInventory": { "base": "block/compass/complete" },
  "attributes": {
    "XZTrackerProps": {
      "needleShapeLocation": "compass:block/compass/needle",
      "needleGlowLevel": 0,
      "needleGlowLevelByType": {
        "*-origin": 25,
        "*-player": 50
      },
      "maximumMeshes": 120,
      "distanceMethod": "manhattan",
      "minTrackingDistance": 5
    }
  },
  "vertexFlags": {
    "glowLevelByType": {
      "*-origin": 10,
      "*-player": 20
    }
  }
}

```
</details>

<details><summary>Asset Properties Details</summary>

#### **shape** and **shapeInventory**: `/shape` and `/shapeInventory`
Due to the hackiness used to allow animations for the compasses in inventory, in display cases, on shelves, and on the ground, a compass's `shape` must be the location of the shape/model asset containing only the non-moving portions, the 'shell' of the compass. `shapeInventory` must be the location of the complete shape/model asset, containing both the shell and the needle.

#### **XZTrackerProps**: `/attributes/XZTrackerProps`
Contains all the custom properties made for compasses.

#### **needleShapeLocation**: `/attributes/XZTrackerProps/needleShapeLocation`
Similar to `shape` and `shapeInventory`, this must be the location of the shape/model asset for the compass's needle. _The origin point of the first root element in this shape is used for rotating the needle to point in the right direction_. Be sure to set the origin accordingly if you are going to use your own model.

#### **needleGlowLevel**: `/attributes/XZTrackerProps/needleGlowLevel`
\[0-255\] default: 0

Because the needle model is rendered separately, it's glow must be set separately from the shell.

#### **maximumMeshes**: `/attributes/XZTrackerProps/maximumMeshes`
default: 120

For inventory, first/third person, and dropped item rendering, a collection of meshes are pre-generated. To 'animate' the needle movement, the closest-matching pre-generated mesh is swapped in each frame. The value of this property determines how many meshes are generated.

#### **distanceMethod**: `/attributes/XZTrackerProps/distanceMethod`
\["manhattan"|"distancesquared"\] default: "manhattan"

The method used to calculate a compass's distance from its target.

#### **minTrackingDistance**: `/attributes/XZTrackerProps/minTrackingDistance`
default: 3

Used with `distanceMethod` to determine when a compass is too close to its target to point in the proper direction.

</details>

## Collaboration?

I am no artist. If you would like to help improve the look of anything, feel free to contact me on the Vintage Story Discord (JapanHasRice) or submit a pull request on the github repo.
