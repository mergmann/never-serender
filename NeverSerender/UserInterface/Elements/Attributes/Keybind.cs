using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NeverSerender.UserInterface.Tools;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Game;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

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

        public IElement<T> GetElement<T>() => (IElement<T>)new KeybindElement
        {
            Description = description,
            Label = label,
        };

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