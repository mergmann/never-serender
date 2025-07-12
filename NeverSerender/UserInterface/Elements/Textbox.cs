using System;
using System.Collections.Generic;
using NeverSerender.UserInterface.Tools;
using Sandbox.Graphics.GUI;

namespace NeverSerender.UserInterface.Elements
{
    internal class TextboxElement : IElement<string>
    {
        public List<Control> GetControls(string name, ElementProperty<string> property, bool enabled)
        {
            var textBox = new MyGuiControlTextbox(defaultText: property.Get())
            {
                Enabled = enabled
            };
            textBox.TextChanged += box => property.Set(box.Text);
            textBox.SetToolTip(Description);
            property.Changed += () => textBox.Text = property.Get();

            var label = UiTools.GetLabelOrDefault(name, Label);
            return new List<Control>
            {
                new Control(new MyGuiControlLabel(text: label), minWidth: Control.LabelMinWidth),
                new Control(textBox, fillFactor: 1f)
            };
        }


        public string Description { get; set; }
        public string Label { get; set; }
    }
}