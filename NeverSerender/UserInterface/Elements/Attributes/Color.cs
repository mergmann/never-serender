using System;
using System.Collections.Generic;
using VRageMath;

namespace NeverSerender.UserInterface.Elements.Attributes
{
    internal class ColorAttribute : Attribute, IElementAttribute
    {
        private readonly string description;
        private readonly bool hasAlpha;
        private readonly string label;

        public ColorAttribute(bool hasAlpha = false, string label = null, string description = null)
        {
            this.hasAlpha = hasAlpha;
            this.label = label;
            this.description = description;
        }

        public IElement<T> GetElement<T>()
        {
            return (IElement<T>)new ColorElement
            {
                Description = description,
                Label = label,
                HasAlpha = hasAlpha
            };
        }

        public List<Type> SupportedTypes { get; } = new List<Type> { typeof(Color) };
    }
}