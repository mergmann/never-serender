using System;
using System.Collections.Generic;
using System.Reflection;

namespace NeverSerender.UserInterface.Elements.Attributes
{
    internal class DropdownAttribute : Attribute, IElementAttribute
    {
        private readonly string description;
        private readonly string label;
        private readonly int visibleRows;

        public DropdownAttribute(string label = null, string description = null, int visibleRows = 20)
        {
            this.label = label;
            this.visibleRows = visibleRows;
            this.description = description;
        }

        public IElement<T> GetElement<T>()
        {
            var type = typeof(T);
            if (!type.IsEnum)
                throw new NotSupportedException(typeof(T).ToString());
            var method = GetType()
                .GetMethod(nameof(GetElementHelper), BindingFlags.NonPublic | BindingFlags.Instance)?
                .MakeGenericMethod(type);
            // ReSharper disable once PossibleNullReferenceException
            return (IElement<T>)method.Invoke(this, new object[] { });
        }

        public List<Type> SupportedTypes { get; } = new List<Type> { typeof(Enum) };

        private IElement<T> GetElementHelper<T>() where T : struct, Enum
        {
            return new DropdownElement<T>
            {
                Description = description,
                Label = label,
                VisibleRows = visibleRows
            };
        }
    }
}