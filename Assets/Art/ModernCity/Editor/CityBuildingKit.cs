using UnityEngine;

namespace Vastcore.ModernCity.Editor
{
    internal enum CityFacade { Stone, Brick, Glass }

    internal static class CityBuildingKit
    {
        internal const float Bay = 4f;
        internal const float Storey = 3f;
        internal const float StairLanding = 2.61f;
        internal const float FlightOffset = 0.93f;
        internal const float DoorWidth = 1.84f;
        internal const float ServiceWidth = 1.46f;

        internal static void Bake(CityKitAssets kit, CityPalette p)
        {
            void Part(string name, System.Action<CityModel> model) => kit.Bake(CityKitAssets.Buildings, name, model);
            Part("Stone window", m => Window(m, p, p.Stone, -0.21f, 2.8f, 0.65f, 2.58f, true));
            Part("Stone loggia", m =>
            {
                Window(m, p, p.Stone, 0.12f, 2.98f, 0.31f, 2.54f, false);
                m.Box(p.Stone, new Vector3(0, 0.10f, -0.45f), new Vector3(3.72f, 0.2f, 0.88f));
                Guard(m, p, new Vector3(-1.82f, 0.2f, -0.85f), new Vector3(1.82f, 0.2f, -0.85f), false);
            });
            Part("Brick window", m => Window(m, p, p.Brick, 0.16f, 2.5f, 0.73f, 2.42f, true));
            Part("Brick paired window", m =>
            {
                Window(m, p, p.Brick, 0, 3.12f, 0.72f, 2.5f, true);
                m.Box(p.Brick, new Vector3(0, 1.58f, -0.04f), new Vector3(0.27f, 1.8f, 0.38f));
            });
            Part("Office curtain wall", m => Window(m, p, p.Blue, 0, 3.78f, 0.22f, 2.78f, false));
            Part("Office fin wall", m =>
            {
                Window(m, p, p.Blue, 0, 3.7f, 0.22f, 2.78f, false);
                for (int i = -1; i <= 1; i++)
                    m.Box(p.Metal, new Vector3(i * 1.25f, 1.5f, -0.16f), new Vector3(0.075f, 3, 0.24f));
            });
            Part("Stone blank bay", m => Wall(m, p.Stone));
            Part("Brick blank bay", m => Wall(m, p.Brick));
            Part("Office blank bay", m => Wall(m, p.Dark));
            Part("Shop display", m => Window(m, p, p.Stone, 0, 3.55f, 0.23f, 2.52f, false));
            Part("Brick shop display", m => Window(m, p, p.Brick, 0, 3.42f, 0.25f, 2.54f, false));
            Part("Lobby display", m => Window(m, p, p.Dark, 0, 3.64f, 0.19f, 2.67f, false));
            Part("Shop entrance", m => Entrance(m, p, p.Stone, DoorWidth, 2.42f, true));
            Part("Brick shop entrance", m => Entrance(m, p, p.Brick, DoorWidth, 2.42f, true));
            Part("Lobby entrance", m =>
            {
                Entrance(m, p, p.Dark, 2.2f, 2.55f, true);
                foreach (float side in new[] { -1f, 1f })
                {
                    m.BeveledBox(p.Stone, new Vector3(side * 1.54f, 1.45f, -0.22f),
                        new Vector3(0.63f, 2.9f, 0.42f), 0.024f, true);
                    m.Box(p.Metal, new Vector3(side * 1.23f, 1.46f, -0.29f), new Vector3(0.05f, 2.87f, 0.26f));
                }
            });
            Part("Service entrance", m => Entrance(m, p, p.Concrete, ServiceWidth, 2.29f, false));
            Part("Stone canopy", m => Canopy(m, p, p.Dark));
            Part("Canvas awning", m => Canopy(m, p, p.Awning));
            Part("Lobby canopy", m => FlatCanopy(m, p, 2.64f, 2.05f, p.Stone));
            Part("Service canopy", m => FlatCanopy(m, p, 2.45f, 1.55f, p.Metal));
            Part("Timber screen", m =>
            {
                for (int i = 0; i < 17; i++)
                    m.Box(p.Timber, new Vector3((i - 8) * 0.14f, 1.49f, -0.2f), new Vector3(0.06f, 2.62f, 0.18f));
            });
            Part("Column", m =>
                m.Box(p.Concrete, new Vector3(0, 1.5f, 0), new Vector3(0.34f, 3, 0.34f), true));
            Part("Partition", m => Wall(m, p.Paint, 0.12f));
            Part("Meeting room screen", m => Window(m, p, p.Paint, 0, 3.54f, 0.46f, 2.64f, false));
            Part("Open partition", m =>
            {
                Opening(m, p.Paint, 0, 1.6f, 0, 2.32f, 0.12f);
                Frame(m, p.Timber, 0, 1.6f, 0, 2.32f, false);
            });
            Part("Paved slab", m => Slab(m, p, p.Paving));
            Part("Plaster slab", m => Slab(m, p, p.Stone));
            Part("Lobby terrazzo slab", m => Slab(m, p, p.Terrazzo));
            Part("Timber slab", m => Slab(m, p, p.Timber));
            Part("Office slab", m => Slab(m, p, p.Textile));
            Part("Roof slab", m => Slab(m, p, p.Concrete));
            Part("Parapet", m =>
            {
                m.Box(p.Concrete, new Vector3(0, 0.37f, 0), new Vector3(4, 0.74f, 0.25f), true);
                m.Box(p.Metal, new Vector3(0, 0.75f, 0), new Vector3(4, 0.075f, 0.31f));
            });
            Part("Terrace guard", m =>
                Guard(m, p, new Vector3(-2, 0, 0), new Vector3(2, 0, 0), true));
            Part("Roof plant screen", m =>
            {
                m.BeveledBox(p.Dark, new Vector3(0, 0.85f, 0), new Vector3(4, 1.7f, 2.35f), 0.045f);
                m.Box(p.Paint, new Vector3(0, 1.74f, 0), new Vector3(4.12f, 0.08f, 2.47f));
                for (int i = 0; i < 12; i++)
                {
                    float y = 0.26f + i * 0.11f;
                    m.Box(p.Metal, new Vector3(0, y, -1.21f), new Vector3(3.73f, 0.043f, 0.11f));
                    m.Box(p.Metal, new Vector3(0, y, 1.21f), new Vector3(3.73f, 0.043f, 0.11f));
                }
            });
            Part("Stair guard", m =>
                Guard(m, p, new Vector3(-2, 0, 0), new Vector3(2, 0, 0), true));
            Part("Half stair guard", m =>
                Guard(m, p, new Vector3(-1, 0, 0), new Vector3(1, 0, 0), true));
            Part("Switchback stair", m => Stair(m, p));
        }

        private static void Slab(CityModel model, CityPalette palette, Material finish)
        {
            model.Box(palette.Paint, new Vector3(0, -0.13f, 0), new Vector3(Bay, 0.22f, Bay), true);
            model.Box(finish, new Vector3(0, -0.02f, 0), new Vector3(Bay, 0.04f, Bay), true);
        }

        private static void Wall(CityModel model, Material material, float depth = 0.22f) =>
            model.Box(material, new Vector3(0, Storey * 0.5f, 0), new Vector3(Bay, Storey, depth), true);

        private static void Opening(CityModel m, Material surface, float offset, float width, float bottom,
            float top, float depth)
        {
            float left = offset - width / 2;
            float right = offset + width / 2;
            m.Box(surface, new Vector3((left - 2) / 2, 1.5f, 0), new Vector3(left + 2, 3, depth), true);
            m.Box(surface, new Vector3((right + 2) / 2, 1.5f, 0), new Vector3(2 - right, 3, depth), true);
            m.Box(surface, new Vector3(offset, (3 + top) / 2, 0), new Vector3(width, 3 - top, depth), true);
            if (bottom > 0)
                m.Box(surface, new Vector3(offset, bottom / 2, 0), new Vector3(width, bottom, depth), true);
        }

        private static void Frame(CityModel m, Material frame, float x, float width, float bottom, float top,
            bool sill)
        {
            const float trim = 0.055f;
            float verticalCentre = (top + bottom) * 0.5f;
            float halfWidth = width * 0.5f;
            m.Box(frame, new Vector3(x - halfWidth + trim * 0.5f, verticalCentre, 0),
                new Vector3(trim, top - bottom, 0.27f));
            m.Box(frame, new Vector3(x + halfWidth - trim * 0.5f, verticalCentre, 0),
                new Vector3(trim, top - bottom, 0.27f));
            m.Box(frame, new Vector3(x, top - trim * 0.5f, 0), new Vector3(width, trim, 0.27f));
            if (sill)
                m.Box(frame, new Vector3(x, bottom + trim * 0.5f, -0.02f), new Vector3(width, trim, 0.31f));
        }

        private static void Window(CityModel m, CityPalette p, Material facade, float x, float width,
            float bottom, float top, bool stoneSill)
        {
            Opening(m, facade, x, width, bottom, top, stoneSill ? 0.36f : 0.23f);
            Frame(m, p.Dark, x, width, bottom, top, true);
            m.Box(p.Dark, new Vector3(x + width * 0.12f, (bottom + top) * 0.5f, 0.018f),
                new Vector3(0.046f, top - bottom, 0.105f));
            m.Box(p.Dark, new Vector3(x, top - 0.43f, 0.018f), new Vector3(width, 0.038f, 0.087f));
            Pane(m, p.Glass, x, width, bottom, top, 0.025f);
            if (stoneSill)
                m.Box(p.Stone, new Vector3(x, bottom + 0.011f, -0.12f),
                    new Vector3(width + 0.1f, 0.064f, 0.38f));
        }

        internal static void Pane(CityModel m, Material glass, float x, float width, float bottom, float top, float z)
        {
            float left = x - width / 2;
            float right = x + width / 2;
            m.Quad(glass, new Vector3(left, bottom, z), new Vector3(left, top, z),
                new Vector3(right, top, z), new Vector3(right, bottom, z));
            m.CollisionBox(new Vector3(x, (top + bottom) / 2, z), new Vector3(width, top - bottom, 0.045f));
        }

        private static void Entrance(CityModel m, CityPalette p, Material facade, float width, float top,
            bool glazed)
        {
            Opening(m, facade, 0, width, 0, top, 0.25f);
            Frame(m, p.Dark, 0, width, 0, top, false);
            m.Box(p.Metal, new Vector3(0, 0.004f, 0), new Vector3(width, 0.008f, 0.24f));
            if (!glazed)
            {
                m.Box(p.Metal, new Vector3(-width / 2 + 0.068f, top / 2, 0.37f),
                    new Vector3(0.042f, top - 0.05f, 0.69f), true);
                m.Box(p.Brass, new Vector3(-width / 2 + 0.087f, 1.02f, 0.63f),
                    new Vector3(0.13f, 0.12f, 0.03f));
                return;
            }
            // Open glass leaves sit parallel to travel, with space between their pulls.
            foreach (float side in new[] { -1f, 1f })
            {
                float x = side * (width / 2 - 0.075f);
                m.Box(p.Dark, new Vector3(x, 0.14f, 0.39f), new Vector3(0.064f, 0.22f, 0.78f));
                m.Box(p.Dark, new Vector3(x, top - 0.1f, 0.39f), new Vector3(0.06f, 0.052f, 0.78f));
                m.Box(p.Dark, new Vector3(x, (top - 0.04f) / 2, 0.78f),
                    new Vector3(0.056f, top - 0.07f, 0.05f));
                m.Quad(p.Glass, new Vector3(x, 0.23f, 0), new Vector3(x, 0.23f, 0.78f),
                    new Vector3(x, top - 0.1f, 0.78f), new Vector3(x, top - 0.1f, 0));
                m.CollisionBox(new Vector3(x, top / 2, 0.39f), new Vector3(0.086f, top, 0.78f));
                m.Tube(p.Brass, new Vector3(x - side * 0.06f, 0.82f, 0.69f),
                    new Vector3(x - side * 0.06f, 1.21f, 0.69f), 0.018f);
            }
        }

        private static void Canopy(CityModel m, CityPalette p, Material cover)
        {
            m.Box(cover, new Vector3(0, 2.7f, -0.72f), new Vector3(3.89f, 0.12f, 1.49f),
                Quaternion.Euler(9, 0, 0));
            m.Box(p.Dark, new Vector3(0, 2.55f, -1.45f), new Vector3(3.92f, 0.11f, 0.1f));
            for (int i = -1; i <= 1; i++)
                m.Box(p.Light, new Vector3(i * 1.23f, 2.632f, -0.63f), new Vector3(0.19f, 0.036f, 0.22f));
        }

        private static void FlatCanopy(CityModel m, CityPalette p, float height, float reach, Material fascia)
        {
            m.BeveledBox(p.Dark, new Vector3(0, height, -reach * 0.5f), new Vector3(3.94f, 0.23f, reach), 0.022f);
            m.BeveledBox(fascia, new Vector3(0, height, -reach), new Vector3(3.94f, 0.19f, 0.11f), 0.017f);
            foreach (float x in new[] { -1.12f, 0f, 1.12f })
                m.Box(p.Light, new Vector3(x, height - 0.122f, -reach * 0.63f), new Vector3(0.48f, 0.02f, 0.08f));
        }

        private static void Stair(CityModel m, CityPalette p)
        {
            const float landingHeight = Storey / 2;
            Flight(m, p, -FlightOffset, 0, StairLanding, 0);
            Flight(m, p, FlightOffset, StairLanding, 0, landingHeight);
            float length = Bay - StairLanding;
            m.Box(p.Concrete, new Vector3(0, landingHeight - 0.13f, StairLanding + length / 2),
                new Vector3(3.78f, 0.26f, length), true);
            Guard(m, p, new Vector3(-1.84f, landingHeight, StairLanding),
                new Vector3(-1.84f, landingHeight, 3.86f), true);
        }

        private static void Flight(CityModel m, CityPalette p, float x, float from, float to, float baseHeight)
        {
            const int risers = 9;
            const float halfRise = Storey / 2;
            const float flightWidth = 1.65f;
            float step = (to - from) / risers;
            for (int i = 0; i < risers; i++)
            {
                float height = (i + 1) * halfRise / risers;
                float z = from + step * (i + 0.5f);
                m.Box(p.Concrete, new Vector3(x, baseHeight + height - 0.23f, z),
                    new Vector3(flightWidth, 0.42f, Mathf.Abs(step) + 0.013f));
                m.Box(p.Stone, new Vector3(x, baseHeight + height - 0.018f, z),
                    new Vector3(flightWidth, 0.036f, Mathf.Abs(step) + 0.022f));
            }

            var start = new Vector3(x, baseHeight, from);
            var end = new Vector3(x, baseHeight + halfRise, to);
            Quaternion slope = Quaternion.LookRotation(end - start, Vector3.up);
            m.CollisionBox((start + end) * 0.5f - slope * Vector3.up * 0.12f,
                new Vector3(flightWidth, 0.24f, Vector3.Distance(start, end) + 0.03f), slope);
            var outer = Vector3.right * Mathf.Sign(x) * 0.82f;
            Guard(m, p, start + outer, end + outer, true);
            Guard(m, p, start - outer, end - outer, true);
        }

        private static void Guard(CityModel m, CityPalette p, Vector3 a, Vector3 b, bool solid)
        {
            const float height = 0.98f;
            var top = Vector3.up * height;
            m.Tube(p.Dark, a + top, b + top, 0.034f, 8);
            m.Tube(p.Metal, a + top * 0.55f, b + top * 0.55f, 0.017f, 8);
            int spans = Mathf.CeilToInt(Vector3.Distance(a, b) / 0.74f);
            for (int i = 0; i <= spans; i++)
            {
                var pin = Vector3.Lerp(a, b, i / (float)spans);
                m.Tube(p.Dark, pin, pin + top, 0.024f, 8);
            }
            if (solid)
            {
                var orientation = Quaternion.LookRotation(b - a, Vector3.up);
                m.CollisionBox((a + b + top) * 0.5f,
                    new Vector3(0.07f, height, Vector3.Distance(a, b)), orientation);
            }
        }
    }
}
