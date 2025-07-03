using System.Collections.Generic;
using NeverSerender.UserInterface.Elements;
using NeverSerender.UserInterface.Layouts;
using Sandbox.Graphics.GUI;
using VRageMath;

namespace NeverSerender.UserInterface
{
    public class ExportScreen : MyGuiScreenBase
    {
        private readonly Layout layout;

        public ExportScreen()
        {
            layout = new Simple(GetControls);
        }
        
        private List<List<Control>> GetControls()
        {
            return null;
        }
        
        public override string GetFriendlyName() => "Export Scene";
        
        public override void LoadContent()
        {
            base.LoadContent();
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            AddCaption(GetFriendlyName());
        }
    }
}