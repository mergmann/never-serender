using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VRage.Utils;

namespace NeverSerender.UserInterface.Elements
{
    public class ElementList
    {
        private class ElementData
        {
            public IElement Element { get; set; }
            public ElementProperty Property { get; set; }
            public Visibility LastVisibility { get; set; }
            public Func<Visibility> GetVisibility { get; set; }
        }

        private readonly Dictionary<string, ElementData> elements = new Dictionary<string, ElementData>();

        public void Add<T>(string name, IElement<T> element, ElementProperty<T> property,
            Func<Visibility> visibility) =>
            elements.Add(name, new ElementData { Element = element, Property = property, GetVisibility = visibility });

        private ElementProperty GetProperty(string name) => elements[name].Property;
        public void Notify(string name) => GetProperty(name).Notify();

        public bool DetectVisibilityChanges() => elements.Values.Any(el => el.LastVisibility != el.GetVisibility());

        private static List<Control> GetControlsHelper<T>(string name, ElementData data, Visibility visibility)
        {
            var elem = (IElement<T>)data.Element;
            var prop = (ElementProperty<T>)data.Property;
            return elem.GetControls(name, prop, visibility == Visibility.Shown);
        }

        public List<List<Control>> GetControls()
        {
            var controls = new List<List<Control>>();

            MyLog.Default.WriteLine("Get Controls");
            foreach (var pair in elements)
            {
                var visibility = pair.Value.GetVisibility();
                pair.Value.LastVisibility = visibility;
                if (visibility == Visibility.Hidden)
                    continue;

                MyLog.Default.WriteLine("Get Control: " + pair.Value.Property.GetType().Name);
                var elemType = pair.Value.Property.GetType().GetGenericArguments()[0];
                // ReSharper disable once PossibleNullReferenceException
                var method = GetType()
                    .GetMethod(nameof(GetControlsHelper), BindingFlags.Static | BindingFlags.NonPublic)
                    .MakeGenericMethod(elemType);
                controls.Add((List<Control>)method.Invoke(null, new object[] { pair.Key, pair.Value, visibility }));
            }

            return controls;
        }
    }
}