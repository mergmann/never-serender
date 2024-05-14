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
                var log = new MiniLog(new StreamWriter(file));
                var assetLibrary = AssetLibrary.OpenOrNew(libraryPath, log);

                var textureExporter = new TextureExporter(contentPath, log);
                var exporter = new Exporter(log, assetLibrary, textureExporter);
                try
                {
                    foreach (var entity in MyEntities.GetEntities())
                    {
                        log.WriteLine($"Prepare Entity DebugName={entity.DebugName} DisplayNameText={entity.DisplayNameText}");
                        if (entity is MyCubeGrid grid)
                        {
                            log.WriteLine($"Prepare Grid BlocksCount={grid.BlocksCount}");
                            foreach (var cell in grid.RenderData.Cells)
                            {
                                try
                                {
                                    exporter.PrepareCell(cell.Value);
                                }
                                catch (Exception ex)
                                {
                                    log.WriteLine($"Prepare Cell Error={ex}");
                                }
                            }
                            foreach (var block in grid.GetBlocks())
                            {
                                try
                                {
                                    if (block.FatBlock != null)
                                        exporter.PrepareBlock(block.FatBlock);
                                }
                                catch (Exception ex)
                                {
                                    log.WriteLine($"Prepare Block Error={ex}");
                                }
                            }
                        }
                    }

                    log.WriteLine("\n---- Preparation done ----");
                    log.WriteLine("Converting textures...");
                    assetLibrary.ExportMissing(textureExporter);
                    log.WriteLine("Exporting scene...\n");

                    var gltfScene = new SceneBuilder("Space Engineers Solar System");
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
                                    log.WriteLine($"Cell Error={ex}");
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
                                    log.WriteLine($"Block Error={ex}");
                                }
                            }
                            gltfScene.AddNode(gltfNode);
                        }
                    }

                    log.WriteLine("Creating model");
                    var gltfModel = gltfScene.ToGltf2();
                    log.WriteLine("Saving model");
                    gltfModel.SaveGLTF(outputPath);
                    log.WriteLine("Done");
                }
                catch (Exception ex)
                {
                    log.WriteLine($"Error {ex}");
                }
                finally
                {
                    assetLibrary.Save();
                }
            }
        }
    }
}
