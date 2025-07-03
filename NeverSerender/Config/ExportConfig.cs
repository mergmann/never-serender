using NeverSerender.UserInterface.Elements.Attributes;

namespace NeverSerender.Config
{
    public class ExportConfig : AbstractConfig
    {
        public static readonly ExportConfig Current = new ExportConfig();

        private string fileName = "model.spacemodel";
        private ExportScope scope = ExportScope.AllEntities;
        private ViewAnchor anchor = ViewAnchor.Origin;
        
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
        
        [Button("Export", "Export the current space model")]
        public void Export() => Plugin.Instance.StartExport();

        public enum ExportScope
        {
            SingleGrid,
            AllGrids,
            AllEntities,
        }
        
        public enum ViewAnchor
        {
            Origin,
            Player,
            Grid,
        }
    }
}