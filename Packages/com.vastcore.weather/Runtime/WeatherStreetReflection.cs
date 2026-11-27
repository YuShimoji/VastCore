using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.Weather
{
    internal sealed class WeatherStreetReflection : IDisposable
    {
        private readonly Camera camera;
        private readonly Skybox sky;
        private RenderTexture target;
        private Matrix4x4 previousView;
        private Matrix4x4 previousProjection;
        private Matrix4x4 reflectionViewProjection;
        private float previousWaterLevel;
        private float previousWeatherTime;
        private bool valid;

        public WeatherStreetReflection(Transform parent)
        {
            var owner = new GameObject("Puddle Reflection", typeof(Camera), typeof(Skybox))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            owner.transform.SetParent(parent, false);
            camera = owner.GetComponent<Camera>();
            camera.enabled = false;
            sky = owner.GetComponent<Skybox>();
        }

        public bool Render(Camera source, WeatherPuddle pool, Material asphalt, int weatherLayer, float weatherTime, bool forceRefresh)
        {
            Vector3 water = pool.SurfacePosition;
            if (!pool.HasWater || source.transform.position.y - water.y > 8f ||
                Vector3.Distance(source.transform.position, water) > 28f)
            {
                valid = false;
                return false;
            }

            Matrix4x4 sourceView = source.worldToCameraMatrix;
            Matrix4x4 sourceProjection = source.projectionMatrix;
            if (!forceRefresh && valid && previousView == sourceView && previousProjection == sourceProjection &&
                Mathf.Abs(previousWaterLevel - water.y) < 0.001f && Mathf.Abs(previousWeatherTime - weatherTime) < 0.066f)
            {
                ApplyReflection(pool, asphalt);
                return true;
            }

            const int height = 360;
            int width = Mathf.Clamp(Mathf.RoundToInt(source.aspect * height), 256, 1024);
            if (!target || target.width != width)
            {
                WeatherField.Dispose(target);
                target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                {
                    name = "Puddle Reflection Color",
                    hideFlags = HideFlags.HideAndDontSave,
                    useMipMap = true,
                    autoGenerateMips = false,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                target.Create();
            }

            camera.CopyFrom(source);
            camera.enabled = false;
            camera.targetTexture = target;
            camera.aspect = source.aspect;
            camera.allowMSAA = false;
            camera.allowHDR = false;
            camera.cullingMask = source.cullingMask & ~(1 << weatherLayer);
            camera.farClipPlane = Mathf.Min(56f, source.farClipPlane);
            var sourceSky = source.GetComponent<Skybox>();
            sky.material = sourceSky ? sourceSky.material : null;

            Transform eye = source.transform;
            Vector3 reflectedEye = eye.position;
            reflectedEye.y = water.y * 2f - reflectedEye.y;
            Vector3 forward = Vector3.Reflect(eye.forward, Vector3.up);
            Vector3 up = -Vector3.Reflect(eye.up, Vector3.up);
            camera.transform.SetPositionAndRotation(reflectedEye, Quaternion.LookRotation(forward, up));
            camera.ResetProjectionMatrix();

            Matrix4x4 view = camera.worldToCameraMatrix;
            Vector3 normal = view.MultiplyVector(Vector3.up).normalized;
            Vector3 origin = view.MultiplyPoint(water + Vector3.up * 0.006f);
            camera.projectionMatrix = camera.CalculateObliqueMatrix(new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(origin, normal)));
            var request = new RenderPipeline.StandardRequest { destination = target };
            if (!RenderPipeline.SupportsRenderRequest(camera, request))
                return false;

            MeshRenderer surface = pool.SurfaceRenderer;
            // The projected wet film is rendered into this target with its own sample disabled.
            ClearFilm(asphalt);
            surface.forceRenderingOff = true;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
            finally
            {
                surface.forceRenderingOff = false;
            }
            target.GenerateMips();
            reflectionViewProjection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * view;
            previousView = sourceView;
            previousProjection = sourceProjection;
            previousWaterLevel = water.y;
            previousWeatherTime = weatherTime;
            valid = true;
            ApplyReflection(pool, asphalt);
            return true;
        }

        private void ApplyReflection(WeatherPuddle pool, Material asphalt)
        {
            pool.SetReflection(target, reflectionViewProjection);
            asphalt.SetTexture("_PlanarReflection", target);
            asphalt.SetMatrix("_ReflectionViewProjection", reflectionViewProjection);
            asphalt.SetFloat("_HasPlanarReflection", 1f);
        }

        internal static void ClearFilm(Material asphalt)
        {
            asphalt.SetTexture("_PlanarReflection", Texture2D.blackTexture);
            asphalt.SetFloat("_HasPlanarReflection", 0f);
        }

        public void Dispose()
        {
            if (camera)
                WeatherField.Dispose(camera.gameObject);
            WeatherField.Dispose(target);
        }
    }
}
