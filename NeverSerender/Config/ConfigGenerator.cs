using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NeverSerender.UserInterface;
using NeverSerender.UserInterface.Elements;
using NeverSerender.UserInterface.Elements.Attributes;
using NeverSerender.UserInterface.Layouts;
using Sandbox.Graphics.GUI;
using VRage.Utils;

namespace NeverSerender.Config
{
    internal class ConfigGenerator
    {
        private List<List<Control>> controls;

        /// <summary>
        ///     This flag is used to prevent UI actions from sending changes to itself
        /// </summary>
        private bool disableNotify;

        private ElementList elements;

        public ConfigGenerator(IConfig config)
        {
            ExtractAttributes(config);
            config.PropertyChanged += (sender, args) =>
            {
                if (!disableNotify)
                    elements.Notify(args.PropertyName);
            };
            ActiveLayout = new None(() => controls);
        }

        public Layout ActiveLayout { get; private set; }

        private static bool ValidateType(Type type, List<Type> typesList)
        {
            return typesList.Any(t => t.IsAssignableFrom(type));
        }

        public bool Update()
        {
            return elements.DetectVisibilityChanges();
        }

        public List<MyGuiControlBase> RecreateControls()
        {
            CreateConfigControls();
            var list = ActiveLayout.RecreateControls();
            ActiveLayout.LayoutControls();
            return list;
        }

        public void SetLayout<T>() where T : Layout
        {
            ActiveLayout = (T)Activator.CreateInstance(typeof(T), (Func<List<List<Control>>>)(() => controls));
        }

        public void RefreshLayout()
        {
            ActiveLayout.LayoutControls();
        }

        private void CreateConfigControls()
        {
            controls = elements.GetControls();
        }

        private static Delegate GetDelegate(MethodInfo methodInfo)
        {
            // Reconstruct the type
            var methodArgs = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            var type = Expression.GetDelegateType(methodArgs.Concat(new[] { methodInfo.ReturnType }).ToArray());

            // Create a delegate
            return Delegate.CreateDelegate(type, null, methodInfo);
        }

        private void AddHelper<T>(string name, IElementAttribute attribute, IConfig config, PropertyInfo property,
            PropertyInfo condition, bool hide)
        {
            MyLog.Default.WriteLine($"{name}: {condition?.Name}");
            var visibility = hide ? Visibility.Hidden : Visibility.Disabled;
            elements.Add(name, attribute.GetElement<T>(), new ElementProperty<T>(Getter, Setter), GetVisible);
            return;

            Visibility GetVisible()
            {
                return (bool)(condition?.GetValue(config) ?? true) ? Visibility.Shown : visibility;
            }

            T Getter()
            {
                return (T)property.GetValue(config);
            }

            void Setter(T value)
            {
                try
                {
                    disableNotify = true;
                    property.SetValue(config, value);
                }
                finally
                {
                    disableNotify = false;
                }
            }
        }

        private void ExtractAttributes(IConfig config)
        {
            elements = new ElementList();
            var type = config.GetType();

            foreach (var property in type.GetProperties())
            {
                var name = property.Name;

                var conditional = property.GetCustomAttribute<ConditionalAttribute>();
                if (conditional != null)
                    MyLog.Default.WriteLine(
                        $"Conditional {name}: {conditional.FieldName}, {type.GetProperty(conditional.FieldName)}");
                var condition = conditional == null
                    ? null
                    : type.GetProperty(conditional.FieldName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var attribute in property.GetCustomAttributes())
                {
                    if (!(attribute is IElementAttribute element)) continue;
                    if (!ValidateType(property.PropertyType, element.SupportedTypes))
                        throw new Exception(
                            $"Element {element.GetType().Name} for {name} expects "
                            + $"{string.Join("/", element.SupportedTypes)} but "
                            + $"received {property.PropertyType.FullName}");

                    // Resharper disable once PossibleNullReferenceException
                    typeof(ConfigGenerator)
                        .GetMethod(nameof(AddHelper), BindingFlags.Instance | BindingFlags.NonPublic)
                        .MakeGenericMethod(property.PropertyType)
                        .Invoke(this,
                            new object[] { name, attribute, config, property, condition, conditional?.Hide ?? true });
                }
            }

            foreach (var methodInfo in config.GetType().GetMethods())
            {
                var name = methodInfo.Name;
                var method = GetDelegate(methodInfo);

                var conditional = methodInfo.GetCustomAttribute<ConditionalAttribute>();
                var condition = conditional == null
                    ? null
                    : type.GetProperty(conditional.FieldName,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var visibility = conditional?.Hide ?? true ? Visibility.Hidden : Visibility.Disabled;

                foreach (var attribute in methodInfo.GetCustomAttributes())
                {
                    if (attribute is IElementAttribute element)
                    {
                        if (!ValidateType(typeof(Action), element.SupportedTypes))
                            throw new Exception(
                                $"Element {element.GetType().Name} for {name} expects "
                                + $"{string.Join("/", element.SupportedTypes)} but "
                                + $"received {typeof(Delegate).FullName}");

                        elements.Add(name, element.GetElement<Action>(),
                            new ElementProperty<Action>(() => (Action)method, null), GetVisible);
                    }

                    continue;

                    Visibility GetVisible()
                    {
                        return (bool)(condition?.GetValue(config) ?? true) ? Visibility.Shown : visibility;
                    }
                }
            }
        }
    }
}