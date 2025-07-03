using System;
using System.IO;
using System.Xml.Serialization;
using NeverSerender;
using VRage.FileSystem;
using VRage.Utils;

namespace NeverSerender.Config
{
    public static class ConfigStorage
    {
        private static readonly string ConfigFileName = string.Concat(Plugin.Name, ".cfg");
        private static string ConfigFilePath => Path.Combine(MyFileSystem.UserDataPath, "Storage", ConfigFileName);

        public static void Save(GlobalConfig globalConfig)
        {
            var path = ConfigFilePath;
            using (var text = File.CreateText(path))
            {
                new XmlSerializer(typeof(GlobalConfig)).Serialize(text, globalConfig);
            }
        }

        public static GlobalConfig Load()
        {
            var path = ConfigFilePath;
            if (!File.Exists(path)) return GlobalConfig.Default;

            var xmlSerializer = new XmlSerializer(typeof(GlobalConfig));
            try
            {
                using (var streamReader = File.OpenText(path))
                {
                    return (GlobalConfig)xmlSerializer.Deserialize(streamReader) ?? GlobalConfig.Default;
                }
            }
            catch (Exception)
            {
                MyLog.Default.Warning($"{ConfigFileName}: Failed to read config file: {ConfigFilePath}");
            }

            return GlobalConfig.Default;
        }
    }
}