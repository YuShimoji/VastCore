using UnityEngine;

namespace Vastcore.ModernCity.Editor
{
    internal static class CityStreetKit
    {
        internal const float Pavement = 0.14f;
        internal const float ApronDrop = 0.028f;
        internal const float RoadEdge = 6f;
        private const float EastCrossing = 72f;
        private const float RearStreet = 42f;

        internal static void Bake(CityKitAssets kit, CityPalette p)
        {
            void Part(string name, System.Action<CityModel> model) => kit.Bake(CityKitAssets.Street, name, model);
            Part("Two lane road", m =>
            {
                Asphalt(m, p, 12);
                Mark(m, p.White, 0, -5.72f, 4, 0.095f);
                Mark(m, p.White, 0, 5.72f, 4, 0.095f);
                Mark(m, p.White, 0, 0, 2.15f, 0.105f);
            });
            Part("Junction road", m =>
            {
                Asphalt(m, p, 12);
                Mark(m, p.White, 0, -5.72f, 4, 0.095f);
            });
            Part("Crossroad", m => Asphalt(m, p, 12));
            Part("Rear junction", m => Asphalt(m, p, 8));
            Part("Pedestrian crossing", m =>
            {
                Asphalt(m, p, 12);
                for (int i = -5; i <= 5; i++)
                    Mark(m, p.White, 0, i * 0.96f, 3.15f, 0.44f);
            });
            Part("Side street", m =>
            {
                Asphalt(m, p, 8);
                Mark(m, p.Yellow, 0, -3.68f, 4, 0.079f);
                Mark(m, p.Yellow, 0, 3.68f, 4, 0.079f);
            });
            Part("Walkway", m => Walkway(m, p));
            Part("Crossing walkway", m =>
            {
                Walkway(m, p);
                for (int col = 0; col < 12; col++)
                for (int row = 0; row < 3; row++)
                {
                    var centre = new Vector3((col - 5.5f) * 0.19f, Pavement + 0.005f, -1.64f + row * 0.19f);
                    m.Tube(p.Yellow, centre, centre + Vector3.up * 0.013f, 0.036f, 8);
                }
            });
            Part("Paving tile", m => m.Box(p.Paving,
                new Vector3(0, Pavement - 0.12f, 0), new Vector3(4, 0.24f, 4), true));
            Part("Block ground", m => m.Box(p.Concrete,
                new Vector3(0, -0.23f, 0), new Vector3(550, 0.34f, 550), true));
            Part("Planted court", m =>
            {
                m.Box(p.Stone, new Vector3(0, 0.02f, 0), new Vector3(8, 0.16f, 8), true);
                m.Box(p.Verge, new Vector3(0, 0.103f, 0), new Vector3(7.68f, 0.025f, 7.68f));
            });
            Part("Parking bay", m =>
            {
                Asphalt(m, p, 4);
                Mark(m, p.White, -1.93f, 0, 0.095f, 3.85f);
                Mark(m, p.White, 0, 1.91f, 3.9f, 0.095f);
            });
        }

        private static void Asphalt(CityModel m, CityPalette p, float width)
        {
            // Repeat in each 4 m length so a straight run has no transverse texture seams.
            for (float z = -width / 2 + 2; z < width / 2; z += 4)
                m.Box(p.Asphalt, new Vector3(0, -0.1f, z), new Vector3(4, 0.2f, 4), true);
        }

        private static void Mark(CityModel m, Material paint, float x, float z, float width, float length) =>
            m.Box(paint, new Vector3(x, 0.004f, z), new Vector3(width, 0.008f, length));

        private static void Walkway(CityModel m, CityPalette p)
        {
            // A shallow joint keeps the individual paving slabs legible at walking height.
            m.Box(p.Dark, new Vector3(0, Pavement - 0.131f, 0), new Vector3(4, 0.218f, 4));
            m.CollisionBox(new Vector3(0, Pavement - 0.12f, 0), new Vector3(4, 0.24f, 4));
            for (int row = 0; row < 8; row++)
            {
                float z = -1.75f + row * 0.5f;
                for (int col = 0; col < 5; col++)
                {
                    float x = -2 + col - (row % 2) * 0.5f;
                    float start = Mathf.Max(-2, x);
                    float end = Mathf.Min(2, x + 1);
                    if (end <= start)
                        continue;
                    m.Box((row * 7 + col * 3) % 11 == 0 ? p.Concrete : p.Stone,
                        new Vector3((start + end) / 2, Pavement - 0.02f, z),
                        new Vector3(end - start - 0.011f, 0.04f, 0.488f));
                }
            }
            m.Box(p.Stone, new Vector3(0, Pavement - 0.076f, -1.94f), new Vector3(3.98f, 0.14f, 0.145f));
        }

        internal static void Assemble(CityKitAssets kit, Transform city, bool narrow)
        {
            var streets = CityKitAssets.Group("Street and sidewalks", city, Vector3.zero).transform;
            kit.Place(CityKitAssets.Street, "Block ground", streets, new Vector3(0, 0, 5));
            float junction = narrow ? 8f : 0f;
            for (float x = -62; x < 116; x += 4)
            {
                float fromJunction = Mathf.Abs(x - junction);
                float fromEast = Mathf.Abs(x - EastCrossing);
                bool crossing = fromJunction == 6f || fromEast == 6f;
                bool junctionTile = fromJunction < 4f;
                bool through = fromEast < 4f;
                string road = through ? "Crossroad" : junctionTile ? "Junction road" :
                    crossing ? "Pedestrian crossing" : "Two lane road";
                if (x < -44f)
                {
                    for (int z = -4; z <= 4; z += 4)
                        kit.Place(CityKitAssets.Street, "Paving tile", streets, new Vector3(x, -ApronDrop, z));
                }
                else
                    kit.Place(CityKitAssets.Street, road, streets, new Vector3(x, 0, 0));
                if (through)
                    continue;
                string sidewalk = crossing ? "Crossing walkway" : "Walkway";
                kit.Place(CityKitAssets.Street, sidewalk, streets, new Vector3(x, 0, -8), 180);
                if (!junctionTile)
                    kit.Place(CityKitAssets.Street, sidewalk, streets, new Vector3(x, 0, 8));
                // The streetside apron extends underneath setback buildings. Its finish sits below their slabs.
                kit.Place(CityKitAssets.Street, "Paving tile", streets, new Vector3(x, -ApronDrop, -12));
                if (!junctionTile)
                    kit.Place(CityKitAssets.Street, "Paving tile", streets, new Vector3(x, -ApronDrop, 12));
            }

            SideRun(kit, streets, junction, 36);
            SideRun(kit, streets, EastCrossing, 36);
            SideRun(kit, streets, EastCrossing, -76);
            for (int x = -42; x < 116; x += 4)
            {
                bool joins = Mathf.Abs(x - junction) < 4 || Mathf.Abs(x - EastCrossing) < 4;
                kit.Place(CityKitAssets.Street, joins ? "Rear junction" : "Side street", streets,
                    new Vector3(x, 0, RearStreet));
                if (!joins)
                    kit.Place(CityKitAssets.Street, "Walkway", streets, new Vector3(x, 0, RearStreet - 6), 180);
                kit.Place(CityKitAssets.Street, "Walkway", streets, new Vector3(x, 0, RearStreet + 6));
            }

            foreach (int x in new[] { 54, 62, 84, 92, 100 })
            foreach (int z in new[] { 16, 24, 32, -24, -32 })
                kit.Place(CityKitAssets.Street, "Planted court", streets, new Vector3(x, 0, z));

            for (int x = 42; x <= 62; x += 4)
                kit.Place(CityKitAssets.Street, "Parking bay", streets, new Vector3(x, 0, -16), 180);

            for (int x = -42; x < EastCrossing - 4; x += 4)
            {
                kit.Place(CityKitAssets.Street, "Side street", streets, new Vector3(x, 0, -34));
                kit.Place(CityKitAssets.Street, "Walkway", streets, new Vector3(x, -ApronDrop, -28));
            }
            for (int x = -38; x <= 42; x += 8)
                kit.Place(CityKitAssets.Street, "Planted court", streets, new Vector3(x, 0, -42));
        }

        private static void SideRun(CityKitAssets kit, Transform streets, float axis, int end)
        {
            int step = end > 0 ? 4 : -4;
            for (int z = step * 2; Mathf.Abs(z) <= Mathf.Abs(end); z += step)
            {
                kit.Place(CityKitAssets.Street, "Side street", streets, new Vector3(axis, 0, z), 90);
                // The transverse sidewalks already cover the corner squares.
                if (Mathf.Abs(z) == 8 || end == 36 && z == 36)
                    continue;
                kit.Place(CityKitAssets.Street, "Walkway", streets, new Vector3(axis + 6, 0, z), -90);
                kit.Place(CityKitAssets.Street, "Walkway", streets, new Vector3(axis - 6, 0, z), 90);
            }
        }
    }
}
