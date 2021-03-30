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
      base.Start(api);
      config = ModConfig.Load(api);

      api.RegisterItemClass("compass-magnetic", typeof(CompassMagneticItem));
    }
  }
}
