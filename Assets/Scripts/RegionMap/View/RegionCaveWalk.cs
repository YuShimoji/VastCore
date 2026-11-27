using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vastcore.RegionMap
{
    [RequireComponent(typeof(Camera))]
    public sealed class RegionCaveWalk : MonoBehaviour
    {
        [SerializeField] private GameObject exterior;
        [SerializeField] private RegionSpec region;
        [SerializeField] private RegionTerrainViewSpec window;
        [SerializeField] private Mesh groundCollision;
        [SerializeField] private Vector3 mouth;
        [SerializeField] private Vector3 outward;
        [SerializeField] private int layoutSeed = 9;
        private Camera eye;
        private CharacterController walker;
        private Vector3 outsideFeet;
        private Scene outsideScene, insideScene;
        private GameObject interior;
        private RegionCaveInterior geometry;
        private bool inside;
        private float pitch, verticalSpeed;
        private GUIStyle hint;

        public void Configure(RegionTerrainView.Result land, RegionSpec regionSpec, RegionTerrainViewSpec viewSpec, int caveLayoutSeed = 9)
        {
            exterior = land.Root;
            region = regionSpec;
            window = viewSpec;
            layoutSeed = caveLayoutSeed;
            // A 3-cell stride spans the 1024-cell window, including its last edge.
            var ground = RegionTerrainMesh.Build(land.Terrain, 3, 1f);
            groundCollision = RegionTerrainView.TerrainMesh(ground, land.Patch);
            var tunnel = land.Volume.Cavities[1];
            mouth = new Vector3(tunnel.Ax, tunnel.Ay, -tunnel.Az);
            outward = Vector3.ProjectOnPlane(land.Focus - mouth, Vector3.up).normalized;
        }

        private void Start()
        {
            // The editor preview uses transient meshes/textures. A saved or reloaded
            // preview can have an empty mesh slot when Play starts.
            var filter = exterior.transform.Find("Volume").GetComponent<MeshFilter>();
            var previewMaterial = filter.GetComponent<MeshRenderer>().sharedMaterial;
            if (!filter.sharedMesh || !previewMaterial || !previewMaterial.GetTexture("_BaseMap") || !groundCollision)
            {
                exterior.SetActive(false);
                Destroy(exterior);
                var land = RegionTerrainView.Build(RegionGenerator.Generate(region), window);
                Configure(land, region, window, layoutSeed);
            }
            exterior.AddComponent<MeshCollider>().sharedMesh = groundCollision;
            var volume = exterior.transform.Find("Volume").GetComponent<MeshFilter>();
            volume.gameObject.AddComponent<MeshCollider>().sharedMesh = volume.sharedMesh;

            eye = GetComponent<Camera>();
            outsideScene = exterior.scene;
            var body = new GameObject("Cave walker");
            SceneManager.MoveGameObjectToScene(body, outsideScene);
            walker = body.AddComponent<CharacterController>();
            walker.height = 1.72f;
            walker.radius = 0.30f;
            walker.center = Vector3.up * 0.86f;
            walker.stepOffset = 0.32f;
            walker.skinWidth = 0.035f;
            transform.SetParent(body.transform, false);
            transform.localPosition = Vector3.up * 1.59f;
            eye.fieldOfView = 68f;
            eye.nearClipPlane = 0.055f;
            Physics.SyncTransforms();
            var approach = mouth + outward * 23f;
            // Start below any overhang, raycast onto the same mesh the walker uses.
            if (Physics.Raycast(approach + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 35f))
                approach.y = hit.point.y + 0.06f;
            outsideFeet = approach;
            PlaceWalker(outsideFeet, -outward);
        }

        public Vector3 Feet => walker.transform.position;
        public bool Inside => inside;
        public int LayoutSeed => layoutSeed;
        public Vector3 InteriorThreshold => geometry.Threshold;
        public Vector3 InteriorTurn => geometry.Turn;
        public int InteriorPropCount => geometry.PropCount;
        public bool CanUseDoor
        {
            get
            {
                if (!walker) return false;
                Vector3 delta = inside ? Feet - InteriorThreshold : Feet + Vector3.up * 1.1f - mouth;
                return Mathf.Abs(delta.y) < 2.5f && delta.x * delta.x + delta.z * delta.z < 3.2f * 3.2f;
            }
        }

        public void UseDoor()
        {
            if (!CanUseDoor) return;
            if (!inside)
            {
                if (!insideScene.IsValid())
                {
                    insideScene = SceneManager.CreateScene("Stone Span Cavern");
                    interior = new GameObject("Stone Span Cavern");
                    SceneManager.MoveGameObjectToScene(interior, insideScene);
                    geometry = RegionCaveInterior.Build(interior, layoutSeed);
                }
                interior.SetActive(true);
                exterior.SetActive(false);
                SceneManager.SetActiveScene(insideScene);
                SceneManager.MoveGameObjectToScene(walker.gameObject, insideScene);
                inside = true;
                PlaceWalker(InteriorThreshold + Vector3.up * 0.15f, Vector3.forward);
            }
            else
            {
                interior.SetActive(false);
                exterior.SetActive(true);
                SceneManager.SetActiveScene(outsideScene);
                SceneManager.MoveGameObjectToScene(walker.gameObject, outsideScene);
                inside = false;
                PlaceWalker(outsideFeet, -outward);
            }
        }

        private void PlaceWalker(Vector3 position, Vector3 facing)
        {
            walker.enabled = false;
            walker.transform.SetPositionAndRotation(position, Quaternion.LookRotation(facing));
            pitch = 0f;
            transform.localRotation = Quaternion.identity;
            verticalSpeed = 0f;
            walker.enabled = true;
            Physics.SyncTransforms();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float yaw = 0, tilt = 0;
            if (Input.GetMouseButton(1))
            {
                yaw = Input.GetAxisRaw("Mouse X") * 2.6f;
                tilt = -Input.GetAxisRaw("Mouse Y") * 2.6f;
            }
            if (Input.GetKey(KeyCode.LeftArrow)) yaw -= 95f * dt;
            if (Input.GetKey(KeyCode.RightArrow)) yaw += 95f * dt;
            if (Input.GetKey(KeyCode.UpArrow)) tilt -= 70f * dt;
            if (Input.GetKey(KeyCode.DownArrow)) tilt += 70f * dt;
            walker.transform.Rotate(0, yaw, 0);
            pitch = Mathf.Clamp(pitch + tilt, -75f, 75f);
            transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            float x = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
            float z = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
            Walk(walker.transform.TransformDirection(new Vector3(x, 0, z)), dt);
            if (Input.GetKeyDown(KeyCode.E)) UseDoor();
        }

        public void Walk(Vector3 direction, float dt)
        {
            verticalSpeed = walker.isGrounded ? -2f : Mathf.Max(-20f, verticalSpeed - 18f * dt);
            walker.Move((Vector3.ClampMagnitude(direction, 1f) * 4f + Vector3.up * verticalSpeed) * dt);
            if (inside && Feet.y < -5f)
                PlaceWalker(InteriorThreshold + Vector3.up * 0.15f, Vector3.forward);
            if (!inside && Feet.y < outsideFeet.y - 45f)
                PlaceWalker(outsideFeet, -outward);
        }

        private void OnGUI()
        {
            if (hint == null)
                hint = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontSize = 15 };
            var area = new Rect((Screen.width - 390f) / 2f, Screen.height - 43f, 390f, 31f);
            string message = CanUseDoor ? (inside ? "E   崖の入口へ戻る" : "E   洞窟へ入る")
                : "WASD  移動     右ドラッグ / 矢印キー  視点";
            GUI.Box(area, message, hint);
        }
    }
}
