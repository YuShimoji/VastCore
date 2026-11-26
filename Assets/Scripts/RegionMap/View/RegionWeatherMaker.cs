using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// Weather Maker（購入済みの空・天気の素材。公開リポジトリには置かないので、無い環境もある）を、有る時だけ使うための橋渡し。
    /// 素材は Assembly-CSharp に入り、このアセンブリからは参照できないので、型と値は名前で探して読み書きする。
    /// 無い環境では <see cref="Available"/> が false になり、呼び出し側は自作の空と霞を使う。
    /// </summary>
    public static class RegionWeatherMaker
    {
        private const string Namespace = "DigitalRuby.WeatherMaker.";
        private static readonly Hashtable types = new Hashtable();
        private static ScriptableRendererData addedTo;
        private static ScriptableRendererFeature addedFeature;

        /// <summary>Weather Maker の本体と、URP で描く機能（UNITY_URP の定義が要る）の両方が有るか。</summary>
        public static bool Available
        {
            get { return Find("WeatherMakerScript") != null && Find("WeatherMakerURPRenderFeatureScript") != null; }
        }

        /// <summary>名前（名前空間を除く）で Weather Maker の型を探す。無ければ null。</summary>
        public static Type Find(string name)
        {
            if (types.ContainsKey(name)) return (Type)types[name];
            Type found = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                found = assembly.GetType(Namespace + name, false);
                if (found != null) break;
            }
            types[name] = found;
            return found;
        }

        /// <summary>場面に置いた Weather Maker の各部（WeatherMakerScript など）の Instance。</summary>
        public static UnityEngine.Object Instance(string name)
        {
            Type type = Require(name);
            PropertyInfo property = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            return property != null ? property.GetValue(null, null) as UnityEngine.Object : null;
        }

        /// <summary>
        /// Weather Maker の描画の機能を、今の品質のレンダラーへ足す。メモリの中だけで足し、レンダラーの資産は書き換えない
        /// （素材の無い環境で、資産に欠けたスクリプトが残らないようにする）。使い終えたら <see cref="RemoveRenderFeature"/>。
        /// </summary>
        public static void AddRenderFeature()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null) throw new InvalidOperationException("URP の描画の資産が使われていない");
            var list = (ScriptableRendererData[])typeof(UniversalRenderPipelineAsset)
                .GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(pipeline);
            ScriptableRendererData data = list[0];
            Type featureType = Require("WeatherMakerURPRenderFeatureScript");
            foreach (ScriptableRendererFeature existing in data.rendererFeatures)
            {
                if (existing != null && existing.GetType() == featureType) return;
            }
            var feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(featureType);
            feature.name = "WeatherMakerURPRenderFeatureScript";
            feature.hideFlags = HideFlags.DontSave;
            data.rendererFeatures.Add(feature);
            data.SetDirty();
            addedTo = data;
            addedFeature = feature;
        }

        public static void RemoveRenderFeature()
        {
            if (addedTo == null) return;
            addedTo.rendererFeatures.Remove(addedFeature);
            addedTo.SetDirty();
            UnityEngine.Object.DestroyImmediate(addedFeature);
            addedTo = null;
            addedFeature = null;
        }

        /// <summary>カメラの後処理を入れるか切る。前の値を返す。</summary>
        public static bool SetPostProcessing(Camera camera, bool enabled)
        {
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            bool previous = data.renderPostProcessing;
            data.renderPostProcessing = enabled;
            return previous;
        }

        /// <summary>カメラを、Weather Maker が描く対象に入れる。</summary>
        public static void AllowCamera(Camera camera)
        {
            var cameras = (IList)Get(Instance("WeatherMakerScript"), "AllowCameras");
            if (!cameras.Contains(camera)) cameras.Add(camera);
        }

        /// <summary>
        /// 雲を全画素で描く（静止画を撮る時）。既定は半分の解像度で描き、数コマかけて埋めるので、1 回の描画ではぼやける。
        /// 性能の設定は写しを作って差し替える（素材の設定ファイルは書き換えない）。
        /// </summary>
        public static void UseFullQualityClouds()
        {
            UnityEngine.Object script = Instance("WeatherMakerScript");
            var copy = UnityEngine.Object.Instantiate((ScriptableObject)Get(script, "PerformanceProfile"));
            copy.hideFlags = HideFlags.DontSave;
            SetEnum(copy, "VolumetricCloudDownsampleScale", "FullResolution");
            SetEnum(copy, "VolumetricCloudTemporalReprojectionSize", "None");
            SetEnum(copy, "VolumetricCloudDownsampleScalePostProcess", "HalfResolution");
            Set(script, "AutoSetPerformanceProfile", false);
            Set(script, "PerformanceProfile", copy);
        }

        /// <summary>空の設定（WeatherMakerSkyProfileScript）を差し替える。</summary>
        public static void SetSky(ScriptableObject skyProfile)
        {
            UnityEngine.Object sky = Instance("WeatherMakerSkySphereScript");
            if (sky != null && skyProfile != null) Set(sky, "SkySphereProfile", skyProfile);
        }

        /// <summary>
        /// 太陽を、高さ altitudeDegrees・方位（太陽の方を向く水平の向き）towardSunYawDegrees に置く（Unity の Y 軸回りの角度。北 0、東 90）。
        /// 空の色は時刻と緯度から計算されるので、日付と場所を決め、午後のうち太陽がその高さになる時刻を探し、
        /// 方位は太陽の軌道を Y 軸回りに回して合わせる。時刻は止める。日・場所の設定は写しを作って差し替える。
        /// </summary>
        public static void SetSun(float altitudeDegrees, float towardSunYawDegrees)
        {
            UnityEngine.Object cycle = Instance("WeatherMakerDayNightCycleManagerScript");
            var profile = UnityEngine.Object.Instantiate((ScriptableObject)Get(cycle, "DayNightProfile"));
            profile.hideFlags = HideFlags.DontSave;
            // 北緯 37 度・春分・経度 0・時差 0。正午の太陽の高さは約 53 度で、午後の太陽が 0〜53 度の全部を通る
            const double latitude = 37.0, longitude = 0.0;
            const int year = 2026, month = 3, day = 20;
            Set(profile, "Latitude", latitude);
            Set(profile, "Longitude", longitude);
            Set(profile, "Year", year);
            Set(profile, "Month", month);
            Set(profile, "Day", day);
            Set(profile, "TimeZoneOffsetSeconds", 0);
            Set(profile, "Speed", 0f);
            Set(profile, "NightSpeed", 0f);
            Set(cycle, "DayNightProfile", profile);

            // 正午（12 時）から日没（18 時）までで、太陽の高さが目標になる時刻を二分法で探す
            double low = 12.0 * 3600.0, high = 18.0 * 3600.0;
            for (int i = 0; i < 40; i++)
            {
                double middle = (low + high) * 0.5;
                float altitude;
                float yaw;
                SunAt(middle, latitude, longitude, year, month, day, out altitude, out yaw);
                if (altitude > altitudeDegrees) low = middle; else high = middle;
            }
            double seconds = (low + high) * 0.5;
            float foundAltitude;
            float foundYaw;
            SunAt(seconds, latitude, longitude, year, month, day, out foundAltitude, out foundYaw);
            Set(cycle, "TimeOfDay", (float)seconds);
            Set(profile, "TimeOfDay", (float)seconds);

            UnityEngine.Object lights = Instance("WeatherMakerLightManagerScript");
            UnityEngine.Object sun = lights != null ? (UnityEngine.Object)Get(lights, "SunPerspective") : null;
            if (sun != null) Set(sun, "RotateYDegrees", Mathf.DeltaAngle(foundYaw, towardSunYawDegrees));
        }

        /// <summary>
        /// 雲の地図（R 量・G 濃さ・B 種類。Weather Maker の weather map）を、箱 box の水平の範囲へ貼り、箱の高さを雲の層にする。
        /// cloudProfile は雲の形と光の設定（WeatherMakerCloudProfileScript）。すぐに切り替える（既定の切り替えは 5 秒かける）。
        /// </summary>
        public static void SetClouds(Texture2D map, BoxCollider box, ScriptableObject cloudProfile)
        {
            UnityEngine.Object clouds = Instance("WeatherMakerFullScreenCloudsScript");
            Set(clouds, "WeatherMapOverride", map);
            Set(clouds, "VolumetricCloudBoxRemap", box);
            MethodInfo show = clouds.GetType().GetMethod("ShowCloudsAnimated",
                new[] { cloudProfile.GetType(), typeof(float), typeof(float), typeof(string) });
            if (show == null) throw new MissingMethodException("WeatherMakerFullScreenCloudsScript.ShowCloudsAnimated");
            show.Invoke(clouds, new object[] { cloudProfile, 0f, 0f, null });
        }

        /// <summary>雲の量が 1 になる 2D の雲の濃さ（0.42 は 2D の地図で雲が不透明になる濃さ。RegionClouds.Shade）。</summary>
        public static float CoverFullDensity = 0.42f;

        /// <summary>
        /// 2D の雲の濃さ（RegionCloudSky の低い雲）から雲の地図の 1 画素を作る。
        /// 量は 2D の地図で雲が見える濃さ（RegionClouds.Shade の不透明さ）と同じ範囲で 0〜1 にし、厚い所ほど積雲に寄せる。
        /// </summary>
        public static Color CloudMapPixel(float density)
        {
            float cover = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(density / CoverFullDensity));
            float cumulus = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((density - 0.1f) / 0.5f));
            return new Color(cover, 1f, 0.35f + 0.65f * cumulus, 1f);
        }

        private static void SunAt(double seconds, double latitude, double longitude, int year, int month, int day, out float altitude, out float yaw)
        {
            Type profileType = Require("WeatherMakerDayNightCycleProfileScript");
            Type infoType = profileType.GetNestedType("SunInfo", BindingFlags.Public);
            object info = Activator.CreateInstance(infoType);
            infoType.GetField("DateTime").SetValue(info, new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc) + TimeSpan.FromSeconds(seconds));
            infoType.GetField("Latitude").SetValue(info, latitude);
            infoType.GetField("Longitude").SetValue(info, longitude);
            infoType.GetField("AxisTilt").SetValue(info, 23.439);
            profileType.GetMethod("CalculateSunPosition", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { info, 0f });
            var up = (Vector3)infoType.GetField("UnitVectorUp").GetValue(info);
            altitude = Mathf.Asin(Mathf.Clamp(up.y, -1f, 1f)) * Mathf.Rad2Deg;
            yaw = Mathf.Atan2(up.x, up.z) * Mathf.Rad2Deg;
        }

        private static Type Require(string name)
        {
            Type type = Find(name);
            if (type == null) throw new InvalidOperationException("Weather Maker の型が無い: " + name);
            return type;
        }

        private static object Get(object target, string name)
        {
            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null) return property.GetValue(target, null);
            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null) return field.GetValue(target);
            throw new MissingMemberException(type.Name, name);
        }

        private static void Set(object target, string name, object value)
        {
            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                property.SetValue(target, Convert(value, property.PropertyType), null);
                return;
            }
            FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field == null) throw new MissingMemberException(type.Name, name);
            field.SetValue(target, Convert(value, field.FieldType));
        }

        private static void SetEnum(object target, string name, string valueName)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field == null) throw new MissingMemberException(target.GetType().Name, name);
            field.SetValue(target, Enum.Parse(field.FieldType, valueName));
        }

        private static object Convert(object value, Type type)
        {
            if (value == null || type.IsInstanceOfType(value)) return value;
            return System.Convert.ChangeType(value, type, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
