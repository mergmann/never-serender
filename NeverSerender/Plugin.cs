using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using NeverSerender.Config;
using NeverSerender.UserInterface;
using NeverSerender.UserInterface.Layouts;
using NeverSerender.UserInterface.Tools;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.Components;
using VRage.Input;
using VRage.Plugins;
using VRage.Utils;
using VRageMath;

namespace NeverSerender
{
    // ReSharper disable once UnusedType.Global
    public class Plugin : IPlugin
    {
        private const string LogPath = "Z:/home/mattisb/spacemodel/neverserender.txt";
        private const string OutPath = "Z:/home/mattisb/spacemodel/model.semodel";

        public const string Name = "NeverSerender";

        public static Plugin Instance { get; private set; }

        private ConfigScreen globalConfigScreen;
        private ConfigScreen exportConfigScreen;
        private KeyBind openSettingsBind = new KeyBind(MyKeys.R, ctrl: true);
        private Capture capture;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Init(object gameInstance)
        {
            var harmony = new Harmony(Name);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Instance = this;

            var globalSettingsGenerator = new ConfigGenerator(GlobalConfig.Current);
            globalConfigScreen = new ConfigScreen(globalSettingsGenerator, "NeverSerender Global Settings");
            globalConfigScreen.Removed += () => ConfigStorage.Save(GlobalConfig.Current);

            var exportSettingsGenerator = new ConfigGenerator(ExportConfig.Current);
            exportConfigScreen = new ConfigScreen(exportSettingsGenerator, "NeverSerender Export");

            MyLog.Default.WriteLineAndConsole("NeverSerender Plugin initialized");
        }

        public void Dispose() => Instance = null;

        public void Update()
        {
            globalConfigScreen.Update();
            exportConfigScreen.Update();
        }

        [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
        public class NeverSerenderPluginSession : MySessionComponentBase
        {
            public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
            {
                MyLog.Default.WriteLineAndConsole("NeverSerender Session initialized");
            }

            public override void UpdateAfterSimulation()
            {
                if (MySession.Static == null || !MySession.Static.Ready)
                    return;

                if (Instance.openSettingsBind.NewPressed(MyInput.Static))
                    Instance.OpenConfigDialog();

                if (GlobalConfig.Current.ExportBind.IsPressed(MyInput.Static))
                {
                    if (Instance.capture != null)
                    {
                        // Stop currently running capture
                        Instance.capture.Finish();
                        Instance.capture = null;
                        MessageCaptureStopped();
                    }
                    else
                    {
                        Instance.OpenExportDialog();
                    }
                }

                // Step forward one frame if the capture is running
                Instance.capture?.Step(1.0f / 60.0f);
            }

            public override void UpdatingStopped()
            {
                if (Instance.capture == null) return;

                // Stop currently running capture
                Instance.capture.Finish();
                Instance.capture = null;
                MessageCaptureStopped();
            }
        }

        // ReSharper disable once UnusedMember.Global
        public void OpenConfigDialog()
        {
            globalConfigScreen.SetLayout<Simple>();
            MyGuiSandbox.AddScreen(globalConfigScreen);
        }

        public void OpenExportDialog()
        {
            exportConfigScreen.SetLayout<Simple>();
            MyGuiSandbox.AddScreen(exportConfigScreen);
        }

        public void StartExport()
        {
            MyLog.Default.WriteLine("NeverSerender: Starting export");
            exportConfigScreen.CloseScreen();

            if (capture != null)
            {
                MyMessageBox.Show("Export already in progress", "Stop the current export before starting a new one.");
                return;
            }

            var globalConfig = GlobalConfig.Current;
            var exportConfig = ExportConfig.Current;

            var logPath = Path.Combine(MyFileSystem.UserDataPath, globalConfig.LogPath);
            var outPath = Path.Combine(MyFileSystem.UserDataPath, globalConfig.OutPath, exportConfig.FileName);

            var targetGrid = MyCubeGrid.GetTargetGrid();

            List<long> entityIds = null;
            if (exportConfig.Scope == ExportConfig.ExportScope.SingleGrid)
            {
                if (targetGrid == null)
                {
                    MessageBoxNoTargetGrid();
                    return;
                }

                entityIds = new List<long> { targetGrid.EntityId };
            }

            MatrixD viewMatrix;
            switch (exportConfig.Anchor)
            {
                case ExportConfig.ViewAnchor.Player:
                    viewMatrix = MySession.Static.LocalCharacter.GetViewMatrix();
                    break;
                case ExportConfig.ViewAnchor.Grid when targetGrid == null:
                    MessageBoxNoTargetGrid();
                    return;
                case ExportConfig.ViewAnchor.Grid:
                    viewMatrix = targetGrid.GetViewMatrix();
                    break;
                case ExportConfig.ViewAnchor.Origin:
                default:
                    viewMatrix = MatrixD.Identity; // Origin
                    break;
            }

            var animation = exportConfig.Animation && exportConfig.ShowAnimation;

            var settings = new ExporterSettings
            {
                LogPath = logPath,
                OutPath = outPath,
                AutoFlush = true,
                ExportNonGrids = exportConfig.Scope == ExportConfig.ExportScope.AllEntities,
                ExportEntityIds = entityIds,
                ViewMatrix = viewMatrix
            };

            if (!File.Exists(outPath))
            {
                StartCapture();
                return;
            }

            MessageBoxOverwrite(result =>
            {
                if (result == MyGuiScreenMessageBox.ResultEnum.YES)
                    StartCapture();
            });

            return;

            void StartCapture()
            {
                capture = new Capture(settings);
                capture.Step(null);
                // Keep the capture running until the export keybind is pressed again
                if (animation) return;
                capture.Finish();
                capture = null;
            }
        }

        private static void MessageBoxNoTargetGrid()
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Info,
                MyMessageBoxButtonsType.OK,
                new StringBuilder("Please look at a grid before exporting."),
                new StringBuilder("No grid targeted"),
                MyStringId.GetOrCompute("If you say so..."),
                MyStringId.NullOrEmpty,
                MyStringId.NullOrEmpty,
                MyStringId.NullOrEmpty,
                result => { },
                -1,
                MyGuiScreenMessageBox.ResultEnum.CANCEL,
                true,
                null));
        }

        private void MessageBoxOverwrite(Action<MyGuiScreenMessageBox.ResultEnum> callback)
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Info,
                MyMessageBoxButtonsType.YES_NO,
                new StringBuilder("Are you sure you want to overwrite the existing file?"),
                new StringBuilder("Model file exists"),
                MyStringId.NullOrEmpty,
                MyStringId.NullOrEmpty,
                MyStringId.GetOrCompute("Yes"),
                MyStringId.GetOrCompute("No"),
                callback,
                -1,
                MyGuiScreenMessageBox.ResultEnum.NO,
                true,
                null));
        }

        private static void MessageCaptureStopped()
        {
            MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Info,
                MyMessageBoxButtonsType.OK,
                new StringBuilder("Capture has been stopped."),
                new StringBuilder("Capture stopped"),
                MyStringId.GetOrCompute("Ok"),
                MyStringId.NullOrEmpty,
                MyStringId.NullOrEmpty,
                MyStringId.NullOrEmpty,
                result => { },
                -1,
                MyGuiScreenMessageBox.ResultEnum.CANCEL,
                true,
                null));
        }
    }
}