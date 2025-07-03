using System;
using System.Collections.Generic;

namespace NeverSerender.UserInterface.Elements.Attributes
{
    internal class ButtonAttribute : Attribute, IElementAttribute
    {
        private readonly string description;
        private readonly string label;

        public ButtonAttribute(string label = null, string description = null)
        {
            this.label = label;
            this.description = description;
        }

        public IElement<T> GetElement<T>() => (IElement<T>)new ButtonElement
        {
            Label = label,
            Description = description
        };

        public List<Type> SupportedTypes { get; } = new List<Type> { typeof(Action) };
    }
}