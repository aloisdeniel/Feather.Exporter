using System;
using System.Net;
using System.Linq;
using System.IO;
using SkiaSharp;
using System.Collections.Generic;

namespace Feather.Exporter
{
    class MainClass
    {
        private static readonly Dictionary<string, float> densities = new Dictionary<string, float>()
        {
            { "@", 1.0f }, 
            { "@2x", 2.0f }, 
            { "@3x", 3.0f },
            { "@mdpi", 1.0f }, 
            { "@hdpi", 2.0f }, 
            { "@xhdpi", 3.0f },
        };

        public static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the feather great exporter!");
            Console.WriteLine("--------------------------------------\n");

            while(true)
            {
                Console.WriteLine("Which icons do you want (separated by ',')?");
                var icons = Console.ReadLine().Split(',').Select(x => x.Trim());

                Console.WriteLine("In which directory(default: current working directory)?");
                var folder = Console.ReadLine().Trim();
                if (string.IsNullOrEmpty(folder))
                    folder = Directory.GetCurrentDirectory();

                Console.WriteLine("What is the icon suffix (default: none)?");
                var suffix = Console.ReadLine().Trim();
                if (!string.IsNullOrEmpty(suffix))
                    suffix = $"_{suffix}";

                Console.WriteLine("In which color (default: #000000)?");
                var colorArg = Console.ReadLine().Trim();
                SKColor color;
                if (!SKColor.TryParse(colorArg, out color))
                    color = SKColors.Black;

                Console.WriteLine("In which size (default: 24)?");
                var sizeArg = Console.ReadLine().Trim();
                int size;
                if (!int.TryParse(sizeArg, out size))
                    size = 24;
     
                foreach (var icon in icons)
                {
                    var client = new WebClient();
                    var svg = client.DownloadString($"https://raw.githubusercontent.com/colebemis/feather/master/icons/{icon}.svg");
                    svg = svg.Replace("stroke=\"#000\"", $"stroke=\"#{color.ToString().Substring(3)}\"");
                    svg = svg.Replace("fill=\"none\"", "fill-opacity=\"0.4\"");

                    foreach (var density in densities)
                    {
                        var path = Path.Combine(folder, $"ic_icon{suffix}{density.Key}.png");
                        Export(path, (int)(size * density.Value), svg);
                    }
                }

                Console.WriteLine("Do you want to generate other icons (default: 'no')?");
                var shouldContinue = Console.ReadLine().Trim().ToLower();

                if (shouldContinue == "n" || shouldContinue == "no")
                    break;
            }
        }

        public static void Export(string path, int size, string iconSvg)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);
                writer.Write(iconSvg);
                writer.Flush();
                stream.Position = 0;

                var svg = new SkiaSharp.Extended.Svg.SKSvg();
                svg.Load(stream);

                float svgMax = Math.Max(svg.Picture.CullRect.Width, svg.Picture.CullRect.Height);
                float scale = size / svgMax;
                var matrix = SKMatrix.MakeScale(scale, scale);

                using(var bitmap = new SKBitmap(size,size))
                using(var canvas = new SKCanvas(bitmap))
                {
                    canvas.Clear(SKColors.Transparent);
                    canvas.DrawPicture(svg.Picture, ref matrix);

                    using (var image = SKImage.FromBitmap(bitmap))
                    using (var data = image.Encode(SKEncodedImageFormat.Png, 80))
                    {
                        using (var output = File.OpenWrite(path))
                        {
                            data.SaveTo(output);
                            Console.WriteLine($"Saved icon to : {path}");
                        }
                    }
                }
            }
        }
    }
}
