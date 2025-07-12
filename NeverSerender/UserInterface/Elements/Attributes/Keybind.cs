using System;
using System.Collections.Generic;
using NeverSerender.UserInterface.Tools;
using VRage.Input;

namespace NeverSerender.UserInterface.Elements.Attributes
{
    internal class KeybindAttribute : Attribute, IElementAttribute
    {
        private readonly string description;
        private readonly string label;

        public KeybindAttribute(string label = null, string description = null)
        {
            this.label = label;
            this.description = description;
        }

        public IElement<T> GetElement<T>()
        {
            return (IElement<T>)new KeybindElement
            {
                Description = description,
                Label = label
            };
        }

        public List<Type> SupportedTypes => new List<Type> { typeof(KeyBind) };

        private class ControlButtonData
        {
            public readonly MyControl Control;
            public readonly MyGuiInputDeviceEnum Device;

            public ControlButtonData(MyControl control, MyGuiInputDeviceEnum device)
            {
                Control = control;
                Device = device;
            }
        }
    }
}