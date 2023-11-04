using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Yonmoku-WPF
{
    class MaterialLoader
    {
        public static async Task<Dictionary<string, Material>> LoadMaterial(string path)
        {
            path = Path.GetFullPath(path);
            Dictionary<string, Material> result = new();
            if (!File.Exists(path))
            {
                return result;
            }

            MaterialGroup material = null;
            DiffuseMaterial diffuseMaterial = new();
            SpecularMaterial specularMaterial = new();

            foreach (string line in await File.ReadAllLinesAsync(path))
            {
                if (line.StartsWith('#'))
                {
                    continue;
                }

                Match match = StringExtension.BinaryPattern.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                string type = match.Groups[1].Value;
                string value = match.Groups[2].Value;

                double[] values = value.SplitToDouble(" ");
                Brush brush = Brushes.Transparent;
                if (values.Length == 3)
                {
                    static byte doubleToByte(double value) => (byte)(value < 0 ? 0 : value > 1 ? 1 : value * 255);
                    brush = new SolidColorBrush(Color.FromRgb(doubleToByte(values[0]), doubleToByte(values[1]), doubleToByte(values[2])));
                }

                switch (type)
                {
                    case "newmtl":
                        material?.Freeze();
                        material = new MaterialGroup();
                        diffuseMaterial = new();
                        specularMaterial = new();
                        result.Add(value, material);
                        break;
                    case "Ka":
                        diffuseMaterial.AmbientColor = ((SolidColorBrush)brush).Color;
                        break;
                    case "Kd":
                        diffuseMaterial = new DiffuseMaterial(brush);
                        material?.Children.Add(diffuseMaterial);
                        break;
                    case "Ks":
                        specularMaterial = new SpecularMaterial(brush, specularMaterial.SpecularPower);
                        material?.Children.Add(specularMaterial);
                        break;
                    case "Ns":
                        if (double.TryParse(value, out double number))
                        {
                            specularMaterial.SpecularPower = number;
                        }
                        break;
                    case "Ke":
                        material?.Children.Add(new EmissiveMaterial(brush));
                        break;
                    case "map_Ka":
                        material?.Children.Add(new DiffuseMaterial(GetImageBrush(Path.Combine(Path.GetDirectoryName(path), value))));
                        break;
                    case "map_Kd":
                        material?.Children.Add(new DiffuseMaterial(GetImageBrush(Path.Combine(Path.GetDirectoryName(path), value))));
                        break;
                }
            }
            material?.Freeze();
            return result;
        }

        private static Brush GetImageBrush(string path)
        {
            path = Path.GetFullPath(path);
            if (File.Exists(path))
            {
                return new ImageBrush(new BitmapImage(new Uri(path))) { ViewportUnits = BrushMappingMode.Absolute };
            }
            return Brushes.Transparent;
        }
    }
}
