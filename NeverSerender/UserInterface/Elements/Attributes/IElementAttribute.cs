using System;
using System.Collections.Generic;

namespace NeverSerender.UserInterface.Elements.Attributes
{
    public interface IElementAttribute
    {
        List<Type> SupportedTypes { get; }
        IElement<T> GetElement<T>();
    }
}