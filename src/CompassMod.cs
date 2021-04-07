using ProtoBuf;
using System;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using System.Linq;
using System.Collections.Generic;
using Vintagestory.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

[assembly: ModInfo("Compass")]

namespace Compass {
  public class CompassMod : ModSystem {
    private ModConfig config;
    public override void Start(ICoreAPI api) {
      api.Logger.Debug("[Compass] Start");
      base.Start(api);
      
      config = ModConfig.Load(api);

      api.RegisterItemClass("ItemMagneticCompass", typeof(ItemMagneticCompass));
      api.RegisterItemClass("ItemRelativeCompass", typeof(ItemRelativeCompass));
      api.RegisterItemClass("ItemOriginCompass", typeof(ItemOriginCompass));

      api.RegisterBlockClass("BlockMagneticCompass", typeof(BlockMagneticCompass));
      api.RegisterBlockClass("BlockRelativeCompass", typeof(BlockRelativeCompass));
      api.RegisterBlockClass("BlockOriginCompass", typeof(BlockOriginCompass));
    }
  }
}
