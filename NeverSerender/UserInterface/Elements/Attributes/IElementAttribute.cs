using System;
using System.Collections.Generic;

namespace NeverSerender.UserInterface.Elements.Attributes
{
    public interface IElementAttribute
    {
        IElement<T> GetElement<T>();
        
        List<Type> SupportedTypes { get; }
    }
}