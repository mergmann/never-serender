using NeverSerender.UserInterface.Elements.Attributes;

namespace NeverSerender.Config
{
    public class ExportConfig : AbstractConfig
    {
        public enum ExportScope
        {
            SingleGrid,
            AllGrids,
            AllEntities
        }

        public enum ViewAnchor
        {
            Origin,
            Player,
            Grid
        }

        public static readonly ExportConfig Current = new ExportConfig();
        private ViewAnchor anchor = ViewAnchor.Origin;
        private bool animation;

        private string fileName = "model.spacemodel";
        private ExportScope scope = ExportScope.AllEntities;

        [Textbox("File Name", "The name of the file to export the model to")]
        public string FileName
        {
            get => fileName;
            set => SetField(ref fileName, value);
        }

        [Dropdown("Export Scope", "What should be exported?")]
        public ExportScope Scope
        {
            get => scope;
            set => SetField(ref scope, value);
        }

        [Dropdown("Initial anchor", "What should be the center of the exported model?")]
        public ViewAnchor Anchor
        {
            get => anchor;
            set => SetField(ref anchor, value);
        }

        public bool ShowAnimation => GlobalConfig.Current.Experimental;

        [Conditional(nameof(ShowAnimation), true)]
        [Checkbox("Animation", "Record an animation until the export keybind is pressed again")]
        public bool Animation
        {
            get => animation;
            set => SetField(ref animation, value);
        }

        [Button("Export", "Export the current space model")]
        public void Export()
        {
            Plugin.Instance.StartExport();
        }
    }
}