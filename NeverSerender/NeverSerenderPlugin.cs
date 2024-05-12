using HarmonyLib;
using Sandbox.Game.World;
using System;
using System.Reflection;
using VRage.Game;
using VRage.Game.Components;
using VRage.Input;
using VRage.Plugins;
using VRage.Utils;

namespace NeverSerender
{
    // ReSharper disable once UnusedType.Global
    public class NeverSerenderPlugin : IPlugin, IDisposable
    {
        public const string Name = "NeverSerender";
        public static NeverSerenderPlugin Instance { get; private set; }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void Init(object gameInstance)
        {
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                var name = new AssemblyName(e.Name);
                MyLog.Default.WriteLineAndConsole($"Assembly load: {e.Name} from {e.RequestingAssembly}");
                if (name.Name == "System.Numerics.Vectors" && name.Version == new Version(4, 1, 3, 0))
                {
                    MyLog.Default.WriteLineAndConsole($"Intercepting assembly load: {name}");
                    return Util.SystemNumericsVector;
                }
                return null;
            };
            Instance = this;
            Harmony harmony = new Harmony(Name);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            MyLog.Default.WriteLineAndConsole("NeverSerender Plugin initialized");
        }

        public void Dispose()
        {
            Instance = null;
        }

        public void Update()
        {
            if (MyInput.Static.IsMiddleMousePressed())
            {
                Environment.Exit(0);
            }
            // TODO: Put your update code here. It is called on every simulation frame!
        }

        [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
        public class NeverSerenderPluginSession : MySessionComponentBase
        {
            public static NeverSerenderPluginSession Instance;

            public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
            {
                Instance = this;
                MyLog.Default.WriteLineAndConsole("NeverSerender Session initialized");
            }

            public override void UpdateAfterSimulation()
            {
                if (MySession.Static == null || !MySession.Static.Ready)
                    return;
                if (MyInput.Static.IsKeyPress(MyKeys.R))
                    Capture.CaptureGame("Z:/home/mattisb/neverserender.txt");
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