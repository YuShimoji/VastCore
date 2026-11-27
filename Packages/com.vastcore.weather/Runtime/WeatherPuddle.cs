using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.Weather
{
    [ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(10)]
    public sealed class WeatherPuddle : MonoBehaviour
    {
        [SerializeField] private WeatherField weather;
        [SerializeField] private Vector2 sizeMeters = new Vector2(2f, 5f);
        [SerializeField] private Color wetGround = new Color(0.095f, 0.11f, 0.13f);
        [SerializeField] private Color reflectedSky = new Color(0.55f, 0.64f, 0.72f);
        [SerializeField] private int outlineSeed = 15;

        private GameObject projection;
        private Mesh mesh;
        private Material material;
        private MeshRenderer renderer;
        private MaterialPropertyBlock properties;

        public void Configure(WeatherField field, Vector2 size, Color ground, Color sky, int seed)
        {
            weather = field;
            sizeMeters = size;
            wetGround = ground;
            reflectedSky = sky;
            outlineSeed = seed;
            Refresh();
        }

        public void Refresh()
        {
            if (!isActiveAndEnabled)
                return;
            if (!weather)
            {
                if (projection)
                    projection.SetActive(false);
                return;
            }
            if (!projection)
                Build();

            float wetness = weather.WetnessAt(transform.position);
            projection.SetActive(wetness > 0.02f);
            if (!projection.activeSelf)
                return;

            projection.transform.localScale = new Vector3(Mathf.Max(0.05f, sizeMeters.x), 1f, Mathf.Max(0.05f, sizeMeters.y));
            properties.SetFloat("_Wetness", wetness);
            properties.SetFloat("_RainIntensity", weather.RainAt(transform.position));
            properties.SetFloat("_WeatherTime", weather.WeatherSeconds);
            properties.SetFloat("_OutlineSeed", outlineSeed & 4095);
            properties.SetColor("_WetGround", wetGround);
            properties.SetColor("_ReflectedSky", reflectedSky);
            renderer.SetPropertyBlock(properties);
        }

        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();

        private void Build()
        {
            projection = new GameObject("Puddle Surface")
            {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave,
                layer = gameObject.layer
            };
            projection.transform.SetParent(transform, false);
            mesh = new Mesh
            {
                name = "Puddle Projection",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new[]
                {
                    new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f),
                    new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f)
                },
                uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
                triangles = new[] { 0, 3, 2, 0, 2, 1 },
                bounds = new Bounds(Vector3.zero, new Vector3(1f, 0.04f, 1f))
            };
            material = WeatherField.NewMaterial("Puddle");
            projection.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = projection.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            properties = new MaterialPropertyBlock();
        }

        private void OnDisable()
        {
            if (projection)
                projection.SetActive(false);
            WeatherField.Dispose(projection);
            WeatherField.Dispose(mesh);
            WeatherField.Dispose(material);
            projection = null;
            mesh = null;
            material = null;
            renderer = null;
        }
    }
}
