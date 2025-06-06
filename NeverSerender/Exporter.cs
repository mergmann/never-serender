using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using NeverSerender.Output;
using NeverSerender.Snapshot;
using Sandbox.Definitions;
using VRage.Game.Entity;
using VRage.Game.Models;
using VRage.Import;
using VRage.Render.Scene;
using VRage.Render.Scene.Components;
using VRageMath;
using VRageRender.Import;
using VRageRender.Models;

namespace NeverSerender
{
    public class Exporter : IDisposable
    {
        private static readonly List<MyMeshDrawTechnique> SupportedDrawModes = new List<MyMeshDrawTechnique>
        {
            MyMeshDrawTechnique.MESH,
            MyMeshDrawTechnique.DECAL,
            MyMeshDrawTechnique.DECAL_CUTOUT,
            MyMeshDrawTechnique.DECAL_NOPREMULT
        };

        private readonly Dictionary<string, MyModel> cubeModels = new Dictionary<string, MyModel>();

        private readonly MiniLog log;
        private readonly MaterialLibrary materialLibrary = new MaterialLibrary();

        private readonly Dictionary<long, uint> trackedEntityIds = new Dictionary<long, uint>();
        private readonly Dictionary<(uint, bool), uint> trackedLightIds = new Dictionary<(uint, bool), uint>();

        private readonly Dictionary<uint, LightSnapshot> trackedLights = new Dictionary<uint, LightSnapshot>();

        private readonly Dictionary<string, uint> trackedMaterialIds = new Dictionary<string, uint>();

        private readonly Dictionary<uint, string[]> trackedModelMaterials = new Dictionary<uint, string[]>();
        private readonly Dictionary<string, uint> trackedModels = new Dictionary<string, uint>();

        private readonly Dictionary<(string, string), uint?>
            trackedOverrides = new Dictionary<(string, string), uint?>();

        private readonly Dictionary<string, uint> trackedTextureIds = new Dictionary<string, uint>();
        private readonly SpaceWriter writer;
        private uint trackedEntityId = 1;
        private uint trackedLightId = 1;
        private uint trackedMaterialId = 1;
        private uint trackedModelId = 1;
        private uint trackedTextureId = 1;

        public Exporter(MiniLog log, SpaceWriter writer)
        {
            this.log = log;
            this.writer = writer;
        }

        public void Dispose()
        {
            cubeModels.ForEach(m => m.Value.UnloadData());
        }

        public IList<LightSnapshot> GetLights()
        {
            var actors = Traverse.Create<MyIDTracker<MyActor>>().Field("m_dict")
                .GetValue<Dictionary<uint, MyIDTracker<MyActor>>>();

            var lights = new List<LightSnapshot>();
            var removedLights = new HashSet<uint>(trackedLights.Keys);

            foreach (var pair in actors)
                try
                {
                    var actor = Traverse.Create(pair.Value).Field("m_value").GetValue<MyActor>();

                    var lightComponent = actor?.GetComponent<MyLightComponent>();
                    if (lightComponent == null)
                        continue;
                    var lightData = lightComponent.Data;

                    // log.WriteLine($"Get Light Name={actor.DebugName} Position={lightData.Position} PointLightOn={lightData.PointLightOn} SpotLightOn={lightData.SpotLightOn} CastShadows={lightData.CastShadows}");
                    if (!lightData.PointLightOn && !lightData.SpotLightOn) continue;

                    var matrix = MatrixD.CreateTranslation(new Vector3D(0.0, 0.0, -0.05)) * actor.WorldMatrix;

                    if (lightData.PointLightOn)
                    {
                        var snapshot = new LightSnapshot
                        {
                            Id = actor.ID,
                            Matrix = matrix,
                            Color = lightData.PointLight.Color,
                            Intensity = lightData.PointIntensity * 10.0f
                        };
                        removedLights.Remove(AddLight(actor.ID, snapshot, lights, false));
                    }

                    if (lightData.SpotLightOn)
                    {
                        var snapshot = new LightSnapshot
                        {
                            Id = actor.ID,
                            Matrix = matrix,
                            Color = lightData.SpotLight.Light.Color,
                            Intensity = lightData.SpotIntensity,
                            Cone = new Vector2(lightData.SpotLight.ApertureCos * 0.9f, lightData.SpotLight.ApertureCos)
                        };
                        removedLights.Remove(AddLight(actor.ID, snapshot, lights, true));
                    }
                }
                catch (Exception ex)
                {
                    log.WriteLine($"Get Light Error={ex}");
                }

            lights.AddRange(removedLights.Select(id => new LightSnapshot { Remove = true, Id = id }));

            return lights;
        }

        private uint AddLight(uint actorId, LightSnapshot light, IList<LightSnapshot> lights, bool isSpot)
        {
            if (trackedLightIds.TryGetValue((actorId, isSpot), out var id))
            {
                if (trackedLights[id].Equals(light)) return id;
            }
            else
            {
                id = trackedLightId++;
                trackedLightIds.Add((actorId, isSpot), id);
            }

            lights.Add(light);
            trackedLights[id] = light;

            return id;
        }

        public void ExportLight(LightSnapshot light)
        {
            var id = trackedLightIds[(light.Id, light.Cone.HasValue)];
            if (light.Remove)
                writer.RemoveLight(id);
            else
                writer.Light(id, light.Matrix, light.Color * light.Intensity, light.Cone);
        }

        public void ExportEntity(EntitySnapshot entity)
        {
            if (!trackedEntityIds.TryGetValue(entity.EntityId, out var id))
            {
                id = trackedEntityId++;
                trackedEntityIds.Add(entity.EntityId, id);
            }

            uint? model = null;
            if (entity.Model != null)
                model = ProcessModel(entity.Model);

            writer.Entity(id, entity, model);
        }

        public void ExportGrid(GridSnapshot grid)
        {
            // log.WriteLine($"Export Grid {grid.Name}");

            var id = trackedEntityIds[grid.EntityId];

            var blocks = grid.Blocks.Select(Block).ToList();
            blocks.AddRange(grid.RemovedBlocks.Select(b => new BlockProperties
            {
                Remove = true,
                Position = b
            }));
            writer.Grid(id, grid.Scale, blocks);
        }

        private BlockProperties Block(BlockSnapshot block)
        {
            // log.WriteLine($"Process Block {slimBlock.BlockDefinition.Id}");

            var skin = block.Skin;
            var colorOverride = materialLibrary.GetColorOverride(skin);

            var color = colorOverride ?? block.Color;
            var hue = (byte)MathHelper.Clamp(color.X * 255.0, 0.0, 255.0);
            var sat = (byte)MathHelper.Clamp((color.Y + 1.0) * 127.5, 0.0, 255.0);
            var val = (byte)MathHelper.Clamp((color.Z + 1.0) * 127.5, 0.0, 255.0);

            var properties = new BlockProperties
            {
                EntityId = block.EntityId,
                Name = $"{block.Definition.Id.SubtypeName}:{block.Position}",
                Position = block.Position,
                Translation = block.Translation,
                Orientation = block.Orientation,
                Color = new Vector3UByte(hue, sat, val)
            };

            var cubeDefinition = block.Definition;

            uint? modelId = null;
            if (!string.IsNullOrWhiteSpace(block.Model))
                modelId = ProcessModel(block.Model);
            else if (cubeDefinition != null)
                modelId = MergeMultiModel(block.Definition);

            if (!modelId.HasValue) return properties;

            properties.Model = modelId;
            var modifiers = trackedModelMaterials[modelId.Value]
                .Select(m => new { id = trackedMaterialIds[m], mod = ProcessOverride(skin, m) })
                .Where(m => m.mod.HasValue)
                .Select(m => (m.id, m.mod.Value))
                .ToList();

            if (modifiers.Count > 0)
                properties.Modifiers = modifiers;

            return properties;
        }

        private uint MergeMultiModel(MyCubeBlockDefinition definition)
        {
            var key = definition.Id.SubtypeName;
            if (trackedModels.TryGetValue(key, out var id))
                return id;

            var cubeDefinition = definition.CubeDefinition;
            if (cubeDefinition == null)
                throw new ArgumentException("Block must have a cube definition");

            var tiles = MyCubeGridDefinitions.GetCubeTiles(definition);
            if (tiles == null || tiles.Length == 0)
                throw new ArgumentException("Block must have cube tiles");

            id = trackedModelId++;
            trackedModels.Add(key, id);

            log.WriteLine($"Merge MultiModel {key}");

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var texCoords = new List<Vector2>();
            var triangles = new List<Vector3I>();
            var meshes = new List<MeshInfo>();
            var materialNames = new List<string>();

            var vertexOffset = 0;
            var triangleOffset = 0;
            foreach (var (tile, asset) in tiles.Zip(cubeDefinition.Model, (t, m) => (t, m)))
            {
                var model = GetCubeModel(asset);
                if (model.Vertices.Length != model.TexCoords.Length)
                    throw new ArgumentException("Model must have equal number of vertices and tex coords");

                foreach (var pair in model.Vertices)
                {
                    var vertex = VF_Packer.UnpackPosition(pair.Position);
                    var normal = VF_Packer.UnpackNormal(pair.Normal);
                    vertices.Add(Vector3.Transform(vertex, tile.LocalMatrix));
                    normals.Add(Vector3.TransformNormal(normal, tile.LocalMatrix));
                }

                texCoords.AddRange(model.TexCoords.Select(texCoord => texCoord.ToVector2()));
                triangles.AddRange(model.Triangles.Select(triangle =>
                    new Vector3I(triangle.I0, triangle.I1, triangle.I2) + vertexOffset));

                foreach (var mesh in model.GetMeshList())
                {
                    meshes.Add(new MeshInfo
                    {
                        TriStart = (uint)(mesh.TriStart + triangleOffset), TriCount = (uint)mesh.TriCount,
                        Material = ProcessMaterial(mesh.Material)
                    });
                    if (mesh.Material.Name != null)
                        materialNames.Add(mesh.Material.Name);
                }

                vertexOffset = vertices.Count;
                triangleOffset = triangles.Count;
            }

            trackedModelMaterials.Add(id, materialNames.ToArray());

            writer.Model(id, key,
                w => vertices.ForEach(w.Write),
                w => normals.ForEach(w.Write),
                w => texCoords.ForEach(w.Write),
                w => triangles.ForEach(w.Write),
                w => meshes
                    .ForEach(t =>
                    {
                        w.Write(t.TriStart);
                        w.Write(t.TriCount);
                        w.Write(t.Material);
                    })
            );

            return id;
        }

        private MyModel GetCubeModel(string asset)
        {
            if (cubeModels.TryGetValue(asset, out var model))
                return model;

            model = new MyModel(asset)
            {
                LoadUV = true
            };
            model.LoadData();
            cubeModels.Add(asset, model);
            return model;
        }

        private uint ProcessModel(string asset)
        {
            if (trackedModels.TryGetValue(asset, out var id))
                return id;
            id = trackedModelId++;
            trackedModels.Add(asset, id);

            var model = new MyModel(asset)
            {
                LoadUV = true
            };
            model.LoadData();
            ProcessModel(model, id);
            model.UnloadData();
            return id;
        }

        private void ProcessModel(MyModel model, uint id)
        {
            var meshes = new List<MeshInfo>();
            var materialNames = new List<string>();

            log.WriteLine($"Process Model {model.AssetName}");

            foreach (var mesh in model.GetMeshList())
            {
                var material = mesh.Material;
                if (!SupportedDrawModes.Contains(material.DrawTechnique))
                    continue;

                var materialId = ProcessMaterial(material);

                if (material.Name != null)
                    materialNames.Add(material.Name);

                meshes.Add(new MeshInfo
                {
                    TriStart = (uint)mesh.TriStart,
                    TriCount = (uint)mesh.TriCount,
                    Material = materialId
                });
            }

            trackedModelMaterials.Add(id, materialNames.ToArray());

            writer.Model(id, model.AssetName,
                w => model.Vertices
                    .Select(v => VF_Packer.UnpackPosition(v.Position))
                    .ForEach(w.Write),
                w => model.Vertices
                    .Select(v => VF_Packer.UnpackNormal(v.Normal))
                    .ForEach(w.Write),
                w => model.TexCoords
                    .Select(v => v.ToVector2())
                    .ForEach(w.Write),
                w => model.Triangles
                    .ForEach(t =>
                    {
                        w.Write(t.I0);
                        w.Write(t.I1);
                        w.Write(t.I2);
                    }),
                w => meshes
                    .ForEach(t =>
                    {
                        w.Write(t.TriStart);
                        w.Write(t.TriCount);
                        w.Write(t.Material);
                    })
            );
        }

        private uint? ProcessOverride(string skin, string material)
        {
            var key = (skin, material);
            if (trackedOverrides.TryGetValue(key, out var cachedId))
                return cachedId;

            var modifier = materialLibrary.GetModifier(skin, material);
            if (modifier == null)
            {
                trackedOverrides.Add(key, null);
                return null;
            }

            var id = trackedMaterialId++;
            trackedOverrides.Add(key, id);

            log.WriteLine($"Process Override Skin={skin} Material={material}");

            writer.Material(id, new MaterialProperties
            {
                ColorMetal = ProcessTexture(modifier.ColorMetal),
                AddMaps = ProcessTexture(modifier.AddMaps),
                NormalGloss = ProcessTexture(modifier.NormalGloss),
                AlphaMask = ProcessTexture(modifier.AlphaMask)
            });

            return id;
        }

        private uint ProcessMaterial(MyMeshMaterial material)
        {
            if (material.Name == null)
                return 0;

            if (trackedMaterialIds.TryGetValue(material.Name, out var id))
                return id;
            id = trackedMaterialId++;
            trackedMaterialIds.Add(material.Name, id);

            log.WriteLine($"Process Material {material.Name}");
            log.WriteLine($"DrawTechnique={material.DrawTechnique} GlassCW={material.GlassCW} GlassCCW={material.GlassCCW} GlassSmooth={material.GlassSmooth}");

            var textures = MaterialLibrary.GetTextures(material);

            writer.Material(id, new MaterialProperties
            {
                ColorMetal = ProcessTexture(textures.ColorMetal),
                AddMaps = ProcessTexture(textures.AddMaps),
                NormalGloss = ProcessTexture(textures.NormalGloss),
                AlphaMask = ProcessTexture(textures.AlphaMask)
            });

            return id;
        }

        private uint ProcessTexture(string asset)
        {
            if (asset == null)
                return 0;

            if (trackedTextureIds.TryGetValue(asset, out var id))
                return id;
            id = trackedTextureId++;
            trackedTextureIds.Add(asset, id);

            log.WriteLine($"Process Texture {asset}");

            var path = Path.Combine(Path.GetDirectoryName(asset) ?? ".", Path.GetFileNameWithoutExtension(asset));

            writer.Texture(id, TextureType.Auto, asset, path, null);
            return id;
        }

        public static void GetSubParts(MiniLog log, IDictionary<string, MyEntitySubpart> dictionary, string prefix = "")
        {
            foreach (var pair in dictionary)
            {
                log.WriteLine($"{prefix}{pair.Key}:{pair.Value.Model.AssetName}");
                GetSubParts(log, pair.Value.Subparts, prefix + "  ");
            }
        }

        private class MeshInfo
        {
            public uint TriStart { get; set; }
            public uint TriCount { get; set; }
            public uint Material { get; set; }
        }
    }
}