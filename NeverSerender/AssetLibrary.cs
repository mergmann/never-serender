using Newtonsoft.Json;
using Sandbox.Definitions;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VRageMath;
using VRageRender.Messages;
using VRageRender.Models;

namespace NeverSerender
{
    public class AssetLibrary
    {
        private readonly string libraryPath;
        private uint current;
        private readonly Dictionary<ColorTextureIdentifier, string> colorTextures;
        private readonly Dictionary<PhysicalTextureIdentifier, PhysicalTextures> physicalTextures;

        private readonly List<(ColorTextureIdentifier, string)> colorToExport;
        private readonly List<(PhysicalTextureIdentifier, string)> physicalToExport;

        private readonly Dictionary<AssetModifierIdentifier, AssetModifier> modifiers;

        private readonly MiniLog log;

        public static AssetLibrary OpenOrNew(string libraryPath, MiniLog log)
        {
            try
            {
                return Open(libraryPath, log);
            }
            catch
            {
                return new AssetLibrary(libraryPath, log);
            }
        }

        public static AssetLibrary Open(string libraryPath, MiniLog log)
        {
            var indexPath = Path.Combine(libraryPath, "index.json");
            var index = JsonConvert.DeserializeObject<AssetsIndex>(File.ReadAllText(indexPath));
            var colorTextures = index.Color.ToDictionary(
                t => new ColorTextureIdentifier(t.ColorMetal, t.AddMaps, t.AlphaMask, ConvertColor(t.Color)),
                t => t.Ident
            );
            var physicalTextures = index.Physical.ToDictionary(
                t => new PhysicalTextureIdentifier(t.ColorMetal, t.NormalGloss),
                t => new PhysicalTextures { Normal = t.NormalIdent, RoughMetal = t.RoughMetalIdent }
            );
            return new AssetLibrary(libraryPath, index.Current, colorTextures, physicalTextures, log);
        }

        public AssetLibrary(string libraryPath, MiniLog log) : this(
                libraryPath,
                0,
                new Dictionary<ColorTextureIdentifier, string>(),
                new Dictionary<PhysicalTextureIdentifier, PhysicalTextures>(),
                log)
        { }

        private AssetLibrary(
            string libraryPath,
            uint current,
            Dictionary<ColorTextureIdentifier, string> colorTextures,
            Dictionary<PhysicalTextureIdentifier, PhysicalTextures> physicalTextures,
            MiniLog log)
        {
            this.libraryPath = libraryPath;

            this.current = current;
            this.colorTextures = colorTextures;
            this.physicalTextures = physicalTextures;
            this.log = log;

            colorToExport = new List<(ColorTextureIdentifier, string)>();
            physicalToExport = new List<(PhysicalTextureIdentifier, string)>();

            modifiers = new Dictionary<AssetModifierIdentifier, AssetModifier>();
            foreach (var modifier in MyDefinitionManager.Static.GetAssetModifierDefinitions())
            {
                foreach (var texture in modifier.Textures)
                {
                    var identifier = new AssetModifierIdentifier(texture.Location, modifier.Id.SubtypeName);
                    if (!modifiers.TryGetValue(identifier, out var value))
                    {
                        value = new AssetModifier();
                        modifiers.Add(identifier, value);
                    }
                    switch (texture.Type)
                    {
                        case MyTextureType.ColorMetal: value.ColorMetal = texture.Filepath; break;
                        case MyTextureType.NormalGloss: value.NormalGloss = texture.Filepath; break;
                        case MyTextureType.Extensions: value.AddMaps = texture.Filepath; break;
                        case MyTextureType.Alphamask: value.AlphaMask = texture.Filepath; break;
                    }
                }
            }
            foreach (var modifier in modifiers)
                log.WriteLine($"Modifier Asset={modifier.Key.asset} Skin={modifier.Key.skin}");
        }

        public void Save()
        {
            var indexPath = Path.Combine(libraryPath, "index.json");
            var assets = new AssetsIndex
            {
                Color = colorTextures
                    .Select(t => new AssetsColor
                    {
                        ColorMetal = t.Key.colorMetal,
                        AddMaps = t.Key.addMaps,
                        AlphaMask = t.Key.alphaMask,
                        Color = ConvertColor(t.Key.color),
                        Ident = t.Value
                    })
                    .ToList(),
                Physical = physicalTextures
                    .Select(t => new AssetsPhysics
                    {
                        ColorMetal = t.Key.colorMetal,
                        NormalGloss = t.Key.normalGloss,
                        NormalIdent = t.Value.Normal,
                        RoughMetalIdent = t.Value.RoughMetal
                    })
                    .ToList(),
                Current = current,
            };
            var index = JsonConvert.SerializeObject(assets);
            File.WriteAllText(indexPath, index);
        }

        public MaterialFiles GetMaterial(MyMeshMaterial source, string skin, Vector3 color)
        {
            log.WriteLine($"Material Get Name={source.Name} Skin={skin} Color={color}");

            foreach (var pair in source.Textures)
                log.WriteLine($"texture Type={pair.Key} Value={pair.Value}");

            source.Textures.TryGetValue("ColorMetalTexture", out var colorMetal);
            source.Textures.TryGetValue("AddMapsTexture", out var addMaps);
            source.Textures.TryGetValue("NormalGlossTexture", out var normalGloss);
            source.Textures.TryGetValue("AlphamaskTexture", out var alphaMask);

            var identifier = new AssetModifierIdentifier(source.Name, skin);
            if (modifiers.TryGetValue(identifier, out var modifier))
            {
                log.WriteLine($"Modifier found");
                if (modifier.ColorMetal != null)
                    colorMetal = modifier.ColorMetal;
                if (modifier.AddMaps != null)
                    addMaps = modifier.AddMaps;
                if (modifier.NormalGloss != null)
                    normalGloss = modifier.NormalGloss;
                if (modifier.AlphaMask != null)
                    alphaMask = modifier.AlphaMask;
            }

            if (colorMetal == null)
                throw new ArgumentNullException("ColorMetal Texture");
            if (addMaps == null)
                throw new ArgumentNullException("AddMaps Texture");
            if (normalGloss == null)
                throw new ArgumentNullException("NormalGloss Texture");

            var colorIdentifier = new ColorTextureIdentifier(colorMetal, addMaps, alphaMask, color);
            if (!colorTextures.TryGetValue(colorIdentifier, out var colorTexture))
            {
                colorTexture = NextIdentifier();
                colorToExport.Add((colorIdentifier, colorTexture));
                colorTextures.Add(colorIdentifier, colorTexture);
            }

            var physicalIdentifier = new PhysicalTextureIdentifier(colorMetal, normalGloss);
            if (!physicalTextures.TryGetValue(physicalIdentifier, out var physicalTexture))
            {
                var name = NextIdentifier();
                physicalTexture = new PhysicalTextures
                {
                    Normal = name,
                    RoughMetal = name,
                };
                physicalToExport.Add((physicalIdentifier, name));
                physicalTextures.Add(physicalIdentifier, physicalTexture);
            }

            var colorPath = Path.Combine(libraryPath, "Color", colorTexture + ".png");
            var normalPath = Path.Combine(libraryPath, "Normal", physicalTexture.Normal + ".png");
            var roughMetalPath = Path.Combine(libraryPath, "Physical", physicalTexture.RoughMetal + ".png");

            return new MaterialFiles
            {
                ColorPath = colorPath,
                NormalPath = normalPath,
                RoughMetalPath = roughMetalPath,
            };
        }

        public void ExportMissing(TextureExporter exporter)
        {
            var colorDir = Path.Combine(libraryPath, "Color");
            if (!Directory.Exists(colorDir))
                Directory.CreateDirectory(colorDir);
            var normalDir = Path.Combine(libraryPath, "Normal");
            if (!Directory.Exists(normalDir))
                Directory.CreateDirectory(normalDir);
            var roughMetalDir = Path.Combine(libraryPath, "Physical");
            if (!Directory.Exists(roughMetalDir))
                Directory.CreateDirectory(roughMetalDir);

            // Render color textures
            Parallel.ForEach(colorToExport, t =>
            {
                //foreach (var t in colorToExport)
                //{
                var material = exporter.ProcessColor(t.Item1.colorMetal, t.Item1.addMaps, t.Item1.alphaMask, t.Item1.color);
                var colorPath = Path.Combine(libraryPath, "Color", t.Item2 + ".png");

                using (var file = File.OpenWrite(colorPath))
                    material.Color.SaveAsPng(file);
                //}
            });
            colorToExport.Clear();

            // Render physical textures (normal, roughness, metalness)
            // Parallelizing since texture loading is very slow
            Parallel.ForEach(physicalToExport, t =>
            {
                //    foreach (var t in physicalToExport)
                //{
                var material = exporter.ProcessPhysical(t.Item1.colorMetal, t.Item1.normalGloss);
                var normalPath = Path.Combine(libraryPath, "Normal", t.Item2 + ".png");
                var roughMetalPath = Path.Combine(libraryPath, "Physical", t.Item2 + ".png");

                using (var file = File.OpenWrite(normalPath))
                    material.Normal.SaveAsPng(file);
                using (var file = File.OpenWrite(roughMetalPath))
                    material.RoughMetal.SaveAsPng(file);
            });
            //}
            physicalToExport.Clear();
        }

        private string NextIdentifier() => $"{current++:x8}";

        private static Vector3 ConvertColor(float[] color) => new Vector3(color[0], color[1], color[2]);
        private static float[] ConvertColor(Vector3 color) => new float[] { color.X, color.Y, color.Z };

        private class AssetModifier
        {
            public string ColorMetal { get; set; }
            public string NormalGloss { get; set; }
            public string AddMaps { get; set; }
            public string AlphaMask { get; set; }
        }

        private class PhysicalTextures
        {
            public string Normal { get; set; }
            public string RoughMetal { get; set; }
        }

        private class AssetModifierIdentifier : IEquatable<AssetModifierIdentifier>
        {
            public readonly string asset;
            public readonly string skin;

            public AssetModifierIdentifier(string asset, string skin)
            {
                this.asset = asset;
                this.skin = skin;
            }

            // https://stackoverflow.com/a/263416
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (int)2166136261;
                    hash = (hash * 16777619) ^ asset.GetHashCode();
                    return (hash * 16777619) ^ skin.GetHashCode();
                }
            }

            public bool Equals(AssetModifierIdentifier other)
            {
                return asset == other.asset
                    && skin == other.skin;
            }
        }

        private class ColorTextureIdentifier : IEquatable<ColorTextureIdentifier>
        {
            public readonly string colorMetal;
            public readonly string addMaps;
            public readonly string alphaMask;
            public readonly Vector3 color;

            public ColorTextureIdentifier(string colorMetal, string addMaps, string alphaMask, Vector3 color)
            {
                this.colorMetal = colorMetal;
                this.addMaps = addMaps;
                this.alphaMask = alphaMask;
                this.color = color;
            }

            // https://stackoverflow.com/a/263416
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (int)2166136261;
                    hash = (hash * 16777619) ^ colorMetal.GetHashCode();
                    hash = (hash * 16777619) ^ addMaps.GetHashCode();
                    hash = (hash * 16777619) ^ alphaMask?.GetHashCode() ?? 0x484EACB5;
                    return (hash * 16777619) ^ color.GetHashCode();
                }
            }

            public bool Equals(ColorTextureIdentifier other)
            {
                return colorMetal == other.colorMetal
                    && addMaps == other.addMaps
                    && alphaMask == other.alphaMask
                    && color == other.color;
            }
        }

        private class PhysicalTextureIdentifier : IEquatable<PhysicalTextureIdentifier>
        {
            public readonly string colorMetal;
            public readonly string normalGloss;

            public PhysicalTextureIdentifier(string colorMetal, string normalGloss)
            {
                this.colorMetal = colorMetal;
                this.normalGloss = normalGloss;
            }

            // https://stackoverflow.com/a/263416
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (int)2166136261;
                    hash = (hash * 16777619) ^ colorMetal.GetHashCode();
                    return (hash * 16777619) ^ normalGloss.GetHashCode();
                }
            }

            public bool Equals(PhysicalTextureIdentifier other)
            {
                return colorMetal == other.colorMetal
                    && normalGloss == other.normalGloss;
            }
        }

        private class AssetsIndex
        {
            public IList<AssetsColor> Color { get; set; }
            public IList<AssetsPhysics> Physical { get; set; }
            public uint Current { get; set; }
        }

        private class AssetsColor
        {
            public string ColorMetal { get; set; }
            public string AddMaps { get; set; }
            public string AlphaMask { get; set; }
            public float[] Color { get; set; }
            public string Ident { get; set; }
        }

        private class AssetsPhysics
        {
            public string ColorMetal { get; set; }
            public string NormalGloss { get; set; }
            public string NormalIdent { get; set; }
            public string RoughMetalIdent { get; set; }
        }
    }
}
