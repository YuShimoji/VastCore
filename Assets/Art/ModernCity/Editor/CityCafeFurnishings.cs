using UnityEngine;

namespace Vastcore.ModernCity.Editor
{
    internal static class CityCafeFurnishings
    {
        internal const string Table = "Beveled cafe table";
        internal const string Chair = "Bentwood cafe chair";
        internal const string Order = "Ribbed order counter";
        internal const string Shelves = "Stocked timber cabinet";
        internal const string WindowDisplay = "Window merchandise island";
        internal const string Sconce = "Bronze wall lantern";
        internal const float OrderZ = 4.62f;
        internal static float OrderX(CityPlot plot) => Mathf.Max(4.89f, plot.EntranceX - 2.14f);

        internal static void Bake(CityKitAssets kit, CityPalette p)
        {
            kit.Bake(CityKitAssets.Props, Table, m => CafeTable(m, p));
            kit.Bake(CityKitAssets.Props, Chair, m => CafeChair(m, p));
            kit.Bake(CityKitAssets.Props, Order, m => Counter(m, p));
            kit.Bake(CityKitAssets.Props, Shelves, m => Cabinet(m, p));
            kit.Bake(CityKitAssets.Props, WindowDisplay, m => Display(m, p));
            kit.Bake(CityKitAssets.Props, Sconce, m => WallLight(m, p));
        }

        internal static void Place(CityKitAssets kit, CityPlot plot, Transform parent, bool furnished)
        {
            void At(string name, float x, float y, float z, float yaw = 0) =>
                kit.Place(CityKitAssets.Props, name, parent, new Vector3(x, y, z), yaw);

            float orderX = OrderX(plot);
            At(Order, orderX, 0, OrderZ);
            At("Point of sale", orderX + 0.54f, 1.039f, OrderZ - 0.10f, 175);
            At(Shelves, orderX, 0, OrderZ + 1.54f);
            At(WindowDisplay, plot.Width - 2.05f, 0, 1.03f);
            At(Sconce, plot.Width - 0.19f, 1.46f, plot.StairZ - 1.18f, -90);
            if (plot.GroundRoom == CityRoom.Cafe)
            {
                At("Espresso machine", orderX - 0.53f, 1.039f, OrderZ + 0.05f, 180);
                AddSeats(2.69f, 1.31f);
                if (furnished)
                    AddSeats(5.62f, 1.51f);
            }
            else
            {
                // At 12 m the same cabinet and island set up the shop display and leave the middle aisle open.
                At(Shelves, 0.48f, 0, 1.97f, -90);
                if (furnished)
                {
                    At(Shelves, 0.48f, 0, 5.21f, -90);
                    At("Glass display case", plot.Width - 1.06f, 0, 3.76f, -90);
                }
            }

            if (furnished)
            {
                At("Timber ceiling slats", 2, 2.755f, 2);
                At("Timber ceiling slats", 6, 2.755f, 2);
                At("Pendant light", orderX - 0.54f, 2.756f, OrderZ + 0.08f);
                At("Pendant light", orderX + 0.64f, 2.756f, OrderZ + 0.08f);
                At("Round planter", plot.Width - 0.66f, 0, plot.StairZ - 2.59f, 55);
                if (plot.GroundRoom == CityRoom.Cafe)
                {
                    At(Shelves, 0.48f, 0, 3.71f, -90);
                    At("Cup stack", orderX - 0.07f, 1.039f, OrderZ + 0.08f);
                }
            }

            void AddSeats(float x, float z)
            {
                At(Table, x, 0, z);
                At(Chair, x - 0.70f, 0, z - 0.01f, 93);
                At(Chair, x + 0.70f, 0, z + 0.045f, 264);
                if (furnished)
                {
                    At("Desk plant", x + 0.078f, 0.751f, z + 0.07f, 35);
                    At("Pendant light", x, 2.756f, z);
                }
            }
        }

        private static void CafeTable(CityModel m, CityPalette p)
        {
            Trim(m, p.Timber, 0, 0.722f, 0, 0.80f, 0.056f, 0.69f, 0.025f);
            Trim(m, p.Dark, 0, 0.668f, 0, 0.70f, 0.062f, 0.565f, 0.012f);
            foreach (int x in new[] { -1, 1 })
            foreach (int z in new[] { -1, 1 })
                m.Frustum(p.Timber, new Vector3(x * 0.346f, 0.015f, z * 0.302f),
                    new Vector3(x * 0.291f, 0.66f, z * 0.24f), 0.022f, 0.036f, 10);
            m.CollisionBox(new Vector3(0, 0.374f, 0), new Vector3(0.80f, 0.747f, 0.69f));
        }

        private static void CafeChair(CityModel m, CityPalette p)
        {
            Trim(m, p.Timber, 0, 0.445f, 0, 0.436f, 0.055f, 0.43f, 0.024f);
            Trim(m, p.Olive, 0, 0.479f, 0.02f, 0.393f, 0.036f, 0.375f, 0.015f);
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * 0.167f;
                m.Frustum(p.Timber, new Vector3(x * 1.28f, 0.02f, 0.235f), new Vector3(x, 0.425f, 0.158f),
                    0.014f, 0.023f, 10);
                m.Frustum(p.Timber, new Vector3(x * 1.29f, 0.018f, -0.239f), new Vector3(x, 0.44f, -0.158f),
                    0.014f, 0.026f, 10);
                m.Tube(p.Timber, new Vector3(x, 0.43f, -0.161f), new Vector3(x * 1.13f, 0.849f, -0.241f), 0.021f);
                m.Tube(p.Brass, new Vector3(x * 1.11f, 0.203f, -0.193f),
                    new Vector3(x * 1.11f, 0.203f, 0.198f), 0.009f, 8);
            }
            Vector3 last = new Vector3(-0.20f, 0.843f, -0.24f);
            for (int i = 1; i <= 8; i++)
            {
                float u = i / 8f;
                var next = new Vector3(-0.20f + 0.40f * u, 0.843f + Mathf.Sin(Mathf.PI * u) * 0.042f,
                    -0.24f - Mathf.Sin(Mathf.PI * u) * 0.025f);
                m.Tube(p.Timber, last, next, 0.027f, 10);
                last = next;
            }
            foreach (float x in new[] { -0.10f, 0f, 0.10f })
                m.Tube(p.Timber, new Vector3(x, 0.46f, -0.169f), new Vector3(x, 0.86f, -0.253f), 0.014f, 8);
            m.CollisionBox(new Vector3(0, 0.395f, -0.017f), new Vector3(0.45f, 0.79f, 0.48f));
        }

        private static void Counter(CityModel m, CityPalette p)
        {
            Trim(m, p.Dark, 0, 0.058f, 0, 1.88f, 0.116f, 0.548f, 0.012f);
            Trim(m, p.Timber, 0, 0.548f, 0, 1.98f, 0.885f, 0.626f, 0.021f);
            for (int i = -16; i <= 16; i++)
                Trim(m, p.Timber, i * 0.057f, 0.537f, -0.320f, 0.031f, 0.808f, 0.043f, 0.006f);
            Trim(m, p.Brass, 0, 0.151f, -0.34f, 1.91f, 0.022f, 0.031f, 0.004f);
            Trim(m, p.Stone, 0, 1.011f, -0.014f, 2.05f, 0.055f, 0.724f, 0.016f);
            for (int side = -1; side <= 1; side += 2)
                Trim(m, p.Dark, side * 0.51f, 0.794f, 0.317f, 0.68f, 0.025f, 0.021f);
            m.CollisionBox(new Vector3(0, 0.51f, 0), new Vector3(2.05f, 1.02f, 0.70f));
        }

        private static void Cabinet(CityModel m, CityPalette p)
        {
            Trim(m, p.Timber, 0, 0.255f, 0.018f, 1.95f, 0.51f, 0.463f, 0.016f);
            Trim(m, p.Dark, 0, 0.045f, 0, 1.84f, 0.09f, 0.453f);
            Trim(m, p.Timber, 0, 1.317f, 0.203f, 1.905f, 1.68f, 0.071f);
            foreach (int side in new[] { -1, 1 })
            {
                Trim(m, p.Timber, 0.949f * side, 1.36f, 0, 0.052f, 1.69f, 0.44f);
                Trim(m, p.Brass, 0.28f * side, 0.364f, -0.223f, 0.107f, 0.024f, 0.022f);
            }
            for (int level = 0; level < 4; level++)
            {
                float y = 0.54f + level * 0.408f;
                Trim(m, p.Timber, 0, y, 0, 1.962f, 0.052f, 0.464f);
                Trim(m, p.Light, 0, y - 0.027f, -0.151f, 1.85f, 0.008f, 0.015f);
                for (int slot = 0; slot < 6; slot++)
                {
                    float x = (slot - 2.5f) * 0.272f;
                    Goods(m, p, new Vector3(x, y + 0.028f, -0.034f + 0.016f * (slot % 3)), slot + level * 2);
                }
            }
            Trim(m, p.Timber, 0, 2.176f, 0, 1.985f, 0.072f, 0.486f);
            m.CollisionBox(new Vector3(0, 1.098f, 0), new Vector3(1.985f, 2.20f, 0.486f));
        }

        private static void Display(CityModel m, CityPalette p)
        {
            Trim(m, p.Timber, 0, 0.783f, 0, 1.67f, 0.062f, 0.62f, 0.022f);
            Trim(m, p.Timber, 0, 0.323f, 0, 1.505f, 0.057f, 0.52f);
            foreach (int x in new[] { -1, 1 })
            foreach (int z in new[] { -1, 1 })
                m.Frustum(p.Timber, new Vector3(x * 0.746f, 0.010f, z * 0.267f),
                    new Vector3(x * 0.673f, 0.766f, z * 0.218f), 0.026f, 0.039f, 8);
            for (int i = 0; i < 5; i++)
            {
                Goods(m, p, new Vector3((i - 2) * 0.279f, 0.814f, 0.102f), i);
                Goods(m, p, new Vector3((i - 2) * 0.274f + 0.068f, 0.352f, -0.061f), i + 2);
            }
            Trim(m, p.Brass, -0.52f, 0.852f, -0.15f, 0.34f, 0.018f, 0.221f);
            foreach (float x in new[] { -0.594f, -0.458f, 0.48f, 0.606f })
            {
                var foot = new Vector3(x, 0.828f, -0.136f);
                m.Frustum(p.Paint, foot, foot + Vector3.up * 0.106f, 0.042f, 0.056f, 14);
                m.Tube(p.Textile, foot + Vector3.up * 0.104f, foot + Vector3.up * 0.108f, 0.041f, 12);
            }
            m.CollisionBox(new Vector3(0, 0.41f, 0), new Vector3(1.67f, 0.82f, 0.62f));
        }

        private static void Goods(CityModel m, CityPalette p, Vector3 at, int variant)
        {
            if (variant % 3 == 0)
            {
                m.Tube(p.Olive, at, at + Vector3.up * 0.163f, 0.052f, 12);
                m.Tube(p.Metal, at + Vector3.up * 0.161f, at + Vector3.up * 0.181f, 0.054f, 12);
                Trim(m, p.White, at.x, at.y + 0.090f, at.z - 0.053f, 0.061f, 0.072f, 0.006f);
            }
            else
            {
                float h = variant % 3 == 1 ? 0.218f : 0.141f;
                Material packet = variant % 3 == 1 ? p.Card : p.Textile;
                Trim(m, packet, at.x, at.y + h / 2, at.z, 0.128f, h, 0.079f, 0.018f);
                Trim(m, p.Paint, at.x, at.y + h * 0.56f, at.z - 0.04f, 0.084f, 0.065f, 0.009f);
                Trim(m, p.Brass, at.x, at.y + h * 0.80f, at.z - 0.044f, 0.051f, 0.009f, 0.006f);
            }
        }

        private static void WallLight(CityModel m, CityPalette p)
        {
            Trim(m, p.Brass, 0, 0.148f, 0.019f, 0.18f, 0.264f, 0.045f, 0.012f);
            m.Tube(p.Brass, new Vector3(0, 0.166f, 0.026f), new Vector3(0, 0.166f, 0.162f), 0.017f);
            m.Tube(p.Light, new Vector3(0, 0.021f, 0.154f), new Vector3(0, 0.256f, 0.154f), 0.058f, 16);
            foreach (float y in new[] { 0.015f, 0.261f })
                m.Tube(p.Brass, new Vector3(0, y - 0.012f, 0.154f), new Vector3(0, y + 0.012f, 0.154f), 0.068f, 16);
        }

        private static void Trim(CityModel m, Material finish, float x, float y, float z,
            float width, float height, float depth, float bevel = 0.006f) =>
            m.BeveledBox(finish, new Vector3(x, y, z), new Vector3(width, height, depth), bevel);
    }
}
