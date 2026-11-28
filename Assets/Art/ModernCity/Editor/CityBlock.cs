using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace Vastcore.ModernCity.Editor
{
    internal enum CityRoom { Cafe, Retail, Reception, Office, Studio, Storage }
    internal enum CityStreetPlan { CornerShops, SideStreet }
    internal enum CityRoof { Parapet, Cornice, Lantern, Monitor }

    internal sealed class CityPlot
    {
        internal readonly string Name;
        internal readonly Vector3 Origin;
        internal readonly int Across;
        internal readonly int Deep;
        internal readonly int Floors;
        internal readonly float Yaw;
        internal readonly CityFacade Facade;
        internal readonly CityRoom GroundRoom;
        internal readonly CityRoom UpperRoom;
        internal readonly bool RecessedStorefront;
        internal readonly bool UpperSetback;
        internal readonly CityRoof Roof;
        internal readonly bool Forecourt;

        internal CityPlot(string name, float x, float z, int across, int deep, int floors, float yaw,
            CityFacade facade, CityRoom groundRoom, CityRoom upperRoom, float elevation = CityStreetKit.Pavement,
            bool recessedStorefront = false, bool upperSetback = false, CityRoof roof = CityRoof.Parapet,
            bool forecourt = false)
        {
            Name = name;
            Origin = new Vector3(x, elevation, z);
            Across = across;
            Deep = deep;
            Floors = floors;
            Yaw = yaw;
            Facade = facade;
            GroundRoom = groundRoom;
            UpperRoom = upperRoom;
            RecessedStorefront = recessedStorefront;
            UpperSetback = upperSetback && floors >= 3;
            Roof = roof;
            Forecourt = forecourt;
        }

        internal float Width => Across * CityBuildingKit.Bay;
        internal float Length => Deep * CityBuildingKit.Bay;
        internal float EntranceX => (Across / 2 + 0.5f) * CityBuildingKit.Bay;
        internal float StairX => Width - CityBuildingKit.Bay / 2;
        internal float StairZ => Length - CityBuildingKit.Bay;
        internal int FrontBay(int floor) => UpperSetback && floor >= Floors - 1 ? 1 : 0;
        internal Vector3 World(float x, float y, float z) =>
            Origin + Quaternion.Euler(0, Yaw, 0) * new Vector3(x, y, z);
    }

    internal sealed class CityBlock
    {
        internal const int DefaultSeed = 417;
        internal const float DefaultSun = 38;
        internal readonly Scene Scene;
        internal readonly Light Sun;
        internal readonly CityPlot[] Plots;
        internal readonly Transform Root;
        internal readonly Dictionary<CityPlot, Transform> Buildings = new Dictionary<CityPlot, Transform>();

        private CityBlock(Scene scene, Transform root, Light sun, CityPlot[] plots)
        {
            Scene = scene;
            Root = root;
            Sun = sun;
            Plots = plots;
        }

        internal static CityBlock Assemble(CityKitAssets kit, int seed, CityStreetPlan street)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject($"Modern City — {street} · seed {seed}").transform;
            bool narrow = street == CityStreetPlan.SideStreet;
            var plots = Design(seed, street);
            CityStreetKit.Assemble(kit, root, narrow);
            var sun = LightScene();
            CreateWalkthrough(plots[0]);
            var block = new CityBlock(scene, root, sun, plots);
            foreach (var plot in plots)
                block.Buildings.Add(plot, AssembleBuilding(kit, plot, root, seed));
            return block;
        }

        internal static CityPlot[] Design(int seed, CityStreetPlan street)
        {
            var rng = new Random(seed);
            bool narrow = street == CityStreetPlan.SideStreet;
            float junction = narrow ? 8f : 0f;
            float southEdge = narrow ? -10f : -10.5f;
            int offset = narrow ? 1 : 0;
            return new[]
            {
                new CityPlot("Corner cafe", junction - 24, 10.2f, 4, 3 + rng.Next(2), 3 + rng.Next(2), 0,
                    CityFacade.Stone, CityRoom.Cafe, CityRoom.Studio, recessedStorefront: !narrow),
                new CityPlot("Brick shops", junction - 42 - offset * 4, 10.7f, 3 + rng.Next(2), 3, 2 + rng.Next(2), 0,
                    CityFacade.Brick, CityRoom.Retail, CityRoom.Office, roof: CityRoof.Cornice),
                new CityPlot("Corner offices", junction + 8, 10.4f, 5, 4, 5 + rng.Next(2), 0,
                    CityFacade.Glass, CityRoom.Reception, CityRoom.Office, upperSetback: true),
                new CityPlot("Stone shop row", junction + 30, 11f, 3, 3 + rng.Next(2), 3 + rng.Next(2), 0,
                    CityFacade.Stone, CityRoom.Retail, CityRoom.Office, recessedStorefront: true),
                new CityPlot("Across the street", 26, southEdge, 4 + rng.Next(2), 3, 3 + rng.Next(2), 180,
                    CityFacade.Brick, CityRoom.Cafe, CityRoom.Studio, upperSetback: true),
                new CityPlot("Lane building", 3, -11.4f, 3, 3, 2 + rng.Next(2), 180,
                    CityFacade.Stone, CityRoom.Retail, CityRoom.Storage, roof: CityRoof.Lantern),
                new CityPlot("South offices", -11, southEdge - 0.5f, 4, 4, 4 + rng.Next(2), 180,
                    CityFacade.Glass, CityRoom.Reception, CityRoom.Office, upperSetback: true),
                new CityPlot("End of the block", -29, -11.6f, 3, 3, 2 + rng.Next(2), 180,
                    CityFacade.Brick, CityRoom.Retail, CityRoom.Studio),
                new CityPlot("Market square", -52, -6, 3, 3, 3 + rng.Next(2), -90,
                    CityFacade.Stone, CityRoom.Retail, CityRoom.Studio, roof: CityRoof.Cornice),
                new CityPlot("East corner offices", 82, 10.65f, 4, 3, narrow ? 4 : 5, 0,
                    CityFacade.Glass, CityRoom.Reception, CityRoom.Office, upperSetback: true, forecourt: true),
                new CityPlot("East shop row", 124, 7f, 3, 3, 2 + rng.Next(2), 90,
                    CityFacade.Stone, CityRoom.Retail, CityRoom.Studio, recessedStorefront: true,
                    roof: CityRoof.Lantern, forecourt: true),
                new CityPlot("South crossing studios", 108, -10.65f, 4, 3, narrow ? 4 : 3, 180,
                    CityFacade.Brick, CityRoom.Retail, CityRoom.Studio, roof: CityRoof.Cornice, forecourt: true),
                new CityPlot("Rear workshops", 20, -42.25f, 4, 3, 2, 180,
                    CityFacade.Brick, CityRoom.Storage, CityRoom.Studio, roof: CityRoof.Monitor, forecourt: true)
            };
        }

        internal static Transform AssembleBuilding(CityKitAssets kit, CityPlot plot, Transform parent, int seed)
        {
            var building = CityKitAssets.Group(plot.Name, parent, plot.Origin, plot.Yaw).transform;
            var rng = new Random(unchecked(seed * 13 + Mathf.RoundToInt(plot.Origin.x * 31 + plot.Origin.z * 67)));
            for (int floor = 0; floor < plot.Floors; floor++)
            {
                float y = floor * CityBuildingKit.Storey;
                var level = CityKitAssets.Group($"Floor {floor + 1}", building, new Vector3(0, y, 0)).transform;
                var use = floor == 0 ? plot.GroundRoom : plot.UpperRoom;
                string slab = SlabFor(use);
                int frontBay = plot.FrontBay(floor);
                for (int x = 0; x < plot.Across; x++)
                for (int z = 0; z < plot.Deep; z++)
                {
                    bool stairOpening = floor > 0 && x == plot.Across - 1 && z == plot.Deep - 1;
                    if (!stairOpening)
                        Place(kit, z < frontBay ? "Roof slab" : slab, level, (x + 0.5f) * 4, 0, (z + 0.5f) * 4);
                }

                AddFacades(kit, plot, level, floor, rng);
                AddPlan(kit, plot, level, floor, use);
                if (frontBay != 0)
                {
                    for (int x = 0; x < plot.Across; x++)
                        Place(kit, "Terrace guard", level, (x + 0.5f) * 4, 0, 0);
                    Place(kit, "Terrace guard", level, 0, 0, 2, 90);
                    Place(kit, "Terrace guard", level, plot.Width, 0, 2, -90);
                }
            }

            var roof = CityKitAssets.Group("Roof", building,
                new Vector3(0, plot.Floors * CityBuildingKit.Storey, 0)).transform;
            int roofFront = plot.FrontBay(plot.Floors - 1);
            string parapet = plot.Roof == CityRoof.Cornice ? "Deep cornice parapet" : "Parapet";
            for (int x = 0; x < plot.Across; x++)
            {
                float centre = (x + 0.5f) * CityBuildingKit.Bay;
                for (int z = roofFront; z < plot.Deep; z++)
                    Place(kit, "Roof slab", roof, centre, 0, (z + 0.5f) * CityBuildingKit.Bay);
                Place(kit, parapet, roof, centre, 0, roofFront * CityBuildingKit.Bay);
                Place(kit, parapet, roof, centre, 0, plot.Length, 180);
            }
            for (int z = roofFront; z < plot.Deep; z++)
            {
                float centre = (z + 0.5f) * CityBuildingKit.Bay;
                Place(kit, parapet, roof, 0, 0, centre, 90);
                Place(kit, parapet, roof, plot.Width, 0, centre, -90);
            }

            if (plot.UpperSetback && plot.Facade == CityFacade.Glass)
                Place(kit, "Roof plant screen", roof, plot.Width * 0.5f, 0, plot.Length - 5.0f);
            if (plot.Roof == CityRoof.Lantern)
                Place(kit, "Roof lantern", roof, plot.Width - 4.15f, 0, plot.Length - 4.1f);
            if (plot.Roof == CityRoof.Monitor)
                for (float x = 4; x < plot.Width - 3; x += 4)
                    Place(kit, "Workshop roof monitor", roof, x, 0, plot.Length * 0.5f);

            // Link service doors to the slab behind each building and to the ground along its side.
            for (int x = 0; x < plot.Across; x++)
            {
                float centre = (x + 0.5f) * CityBuildingKit.Bay;
                kit.Place(CityKitAssets.Street, "Paving tile", building,
                    new Vector3(centre, -CityStreetKit.Pavement, plot.Length + 2));
                if (plot.Forecourt)
                    kit.Place(CityKitAssets.Street, "Paving tile", building,
                        new Vector3(centre, -CityStreetKit.Pavement - CityStreetKit.ApronDrop - 0.006f, -2));
            }
            for (int z = 0; z <= plot.Deep; z++)
            {
                float centre = (z + 0.5f) * CityBuildingKit.Bay;
                float side = plot.Across == 5 ? -2 : plot.Width + 2;
                kit.Place(CityKitAssets.Street, "Paving tile", building,
                    new Vector3(side, -CityStreetKit.Pavement - 0.03f, centre));
            }
            return building;
        }

        private static void AddFacades(CityKitAssets kit, CityPlot plot, Transform floorRoot, int floor, Random rng)
        {
            int frontBay = plot.FrontBay(floor);
            for (int x = 0; x < plot.Across; x++)
            {
                float cx = (x + 0.5f) * CityBuildingKit.Bay;
                bool entry = floor == 0 && x == plot.Across / 2;
                string front = floor == 0 ? StreetFront(plot, entry) : WindowFor(plot.Facade, rng.Next(2));
                Place(kit, front, floorRoot, cx, 0, frontBay * CityBuildingKit.Bay);
                if (floor == 0 && (entry || x == 0 && plot.Facade != CityFacade.Glass))
                {
                    string canopy = entry && plot.GroundRoom == CityRoom.Reception ? "Lobby canopy"
                        : plot.RecessedStorefront ? CityStorefrontKit.Canopy
                        : plot.Facade == CityFacade.Brick || plot.GroundRoom == CityRoom.Cafe ? "Canvas awning" : "Stone canopy";
                    Place(kit, canopy, floorRoot, cx, 0, 0);
                }
                if (floor > 0 && floor % 2 == 0 && x == 0 && plot.Facade == CityFacade.Stone)
                    Place(kit, "Timber screen", floorRoot, cx, 0, frontBay * CityBuildingKit.Bay);
                string back = floor == 0 && x == 0 ? "Service entrance" :
                    x == plot.Across - 1 ? BlankFor(plot.Facade) : WindowFor(plot.Facade, rng.Next(2));
                Place(kit, back, floorRoot, cx, 0, plot.Length, 180);
                if (floor == 0 && x == 0)
                    Place(kit, "Service canopy", floorRoot, cx, 0, plot.Length, 180);
            }

            for (int z = frontBay; z < plot.Deep; z++)
            {
                float centre = (z + 0.5f) * CityBuildingKit.Bay;
                string left = WindowFor(plot.Facade, rng.Next(2));
                if (floor == 0 && z == 0 && plot.RecessedStorefront)
                    left = CityStorefrontKit.Window;
                Place(kit, left, floorRoot, 0, 0, centre, 90);
                string right = z == plot.Deep - 1 ? BlankFor(plot.Facade) : WindowFor(plot.Facade, rng.Next(2));
                Place(kit, right, floorRoot, plot.Width, 0, centre, -90);
            }
        }

        private static void AddPlan(CityKitAssets kit, CityPlot plot, Transform level, int floor, CityRoom use)
        {
            bool storefront = floor == 0 && plot.RecessedStorefront;
            float front = plot.FrontBay(floor) * CityBuildingKit.Bay;
            float division = storefront || use == CityRoom.Reception || front > 0 ? plot.StairZ : plot.Length - 8;
            for (int x = 0; x < plot.Across - 1; x++)
                Place(kit, x == 0 || x == plot.Across - 2 ? "Open partition" : "Partition",
                    level, (x + 0.5f) * CityBuildingKit.Bay, 0, division);
            Place(kit, "Open partition", level, plot.Width - 4, 0, division + 2, 90);
            Place(kit, "Column", level, 4, 0, front + 4);
            if (plot.Across >= 4)
                Place(kit, "Column", level, plot.Width - 4, 0, front + 4);
            if (use == CityRoom.Office && plot.Length - front > 12)
                Place(kit, "Meeting room screen", level, 8, 0, 6, 90);
            if (floor < plot.Floors - 1)
                Place(kit, "Switchback stair", level, plot.StairX, 0, plot.StairZ);
            else if (floor > 0)
                Place(kit, "Half stair guard", level, plot.StairX - 1, 0, plot.StairZ);
            if (floor > 0)
                Place(kit, "Stair guard", level, plot.Width - 4, 0, plot.StairZ + 2, 90);
            if (storefront && plot.Floors > 1)
                Place(kit, CityStorefrontKit.StairPortal, level, plot.StairX, 0, plot.StairZ - 1.02f);
        }

        private static void Place(CityKitAssets kit, string name, Transform parent, float x, float y, float z, float yaw = 0) =>
            kit.Place(CityKitAssets.Buildings, name, parent, new Vector3(x, y, z), yaw);

        private static string SlabFor(CityRoom use) => use switch
        {
            CityRoom.Cafe or CityRoom.Studio => "Timber slab",
            CityRoom.Reception => "Lobby terrazzo slab",
            CityRoom.Office => "Office slab",
            CityRoom.Storage => "Paved slab",
            _ => "Plaster slab"
        };

        private static string WindowFor(CityFacade facade, int variation) => (facade, variation) switch
        {
            (CityFacade.Stone, 0) => "Stone window",
            (CityFacade.Stone, _) => "Stone loggia",
            (CityFacade.Brick, 0) => "Brick window",
            (CityFacade.Brick, _) => "Brick paired window",
            (CityFacade.Glass, 0) => "Office curtain wall",
            _ => "Office fin wall"
        };

        private static string BlankFor(CityFacade facade) => facade switch
        {
            CityFacade.Stone => "Stone blank bay",
            CityFacade.Brick => "Brick blank bay",
            _ => "Office blank bay"
        };

        private static string StreetFront(CityPlot plot, bool entry)
        {
            if (plot.GroundRoom == CityRoom.Reception)
                return entry ? "Lobby entrance" : "Lobby display";
            if (plot.RecessedStorefront)
                return entry ? CityStorefrontKit.Entry : CityStorefrontKit.Window;
            return (plot.Facade, entry) switch
            {
                (CityFacade.Stone, true) => "Shop entrance",
                (CityFacade.Stone, false) => "Shop display",
                (CityFacade.Brick, true) => "Brick shop entrance",
                (CityFacade.Brick, false) => "Brick shop display",
                (CityFacade.Glass, true) => "Lobby entrance",
                _ => "Lobby display"
            };
        }

        private static void CreateWalkthrough(CityPlot first)
        {
            var walker = CityKitAssets.Group("Walkthrough", null, first.World(first.EntranceX - 1.25f, 0.02f, -3.05f), 17);
            AddWalkerCollider(walker);
            var eye = CityKitAssets.Group("Main Camera", walker.transform, Vector3.up * 1.64f);
            eye.tag = "MainCamera";
            var camera = eye.AddComponent<Camera>();
            camera.nearClipPlane = 0.045f;
            camera.farClipPlane = 260;
            camera.fieldOfView = 67;
            eye.AddComponent<AudioListener>();
            eye.AddComponent<UniversalAdditionalCameraData>().antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            walker.AddComponent<CityWalkthrough>();
        }

        internal static CharacterController AddWalkerCollider(GameObject walker)
        {
            var capsule = walker.AddComponent<CharacterController>();
            capsule.height = 1.8f;
            capsule.radius = 0.34f;
            capsule.center = Vector3.up * 0.9f;
            capsule.slopeLimit = 48;
            capsule.stepOffset = 0.21f;
            capsule.skinWidth = 0.035f;
            capsule.minMoveDistance = 0;
            return capsule;
        }

        private static Light LightScene()
        {
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(CityKitAssets.Sky);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.63f, 0.68f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.42f, 0.41f);
            RenderSettings.ambientGroundColor = new Color(0.27f, 0.25f, 0.23f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.72f, 0.77f, 0.84f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 64;
            RenderSettings.fogEndDistance = 185;
            var light = new GameObject("Daylight").AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.color = new Color(1f, 0.93f, 0.82f);
            light.intensity = 1.35f;
            light.shadowStrength = 0.88f;
            light.transform.rotation = Quaternion.Euler(DefaultSun, -36, 0);
            RenderSettings.sun = light;
            return light;
        }
    }
}
