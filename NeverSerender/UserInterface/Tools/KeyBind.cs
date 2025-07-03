using VRage.Input;

namespace NeverSerender.UserInterface.Tools
{
    public struct KeyBind
    {
        public MyKeys Key;
        public bool Ctrl;
        public bool Alt;
        public bool Shift;

        public KeyBind(MyKeys key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            Key = key;
            Ctrl = ctrl;
            Alt = alt;
            Shift = shift;
        }

        public override string ToString()
        {
            if (Key == MyKeys.None)
                return "None";

            var ctrl = Ctrl ? "Ctrl+" : "";
            var alt = Alt ? "Alt+" : "";
            var shift = Shift ? "Shift+" : "";
            return $"{ctrl}{alt}{shift}{Key}";
        }

        public bool IsPressed(IMyInput input)
        {
            return Key != MyKeys.None && AreModifiersMatch(input) && input.IsKeyPress(Key);
        }

        public bool NewPressed(IMyInput input)
        {
            return Key != MyKeys.None && AreModifiersMatch(input) && input.IsNewKeyPressed(Key);
        }

        private bool AreModifiersMatch(IMyInput input)
        {
            return input.IsAnyCtrlKeyPressed() == Ctrl &&
                   input.IsAnyAltKeyPressed() == Alt &&
                   input.IsAnyShiftKeyPressed() == Shift;
        }
    }
}