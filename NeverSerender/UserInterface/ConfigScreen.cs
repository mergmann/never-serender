using System;
using System.Collections.Generic;
using NeverSerender.Config;
using NeverSerender.UserInterface.Layouts;
using Sandbox;
using Sandbox.Graphics.GUI;
using VRageMath;

namespace NeverSerender.UserInterface
{
    internal class ConfigScreen : MyGuiScreenBase
    {
        private readonly ConfigGenerator config;
        private readonly string friendlyName;

        public event Action Removed;

        public ConfigScreen(
            ConfigGenerator config,
            string friendlyName,
            Vector2? position = null
        ) : base(
            position ?? new Vector2(0.5f, 0.5f),
            MyGuiConstants.SCREEN_BACKGROUND_COLOR,
            config.ActiveLayout.PanelSize,
            false,
            null,
            MySandboxGame.Config.UIBkOpacity,
            MySandboxGame.Config.UIOpacity)
        {
            this.config = config;
            this.friendlyName = friendlyName;
            EnabledBackgroundFade = true;
            m_closeOnEsc = true;
            m_drawEvenWithoutFocus = true;
            CanHideOthers = true;
            CanBeHidden = true;
            CloseButtonEnabled = true;
        }

        public override string GetFriendlyName() => friendlyName;

        public void Update()
        {
            if(config.Update())
                RecreateControls(true);
        }

        public void SetLayout<T>() where T : Layout
        {
            config.SetLayout<T>();
            Size = config.ActiveLayout.PanelSize;
            CloseButtonEnabled = CloseButtonEnabled; // Force close button to update
        }

        public override void LoadContent()
        {
            base.LoadContent();
            RecreateControls(true);
        }

        public override void OnRemoved()
        {
            Removed?.Invoke();
            base.OnRemoved();
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            AddCaption(Name);

            foreach (var item in config.RecreateControls()) Controls.Add(item);
        }
    }
}