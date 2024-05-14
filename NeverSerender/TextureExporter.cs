using Pfim;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Threading.Tasks;
using VRageMath;

namespace NeverSerender
{
    public class TextureExporter
    {
        private readonly string contentPath;
        private readonly MiniLog log;

        public TextureExporter(string contentPath, MiniLog log)
        {
            this.contentPath = contentPath;
            this.log = log;
        }

        public Material ProcessColor(string colorMetalAsset, string addMapsAsset, string alphaMaskAsset, Vector3 colorMask)
        {
            if (colorMetalAsset == null)
                throw new ArgumentNullException(nameof(colorMetalAsset));
            if (addMapsAsset == null)
                throw new ArgumentNullException(nameof(addMapsAsset));

            log.WriteLine("Loading textures...");
            var colorMetalTexture = LoadDDS<Rgba32>(colorMetalAsset, ImageFormat.Rgba32);
            var addMapsTexture = LoadDDS<Rgba32>(addMapsAsset, ImageFormat.Rgba32);
            var alphaMaskTexture = LoadDDS<Gray8>(alphaMaskAsset, ImageFormat.Rgb8);

            var width = colorMetalTexture.Width;
            var height = colorMetalTexture.Height;

            var addMapsScale = (float)addMapsTexture.Width / width;
            var alphaMaskScale = 1f;
            if (alphaMaskTexture != null)
                alphaMaskScale = (float)alphaMaskTexture.Width / width;

            var colorTexture = new Image<Rgba32>(width, height);

            log.WriteLine("Running pixel converters...");
            Parallel.For(0, height, y =>
            {
                for (var x = 0; x < width; x++)
                {
                    // TODO: Add linear interpolation instead of nearest
                    var addMapsX = (int)(x * addMapsScale);
                    var addMapsY = (int)(y * addMapsScale);
                    var alphaMaskX = (int)(x * alphaMaskScale);
                    var alphaMaskY = (int)(y * alphaMaskScale);

                    try
                    {
                        var colorMetal = colorMetalTexture[x, y];
                        var addMaps = addMapsTexture[addMapsX, addMapsY];
                        var alphaMask = alphaMaskTexture?[alphaMaskX, alphaMaskY] ?? new Gray8(255);
                        colorTexture[x, y] = RenderTexture.Process(colorMetal, addMaps, alphaMask, colorMask);
                    }
                    catch
                    {
                        // There must be a race condition somewhere causing some pixels to be "Out of Range"
                        // Doesn't happen when running single threaded (no Parallel.ForEach)
                        log.WriteLine($"Error at pixel x={x} y={y} w={width} h={height}");
                    }
                }
            });

            log.WriteLine("Texture conversion done");

            return new Material
            {
                Color = colorTexture,
            };
        }

        public Material ProcessPhysical(string colorMetalAsset, string normalGlossAsset)
        {
            if (colorMetalAsset == null)
                throw new ArgumentNullException(nameof(colorMetalAsset));

            log.WriteLine("Loading textures...");
            var colorMetalTexture = LoadDDS<Rgba32>(colorMetalAsset, ImageFormat.Rgba32);
            var normalGlossTexture = LoadDDS<Rgba32>(normalGlossAsset, ImageFormat.Rgba32);

            var width = colorMetalTexture.Width;
            var height = colorMetalTexture.Height;

            var normalGlossScale = 1f;
            if (normalGlossTexture != null)
                normalGlossScale = normalGlossTexture.Width / width;

            var normalTexture = new Image<Rgb24>(width, height);
            var roughMetalTexture = new Image<Rgb24>(width, height);

            log.WriteLine("Running pixel converters...");
            Parallel.For(0, height, y =>
            {
                for (var x = 0; x < width; x++)
                {
                    // TODO: Add linear interpolation instead of nearest
                    var normalGlossX = (int)(x * normalGlossScale);
                    var normalGlossY = (int)(y * normalGlossScale);

                    try
                    {
                        var colorMetal = colorMetalTexture[x, y];
                        var normal = new Rgb24(127, 127, 255);
                        var roughMetal = new Rgb24(0, 255, colorMetal.A);
                        if (normalGlossTexture != null)
                        {
                            var normalGloss = normalGlossTexture[normalGlossX, normalGlossY];
                            normal = new Rgb24(normalGloss.B, normalGloss.G, normalGloss.R);
                            roughMetal.G = (byte)(255 - normalGloss.A);
                        }

                        normalTexture[x, y] = normal;
                        roughMetalTexture[x, y] = roughMetal;
                    }
                    catch
                    {
                        log.WriteLine($"Error at pixel x={x} y={y} w={width} h={height}");
                    }
                }
            });

            log.WriteLine("Texture conversion done");

            return new Material
            {
                Normal = normalTexture,
                RoughMetal = roughMetalTexture,
            };
        }

        private Image<TPixel> LoadDDS<TPixel>(string asset, ImageFormat expected) where TPixel : struct, IPixel<TPixel>
        {
            if (asset == null)
                return null;

            log.WriteLine($"Loading DDS asset={asset}");
            var path = Path.Combine(contentPath, asset).ToLower();
            var dds = (CompressedDds)Pfimage.FromFile(path);
            if (dds.Format != expected)
                throw new NotSupportedException($"Only {expected} is currently supported for texture {asset}");
            //var data = new byte[dds.DataLen];
            //Array.Copy(dds.Data, data, data.Length);
            var data = dds.Data.Span(0, dds.DataLen);
            log.WriteLine($"Loading pixel data Length={dds.Data.Length} DataLen={dds.DataLen} NewLength={data.Length} Width={dds.Width} Height={dds.Height}");
            return Image.LoadPixelData<TPixel>(data, dds.Width, dds.Height);
        }
    }
}
