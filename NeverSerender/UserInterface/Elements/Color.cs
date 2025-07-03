using System;
using System.Collections.Generic;
using NeverSerender.UserInterface.Tools;
using Sandbox.Graphics.GUI;
using VRage.Game;
using VRageMath;

namespace NeverSerender.UserInterface.Elements
{
    internal class ColorElement : IElement<Color>
    {
        private Color originalBorderColor;

        public List<Control> GetControls(string name, ElementProperty<Color> property)
        {
            var defaultColor = property.Get();
            var defaultColorHex = HasAlpha ? defaultColor.ToHexStringRgba() : defaultColor.ToHexStringRgb();

            var sample = new MyGuiControlButton(visualStyle: MyGuiControlButtonStyleEnum.SquareSmall)
            {
                CanHaveFocus = false,
                BorderColor = defaultColor,
                BorderEnabled = true,
                BorderSize = 20
            };

            var textBox = new MyGuiControlTextbox(defaultText: defaultColorHex, maxLength: HasAlpha ? 8 : 6)
            {
                Size = new Vector2(0.1f, 0.04f)
            };

            originalBorderColor = textBox.BorderColor;

            textBox.TextChanged += box =>
            {
                var success = HasAlpha
                    ? box.Text.TryParseColorFromHexRgba(out var color)
                    : box.Text.TryParseColorFromHexRgb(out color);
                if (!success)
                {
                    box.BorderColor = Color.Red;
                    box.BorderEnabled = true;
                    return;
                }

                box.BorderColor = originalBorderColor;
                box.BorderEnabled = false;

                sample.BorderColor = color;

                if (color != property.Get())
                    property.Set(color);

                var text = HasAlpha ? color.ToHexStringRgba() : color.ToHexStringRgb();
                if (text != box.Text)
                    box.Text = text;
            };

            textBox.SetToolTip(Description);

            var label = UiTools.GetLabelOrDefault(name, Label);
            return new List<Control>
            {
                new Control(new MyGuiControlLabel(text: label), minWidth: Control.LabelMinWidth),
                new Control(sample, offset: new Vector2(0f, 0.005f)),
                new Control(textBox, textBox.Size.X)
            };
        }

        public string Description { get; set; }
        public bool HasAlpha { get; set; }
        public string Label { get; set; }
    }
}