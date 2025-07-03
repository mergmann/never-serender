using System;
using System.Collections.Generic;
using System.Reflection;
using NeverSerender.UserInterface.Tools;
using VRage.Utils;

namespace NeverSerender.UserInterface.Elements
{
    public class ElementList
    {
        private class ElementData<T>
        {
            public IElement<T> Element { get; set; }
            public ElementProperty<T> Property { get; set; }
        }

        private readonly Dictionary<string, object> elements = new Dictionary<string, object>();

        public void Add<T>(string name, IElement<T> element, ElementProperty<T> property) =>
            elements.Add(name, new ElementData<T> { Element = element, Property = property });

        private static ElementProperty GetPropertyHelper<T>(object data)
        {
            var casted = (ElementData<T>)data;
            return casted.Property;
        }

        private ElementProperty GetProperty(string name)
        {
            var value = elements[name];
            var elemType = value.GetType().GetGenericArguments()[0];
            // ReSharper disable once PossibleNullReferenceException
            var method = GetType().GetMethod(nameof(GetPropertyHelper), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(elemType);
            return (ElementProperty)method.Invoke(null, new object[] { value });
        }

        public void Notify(string name) => GetProperty(name).Notify();

        private static List<Control> GetControlsHelper<T>(string name, object data)
        {
            var casted = (ElementData<T>)data;
            return casted.Element.GetControls(name, casted.Property);
        }

        public List<List<Control>> GetControls()
        {
            var controls = new List<List<Control>>();
            foreach (var pair in elements)
            {
                MyLog.Default.WriteLine($"ElemenV {pair.Key}: {pair.Value}");
                var elemType = pair.Value.GetType().GetGenericArguments()[0];
                MyLog.Default.WriteLine($"Element {pair.Key}: {elemType?.Name}");
                // ReSharper disable once PossibleNullReferenceException
                var method = GetType()
                    .GetMethod(nameof(GetControlsHelper), BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(elemType);
                controls.Add((List<Control>)method.Invoke(null, new object[] { pair.Key, pair.Value }));
            }

            return controls;
        }
    }
}