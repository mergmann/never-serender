using System;
using System.Collections.Generic;
using System.Text;
using NeverSerender.UserInterface.Tools;
using Sandbox.Graphics.GUI;
using VRage.Utils;

namespace NeverSerender.UserInterface.Elements
{
    internal class ButtonElement : IElement<Action>
    {
        public List<Control> GetControls(string name, ElementProperty<Action> property, bool enabled)
        {
            var label = UiTools.GetLabelOrDefault(name, Label);
            var button = new MyGuiControlButton(text: new StringBuilder(label), toolTip: Description);
            button.ButtonClicked += _ => property.Get()();
            button.Enabled = enabled;

            return new List<Control>
            {
                new Control(new MyGuiControlLabel(text: label), Control.LabelMinWidth),
                new Control(button, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP)
            };
        }
        
        public string Description { get; set; }
        public string Label { get; set; }
    }
}