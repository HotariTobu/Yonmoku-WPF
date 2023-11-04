using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Yonmoku-WPF
{
    static class ModelLoader2
    {
        public static async Task<Model3D> LoadModel(string path)
        {
            path = Path.GetFullPath(path);
            if (!File.Exists(path))
            {
                return null;
            }

            Model3DCollection models = new();

            ObjectData data = new();
            Dictionary<string, Material> materials = new();

            string lastType = "";
            AppendAction appendAction = None;

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
                if (type.Equals(lastType))
                {
                    appendAction(data, value);
                }
                else
                {
                    lastType = type;
                    switch (type)
                    {
                        case "mtllib":
                            materials = await MaterialLoader.LoadMaterial(Path.Combine(Path.GetDirectoryName(path), value));
                            break;
                        case "usemtl":
                            data.Material = materials.GetValueOrDefault(value, null);
                            break;
                        case "o":
                            freeze();
                            data = new();
                            break;
                        default:
                            appendAction = type switch
                            {
                                "v" => Vertex,
                                "vt" => VertexTexture,
                                "vn" => VertexNormal,
                                "f" => Face,
                                _ => None
                            };
                            appendAction(data, value);
                            break;
                    }
                }
            }

            freeze();

            Model3D result = new Model3DGroup() { Children = models };
            result.Freeze();
            return result;

            void freeze()
            {
                if (data.Models.Any())
                {
                    Model3DGroup model = new() { Children = data.Models };
                    model.Freeze();
                    models.Add(model);
                }
            }
        }

        private delegate void AppendAction(ObjectData data, string value);

        private static AppendAction Vertex = (data, value) =>
        {
            double[] values = value.SplitToDouble(" ");
            if (values.Length == 3)
            {
                data.Vertexes.Add(new Point3D(values[0], values[1], values[2]));
            }
        };

        private static AppendAction VertexTexture = (data, value) =>
        {
            double[] values = value.SplitToDouble(" ");
            if (values.Length == 2)
            {
                data.TextureCoordinates.Add(new Point(values[0], 1 - values[1]));
            }
        };

        private static AppendAction VertexNormal = (data, value) =>
        {
            double[] values = value.SplitToDouble(" ");
            if (values.Length == 3)
            {
                Vector3D vector = new Vector3D(values[0], values[1], values[2]);
                vector.Normalize();
                data.VertexNormalVectors.Add(vector);
            }
        };

        private static List<int> TriangleIndices = new();

        private static AppendAction Face = (data, value) =>
        {
            MeshGeometry3D mesh = new();
            int[][] tokens = value.Split(' ').Select(x => x.Split('/').Select(x => int.TryParse(x, out int y) ? y - 1 : -1).ToArray()).ToArray();

            foreach (int[] values in tokens)
            {
                switch (values.Length)
                {
                    case 1:
                        mesh.Positions.Add(data.Vertexes[values[0]]);
                        break;
                    case 2:
                        mesh.Positions.Add(data.Vertexes[values[0]]);
                        mesh.TextureCoordinates.Add(data.TextureCoordinates[values[1]]);
                        break;
                    case 3:
                        mesh.Positions.Add(data.Vertexes[values[0]]);
                        if (values[1] != -1)
                        {
                            mesh.TextureCoordinates.Add(data.TextureCoordinates[values[1]]);
                        }
                        mesh.Normals.Add(data.VertexNormalVectors[values[2]]);
                        break;
                }
            }

            int lastTriangleCount = TriangleIndices.Count / 3;
            int triangleCount = tokens.Length - 2;
            if (lastTriangleCount < triangleCount)
            {
                for (int i = lastTriangleCount + 1; i <= triangleCount; i++)
                {
                    TriangleIndices.Add(0);
                    TriangleIndices.Add(i);
                    TriangleIndices.Add(i + 1);
                }
            }
            mesh.TriangleIndices = new Int32Collection(TriangleIndices.Take(triangleCount * 3));

            GeometryModel3D model = new();
            model.Geometry = mesh;
            if (data.Material != null)
            {
                model.Material = data.Material;
            }
            data.Models.Add(model);
        };

        private static AppendAction None = (data, value) => { };

        private class ObjectData
        {
            public Point3DCollection Vertexes = new();
            public PointCollection TextureCoordinates = new();
            public Vector3DCollection VertexNormalVectors = new();

            public Material Material = null;
            public Model3DCollection Models = new();
        }
    }
}
