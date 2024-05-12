using Sandbox.Game.Entities;
using SharpGLTF.Scenes;
using System;
using System.IO;
using VRage.Utils;

namespace NeverSerender
{
    public class Capture
    {

        public static void CaptureGame(string logPath)
        {
            string outputPath = "Z:/home/mattisb/semodel/model.gltf";
            string libraryPath = "Z:/home/mattisb/setex/";
            string contentPath = "Z:/home/mattisb/steamapps/common/SpaceEngineers/Content/";

            MyLog.Default.WriteLineAndConsole("NeverSerender Capture");
            if (File.Exists(logPath))
                File.Delete(logPath);

            using (var file = File.OpenWrite(logPath))
            {
                var log = new StreamWriter(file);
                AssetLibrary assetLibrary;
                try
                {
                    assetLibrary = AssetLibrary.Open(libraryPath, contentPath);
                }
                catch (Exception ex)
                {
                    log.WriteLine($"Asset library could not be opened: {ex}");
                    assetLibrary = new AssetLibrary(libraryPath, contentPath);
                }
                var textureExporter = new TextureExporter(contentPath);
                var exporter = new Exporter(log, assetLibrary, textureExporter);
                var gltfScene = new SceneBuilder("Space Engineers Solar System");
                try
                {
                    foreach (var entity in MyEntities.GetEntities())
                    {
                        log.WriteLine($"Entity DebugName={entity.DebugName} DisplayNameText={entity.DisplayNameText}");
                        if (entity is MyCubeGrid grid)
                        {
                            log.WriteLine($"Grid BlocksCount={grid.BlocksCount}");
                            var gltfNode = new NodeBuilder($"Grid {grid.Name}")
                            {
                                LocalMatrix = Util2.ConvertMatrix(grid.WorldMatrix)
                            };
                            foreach (var cell in grid.RenderData.Cells)
                            {
                                try
                                {
                                    exporter.ExportCell(cell.Value, gltfScene, gltfNode);
                                }
                                catch (Exception ex)
                                {
                                    log.WriteLine($"Grid Error={ex}");
                                }
                            }
                            foreach (var block in grid.GetBlocks())
                            {
                                try
                                {
                                    if (block.FatBlock != null)
                                        exporter.ExportBlock(block.FatBlock, gltfScene, gltfNode);
                                }
                                catch (Exception ex)
                                {
                                    log.WriteLine($"Grid Error={ex}");
                                }
                            }
                            gltfScene.AddNode(gltfNode);
                        }
                    }

                    log.WriteLine("Creating model");
                    log.Flush();
                    var gltfModel = gltfScene.ToGltf2();
                    log.WriteLine("Saving model");
                    log.Flush();
                    gltfModel.SaveGLTF(outputPath);
                    log.WriteLine("Done");
                }
                finally
                {
                    log.Flush();
                    assetLibrary.Save();
                }
            }
        }
    }
}
