using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vastcore.Weather
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera), typeof(Skybox))]
    public sealed class WeatherEnvironmentSample : MonoBehaviour
    {
        [SerializeField] private WeatherSampleSite site;
        [SerializeField] private int seed = 17;
        [SerializeField] private bool showControls = true;

        private GameObject world;
        private WeatherField field;
        private WeatherPreset preset;
        private WeatherPreset clear;
        private Light sun;
        private WeatherSampleGeometry geometry;
        private Material sky;
        private Material previousSky;
        private float seconds;
        private bool clearSky;
        private bool playing;
        private WeatherSampleSite builtSite;
        private int builtSeed;

        private static readonly string[] SiteNames =
        {
            "Street thunderstorm", "Mountain valley fog", "Storm across the sea", "Storm beyond the ridge",
            "Morning Glory", "Ridge cloud bands", "Great Plains supercell"
        };

        public Camera SampleCamera => GetComponent<Camera>();
        public WeatherField Field => field;
        public WeatherSampleSite Site => site;
        public int Seed => seed;
        public int ViewIndex { get; private set; }
        public float FeaturedTime => site == WeatherSampleSite.StreetThunderstorm ? 42f : 12f;
        public bool ShowControls { get => showControls; set => showControls = value; }

        private void OnEnable()
        {
            previousSky = GetComponent<Skybox>().material;
            Open(site, seed);
        }

        private void OnDisable()
        {
            GetComponent<Skybox>().material = previousSky;
            Release();
        }

        public void Open(WeatherSampleSite requestedSite, int variationSeed)
        {
            if (world)
                world.SetActive(false);
            Release();
            site = requestedSite;
            seed = variationSeed;
            builtSite = site;
            builtSeed = seed;
            clearSky = false;
            playing = false;
            world = new GameObject("Weather Environment") { hideFlags = HideFlags.DontSave };
            SceneManager.MoveGameObjectToScene(world, gameObject.scene);

            preset = WeatherSamplePresets.Create(site);
            clear = ScriptableObject.CreateInstance<WeatherPreset>();
            clear.hideFlags = HideFlags.DontSave;
            clear.name = "Clear";
            var light = new GameObject("Daylight", typeof(Light)) { hideFlags = HideFlags.DontSave };
            light.transform.SetParent(world.transform, false);
            sun = light.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.None;

            sky = WeatherField.NewMaterial("SampleSky");
            Color zenith;
            Color haze = preset.AerialHaze;
            Color sunlight;
            float altitude;
            switch (site)
            {
                case WeatherSampleSite.StreetThunderstorm:
                    zenith = Tone(0x344760);
                    sunlight = Tone(0xBBC2CB);
                    altitude = 26f;
                    break;
                case WeatherSampleSite.MountainValleyFog:
                case WeatherSampleSite.MorningGlory:
                case WeatherSampleSite.RidgeCloudBands:
                    zenith = Tone(0x4C78AB);
                    sunlight = Tone(0xFFC797);
                    altitude = site == WeatherSampleSite.MorningGlory ? 7f : 11f;
                    break;
                case WeatherSampleSite.GreatPlainsSupercell:
                    zenith = Tone(0x415C72);
                    sunlight = Tone(0xEAD6AA);
                    altitude = 21f;
                    break;
                default:
                    zenith = Tone(0x3E79B2);
                    sunlight = Tone(0xFFF3D6);
                    altitude = 42f;
                    break;
            }
            sun.transform.rotation = Quaternion.Euler(altitude, 47f, 0f);
            sun.color = sunlight;
            Vector3 toSun = -sun.transform.forward;
            sky.SetColor("_Zenith", zenith);
            sky.SetColor("_Horizon", haze);
            sky.SetColor("_Sun", sunlight);
            sky.SetVector("_SunDirection", toSun);
            GetComponent<Skybox>().material = sky;

            Camera camera = SampleCamera;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.nearClipPlane = 0.17f;
            camera.farClipPlane = 135000f;
            camera.fieldOfView = 47f;

            var fieldObject = new GameObject("Weather Field") { hideFlags = HideFlags.DontSave };
            fieldObject.transform.SetParent(world.transform, false);
            field = fieldObject.AddComponent<WeatherField>();
            field.Configure(preset, camera, sun, seed, false);
            geometry = new WeatherSampleGeometry(world.transform, field, haze, sunlight, toSun, preset.HazePerKilometer, seed);
            geometry.Build(site);
            PlaceView(0);
            SetFrame(FeaturedTime, false);
        }

        public void PlaceView(int index)
        {
            ViewIndex = Mathf.Clamp(index, 0, 2);
            Vector3 eye;
            Vector3 target;
            float fov = 47f;
            switch (site)
            {
                case WeatherSampleSite.StreetThunderstorm:
                    eye = ViewIndex == 0 ? new Vector3(-4.65f, 1.60f, -34f)
                        : ViewIndex == 1 ? new Vector3(-127f, 50f, -168f) : new Vector3(-4.4f, 0.87f, -30.8f);
                    target = ViewIndex == 0 ? new Vector3(1.7f, 4.45f, 80f)
                        : ViewIndex == 1 ? new Vector3(43f, 42f, 320f) : new Vector3(-4f, -0.95f, -20f);
                    fov = ViewIndex == 2 ? 55f : 53f;
                    break;
                case WeatherSampleSite.MountainValleyFog:
                case WeatherSampleSite.CumulonimbusBeyondRidge:
                case WeatherSampleSite.RidgeCloudBands:
                    eye = ViewIndex == 0 ? new Vector3(-2410f, 212f, -2110f)
                        : ViewIndex == 1 ? new Vector3(-350f, 2.2f, -1080f) : new Vector3(3260f, 160f, 680f);
                    if (site == WeatherSampleSite.MountainValleyFog && ViewIndex == 1)
                        eye.z = 3160f;
                    eye.y += geometry.MountainHeight(eye.x, eye.z);
                    target = site == WeatherSampleSite.MountainValleyFog
                        ? new Vector3(ViewIndex == 2 ? -1900f : 500f, 285f, 7170f)
                        : new Vector3(160f, site == WeatherSampleSite.RidgeCloudBands ? 2050f : 2590f, 11200f);
                    fov = site == WeatherSampleSite.MountainValleyFog ? 49f : 44f;
                    break;
                case WeatherSampleSite.CumulonimbusAcrossSea:
                case WeatherSampleSite.MorningGlory:
                    eye = ViewIndex == 0 ? new Vector3(-950f, 21f, -340f)
                        : ViewIndex == 1 ? new Vector3(1550f, 31f, -90f) : new Vector3(-3300f, 95f, -860f);
                    eye.y += Mathf.Max(0f, geometry.CoastHeight(eye.x, eye.z));
                    target = site == WeatherSampleSite.CumulonimbusAcrossSea
                        ? new Vector3(1700f, 3880f, 30300f)
                        : new Vector3(ViewIndex == 2 ? 9300f : 3960f, 320f, 6160f);
                    if (site == WeatherSampleSite.MorningGlory && ViewIndex == 1)
                        target = new Vector3(1810f, 565f, 7400f);
                    fov = site == WeatherSampleSite.MorningGlory ? 42f : 37f;
                    break;
                default:
                    eye = ViewIndex == 0 ? new Vector3(-10f, 2.15f, -254f)
                        : ViewIndex == 1 ? new Vector3(1980f, 173f, 540f) : new Vector3(-800f, 73f, 2420f);
                    eye.y += geometry.PlainHeight(eye.x, eye.z);
                    target = new Vector3(-270f, 2950f, 21500f);
                    fov = 42f;
                    break;
            }
            SampleCamera.fieldOfView = fov;
            transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye, Vector3.up));
            if (field)
                field.SetFrame(seconds);
        }

        public void SetFrame(float time, bool clearConditions)
        {
            clearSky = clearConditions;
            seconds = Mathf.Clamp(time, 0f, 110f);
            WeatherPreset active = clearConditions ? clear : preset;
            if (field.Preset != active)
                field.Configure(active, SampleCamera, sun, seed, false);
            field.SetFrame(seconds);
            geometry.UpdateWetness();
        }

        private void Update()
        {
            if (world && (site != builtSite || seed != builtSeed))
                Open(site, seed);
            else if (playing && Application.IsPlaying(gameObject))
                SetFrame(Mathf.Repeat(seconds + Time.deltaTime, 110f), clearSky);
        }

        private void OnGUI()
        {
            if (!showControls || !Application.IsPlaying(gameObject) || !field)
                return;

            GUILayout.BeginArea(new Rect(12f, 12f, Mathf.Max(220f, Mathf.Min(292f, Screen.width - 24f)), 167f), GUI.skin.box);
            GUILayout.Label(SiteNames[(int)site]);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Previous site"))
                ChangeSite(-1);
            if (GUILayout.Button("Next site"))
                ChangeSite(1);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("View " + (ViewIndex + 1)))
                PlaceView((ViewIndex + 1) % 3);
            if (GUILayout.Button(clearSky ? "Show weather" : "Show clear"))
                SetFrame(seconds, !clearSky);
            if (GUILayout.Button("Seed " + seed))
                Open(site, seed + 1);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(playing ? "Pause" : "Play time", GUILayout.Width(82f)))
                playing = !playing;
            GUILayout.Label(seconds.ToString("0.0") + " s", GUILayout.Width(55f));
            float next = GUILayout.HorizontalSlider(seconds, 0f, 110f);
            GUILayout.EndHorizontal();
            if (!Mathf.Approximately(next, seconds))
            {
                playing = false;
                SetFrame(next, clearSky);
            }
            GUILayout.EndArea();
        }

        private void ChangeSite(int delta) =>
            Open((WeatherSampleSite)(((int)site + delta + SiteNames.Length) % SiteNames.Length), seed);

        private void Release()
        {
            WeatherField.Dispose(world);
            WeatherField.Dispose(preset);
            WeatherField.Dispose(clear);
            WeatherField.Dispose(sky);
            geometry?.Dispose();
            geometry = null;
            world = null;
        }

        private static Color Tone(int rgb) => WeatherSamplePresets.Tone(rgb);
    }
}
