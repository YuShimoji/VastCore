using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// Weather Maker の塊の雲の形の模様を、2D の雲の模様が流れた距離だけずらしておく（本人の選択「模様を2Dの風で流す」、2026-11-27）。
    /// 雲の地図は雲の出る場所だけを決め、塊の形と位置は形の模様で決まるので、模様を動かさないと塊が 2D と一緒に流れない（sky-trial m37〜m39）。
    /// Weather Maker は模様を (位置 + 速さ × 経過秒) で読み、ずれそのものは渡せないので、描く前に毎回 速さ = −ずれ ÷ 経過秒 にする。
    /// 経過秒は Weather Maker と同じ Time.timeSinceLevelLoad（再生していない時は 0 なので、時刻を 1 秒として書き直す）。
    /// </summary>
    [ExecuteAlways]
    public sealed class RegionCloudShapeShift : MonoBehaviour
    {
        /// <summary>模様を流す距離（m）。x が東、z が北。時刻に比例して増え、一周で戻らない（RegionCloudSky.PatternShift）。</summary>
        public Vector3 Shift;

        /// <summary>Weather Maker の塊の雲の層（WeatherMakerCloudVolumetricProfileScript）。</summary>
        public ScriptableObject Layer;

        private FieldInfo shape, detail;
        private static readonly int WeatherMakerTime = Shader.PropertyToID("_WeatherMakerTime");

        private void OnEnable()
        {
            RenderPipelineManager.beginContextRendering += BeforeRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginContextRendering -= BeforeRendering;
        }

        private void Update()
        {
            Apply();
        }

        private void BeforeRendering(ScriptableRenderContext context, System.Collections.Generic.List<Camera> cameras)
        {
            Apply();
        }

        /// <summary>今の経過秒で模様がちょうど Shift だけずれる速さを層に入れる。</summary>
        public void Apply()
        {
            if (Layer == null) return;
            if (shape == null)
            {
                shape = Layer.GetType().GetField("CloudShapeAnimationVelocity");
                detail = Layer.GetType().GetField("CloudDetailAnimationVelocity");
            }
            float seconds = Time.timeSinceLevelLoad;
            if (seconds <= 0.01f)
            {
                // 再生していない時（エディタでの撮影）は経過秒が 0 のままで、速さを掛けてもずれない。
                // Weather Maker が LateUpdate で書く時刻を、描く直前に 1 秒として書き直す（他の時刻の動きは 1 秒の時点で止まって見える）
                seconds = 1f;
                Shader.SetGlobalVector(WeatherMakerTime, new Vector4(seconds * 0.05f, seconds, 0f, seconds));
            }
            Vector3 velocity = -Shift / seconds;
            if (shape != null) shape.SetValue(Layer, velocity);
            if (detail != null) detail.SetValue(Layer, velocity);
        }
    }
}
