using Pfim;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using VRageRender;
using VRageRender.Models;

namespace NeverSerender
{
    public class TextureExporter
    {
        private readonly string contentPath;

        public TextureExporter(string contentPath)
        {
            this.contentPath = contentPath;
        }

        private readonly IDictionary<string, Material> processedMaterials = new Dictionary<string, Material>();

        public bool ProcessMaterial(MyMeshMaterial material, out Material processed)
        {
            if (processedMaterials.TryGetValue(material.Name, out var value))
            {
                processed = value;
                return false;
            }

            var colorMetalAsset = material.Textures.Get("ColorMetalTexture");
            var normalGlossAsset = material.Textures.Get("NormalGlossTexture");
            //var addMapsAsset = material.Textures.Get("AddMapsTexture");
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

            var colorTexture = new Image<Rgba32>(width, height);
            var normalTexture = new Image<Rgb24>(width, height);
            var roughMetalTexture = new Image<Rgb24>(width, height);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var colorMetal = colorMetalTexture[x, y];
                    var alpha = alphaMaskTexture?[x, y].PackedValue ?? 255;
                    var color = new Rgba32(colorMetal.B, colorMetal.G, colorMetal.R, alpha);
                    var normal = new Rgb24(127, 127, 255);
                    var roughMetal = new Rgb24(0, 255, colorMetal.A);
                    if (normalGlossTexture != null)
                    {
                        var normalGloss = normalGlossTexture[x, y];
                        normal = new Rgb24(normalGloss.B, normalGloss.G, normalGloss.R);
                        roughMetal.G = (byte)(255 - normalGloss.A);
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
                    colorTexture[x, y] = color;
                    normalTexture[x, y] = normal;
                    roughMetalTexture[x, y] = roughMetal;
                }
            }

            processed = new Material
            {
                Name = material.Name,
                Color = colorTexture,
                Normal = normalTexture,
                RoughMetal = roughMetalTexture,
            };
            processedMaterials.Add(material.Name, processed);
            return true;
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
    }
}
