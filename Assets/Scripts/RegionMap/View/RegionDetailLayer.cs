using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Vastcore.RegionMap
{
    /// <summary>細部の層の状態。</summary>
    public enum RegionDetailPhase
    {
        /// <summary>出していない（縮尺が小さい、または切ってある）。</summary>
        Off = 0,

        /// <summary>見ている範囲が落ち着くのを待っている。</summary>
        Waiting = 1,

        /// <summary>別スレッドで作っている。できるまでは広域の絵（または前の細部）を出す。</summary>
        Building = 2,

        /// <summary>見ている範囲を覆う細部が出ている。</summary>
        Ready = 3,

        /// <summary>作成に失敗した。同じ場所では作り直さない。</summary>
        Failed = 4,
    }

    /// <summary>
    /// 探索画面で拡大した時に、見ている場所を細かい地形（RegionDetailBuilder）で描き直して広域の絵の上へ重ねる層。
    /// 窓は、拡大率が 2 倍になるごとに半分の広さへ作り直す。窓は見える範囲より少し広く（WindowMargin 倍）、
    /// 見える範囲が窓から出るか、拡大率が次の段へ入ると、落ち着いてから作り直す。作成は別スレッドで進み、
    /// できるまでは広域の絵（または前の窓）がそのまま見える。できた窓は 0.4 秒で重ねて現れ、縁は広域の絵へ溶ける。
    /// 海岸・川・山の位置は、RegionDetailBuilder が広域のデータから引くので広域と一致する。
    /// </summary>
    public sealed class RegionDetailLayer
    {
        /// <summary>この拡大率から細部を出す（地図の幅が画面の幅に合う時が 1）。</summary>
        public const float MinZoom = 4f;

        /// <summary>窓の一辺のセル数。</summary>
        public const int Resolution = 1024;

        /// <summary>窓の広さを、その段の見える幅の何倍にするか。</summary>
        public const float WindowMargin = 1.25f;

        /// <summary>細部の絵の起伏の強調。広域と同じ 2.6 倍のままだと、影が実際の 2.6 倍に伸びる。</summary>
        public const float DetailExaggeration = 1.5f;

        private const float SettleSeconds = 0.5f;
        private const float FadeSeconds = 0.4f;
        // 窓の縁が溶ける幅（窓の一辺に対する割合）
        private const float FeatherFraction = 0.035f;
        // 窓の中心が前回の狙いからこれ以上（窓の一辺に対する割合）動いたら、落ち着くまで待ち直す
        private const float RetargetFraction = 0.04f;

        /// <summary>別スレッドで作った結果。</summary>
        public sealed class Result
        {
            public RegionDetail Detail;
            public byte[] Pixels;
            public long BuildMs;
            public long RenderMs;
            public int Level;
            public RegionMapStyle Style;
            public int Generation;
            public bool Restyled;
        }

        private sealed class Slot
        {
            public RawImage Image;
            public Texture2D Texture;
            public RegionDetail Detail;
            public int Level;
            public RegionMapStyle Style;
            public float Alpha;
        }

        // 作成の並列処理に使うコアを、画面の更新や描画用に 2 つ残す。全コアを使い切ると、作っている間に画面の更新が数百 ms 止まる
        // （Parallel.For は、呼んだタスクのスケジューラの同時実行数までしか広がらない）
        private static readonly TaskScheduler Background =
            new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, Math.Max(1, Environment.ProcessorCount - 2)).ConcurrentScheduler;

        private readonly RectTransform _root;
        private Slot _front;
        private Slot _back;
        private Task<Result> _task;
        private bool _taskRestyle;
        private int _generation;
        private float _startedAt;

        private int _wantLevel = int.MinValue;
        private float _wantX;
        private float _wantY;
        private float _waited;
        private int _failedLevel = int.MinValue;
        private float _failedX;
        private float _failedY;
        private string _failure = string.Empty;

        /// <summary>切ると細部を出さず、広域の絵だけにする（確認用）。</summary>
        public bool Enabled = true;

        public RegionDetailPhase Phase { get; private set; }

        /// <summary>最後に完了した作成の時間（ms）。</summary>
        public long LastBuildMs { get; private set; }

        public long LastRenderMs { get; private set; }

        /// <summary>これまでに作った窓の数（絵の描き直しだけの分は数えない）。</summary>
        public int BuildCount { get; private set; }

        public string Failure
        {
            get { return _failure; }
        }

        /// <summary>出ている窓（無ければ null）。</summary>
        public RegionDetail Current
        {
            get { return _front != null ? _front.Detail : null; }
        }

        /// <summary>作っている最中の経過秒。作っていなければ 0。</summary>
        public float BuildSeconds
        {
            get { return _task != null ? Time.realtimeSinceStartup - _startedAt : 0f; }
        }

        /// <summary>窓が見えている濃さ（0〜1）。</summary>
        public float Alpha
        {
            get { return _front != null ? _front.Alpha : 0f; }
        }

        public RegionDetailLayer(RectTransform parent)
        {
            var created = new GameObject("Detail", typeof(RectTransform));
            created.transform.SetParent(parent, false);
            _root = (RectTransform)created.transform;
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;
            _back = CreateSlot("Detail Back");
            _front = CreateSlot("Detail Front");
        }

        private Slot CreateSlot(string name)
        {
            var created = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            created.transform.SetParent(_root, false);
            var image = created.GetComponent<RawImage>();
            image.raycastTarget = false;
            image.enabled = false;
            return new Slot { Image = image };
        }

        // ---- 窓の決め方（UnityEngine に依存しない部分。テストが直接呼ぶ） ----

        /// <summary>拡大率が入っている段（2 倍ごと。MinZoom の段が 2）。</summary>
        public static int LevelOf(float zoom)
        {
            return (int)Math.Floor(Math.Log(Math.Max(zoom, 0.0001f), 2.0));
        }

        /// <summary>
        /// その段で作る窓の一辺（km）。段の下限の拡大率で見える幅の WindowMargin 倍。
        /// RegionDetailBuilder が作れる下限（0.5 km）より狭くはしない（小さい地域を深く拡大した時）。
        /// </summary>
        public static float WindowKmFor(float regionKm, int level)
        {
            return Math.Min(regionKm, Math.Max(0.5f, regionKm / (float)Math.Pow(2.0, level) * WindowMargin));
        }

        /// <summary>見えている範囲（地図座標）が、窓の中に収まっているか。</summary>
        public static bool Covers(RegionDetail detail, float centerX, float centerY, float halfWidth, float halfHeight)
        {
            if (detail == null) return false;
            float west = Math.Max(0f, centerX - halfWidth);
            float east = Math.Min(RegionData.MapUnits, centerX + halfWidth);
            float north = Math.Max(0f, centerY - halfHeight);
            float south = Math.Min(RegionData.MapUnits, centerY + halfHeight);
            const float Slack = 0.001f;
            return west >= detail.OriginMapX - Slack && east <= detail.OriginMapX + detail.SizeMap + Slack
                && north >= detail.OriginMapY - Slack && south <= detail.OriginMapY + detail.SizeMap + Slack;
        }

        /// <summary>窓の縁を、広域の絵へ溶けるように透明にする（RGBA32 の A を書き換える）。</summary>
        public static void Feather(byte[] pixels, int size)
        {
            int width = Math.Max(2, (int)(size * FeatherFraction));
            for (int y = 0; y < size; y++)
            {
                int dy = Math.Min(y, size - 1 - y);
                for (int x = 0; x < size; x++)
                {
                    int d = Math.Min(Math.Min(x, size - 1 - x), dy);
                    if (d >= width) continue;
                    float t = (d + 0.5f) / width;
                    t = t * t * (3f - 2f * t);
                    pixels[(x + y * size) * 4 + 3] = (byte)(255f * t + 0.5f);
                }
            }
        }

        /// <summary>細部の絵の描き方。広域と同じ様式で、雲は描かず、起伏の強調は弱くする。</summary>
        public static RegionRenderOptions DetailOptions(RegionRenderOptions render)
        {
            return new RegionRenderOptions
            {
                Style = render.Style,
                CloudCover = 0f,
                Exaggeration = DetailExaggeration,
                CastShadow = render.CastShadow,
                SunAltitude = render.SunAltitude,
                Rivers = render.Rivers,
            };
        }

        /// <summary>窓を作って描く（別スレッドで呼ぶ。Unity の API は使わない）。</summary>
        public static Result Make(RegionData region, float centerX, float centerY, float windowKm, int resolution, RegionRenderOptions render)
        {
            var watch = Stopwatch.StartNew();
            RegionDetail detail = RegionDetailBuilder.Build(region, new RegionDetailSpec
            {
                CenterMapX = centerX,
                CenterMapY = centerY,
                SizeKm = windowKm,
                Resolution = resolution,
            });
            long buildMs = watch.ElapsedMilliseconds;
            watch.Restart();
            byte[] pixels = RegionMapRenderer.Render(detail.Data, DetailOptions(render));
            Feather(pixels, detail.Data.Resolution);
            return new Result { Detail = detail, Pixels = pixels, BuildMs = buildMs, RenderMs = watch.ElapsedMilliseconds, Style = render.Style };
        }

        /// <summary>同じ窓を別の描き方で描き直す（別スレッドで呼ぶ）。</summary>
        public static Result Redraw(RegionDetail detail, RegionRenderOptions render)
        {
            var watch = Stopwatch.StartNew();
            byte[] pixels = RegionMapRenderer.Render(detail.Data, DetailOptions(render));
            Feather(pixels, detail.Data.Resolution);
            return new Result { Detail = detail, Pixels = pixels, RenderMs = watch.ElapsedMilliseconds, Style = render.Style, Restyled = true };
        }

        // ---- 毎フレーム ----

        /// <summary>
        /// 見ている範囲（中心と半分の幅・高さ、地図座標）と拡大率から、窓を作る・出す・消すを進める。
        /// render は今の描き方（作る時に写しを取る）。
        /// </summary>
        public void Tick(RegionData region, RegionRenderOptions render, float centerX, float centerY, float halfWidth, float halfHeight, float zoom, float deltaTime)
        {
            Collect(zoom);
            bool show = region != null && Enabled && zoom >= MinZoom;
            UpdateFade(show, deltaTime);
            if (!show)
            {
                Phase = RegionDetailPhase.Off;
                _wantLevel = int.MinValue;
                _waited = 0f;
                return;
            }

            int level = LevelOf(zoom);
            float windowKm = WindowKmFor(region.SizeKm, level);
            float windowMap = windowKm / region.SizeKm * RegionData.MapUnits;
            Slot front = _front;
            bool covers = front.Detail != null && front.Level == level && Covers(front.Detail, centerX, centerY, halfWidth, halfHeight);

            if (covers && front.Style == render.Style)
            {
                Phase = RegionDetailPhase.Ready;
                _wantLevel = int.MinValue;
                _waited = 0f;
                return;
            }

            if (_task != null)
            {
                Phase = RegionDetailPhase.Building;
                return;
            }

            // 窓は合っているが描き方だけ違う（描き方を切り替えた直後）。描き直すだけでよい
            if (covers)
            {
                Begin(true, region, render, level, centerX, centerY, windowKm);
                return;
            }

            if (_failedLevel == level && Math.Abs(_failedX - centerX) < windowMap * RetargetFraction && Math.Abs(_failedY - centerY) < windowMap * RetargetFraction)
            {
                Phase = RegionDetailPhase.Failed;
                return;
            }

            if (_wantLevel != level || Math.Abs(_wantX - centerX) > windowMap * RetargetFraction || Math.Abs(_wantY - centerY) > windowMap * RetargetFraction)
            {
                _wantLevel = level;
                _wantX = centerX;
                _wantY = centerY;
                _waited = 0f;
            }
            _waited += deltaTime;
            Phase = RegionDetailPhase.Waiting;
            if (_waited >= SettleSeconds) Begin(false, region, render, level, centerX, centerY, windowKm);
        }

        private void Begin(bool restyle, RegionData region, RegionRenderOptions render, int level, float centerX, float centerY, float windowKm)
        {
            RegionRenderOptions options = CloneOptions(render);
            int generation = _generation;
            _startedAt = Time.realtimeSinceStartup;
            _taskRestyle = restyle;
            Phase = RegionDetailPhase.Building;
            if (restyle)
            {
                RegionDetail detail = _front.Detail;
                _task = Task.Factory.StartNew(() =>
                {
                    Result r = Redraw(detail, options);
                    r.Level = level;
                    r.Generation = generation;
                    return r;
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, Background);
            }
            else
            {
                _task = Task.Factory.StartNew(() =>
                {
                    Result r = Make(region, centerX, centerY, windowKm, Resolution, options);
                    r.Level = level;
                    r.Generation = generation;
                    return r;
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, Background);
            }
        }

        private static RegionRenderOptions CloneOptions(RegionRenderOptions render)
        {
            return new RegionRenderOptions
            {
                Style = render.Style,
                CloudCover = render.CloudCover,
                Exaggeration = render.Exaggeration,
                CastShadow = render.CastShadow,
                SunAltitude = render.SunAltitude,
                Rivers = render.Rivers,
            };
        }

        /// <summary>終わった作成を受け取り、窓を差し替える。古くなった結果は捨てる。</summary>
        private void Collect(float zoom)
        {
            if (_task == null || !_task.IsCompleted) return;
            Task<Result> finished = _task;
            _task = null;
            if (finished.IsFaulted)
            {
                Exception error = finished.Exception != null ? finished.Exception.GetBaseException() : null;
                _failure = error != null ? error.Message : "原因不明";
                _failedLevel = _wantLevel;
                _failedX = _wantX;
                _failedY = _wantY;
                Phase = RegionDetailPhase.Failed;
                return;
            }
            Result result = finished.Result;
            // 地域が変わった、細部を出さない縮尺になった、段が2つ以上ずれた結果は使わない
            if (result.Generation != _generation || zoom < MinZoom || Math.Abs(result.Level - LevelOf(zoom)) > 1) return;
            if (result.Restyled)
            {
                // 描き直した間に窓が差し替わっていたら捨てる
                if (_front.Detail != result.Detail) return;
            }
            else
            {
                LastBuildMs = result.BuildMs;
                BuildCount++;
            }
            LastRenderMs = result.RenderMs;
            Swap(result);
        }

        private void Swap(Result result)
        {
            // 今の窓は後ろへ回し、新しい窓を前で 0 から濃くする。後ろの窓は前が濃くなりきったら消す
            DestroySlotTexture(_back);
            Slot old = _back;
            _back = _front;
            _front = old;
            _back.Detail = null;
            _front.Detail = result.Detail;
            _front.Level = result.Level;
            _front.Style = result.Style;
            _front.Alpha = 0f;
            _front.Texture = RegionMapTexture.Create(result.Pixels, result.Detail.Data.Resolution);
            _front.Image.texture = _front.Texture;
            RegionDetail d = result.Detail;
            RectTransform rect = _front.Image.rectTransform;
            rect.anchorMin = new Vector2(d.OriginMapX / RegionData.MapUnits, 1f - (d.OriginMapY + d.SizeMap) / RegionData.MapUnits);
            rect.anchorMax = new Vector2((d.OriginMapX + d.SizeMap) / RegionData.MapUnits, 1f - d.OriginMapY / RegionData.MapUnits);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _front.Image.transform.SetAsLastSibling();
        }

        private void UpdateFade(bool show, float deltaTime)
        {
            float step = FadeSeconds > 0f ? deltaTime / FadeSeconds : 1f;
            float target = show ? 1f : 0f;
            if (_front.Texture != null) _front.Alpha = Mathf.MoveTowards(_front.Alpha, target, step);
            if (_back.Texture != null)
            {
                // 前が濃くなりきるまで後ろは出したまま。細部を出さない時は両方いっしょに薄くする
                if (!show) _back.Alpha = Mathf.MoveTowards(_back.Alpha, 0f, step);
                else _back.Alpha = 1f;
                if (show && _front.Alpha >= 1f) DestroySlotTexture(_back);
            }
            Apply(_front);
            Apply(_back);
        }

        private static void Apply(Slot slot)
        {
            slot.Image.color = new Color(1f, 1f, 1f, slot.Alpha);
            slot.Image.enabled = slot.Texture != null && slot.Alpha > 0.001f;
        }

        private static void DestroySlotTexture(Slot slot)
        {
            if (slot.Texture != null) UnityEngine.Object.Destroy(slot.Texture);
            slot.Texture = null;
            slot.Image.texture = null;
            slot.Image.enabled = false;
            slot.Alpha = 0f;
            slot.Detail = null;
        }

        /// <summary>窓の中の地点の標高（水深は負）。窓が出ていない所では false。</summary>
        public bool TryHeightAt(float mapX, float mapY, out float height)
        {
            height = 0f;
            RegionDetail d = Current;
            if (d == null || _front.Alpha < 0.5f || !d.Contains(mapX, mapY)) return false;
            int n = d.Data.Resolution;
            int x = Mathf.Clamp((int)d.LocalX(mapX), 0, n - 1);
            int y = Mathf.Clamp((int)d.LocalY(mapY), 0, n - 1);
            height = d.Data.Height[x + y * n];
            return true;
        }

        /// <summary>地域が変わる時。窓を消し、作っている最中の結果は捨てる。</summary>
        public void Reset()
        {
            _generation++;
            DestroySlotTexture(_front);
            DestroySlotTexture(_back);
            _front.Detail = null;
            _wantLevel = int.MinValue;
            _failedLevel = int.MinValue;
            _failure = string.Empty;
            Phase = RegionDetailPhase.Off;
        }

        public void Dispose()
        {
            Reset();
        }
    }
}
