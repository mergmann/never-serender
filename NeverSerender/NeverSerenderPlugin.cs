using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Game.Components;
using VRage.Input;
using VRage.Plugins;
using VRage.Utils;

namespace NeverSerender
{
    // ReSharper disable once UnusedType.Global
    public class NeverSerenderPlugin : IPlugin
    {
        private const string LogPath = "Z:/home/mattisb/spacemodel/neverserender.txt";
        private const string OutPath = "Z:/home/mattisb/spacemodel/model.semodel";

        public const string Name = "NeverSerender";

        public static NeverSerenderPlugin Instance { get; private set; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Init(object gameInstance)
        {
            var harmony = new Harmony(Name);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Instance = this;
            MyLog.Default.WriteLineAndConsole("NeverSerender Plugin initialized");
        }

        public void Dispose()
        {
            Instance = null;
        }

        public void Update()
        {
            // TODO: Put your update code here. It is called on every simulation frame!
        }

        [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
        public class NeverSerenderPluginSession : MySessionComponentBase
        {
            private static readonly ExportSettings Settings = new ExportSettings
            {
                LogPath = LogPath,
                OutPath = OutPath,
                AutoFlush = true
            };

            private Capture capture;

            public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
            {
                MyLog.Default.WriteLineAndConsole("NeverSerender Session initialized");
            }

            public override void UpdateAfterSimulation()
            {
                if (MySession.Static == null || !MySession.Static.Ready)
                    return;

                capture?.Step(1.0f / 60.0f);

                if (!MyInput.Static.IsNewKeyPressed(MyKeys.R)) return;

                if (MyInput.Static.IsAnyCtrlKeyPressed())
                {
                    var captureOnce = new Capture(Settings);
                    captureOnce.Step(null);
                    captureOnce.Finish();
                }

                if (!MyInput.Static.IsAnyShiftKeyPressed()) return;
                if (capture == null)
                {
                    capture = new Capture(Settings);
                    capture.Step(null);
                }
                else
                {
                    capture.Finish();
                    capture = null;
                }
            }
        }

        // TODO: Uncomment and use this method to create a plugin configuration dialog
        // ReSharper disable once UnusedMember.Global
        /*public void OpenConfigDialog()
        {
            MyGuiSandbox.AddScreen(new MyPluginConfigDialog());
        }*/

        //TODO: Uncomment and use this method to load asset files
        /*public void LoadAssets(string folder)
        {

        }*/
    }
}