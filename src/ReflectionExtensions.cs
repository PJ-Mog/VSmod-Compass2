using System;
using System.Reflection;

namespace ModIntegrity {
  public static class ReflectionExtensions {
    public static T XXX_GetFieldValue<T>(this object obj, string name) {
      var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
      var field = obj.GetType().GetField(name, bindingFlags);
      return (T)field?.GetValue(obj);
    }
    // e.g. .XXX_GetMethod("foo", new Type[] { typeof(int), typeof(byte[]) }) // finds `void foo(inf, byte[])`
    public static MethodInfo XXX_GetMethod(this object obj, string name, Type[] parameterTypes = null) {
      var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
      var method = obj.GetType().GetMethod(name, bindingFlags, null, CallingConventions.Any, parameterTypes, null);
      return method;
    }
  }
}