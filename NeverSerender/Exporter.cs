using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using System.Collections.Generic;
using System.IO;
using VRage.Game.Models;
using VRageRender.Models;

namespace NeverSerender
{
    using GLTFMesh = MeshBuilder<VertexPositionNormal, VertexTexture1, VertexEmpty>;
    using GLTFVertex = VertexBuilder<VertexPositionNormal, VertexTexture1, VertexEmpty>;

    public class Exporter
    {
        private readonly StreamWriter log;

        private readonly AssetLibrary assetLibrary;
        private readonly TextureExporter textureExporter;
        private readonly IDictionary<string, MyModel> models;
        private readonly IDictionary<string, MaterialBuilder> gltfMaterials;
        private readonly IDictionary<string, GLTFMesh> gltfMeshes;

        public Exporter(StreamWriter log, AssetLibrary assetLibrary, TextureExporter textureExporter)
        {
            this.log = log;
            this.assetLibrary = assetLibrary;
            this.textureExporter = textureExporter;
            models = new Dictionary<string, MyModel>();
            gltfMaterials = new Dictionary<string, MaterialBuilder>();
            gltfMeshes = new Dictionary<string, GLTFMesh>();
        }

        public void ExportBlock(MyCubeBlock block, SceneBuilder gltfScene, NodeBuilder gltfParentNode)
        {
            log.WriteLine($"Block Name={block.DisplayNameText} Model={block.Model} Skin={block.SlimBlock.SkinSubtypeId}");
            var gltfNode = gltfParentNode.CreateNode(block.DisplayNameText);

            block.GetLocalMatrix(out var matrix);
            gltfNode.LocalMatrix = Util2.ConvertMatrix(matrix);
            gltfScene.AddRigidMesh(ExportModel(block.Model), gltfNode);
        }

        public void ExportCell(MyCubeGridRenderCell cell, SceneBuilder gltfScene, NodeBuilder gltfParentNode)
        {
            var gltfNode = gltfParentNode.CreateNode($"Cube {cell.DebugName}");
            foreach (var pair in cell.CubeParts)
                ExportCubePart(pair.Key, gltfScene, gltfNode);
        }

        public void ExportCubePart(MyCubePart part, SceneBuilder gltfScene, NodeBuilder gltfParentNode)
        {
            log.WriteLine($"CubePart Model={part.Model} Skin={part.SkinSubtypeId}");
            var data = part.InstanceData;
            var gltfNode = gltfParentNode.CreateNode($"CubePart Model {part.Model}");
            gltfNode.LocalMatrix = Util2.ConvertMatrix(data.LocalMatrix);
            gltfScene.AddRigidMesh(ExportModel(part.Model), gltfNode);
        }

        public GLTFMesh ExportModel(string asset)
        {
            if (!models.TryGetValue(asset, out var model))
            {
                model = new MyModel(asset)
                {
                    LoadUV = true,
                };
                model.LoadData();
                models.Add(asset, model);
            }

            return ExportModel(model);
        }

        public GLTFMesh ExportModel(MyModel model)
        {
            log.WriteLine($"\nModel Name={model.AssetName} VertexCount={model.GetVerticesCount()} TriCount={model.GetTrianglesCount()}");

            if (gltfMeshes.TryGetValue(model.AssetName, out var gltfMesh))
                return gltfMesh;

            if (!model.HasUV)
            {
                model.LoadUV = true;
                model.UnloadData();
                model.LoadData();
            }

            gltfMesh = GLTFVertex.CreateCompatibleMesh(model.AssetName);

            foreach (var mesh in model.GetMeshList())
            {
                var material = mesh.Material;
                log.WriteLine($"Material Name={material.Name} Draw={material.DrawTechnique} GlassCW={material.GlassCW} GlassCCW={material.GlassCCW} GlassSmooth={material.GlassSmooth}");
                var gltfMaterial = ExportMaterial(material);
                var gltfPrimitive = gltfMesh.UsePrimitive(gltfMaterial);
                log.WriteLine($"Mesh Name={mesh.AssetName} IndexStart={mesh.IndexStart} TriStart={mesh.TriStart} TriCount={mesh.TriCount}");
                for (var i = 0; i < mesh.TriCount; i++)
                {
                    var triangle = model.Triangles[i + mesh.TriStart];
                    var v0 = Util2.ConvertVector(model.GetVertex(triangle.I0));
                    var v1 = Util2.ConvertVector(model.GetVertex(triangle.I1));
                    var v2 = Util2.ConvertVector(model.GetVertex(triangle.I2));
                    var n0 = Util2.ConvertVector(model.GetVertexNormal(triangle.I0));
                    var n1 = Util2.ConvertVector(model.GetVertexNormal(triangle.I1));
                    var n2 = Util2.ConvertVector(model.GetVertexNormal(triangle.I2));
                    var t0 = Util2.ConvertVector(model.TexCoords[triangle.I0].ToVector2());
                    var t1 = Util2.ConvertVector(model.TexCoords[triangle.I1].ToVector2());
                    var t2 = Util2.ConvertVector(model.TexCoords[triangle.I2].ToVector2());
                    gltfPrimitive.AddTriangle(((v0, n0), t0), ((v1, n1), t1), ((v2, n2), t2));
                }
            }

            log.WriteLine("Model end\n");

            gltfMeshes.Add(model.AssetName, gltfMesh);

            return gltfMesh;
        }

        public MaterialBuilder ExportMaterial(MyMeshMaterial material)
        {
            if (gltfMaterials.TryGetValue(material.Name, out var gltfMaterial))
                return gltfMaterial;

            var file = assetLibrary.GetMaterial(material);
            if (file == null)
            {
                if (textureExporter.ProcessMaterial(material, out var processed))
                {
                    log.WriteLine($"Material {processed.Name} processed");
                    file = assetLibrary.AddMaterial(material, processed);
                }
                else
                {
                    log.WriteLine($"Material {processed.Name} cached");
                }
            }

            gltfMaterial = new MaterialBuilder()
                .WithMetallicRoughnessShader()
                .WithBaseColor(file.ColorPath)
                .WithNormal(file.NormalPath)
                .WithChannelImage(KnownChannel.MetallicRoughness, file.RoughMetalPath);
            gltfMaterials.Add(file.Name, gltfMaterial);

            return gltfMaterial;
        }
    }
}
