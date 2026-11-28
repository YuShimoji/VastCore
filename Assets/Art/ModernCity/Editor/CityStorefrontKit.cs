using UnityEditor;
using UnityEngine;

namespace Vastcore.ModernCity.Editor
{
    internal static class CityStorefrontKit
    {
        internal const string Window = "Recessed storefront window";
        internal const string Entry = "Recessed storefront entrance";
        internal const string Canopy = "Timber soffit canopy";
        internal const string StairPortal = "Timber stair opening";
        private const float Face = -0.32f;
        private const float GlassZ = 0.215f;
        private const float DoorZ = 0.325f;
        private const float Storey = CityBuildingKit.Storey;
        private const float OpeningHead = 2.49f;
        private const float SillHeight = 0.44f;
        private const float WindowWidth = 3.06f;

        internal static void Bake(CityKitAssets kit, CityPalette basePalette)
        {
            var p = new Finishes(basePalette);
            kit.Bake(CityKitAssets.Buildings, Window, m => WindowBay(m, p));
            kit.Bake(CityKitAssets.Buildings, Entry, m => EntranceBay(m, p));
            kit.Bake(CityKitAssets.Buildings, Canopy, m => Awning(m, p));
            kit.Bake(CityKitAssets.Buildings, StairPortal, m => StairFrame(m, p));
        }

        private static void WindowBay(CityModel m, Finishes p)
        {
            Opening(m, p, WindowWidth, SillHeight);
            Horizontal(m, p.Coping, 0, SillHeight + 0.034f, -0.062f, WindowWidth + 0.16f, 0.078f, 0.59f);
            Horizontal(m, p.Frame, 0, SillHeight - 0.005f, -0.358f, WindowWidth + 0.11f, 0.015f, 0.051f);
            Frame(m, p, WindowWidth, SillHeight + 0.071f, OpeningHead, GlassZ, true);
            foreach (float x in new[] { -1.02f, 0f, 1.02f })
                Horizontal(m, p.Frame, x, 1.5f, GlassZ, 0.034f, 1.992f, 0.071f);
            Horizontal(m, p.Frame, 0, 2.18f, GlassZ, WindowWidth, 0.041f, 0.076f);
            CityBuildingKit.Pane(m, p.Glazing, 0, WindowWidth, SillHeight + 0.072f, OpeningHead, GlassZ);
        }

        private static void EntranceBay(CityModel m, Finishes p)
        {
            const float width = CityBuildingKit.DoorWidth;
            Opening(m, p, width, 0);
            Frame(m, p, width, 0.01f, OpeningHead, DoorZ, false);

            // The apron sits 28 mm below slab height. The chamfer and metal saddle join the two faces.
            m.BeveledBox(p.Coping, new Vector3(0, -0.027f, -0.043f),
                new Vector3(width, 0.061f, 0.716f), 0.012f, true);
            Horizontal(m, p.Frame, 0, 0.009f, DoorZ + 0.009f, width, 0.013f, 0.109f);
            for (int n = -1; n <= 1; n++)
                Horizontal(m, p.Base.Brass, 0, 0.016f, DoorZ + 0.021f * n, width - 0.044f, 0.003f, 0.004f);

            // The glazed leaves are held open along the reveal. The clear center remains a continuous path.
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * (width / 2 - 0.07f);
                float z = DoorZ + 0.37f;
                Horizontal(m, p.Frame, x, 0.105f, z, 0.046f, 0.175f, 0.74f);
                Horizontal(m, p.Frame, x, 2.393f, z, 0.043f, 0.067f, 0.74f);
                Horizontal(m, p.Frame, x, 1.28f, z + 0.36f, 0.052f, 2.37f, 0.043f);
                m.Quad(p.Glazing, new Vector3(x, 0.19f, DoorZ), new Vector3(x, 0.19f, z + 0.36f),
                    new Vector3(x, 2.367f, z + 0.36f), new Vector3(x, 2.367f, DoorZ));
                m.CollisionBox(new Vector3(x, 1.24f, z), new Vector3(0.068f, 2.45f, 0.74f));
                float handleX = x - side * 0.07f;
                m.Tube(p.Base.Brass, new Vector3(handleX, 0.89f, z + 0.29f),
                    new Vector3(handleX, 1.25f, z + 0.29f), 0.013f, 12);
            }
        }

        private static void Opening(CityModel m, Finishes p, float openingWidth, float sill)
        {
            float halfOpening = openingWidth * 0.5f;
            StoneField(m, p, -2f, -halfOpening, 0, Storey);
            StoneField(m, p, halfOpening, 2f, 0, Storey);
            StoneField(m, p, -halfOpening, halfOpening, OpeningHead, Storey);
            if (sill > 0)
                StoneField(m, p, -halfOpening, halfOpening, 0, sill);
        }

        private static void StoneField(CityModel m, Finishes p, float left, float right, float bottom, float top)
        {
            // Joints have a darker backing. The jamb continues through to the room behind the face panels.
            m.Box(p.Coping, new Vector3((left + right) * 0.5f, (bottom + top) * 0.5f, 0.018f),
                new Vector3(right - left, top - bottom, 0.598f), true);
            int rows = Mathf.Max(1, Mathf.RoundToInt((top - bottom) / 0.47f));
            int slabs = Mathf.Max(1, Mathf.RoundToInt((right - left) / 0.78f));
            float height = (top - bottom) / rows;
            float width = (right - left) / slabs;
            for (int row = 0; row < rows; row++)
            for (int col = 0; col < slabs; col++)
            {
                int pattern = row * 17 + col * 11 + Mathf.RoundToInt(left * 37 + bottom * 20);
                var stone = Mathf.Abs(pattern) % 7 < 2 ? p.StoneDark : p.Stone;
                var centre = new Vector3(left + (col + 0.5f) * width, bottom + (row + 0.5f) * height, Face + 0.012f);
                var sample = new Vector2(Mathf.Repeat(pattern * 0.193f, 2.4f), Mathf.Repeat(pattern * 0.127f, 1.2f));
                m.BeveledBox(stone, centre, new Vector3(width - 0.009f, height - 0.009f, 0.067f), 0.008f, uvOffset: sample);
            }
        }

        private static void Frame(CityModel m, Finishes p, float width, float bottom, float top, float z, bool sill)
        {
            float mid = (bottom + top) * 0.5f;
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * (width * 0.5f - 0.031f);
                Horizontal(m, p.Frame, x, mid, z, 0.062f, top - bottom, 0.13f);
                Horizontal(m, p.LightMetal, x - side * 0.021f, mid, z - 0.067f, 0.013f, top - bottom, 0.012f);
            }
            Horizontal(m, p.Frame, 0, top - 0.026f, z, width, 0.052f, 0.13f);
            if (sill)
                Horizontal(m, p.Frame, 0, bottom + 0.028f, z, width, 0.054f, 0.14f);
        }

        private static void Awning(CityModel m, Finishes p)
        {
            m.BeveledBox(p.Frame, new Vector3(0, 2.744f, -0.87f), new Vector3(3.88f, 0.164f, 1.18f), 0.016f);
            Horizontal(m, p.LightMetal, 0, 2.685f, -1.465f, 3.83f, 0.023f, 0.032f);
            for (int i = -1; i <= 1; i++)
                Horizontal(m, p.Coping, i * 1.15f, 2.83f, -0.858f, 0.018f, 0.023f, 1.08f);
            for (int i = 0; i < 9; i++)
                Horizontal(m, p.Base.Timber, 0, 2.65f, -1.385f + i * 0.124f, 3.75f, 0.040f, 0.117f);
            foreach (int side in new[] { -1, 1 })
            {
                Horizontal(m, p.Frame, side * 1.875f, 2.638f, -0.862f, 0.056f, 0.084f, 1.085f);
                Horizontal(m, p.Frame, side * 1.608f, 2.59f, -0.369f, 0.093f, 0.278f, 0.341f);
            }
            foreach (float x in new[] { -1.23f, 0f, 1.23f })
            {
                m.Tube(p.LightMetal, new Vector3(x, 2.615f, -0.905f), new Vector3(x, 2.632f, -0.905f), 0.081f, 18);
                m.Tube(p.Base.Light, new Vector3(x, 2.612f, -0.905f), new Vector3(x, 2.616f, -0.905f), 0.052f, 14);
            }
        }

        private static void StairFrame(CityModel m, Finishes p)
        {
            foreach (int side in new[] { -1, 1 })
            {
                m.BeveledBox(p.Base.Paint, new Vector3(side * 1.59f, 1.32f, 0),
                    new Vector3(0.32f, 2.64f, 0.29f), 0.016f, true);
                Horizontal(m, p.Base.Timber, side * 1.421f, 1.233f, -0.121f, 0.075f, 2.466f, 0.21f);
                Horizontal(m, p.Coping, side * 1.59f, 0.083f, -0.061f, 0.342f, 0.166f, 0.191f);
            }
            m.BeveledBox(p.Base.Paint, new Vector3(0, 2.729f, 0), new Vector3(3.49f, 0.24f, 0.29f), 0.014f, true);
            Horizontal(m, p.Base.Timber, 0, 2.481f, -0.121f, 2.915f, 0.061f, 0.21f);
        }

        private static void Horizontal(CityModel m, Material mat, float x, float y, float z,
            float width, float height, float depth) =>
            m.BeveledBox(mat, new Vector3(x, y, z), new Vector3(width, height, depth),
                Mathf.Min(0.009f, height * 0.20f));

        private sealed class Finishes
        {
            private const string Surface = CityKitAssets.Root + "/Textures/Storefront/Concrete034_1K-JPG";
            internal readonly CityPalette Base;
            internal readonly Material Stone = Cladding("Storefront stone", "E4DBCC");
            internal readonly Material StoneDark = Cladding("Storefront stone shade", "C7C2B7");
            internal readonly Material Coping = CityPalette.Make("Storefront sill stone", "989184", 0.24f);
            internal readonly Material Frame = CityPalette.Make("Storefront anodised bronze", "55473D", 0.47f, 0.48f);
            internal readonly Material LightMetal = CityPalette.Make("Storefront brushed bronze", "917B62", 0.56f, 0.52f);
            internal readonly Material Glazing = CityPalette.Make("Storefront clear glazing", "ABC5C8", 0.93f, 0.12f,
                glass: true, glassOpacity: 0.13f, preserveSpecular: true);

            internal Finishes(CityPalette basePalette) => Base = basePalette;

            private static Material Cladding(string name, string tint)
            {
                Material material = CityPalette.Textured(name, CityPalette.LimestoneMap, tint, 2.4f, 0.25f,
                    Surface + "_NormalGL.jpg");
                material.SetTextureScale("_BaseMap", new Vector2(1f / 2.4f, 1f / 1.2f));
                material.SetFloat("_BumpScale", 0.35f);
                EditorUtility.SetDirty(material);
                return material;
            }
        }
    }
}
