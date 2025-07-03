using System;
using System.Collections.Generic;
using NeverSerender.UserInterface.Tools;
using Sandbox.Graphics.GUI;

namespace NeverSerender.UserInterface.Elements.Attributes
{
    internal class TextboxAttribute : Attribute, IElementAttribute
    {
        private readonly string description;
        private readonly string label;

        public TextboxAttribute(string label = null, string description = null)
        {
            this.label = label;
            this.description = description;
        }

        public IElement<T> GetElement<T>() => (IElement<T>)new TextboxElement
        {
            Description = description,
            Label = label,
        };

        public List<Type> SupportedTypes { get; } = new List<Type> { typeof(string) };
    }
}