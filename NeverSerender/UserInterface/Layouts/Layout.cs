using System;
using System.Collections.Generic;
using NeverSerender.UserInterface.Elements;
using Sandbox.Graphics.GUI;
using VRageMath;

namespace NeverSerender.UserInterface.Layouts
{
    internal abstract class Layout
    {
        /// <summary>
        ///     Call this to receive a list of rows of controls.
        /// </summary>
        protected readonly Func<List<List<Control>>> GetControls;

        public Layout(Func<List<List<Control>>> getControls)
        {
            GetControls = getControls;
        }

        /// <summary>
        ///     Size of the UI screen this layout is responsible for.
        /// </summary>
        public abstract Vector2 PanelSize { get; }

        /// <summary>
        ///     Recreates all the layout-specific controls.
        ///     Includes setting up parents.
        /// </summary>
        /// <returns>Any controls to be parented to the screen.</returns>
        public abstract List<MyGuiControlBase> RecreateControls();

        /// <summary>
        ///     Layout all the existing controls. Do not create controls in here.
        /// </summary>
        public abstract void LayoutControls();
    }
}