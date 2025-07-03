using System;
using System.Collections.Generic;

namespace NeverSerender.UserInterface.Elements.Attributes
{
    internal class CheckboxAttribute : Attribute, IElementAttribute
    {
        private readonly string description;
        private readonly string label;

        public CheckboxAttribute(string label = null, string description = null)
        {
            this.label = label;
            this.description = description;
        }

        public IElement<T> GetElement<T>() => (IElement<T>)new CheckboxElement
        {
            Label = label,
            Description = description
        };

        public List<Type> SupportedTypes { get; } = new List<Type> { typeof(bool) };
    }
}