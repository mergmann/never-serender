using NeverSerender.UserInterface.Elements.Attributes;
using NeverSerender.UserInterface.Tools;
using VRage.Input;

namespace NeverSerender.Config
{
    public class GlobalConfig : AbstractConfig
    {
        public static readonly GlobalConfig Default = new GlobalConfig();
        public static readonly GlobalConfig Current = ConfigStorage.Load();
        
        private string outPath = "SpaceExport/";
        private string logPath = "SpaceExport/export.log";
        private KeyBind exportBind = new KeyBind(MyKeys.None);

        [Textbox(label: "Output path", description: "Folder where models are exported to")]
        public string OutPath
        {
            get => outPath;
            set => SetField(ref outPath, value);
        }

        [Textbox(label: "Log path", description: "File where the export log is saved")]
        public string LogPath
        {
            get => logPath;
            set => SetField(ref logPath, value);
        }

        [Keybind(label: "Run Export", description: "Keybind used to show the export dialog")]
        public KeyBind ExportBind
        {
            get => exportBind;
            set => SetField(ref exportBind, value);
        }
    }
}