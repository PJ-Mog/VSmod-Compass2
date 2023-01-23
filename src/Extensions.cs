using Vintagestory.API.Common;

namespace Compass {
  public class Logger {
    private static Logger Singleton;
    public static Logger GetLogger(ILogger gameLogger) {
      Singleton = Singleton ?? new Logger(gameLogger);
      return Singleton;
    }

    private ILogger GameLogger;
    private Logger(ILogger gameLogger) {
      GameLogger = gameLogger;
    }

    private static string PrefixWithModId(string format) {
      return $"[{CompassMod.ModId}] " + format;
    }
    public void Debug(string format, params object[] args) {
      GameLogger.Debug(PrefixWithModId(format), args);
    }
    public void Error(string format, params object[] args) {
      GameLogger.Error(PrefixWithModId(format), args);
    }
    public void Notification(string format, params object[] args) {
      GameLogger.Notification(PrefixWithModId(format), args);
    }
    public void Warning(string format, params object[] args) {
      GameLogger.Warning(PrefixWithModId(format), args);
    }
    public void VerboseDebug(string format, params object[] args) {
      GameLogger.VerboseDebug(PrefixWithModId(format), args);
    }
  }
  public static class UtilityExtensions {
    public static Logger ModLogger(this ICoreAPI api) {
      return Logger.GetLogger(api.Logger);
    }
  }
}
