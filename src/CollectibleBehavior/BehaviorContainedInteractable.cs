using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Compass {
  public class Instruction {
    public string CollectibleType = "item";
    public int ConsumeQuantity = 1;
    public string ConvertsToLocation = "";
    public string ConvertsToType = "item";
    public float durationSeconds = 0.5f;
  }

  public class CollectibleBehaviorContainedInteractable : CollectibleBehavior {
    protected Dictionary<string, Instruction> instructions = new Dictionary<string, Instruction>();
    protected Instruction currentInstruction;
    protected bool WasSuccessfulInteraction = false;
    public bool IsInteracting = false;

    public CollectibleBehaviorContainedInteractable(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties) {
      base.Initialize(properties);
      JObject jsonObj = (JObject)(object)((properties?.Token is JObject) ? properties.Token : null);
      foreach (var definition in jsonObj) {
        instructions.Add(definition.Key, properties[definition.Key].AsObject<Instruction>());
      }
    }

    public virtual bool OnContainedInteractStart(BlockEntityContainer container, ItemSlot inSlot, IPlayer byPlayer, BlockSelection blockSelection) {
      IsInteracting = false;
      var handItem = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
      var itemKey = handItem?.Collectible.Code.ToString() ?? "oneemptyhand";
      container.Api.Logger.Debug("[CompassMod] interact with {0} using {1}", inSlot.Itemstack?.Collectible?.Code, handItem?.Collectible?.Code);
      if (instructions.TryGetValue(itemKey, out Instruction instruction)) {
        container.Api.Logger.Debug("[CompassMod] instructions found");
        currentInstruction = instruction;
        IsInteracting = true;
      }
      return IsInteracting;
    }

    public bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer container, ItemSlot inSlot, IPlayer byPlayer, BlockSelection blockSel) {
      bool keepInteracting = true;
      container.Api.Logger.Debug("[CompassMod] using for {0}", secondsUsed);
      if (secondsUsed >= currentInstruction.durationSeconds) {
        keepInteracting = false;
        WasSuccessfulInteraction = true;
      }
      return keepInteracting;
    }

    public void OnContainedInteractStop(float secondsUsed, BlockEntityContainer container, ItemSlot inSlot, IPlayer byPlayer, BlockSelection blockSel) {
      IsInteracting = false;
      if (!WasSuccessfulInteraction) { return; }
      container.Api.Logger.Debug("[CompassMod] done");
      CollectibleObject newCollectible;
      var newCollectibleLocation = new AssetLocation(currentInstruction.ConvertsToLocation);
      if (currentInstruction.ConvertsToType.Equals("block", StringComparison.InvariantCultureIgnoreCase)) {
        newCollectible = container.Api.World.GetBlock(newCollectibleLocation);
      }
      else {
        newCollectible = container.Api.World.GetItem(newCollectibleLocation);
      }
      if (newCollectible is null) {
        container.Api.Logger.Warning("[CompassMod] Could not locate collectible {0}.", newCollectibleLocation);
        return;
      }
      inSlot.Itemstack = new ItemStack(newCollectible);
      inSlot.Itemstack.ResolveBlockOrItem(container.Api.World);
      container.MarkDirty(true);
      byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(currentInstruction.ConsumeQuantity);
    }
  }
}
