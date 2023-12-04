using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace NicholaScott.BepInEx.Utils.Patching
{
    [AttributeUsage(AttributeTargets.Class)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Production : Attribute {}
    /// <summary>
    /// Contains all extension methods for patching related features.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Provides patching methods for any classes that have the designated Attribute
        /// </summary>
        /// <param name="sourceAssembly"></param>
        /// <param name="guid">A unique identifier for the harmony instance.</param>
        /// <param name="logMethod">An optional method to log information to.</param>
        /// <typeparam name="TAttr">The Attribute to hunt down.</typeparam>
        /// <returns></returns>
        public static Harmony PatchAttribute<TAttr>(this Assembly sourceAssembly, string guid,
            Action<object> logMethod = null) where TAttr : Attribute
        {
            var newHarmony = new Harmony(string.Join(".", guid, typeof(TAttr).Name));
            foreach (var type in sourceAssembly.GetTypes()
                         .Where(t => t.IsClass && t.GetCustomAttributes(typeof(TAttr)).Any()))
            {
                var prevCount = newHarmony.GetPatchedMethods().Count();
                newHarmony.PatchAll(type);
                var newCount = newHarmony.GetPatchedMethods().Count();
                var amount = newCount - prevCount;
                logMethod?.Invoke(
                    $"[{typeof(TAttr).Name}] Patched class {type.Name}, containing {amount} method{(amount > 1 ? "s." : ".")}");
            }
            return newHarmony;
        }
    }
}