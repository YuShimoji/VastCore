using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Vastcore.Water;

namespace Vastcore.CoastalWater
{
    [RequireComponent(typeof(Camera))]
    public sealed class CoastalWaterSample : MonoBehaviour
    {
        [Serializable]
        public sealed class Site
        {
            public string Name = "Shingle beach";
            public Shoreline Shore = new Shoreline();
            [Min(0f)] public float Headlands = 1f;
            public float CameraZ;
            public float FocusZ = 17f;
            public int DefaultSea;
        }

        public Shader OceanShader;
        public Shader ShoreShader;
        public Shader SkyShader;
        public Shader GroundShader;
        public Texture2D Sand;
        public Texture2D Asphalt;
        public Texture2D Stone;
        public Texture2D Turf;
        public SeaState[] SeaStates;
        public Site[] Sites;
        [Min(0)] public int StartSite;
        public bool ShowControls = true;

        private Camera sampleCamera;
        private Skybox sky;
        private Material skyMaterial;
        private CoastalGround ground;
        private CoastalWaterRenderer water;
        private int siteIndex;
        private int seaIndex;
        private int viewIndex;
        private float time;
        private bool flat;
        private bool paused;

        private void OnEnable()
        {
            if (Application.isPlaying) OpenSite(StartSite);
        }

        private void OnDisable()
        {
            ClearEnvironment();
            if (sky) sky.material = null;
            if (skyMaterial)
            {
                if (Application.isPlaying) Destroy(skyMaterial);
                else DestroyImmediate(skyMaterial);
            }
            skyMaterial = null;
        }

        public void OpenSite(int index, float? crestOverride = null)
        {
            if (Sites == null || Sites.Length == 0 || SeaStates == null || SeaStates.Length == 0) return;
            if (!sampleCamera) sampleCamera = GetComponent<Camera>();
            if (!sky && !TryGetComponent(out sky)) sky = gameObject.AddComponent<Skybox>();
            if (!skyMaterial && SkyShader) skyMaterial = new Material(SkyShader);
            sky.material = skyMaterial;

            ClearEnvironment();
            siteIndex = Mathf.Clamp(index, 0, Sites.Length - 1);
            Site site = Sites[siteIndex];
            Shoreline shore = site.Shore;
            float crest = shore.CrestHeight;
            try
            {
                if (crestOverride.HasValue) shore.CrestHeight = crestOverride.Value;
                ground = new CoastalGround(gameObject.scene, shore, site.Headlands, site.CameraZ, GroundShader, Sand, Asphalt, Stone, Turf);
                water = new CoastalWaterRenderer(ground.Transform, shore, OceanShader, ShoreShader,
                    ground.ReceivingSurface, ground.SwashObstacles);
            }
            finally
            {
                shore.CrestHeight = crest;
            }

            seaIndex = Mathf.Clamp(site.DefaultSea, 0, SeaStates.Length - 1);
            PlaceCamera(0);
            SetFrame(seaIndex, 0f, false);
        }

        public void SetFrame(int profile, float seconds, bool flatWater)
        {
            seaIndex = Mathf.Clamp(profile, 0, SeaStates.Length - 1);
            time = seconds;
            flat = flatWater;
            water?.Render(SeaStates[seaIndex], time, flat);
        }

        public void PlaceCamera(int view)
        {
            Site site = Sites[siteIndex];
            Shoreline shore = site.Shore;
            bool road = shore.Bank == BankType.Barrier;
            viewIndex = (view + 3) % 3;
            float z = site.CameraZ;
            float across = road ? 4.6f : 5.4f;
            float height = shore.ReceivingY(across) + 1.68f;
            float targetZ = z + 33f;
            float targetX = road ? 1.6f : -7.2f;
            float targetY = shore.SeaLevel + (road ? 1.72f : 0.18f);
            if (viewIndex == 1)
            {
                across = road ? 10.4f : 12.2f;
                height = shore.SeaLevel + 6.4f;
                z += 6.0f;
                targetZ += 10.0f;
                targetX = -6.2f;
            }
            if (viewIndex == 2)
            {
                across = 32f;
                height = shore.SeaLevel + 16.8f;
                z -= 3.0f;
                targetZ += 54.0f;
                targetX = -22.0f;
            }

            Vector3 position = new Vector3(shore.X(across, z), height, z);
            Vector3 look = new Vector3(shore.X(targetX, targetZ), targetY, targetZ);
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(look - position));
            if (sampleCamera) sampleCamera.fieldOfView = viewIndex == 2 ? 48f : 52f;
        }

        private void Update()
        {
            if (water == null) return;
            Keyboard key = Keyboard.current;
            if (key != null)
            {
                if (key.digit1Key.wasPressedThisFrame) OpenSite(0);
                if (key.digit2Key.wasPressedThisFrame) OpenSite(1);
                if (key.digit3Key.wasPressedThisFrame) OpenSite(2);
                if (key.nKey.wasPressedThisFrame) OpenSite((siteIndex + 1) % Sites.Length);
                if (key.tabKey.wasPressedThisFrame) seaIndex = (seaIndex + 1) % SeaStates.Length;
                if (key.cKey.wasPressedThisFrame) PlaceCamera(viewIndex + 1);
                if (key.bKey.wasPressedThisFrame) flat = !flat;
                if (key.spaceKey.wasPressedThisFrame) paused = !paused;
            }
            if (!paused) time += Time.deltaTime;
            SetFrame(seaIndex, time, flat);
        }

        private void OnGUI()
        {
            if (!ShowControls || water == null) return;
            string view = viewIndex == 0 ? "Eye level" : viewIndex == 1 ? "Mid" : "Far";
            string title = $"{Sites[siteIndex].Name}   |   {SeaStates[seaIndex].name}   |   {view}";
            if (flat) title += "   |   Flat water";
            if (paused) title += "   |   Paused";
            var rect = new Rect(12, Screen.height - 60, Mathf.Min(670, Screen.width - 24), 48);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label(title);
            GUILayout.Label("1–3 / N Coast   Tab Sea state   C Camera   B Flat comparison   Space Pause");
            GUILayout.EndArea();
        }

        private void ClearEnvironment()
        {
            water?.Dispose();
            ground?.Dispose();
            water = null;
            ground = null;
        }
    }
}
