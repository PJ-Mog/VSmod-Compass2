using System;
using System.Reflection;
using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Compass.ConfigSystem {
  public abstract class Config {
    public static T LoadOrCreateDefault<T>(ICoreAPI api, string filename) where T : Config, new() {
      T config = TryLoadModConfig<T>(api, filename);

      if (config == null) {
        api.ModLogger().Notification("Unable to load valid config file. Generating {0} with defaults.", filename);
        config = new T();
      }
      else {
        config.Clamp(api.ModLogger());
      }
      config.Save(api, filename);
      return config;
    }

    // Throws exception if the config file exists, but had parsing errors.
    // Returns null if no config file exists.
    public static T TryLoadModConfig<T>(ICoreAPI api, string filename) where T : Config {
      T config = default(T);
      try {
        config = api.LoadModConfig<T>(filename);
      }
      catch (JsonReaderException e) {
        api.ModLogger().Error("Unable to parse configuration file, {0}. Correct syntax errors and retry, or delete.", filename);
        throw e;
      }
      catch (Exception e) {
        api.ModLogger().Error("I don't know what happened. Delete {0} in the mod config folder and try again.", filename);
        throw e;
      }

      return config;
    }

    public void Clamp(Logger logger) {
      var privateFields = GetType().GetTypeInfo().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
      foreach (var restrictingField in privateFields) {
        if (restrictingField.Name.Length < 4) { continue; }
        var baseFieldName = restrictingField.Name.Substring(0, restrictingField.Name.Length - 3);
        var baseField = GetType().GetTypeInfo().GetField(baseFieldName, BindingFlags.Public | BindingFlags.Instance);
        if (baseField == null) {
          logger.Debug("Could not find public field \"{0}\" to clamp with \"{1}\".", baseFieldName, restrictingField.Name);
          continue;
        }
        var lastThree = restrictingField.Name.Substring(restrictingField.Name.Length - 3);
        switch (lastThree) {
          case "Min":
            var min = restrictingField.GetValue(this);
            try {
              if (min == null) { continue; }
              Type[] types = { restrictingField.FieldType, baseField.FieldType };
              var oldValue = baseField.GetValue(this);
              var newValue = typeof(Math).GetTypeInfo().GetMethod("Max", types)?.Invoke(null, new object[] { min, oldValue });
              if (newValue == null) {
                logger.Error("Error while applying the minimum value '{0}' ({1}) for '{2}' ({3})", min, min.GetType(), baseField.Name, baseField.FieldType);
                continue;
              }
              baseField.SetValue(this, newValue);
              if (newValue.ToString() != oldValue.ToString()) {
                logger.Warning("Value for \"{0}\" was out of bounds ({1}). Using '{2}'.", baseField.Name, oldValue, newValue);
              }
            }
            catch (System.Exception e) {
              logger.Error("Error while applying the minimum value '{0}' ({1}) for '{2}' ({3})", min, min.GetType(), baseField.Name, baseField.FieldType);
              logger.Error("{0}", e);
            }
            break;
          case "Max":
            var max = restrictingField.GetValue(this); ;
            try {
              if (max == null) { continue; }
              Type[] types = { restrictingField.FieldType, baseField.FieldType };
              var oldValue = baseField.GetValue(this);
              var newValue = typeof(Math).GetTypeInfo().GetMethod("Min", types)?.Invoke(null, new object[] { max, oldValue });
              if (newValue == null) {
                logger.Error("Error while applying the maximum value '{0}' ({1}) for '{2}' ({3})", max, max.GetType(), baseField.Name, baseField.FieldType);
                continue;
              }
              baseField.SetValue(this, newValue);
              if (newValue.ToString() != oldValue.ToString()) {
                logger.Warning("Value for \"{0}\" was out of bounds ({1}). Using '{2}'.", baseField.Name, oldValue, newValue);
              }
            }
            catch (System.Exception e) {
              logger.Error("Error while applying the maximum value '{0}' ({1}) for '{2}' ({3})", max, max.GetType(), baseField.Name, baseField.FieldType);
              logger.Error("{0}", e);
            }
            break;
          case "Dft":
            break;
        }
      }
    }

    public void Save(ICoreAPI api, string filename) {
      api.StoreModConfig(this, filename);
    }
  }
}
