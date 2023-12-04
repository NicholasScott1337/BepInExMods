using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;

namespace NicholaScott.BepInEx.Utils.Instancing
{
    public class Singleton<TPrepare>
    {
        public static TPrepare Instance;
        public static ManualLogSource Logger => typeof(BaseUnityPlugin).GetProperty("Logger", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetGetMethod(true)?.Invoke(Instance, Array.Empty<object>()) as ManualLogSource;
    }

    public class Singleton<TPrepare, TConfig> : Singleton<TPrepare>
    {
        public static TConfig Configuration;
    }
}