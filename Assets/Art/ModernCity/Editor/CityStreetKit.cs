using UnityEngine;

namespace Vastcore.ModernCity.Editor
{
    internal static class CityStreetKit
    {
        internal const float Pavement = 0.14f;
        internal const float ApronDrop = 0.028f;
        internal const float RoadEdge = 6f;
        internal const float StreetEnd = 64f;

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
                new Vector3(0, -0.23f, 0), new Vector3(160, 0.34f, 115), true));
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
            for (float x = -StreetEnd + 2; x < StreetEnd; x += 4)
            {
                float fromJunction = Mathf.Abs(x - junction);
                bool crossing = Mathf.Approximately(fromJunction, 6f);
                bool junctionTile = fromJunction < 4f;
                string road = junctionTile ? "Junction road" : crossing ? "Pedestrian crossing" : "Two lane road";
                if (x < -44f)
                {
                    for (int z = -4; z <= 4; z += 4)
                        kit.Place(CityKitAssets.Street, "Paving tile", streets, new Vector3(x, -ApronDrop, z));
                }
                else
                    kit.Place(CityKitAssets.Street, road, streets, new Vector3(x, 0, 0));
                kit.Place(CityKitAssets.Street, crossing ? "Crossing walkway" : "Walkway", streets,
                    new Vector3(x, 0, -8), 180);
                if (!junctionTile)
                    kit.Place(CityKitAssets.Street, crossing ? "Crossing walkway" : "Walkway", streets,
                        new Vector3(x, 0, 8));
                // The streetside apron extends underneath setback buildings. Its finish sits below their slabs.
                kit.Place(CityKitAssets.Street, "Paving tile", streets, new Vector3(x, -ApronDrop, -12));
                if (!junctionTile)
                    kit.Place(CityKitAssets.Street, "Paving tile", streets, new Vector3(x, -ApronDrop, 12));
            }

            for (float z = 8; z < 38; z += 4)
            {
                kit.Place(CityKitAssets.Street, "Side street", streets, new Vector3(junction, 0, z), 90);
                kit.Place(CityKitAssets.Street, "Walkway", streets, new Vector3(junction + 6, 0, z), -90);
                kit.Place(CityKitAssets.Street, "Walkway", streets, new Vector3(junction - 6, 0, z), 90);
            }

        }
    }
}
