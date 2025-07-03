using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NeverSerender.UserInterface.Tools;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using VRage;
using VRage.Game;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;

namespace NeverSerender.UserInterface.Elements
{
    internal class KeybindElement : IElement<KeyBind>
    {
        private Func<KeyBind> propertyGetter;
        private Action<KeyBind> propertySetter;

        // TODO: Change detection
        public List<Control> GetControls(string name, ElementProperty<KeyBind> property)
        {
            propertyGetter = property.Get;
            propertySetter = property.Set;

            var binding = property.Get();

            var label = new MyGuiControlLabel(text: UiTools.GetLabelOrDefault(name, Label));
            
            var modifiers = MyKeyboardModifiers.None;
            if (binding.Ctrl)
                modifiers |= MyKeyboardModifiers.Control;
            if (binding.Shift)
                modifiers |= MyKeyboardModifiers.Shift;
            if (binding.Alt)
                modifiers |= MyKeyboardModifiers.Alt;

            var control = new MyControl(
                MyStringId.GetOrCompute($"{name.Replace(" ", "")}Keybind"),
                MyStringId.GetOrCompute(name),
                MyGuiControlTypeEnum.General,
                null,
                binding.Key,
                keyModifiers: modifiers);

            StringBuilder output = null;
            control.AppendBoundButtonNames(ref output, MyGuiInputDeviceEnum.Keyboard);
            MyControl.AppendUnknownTextIfNeeded(ref output, MyTexts.GetString(MyCommonTexts.UnknownControl_None));

            var button = new MyGuiControlButton(
                text: output,
                onButtonClick: OnRebindClick,
                onSecondaryButtonClick: OnUnbindClick,
                toolTip: Description)
            {
                VisualStyle = MyGuiControlButtonStyleEnum.ControlSetting,
                UserData = new ControlButtonData(control, MyGuiInputDeviceEnum.Keyboard)
            };

            return new List<Control>
            {
                new Control(label, minWidth: Control.LabelMinWidth),
                new Control(button),
            };
        }

        private void OnRebindClick(MyGuiControlButton button)
        {
            var userData = (ControlButtonData)button.UserData;
            var messageText = MyCommonTexts.AssignControlKeyboard;
            if (userData.Device == MyGuiInputDeviceEnum.Mouse)
                messageText = MyCommonTexts.AssignControlMouse;

            // KEEN!!! MyGuiScreenOptionsControls.MyGuiControlAssignKeyMessageBox is PRIVATE!
            var screenClass = typeof(MyGuiScreenOptionsControls).GetNestedType(
                "MyGuiControlAssignKeyMessageBox",
                BindingFlags.NonPublic);

            var editBindingDialog = (MyGuiScreenBase)Activator.CreateInstance(
                screenClass,
                BindingFlags.CreateInstance,
                null,
                new object[]
                {
                    userData.Device,
                    userData.Control,
                    messageText
                },
                null);

            editBindingDialog.Closed += (s, isUnloading) => StoreControl(button);
            MyGuiSandbox.AddScreen(editBindingDialog);
        }

        private void OnUnbindClick(MyGuiControlButton button)
        {
            void Callback(MyGuiScreenMessageBox.ResultEnum result)
            {
                if (result == MyGuiScreenMessageBox.ResultEnum.NO)
                    return;

                var userData = (ControlButtonData)button.UserData;
                userData.Control.SetControl(userData.Device, MyKeys.None);

                StoreControl(button);
            }

            var alert = MyGuiSandbox.CreateMessageBox(
                MyMessageBoxStyleEnum.Info,
                MyMessageBoxButtonsType.YES_NO,
                new StringBuilder("Are you sure?"),
                new StringBuilder("UNBIND CONTROL"),
                yesButtonText: MyStringId.GetOrCompute("Confirm"),
                noButtonText: MyStringId.GetOrCompute("Cancel"),
                callback: Callback
            );

            MyGuiSandbox.AddScreen(alert);
        }

        private void StoreControl(MyGuiControlButton button)
        {
            StringBuilder output = null;
            var userData = (ControlButtonData)button.UserData;
            userData.Control.AppendBoundButtonNames(ref output, userData.Device);

            userData.Control.GetKeyboardModifier();
            
            var modifiers = userData.Control.GetKeyboardModifier();
            
            var binding = propertyGetter();
            binding.Key = userData.Control.GetKeyboardControl();
            binding.Ctrl = modifiers.HasFlag(MyKeyboardModifiers.Control);
            binding.Shift = modifiers.HasFlag(MyKeyboardModifiers.Shift);
            binding.Alt = modifiers.HasFlag(MyKeyboardModifiers.Alt);
            propertySetter(binding);

            MyControl.AppendUnknownTextIfNeeded(ref output, MyTexts.GetString(MyCommonTexts.UnknownControl_None));
            button.Text = output.ToString();
            output.Clear();
        }

        private class ControlButtonData
        {
            public readonly MyControl Control;
            public readonly MyGuiInputDeviceEnum Device;

            public ControlButtonData(MyControl control, MyGuiInputDeviceEnum device)
            {
                Control = control;
                Device = device;
            }
        }
        
        public string Description { get; set; }
        public string Label { get; set; }
    }
}