using System;

namespace NeverSerender.UserInterface.Elements.Attributes
{
    public class ConditionalAttribute : Attribute
    {
        public ConditionalAttribute(string fieldName, bool hide = false)
        {
            FieldName = fieldName;
            Hide = hide;
        }
        
        public string FieldName { get; }
        
        public bool Hide { get; }
    }
}