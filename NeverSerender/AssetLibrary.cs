using Newtonsoft.Json;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using VRageRender;
using VRageRender.Models;

namespace NeverSerender
{
    public class AssetLibrary
    {
        private readonly string libraryPath;
        private readonly string contentPath;
        private readonly IDictionary<string, MaterialAsset> materials;

        public static AssetLibrary Open(string libraryPath, string contentPath)
        {
            var indexPath = Path.Combine(libraryPath, "index.json");
            var index = JsonConvert.DeserializeObject<Assets>(File.ReadAllText(indexPath));
            var materials = index.Materials.ToDictionary(m => m.Name);
            return new AssetLibrary(libraryPath, contentPath, materials);
        }

        public AssetLibrary(string libraryPath, string contentPath)
        {
            this.libraryPath = libraryPath;
            this.contentPath = contentPath;
            materials = new Dictionary<string, MaterialAsset>();
        }

        private AssetLibrary(string libraryPath, string contentPath, IDictionary<string, MaterialAsset> materials)
        {
            this.libraryPath = libraryPath;
            this.contentPath = contentPath;
            this.materials = materials;
        }

        public void Save()
        {
            var indexPath = Path.Combine(libraryPath, "index.json");
            var assets = new Assets
            {
                Materials = materials.Select(p => p.Value).ToList(),
            };
            var index = JsonConvert.SerializeObject(assets);
            File.WriteAllText(indexPath, index);
        }

        public MaterialFiles GetMaterial(MyMeshMaterial source)
        {
            try
            {
                return GetMaterialInternal(source);
            }
            catch
            {
                return null;
            }
        }

        private MaterialFiles GetMaterialInternal(MyMeshMaterial source)
        {
            if (!materials.TryGetValue(source.Name, out var asset))
                return null;

            var colorPath = Path.Combine(libraryPath, "Textures", asset.Color);
            var normalPath = Path.Combine(libraryPath, "Textures", asset.Normal);
            var roughMetalPath = Path.Combine(libraryPath, "Textures", asset.RoughMetal);

            if (!HashExport(colorPath, normalPath, roughMetalPath).SequenceEqual(asset.ExportHash))
                return null;

            var colorMetalAsset = source.Textures.Get("ColorMetalTexture");
            var normalGlossAsset = source.Textures.Get("NormalGlossTexture");
            //var addMapsAsset = source.Textures.Get("AddMapsTexture");
            var alphaMaskAsset = source.Textures.Get("AlphamaskTexture");

            string colorMetalPath = null;
            string normalGlossPath = null;
            string alphaMaskPath = null;
            if (!string.IsNullOrEmpty(colorMetalAsset))
                colorMetalPath = Path.Combine(contentPath, colorMetalAsset);
            if (!string.IsNullOrEmpty(normalGlossAsset))
                normalGlossPath = Path.Combine(contentPath, normalGlossAsset);
            if (!string.IsNullOrEmpty(alphaMaskAsset))
                alphaMaskPath = Path.Combine(contentPath, alphaMaskAsset);

            if (!HashSource(colorMetalPath, normalGlossPath, alphaMaskPath).SequenceEqual(asset.SourceHash))
                return null;

            return new MaterialFiles
            {
                Name = source.Name,
                ColorPath = colorPath,
                NormalPath = normalPath,
                RoughMetalPath = roughMetalPath,
            };
        }

        public MaterialFiles AddMaterial(MyMeshMaterial source, Material processed)
        {
            if (materials.ContainsKey(source.Name))
                throw new InvalidOperationException("Material has already been added");

            var dirPath = Path.Combine(libraryPath, "Textures");
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            var colorPath = Path.Combine(dirPath, processed.Name + "_color.png");
            var normalPath = Path.Combine(dirPath, processed.Name + "_normal.png");
            var roughMetalPath = Path.Combine(dirPath, processed.Name + "_pbr.png");
            using (var file = File.OpenWrite(colorPath))
                processed.Color.SaveAsPng(file);
            using (var file = File.OpenWrite(normalPath))
                processed.Normal.SaveAsPng(file);
            using (var file = File.OpenWrite(roughMetalPath))
                processed.RoughMetal.SaveAsPng(file);

            var exportHash = HashExport(colorPath, normalPath, roughMetalPath);

            var colorMetalAsset = source.Textures.Get("ColorMetalTexture");
            var normalGlossAsset = source.Textures.Get("NormalGlossTexture");
            //var addMapsAsset = source.Textures.Get("AddMapsTexture");
            var alphaMaskAsset = source.Textures.Get("AlphamaskTexture");

            string colorMetalPath = null;
            string normalGlossPath = null;
            string alphaMaskPath = null;
            if (!string.IsNullOrEmpty(colorMetalAsset))
                colorMetalPath = Path.Combine(contentPath, colorMetalAsset);
            if (!string.IsNullOrEmpty(normalGlossAsset))
                normalGlossPath = Path.Combine(contentPath, normalGlossAsset);
            if (!string.IsNullOrEmpty(alphaMaskAsset))
                alphaMaskPath = Path.Combine(contentPath, alphaMaskAsset);

            var sourceHash = HashSource(colorMetalPath, normalGlossPath, alphaMaskPath);

            var asset = new MaterialAsset
            {
                Name = processed.Name,
                Color = processed.Name + "_color.png",
                Normal = processed.Name + "_normal.png",
                RoughMetal = processed.Name + "_pbr.png",
                ExportHash = exportHash,
                SourceHash = sourceHash,
            };

            materials.Add(asset.Name, asset);

            return new MaterialFiles
            {
                Name = source.Name,
                ColorPath = colorPath,
                NormalPath = normalPath,
                RoughMetalPath = roughMetalPath,
            };
        }

        private byte[] HashExport(string colorPath, string normalPath, string roughMetalPath)
        {

            using (var md5 = MD5.Create())
            {
                var nameBytes = Encoding.ASCII.GetBytes("color");
                md5.TransformBlock(nameBytes, 0, nameBytes.Length, nameBytes, 0);
                // TODO: Compute hash in a streaming way
                var contentBytes = File.ReadAllBytes(colorPath);
                md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);

                nameBytes = Encoding.ASCII.GetBytes("normal");
                md5.TransformBlock(nameBytes, 0, nameBytes.Length, nameBytes, 0);
                contentBytes = File.ReadAllBytes(normalPath);
                md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);

                nameBytes = Encoding.ASCII.GetBytes("pbr");
                md5.TransformBlock(nameBytes, 0, nameBytes.Length, nameBytes, 0);
                contentBytes = File.ReadAllBytes(roughMetalPath);
                md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);

                return md5.Hash;
            }
        }

        private byte[] HashSource(string colorMetalPath, string normalGlossPath, string alphaMaskPath)
        {

            using (var md5 = MD5.Create())
            {
                if (colorMetalPath != null)
                {
                    var nameBytes = Encoding.ASCII.GetBytes("color");
                    md5.TransformBlock(nameBytes, 0, nameBytes.Length, nameBytes, 0);
                    // TODO: Compute hash in a streaming way
                    var contentBytes = File.ReadAllBytes(colorMetalPath);
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
                }
                if (normalGlossPath != null)
                {
                    var nameBytes = Encoding.ASCII.GetBytes("normal");
                    md5.TransformBlock(nameBytes, 0, nameBytes.Length, nameBytes, 0);
                    var contentBytes = File.ReadAllBytes(normalGlossPath);
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
                }
                if (alphaMaskPath != null)
                {
                    var nameBytes = Encoding.ASCII.GetBytes("alpha");
                    md5.TransformBlock(nameBytes, 0, nameBytes.Length, nameBytes, 0);
                    var contentBytes = File.ReadAllBytes(alphaMaskPath);
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
                }
                var finalBytes = Encoding.ASCII.GetBytes("AssetLibrary");
                md5.TransformFinalBlock(finalBytes, 0, finalBytes.Length);

                return md5.Hash;
            }
        }

        private class Assets
        {
            public IList<MaterialAsset> Materials { get; set; }
        }

        private class MaterialAsset
        {
            public string Name { get; set; }
            public string Color { get; set; }
            public string Normal { get; set; }
            public string RoughMetal { get; set; }
            public byte[] SourceHash { get; set; }
            public byte[] ExportHash { get; set; }
        }
    }
}
