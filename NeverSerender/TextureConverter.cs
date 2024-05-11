using Pfim;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using VRageRender;
using VRageRender.Models;

namespace ClientPlugin
{
    public class TextureConverter
    {
        private readonly string contentPath;
        private readonly Dictionary<string, Image<Rgba32>> colorMetalTextures;
        private readonly Dictionary<string, Image<Rgba32>> normalGlossTextures;
        private readonly Dictionary<string, Image<Rgba32>> addMapsTextures;
        private readonly Dictionary<string, Image<Gray8>> alphaMaskTextures;

        public TextureConverter(string contentPath)
        {
            this.contentPath = contentPath;
            colorMetalTextures = new Dictionary<string, Image<Rgba32>>();
            normalGlossTextures = new Dictionary<string, Image<Rgba32>>();
            addMapsTextures = new Dictionary<string, Image<Rgba32>>();
            alphaMaskTextures = new Dictionary<string, Image<Gray8>>();
        }

        private readonly IDictionary<string, MeshMaterial> processedMaterials = new Dictionary<string, MeshMaterial>();

        public MeshMaterial ProcessMaterial(MyMeshMaterial material)
        {
            if (processedMaterials.TryGetValue(material.Name, out var value))
                return null;

            var colorMetalAsset = material.Textures.Get("ColorMetalTexture");
            var normalGlossAsset = material.Textures.Get("NormalGlossTexture");
            var addMapsAsset = material.Textures.Get("AddMapsTexture");
            var alphaMaskAsset = material.Textures.Get("AlphamaskTexture");

            var colorMetalTexture = LoadDDS<Rgba32>(colorMetalAsset, ImageFormat.Rgba32);
            var normalGlossTexture = LoadDDS<Rgba32>(normalGlossAsset, ImageFormat.Rgba32);
            //var addMapsTexture = LoadDDS<Rgba32>(addMapsAsset, ImageFormat.Rgba32);
            var alphaMaskTexture = LoadDDS<Gray8>(alphaMaskAsset, ImageFormat.Rgb8);

            var width = colorMetalTexture.Width;
            var height = colorMetalTexture.Height;

            if (normalGlossTexture != null && normalGlossTexture.Width != width && normalGlossTexture.Height != height)
                throw new ArgumentException("normal-gloss texture has a different size");
            //if (addMapsTexture != null && addMapsTexture.Width != width && addMapsTexture.Height != height)
            //    throw new ArgumentException("addmaps texture has a different size");
            if (alphaMaskTexture != null && alphaMaskTexture.Width != width && alphaMaskTexture.Height != height)
                throw new ArgumentException("alphamask texture has a different size");

            var colorAlphaTexture = new Image<Rgba32>(width, height);
            var normalTexture = new Image<Rgb24>(width, height);
            var roughMetalTexture = new Image<Rgb24>(width, height);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var colorMetal = colorMetalTexture[x, y];
                    var alpha = alphaMaskTexture?[x, y].PackedValue ?? 255;
                    var RoughMetal = new Rgb24(0, 255, colorMetal.A);
                    var colorAlpha = new Rgba32(colorMetal.B, colorMetal.G, colorMetal.R, alpha);
                    if (normalGlossTexture != null)
                    {
                        var normalGloss = normalGlossTexture[x, y];
                        normalTexture[x, y] = new Rgb24(normalGloss.B, normalGloss.G, normalGloss.R);
                        RoughMetal.G = (byte)(255 - normalGloss.A);
                    }
                    //if (addMapsTexture != null)
                    //{
                    //    var addMaps = addMapsTexture[x, y];
                    //    var r = alpha * addMaps.R + (255 - alpha) * colorAlpha.R;
                    //    var g = alpha * addMaps.G + (255 - alpha) * colorAlpha.G;
                    //    var b = alpha * addMaps.B + (255 - alpha) * colorAlpha.B;
                    //    var a = alpha * addMaps.A + (255 - alpha) * colorAlpha.A;
                    //    colorAlpha = new Rgba32(
                    //        (byte)Math.Max(r / 255, 255),
                    //        (byte)Math.Max(g / 255, 255),
                    //        (byte)Math.Max(b / 255, 255),
                    //        (byte)Math.Max(a / 255, 255)
                    //    );
                    //}
                    colorAlphaTexture[x, y] = colorAlpha;
                    roughMetalTexture[x, y] = RoughMetal;
                }
            }

            var processed = new MeshMaterial
            {
                Name = material.Name,
                Color = colorAlphaTexture,
                Normal = normalGlossTexture != null ? normalTexture : null,
                RoughMetal = roughMetalTexture,
            };
            processedMaterials.Add(material.Name, processed);
            return processed;
        }

        private Image<TPixel> LoadDDS<TPixel>(string asset, ImageFormat expected) where TPixel : struct, IPixel<TPixel>
        {
            if (asset == null)
                return null;
            var path = Path.Combine(contentPath, asset).ToLower();
            var dds = Pfimage.FromFile(path);
            dds.Decompress();
            if (dds.Format != expected)
                throw new NotSupportedException($"Only {expected} is currently supported for texture {asset}");
            return Image.LoadPixelData<TPixel>(dds.Data, dds.Width, dds.Height);
        }

        public class MeshMaterial
        {
            public void Save(string dir)
            {
                var colorPath = Path.Combine(dir, Name + "_color.png");
                var normalPath = Path.Combine(dir, Name + "_normal.png");
                var roughMetalPath = Path.Combine(dir, Name + "_pbr.png");
                using (var file = File.OpenWrite(colorPath))
                    Color.SaveAsPng(file);
                using (var file = File.OpenWrite(normalPath))
                    Normal.SaveAsPng(file);
                using (var file = File.OpenWrite(roughMetalPath))
                    RoughMetal.SaveAsPng(file);
            }

            public string Name { get; set; }
            public Image<Rgba32> Color { get; set; }
            public Image<Rgb24> Normal { get; set; }
            public Image<Rgb24> RoughMetal { get; set; }
        }
    }
}
