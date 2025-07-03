using System;
using System.Collections.Generic;
using System.Reflection;
using NeverSerender.UserInterface.Tools;
using Sandbox;
using Sandbox.Graphics.GUI;
using VRage.Utils;

namespace NeverSerender.UserInterface.Elements
{
    internal class SliderIntElement : IElement<int>
    {
        public List<Control> GetControls(string name, ElementProperty<int> property)
        {
            var valueLabel = new MyGuiControlLabel();

            var slider = new MyGuiControlSlider(
                toolTip: Description,
                defaultValue: Convert.ToSingle(property.Get()),
                minValue: Min,
                maxValue: Max,
                intValue: true)
            {
                MinimumStepOverride = Step
            };

            slider.ValueChanged += ValueUpdate;
            slider.SliderSetValueManual = SpecifyValue;

            ValueUpdate(slider);
            
            property.Changed += () =>
            {
                slider.Value = Convert.ToSingle(property.Get());
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
                var intValue = Convert.ToInt32(element.Value);
                property.Set(intValue);
                valueLabel.Text = intValue.ToString();
            }

            bool SpecifyValue(MyGuiControlSlider element)
            {
                var screen = new MyGuiScreenDialogAmount(
                    Min,
                    Max,
                    MyCommonTexts.DialogAmount_SetValueCaption,
                    defaultAmount: property.Get(),
                    parseAsInteger: true,
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
        
        public string Description { get; set; }
        public string Label { get; set; }
        public int Max { get; set; }
        public int Min { get; set; }
        public int Step { get; set; }
    }
}