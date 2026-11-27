using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.Weather
{
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class WeatherField : MonoBehaviour
    {
        [SerializeField] private WeatherPreset preset;
        [Tooltip("Camera for the nearby raindrops. Enable Depth Texture on cameras that render weather volumes.")]
        [SerializeField] private Camera rainObserver;
        [SerializeField] private Light sun;
        [SerializeField] private int seed = 17;
        [SerializeField] private bool advanceInPlay = true;
        [SerializeField, Min(0f)] private float weatherSeconds;
        [SerializeField, Min(0f)] private float playbackRate = 1f;

        private GameObject effects;
        private Mesh box;
        private Mesh drops;
        private Material volumeMaterial;
        private Material precipitationMaterial;
        private Material boltMaterial;
        private MeshRenderer[] volumes;
        private MeshRenderer curtain;
        private MeshRenderer streaks;
        private LineRenderer[] bolts;
        private WeatherPreset renderedPreset;
        private int boltEvent = -1;
        private MaterialPropertyBlock properties;

        public WeatherPreset Preset => preset;
        public float WeatherSeconds => weatherSeconds;

        public void Configure(WeatherPreset profile, Camera observer, Light daylight, int variationSeed, bool autoAdvance = true)
        {
            preset = profile;
            rainObserver = observer;
            sun = daylight;
            seed = variationSeed;
            advanceInPlay = autoAdvance;
            boltEvent = -1;
            Refresh();
        }

        public void SetFrame(float seconds)
        {
            weatherSeconds = Mathf.Max(0f, seconds);
            Refresh();
        }

        public float RainAt(Vector3 worldPosition) =>
            isActiveAndEnabled && preset && preset.Rain != null
                ? preset.Rain.RainAt(transform.InverseTransformPoint(worldPosition), weatherSeconds)
                : 0f;

        public float WetnessAt(Vector3 worldPosition)
        {
            if (!isActiveAndEnabled || !preset || preset.Rain == null || preset.Rain.Intensity <= 0f)
                return 0f;

            WeatherRainCell rain = preset.Rain;
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            float duration = Mathf.Max(1f, rain.DryOutSeconds);
            float wetness = 0f;
            const int historySamples = 12;
            for (int index = 0; index <= historySamples; index++)
            {
                float fraction = index / (float)historySamples;
                float deposit = rain.RainAt(localPosition, weatherSeconds - fraction * duration);
                wetness = Mathf.Max(wetness, deposit * Mathf.Exp(-fraction * 3.6f));
            }
            return wetness;
        }

        private void OnEnable() => Refresh();
        private void OnDisable() => Release();

        private void LateUpdate()
        {
            if (Application.IsPlaying(gameObject) && advanceInPlay)
                weatherSeconds += Time.deltaTime * playbackRate;
            Refresh();
        }

        private void Refresh()
        {
            if (!isActiveAndEnabled)
                return;

            int count = preset && preset.Volumes != null ? preset.Volumes.Length : 0;
            bool hasRain = preset && preset.Rain != null && preset.Rain.Intensity > 0f;
            if (preset != renderedPreset || volumes?.Length != count || hasRain != (streaks != null) || (preset && !effects))
                Build(count, hasRain);
            if (!effects)
                return;

            properties ??= new MaterialPropertyBlock();
            Vector3 toSun = sun ? -sun.transform.forward : new Vector3(-0.48f, 0.66f, -0.57f).normalized;
            float flash = hasRain ? UpdateRain(preset.Rain, toSun) : 0f;
            for (int index = 0; index < count; index++)
            {
                WeatherVolume volume = preset.Volumes[index];
                MeshRenderer visual = volumes[index];
                visual.enabled = volume != null && volume.ExtinctionPerKilometer > 0f;
                if (!visual.enabled)
                    continue;

                visual.transform.localPosition = volume.CenterMeters + volume.DriftMetersPerSecond * weatherSeconds;
                visual.transform.localRotation = Quaternion.Euler(0f, volume.YawDegrees, 0f);
                visual.transform.localScale = Vector3.Max(Vector3.one, volume.SizeMeters);
                ApplyVolume(visual, volume.Shape, volume.ExtinctionPerKilometer, volume.DetailSizeMeters,
                    volume.SunlitColor, volume.ShadeColor, weatherSeconds * volume.FlowRadiansPerSecond,
                    toSun, flash, index);
            }
        }

        private void Build(int count, bool hasRain)
        {
            Release();
            renderedPreset = preset;
            volumes = new MeshRenderer[count];
            if (!preset)
                return;

            effects = new GameObject("Weather Effects") { hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave };
            effects.layer = gameObject.layer;
            effects.transform.SetParent(transform, false);
            box = CreateBox();
            volumeMaterial = NewMaterial("Volume");
            for (int index = 0; index < count; index++)
                volumes[index] = NewMeshRenderer("Weather Volume " + (index + 1), box, volumeMaterial);

            if (!hasRain)
                return;

            curtain = NewMeshRenderer("Rain Curtain", box, volumeMaterial);
            drops = CreateDrops();
            precipitationMaterial = NewMaterial("Precipitation");
            streaks = NewMeshRenderer("Visible Raindrops", drops, precipitationMaterial);
            boltMaterial = NewMaterial("Precipitation");
            boltMaterial.SetFloat("_RenderBolt", 1f);
            bolts = new LineRenderer[3];
            for (int index = 0; index < bolts.Length; index++)
            {
                GameObject child = NewEffect("Lightning");
                var line = child.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.sharedMaterial = boltMaterial;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.enabled = false;
                bolts[index] = line;
            }
        }

        private float UpdateRain(WeatherRainCell rain, Vector3 toSun)
        {
            Vector3 center = rain.CenterAt(weatherSeconds);
            Vector2 radius = rain.SafeRadius;
            float strength = rain.IntensityAt(weatherSeconds);
            float cloudBase = Mathf.Max(1f, rain.CloudBaseMeters);
            curtain.enabled = strength > 0f && rain.CurtainExtinctionPerKilometer > 0f;
            if (curtain.enabled)
            {
                curtain.transform.localPosition = center + Vector3.up * cloudBase * 0.49f;
                curtain.transform.localScale = new Vector3(radius.x * 2.2f, cloudBase * 1.06f, radius.y * 2.2f);
                ApplyVolume(curtain, WeatherShape.RainCurtain, strength * rain.CurtainExtinctionPerKilometer,
                    180f, rain.CurtainColor, rain.CurtainColor, 0.13f * weatherSeconds, toSun, 0f, volumes.Length);
            }

            streaks.enabled = rainObserver && strength > 0f;
            if (streaks.enabled)
            {
                Vector3 eye = rainObserver.transform.position;
                streaks.transform.position = eye;
                Vector3 wind = transform.TransformVector(new Vector3(rain.WindMetersPerSecond.x, 0f, rain.WindMetersPerSecond.y));
                properties.Clear();
                properties.SetMatrix("_WeatherWorldToLocal", transform.worldToLocalMatrix);
                properties.SetVector("_RainCenter", center);
                properties.SetVector("_RainRadius", new Vector4(radius.x, radius.y, 0f, 0f));
                properties.SetVector("_RainWind", wind);
                properties.SetVector("_RainObserver", eye);
                properties.SetFloat("_WeatherTime", weatherSeconds);
                properties.SetFloat("_RainIntensity", strength);
                properties.SetFloat("_WeatherSeed", seed & 4095);
                streaks.SetPropertyBlock(properties);
            }

            return UpdateLightning(rain, center, radius, strength);
        }

        private float UpdateLightning(WeatherRainCell rain, Vector3 center, Vector2 radius, float intensity)
        {
            float interval = Mathf.Max(6f, rain.LightningIntervalSeconds);
            float stormTime = Mathf.Max(0f, weatherSeconds - rain.StartSeconds);
            int strike = Mathf.FloorToInt(stormTime / interval);
            float phase = stormTime - strike * interval - interval * 0.45f;
            bool visible = rain.LightningIntervalSeconds > 0f && intensity > 0.3f && phase >= 0f && phase < 0.22f;
            float brightness = visible ? Mathf.Exp(-phase * 12f) * (0.58f + 0.42f * Mathf.Abs(Mathf.Cos(phase * 58f))) : 0f;

            if (visible && strike != boltEvent)
            {
                boltEvent = strike;
                var random = new System.Random(unchecked(seed * 1723 + strike * 1447));
                float spread = Mathf.Min(radius.x, radius.y);
                Vector3 ground = new Vector3((float)random.NextDouble() - 0.5f, 0f, (float)random.NextDouble() - 0.5f) * spread;
                Vector3 top = ground + Vector3.up * rain.CloudBaseMeters + Vector3.right * (spread * 0.36f);
                Vector3 junction = MakeBolt(bolts[0], top, ground, 24, spread * 0.12f, random);
                MakeBolt(bolts[1], junction, ground - new Vector3(spread * 0.40f, -rain.CloudBaseMeters * 0.19f, spread * 0.20f),
                    11, spread * 0.055f, random);
                MakeBolt(bolts[2], junction, ground + new Vector3(spread * 0.32f, rain.CloudBaseMeters * 0.26f, -spread * 0.3f),
                    9, spread * 0.045f, random);
            }

            for (int index = 0; index < bolts.Length; index++)
            {
                LineRenderer line = bolts[index];
                line.enabled = visible;
                if (!visible)
                    continue;
                line.transform.localPosition = center;
                properties.Clear();
                properties.SetColor("_BoltColor", new Color(0.67f, 0.82f, 1f, brightness));
                line.SetPropertyBlock(properties);
            }

            return brightness;
        }

        private static Vector3 MakeBolt(LineRenderer line, Vector3 from, Vector3 to, int segments, float width, System.Random random)
        {
            line.positionCount = segments + 1;
            line.startWidth = Mathf.Clamp(width * 0.05f, 0.4f, 5f);
            line.endWidth = line.startWidth * 0.25f;
            for (int index = 0; index <= segments; index++)
            {
                float fraction = index / (float)segments;
                Vector3 point = Vector3.Lerp(from, to, fraction);
                float step = Mathf.Sin(fraction * Mathf.PI) * width;
                point.x += ((float)random.NextDouble() * 2f - 1f) * step;
                point.z += ((float)random.NextDouble() * 2f - 1f) * step;
                line.SetPosition(index, point);
            }
            return line.GetPosition(segments / 3);
        }

        private void ApplyVolume(MeshRenderer target, WeatherShape shape, float extinction, float detailSize,
            Color light, Color shade, float flow, Vector3 sunDirection, float flash, int index)
        {
            properties.Clear();
            properties.SetFloat("_Shape", (float)shape);
            properties.SetFloat("_Extinction", extinction * 0.001f);
            properties.SetFloat("_VolumeSteps", Mathf.Clamp(preset.VolumeSteps, 16, 64));
            properties.SetFloat("_Flash", flash);
            properties.SetColor("_SunlitColor", light);
            properties.SetColor("_ShadeColor", shade);
            properties.SetColor("_AerialHaze", preset.AerialHaze);
            properties.SetFloat("_HazeExtinction", preset.HazePerKilometer * 0.001f);
            properties.SetVector("_WeatherSun", sunDirection);
            properties.SetVector("_VolumeDetail", new Vector4(
                target.transform.localScale.x / Mathf.Max(1f, detailSize),
                target.transform.localScale.y / Mathf.Max(1f, detailSize),
                target.transform.localScale.z / Mathf.Max(1f, detailSize),
                flow));
            properties.SetFloat("_WeatherSeed", (seed + index * 61) & 4095);
            target.SetPropertyBlock(properties);
        }

        private GameObject NewEffect(string label)
        {
            var child = new GameObject(label) { hideFlags = HideFlags.DontSave, layer = gameObject.layer };
            child.transform.SetParent(effects.transform, false);
            return child;
        }

        private MeshRenderer NewMeshRenderer(string label, Mesh mesh, Material material)
        {
            GameObject child = NewEffect(label);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return renderer;
        }

        private void Release()
        {
            if (effects)
                effects.SetActive(false);
            Dispose(effects);
            Dispose(box);
            Dispose(drops);
            Dispose(volumeMaterial);
            Dispose(precipitationMaterial);
            Dispose(boltMaterial);
            effects = null;
            box = null;
            drops = null;
            volumeMaterial = null;
            precipitationMaterial = null;
            boltMaterial = null;
            volumes = null;
            curtain = null;
            streaks = null;
            bolts = null;
            renderedPreset = null;
            boltEvent = -1;
        }

        internal static Material NewMaterial(string shader)
        {
            var source = Resources.Load<Shader>("VastcoreWeather/" + shader);
            if (!source)
                throw new System.InvalidOperationException("VastCore Weather shader is missing: " + shader);
            return new Material(source) { name = "Weather " + shader, hideFlags = HideFlags.HideAndDontSave };
        }

        internal static void Dispose(Object asset)
        {
            if (!asset)
                return;
            if (Application.isPlaying)
                Destroy(asset);
            else
                DestroyImmediate(asset);
        }

        private static Mesh CreateBox()
        {
            return new Mesh
            {
                name = "Weather Volume Bounds",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
                    new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                    new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
                    new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
                },
                triangles = new[]
                {
                    0, 3, 2, 0, 2, 1, 4, 5, 6, 4, 6, 7, 0, 4, 7, 0, 7, 3,
                    1, 2, 6, 1, 6, 5, 0, 1, 5, 0, 5, 4, 3, 7, 6, 3, 6, 2
                },
                bounds = new Bounds(Vector3.zero, Vector3.one)
            };
        }

        private static Mesh CreateDrops()
        {
            const int count = 2500;
            var vertices = new Vector3[count * 4];
            var corners = new Vector2[count * 4];
            var indices = new int[count * 6];
            var random = new System.Random(917);
            for (int index = 0; index < count; index++)
            {
                int vertex = index * 4;
                int triangle = index * 6;
                var point = new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
                for (int corner = 0; corner < 4; corner++)
                    vertices[vertex + corner] = point;
                corners[vertex] = new Vector2(-1f, 0f);
                corners[vertex + 1] = new Vector2(1f, 0f);
                corners[vertex + 2] = new Vector2(1f, 1f);
                corners[vertex + 3] = new Vector2(-1f, 1f);
                indices[triangle] = vertex;
                indices[triangle + 1] = vertex + 1;
                indices[triangle + 2] = vertex + 2;
                indices[triangle + 3] = vertex;
                indices[triangle + 4] = vertex + 2;
                indices[triangle + 5] = vertex + 3;
            }

            // Vertices hold random numbers. The vertex shader places the streaks around the observer.
            return new Mesh
            {
                name = "Weather Rain Streak Seeds",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = vertices,
                uv = corners,
                triangles = indices,
                bounds = new Bounds(Vector3.zero, Vector3.one * 48f)
            };
        }
    }
}
