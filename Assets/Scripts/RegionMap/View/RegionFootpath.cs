using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vastcore.RegionMap
{
    /// <summary>The sandstone footpath in the regional terrain at seed 8.</summary>
    public sealed class RegionFootpath : MonoBehaviour
    {
        public const int Seed = 8;
        public const string Resource = "RegionMap3D/Footpath/Footpath";

        private const float EyeHeight = 1.67f;
        private const float WalkSpeed = 2.5f;
        private static readonly Vector2 Site = new Vector2(82.93144f, 62.38425f);
        private static readonly Vector3 Position = new Vector3(2950.868896f, 0f, -2950.867188f);

        private Scene _mapScene;
        private GameObject[] _mapRoots;
        private RegionLookScene _terrain;
        private Vector3 _start, _end, _focus;
        private LODGroup _previousShelf, _pbrShelf;
        private Mesh _tread;
        private float _walk;

        public Camera View { get; private set; }
        public LODGroup Shelf => UsesPbr ? _pbrShelf : _previousShelf;
        public bool UsesPbr => _pbrShelf.gameObject.activeSelf;
        public float DistanceToShelf => Vector3.Distance(transform.position, Shelf.transform.TransformPoint(Shelf.localReferencePoint));
        public float WalkingMetres => _walk * Vector3.Distance(_start, _end);
        public float RouteOffset { get; private set; }
        public float RockClearance { get; private set; }

        internal static RegionLookScene Generate(RegionData region)
        {
            if (region.Spec.Seed != Seed) region = RegionGenerator.Generate(new RegionSpec { Seed = Seed });
            return RegionLookScene.Build(region, new RegionLookSpec { CenterMapX = Site.x, CenterMapY = Site.y });
        }

        internal static RegionFootpath Open(RegionLookScene scene, GameObject prefab)
        {
            Scene map = SceneManager.GetActiveScene();
            GameObject[] mapRoots = Array.FindAll(map.GetRootGameObjects(), item => item.activeSelf);
            Scene ground = SceneManager.CreateScene("Sandstone footpath");
            SceneManager.SetActiveScene(ground);
            try
            {
                var atmosphere = RegionAtmosphere.Create(RegionAtmosphere.Preset.ClearAfternoon);
                var look = RegionTerrainView.BuildLook(scene);

                GameObject footpath = Instantiate(prefab, look.Root.transform);
                Vector3 anchor = Position;
                anchor.y = look.Scene.HeightAt(anchor.x, -anchor.z);
                footpath.transform.position = anchor;
                var camera = new GameObject("Footpath camera").AddComponent<Camera>();
                camera.fieldOfView = 52f;
                camera.nearClipPlane = .15f;
                camera.farClipPlane = scene.Spec.OuterKm * 1500f + 1000f;
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                atmosphere.Apply(camera, sun, anchor.y);

                var walk = camera.gameObject.AddComponent<RegionFootpath>();
                walk.View = camera;
                walk._mapScene = map;
                walk._mapRoots = mapRoots;
                walk._terrain = look.Scene;
                Transform track = footpath.transform.Find("Track");
                walk._start = track.GetChild(0).position;
                walk._end = track.GetChild(1).position;
                walk._focus = track.GetChild(2).position;
                walk._previousShelf = footpath.transform.Find("Natural assets/ledge-desert-0").GetComponent<LODGroup>();
                walk._pbrShelf = footpath.transform.Find("Natural assets/Shelf PBR").GetComponent<LODGroup>();
                walk.FitToTerrain(footpath.transform);
                walk.Place(.47f, true);
                camera.gameObject.AddComponent<RegionGroundLook>();
                foreach (GameObject item in mapRoots) item.SetActive(false);
                return walk;
            }
            catch
            {
                SceneManager.SetActiveScene(map);
                SceneManager.UnloadSceneAsync(ground);
                throw;
            }
            finally
            {
                RegionVolumePatch.SunAltitudeDegrees = RegionVolumePatch.DefaultSunAltitudeDegrees;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { ReturnToMap(); return; }
            if (Input.GetKeyDown(KeyCode.Tab)) UsePbr(!UsesPbr);
            float direction = (Input.GetKey(KeyCode.S) ? 1f : 0f) - (Input.GetKey(KeyCode.W) ? 1f : 0f);
            if (direction != 0f) Move(direction * WalkSpeed * Time.deltaTime);
        }

        private void OnGUI()
        {
            if (!View) return;
            GUI.Label(new Rect(15, 12, 760, 28), "W / S   Walk    Right drag   Look    Tab   Compare shelf    Esc   Map");
            GUI.Label(new Rect(15, 36, 400, 26), $"{(UsesPbr ? "PBR" : "Previous")}   |   Shelf {DistanceToShelf:F1} m");
        }

        public void UsePbr(bool value)
        {
            _previousShelf.gameObject.SetActive(!value);
            _pbrShelf.gameObject.SetActive(value);
        }

        public void Move(float metres) => Place(_walk + metres / Vector3.Distance(_start, _end), false);

        public void Place(float progress, bool lookAtShelf)
        {
            _walk = Mathf.Clamp01(progress);
            var eye = Vector3.Lerp(_start, _end, _walk);
            eye.y = _terrain.HeightAt(eye.x, -eye.z) + EyeHeight;
            transform.position = eye;
            if (lookAtShelf) transform.rotation = Quaternion.LookRotation(_focus - eye, Vector3.up);
        }

        public void ReturnToMap()
        {
            SceneManager.SetActiveScene(_mapScene);
            foreach (GameObject item in _mapRoots) if (item) item.SetActive(true);
            SceneManager.UnloadSceneAsync(gameObject.scene);
        }

        private float Ground(Vector3 point) => _terrain.HeightAt(point.x, -point.z);

        private void FitToTerrain(Transform footpath)
        {
            Transform path = footpath.Find("Path");
            PlaceAtGround(path.Find("marker-intact"), new[] { new Vector2(-.25f, 0), new Vector2(.25f, 0), new Vector2(0, -.16f), new Vector2(0, .16f) });
            PlaceAtGround(path.Find("wall-intact"), new[] { new Vector2(-2.18f, 0), new Vector2(-1.10f, 0), Vector2.zero, new Vector2(1.10f, 0), new Vector2(2.18f, 0) }, rollWidth: 2.18f);
            Transform shelter = path.Find("shelter-intact");
            PlaceAtGround(shelter, Corners(1.25f, -.61f, .65f), useHighest: true);
            FitShelterFeet(shelter);
            PlaceAtGround(path.Find("cargo-intact"), Corners(.54f, -.32f, .32f), pitchDepth: .32f);

            var filter = path.Find("Compacted footprint along the contour").GetComponent<MeshFilter>();
            _tread = Instantiate(filter.sharedMesh);
            Vector3[] vertices = _tread.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 point = filter.transform.TransformPoint(vertices[i]);
                point.y = Ground(point) + .048f;
                vertices[i] = filter.transform.InverseTransformPoint(point);
            }
            _tread.vertices = vertices;
            _tread.RecalculateBounds();
            filter.sharedMesh = _tread;

            foreach (Transform item in footpath.Find("Natural assets"))
            {
                Vector3 point = item.position;
                point.y = Ground(point);
                item.position = point;
            }
            _focus.y = Ground(_focus) + .45f;
            AvoidStockRocks();
        }

        private static Vector2[] Corners(float x, float z0, float z1) =>
            new[] { new Vector2(-x, z0), new Vector2(x, z0), new Vector2(-x, z1), new Vector2(x, z1) };

        private void PlaceAtGround(Transform prop, Vector2[] contacts, float rollWidth = 0f, float pitchDepth = 0f, bool useHighest = false)
        {
            prop.rotation = Quaternion.Euler(0f, prop.eulerAngles.y, 0f);
            if (rollWidth > 0f)
            {
                float rise = Ground(prop.TransformPoint(Vector3.right * rollWidth)) - Ground(prop.TransformPoint(Vector3.left * rollWidth));
                prop.rotation *= Quaternion.Euler(0f, 0f, Mathf.Clamp(Mathf.Atan2(rise, rollWidth * 2f) * Mathf.Rad2Deg, -16f, 16f));
            }
            if (pitchDepth > 0f)
            {
                float rise = Ground(prop.TransformPoint(Vector3.forward * pitchDepth)) - Ground(prop.TransformPoint(Vector3.back * pitchDepth));
                prop.rotation *= Quaternion.Euler(-Mathf.Clamp(Mathf.Atan2(rise, pitchDepth * 2f) * Mathf.Rad2Deg, -21f, 21f), 0f, 0f);
            }
            float[] heights = contacts.Select(contact =>
            {
                Vector3 point = prop.TransformPoint(new Vector3(contact.x, 0f, contact.y));
                return Ground(point) - (point.y - prop.position.y);
            }).ToArray();
            Vector3 position = prop.position;
            position.y = useHighest ? heights.Max() + .021f : rollWidth > 0f ? heights.Min() : heights.Average();
            prop.position = position;
        }

        private void FitShelterFeet(Transform shelter)
        {
            MeshFilter[] pieces = shelter.GetComponentsInChildren<MeshFilter>();
            for (int i = 1; i <= 4; i++)
            {
                var foot = pieces.FirstOrDefault(item => item.name.Split('.')[0] == $"Footing {i}");
                var timber = pieces.FirstOrDefault(item => item.name.Split('.')[0] == $"Post {i}");
                if (!foot || !timber) continue;
                Bounds footBounds = foot.sharedMesh.bounds;
                Vector3 bottom = foot.transform.TransformPoint(new Vector3(footBounds.center.x, footBounds.min.y, footBounds.center.z));
                float drop = bottom.y - Ground(bottom) + .028f;
                foot.transform.position -= Vector3.up * drop;
                Bounds post = timber.sharedMesh.bounds;
                Vector3 topLocal = new Vector3(post.center.x, post.max.y, post.center.z);
                Vector3 top = timber.transform.TransformPoint(topLocal);
                Vector3 basePoint = timber.transform.TransformPoint(new Vector3(post.center.x, post.min.y, post.center.z));
                Vector3 scale = timber.transform.localScale;
                scale.y *= Mathf.Max(.1f, 1f + drop / Mathf.Max(.001f, top.y - basePoint.y));
                timber.transform.localScale = scale;
                timber.transform.position += top - timber.transform.TransformPoint(topLocal);
            }
        }

        private void AvoidStockRocks()
        {
            Vector2 start = new Vector2(_start.x, _start.z), end = new Vector2(_end.x, _end.z);
            Vector2 travel = end - start;
            Vector2 across = new Vector2(-travel.y, travel.x).normalized;
            RegionRock[] obstacles = (_terrain.InnerRocks?.Rocks ?? Array.Empty<RegionRock>()).Where(rock => rock.Top > .38f &&
                Mathf.Abs(rock.X - start.x) < 52f && Mathf.Abs(-rock.Z - start.y) < 52f).ToArray();
            float best = float.NegativeInfinity;
            Vector2 offset = Vector2.zero;
            for (int i = 0; i <= 24; i++)
            {
                float metres = ((i + 1) / 2) * .5f * (i % 2 == 1 ? 1 : -1);
                Vector2 test = across * metres;
                float clearance = obstacles.Select(rock =>
                {
                    Vector2 point = new Vector2(rock.X, -rock.Z) - start - test;
                    float t = Mathf.Clamp01(Vector2.Dot(point, travel) / travel.sqrMagnitude);
                    return (point - t * travel).magnitude - rock.Size * .5f;
                }).DefaultIfEmpty(10f).Min();
                if (clearance > best) { best = clearance; offset = test; }
                if (clearance >= .42f) break;
            }
            var shift = new Vector3(offset.x, 0f, offset.y);
            _start += shift;
            _end += shift;
            RouteOffset = Vector2.Dot(offset, across);
            RockClearance = best;
        }

        private void OnDestroy()
        {
            if (_tread) Destroy(_tread);
        }
    }
}
