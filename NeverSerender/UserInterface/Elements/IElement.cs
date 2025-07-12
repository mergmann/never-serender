using System;
using System.Collections.Generic;

namespace NeverSerender.UserInterface.Elements
{
    public interface IElement { }
    
    public interface IElement<T> : IElement
    {
        List<Control> GetControls(string name, ElementProperty<T> property, bool enabled);
    }
}