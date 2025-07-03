using System;
using System.Collections.Generic;
using System.Reflection;
using NeverSerender.UserInterface.Tools;
using Sandbox;
using Sandbox.Graphics.GUI;
using VRage.Utils;

namespace NeverSerender.UserInterface.Elements.Attributes
{
    internal class SliderAttribute : Attribute, IElementAttribute
    {
        private readonly string description;
        private readonly string label;
        private readonly float max;
        private readonly float min;
        private readonly float step;

        public SliderAttribute(float min, float max, float step = 1f, string label = null, string description = null)
        {
            this.min = min;
            this.max = max;
            this.step = step;
            this.label = label;
            this.description = description;
        }

        public IElement<T> GetElement<T>()
        {
            var type = typeof(T);
            if (typeof(int).IsAssignableFrom(type))
                return (IElement<T>)new SliderIntElement
                {
                    Min = (int)min,
                    Max = (int)max,
                    Step = (int)step,
                    Label = label,
                    Description = description
                };
            if (typeof(float).IsAssignableFrom(type))
                return (IElement<T>)new SliderFloatElement
                {
                    Min = min,
                    Max = max,
                    Step = step,
                    Label = label,
                    Description = description
                };

            throw new NotSupportedException(type.ToString());
        }

        public List<Type> SupportedTypes { get; } = new List<Type>
        {
            typeof(float),
            typeof(int)
        };
    }
}