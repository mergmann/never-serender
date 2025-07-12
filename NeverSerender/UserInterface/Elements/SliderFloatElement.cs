using System;
using System.Collections.Generic;
using System.Reflection;
using NeverSerender.UserInterface.Tools;
using Sandbox;
using Sandbox.Graphics.GUI;
using VRage.Utils;

namespace NeverSerender.UserInterface.Elements
{
    internal class SliderFloatElement : IElement<float>
    {
        public string Description { get; set; }
        public string Label { get; set; }
        public float Max { get; set; }
        public float Min { get; set; }
        public float Step { get; set; }

        public List<Control> GetControls(string name, ElementProperty<float> property, bool enabled)
        {
            var valueLabel = new MyGuiControlLabel();

            var slider = new MyGuiControlSlider(toolTip: Description, defaultValue: property.Get(), minValue: Min,
                maxValue: Max)
            {
                MinimumStepOverride = Step,
                LabelDecimalPlaces = (int)Math.Max(1, Math.Ceiling(-Math.Log10(2f * Step))),
                Enabled = enabled
            };

            slider.ValueChanged += ValueUpdate;
            slider.SliderSetValueManual = SpecifyValue;

            ValueUpdate(slider);

            property.Changed += () =>
            {
                slider.Value = property.Get();
                ValueUpdate(slider);
            };

            var label = UiTools.GetLabelOrDefault(name, Label);
            return new List<Control>
            {
                new Control(new MyGuiControlLabel(text: label), minWidth: Control.LabelMinWidth),
                new Control(slider, fillFactor: 1f, rightMargin: 0.005f),
                new Control(valueLabel, minWidth: 0.06f)
            };

            void ValueUpdate(MyGuiControlSlider element)
            {
                property.Set(element.Value);
                valueLabel.Text = MyValueFormatter.GetFormatedFloat(element.Value, element.LabelDecimalPlaces);
            }

            bool SpecifyValue(MyGuiControlSlider element)
            {
                var screen = new MyGuiScreenDialogAmount(
                    Min,
                    Max,
                    MyCommonTexts.DialogAmount_SetValueCaption,
                    defaultAmount: Convert.ToSingle(property.Get()),
                    backgroundTransition: MySandboxGame.Config.UIBkOpacity,
                    guiTransition: MySandboxGame.Config.UIOpacity);

                screen.OnConfirmed += value => element.Value = value;

                // Much needed visual change requires reflection due to private types
                typeof(MyGuiScreenBase)
                    .GetField("m_canHideOthers", BindingFlags.NonPublic | BindingFlags.Instance)?
                    .SetValue(screen, true);

                MyGuiSandbox.AddScreen(screen);
                return true;
            }
        }
    }
}