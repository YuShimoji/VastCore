using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.RegionMap
{
    // One exposed joint and its apron of fallen rock, anchored to the generated
    // tunnel rather than to a world coordinate or the camera.
    internal sealed class RegionCaveMouth
    {
        private const string Folder = "RegionMap3D/CaveMouth/";
        private readonly RegionTerrainView.Result land;
        private readonly Vector3 mouth, outward, right;
        private readonly Transform root;
        private readonly Material stone;
        private readonly Mesh outcrop, chip;

        internal static void ShapeRecess(RegionVolumeSpec volume)
        {
            var tunnel = volume.Cavities[1];
            var shelter = volume.Cavities[0];
            float floor = shelter.FloorY;
            // The local shelter is wider than the tunnel. Shorten its flanks so
            // the exposed beds meet solid cliff on either side of the walkable gap.
            float ax = shelter.Ax, az = shelter.Az;
            shelter.Ax = Mathf.Lerp(ax, shelter.Bx, 0.30f);
            shelter.Az = Mathf.Lerp(az, shelter.Bz, 0.30f);
            shelter.Bx = Mathf.Lerp(ax, shelter.Bx, 0.68f);
            shelter.Bz = Mathf.Lerp(az, shelter.Bz, 0.68f);
            var outAxis = new Vector2(volume.FocusX - tunnel.Ax, volume.FocusZ - tunnel.Az).normalized;
            var sideAxis = new Vector2(outAxis.y, -outAxis.x);
            float scale = tunnel.RadiusA / 2.8f;
            var bend = new Vector2(tunnel.Bx - tunnel.Ax, tunnel.Bz - tunnel.Az);
            float bendSlope = Vector2.Dot(bend, sideAxis) / Vector2.Dot(bend, outAxis);
            volume.LocalFormation = (x, y, z, terrain) =>
            {
                float h = (y - floor) / scale;
                if (h < -3 || h > 18) return terrain;
                var offset = new Vector2(x - tunnel.Ax, z - tunnel.Az) / scale;
                float u = Vector2.Dot(offset, sideAxis), v = Vector2.Dot(offset, outAxis);
                if (Mathf.Abs(u) > 24 || v < -15 || v > 18) return terrain;

                // Dipping beds end in offset fracture faces. The upper ledge and
                // abutments are solid all the way back into the original hillside.
                float grain = RegionNoise.Fbm(u * 0.26f + v * 0.09f, h * 0.31f, 2, volume.Seed);
                float bed = h + 0.27f * u + 0.09f * v + 0.7f * grain;
                float bedding = Mathf.Sin(bed * 1.24f + 0.5f * Mathf.Sin(bed * 0.48f));
                float fracture = RegionNoise.Fbm(u * 0.29f, h * 0.13f, 2, volume.Seed + 11);
                float joint = Mathf.Max(2.8f - 0.72f * Mathf.Abs(u + 3.7f),
                    1.2f - 0.58f * Mathf.Abs(u - 8));
                float toe = 7.3f + joint + 0.94f * Mathf.Clamp(bedding * 2.1f,-1,1)
                    + 2.2f * fracture + grain - 0.19f * h;
                float crest = 13.4f - 0.48f * Mathf.Abs(u + 3.4f) - 0.58f * Mathf.Max(v, 0) + grain;
                float mass = Mathf.Min(toe - v, crest - h);
                mass = Mathf.Min(mass, Mathf.Min(20 - Mathf.Abs(u), v + 13));
                mass = Mathf.Min(mass, h + 1.4f);

                // The tilted crown meets a shoulder on one side, leaving space to
                // walk under it. Its axis follows the existing bend beyond the sill.
                float across = u - Mathf.Min(v, 0) * bendSlope;
                float rise = Mathf.Clamp(-v * 0.14f, 0, 2);
                float width = 2.45f + Mathf.Clamp((v + 4) * 0.22f, 0, 2.9f);
                float roof = rise + 3.75f - 0.26f * across - 0.24f * Mathf.Abs(across)
                    + 0.36f * grain + 0.25f * Mathf.Sin(v * 0.62f);
                float wall = Mathf.Abs(across + 0.17f * (h - rise)) - width + 0.33f * grain;
                float voidEdge = Mathf.Max(wall, Mathf.Max(h - roof, rise - 0.36f - h));
                return Mathf.Max(terrain, Mathf.Min(mass, voidEdge) * scale);
            };
        }

        internal static void Build(RegionTerrainView.Result land, Material terrain)
        {
            var entrance = new RegionCaveMouth(land, terrain);
            entrance.PlaceRocks();
        }

        private RegionCaveMouth(RegionTerrainView.Result land, Material terrain)
        {
            this.land = land;
            var tunnel = land.Volume.Cavities[1];
            mouth = new Vector3(tunnel.Ax, tunnel.Ay, -tunnel.Az);
            outward = Vector3.ProjectOnPlane(land.Focus - mouth, Vector3.up).normalized;
            right = Vector3.Cross(Vector3.up, -outward);
            terrain.SetVector("_Mouth", mouth);
            terrain.SetVector("_Outward", outward);
            terrain.SetTexture("_CliffPhoto", Resources.Load<Texture2D>(Folder + "CliffColor"));
            terrain.SetTexture("_CliffNormal", Resources.Load<Texture2D>(Folder + "CliffNormal"));
            terrain.SetTexture("_Gravel", Resources.Load<Texture2D>(
                "MapGenerator/Textures/Ground081_1K-JPG/Ground081_1K-JPG_Color"));
            terrain.SetTexture("_Scree", Resources.Load<Texture2D>(
                "MapGenerator/Textures/Ground007_1K-JPG/Ground007_1K-JPG_Color"));
            terrain.EnableKeyword("REGION_CAVE_MOUTH");
            var cliff = land.Root.transform.Find("Volume").GetComponent<MeshRenderer>();
            var cliffMaterial = new Material(terrain) { name = "Cave cliff", hideFlags = HideFlags.DontSave };
            cliffMaterial.SetFloat("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            cliff.sharedMaterial = cliffMaterial;
            stone = new Material(terrain) { name = "Weathered cave blocks", hideFlags = HideFlags.DontSave };
            stone.SetFloat("_Boulder", 1);
            stone.SetTexture("_BaseMap", Resources.Load<Texture2D>(Folder + "BoulderColor"));
            var model = Resources.Load<GameObject>(Folder + "WeatheredBoulder");
            foreach (var part in model.GetComponentsInChildren<MeshFilter>())
            {
                if (part.name == "Outcrop") outcrop = part.sharedMesh;
                if (part.name == "Chip") chip = part.sharedMesh;
            }
            root = new GameObject("Cave mouth talus").transform;
            root.SetParent(land.Root.transform, false);
        }

        private void PlaceRocks()
        {
            // Detached blocks at the foot of the continuous cliff, clear of the trail.
            Block(-6.4f, 6.9f, 0.75f, new Vector3(7.2f, 6.9f, 6.0f), 81, -17);
            Block(5.0f, 4.2f, 0.7f, new Vector3(6.4f, 5.1f, 5.3f), -82, -12);
            Block(-3.0f, -4.7f, 0.4f, new Vector3(3.3f, 3.1f, 3.5f), -144, 15);
            Block(-6.8f, 8.9f, -0.25f, new Vector3(5.3f, 3.3f, 4.4f), 67, -13);
            Block(-8.7f, 11.1f, -0.15f, new Vector3(3.1f, 1.8f, 2.8f), -49, 18);
            Block(6.3f, 7.2f, -0.15f, new Vector3(4.1f, 3.2f, 4.5f), 149, 12);
            Block(9.4f, 9, -0.1f, new Vector3(3.4f, 2.5f, 3.5f), -24, -10);
            var rng = new System.Random(unchecked((int)land.Volume.Seed));
            float Sample(float lo, float hi) => Mathf.Lerp(lo, hi, (float)rng.NextDouble());
            var plants = new List<RegionPlant>();
            for (int i = 0; i < 185; i++)
            {
                float apron = Sample(-2, 23);
                float side = Sample(2.4f, 13f) * (i % 2 == 0 ? 1 : -1);
                float spread = Sample(0, 1);
                bool trail = i % 6 == 0;
                if (trail) side = Sample(-2.6f, 2.6f);
                float width = trail ? Sample(0.06f, 0.22f) : 0.09f + spread * spread * spread * 1.45f;
                Block(side, apron, 0, new Vector3(width, width * Sample(0.28f,0.7f),
                    width * Sample(0.55f,1.2f)), Sample(-180,180), Sample(-20,20));
                if (!trail && apron > 8 && Mathf.PerlinNoise(side * 0.17f, apron * 0.16f) > 0.49f)
                {
                    Vector3 pos = FloorPoint(side, apron);
                    if (!CanHoldDebris(pos)) continue;
                    plants.Add(new RegionPlant { X = pos.x, Y = pos.y, Z = -pos.z,
                        Kind = i % 7 == 0 ? RegionPlantKind.Shrub : RegionPlantKind.Grass,
                        Height = Sample(0.35f, 0.86f), Yaw = Sample(0, Mathf.PI),
                        Variant = i % 4, Sun = 0.8f, Sky = 1 });
                }
            }
            // A fan of fall from the high bank: sheltered pockets between stones
            // can hold grass while the middle of the approach remains worn.
            for (int group = 0; group < 10; group++)
            {
                float reach = Sample(11, 21);
                float flank = Sample(4, 9) * (group < 7 ? -1 : 1);
                for (int i = 0; i < 8; i++)
                {
                    float side = flank + Sample(-0.9f,0.9f), distance = reach + Sample(-1.2f,1.2f);
                    Vector3 foot = FloorPoint(side, distance);
                    if (i < 3)
                    {
                        float w = Sample(0.20f,0.80f);
                        Block(side, distance, 0, new Vector3(w,0.55f*w,0.8f*w), Sample(-180,180), Sample(-15,15));
                    }
                    else if (CanHoldDebris(foot)) plants.Add(new RegionPlant { X = foot.x, Y = foot.y, Z = -foot.z,
                        Kind = i == 4 ? RegionPlantKind.Shrub : RegionPlantKind.Grass,
                        Height = Sample(0.35f,0.83f), Yaw = Sample(0,Mathf.PI), Variant = i % 4, Sun = 0.88f, Sky = 1 });
                }
            }
            RegionPlantView.AddTo(root, plants);
        }

        private Vector3 FloorPoint(float side, float distance)
        {
            Vector3 at = mouth + right * side + outward * distance;
            // Find the local floor from the air in the original recess. Sampling from
            // the sky would instead place the talus on top of the overhanging cliff.
            float floor = land.Volume.Cavities[0].FloorY;
            float y = distance >= 10 ? Mathf.Max(floor, land.Patch.HeightAt(at.x, -at.z)) + 16 : floor + 1.5f;
            if (land.Patch.Density(at.x, y, -at.z) < 0)
            {
                while (y > floor - 32 && land.Patch.Density(at.x, y, -at.z) < 0)
                    y -= 0.12f;
                floor = y;
            }
            at.y = floor;
            return at;
        }

        private bool CanHoldDebris(Vector3 point)
        {
            float x = point.x, y = point.y, z = -point.z;
            const float step = 0.25f;
            float dx = land.Patch.Density(x + step, y, z) - land.Patch.Density(x - step, y, z);
            float dz = land.Patch.Density(x, y, z + step) - land.Patch.Density(x, y, z - step);
            float up = land.Patch.Density(x, y - step, z) - land.Patch.Density(x, y + step, z);
            return up > 0 && dx * dx + dz * dz < up * up;
        }

        private void Block(float side, float distance, float rise, Vector3 size, float yaw, float bank)
        {
            Vector3 at = FloorPoint(side, distance);
            bool large = size.x > 2;
            if (!large && !CanHoldDebris(at)) return;
            Mesh mesh = large ? outcrop : chip;
            var obj = new GameObject(large ? "Fractured outcrop" : "Scree");
            obj.transform.SetParent(root, false);
            var rotation = Quaternion.LookRotation(-outward) * Quaternion.Euler(0, yaw, bank);
            Vector3 span = mesh.bounds.size;
            Vector3 scale = new Vector3(size.x / span.x, size.y / span.y, size.z / span.z);
            at.y += rise + size.y * 0.24f;
            obj.transform.SetPositionAndRotation(at - rotation * Vector3.Scale(mesh.bounds.center, scale), rotation);
            obj.transform.localScale = scale;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = stone;
            if (large || size.x > 0.8f) obj.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
    }
}
