using UnityEngine;
using Random = System.Random;

namespace Vastcore.ModernCity.Editor
{
    internal enum CityDetail { Essential, Furnished }

    internal sealed class CityFurnishing
    {
        private readonly CityKitAssets kit;
        private readonly bool furnished;
        private Transform parent;

        private CityFurnishing(CityKitAssets kit, CityDetail detail)
        {
            this.kit = kit;
            furnished = detail == CityDetail.Furnished;
        }

        internal static GameObject Dress(CityKitAssets kit, CityBlock block, CityDetail detail, int seed)
        {
            var dressing = new CityFurnishing(kit, detail);
            var root = CityKitAssets.Group("Street props", block.Root, Vector3.zero);
            dressing.Street(root.transform, block, seed);
            foreach (var plot in block.Plots)
                dressing.Building(plot, block.Buildings[plot], seed);
            return root;
        }

        internal static void DressBuilding(CityKitAssets kit, CityPlot plot, Transform building, CityDetail detail,
            int seed)
        {
            new CityFurnishing(kit, detail).Building(plot, building, seed);
        }

        private void Street(Transform street, CityBlock block, int seed)
        {
            parent = street;
            var rng = new Random(unchecked(seed * 29 + 71));
            float junction = block.Plots[2].Origin.x - 8;
            float kerb = CityStreetKit.Pavement;
            foreach (float x in new[] { -46f, -33f, -17f, 14f, 29f, 43f })
            {
                float curbX = x + junction;
                At("Street lamp", curbX, kerb, 6.55f, 180);
                At("Street lamp", x + 2.5f, kerb, -6.55f);
                if (furnished)
                {
                    float treeOffset = x == 14 ? 6.65f : x == 29 ? 4.46f : 2.65f;
                    At("Street tree", curbX + treeOffset, kerb, x == 29 ? 6.59f : 7.09f, rng.Next(360));
                    At("Courtyard tree", x - 1.6f, kerb, -9.14f, rng.Next(360));
                    At("Storm drain", curbX + 1.1f, 0, 5.77f);
                }
            }

            foreach (float x in new[] { junction - 7.8f, junction + 7.8f })
            {
                At("Signal pole", x, kerb, 7.14f, 180);
                At("Signal pole", x, kerb, -7.14f, 0);
                for (int i = -1; i <= 1; i++)
                    At("Bollard", x + i * 0.81f, kerb, 6.22f);
            }
            if (!furnished)
                return;

            At("Street bench", junction - 18.2f, kerb, 7.7f);
            At("Street bench", junction + 26.9f, kerb, -9.3f, 180);
            At("Litter bin", junction - 19.72f, kerb, 7.8f);
            At("Litter bin", junction + 28.48f, kerb, -9.2f);
            At("Bike rack", junction - 12.8f, kerb, 9.12f);
            At("City bicycle", junction - 12.54f, kerb, 8.93f, 79);
            At("Utility cabinet", junction + 5.95f, kerb, 17.4f, -90);
            At("Steel bin pair", junction - 5.76f, kerb, 21.78f, 90);
            At("Sedan", junction - 28.25f, 0, 4.33f, -90);
            At("Delivery van", junction + 20.65f, 0, 4.62f, -90);
            At("Sedan", junction + 34.85f, 0, -4.59f, 90);
            At("Manhole", junction - 2.5f, 0, 2.17f);
            At("Manhole", junction + 17.9f, 0, -3.21f, 60);
            At("Street shelter", 58, 0.115f, 12.85f, 180);
            At("Street shelter", 85, 0.115f, -12.75f);
            foreach (int x in new[] { 50, 60, 80, 94, 108 })
            {
                At("Street lamp", x, kerb, 6.54f, 180);
                At("Courtyard tree", x + 0.9f, 0.116f, 16.65f, rng.Next(360));
                At("Courtyard tree", x - 0.56f, 0.116f, -23.8f, rng.Next(360));
            }
            foreach (int x in new[] { -29, -17, 16, 30, 49, 61, 87, 104 })
            {
                At("Courtyard tree", x, -0.06f, 53.4f + rng.Next(4), rng.Next(360));
                At("Street lamp", x + 2.2f, kerb, 47.15f);
            }
            At("Delivery van", 56, 0, -16, 90);
            At("Sedan", 44, 0, -16, 90);
            foreach (int x in new[] { -36, -26, -12, 5, 17, 32, 43 })
                At("Courtyard tree", x, 0.115f, -42 + rng.Next(-2, 3), rng.Next(360));
            float plaza = kerb - CityStreetKit.ApronDrop;
            At("Street tree", -48.5f, plaza, -4.31f, 28);
            At("Street tree", -49.25f, plaza, 4.47f, 114);
            At("Street bench", -47.8f, plaza, -3.21f, 90);
            for (int i = 0; i < 5; i++)
            {
                At("Courtyard tree", junction - 13.2f - 3.7f * i, -0.06f, 30.3f + i % 2, rng.Next(360));
                At("Courtyard tree", junction + 10 + 4.1f * i, -0.06f, 31.4f + i % 2, rng.Next(360));
            }
        }

        private void Building(CityPlot plot, Transform building, int seed)
        {
            var rng = new Random(unchecked(seed * 23 + Mathf.RoundToInt(plot.Origin.x * 17)));
            var exterior = CityKitAssets.Group("Storefront and service yard", building, Vector3.zero).transform;
            parent = exterior;
            string sign = plot.GroundRoom switch
            {
                CityRoom.Cafe => "Cafe sign",
                CityRoom.Reception or CityRoom.Office or CityRoom.Studio => "Office sign",
                CityRoom.Storage => "Service sign",
                _ => "Shop sign"
            };
            At(sign, plot.EntranceX, 2.26f, plot.RecessedStorefront ? -0.395f : -0.173f, 180);
            At("Service sign", 2, 2.1f, plot.Length + 0.17f);
            At("Loading stripes", 2, 0.006f, plot.Length + 1.35f);
            At("Floor mat", plot.EntranceX, 0.002f, plot.RecessedStorefront ? 1.16f : 0.94f);
            if (furnished)
            {
                At("Limestone planter", plot.EntranceX + 2.37f, 0, -0.56f, 180);
                At("Round planter", plot.EntranceX - 2.06f, 0, -0.49f, 120);
                if (plot.GroundRoom != CityRoom.Reception)
                {
                    At("Sandwich board", plot.EntranceX + 1.48f, 0, -1.06f, 13);
                    At("Wall condenser", plot.Width - 1.15f, 2.94f, plot.Length + 0.29f);
                }
                At("Pallet", 4.55f, 0, plot.Length + 0.88f, -8);
                At("Shipping carton", 4.6f, 0.15f, plot.Length + 0.92f, -12);
                At("Hand truck", 3.71f, 0, plot.Length + 0.85f, -25);
                At("Waste cart", 0.63f, 0, plot.Length + 1.27f, 30);
            }

            for (int i = 0; i < plot.Floors; i++)
            {
                var level = building.Find($"Floor {i + 1}");
                var interior = CityKitAssets.Group("Interior props", level, Vector3.zero).transform;
                float setback = plot.FrontBay(i) * CityBuildingKit.Bay;
                parent = setback == 0 ? interior :
                    CityKitAssets.Group("Setback room", interior, Vector3.forward * setback).transform;
                var use = i == 0 ? plot.GroundRoom : plot.UpperRoom;
                bool storefront = i == 0 && plot.RecessedStorefront;
                if (storefront)
                {
                    CityCafeFurnishings.Place(kit, plot, parent, furnished);
                }
                else
                {
                    switch (use)
                    {
                        case CityRoom.Cafe: Cafe(plot, rng); break;
                        case CityRoom.Retail: Retail(plot); break;
                        case CityRoom.Reception: Reception(plot); break;
                        case CityRoom.Office: Office(plot, rng, plot.Length - setback); break;
                        case CityRoom.Studio: Studio(plot, rng, plot.Length - setback); break;
                        case CityRoom.Storage: Storage(plot); break;
                    }
                }
                parent = interior;
                ServiceRoom(plot);
                FloorLighting(plot, use, storefront, setback);
                if (i == 0 && (use == CityRoom.Reception || use == CityRoom.Storage))
                {
                    At("Bronze wall lantern", 3.23f, 1.62f, plot.Length + 0.16f);
                    Spot("Loading recess light", 2.18f, 2.27f, plot.Length + 0.68f,
                        new Vector3(2.08f, 0.72f, plot.Length - 0.19f), 4.2f, 89, 1.5f);
                }
                if (setback != 0 && furnished)
                {
                    At("Limestone planter", 1.26f, 0, 0.68f);
                    At("Limestone planter", plot.Width - 1.26f, 0, 0.68f);
                    At("Street bench", 3.2f, 0, 0.69f);
                }
            }

            parent = CityKitAssets.Group("Roof equipment", building.Find("Roof"), Vector3.zero).transform;
            At("Roof air conditioner", plot.Width * 0.54f, 0, plot.Length * 0.62f, 180);
            if (furnished)
            {
                At("Roof duct", plot.Width * 0.54f + 1.58f, 0, plot.Length * 0.62f, 16);
                At("Exhaust cowl", 2.2f, 0, plot.Length - 2.34f);
                At("Service pipe bundle", plot.Width - 2.31f, 0, plot.Length - 1.82f, 90);
                At("Roof antenna", plot.Width - 1.5f, 0, plot.Length * 0.57f, rng.Next(180));
            }
        }

        private void Cafe(CityPlot plot, Random rng)
        {
            At("Shop counter", 0.84f, 0, 2.02f, 90);
            At("Espresso machine", 0.93f, 1.005f, 1.68f, 90);
            At("Round cafe table", 2.65f, 0, 1.29f);
            At("Cafe chair", 1.89f, 0, 1.29f, 90);
            At("Cafe chair", 3.41f, 0, 1.29f, 270);
            if (!furnished)
                return;

            At("Cup stack", 0.87f, 1.004f, 2.41f, 60);
            At("Product shelving", 0.45f, 0, 3.83f, 90);
            At("Glass display case", 0.79f, 0, 4.96f, 90);
            At("Point of sale", 0.915f, 1.002f, 2.68f, 70);
            At("Round cafe table", 5.52f, 0, 1.43f);
            At("Cafe chair", 4.76f, 0, 1.43f, 90);
            At("Cafe chair", 6.28f, 0, 1.43f, 271 + rng.Next(-13, 13));
            At("Desk plant", 2.77f, 0.739f, 1.41f, 35);
            At("Bottle tray", 5.55f, 0.739f, 1.46f);
            At("Bistro stool", 7.42f, 0, 0.94f);
            At("Round planter", plot.Width - 0.65f, 0, 1.25f, 55);
            At("Pendant light", 2.65f, 2.76f, 1.29f);
            At("Pendant light", 5.52f, 2.76f, 1.43f);
            At("Timber ceiling slats", 2, 2.755f, 2);
            At("Timber ceiling slats", 6, 2.755f, 2);
        }

        private void Retail(CityPlot plot)
        {
            At("Shop counter", 0.78f, 0, 2.08f, 90);
            At("Product shelving", 3.59f, 0, 0.58f);
            if (!furnished)
                return;

            At("Point of sale", 0.78f, 1.003f, 2.06f, 90);
            At("Product shelving", 0.46f, 0, plot.Length - 5.1f, 90);
            At("Glass display case", plot.EntranceX + 1.65f, 0, 0.56f);
            At("Bottle tray", 0.87f, 1.006f, 2.54f, 90);
            At("Sideboard", 4.25f, 0, plot.Length - 5.2f, 7);
            At("Desk plant", 4.87f, 0.81f, plot.Length - 5.2f);
        }

        private void Reception(CityPlot plot)
        {
            float desk = plot.EntranceX - 1.8f;
            At("Reception desk", desk, 0, 4.75f, 180);
            At("Lobby directory", desk, 0, 6.28f);
            At("Lobby sofa", plot.EntranceX - 3.8f, 0, 0.67f);
            At("Low coffee table", plot.EntranceX - 3.82f, 0, 1.62f);
            At("Floor mat", desk, 0.002f, 3.97f);
            if (!furnished)
                return;

            At("Monitor", desk + 0.28f, 1.09f, 4.83f, 180);
            At("Desk plant", desk - 0.76f, 1.09f, 4.69f);
            At("Lounge chair", plot.EntranceX - 2.65f, 0, 1.35f, -50);
            At("Floor lamp", plot.EntranceX - 5.18f, 0, 0.77f);
            At("Round planter", plot.EntranceX + 2.25f, 0, 1.19f, 40);
            if (plot.Across >= 4)
            {
                At("Lounge chair", 4.25f, 0, 5.09f, 42);
                At("Low coffee table", 4.9f, 0, 5.41f, 30);
                At("Round planter", 3.44f, 0, 5.86f, 20);
                At("Floor lamp", 5.42f, 0, 6.38f);
            }
        }

        private void Office(CityPlot plot, Random rng, float length)
        {
            for (float x = 2.35f; x < plot.Width - 4.2f; x += 2.7f)
            {
                At("Office desk", x, 0, 1.08f);
                At("Office chair", x, 0, 1.91f, 180 + rng.Next(-13, 13));
                if (!furnished)
                    continue;
                At("Monitor", x, 0.74f, 0.96f, rng.Next(-12, 12));
                At("Keyboard", x, 0.74f, 1.25f);
                At("Computer tower", x + 0.48f, 0, 0.94f);
                At(rng.Next(2) == 0 ? "Desk plant" : "Desk lamp", x - 0.46f, 0.74f, 0.99f);
            }
            if (length > 12)
            {
                At("Meeting table", 5.26f, 0, 5.99f);
                At("Visitor chair", 4.67f, 0, 5.05f);
                At("Visitor chair", 5.9f, 0, 6.94f, 180);
            }
            if (furnished)
            {
                At("Water cooler", 0.42f, 0, 2.18f, 90);
                At("Sideboard", 0.69f, 0, length - 5.4f, 90);
                At("Laser printer", 0.69f, 0.81f, length - 5.61f, 90);
                At("Document binders", 0.68f, 0.81f, length - 4.99f, 90);
                At("Whiteboard", 0.151f, 1.03f, 3.65f, 90);
                At("Wall clock", 0.15f, 2.22f, 3.65f, 90);
            }
        }

        private void Studio(CityPlot plot, Random rng, float length)
        {
            At("Lobby sofa", 3.4f, 0, 0.61f);
            At("Low coffee table", 3.41f, 0, 1.6f);
            At("Lounge chair", 4.88f, 0, 1.19f, -88 + rng.Next(-11, 11));
            if (furnished)
            {
                At("Floor lamp", 2.22f, 0, 0.59f);
                At("Round planter", plot.Width - 1.08f, 0, 0.82f, 67);
                At("Sideboard", 0.68f, 0, length - 5.16f, 90);
                At("Desk lamp", 0.7f, 0.813f, length - 4.81f, 90);
                At("Document binders", 0.61f, 0.813f, length - 5.59f, 90);
            }
        }

        private void Storage(CityPlot plot)
        {
            At("Workshop bench", 5.6f, 0, 0.85f);
            At("Spare storage rack", 3.2f, 0, 0.65f);
            if (furnished)
            {
                At("Hand truck", 0.86f, 0, 1.63f, -25);
                At("Mop bucket", 0.76f, 0, plot.Length - 4.68f, 20);
                At("Pallet", 8.26f, 0, 1.14f, -12);
                At("Shipping carton", 8.23f, 0.15f, 1.15f, 7);
            }
        }

        private void ServiceRoom(CityPlot plot)
        {
            At("Spare storage rack", 4.63f, 0, plot.Length - 0.71f, 180);
            if (!furnished)
                return;

            At("Shipping carton", 4.18f, 0.16f, plot.Length - 0.72f, 180);
            At("Shipping carton", 4.98f, 0.63f, plot.Length - 0.73f, 180);
            At("Breaker panel", 0.22f, 1.05f, plot.Length - 1.13f, 90);
            At("Fire extinguisher", 0.36f, 0.05f, plot.Length - 1.86f, 90);
            At("Hand truck", 5.27f, 0, plot.Length - 2.46f, -13);
            At("Service pipe bundle", 0.51f, 0, plot.Length - 3.6f, -2);
        }

        private void FloorLighting(CityPlot plot, CityRoom use, bool storefront, float setback)
        {
            for (float x = 2; x <= plot.Width - 2; x += 4)
            {
                At("Linear ceiling light", x, 2.753f, 1.98f + setback);
                At("Linear ceiling light", x, 2.753f, plot.Length - 6.22f);
            }
            if (use == CityRoom.Reception)
            {
                float desk = plot.EntranceX - 1.8f;
                Spot("Reception and directory", desk + 0.71f, 2.58f, 4.01f,
                    new Vector3(desk, 0.95f, 6.04f), 5.5f, 76, 3.6f);
                Spot("Window seating", plot.EntranceX - 3.2f, 2.66f, 2.2f,
                    new Vector3(plot.EntranceX - 3.8f, 0.56f, 1.24f), 5.1f, 85, 1.6f);
            }
            else if (!storefront)
                PointLight("Front room light", Mathf.Min(5.2f, plot.Width * 0.32f), 2.28f, 2.61f + setback, 7.6f,
                    use == CityRoom.Cafe ? new Color(1, 0.82f, 0.61f) : new Color(1, 0.94f, 0.83f));
            if (storefront)
            {
                Spot("Front room fill", plot.EntranceX - 1.54f, 2.65f, 2.05f,
                    new Vector3(plot.EntranceX - 3.15f, .83f, 2.68f), 6.5f, 112, 2.2f);
                PointLight("Window merchandise", plot.Width - 2.05f, 1.84f, .76f, 3.62f,
                    new Color(1, 0.87f, 0.71f), 0.59f);
                PointLight("Order and wall shelves", CityCafeFurnishings.OrderX(plot), 1.94f,
                    CityCafeFurnishings.OrderZ + 0.77f, 3.94f, new Color(1, 0.84f, 0.65f), 0.70f);
                PointLight("Stair opening wall wash", plot.Width - 0.68f, 1.67f, plot.StairZ - 0.56f,
                    3.3f, new Color(1, 0.82f, 0.61f), 0.47f);
                return;
            }
            PointLight("Landing light", plot.StairX, 2.17f, plot.StairZ - 0.63f, 5.4f, new Color(1, 0.91f, 0.77f));
        }

        private void Spot(string name, float x, float y, float z, Vector3 aim, float range, float angle, float intensity)
        {
            var from = new Vector3(x, y, z);
            var light = CityKitAssets.Group(name, parent, from).AddComponent<Light>();
            light.transform.localRotation = Quaternion.LookRotation(aim - from);
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.87f, 0.72f);
            light.range = range;
            light.spotAngle = angle;
            light.innerSpotAngle = angle * 0.58f;
            light.intensity = intensity;
        }

        private void PointLight(string name, float x, float y, float z, float range, Color color, float intensity = 0.88f)
        {
            var fixture = CityKitAssets.Group(name, parent, new Vector3(x, y, z));
            var light = fixture.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        private void At(string name, float x, float y, float z, float yaw = 0) =>
            kit.Place(CityKitAssets.Props, name, parent, new Vector3(x, y, z), yaw);
    }
}
