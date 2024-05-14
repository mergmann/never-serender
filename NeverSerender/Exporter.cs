using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using SharpGLTF.Geometry;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using System.Collections.Generic;
using VRage.Game.Models;
using VRageMath;
using VRageRender.Models;

namespace NeverSerender
{
    using GLTFMesh = MeshBuilder<VertexPositionNormal, VertexTexture1, VertexEmpty>;
    using GLTFVertex = VertexBuilder<VertexPositionNormal, VertexTexture1, VertexEmpty>;

    public class Exporter
    {
        private readonly MiniLog log;

        private readonly AssetLibrary assetLibrary;
        private readonly IDictionary<string, MyModel> models;
        private readonly IDictionary<AssetIdentifier, MaterialFiles> materials;
        private readonly IDictionary<AssetIdentifier, MaterialBuilder> gltfMaterials;
        private readonly IDictionary<AssetIdentifier, GLTFMesh> gltfMeshes;

        public Exporter(MiniLog log, AssetLibrary assetLibrary, TextureExporter textureExporter)
        {
            this.log = log;
            this.assetLibrary = assetLibrary;
            models = new Dictionary<string, MyModel>();
            materials = new Dictionary<AssetIdentifier, MaterialFiles>();
            gltfMaterials = new Dictionary<AssetIdentifier, MaterialBuilder>();
            gltfMeshes = new Dictionary<AssetIdentifier, GLTFMesh>();
        }

        public void PrepareBlock(MyCubeBlock block)
        {
            var slimBlock = block.SlimBlock;
            log.WriteLine($"Prepare Block Name={block.DisplayNameText} Model={block.Model} Skin={slimBlock.SkinSubtypeId} Color={slimBlock.ColorMaskHSV}");
            ExportModel(block.Model, slimBlock.SkinSubtypeId.String, slimBlock.ColorMaskHSV);
        }

        public void PrepareCell(MyCubeGridRenderCell cell)
        {
            log.WriteLine($"Prepare Cell Name={cell.DebugName}");
            foreach (var pair in cell.CubeParts)
                PrepareCubePart(pair.Key);
        }

        public void PrepareCubePart(MyCubePart part)
        {
            log.WriteLine($"Prepare CubePart Model={part.Model.AssetName} Skin={part.SkinSubtypeId} Color={part.InstanceData.ColorMaskHSV}");
            var color4 = part.InstanceData.ColorMaskHSV;
            var color = new Vector3(color4.X, color4.Y, color4.Z);
            PrepareModel(part.Model, part.SkinSubtypeId.String, color);
        }

        public void PrepareModel(string asset, string skin, Vector3 color)
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

            PrepareModel(model, skin, color);
        }

        public void PrepareModel(MyModel model, string skin, Vector3 color)
        {
            log.WriteLine($"\nPrepare Model Name={model.AssetName} VertexCount={model.GetVerticesCount()} TriCount={model.GetTrianglesCount()}");

            if (!model.HasUV)
            {
                model.LoadUV = true;
                model.UnloadData();
                model.LoadData();
            }

            foreach (var mesh in model.GetMeshList())
                PrepareMaterial(mesh.Material, skin, color);
        }

        public void PrepareMaterial(MyMeshMaterial material, string skin, Vector3 color)
        {
            log.WriteLine($"Prepare Material Name={material.Name} Draw={material.DrawTechnique} GlassCW={material.GlassCW} GlassCCW={material.GlassCCW} GlassSmooth={material.GlassSmooth}");

            var identifier = new AssetIdentifier(material.Name, skin, color);
            if (materials.ContainsKey(identifier))
                return;

            var files = assetLibrary.GetMaterial(material, skin, color);
            materials.Add(identifier, files);
        }

        public void ExportBlock(MyCubeBlock block, SceneBuilder gltfScene, NodeBuilder gltfParentNode)
        {
            var slimBlock = block.SlimBlock;
            log.WriteLine($"Block Name={block.DisplayNameText} Model={block.Model} Skin={slimBlock.SkinSubtypeId} Color={slimBlock.ColorMaskHSV}");
            var gltfNode = gltfParentNode.CreateNode(block.DisplayNameText);

            block.GetLocalMatrix(out var matrix);
            gltfNode.LocalMatrix = Util2.ConvertMatrix(matrix);
            gltfScene.AddRigidMesh(ExportModel(block.Model, slimBlock.SkinSubtypeId.String, slimBlock.ColorMaskHSV), gltfNode);
        }

        public void ExportCell(MyCubeGridRenderCell cell, SceneBuilder gltfScene, NodeBuilder gltfParentNode)
        {
            log.WriteLine($"Cell Name={cell.DebugName}");
            var gltfNode = gltfParentNode.CreateNode($"Cube {cell.DebugName}");
            foreach (var pair in cell.CubeParts)
                ExportCubePart(pair.Key, gltfScene, gltfNode);
        }

        public void ExportCubePart(MyCubePart part, SceneBuilder gltfScene, NodeBuilder gltfParentNode)
        {
            log.WriteLine($"CubePart Model={part.Model.AssetName} Skin={part.SkinSubtypeId} Color={part.InstanceData.ColorMaskHSV}");
            var data = part.InstanceData;
            var gltfNode = gltfParentNode.CreateNode($"CubePart Model {part.Model}");
            gltfNode.LocalMatrix = Util2.ConvertMatrix(data.LocalMatrix);
            var color4 = part.InstanceData.ColorMaskHSV;
            var color = new Vector3(color4.X, color4.Y, color4.Z);
            gltfScene.AddRigidMesh(ExportModel(part.Model, part.SkinSubtypeId.String, color), gltfNode);
        }

        public GLTFMesh ExportModel(string asset, string skin, Vector3 color)
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

            return ExportModel(model, skin, color);
        }

        public GLTFMesh ExportModel(MyModel model, string skin, Vector3 color)
        {
            log.WriteLine($"\nModel Name={model.AssetName} VertexCount={model.GetVerticesCount()} TriCount={model.GetTrianglesCount()}");
            var identifier = new AssetIdentifier(model.AssetName, skin, color);
            if (gltfMeshes.TryGetValue(identifier, out var gltfMesh))
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
                var gltfMaterial = ExportMaterial(material, skin, color);
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

            gltfMeshes.Add(identifier, gltfMesh);

            return gltfMesh;
        }

        public MaterialBuilder ExportMaterial(MyMeshMaterial material, string skin, Vector3 color)
        {
            var identifier = new AssetIdentifier(material.Name, skin, color);
            if (gltfMaterials.TryGetValue(identifier, out var gltfMaterial))
                return gltfMaterial;

            var files = materials[identifier];

            gltfMaterial = new MaterialBuilder()
                .WithMetallicRoughnessShader()
                .WithBaseColor(files.ColorPath)
                .WithNormal(files.NormalPath)
                .WithChannelImage(KnownChannel.MetallicRoughness, files.RoughMetalPath);
            gltfMaterials.Add(identifier, gltfMaterial);

            return gltfMaterial;
        }
    }
}
