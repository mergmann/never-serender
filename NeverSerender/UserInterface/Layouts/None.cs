using System;
using System.Collections.Generic;
using System.Linq;
using NeverSerender.UserInterface.Elements;
using Sandbox.Graphics.GUI;
using VRageMath;

namespace NeverSerender.UserInterface.Layouts
{
    internal class None : Layout
    {
        public None(Func<List<List<Control>>> getControls) : base(getControls)
        {
        }

        public override Vector2 PanelSize => new Vector2(0.5f, 0.5f);

        public override List<MyGuiControlBase> RecreateControls()
        {
            return GetControls().SelectMany(x => x.Select(c => c.GuiControl)).ToList();
        }

        public override void LayoutControls()
        {
            foreach (var group in GetControls())
            foreach (var control in group)
                control.GuiControl.Position = Vector2.Zero;
        }
    }
}