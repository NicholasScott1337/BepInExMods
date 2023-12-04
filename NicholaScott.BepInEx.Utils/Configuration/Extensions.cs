using System;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;

namespace NicholaScott.BepInEx.Utils.Configuration
{
    /// <summary>
    /// Defines more config specifiers for struct fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ConfigEntryDefinition : Attribute
    {
        /// <summary>
        /// The directory to where this entry belongs in the config file.
        /// </summary>
        public string Category;
        /// <summary>
        /// Alternate config name instead of the existing name.
        /// </summary>
        public string Name;
        /// <summary>
        /// Description to be provided inside the config.
        /// </summary>
        public string Description;
    }
    /// <summary>
    /// Contains all extension methods for configuration related features.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Tries to interpret the provided struct as a source for config files.
        /// </summary>
        /// <param name="config">The config file to bind values from.</param>
        /// <param name="defaults">The default values for the struct.</param>
        /// <typeparam name="TStruct">The struct to bind values from and to.</typeparam>
        /// <returns>A form of TStruct that represents the config file.</returns>
        public static TStruct BindStruct<TStruct>(this ConfigFile config, TStruct defaults) where TStruct : struct
        {
            object result = new TStruct();
            foreach (var field in typeof(TStruct).GetFields())
            {
                var entryDefinition =
                    field.GetCustomAttribute<ConfigEntryDefinition>() ?? new ConfigEntryDefinition();
                entryDefinition.Category = entryDefinition.Category ?? "General";
                entryDefinition.Name = entryDefinition.Name ?? field.Name;
                entryDefinition.Description = entryDefinition.Description ?? "";
                
                var realMethod = typeof(ConfigFile).GetMethods()?.Where(m =>
                    m.IsGenericMethod && m.Name.Contains("Bind") &&
                    m.GetParameters()[0].ParameterType == typeof(ConfigDefinition)).First();
                var generic = realMethod?.MakeGenericMethod(field.FieldType);
                var configEntry = generic?.Invoke(config, new object[]
                {
                    new ConfigDefinition(entryDefinition.Category, entryDefinition.Name), 
                    field.GetValue(defaults), new ConfigDescription(entryDefinition.Description)
                });
                var configEntryProperty = configEntry?.GetType().GetProperty("Value");
                var valueResult = configEntryProperty?.GetGetMethod().Invoke(configEntry, null);

                field.SetValue(result, valueResult ?? field.GetValue(defaults));
            }
            return (TStruct)result;
        }
    }
}