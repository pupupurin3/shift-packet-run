using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftRunner.Presentation
{
    // A small unlit 2D batch. The same embedded bitmap alphabet works offline and on WebGL.
    internal sealed class TerminalCanvas : IDisposable
    {
        public const int Width = 1280, Height = 720;
        readonly List<Vector3> positions = new List<Vector3>(16000);
        readonly List<Vector2> uv = new List<Vector2>(16000);
        readonly List<Color32> colors = new List<Color32>(16000);
        readonly List<int> indices = new List<int>(24000);
        readonly Mesh mesh;
        readonly Material material;
        readonly Texture2D atlas;
        static readonly Vector2 White = new Vector2(127.5f / 128, 63.5f / 64);

        public TerminalCanvas(GameObject host)
        {
            atlas = MakeAtlas();
            material = new Material(Resources.Load<Shader>("TerminalInk")) { mainTexture = atlas };
            mesh = new Mesh { name = "Terminal drawing" };
            mesh.MarkDynamic();
            host.AddComponent<MeshFilter>().sharedMesh = mesh;
            host.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        public void Begin()
        {
            positions.Clear(); colors.Clear(); uv.Clear(); indices.Clear();
        }

        public void Flush()
        {
            mesh.Clear(false);
            mesh.SetVertices(positions);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(indices, 0, false);
            mesh.bounds = new Bounds(new Vector3(Width / 2, -Height / 2), new Vector3(Width + 8, Height + 8, 2));
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(mesh);
            UnityEngine.Object.Destroy(material);
            UnityEngine.Object.Destroy(atlas);
        }

        void Vertex(Vector2 screen, Color32 color, Vector2 tex)
        {
            positions.Add(new Vector3(screen.x, -screen.y));
            colors.Add(color);
            uv.Add(tex);
        }

        void AddIndices()
        {
            int n = positions.Count;
            indices.Add(n); indices.Add(n + 1); indices.Add(n + 2);
            indices.Add(n); indices.Add(n + 2); indices.Add(n + 3);
        }

        public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 color)
        {
            AddIndices();
            Vertex(a, color, White); Vertex(b, color, White); Vertex(c, color, White); Vertex(d, color, White);
        }

        public void Rect(float x, float y, float w, float h, Color32 color)
        {
            if (w <= 0 || h <= 0) return;
            Quad(new Vector2(x,y), new Vector2(x+w,y), new Vector2(x+w,y+h), new Vector2(x,y+h), color);
        }

        public void Gradient(float x, float y, float w, float h, Color32 top, Color32 bottom)
        {
            AddIndices();
            Vertex(new Vector2(x,y), top, White); Vertex(new Vector2(x+w,y), top, White);
            Vertex(new Vector2(x+w,y+h), bottom, White); Vertex(new Vector2(x,y+h), bottom, White);
        }

        public void Line(float x1, float y1, float x2, float y2, float thickness, Color32 color)
        {
            Vector2 a = new Vector2(x1, y1), b = new Vector2(x2, y2);
            Vector2 normal = new Vector2(a.y-b.y, b.x-a.x).normalized * (thickness * .5f);
            Quad(a+normal, b+normal, b-normal, a-normal, color);
        }

        public void Frame(float x, float y, float w, float h, float t, Color32 color)
        {
            Rect(x,y,w,t,color); Rect(x,y+h-t,w,t,color);
            Rect(x,y+t,t,h-2*t,color); Rect(x+w-t,y+t,t,h-2*t,color);
        }

        public void Ring(float x, float y, float radius, int sides, float thickness, Color32 color)
        {
            Vector2 last = new Vector2(x + radius, y);
            for (int i=1; i<=sides; i++)
            {
                float angle = i * Mathf.PI * 2 / sides;
                Vector2 next = new Vector2(x + radius * Mathf.Cos(angle), y + radius * Mathf.Sin(angle));
                Line(last.x, last.y, next.x, next.y, thickness, color);
                last = next;
            }
        }

        public static float TextWidth(string value, float size)
        {
            return Mathf.Max(0, value.Length * 6 - 1) * size;
        }

        // Size is the width of one source pixel. Slant is used only for the wordmark.
        public void Text(string value, float x, float y, float size, Color32 tint, float slant = 0)
        {
            foreach (char ch in value)
            {
                int glyph = char.ToUpperInvariant(ch) - 32;
                if (glyph > 0 && glyph < 96)
                {
                    float u0 = (glyph % 16) * 8 / 128f, v0 = (glyph / 16) * 10 / 64f;
                    float u1 = u0 + 5f / 128, v1 = v0 + 7f / 64;
                    AddIndices();
                    Vertex(new Vector2(x+slant,y), tint, new Vector2(u0,v1));
                    Vertex(new Vector2(x+5*size+slant,y), tint, new Vector2(u1,v1));
                    Vertex(new Vector2(x+5*size,y+7*size), tint, new Vector2(u1,v0));
                    Vertex(new Vector2(x,y+7*size), tint, new Vector2(u0,v0));
                }
                x += 6 * size;
            }
        }

        static Texture2D MakeAtlas()
        {
            // Each row is a five-bit mask, from the top of the letter to its baseline.
            const string glyphs = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-+=./:[]<>_!?%#()|,'*";
            string[] rows = {
                "0E11111F111111","1E11111E11111E","0F10101010100F","1E11111111111E",
                "1F10101E10101F","1F10101E101010","0F10101711110F","1111111F111111",
                "1F04040404041F","0702020212120C","11121418141211","1010101010101F",
                "111B1515111111","11191915131311","0E11111111110E","1E11111E101010",
                "0E11111115120D","1E11111E141211","0F10100E01011E","1F040404040404",
                "1111111111110E","11111111110A04","1111111515150A","11110A040A1111",
                "11110A04040404","1F01020408101F","0E11131519110E","040C040404040E",
                "0E11010204081F","1E01010E01011E","02060A121F0202","1F10101E01011E",
                "0E10101E11110E","1F010204080808","0E11110E11110E","0E11110F01010E",
                "0000001F000000","0004041F040400","00001F001F0000","00000000000C0C",
                "01010204081010","00000C00000C00","0E08080808080E","0E02020202020E",
                "01020408040201","10080402040810","0000000000001F","04040404040004",
                "0E110102040004","19190204081313","0A0A1F0A1F0A0A","02040808080402",
                "08040202020408","04040404040404","00000000000408","04040800000000",
                "00040A1F0A0400"
            };
            var data = new Color32[128 * 64];
            var white = new Color32(255,255,255,255);
            for (int i=0; i<glyphs.Length; i++)
            {
                int tile = glyphs[i] - 32, x0 = tile % 16 * 8, y0 = tile / 16 * 10;
                for (int y=0; y<7; y++)
                {
                    int mask = Convert.ToInt32(rows[i].Substring(y * 2, 2), 16);
                    for (int x=0; x<5; x++)
                        if ((mask & (16 >> x)) != 0) data[(y0+6-y)*128+x0+x] = white;
                }
            }
            data[data.Length-1] = white;
            var texture = new Texture2D(128, 64, TextureFormat.RGBA32, false, true) {
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Terminal glyphs"
            };
            texture.SetPixels32(data);
            texture.Apply(false, true);
            return texture;
        }
    }

    internal static class Ink
    {
        public static readonly Color32 Night = new Color32(6,15,22,255), Panel = new Color32(10,24,32,255),
            Edge = new Color32(39,67,74,255), Muted = new Color32(141,172,176,255),
            Bright = new Color32(226,247,242,255), Mint = new Color32(142,255,173,255),
            Cyan = new Color32(90,239,247,255), Amber = new Color32(255,211,115,255),
            Red = new Color32(255,118,99,255);
        public static Color32 Alpha(Color32 color, byte alpha) { color.a = alpha; return color; }
    }
}
