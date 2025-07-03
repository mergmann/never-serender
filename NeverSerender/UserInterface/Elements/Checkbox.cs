using System;
using System.Collections.Generic;
using NeverSerender.UserInterface.Tools;
using Sandbox.Graphics.GUI;

namespace NeverSerender.UserInterface.Elements
{
    internal class CheckboxElement : IElement<bool>
    {
        public List<Control> GetControls(string name, ElementProperty<bool> property)
        {
            var label = UiTools.GetLabelOrDefault(name, Label);

            var checkbox = new MyGuiControlCheckbox(toolTip: Description)
            {
                IsChecked = property.Get(),
                IsCheckedChanged = x => property.Set(x.IsChecked)
            };
            property.Changed += () => checkbox.IsChecked = property.Get();
            
            return new List<Control>
            {
                new Control(new MyGuiControlLabel(text: label), Control.LabelMinWidth),
                new Control(checkbox)
            };
        }
        
        public string Description { get; set; }
        public string Label { get; set; }
    }
}