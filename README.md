# Compass2

## Latest Update Changes
* Active temporal storms interfere with compasses, rendering them useless. (configurable)
* Approaching temporal storms interfere with compasses, rendering them useless. (Disabled by default, configurable)
* The Relative Compass can only be crafted and attuned to areas of low stability (as measured at sea level, configurable)

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
  * **\*\*NEW\*\*** Can only be crafted in low stability areas.
  * Crafted with 1 Origin Compass and 2 Temporal Gears.
* Seraph Compass
  * Points to the player who first picks it up (usually the one who crafted it).
  * Does not track an offline player.
  * No crafting recipe yet. Feel free to add your own!

### **\*\*NEW\*\*** Mechanics
* Active temporal storms interfere with compasses
* Approching temporal storms interfere with compasses

### Storage
* Compasses can be held in your offhand.
* Compasses can be placed on the ground with other ground-storables.
* Compasses can be placed on shelves.
* Compasses can be placed in display cases.

### Commands
* `/compass`
  * Requires "commandplayer" permissions.
  * Only function on the compass held in the active hotbar slot
  * `/compass show`
    * Displays the compass's internal data: creator and target position.
  * `/compass set target <X> <Y> <Z>`
    * Changes the compass's target position to the provided position.
    * Supports flexible pos inputs, i.e. `~10 ~0 ~-15` denotes a position relative to the player, `=512000 =120 =512000` denotes an absolute position, and `0 120 0` denotes a map coordinate.
  * `/compass set craftedBy [<player_name>|<player_uid>|<any_string>]`
    * Changes who the compass was created by (currently only matters for the Seraph Compass).
    * Can be a player's Name, a player's UID, or an unrelated string.
  * `/compass reset`
    * Causes the compass to act as though it had just been crafted.
  * `/compass remove target`
    * Deletes the compass's saved target position.
  * `/compass remove craftedBy`
    * Deletes the compass's saved creator.
  * `/compass for [<player_name>|<player_uid>] [show|set|reset]`
    * Performs the given command on behalf of the specified player, as if that player had run the command themselves.
    * Example: ServerAdmin runs the command `/compass for OtherPlayer set target 10000 120 10000`. If OtherPlayer is holding a compass in their active hotbar slot, its target will be set to 10000, 120, 10000.

### Configuration
The below configuration options are applied before Vintage Story's JSON patching system. Changes made through JSON patching take priority over the changes made in the provided configuration file and should be safe from conflicts.

If a config file is not found in the ModConfig folder, it will be created with default values when the world is loaded.

If an individual setting is deleted from a config file, it will be added back in with its default value next time a world is loaded.

<details> <summary>Server Configuration</summary>

| Setting | Default | Min | Max | Description |
|-|:-:|:-:|:-:|-|
| -_Crafting Options_- |-|-|-|-|
| EnableMagneticRecipe | true ||| Allow crafting a Magnetic Compass with a Magnetite Nugget. |
| EnableScrapRecipe | true ||| Allow crafting a Magnetic Compass with a Metal Scraps. |
| EnableOriginRecipe | true ||| Allow crafting an Origin Compass. |
| OriginCompassGears | 2 | 1 | 8 | Number of Temporal Gears required to craft an Origin Compass. |
| EnableRelativeRecipe | true ||| Allow crafting a Relative Compass |
| RelativeCompassGears | 2 | 1 | 8 | Number of Temporal Gears required to craft a Relative Compass. |
| **\*\*NEW\*\*** RestrictRelativeCompassCraftingByStability | true ||| Prevent crafting a Relative Compass based on temporal stability. Must be enabled for `AllowRelativeCompassCraftingBelowStability` to have any effect. |
| **\*\*NEW\*\*** AllowRelativeCompassCraftingBelowStability | 0.9 | 0.1 || Temporal stability at or above this value (as measured at sea level) will prevent the crafting of a Relative Compass. NOTES: Vanilla stability values range from 0 to 1.5 (2 if temporal stability is disabled). Stability values below 1 cause a reduction in player stability. |
| -_Gameplay Options_- |-|-|-|-|
| **\*\*NEW\*\*** ActiveTemporalStormsAffectCompasses | true ||| During active temporal storms, compasses will be distorted. |
| **\*\*NEW\*\*** ApproachingTemporalStormsAffectCompasses | false ||| When a temporal storm is approaching, compasses will be distorted. |
| **\*\*NEW\*\*** ApproachingTemporalStormInterferenceBeginsDays | 0.35 | 0.1 || Number of days before a storm that compasses will be affected by an approaching temporal storm. |
| -_Other_- |-|-|-|-|
| AllowCompassesInOffhand | true ||| Allow compasses to be placed in the offhand slot. |

</details>

<details> <summary>Client Configuration</summary>

| Setting | Default | Min | Max | Description |
|-|:-:|:-:|:-:|-|
| MaximumPreGeneratedMeshes | 120 | 8 || Maximum number of meshes to use for animating needle movement of held compasses. |
| ThirdPersonRenderUpdateTickIntervalMs | 1 | 1 || Milliseconds between updates to compasses rendered in another player's hand. Only updates on game ticks. |
| **\*\*NEW\*\*** PlacedCompassRenderUpdateTickIntervalMs | 500 | 1 || Milliseconds between updates to compasses which are placed as blocks or displayed inside another. Only affects compasses with moving targets and only updates on game ticks. |

</details>

### Modding the mod
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

#### **distanceMethod**: `/attributes/XZTrackerProps/distanceMethod`
\["manhattan"|"distancesquared"\] default: "manhattan"

The method used to calculate a compass's distance from its target.

#### **minTrackingDistance**: `/attributes/XZTrackerProps/minTrackingDistance`
default: 3

Used with `distanceMethod` to determine when a compass is too close to its target to point in the proper direction.

</details>

## Collaboration?

If you have any suggestions or requests or would like to contribute, especially for modeling and animation, feel free to contact me on the Vintage Story Discord (JapanHasRice) or submit a pull request on the github repo.
