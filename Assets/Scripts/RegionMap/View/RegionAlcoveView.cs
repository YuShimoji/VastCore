using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    /// <summary>Local surface-net inset, on the same grid and sandstone material as its surroundings.</summary>
    public sealed class RegionAlcoveView
    {
        public RegionMesaAlcove Site { get; private set; }
        public RegionVolumePatch Patch { get; private set; }
        public int SeepPlants { get; private set; }
        public int FallenBlocks { get; private set; }
        private RegionLookScene scene;
        private Mesh terrain;
        private int[] outside;
        private GameObject inset, seep;
        private bool carved;
        private Mesh rocks;
        private int supportedIndices, allRockIndices;
        internal GameObject UnsupportedPlants;

        internal static RegionAlcoveView Build(Transform parent, RegionLookScene scene, MeshRenderer inner, Material sandstone)
        {
            var site = RegionMesaAlcove.Find(scene.Inner.Data);
            if (site == null) return null;
            var view = new RegionAlcoveView
            {
                Site = site, scene = scene, Patch = RegionVolumePatch.Build(scene.Inner.Data, site.Volume),
                terrain = inner.GetComponent<MeshFilter>().sharedMesh, inset = new GameObject("Alcove")
            };
            view.inset.transform.SetParent(parent, false);
            view.outside = view.OutsideIndices();
            RegionTerrainView.AddPart(view.inset.transform, "Bedding contact", view.BuildSurface(), sandstone);
            if (scene.Spec.Rocks) view.AddFalls(sandstone);
            if (scene.Spec.Vegetation) view.AddSeepPlants();
            return view;
        }

        private int[] OutsideIndices()
        {
            int n = scene.Inner.Data.Resolution - 1;
            int left = Mathf.RoundToInt(Patch.MinX / scene.InnerCell + 0.5f);
            int top = Mathf.RoundToInt(Patch.MinZ / scene.InnerCell + 0.5f);
            int width = Patch.SamplesX - 2;
            int[] source = scene.InnerTerrain.Indices;
            int[] keep = new int[source.Length - 6 * width * width];
            int from = 0, to = 0;
            for (int row = top; row < top + width; row++)
            {
                int stop = 6 * (row * n + left);
                Array.Copy(source, from, keep, to, stop - from);
                to += stop - from;
                from = stop + 6 * width;
            }
            Array.Copy(source, from, keep, to, source.Length - from);
            return keep;
        }

        public void Show(bool cut, bool vegetation)
        {
            if (cut != carved)
            {
                terrain.SetTriangles(cut ? outside : scene.InnerTerrain.Indices, 0, false);
                if (rocks)
                {
                    var sub = rocks.GetSubMesh(0);
                    sub.indexCount = cut ? supportedIndices : allRockIndices;
                    rocks.SetSubMesh(0, sub, MeshUpdateFlags.DontRecalculateBounds);
                }
                carved = cut;
            }
            if (UnsupportedPlants) UnsupportedPlants.SetActive(!cut);
            inset.SetActive(cut);
            if (seep) seep.SetActive(vegetation);
        }

        internal bool LostSupport(float worldX, float y, float worldZ, float tolerance)
        {
            float x = worldX - scene.InnerMinX, z = worldZ - scene.InnerMinZ;
            if (x < Patch.MinX || x > Patch.MaxX || z < Patch.MinZ || z > Patch.MaxZ) return false;
            return !GroundBelow(x, z, y + tolerance, out float h) || y - h > tolerance;
        }

        // All loose rocks share one draw. Put the dislodged triangles at the end
        // so the old and carved views differ only by the displayed index count.
        internal void BindRocks(Mesh mesh)
        {
            var removed = new List<RegionRock>();
            int removedCount = 0;
            foreach (var rock in scene.InnerRocks.Rocks)
            {
                if (!LostSupport(rock.X, rock.Y, rock.Z, Mathf.Clamp(rock.Size * 0.13f, 0.08f, 0.4f))) continue;
                removed.Add(rock);
                removedCount += rock.VertexCount;
            }
            if (removedCount == 0) return;
            int[] source = mesh.triangles;
            int[] sorted = new int[source.Length];
            supportedIndices = source.Length - removedCount;
            allRockIndices = source.Length;
            int from = 0, keep = 0, tail = supportedIndices;
            foreach (var rock in removed)
            {
                int length = rock.FirstVertex - from;
                Array.Copy(source, from, sorted, keep, length);
                Array.Copy(source, rock.FirstVertex, sorted, tail, rock.VertexCount);
                keep += length;
                tail += rock.VertexCount;
                from = rock.FirstVertex + rock.VertexCount;
            }
            Array.Copy(source, from, sorted, keep, source.Length - from);
            mesh.SetTriangles(sorted, 0, false);
            rocks = mesh;
        }

        private Mesh BuildSurface()
        {
            var data = scene.Inner.Data;
            float cell = scene.InnerCell, span = (data.Resolution - 1) * cell;
            Mesh mesh = RegionTerrainView.PatchMesh(Patch, span);
            var vertices = mesh.vertices;
            var colors = new Color[vertices.Length];
            var surface = new Vector4[vertices.Length];
            var terrainPart = scene.InnerTerrain;
            for (int v = 0; v < vertices.Length; v++)
            {
                float x = vertices[v].x, z = -vertices[v].z, y = vertices[v].y;
                int i = Mathf.RoundToInt(x / cell) + data.Resolution * Mathf.RoundToInt(z / cell);
                float edge = Mathf.Min(Mathf.Min(x - Patch.MinX, Patch.MaxX - x), Mathf.Min(z - Patch.MinZ, Patch.MaxZ - z));
                float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((edge - 2f) / 7f));
                float sky = terrainPart.Sky[i] * Mathf.Lerp(1f, Patch.Sky[v], blend);
                // Talus shadows baked at the old height do not belong on the newly exposed bed.
                float cutDepth = Mathf.InverseLerp(0.4f, 4f, Patch.HeightAt(x, z) - y);
                float sun = Mathf.Lerp(terrainPart.Sun[i], 1f, cutDepth) * Mathf.Lerp(1f, Patch.Sun[v], blend);
                colors[v] = new Color(sky, sun, 1f - terrainPart.Rock[i], terrainPart.Tint[i]);
                surface[v] = RegionTerrainView.SandstoneChannels(data, i, y);
                vertices[v] += new Vector3(scene.InnerMinX, 0f, -scene.InnerMinZ);
            }
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.SetUVs(1, surface);
            mesh.RecalculateBounds();
            return mesh;
        }

        // Highest upward-facing triangle below the roof, including the talus outside the mouth.
        private bool Shelf(float x, float z, out float height) => GroundBelow(x, z, Site.Floor + 3.5f, out height);

        private bool GroundBelow(float x, float z, float ceiling, out float height)
        {
            height = float.NegativeInfinity;
            float[] p = Patch.Positions;
            int[] t = Patch.Indices;
            for (int j = 0; j < t.Length; j += 3)
            {
                int a = t[j] * 3, b = t[j + 1] * 3, c = t[j + 2] * 3;
                float dx = x - p[a], dz = z - p[a + 2];
                if (Mathf.Abs(dx) > 2f * scene.InnerCell || Mathf.Abs(dz) > 2f * scene.InnerCell) continue;
                float bx = p[b] - p[a], bz = p[b + 2] - p[a + 2];
                float cx = p[c] - p[a], cz = p[c + 2] - p[a + 2];
                float det = bx * cz - cx * bz;
                if (det >= -0.00001f) continue;
                float u = (dx * cz - dz * cx) / det, v = (dz * bx - dx * bz) / det;
                if (u < 0f || v < 0f || u + v > 1f) continue;
                float h = p[a + 1] + u * (p[b + 1] - p[a + 1]) + v * (p[c + 1] - p[a + 1]);
                if (h < ceiling) height = Mathf.Max(h, height);
            }
            return !float.IsNegativeInfinity(height);
        }

        private float Noise(int i, int salt) => RegionNoise.Hash01(i, salt, Site.Volume.Seed);

        private void AddSeepPlants()
        {
            var plants = new List<RegionPlant>();
            // Broken colonies just inside the drip line; require a shelf and open space above each root.
            for (int i = 0; i < 140; i++)
            {
                float along = Mathf.Lerp(-21f, 20f, Noise(i, 1));
                float inward = 4f + 2f * Mathf.Sin(along * 0.26f) + Noise(i, 2) * 2f;
                if (Mathf.Sin(along * 0.61f) + 1.3f * Noise(i, 3) < 0.35f) continue;
                Site.Point(along, inward, out float x, out float z);
                if (!Shelf(x, z, out float y) || Patch.Density(x, y + 0.9f, z) >= 0f ||
                    Patch.HeightAt(x, z) < y + 5f) continue;
                bool shrub = Noise(i, 4) < 0.35f;
                plants.Add(new RegionPlant { X = scene.InnerMinX + x, Y = y, Z = scene.InnerMinZ + z,
                    Height = (shrub ? 0.76f : 0.30f) + Noise(i, 5) * 0.54f,
                    Kind = shrub ? RegionPlantKind.Shrub : RegionPlantKind.Grass,
                    Variant = Mathf.Min(3, (int)(4 * Noise(i, 6))), Yaw = Noise(i, 7) * 2f * Mathf.PI,
                    Sky = 0.65f, Sun = 0.40f });
            }
            SeepPlants = plants.Count;
            RegionPlantView.AddPlants(inset.transform, plants, "Seep vegetation");
            seep = inset.transform.Find("Seep vegetation")?.gameObject;
        }

        private void AddFalls(Material sandstone)
        {
            var p = new List<float>();
            var n = new List<float>();
            for (int i = 0; i < 32; i++)
            {
                float along = Mathf.Lerp(-23f, 21f, Noise(i, 11));
                float outward = 1f + 20f * Noise(i, 12);
                Site.Point(along, -outward, out float x, out float z);
                if (!Shelf(x, z, out float y)) continue;
                float size = 0.8f + 4.3f * Noise(i, 13) * Noise(i, 13);
                RegionRockField.AppendFallenSlab(p, n, i, scene.InnerMinX + x, y, scene.InnerMinZ + z,
                    size, 6.28f * Noise(i, 14), Noise(i, 15) - 0.5f);
                FallenBlocks++;
            }
            int count = p.Count / 3;
            if (count == 0) return;
            var part = new RegionLookPart { Name = "Fallen sandstone", Positions = p.ToArray(), Normals = n.ToArray(),
                Indices = new int[count], Rock = new float[count], Tint = new float[count], Sky = new float[count], Sun = new float[count] };
            for (int i = 0; i < count; i++)
            {
                int ix = Mathf.RoundToInt((part.Positions[i * 3] - scene.InnerMinX) / scene.InnerCell);
                int iz = Mathf.RoundToInt((part.Positions[i * 3 + 2] - scene.InnerMinZ) / scene.InnerCell);
                int cell = ix + iz * scene.Inner.Data.Resolution;
                part.Indices[i] = i; part.Rock[i] = 1f; part.Tint[i] = 0.45f;
                part.Sun[i] = scene.InnerTerrain.Sun[cell]; part.Sky[i] = scene.InnerTerrain.Sky[cell];
            }
            var material = new Material(sandstone) { name = "Fallen sandstone", hideFlags = HideFlags.DontSave };
            material.SetFloat("_LooseRock", 1f);
            RegionTerrainView.AddPart(inset.transform, part.Name, RegionTerrainView.LookMesh(part), material);
        }

        public void AddViewpoints(Transform parent)
        {
            foreach (string name in new[] { "alcove-mid", "alcove-near", "alcove-shelf" })
            {
                var point = new GameObject(name);
                point.transform.SetParent(parent, false);
                Place(point.transform, name == "alcove-mid", name == "alcove-shelf" ? 1f : 0.57f, name != "alcove-shelf");
            }
        }

        public void Place(Transform camera, bool mid, float approach, bool sharedView = false)
        {
            float along = mid ? -20f : Mathf.Lerp(-12f, -5f, approach);
            float depth = mid ? -85f : Mathf.Lerp(-14f, 3.5f, approach);
            Site.Point(along, depth, out float x, out float z);
            if (!Shelf(x, z, out float ground)) ground = scene.HeightAt(scene.InnerMinX + x, scene.InnerMinZ + z);
            Site.Point(mid ? 0f : 8f, mid ? -3f : 7f, out float tx, out float tz);
            // A comparison eye must also clear the original, higher talus.
            if (sharedView) ground = Mathf.Max(ground, scene.HeightAt(scene.InnerMinX + x, scene.InnerMinZ + z));
            float eyeY = ground + (mid ? 4f : 1.8f);
            if (mid)
            {
                // Clear intervening benches on the sight line to the mouth.
                float lip = Site.Floor + 4f;
                for (int i = 1; i <= 40; i++)
                {
                    float t = i / 40f;
                    float h = scene.HeightAt(scene.InnerMinX + Mathf.Lerp(tx, x, t), scene.InnerMinZ + Mathf.Lerp(tz, z, t));
                    eyeY = Mathf.Max(eyeY, lip + (h + 1.8f - lip) / t);
                }
                // A detached boulder on a bench can still fill the middle of the frame.
                if (scene.InnerRocks != null)
                {
                    var sight = new Vector2(x - tx, z - tz);
                    foreach (var rock in scene.InnerRocks.Rocks)
                    {
                        if (rock.Size < 1f) continue;
                        var offset = new Vector2(rock.X - scene.InnerMinX - tx, rock.Z - scene.InnerMinZ - tz);
                        float t = Vector2.Dot(offset, sight) / sight.sqrMagnitude;
                        if (t < 0.22f || t > 1.05f) continue;
                        float width = 0.65f * rock.Size + (1f - Mathf.Clamp01(t)) * 12f;
                        if ((offset - sight * t).sqrMagnitude > width * width) continue;
                        eyeY = Mathf.Max(eyeY, lip + (rock.Y + rock.Top + 1.8f - lip) / t);
                    }
                }
            }
            camera.position = new Vector3(scene.InnerMinX + x, eyeY, -scene.InnerMinZ - z);
            Vector3 target = new Vector3(scene.InnerMinX + tx, Site.Floor + (mid ? 6f : 3.8f), -scene.InnerMinZ - tz);
            camera.rotation = Quaternion.LookRotation(target - camera.position, Vector3.up);
        }
    }
}
