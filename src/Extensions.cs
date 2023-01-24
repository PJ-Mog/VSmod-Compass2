using Vintagestory.API.Common;

namespace Compass {
  public static class UtilityExtensions {
    private static string PrefixWithModId(string format) {
      return $"[{CompassMod.ModId}] " + format;
    }

    public static void ModDebug(this ILogger logger, string format, params object[] args) {
      logger.Debug(PrefixWithModId(format), args);
    }

    public static void ModError(this ILogger logger, string format, params object[] args) {
      logger.Error(PrefixWithModId(format), args);
    }

    public static void ModNotification(this ILogger logger, string format, params object[] args) {
      logger.Notification(PrefixWithModId(format), args);
    }

    public static void ModWarning(this ILogger logger, string format, params object[] args) {
      logger.Warning(PrefixWithModId(format), args);
    }

    public static void ModVerboseDebug(this ILogger logger, string format, params object[] args) {
      logger.VerboseDebug(PrefixWithModId(format), args);
    }
  }
}
