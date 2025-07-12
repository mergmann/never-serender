using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NeverSerender.UserInterface.Tools;
using Sandbox.Graphics.GUI;

namespace NeverSerender.UserInterface.Elements
{
    internal class DropdownElement<T> : IElement<T> where T: struct, Enum
    {
        public List<Control> GetControls(string name, ElementProperty<T> property, bool enabled)
        {
            var selectedEnum = property.Get();

            var dropdown = new MyGuiControlCombobox(toolTip: Description);
            var elements = Enum.GetNames(typeof(T));

            for (var i = 0; i < elements.Length; i++) dropdown.AddItem(i, UnCamelCase(elements[i]));

            dropdown.ItemSelected += OnItemSelect;
            dropdown.SelectItemByIndex(Convert.ToInt32(selectedEnum));
            
            dropdown.Enabled = enabled;
            
            property.Changed += () => dropdown.SelectItemByIndex(Convert.ToInt32(property.Get()));

            var label = UiTools.GetLabelOrDefault(name, Label);
            return new List<Control>
            {
                new Control(new MyGuiControlLabel(text: label), minWidth: Control.LabelMinWidth),
                new Control(dropdown, fillFactor: 1f)
            };

            void OnItemSelect()
            {
                var key = dropdown.GetSelectedKey();
                var value = elements[key];

                if (!Enum.TryParse<T>(value, out var enumValue))
                    throw new Exception($"{value} is not valid member of enumeration MyEnum");
                property.Set(enumValue);
            }
        }

        private static string UnCamelCase(string str)
        {
            return Regex.Replace(
                Regex.Replace(
                    str,
                    @"(\P{Ll})(\P{Ll}\p{Ll})",
                    "$1 $2"
                ),
                @"(\p{Ll})(\P{Ll})",
                "$1 $2"
            );
        }

        public string Description { get; set; }
        public string Label { get; set; }
        public int VisibleRows { get; set; }
    }
}