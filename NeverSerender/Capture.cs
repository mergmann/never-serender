using Sandbox.Game.Entities;
using System;
using System.IO;
using VRage.Game.Models;
using VRage.Utils;

namespace ClientPlugin
{
    public class Capture
    {
        public static void CaptureGame(string path)
        {
            MyLog.Default.WriteLineAndConsole("NeverSerender Capture");
            if (File.Exists(path))
                File.Delete(path);
            using (var file = File.OpenWrite(path))
            {
                var writer = new StreamWriter(file);
                var converter = new TextureConverter("Z:/home/mattisb/steamapps/common/SpaceEngineers/Content/");
                try
                {
                    foreach (var entity in MyEntities.GetEntities())
                    {
                        writer.WriteLine($"Entity DebugName={entity.DebugName} DisplayNameText={entity.DisplayNameText}");
                        if (entity is MyCubeGrid grid)
                        {
                            writer.WriteLine($"Grid BlocksCount={grid.BlocksCount}");
                            foreach (var block in grid.GetBlocks())
                            {
                                var definition = block.BlockDefinition;
                                writer.WriteLine($"Block Name={definition.DisplayNameText} Model={definition.Model}");
                                if (definition.CubeDefinition != null)
                                {
                                    var cubeDefinition = definition.CubeDefinition;
                                    writer.WriteLine("Cube");
                                    foreach (var model in cubeDefinition.Model)
                                    {
                                        writer.WriteLine($"Model={model}");
                                        ExportModel(converter, model, writer);
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    writer.Flush();
                }
            }
        }

        internal static void ExportModel(TextureConverter converter, string name, StreamWriter writer)
        {
            var model = MyModels.GetModel(name);
            writer.WriteLine($"Mode Name={model.AssetName} VertexCount={model.GetVerticesCount()} TriCount={model.GetTrianglesCount()}");
            foreach (var mesh in model.GetMeshList())
            {
                writer.WriteLine($"Mesh Name={mesh.AssetName} IndexStart={mesh.IndexStart} TriStart={mesh.TriStart} TriCount={mesh.TriCount}");
                var material = mesh.Material;
                writer.WriteLine($"Material Name={material.Name} Draw={material.DrawTechnique} GlassCW={material.GlassCW} GlassCCW={material.GlassCCW} GlassSmooth={material.GlassSmooth}");
                try
                {
                    var processed = converter.ProcessMaterial(material);
                    processed?.Save("Z:/home/mattisb/setex/");
                    writer.WriteLine($"Material {material.Name} processed");
                    writer.Flush();
                }
                catch (Exception ex)
                {
                    writer.WriteLine($"Material {material.Name} errored: {ex}");
                }
            }
        }
    }
}
