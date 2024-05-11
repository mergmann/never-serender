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
        private readonly IDictionary<string, GLTFMesh> gltfModels;

        public Exporter(StreamWriter log, AssetLibrary assetLibrary, TextureExporter textureExporter)
        {
            this.log = log;
            this.assetLibrary = assetLibrary;
            this.textureExporter = textureExporter;
            models = new Dictionary<string, MyModel>();
            gltfMaterials = new Dictionary<string, MaterialBuilder>();
            gltfModels = new Dictionary<string, GLTFMesh>();
        }

        public void ExportBlock(MySlimBlock block, SceneBuilder gltfScene, NodeBuilder gltfRootNode)
        {
            var definition = block.BlockDefinition;
            log.WriteLine($"Block Name={definition.DisplayNameText} Model={definition.Model} Skin={block.SkinSubtypeId.String}");
            block.GetLocalMatrix(out var localMatrix);
            var gltfNode = gltfRootNode.CreateNode(definition.DisplayNameText);
            gltfNode.LocalMatrix = Util.ConvertMatrix(localMatrix);

            if (block.BuildLevelRatio == 1.0)
            {
                if (!string.IsNullOrEmpty(definition.Model))
                    gltfScene.AddRigidMesh(ExportModel(definition.Model), gltfNode);
                if (definition.CubeDefinition != null)
                {
                    var cubeDefinition = definition.CubeDefinition;
                    foreach (var model in cubeDefinition.Model)
                    {
                        var gltfSubNode = gltfNode.CreateNode($"Cube Model {model}");
                        gltfScene.AddRigidMesh(ExportModel(model), gltfSubNode);
                    }
                }
            }
            else
            {
                var index = definition.GetBuildProgressModelIndex(block.BuildLevelRatio);
                ExportModel(definition.BuildProgressModels[index].File);
            }
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
            log.WriteLine($"Model Name={model.AssetName} VertexCount={model.GetVerticesCount()} TriCount={model.GetTrianglesCount()}");

            if (gltfModels.TryGetValue(model.AssetName, out var gltfMesh))
                return gltfMesh;

            gltfMesh = GLTFVertex.CreateCompatibleMesh(model.AssetName);

            foreach (var mesh in model.GetMeshList())
            {
                log.WriteLine($"Mesh Name={mesh.AssetName} IndexStart={mesh.IndexStart} TriStart={mesh.TriStart} TriCount={mesh.TriCount}");
                var material = mesh.Material;
                log.WriteLine($"Material Name={material.Name} Draw={material.DrawTechnique} GlassCW={material.GlassCW} GlassCCW={material.GlassCCW} GlassSmooth={material.GlassSmooth}");
                var gltfMaterial = ExportMaterial(material);
                var gltfPrimitive = gltfMesh.UsePrimitive(gltfMaterial);
                for (var i = 0; i < mesh.TriCount; i++)
                {
                    var triangle = model.Triangles[i + mesh.TriStart];
                    var v0 = Util.ConvertVector(model.GetVertex(triangle.I0));
                    var v1 = Util.ConvertVector(model.GetVertex(triangle.I1));
                    var v2 = Util.ConvertVector(model.GetVertex(triangle.I2));
                    var n0 = Util.ConvertVector(model.GetVertexNormal(triangle.I0));
                    var n1 = Util.ConvertVector(model.GetVertexNormal(triangle.I1));
                    var n2 = Util.ConvertVector(model.GetVertexNormal(triangle.I2));
                    var t0 = Util.ConvertVector(model.TexCoords[triangle.I0].ToVector2());
                    var t1 = Util.ConvertVector(model.TexCoords[triangle.I1].ToVector2());
                    var t2 = Util.ConvertVector(model.TexCoords[triangle.I2].ToVector2());
                    gltfPrimitive.AddTriangle(
                        ((v0, n0), t0),
                        ((v1, n1), t1),
                        ((v2, n2), t2)
                    );
                }
            }

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
                    log.WriteLine($"Material {file.Name} processed");
                    file = assetLibrary.AddMaterial(material, processed);
                }
                else
                {
                    log.WriteLine($"Material {file.Name} cached");
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
