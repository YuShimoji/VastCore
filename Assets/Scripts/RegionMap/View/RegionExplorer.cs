using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 自動生成した地域の鳥瞰図を画面全体に表示し、位置を指定してサーチする探索画面。
    /// 情報とボタンは、半透明の小さな部品として地図の上に重ねる。
    /// 空のシーンにこのコンポーネントを 1 つ置けば動く（画面部品は起動時に組み立てる）。
    /// 操作: 地図を左クリックでカーソル移動、サーチボタンか Space でサーチ、ホイールで拡大・縮小、右ドラッグで移動。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RegionExplorer : MonoBehaviour
    {
        // 探索画面の呼び名は、規則から作った名前（記号名・土地の名・古い呼び名）を既定にする
        [SerializeField] private RegionSpec _spec = new RegionSpec { NameStyle = RegionNameStyle.Systematic };
        [SerializeField] private RegionRenderOptions _render = new RegionRenderOptions();

        [Tooltip("オンにすると、サーチしなくてもカーソル位置の反応を常に表示する。")]
        [SerializeField] private bool _liveSignal;

        [Tooltip("雲が地図の幅を横切るのにかかる秒数。0 にすると雲は動かない。")]
        [SerializeField] private float _cloudCrossSeconds = 420f;

        // 寸法は Canvas の単位（高さ 1080 を基準に画面へ合わせる）
        private const float Margin = 28f;
        private const float PartWidth = 392f;
        private const float PartGap = 10f;
        private const float PartPad = 18f;
        private const float PartInner = PartWidth - PartPad * 2f;
        // 左の部品が覆う幅。地図の西端は、この幅だけ画面の内側へ引き出せる
        private const float LeftInset = Margin + PartWidth + Margin;
        private const float ToolHeight = 42f;
        private const float ListHeader = 44f;
        private const float ListRow = 27f;
        // 一覧の行に由来の小さな行を出す時の高さ。行が多くて入らない時は 1 行（ListRow）へ戻す
        private const float ListRowWithGloss = 42f;
        private const float CardWidth = 380f;
        private const float CardHeight = 218f;
        // 細部の地形（RegionDetailLayer）へ切り替わる拡大率から、窓が地図の幅 4 km になる 64 倍まで拡大できる
        private const float MaxZoom = 64f;
        private const float DetailBadgeWidth = 360f;
        private const float DetailBadgeHeight = 66f;

        // 重ねる部品の面は半透明。下の地図と雲が透けて見える
        private static readonly Color PartColor = new Color(10f / 255f, 13f / 255f, 18f / 255f, 0.56f);
        private static readonly Color CardColor = new Color(10f / 255f, 13f / 255f, 18f / 255f, 0.64f);
        private static readonly Color LineColor = new Color(1f, 1f, 1f, 0.22f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Color TextColor = new Color32(240, 238, 232, 255);
        private static readonly Color MutedColor = new Color32(198, 204, 211, 255);
        private static readonly Color AccentColor = new Color32(232, 170, 70, 255);
        private static readonly Color AccentLightColor = new Color32(255, 207, 122, 255);
        private static readonly Color AccentTextColor = new Color32(40, 28, 8, 255);
        private static readonly Color OffColor = new Color(1f, 1f, 1f, 0.22f);
        private static readonly Color AreaNameColor = new Color(1f, 1f, 1f, 0.95f);
        // 川と湾の名前は、陸の名前と見分けられるよう水色へ寄せる
        private static readonly Color FeatureWaterColor = new Color(0.78f, 0.9f, 1f, 0.95f);

        // 雲の濃さの段（ボタンで順に切り替える）
        private static readonly float[] CloudOpacity = { 1f, 0.45f, 0f };
        private static readonly string[] CloudOpacityName = { "濃い", "薄い", "なし" };
        // 地名の出し方の段（ボタンで順に切り替える）
        private static readonly string[] AreaDisplayName = { "名前", "名前と境界", "なし" };

        private sealed class Generated
        {
            public RegionData Data;
            public byte[] Pixels;
            // 雲（低い塊の雲・高い筋雲）と影の層。地域が変わらない描き直しでは null（前の層をそのまま使う）
            public byte[] CloudPixels;
            public byte[] HighCloudPixels;
            public byte[] ShadowPixels;
            public int CloudSize;
            // エリアの境界線。地域が変わらない描き直しでは null
            public byte[] AreaPixels;
        }

        private Task<Generated> _task;
        private RegionData _data;
        private ExplorationSession _session;
        private Texture2D _texture;
        private Texture2D _cloudTexture;
        private Texture2D _highCloudTexture;
        private Texture2D _shadowTexture;
        private Texture2D _areaTexture;
        private Font _font;
        private Font _numberFont;
        private Sprite _ring;
        private Sprite _markRing;
        private Sprite _dot;
        private Sprite _crosshair;
        // 外から差し替えた絵を使っているか（RegionMapSkin）。差し替えた絵は元の色のまま出し、コードで作った絵用の色付けと縁取りは付けない
        private bool _skinCrosshair;
        private bool _skinSearchRing;
        private bool _skinFoundRing;
        private bool _skinFoundDot;

        private RectTransform _viewRect;
        private RectTransform _content;
        private RawImage _map;
        private RegionCloudLayer _clouds;
        private RegionCloudLayer _highClouds;
        private RegionCloudLayer _cloudShadows;
        private RegionDetailLayer _detail;
        private Text _zoomText;
        private Text _detailText;
        private RectTransform _detailBar;
        private RawImage _areaLines;
        private RectTransform _areaLabelRoot;
        private readonly List<RectTransform> _areaLabels = new List<RectTransform>();
        private readonly List<RectTransform> _featureLabels = new List<RectTransform>();
        private readonly List<RegionFeature> _featureLabelOwners = new List<RegionFeature>();
        private readonly List<Text> _listGloss = new List<Text>();
        private RectTransform _cursor;
        private RectTransform _range;
        private RectTransform _markerRoot;
        private readonly List<RectTransform> _markers = new List<RectTransform>();
        private readonly List<RegionSite> _markerSites = new List<RegionSite>();

        private Text _title;
        private Text _subtitle;
        private Text _positionLabel;
        private Text _coordinateX;
        private Text _coordinateY;
        private Text _groundLabel;
        private Text _groundValue;
        private Text _biomeValue;
        private Text _areaValue;
        private Text _signalText;
        private Image[] _signalBars;
        private RectTransform _listPart;
        private RectTransform _listRoot;
        private float _listTop;
        private readonly List<RectTransform> _listRows = new List<RectTransform>();
        private Text _listCount;
        private Text _searchCount;
        private Text _message;
        private CanvasGroup _messageGroup;
        private float _messageHideAt;
        private Text _styleLabel;
        private Text _cloudLabel;
        private Text _areaLabel;
        private Button _searchButton;
        private Button _regionButton;
        private Button _styleButton;
        private Button _cloudButton;
        private Button _areaButton;

        // 発見した地点のカード（印のそばに出る）
        private RectTransform _card;
        private RectTransform _cardLine;
        private Text _cardEyebrow;
        private Text _cardTitle;
        private Text _cardAlias;
        private Text _cardOrigin;
        private Text _cardKind;
        private Text _cardPosition;
        private Text _cardHeight;
        private RegionSite _cardSite;

        private int _cloudLevel;
        private int _areaLevel;
        private float _cloudTime;
        private float _cloudOpacity;
        private Vector2 _cloudDrift;
        // 生成中の工程名。別スレッドから書き、Update で読んで知らせに出す
        private volatile string _phase;
        private string _phaseShown;
        private string _phaseMessage = string.Empty;

        // _zoom が 1 の時、地図の幅が画面の幅に合う。_center は画面の中央に来る地図上の位置（0〜1、v は北が正）
        private float _zoom = 1f;
        private Vector2 _center = new Vector2(0.5f, 0.5f);
        private Vector2 _cursorMap = new Vector2(50f, 50f);
        private Vector2 _hoverMap;
        private bool _hovering;
        private bool _panning;
        private Vector2 _panLastLocal;
        private SearchSignal _lastSignal = SearchSignal.None;

        public RegionData Region
        {
            get { return _data; }
        }

        public ExplorationSession Session
        {
            get { return _session; }
        }

        /// <summary>地域の生成と描画が終わり、操作できる状態か。</summary>
        public bool IsReady
        {
            get { return _data != null && _task == null; }
        }

        public Vector2 CursorMapPosition
        {
            get { return _cursorMap; }
        }

        /// <summary>低い雲の層が一様な流れで動いた量（地図の幅を 1 とした割合。1 で一周して 0 へ戻る）。場所ごとの渦のずれは含まない。</summary>
        public Vector2 CloudDrift { get { return _cloudDrift; } }

        /// <summary>地名の出し方の段（0 名前、1 名前と境界、2 なし）。</summary>
        public int AreaDisplay
        {
            get { return _areaLevel; }
        }

        /// <summary>雲の濃さの段（0 濃い、1 薄い、2 なし）。</summary>
        public int CloudLevel
        {
            get { return _cloudLevel; }
        }

        /// <summary>表示の倍率。1 で地図の幅が画面の幅に合う。</summary>
        public float Zoom
        {
            get { return _zoom; }
        }

        /// <summary>拡大した時に出す細部の地形の層。状態と作成時間を読める。</summary>
        public RegionDetailLayer Detail
        {
            get { return _detail; }
        }

        /// <summary>外から差し替えた絵を使っている部品の名前（RegionMapSkin の名前をコンマで区切る）。差し替えが無ければ空。</summary>
        public string CustomSkin
        {
            get
            {
                var used = new List<string>();
                if (_skinCrosshair) used.Add(RegionMapSkin.Crosshair);
                if (_skinSearchRing) used.Add(RegionMapSkin.SearchRing);
                if (_skinFoundRing) used.Add(RegionMapSkin.FoundRing);
                if (_skinFoundDot) used.Add(RegionMapSkin.FoundDot);
                return string.Join(",", used.ToArray());
            }
        }

        private void Awake()
        {
            _font = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI", "Meiryo UI", "Meiryo", "MS Gothic", "Arial" }, 28);
            // 数字は幅の揃った書体で出す（座標が動いても桁が揺れにくい）
            _numberFont = Font.CreateDynamicFontFromOSFont(new[] { "Bahnschrift", "Consolas", "Segoe UI", "Arial" }, 28);
            // Resources/RegionMapSkin/ に画像があればそれを使い、無ければコードで作る既定の絵にする
            _ring = RegionMapSkin.Load(RegionMapSkin.SearchRing);
            _skinSearchRing = _ring != null;
            _markRing = RegionMapSkin.Load(RegionMapSkin.FoundRing);
            _skinFoundRing = _markRing != null;
            if (_markRing == null) _markRing = RegionMapTexture.CreateRingSprite(96, 6f, false);
            _dot = RegionMapSkin.Load(RegionMapSkin.FoundDot);
            _skinFoundDot = _dot != null;
            if (_dot == null) _dot = RegionMapTexture.CreateRingSprite(32, 0f, true);
            _crosshair = RegionMapSkin.Load(RegionMapSkin.Crosshair);
            _skinCrosshair = _crosshair != null;
            // 十字の腕は、発見した地点の輪（半径 27）の外に出す。発見した地点に重ねても、輪と腕が重ならない
            if (_crosshair == null) _crosshair = RegionMapTexture.CreateCrosshairSprite(176, 68f, 5f, 2.5f);
            EnsureCamera();
            EnsureEventSystem();
            BuildScreen();
        }

        private void Start()
        {
            BeginGenerate("地域を生成しています…");
        }

        private void OnDestroy()
        {
            if (_texture != null) Destroy(_texture);
            if (_cloudTexture != null) Destroy(_cloudTexture);
            if (_highCloudTexture != null) Destroy(_highCloudTexture);
            if (_shadowTexture != null) Destroy(_shadowTexture);
            if (_areaTexture != null) Destroy(_areaTexture);
            if (_detail != null) _detail.Dispose();
        }

        private void Update()
        {
            FadeMessage();
            // クリックしたボタンが選択されたままだと、Space と Enter がそのボタンの「決定」にもなり、
            // 下の HandleKeys と合わせて 1 回の押下で 2 回動く。選択は毎フレーム外す
            EventSystem events = EventSystem.current;
            if (events != null && events.currentSelectedGameObject != null) events.SetSelectedGameObject(null);

            if (_task != null)
            {
                if (!_task.IsCompleted)
                {
                    // 生成は別スレッドで進む。今どの工程かを、待っている間の知らせに出す
                    string phase = _phase;
                    if (!ReferenceEquals(phase, _phaseShown))
                    {
                        _phaseShown = phase;
                        if (!string.IsNullOrEmpty(phase)) ShowMessage(_phaseMessage + "（" + phase + "）", 0f);
                    }
                    return;
                }
                Task<Generated> finished = _task;
                _task = null;
                if (finished.IsFaulted)
                {
                    Exception error = finished.Exception != null ? finished.Exception.GetBaseException() : null;
                    ShowMessage("地域を生成できませんでした: " + (error != null ? error.Message : "原因不明"), 0f);
                    SetButtons(true);
                    return;
                }
                ApplyGenerated(finished.Result);
            }

            if (_data == null) return;
            HandlePointer();
            HandleKeys();
            RefreshOverlay();
            UpdateDetail();
        }

        // ---- 外から呼べる操作（ボタンとテストの両方が使う） ----

        /// <summary>カーソルを地図座標（0〜100）へ動かす。画面の外や部品の下になる時は、見える位置まで地図を送る。</summary>
        public void MoveCursor(float mapX, float mapY)
        {
            _cursorMap = new Vector2(Mathf.Clamp(mapX, 0f, RegionData.MapUnits), Mathf.Clamp(mapY, 0f, RegionData.MapUnits));
            EnsureVisible(_cursorMap);
            RefreshReadout();
        }

        /// <summary>カーソル位置でサーチする。</summary>
        public SearchResult SearchAtCursor()
        {
            if (!IsReady) return new SearchResult();
            HideCard();
            SearchResult result = _session.Search(_cursorMap.x, _cursorMap.y);
            _lastSignal = result.Signal;
            switch (result.Signal)
            {
                case SearchSignal.Found:
                    AddDiscovered(result.Site);
                    ShowCard(result.Site);
                    HideMessage();
                    break;
                case SearchSignal.Strong:
                    ShowMessage("強い反応。すぐ近くに何かある", 4f);
                    break;
                case SearchSignal.Faint:
                    ShowMessage("弱い反応。この辺りのどこかにある", 4f);
                    break;
                default:
                    ShowMessage(result.Distance < 0f ? "すべての地点を発見済み" : "反応なし", 3f);
                    break;
            }
            RefreshReadout();
            return result;
        }

        /// <summary>seed を 1 つ進めて別の地域を作る。</summary>
        public void NextRegion()
        {
            if (_task != null) return;
            _spec.Seed++;
            BeginGenerate("別の地域を生成しています…");
        }

        /// <summary>同じ地域を次の描き方で描き直す。</summary>
        public void NextStyle()
        {
            if (!IsReady) return;
            int next = ((int)_render.Style + 1) % Enum.GetValues(typeof(RegionMapStyle)).Length;
            _render.Style = (RegionMapStyle)next;
            _styleLabel.text = "描き方：" + StyleName(_render.Style);
            RegionData data = _data;
            RegionRenderOptions options = CloneRender();
            options.CloudCover = 0f;
            ShowMessage("描き直しています…", 0f);
            SetButtons(false);
            _task = Task.Run(() => new Generated { Data = data, Pixels = RegionMapRenderer.Render(data, options) });
        }

        /// <summary>雲の濃さを次の段へ切り替える（濃い → 薄い → なし）。描き直しは要らない。</summary>
        public void NextCloudOpacity()
        {
            _cloudLevel = (_cloudLevel + 1) % CloudOpacity.Length;
            RefreshCloudLayers();
            if (IsReady) ShowMessage("雲：" + CloudOpacityName[_cloudLevel], 3f);
        }

        /// <summary>地名の出し方を次の段へ切り替える（名前 → 名前と境界 → なし）。</summary>
        public void NextAreaDisplay()
        {
            _areaLevel = (_areaLevel + 1) % AreaDisplayName.Length;
            RefreshAreaLayers();
            if (IsReady) ShowMessage("地名：" + AreaDisplayName[_areaLevel], 3f);
        }

        /// <summary>地域の全体が画面に入る倍率まで縮める。</summary>
        public void ShowWholeRegion()
        {
            _zoom = MinZoom();
            _center = new Vector2(0.5f, 0.5f);
            ClampView();
        }

        /// <summary>地図の幅を画面の幅に合わせる（起動時の見え方）。カーソルが見える位置へ地図を送る。</summary>
        public void ShowFullWidth()
        {
            _zoom = 1f;
            _center = new Vector2(0.5f, 1f - _cursorMap.y / RegionData.MapUnits);
            ClampView();
            EnsureVisible(_cursorMap);
        }

        /// <summary>拡大率と、画面の中央に来る地図座標（0〜100）を決める（確認用の入口）。</summary>
        public void SetView(float zoom, float mapX, float mapY)
        {
            _zoom = Mathf.Clamp(zoom, MinZoom(), MaxZoom);
            _center = new Vector2(mapX / RegionData.MapUnits, 1f - mapY / RegionData.MapUnits);
            ClampView();
        }

        /// <summary>画面の下に知らせを出す。seconds が 0 以下なら、次の知らせまで出したままにする。</summary>
        private void ShowMessage(string text, float seconds)
        {
            _message.text = text;
            _messageGroup.alpha = 1f;
            _messageHideAt = seconds > 0f ? Time.unscaledTime + seconds : -1f;
        }

        private void HideMessage()
        {
            _messageGroup.alpha = 0f;
            _messageHideAt = -1f;
        }

        /// <summary>時間が来た知らせを薄くして消す。出したままだと下の地図が読めない。</summary>
        private void FadeMessage()
        {
            if (_messageHideAt < 0f || _messageGroup.alpha <= 0f) return;
            float over = Time.unscaledTime - _messageHideAt;
            if (over <= 0f) return;
            _messageGroup.alpha = Mathf.Clamp01(1f - over / 0.5f);
        }

        // ---- 生成 ----

        private void BeginGenerate(string message)
        {
            RegionSpec spec = _spec.Clone();
            RegionRenderOptions options = CloneRender();
            // 雲は地図へ焼き込まず、上に重ねる層として別に作る（濃さの切替と、流れる動きのため）
            float cloudCover = options.CloudCover;
            options.CloudCover = 0f;
            ShowMessage(message, 0f);
            SetButtons(false);
            _phaseMessage = message.TrimEnd('…');
            _phase = null;
            _phaseShown = null;
            _task = Task.Run(() =>
            {
                // 雲の層は地形を使わないので、地域の生成と同時に作り始める
                int cloudSize = (spec.Resolution + 1) / 2;
                byte[] cloudPixels = null;
                byte[] highPixels = null;
                byte[] shadowPixels = null;
                Task clouds = Task.Run(() =>
                {
                    // 低い塊の雲と高い筋雲を別の層にする。別々の風で流すので、重なり方が時間で変わる。影は低い雲だけが落とす
                    float[] low;
                    float[] high;
                    byte[] unused;
                    RegionClouds.BuildLayerFields(cloudSize, spec.Seed, cloudCover, out low, out high);
                    RegionClouds.RenderLayers(low, cloudSize, out cloudPixels, out shadowPixels);
                    RegionClouds.RenderLayers(high, cloudSize, out highPixels, out unused);
                });
                RegionData data = RegionGenerator.Generate(spec, phase => _phase = phase);
                _phase = "地図を描く";
                byte[] pixels = RegionMapRenderer.Render(data, options);
                byte[] areaPixels = RegionAreas.RenderBoundaries(data);
                clouds.Wait();
                return new Generated
                {
                    Data = data,
                    Pixels = pixels,
                    CloudSize = cloudSize,
                    CloudPixels = cloudPixels,
                    HighCloudPixels = highPixels,
                    ShadowPixels = shadowPixels,
                    AreaPixels = areaPixels,
                };
            });
        }

        private RegionRenderOptions CloneRender()
        {
            return new RegionRenderOptions
            {
                Style = _render.Style,
                CloudCover = _render.CloudCover,
                Exaggeration = _render.Exaggeration,
                CastShadow = _render.CastShadow,
                SunAltitude = _render.SunAltitude,
                Rivers = _render.Rivers,
            };
        }

        private void ApplyGenerated(Generated generated)
        {
            bool sameRegion = ReferenceEquals(generated.Data, _data);
            _data = generated.Data;
            int size = _data.Resolution;
            if (_texture == null || _texture.width != size)
            {
                if (_texture != null) Destroy(_texture);
                _texture = RegionMapTexture.Create(generated.Pixels, size);
                _map.texture = _texture;
            }
            else
            {
                RegionMapTexture.Apply(_texture, generated.Pixels, size);
            }
            _map.enabled = true;

            if (generated.CloudPixels != null)
            {
                if (_cloudTexture != null) Destroy(_cloudTexture);
                if (_highCloudTexture != null) Destroy(_highCloudTexture);
                if (_shadowTexture != null) Destroy(_shadowTexture);
                _cloudTexture = RegionMapTexture.Create(generated.CloudPixels, generated.CloudSize, true);
                _highCloudTexture = RegionMapTexture.Create(generated.HighCloudPixels, generated.CloudSize, true);
                _shadowTexture = RegionMapTexture.Create(generated.ShadowPixels, generated.CloudSize, true);
                _clouds.Texture = _cloudTexture;
                _highClouds.Texture = _highCloudTexture;
                _cloudShadows.Texture = _shadowTexture;
                _cloudTime = 0f;
                _cloudDrift = Vector2.zero;
            }

            if (generated.AreaPixels != null)
            {
                if (_areaTexture != null) Destroy(_areaTexture);
                _areaTexture = RegionMapTexture.Create(generated.AreaPixels, size);
                _areaLines.texture = _areaTexture;
            }

            if (!sameRegion)
            {
                _session = new ExplorationSession(_data);
                _lastSignal = SearchSignal.None;
                _zoom = 1f;
                _center = new Vector2(0.5f, 0.5f);
                _cursorMap = new Vector2(50f, 50f);
                _detail.Reset();
                ClearDiscovered();
                BuildAreaLabels();
                ClampView();
                RegionFeature region = _data.Features.Find(f => f.Kind == RegionFeatureKind.Region);
                _title.text = (region != null && !string.IsNullOrEmpty(region.Name) ? region.Name : "地域") + "　seed " + _data.Spec.Seed;
                _subtitle.text = string.Format("{0:0} km 四方 ・ 陸 {1:0}% ・ 最高 {2} m", _data.SizeKm, _data.LandFraction * 100f, Meters(_data.MaxHeight));
                ShowMessage("地図をクリックして位置を決め、サーチで調べる", 8f);
            }
            else
            {
                ShowMessage("描き方：" + StyleName(_render.Style), 3f);
            }
            _cursor.gameObject.SetActive(true);
            _range.gameObject.SetActive(true);
            SetButtons(true);
            RefreshCloudLayers();
            RefreshAreaLayers();
            RefreshReadout();
        }

        // ---- 入力 ----

        private void HandlePointer()
        {
            Vector2 local;
            bool inside = RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewRect, Input.mousePosition, null, out local)
                && _viewRect.rect.Contains(local);
            // 部品とボタンの上では地図を操作しない（面の下の地図は、右ドラッグで引き出して触る）
            EventSystem events = EventSystem.current;
            bool free = inside && !(events != null && events.IsPointerOverGameObject());
            Vector2 uv = LocalToUv(local);
            _hovering = free && uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;

            if (_hovering)
            {
                _hoverMap = LocalToMap(local);
                // クリックした位置は必ず見えているので、地図は送らない
                if (Input.GetMouseButtonDown(0)) _cursorMap = _hoverMap;
            }

            if (free)
            {
                float wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.01f) ZoomAt(local, wheel > 0f ? 1.25f : 0.8f);
                if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                {
                    _panning = true;
                    _panLastLocal = local;
                }
            }

            if (_panning)
            {
                if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
                {
                    Vector2 delta = local - _panLastLocal;
                    _panLastLocal = local;
                    _center -= delta / MapSide();
                    ClampView();
                }
                else
                {
                    _panning = false;
                }
            }
            RefreshReadout();
        }

        private void HandleKeys()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) SearchAtCursor();
            if (Input.GetKeyDown(KeyCode.Escape)) HideCard();
            if (Input.GetKeyDown(KeyCode.Home))
            {
                if (_zoom > MinZoom() + 0.001f) ShowWholeRegion(); else ShowFullWidth();
            }

            // 矢印キーの 1 回分は、拡大しても画面上で同じ長さに近づける（細部へ拡大した画面で 1.3 km も飛ばない）
            float step = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 0.1f : 0.5f) / Mathf.Max(1f, _zoom / 4f);
            float dx = 0f;
            float dy = 0f;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) dx -= step;
            if (Input.GetKeyDown(KeyCode.RightArrow)) dx += step;
            if (Input.GetKeyDown(KeyCode.UpArrow)) dy -= step;
            if (Input.GetKeyDown(KeyCode.DownArrow)) dy += step;
            if (dx != 0f || dy != 0f) MoveCursor(_cursorMap.x + dx, _cursorMap.y + dy);
        }

        private void ZoomAt(Vector2 local, float factor)
        {
            Vector2 before = LocalToUv(local);
            _zoom = Mathf.Clamp(_zoom * factor, MinZoom(), MaxZoom);
            Vector2 after = LocalToUv(local);
            _center += before - after;
            ClampView();
        }

        // ---- 表示範囲（地図の板を、画面の中央を原点にして動かす） ----

        private Vector2 ViewSize()
        {
            Rect rect = _viewRect.rect;
            return rect.width > 1f && rect.height > 1f ? rect.size : new Vector2(1920f, 1080f);
        }

        /// <summary>画面上の地図の 1 辺（Canvas の単位）。</summary>
        private float MapSide()
        {
            return ViewSize().x * _zoom;
        }

        /// <summary>地域の全体が縦に収まる倍率。これより小さくは縮めない。</summary>
        private float MinZoom()
        {
            Vector2 view = ViewSize();
            return Mathf.Min(1f, view.y / view.x);
        }

        private void ClampView()
        {
            Vector2 view = ViewSize();
            float side = view.x * _zoom;

            // 横：地図の西端は、左の部品の右端までしか内側へ入れない。東端は画面の右端より内側へ入れない
            float open = view.x - LeftInset;
            float west = -_center.x * side;
            if (side <= open) west = -view.x * 0.5f + LeftInset + (open - side) * 0.5f;
            else west = Mathf.Clamp(west, view.x * 0.5f - side, -view.x * 0.5f + LeftInset);
            _center.x = -west / side;

            // 縦：地図が画面より低い時は中央に置く
            if (side <= view.y)
            {
                _center.y = 0.5f;
            }
            else
            {
                float half = view.y * 0.5f / side;
                _center.y = Mathf.Clamp(_center.y, half, 1f - half);
            }
        }

        /// <summary>地図上の位置が、画面の端や左の部品の下にある時、見える位置まで地図を送る。</summary>
        private void EnsureVisible(Vector2 map)
        {
            if (_data == null) return;
            Vector2 view = ViewSize();
            Vector2 local = MapToLocal(map);
            float left = -view.x * 0.5f + LeftInset + 60f;
            float right = view.x * 0.5f - 140f;
            float bottom = -view.y * 0.5f + 190f;
            float top = view.y * 0.5f - 150f;
            Vector2 shift = Vector2.zero;
            if (local.x < left) shift.x = local.x - left;
            else if (local.x > right) shift.x = local.x - right;
            if (local.y < bottom) shift.y = local.y - bottom;
            else if (local.y > top) shift.y = local.y - top;
            if (shift == Vector2.zero) return;
            _center += shift / MapSide();
            ClampView();
        }

        // ---- 座標の換算（地図座標は Y が南向き、テクスチャは V が上向き） ----

        private Vector2 LocalToUv(Vector2 local)
        {
            float side = MapSide();
            return new Vector2(_center.x + local.x / side, _center.y + local.y / side);
        }

        private Vector2 LocalToMap(Vector2 local)
        {
            Vector2 uv = LocalToUv(local);
            return new Vector2(Mathf.Clamp01(uv.x) * RegionData.MapUnits, Mathf.Clamp01(1f - uv.y) * RegionData.MapUnits);
        }

        private Vector2 MapToLocal(Vector2 map)
        {
            float side = MapSide();
            float u = map.x / RegionData.MapUnits;
            float v = 1f - map.y / RegionData.MapUnits;
            return new Vector2((u - _center.x) * side, (v - _center.y) * side);
        }

        // ---- 細部の地形 ----

        /// <summary>見ている範囲を層へ渡し、札を更新する。</summary>
        private void UpdateDetail()
        {
            Vector2 view = ViewSize();
            float side = MapSide();
            float centerX = _center.x * RegionData.MapUnits;
            float centerY = (1f - _center.y) * RegionData.MapUnits;
            float halfWidth = view.x / side * 0.5f * RegionData.MapUnits;
            float halfHeight = view.y / side * 0.5f * RegionData.MapUnits;
            _detail.Tick(_data, _render, centerX, centerY, halfWidth, halfHeight, _zoom, Time.unscaledDeltaTime);
            RefreshDetailBadge();
        }

        private void RefreshDetailBadge()
        {
            _zoomText.text = "拡大 ×" + _zoom.ToString(_zoom < 10f ? "0.0" : "0", CultureInfo.InvariantCulture);
            RegionDetail current = _detail.Current;
            RegionDetailPhase phase = _detail.Phase;
            bool building = phase == RegionDetailPhase.Building || phase == RegionDetailPhase.Waiting;
            string text;
            if (building)
            {
                text = "細部を作成中…" + (_detail.BuildSeconds > 0f ? " " + _detail.BuildSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " 秒" : string.Empty);
            }
            else if (phase == RegionDetailPhase.Failed)
            {
                text = "細部を作成できませんでした";
            }
            else if (phase == RegionDetailPhase.Ready && current != null)
            {
                text = "地形：細部（" + current.Data.CellMeters.ToString("0", CultureInfo.InvariantCulture) + " m／セル）";
            }
            else
            {
                text = "地形：広域（" + _data.CellMeters.ToString("0", CultureInfo.InvariantCulture) + " m／セル）"
                    + (_detail.Enabled && _zoom < RegionDetailLayer.MinZoom ? " ・ ×" + RegionDetailLayer.MinZoom.ToString("0", CultureInfo.InvariantCulture) + " から細部" : string.Empty);
            }
            _detailText.text = text;
            _detailText.color = building ? AccentLightColor : MutedColor;

            GameObject track = _detailBar.parent.gameObject;
            if (track.activeSelf != building) track.SetActive(building);
            if (building)
            {
                // 帯が左右を行き来する（作成の進み具合は分からないので、動いていることだけを見せる）
                float width = DetailBadgeWidth - PartPad * 2f;
                float travel = width - 96f;
                float t = Mathf.PingPong(Time.unscaledTime * 0.9f, 1f);
                _detailBar.anchoredPosition = new Vector2(travel * t, 0f);
            }
        }

        // ---- 表示の更新 ----

        private void RefreshOverlay()
        {
            float side = MapSide();
            _content.sizeDelta = new Vector2(side, side);
            _content.anchoredPosition = new Vector2((0.5f - _center.x) * side, (0.5f - _center.y) * side);

            // 雲は 2 層を別々の風で流し、場所ごとに渦でずらす（RegionCloudFlow）。
            // 影は低い雲と同じ動きで、雲より南東（u は東、v は北が正なので +u, -v）に落ちる
            if (_cloudCrossSeconds > 0f) _cloudTime += Time.deltaTime;
            if (_cloudOpacity > 0f)
            {
                var shadowShift = new Vector2(RegionClouds.ShadowOffset, -RegionClouds.ShadowOffset);
                _cloudShadows.SetFlow(RegionCloudFlow.Low, _cloudTime, _cloudCrossSeconds, shadowShift, _cloudOpacity);
                _clouds.SetFlow(RegionCloudFlow.Low, _cloudTime, _cloudCrossSeconds, Vector2.zero, _cloudOpacity);
                _highClouds.SetFlow(RegionCloudFlow.High, _cloudTime, _cloudCrossSeconds, Vector2.zero, _cloudOpacity);
            }
            float driftU;
            float driftV;
            RegionCloudFlow.Drift(RegionCloudFlow.Low, _cloudTime, _cloudCrossSeconds, out driftU, out driftV);
            _cloudDrift = new Vector2(driftU, driftV);

            for (int a = 0; a < _areaLabels.Count; a++)
            {
                RegionArea area = _data.Areas[a];
                _areaLabels[a].anchoredPosition = MapToLocal(new Vector2(area.LabelX, area.LabelY));
            }
            for (int f = 0; f < _featureLabels.Count; f++)
            {
                RegionFeature feature = _featureLabelOwners[f];
                // 峰は頂の印に重ならないよう、名前を少し下へずらす
                Vector2 at = MapToLocal(new Vector2(feature.MapX, feature.MapY));
                _featureLabels[f].anchoredPosition = feature.Kind == RegionFeatureKind.Peak ? at + new Vector2(0f, -16f) : at;
            }

            _cursor.anchoredPosition = MapToLocal(_cursorMap);
            _range.anchoredPosition = _cursor.anchoredPosition;
            float diameter = _session.FoundRadius / RegionData.MapUnits * side * 2f;
            _range.sizeDelta = new Vector2(diameter, diameter);
            for (int m = 0; m < _markers.Count; m++)
            {
                RegionSite site = _markerSites[m];
                _markers[m].anchoredPosition = MapToLocal(new Vector2(site.MapX, site.MapY));
            }
            PlaceCard();
            RefreshLabelCover();
        }

        /// <summary>
        /// 読めなくなる地名を隠す。カードの下に入る地名（カードは半透明なので、字が透けて重なる）、
        /// カードを出している地点の輪に重なる地名、下端の案内の帯に掛かる地名、エリアの名前や先に置いた名前に重なる川・峰・湾の名前。
        /// 拡大して離れれば、また出る。
        /// </summary>
        private void RefreshLabelCover()
        {
            bool card = _cardSite != null;
            Vector2 corner = _card.anchoredPosition;
            Vector2 ring = card ? MapToLocal(new Vector2(_cardSite.MapX, _cardSite.MapY)) : Vector2.zero;
            float floor = -ViewSize().y * 0.5f + Margin + ToolHeight + 18f;
            for (int a = 0; a < _areaLabels.Count; a++)
            {
                Vector2 at = _areaLabels[a].anchoredPosition;
                _areaLabels[a].gameObject.SetActive(at.y >= floor && !(card && (UnderCard(at, corner, 76f, 14f) || Near(at, ring, 96f, 44f))));
            }
            for (int f = 0; f < _featureLabels.Count; f++)
            {
                Vector2 at = _featureLabels[f].anchoredPosition;
                bool hidden = at.y < floor || (card && (UnderCard(at, corner, 60f, 12f) || Near(at, ring, 84f, 42f)));
                for (int a = 0; a < _areaLabels.Count && !hidden; a++)
                    hidden = _areaLabels[a].gameObject.activeSelf && Near(at, _areaLabels[a].anchoredPosition, 132f, 24f);
                for (int g = 0; g < f && !hidden; g++)
                    hidden = _featureLabels[g].gameObject.activeSelf && Near(at, _featureLabels[g].anchoredPosition, 104f, 18f);
                _featureLabels[f].gameObject.SetActive(!hidden);
            }
        }

        /// <summary>中心が at で、半分の幅と高さが halfWidth・halfHeight の字が、カード（左上の角が corner）に掛かるか。</summary>
        private static bool UnderCard(Vector2 at, Vector2 corner, float halfWidth, float halfHeight)
        {
            return at.x + halfWidth > corner.x && at.x - halfWidth < corner.x + CardWidth
                && at.y - halfHeight < corner.y && at.y + halfHeight > corner.y - CardHeight;
        }

        private static bool Near(Vector2 a, Vector2 b, float width, float height)
        {
            return Mathf.Abs(a.x - b.x) < width && Mathf.Abs(a.y - b.y) < height;
        }

        /// <summary>雲と影の層の見え方を、描き方と濃さの段に合わせる。雲があるのは衛星写真の時だけ。</summary>
        private void RefreshCloudLayers()
        {
            bool satellite = _render.Style == RegionMapStyle.Satellite;
            float opacity = satellite && _cloudTexture != null ? CloudOpacity[_cloudLevel] : 0f;
            _cloudOpacity = opacity;
            _clouds.enabled = opacity > 0f;
            _highClouds.enabled = opacity > 0f;
            _cloudShadows.enabled = opacity > 0f;
            _cloudLabel.text = satellite ? "雲：" + CloudOpacityName[_cloudLevel] : "雲：衛星写真のみ";
            _cloudButton.interactable = satellite && _task == null;
        }

        /// <summary>エリアの名前と境界線の見え方を、地名の段に合わせる。</summary>
        private void RefreshAreaLayers()
        {
            bool names = _areaLevel != 2 && _data != null;
            _areaLabelRoot.gameObject.SetActive(names);
            _areaLines.enabled = _areaLevel == 1 && _areaTexture != null;
            _areaLabel.text = "地名：" + AreaDisplayName[_areaLevel];
        }

        /// <summary>エリアの名前を、各エリアの中ほど（境界からいちばん離れた所）に置く。</summary>
        private void BuildAreaLabels()
        {
            for (int a = 0; a < _areaLabels.Count; a++) Destroy(_areaLabels[a].gameObject);
            _areaLabels.Clear();
            for (int a = 0; a < _data.Areas.Count; a++)
            {
                RegionArea area = _data.Areas[a];
                // 島のような狭いエリアは小さな字にする
                int size = area.AreaKm2 >= 600f ? 21 : 17;
                Text label = CreateText(_areaLabelRoot, "Area " + area.Id, area.Name, size, AreaNameColor, TextAnchor.MiddleCenter, _font);
                Outline edge = label.gameObject.AddComponent<Outline>();
                // 白い雲の上にも名前が乗るので、暗い縁を濃く太めにする
                edge.effectColor = new Color(0f, 0f, 0f, 0.9f);
                edge.effectDistance = new Vector2(1.5f, -1.5f);
                label.rectTransform.sizeDelta = new Vector2(240f, 30f);
                _areaLabels.Add(label.rectTransform);
            }

            // 川・峰・湾の名前（規則から名前を付けた時だけある）。エリアの名前より小さく、記号名だけの物はさらに薄くする
            for (int f = 0; f < _featureLabels.Count; f++) Destroy(_featureLabels[f].gameObject);
            _featureLabels.Clear();
            _featureLabelOwners.Clear();
            for (int f = 0; f < _data.Features.Count; f++)
            {
                RegionFeature feature = _data.Features[f];
                if (feature.Kind == RegionFeatureKind.Region || string.IsNullOrEmpty(feature.Name)) continue;
                bool codeOnly = feature.Names.Count > 0 && feature.Names[0].Kind == RegionNameKind.Designation;
                Color color = feature.Kind == RegionFeatureKind.Peak ? AreaNameColor : FeatureWaterColor;
                if (codeOnly) color.a = 0.62f;
                Text label = CreateText(_areaLabelRoot, feature.Kind + " " + feature.Id, feature.Name, codeOnly ? 13 : 15, color, TextAnchor.MiddleCenter, codeOnly ? _numberFont : _font);
                Outline edge = label.gameObject.AddComponent<Outline>();
                edge.effectColor = new Color(0f, 0f, 0f, 0.9f);
                edge.effectDistance = new Vector2(1.2f, -1.2f);
                label.rectTransform.sizeDelta = new Vector2(200f, 24f);
                _featureLabels.Add(label.rectTransform);
                _featureLabelOwners.Add(feature);
            }
        }

        private void RefreshReadout()
        {
            if (_data == null) return;
            Vector2 at = _hovering ? _hoverMap : _cursorMap;
            _positionLabel.text = _hovering ? "ポインター位置" : "カーソル位置";
            _coordinateX.text = at.x.ToString("00.0", CultureInfo.InvariantCulture);
            _coordinateY.text = at.y.ToString("00.0", CultureInfo.InvariantCulture);
            float height = _data.HeightAtMap(at.x, at.y);
            float fine;
            if (_detail.TryHeightAt(at.x, at.y, out fine)) height = fine;
            _groundLabel.text = height > 0f ? "標高" : "水深";
            _groundValue.text = Meters(Mathf.Abs(height)) + " m";
            _biomeValue.text = BiomeName(_data.BiomeAtMap(at.x, at.y));
            _areaValue.text = _data.PlaceNameAtMap(at.x, at.y);

            SearchSignal signal = _liveSignal ? _session.Sense(_cursorMap.x, _cursorMap.y).Signal : _lastSignal;
            int level = (int)signal;
            for (int b = 0; b < _signalBars.Length; b++) _signalBars[b].color = b < level ? AccentColor : OffColor;
            _signalText.text = SignalName(signal);
            _listCount.text = _session.Discovered.Count + " / " + _session.TotalSites;
            _searchCount.text = "サーチ " + _session.SearchCount + " 回";
        }

        private static string Meters(float value)
        {
            return Mathf.RoundToInt(value).ToString("#,0", CultureInfo.InvariantCulture);
        }

        private void AddDiscovered(RegionSite site)
        {
            // 印は橙の輪と中心の点。輪には暗い縁を付け、雲や雪の上でも読めるようにする
            // 差し替えた絵は元の色のまま出し、縁取りも付けない（縁は絵の側で決める）
            Image markerImage = CreateImage(_markerRoot, "Marker " + site.Id, _skinFoundDot ? Color.white : AccentColor, _dot);
            RectTransform marker = markerImage.rectTransform;
            marker.sizeDelta = new Vector2(14f, 14f);
            markerImage.preserveAspect = _skinFoundDot;
            if (!_skinFoundDot) AddDarkEdge(marker.gameObject);
            Image haloImage = CreateImage(marker, "Halo", _skinFoundRing ? Color.white : AccentColor, _markRing);
            RectTransform halo = haloImage.rectTransform;
            halo.sizeDelta = new Vector2(54f, 54f);
            haloImage.preserveAspect = _skinFoundRing;
            if (!_skinFoundRing) AddDarkEdge(halo.gameObject);
            _markers.Add(marker);
            _markerSites.Add(site);

            int number = _markers.Count;
            Button entry = CreateRowButton(_listRoot, "Row " + number);
            RectTransform row = (RectTransform)entry.transform;
            Text index = CreateText(row, "Number", number.ToString("00", CultureInfo.InvariantCulture), 18, AccentLightColor, TextAnchor.MiddleLeft, _numberFont);
            Place(index.rectTransform, 0f, 0f, 30f, ListRow - 1f);
            Text name = CreateText(row, "Name", site.Name, 18, TextColor, TextAnchor.MiddleLeft, _font);
            Place(name.rectTransform, 34f, 0f, PartInner - 34f - 112f, ListRow - 1f);
            // 名前の下に、由来を小さく出す（規則から作った名前の時だけ中身がある）
            Text gloss = CreateText(row, "Origin", site.Names.Count > 0 ? site.Names[0].Gloss : "", 13, MutedColor, TextAnchor.UpperLeft, _font, false);
            Place(gloss.rectTransform, 34f, -(ListRow - 3f), PartInner - 34f, 17f);
            gloss.horizontalOverflow = HorizontalWrapMode.Wrap;
            gloss.verticalOverflow = VerticalWrapMode.Truncate;
            gloss.resizeTextForBestFit = true;
            gloss.resizeTextMinSize = 10;
            gloss.resizeTextMaxSize = 13;
            _listGloss.Add(gloss);
            // 「南東の第二平野・川辺」のような長い名前は、座標に重ねず、枠の幅に収まるまで字を小さくする
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 12;
            name.resizeTextMaxSize = 18;
            string where = site.MapX.ToString("0.0", CultureInfo.InvariantCulture) + " , " + site.MapY.ToString("0.0", CultureInfo.InvariantCulture);
            Text position = CreateText(row, "Position", where, 16, MutedColor, TextAnchor.MiddleRight, _numberFont);
            Place(position.rectTransform, PartInner - 110f, 0f, 110f, ListRow - 1f);
            entry.onClick.AddListener(() => FocusSite(site));
            _listRows.Add(row);
            LayoutList();
        }

        /// <summary>発見した地点の一覧を、件数に合わせた高さにする。空の時は見出しだけの小さな部品になる。</summary>
        private void LayoutList()
        {
            int count = _listRows.Count;
            float room = ViewSize().y - _listTop - ListHeader - 10f - (Margin + ToolHeight + 14f);
            // 由来の行は、全部の行が重ならずに入る時だけ出す。入らなければ名前だけの行へ戻す
            bool withGloss = count > 0 && room / count >= ListRowWithGloss && _listGloss.Exists(g => g.text.Length > 0);
            float rowHeight = withGloss ? ListRowWithGloss : ListRow;
            float pitch = count > 0 ? Mathf.Min(rowHeight, room / count) : rowHeight;
            for (int r = 0; r < count; r++)
            {
                RectTransform row = _listRows[r];
                row.anchorMin = new Vector2(0f, 1f);
                row.anchorMax = new Vector2(1f, 1f);
                row.pivot = new Vector2(0.5f, 1f);
                row.sizeDelta = new Vector2(0f, rowHeight - 1f);
                row.anchoredPosition = new Vector2(0f, -r * pitch);
                _listGloss[r].gameObject.SetActive(withGloss);
            }
            float height = count > 0 ? ListHeader + (count - 1) * pitch + rowHeight + 8f : ListHeader;
            _listPart.sizeDelta = new Vector2(PartWidth, height);
        }

        private void FocusSite(RegionSite site)
        {
            _cursorMap = new Vector2(site.MapX, site.MapY);
            _center = new Vector2(site.MapX / RegionData.MapUnits, 1f - site.MapY / RegionData.MapUnits);
            ClampView();
            EnsureVisible(_cursorMap);
            ShowCard(site);
            RefreshReadout();
        }

        private void ClearDiscovered()
        {
            for (int m = 0; m < _markers.Count; m++) Destroy(_markers[m].gameObject);
            _markers.Clear();
            _markerSites.Clear();
            for (int r = 0; r < _listRows.Count; r++) Destroy(_listRows[r].gameObject);
            _listRows.Clear();
            _listGloss.Clear();
            LayoutList();
            HideCard();
        }

        // ---- 発見した地点のカード ----

        private void ShowCard(RegionSite site)
        {
            _cardSite = site;
            int number = _markerSites.IndexOf(site) + 1;
            RegionArea area = _data.AreaById(site.AreaId);
            string where = area != null ? area.Name : _data.PlaceNameAtMap(site.MapX, site.MapY);
            _cardEyebrow.text = "発見　" + number.ToString("00", CultureInfo.InvariantCulture) + " / " + _session.TotalSites + "　・　" + where;
            _cardTitle.text = site.Feature;
            // 別名＝表に出ている名前の他に付いている名前（古い呼び名・記号名）。由来＝表に出ている名前の成り立ち
            var others = new List<string>();
            for (int n = 1; n < site.Names.Count; n++) others.Add(site.Names[n].Text + "（" + RegionNaming.KindLabel(site.Names[n].Kind) + "）");
            _cardAlias.text = site.Names.Count == 0 ? "" : "別名　" + (others.Count > 0 ? string.Join(" ・ ", others.ToArray()) : "なし");
            _cardOrigin.text = site.Names.Count == 0 ? "" : RegionNaming.KindLabel(site.Names[0].Kind) + "。" + site.Names[0].Gloss;
            _cardKind.text = KindName(site.Kind);
            _cardPosition.text = site.MapX.ToString("0.0", CultureInfo.InvariantCulture) + " , " + site.MapY.ToString("0.0", CultureInfo.InvariantCulture);
            _cardHeight.text = Meters(site.HeightMeters) + " m";
            _card.gameObject.SetActive(true);
            _cardLine.gameObject.SetActive(true);
            PlaceCard();
            RefreshLabelCover();
        }

        private void HideCard()
        {
            _cardSite = null;
            _card.gameObject.SetActive(false);
            _cardLine.gameObject.SetActive(false);
            RefreshLabelCover();
        }

        /// <summary>カードを印の右下に置く。画面の右や下にはみ出す時は、反対側へ回す。印とカードは細い線でつなぐ。</summary>
        private void PlaceCard()
        {
            if (_cardSite == null) return;
            Vector2 view = ViewSize();
            Vector2 at = MapToLocal(new Vector2(_cardSite.MapX, _cardSite.MapY));
            bool right = at.x + 64f + CardWidth <= view.x * 0.5f - Margin;
            bool below = at.y - 44f - CardHeight >= -view.y * 0.5f + Margin + ToolHeight + 14f;
            float x = right ? at.x + 64f : at.x - 64f - CardWidth;
            float y = below ? at.y - 44f : at.y + 44f + CardHeight;
            _card.anchoredPosition = new Vector2(x, y);

            // 線は、印の輪の外から、カードの印に近い角まで
            var corner = new Vector2(right ? x : x + CardWidth, below ? y : y - CardHeight);
            Vector2 direction = (corner - at).normalized;
            Vector2 start = at + direction * 30f;
            Vector2 span = corner - start;
            _cardLine.anchoredPosition = start;
            _cardLine.sizeDelta = new Vector2(span.magnitude, 1.5f);
            _cardLine.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(span.y, span.x) * Mathf.Rad2Deg);
        }

        private void SetButtons(bool enabled)
        {
            _searchButton.interactable = enabled;
            _regionButton.interactable = enabled;
            _styleButton.interactable = enabled;
            _cloudButton.interactable = enabled && _render.Style == RegionMapStyle.Satellite;
            _areaButton.interactable = enabled;
        }

        private static string SignalName(SearchSignal signal)
        {
            switch (signal)
            {
                case SearchSignal.Found: return "発見";
                case SearchSignal.Strong: return "強い反応";
                case SearchSignal.Faint: return "弱い反応";
                default: return "反応なし";
            }
        }

        private static string StyleName(RegionMapStyle style)
        {
            switch (style)
            {
                case RegionMapStyle.Atlas: return "地図帳";
                case RegionMapStyle.Relief: return "陰影";
                default: return "衛星写真";
            }
        }

        private static string KindName(RegionSiteKind kind)
        {
            switch (kind)
            {
                case RegionSiteKind.Monolith: return "モノリス";
                case RegionSiteKind.Arch: return "アーチ";
                case RegionSiteKind.Ring: return "環";
                case RegionSiteKind.Tower: return "塔";
                case RegionSiteKind.Dome: return "ドーム";
                case RegionSiteKind.Terrace: return "段丘";
                default: return "遺構";
            }
        }

        private static string BiomeName(RegionBiome biome)
        {
            switch (biome)
            {
                case RegionBiome.Ocean: return "海";
                case RegionBiome.Coast: return "海岸";
                case RegionBiome.Desert: return "乾燥地";
                case RegionBiome.Savanna: return "疎林の草原";
                case RegionBiome.Grassland: return "草原";
                case RegionBiome.Forest: return "森";
                case RegionBiome.Rainforest: return "雨林";
                case RegionBiome.Taiga: return "針葉樹林";
                case RegionBiome.Tundra: return "ツンドラ";
                case RegionBiome.Snow: return "雪原";
                case RegionBiome.Rock: return "岩場";
                case RegionBiome.River: return "川";
                default: return "湖";
            }
        }

        // ---- 画面の組み立て ----

        private static void EnsureCamera()
        {
            if (Camera.main != null || FindFirstObjectByType<Camera>() != null) return;
            var cameraObject = new GameObject("Region Explorer Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(10, 13, 18, 255);
            camera.orthographic = true;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private void BuildScreen()
        {
            var canvasObject = new GameObject("Region Explorer Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            // 下から順に、地図 → 左の部品 → 下端のボタンと操作の案内 → 知らせ
            BuildMap(canvasObject.transform);
            BuildParts(canvasObject.transform);
            BuildTools(canvasObject.transform);
            BuildHints(canvasObject.transform);
            BuildDetailBadge(canvasObject.transform);
            BuildMessage(canvasObject.transform);
        }

        private void BuildMap(Transform canvas)
        {
            // 画面全体が地図の表示範囲。地図の外（縮めた時の左右）は、暗い地色がそのまま見える
            _viewRect = CreateRect(canvas, "Map View");
            Stretch(_viewRect);
            Image backing = CreateImage(_viewRect, "Backing", new Color32(10, 13, 18, 255), null);
            Stretch(backing.rectTransform);

            // 地図の板。この板を動かし、大きさを変えて、拡大と移動を表す。雲と影は板の子なので地図とずれない
            _content = CreateRect(_viewRect, "Map");
            _content.sizeDelta = new Vector2(1920f, 1920f);
            _map = CreateLayer(_content, "Image");
            // 細部の層は広域の絵のすぐ上、雲の下に置く（雲と影は細部の上も流れる）
            _detail = new RegionDetailLayer(_content);
            _cloudShadows = CreateCloudLayer(_content, "Cloud Shadows");
            _clouds = CreateCloudLayer(_content, "Clouds");
            _highClouds = CreateCloudLayer(_content, "High Clouds");
            // エリアの境界線は雲の上に置く（雲に隠れると、どこまでが同じエリアか追えなくなる）
            _areaLines = CreateLayer(_content, "Area Lines");
            _areaLines.color = new Color(1f, 1f, 1f, 0.6f);

            // エリアの名前は板の子にせず、位置だけ毎フレーム合わせる（拡大しても字の大きさが変わらない）
            _areaLabelRoot = CreateRect(_viewRect, "Area Names");
            _markerRoot = CreateRect(_viewRect, "Markers");
            _range = CreateSearchRange(_viewRect);
            _range.gameObject.SetActive(false);

            // 十字は 1 枚の画像（表示の 2 倍の細かさで作り、縮めて出す）
            Image cursorImage = CreateImage(_viewRect, "Cursor", Color.white, _crosshair);
            cursorImage.preserveAspect = _skinCrosshair;
            _cursor = cursorImage.rectTransform;
            _cursor.sizeDelta = new Vector2(88f, 88f);
            _cursor.gameObject.SetActive(false);

            BuildCard(_viewRect);
        }

        /// <summary>
        /// サーチの範囲の輪。画像を差し替えた時はその画像、無ければ線の太さが一定の輪（RegionRingGraphic）。
        /// 細部まで拡大すると輪の直径が画面の数倍になるので、画像を引き伸ばす輪は太い帯になってしまう。
        /// </summary>
        private RectTransform CreateSearchRange(RectTransform parent)
        {
            if (_skinSearchRing)
            {
                Image image = CreateImage(parent, "Search Range", Color.white, _ring);
                image.preserveAspect = true;
                return image.rectTransform;
            }
            var created = new GameObject("Search Range", typeof(RectTransform), typeof(RegionRingGraphic));
            created.transform.SetParent(parent, false);
            var ring = created.GetComponent<RegionRingGraphic>();
            ring.raycastTarget = false;
            ring.color = new Color(1f, 1f, 1f, 0.85f);
            return ring.rectTransform;
        }

        private void BuildCard(RectTransform parent)
        {
            _cardLine = CreateImage(parent, "Found Line", AccentColor, null).rectTransform;
            _cardLine.pivot = new Vector2(0f, 0.5f);

            Image card = CreateImage(parent, "Found Card", CardColor, null);
            _card = card.rectTransform;
            _card.pivot = new Vector2(0f, 1f);
            _card.sizeDelta = new Vector2(CardWidth, CardHeight);
            AddFrame(_card, AccentColor);

            _cardEyebrow = CreateText(_card, "Eyebrow", "発見", 15, AccentLightColor, TextAnchor.UpperLeft, _font);
            Place(_cardEyebrow.rectTransform, PartPad, -10f, CardWidth - PartPad * 2f, 22f);
            _cardTitle = CreateText(_card, "Title", "", 25, TextColor, TextAnchor.UpperLeft, _font);
            Place(_cardTitle.rectTransform, PartPad, -30f, CardWidth - PartPad * 2f, 36f);
            _cardAlias = CreateText(_card, "Alias", "", 15, MutedColor, TextAnchor.UpperLeft, _font);
            Place(_cardAlias.rectTransform, PartPad, -66f, CardWidth - PartPad * 2f, 20f);
            FitText(_cardAlias, 11, 15);
            _cardKind = CreateFact(_card, "種類", PartPad, -92f, 96f, _font);
            _cardPosition = CreateFact(_card, "座標", PartPad + 100f, -92f, 128f, _numberFont);
            _cardHeight = CreateFact(_card, "標高", PartPad + 232f, -92f, 110f, _numberFont);
            Text originLabel = CreateText(_card, "Origin Label", "由来", 14, MutedColor, TextAnchor.UpperLeft, _font);
            Place(originLabel.rectTransform, PartPad, -144f, 40f, 20f);
            _cardOrigin = CreateText(_card, "Origin", "", 15, TextColor, TextAnchor.UpperLeft, _font);
            Place(_cardOrigin.rectTransform, PartPad + 44f, -143f, CardWidth - PartPad * 2f - 44f, 42f);
            FitText(_cardOrigin, 11, 15);
            Text close = CreateText(_card, "Close", "次のサーチか Esc で閉じる", 15, MutedColor, TextAnchor.UpperLeft, _font);
            Place(close.rectTransform, PartPad, -190f, CardWidth - PartPad * 2f, 22f);

            _card.gameObject.SetActive(false);
            _cardLine.gameObject.SetActive(false);
        }

        /// <summary>枠に収まるまで字を小さくし、枠の幅で折り返す（長い別名・由来のため）。</summary>
        private static void FitText(Text text, int min, int max)
        {
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = min;
            text.resizeTextMaxSize = max;
        }

        /// <summary>小さな見出しと、その下の値。値の Text を返す。</summary>
        private Text CreateFact(RectTransform parent, string label, float x, float y, float width, Font valueFont)
        {
            Text caption;
            return CreateFact(parent, label, x, y, width, valueFont, out caption);
        }

        /// <summary>見出しの文字を後から変える時（標高／水深）は、見出しの Text も受け取る。</summary>
        private Text CreateFact(RectTransform parent, string label, float x, float y, float width, Font valueFont, out Text caption)
        {
            caption = CreateText(parent, label, label, 14, MutedColor, TextAnchor.UpperLeft, _font);
            Place(caption.rectTransform, x, y, width, 20f);
            // 日本語の書体は数字の書体より字面が大きく、行の中で低い位置に出る。大きさと位置を書体ごとに変えて、横に並べた時に揃える
            bool numeric = valueFont == _numberFont;
            Text value = CreateText(parent, label + " Value", "", numeric ? 20 : 18, TextColor, TextAnchor.UpperLeft, valueFont);
            Place(value.rectTransform, x, numeric ? y - 19f : y - 15f, width, 28f);
            return value;
        }

        private void BuildParts(Transform canvas)
        {
            float y = -Margin;

            // 地域
            RectTransform region = CreatePart(canvas, "Region", y, 88f);
            _title = CreateText(region, "Title", "地域", 27, TextColor, TextAnchor.UpperLeft, _font);
            Place(_title.rectTransform, PartPad, -11f, PartInner, 36f);
            _subtitle = CreateText(region, "Subtitle", "", 17, MutedColor, TextAnchor.UpperLeft, _font);
            Place(_subtitle.rectTransform, PartPad, -52f, PartInner, 24f);
            y -= 88f + PartGap;

            // カーソル位置
            RectTransform position = CreatePart(canvas, "Position", y, 194f);
            _positionLabel = CreateText(position, "Label", "カーソル位置", 15, MutedColor, TextAnchor.UpperLeft, _font);
            Place(_positionLabel.rectTransform, PartPad, -10f, PartInner, 22f);
            Text labelX = CreateText(position, "X", "X", 20, MutedColor, TextAnchor.LowerLeft, _numberFont);
            Place(labelX.rectTransform, PartPad, -32f, 20f, 46f);
            _coordinateX = CreateText(position, "X Value", "--.-", 44, TextColor, TextAnchor.LowerLeft, _numberFont);
            Place(_coordinateX.rectTransform, PartPad + 22f, -32f, 130f, 52f);
            Text labelY = CreateText(position, "Y", "Y", 20, MutedColor, TextAnchor.LowerLeft, _numberFont);
            Place(labelY.rectTransform, PartPad + 164f, -32f, 20f, 46f);
            _coordinateY = CreateText(position, "Y Value", "--.-", 44, TextColor, TextAnchor.LowerLeft, _numberFont);
            Place(_coordinateY.rectTransform, PartPad + 186f, -32f, 130f, 52f);
            _groundValue = CreateFact(position, "標高", PartPad, -92f, 120f, _numberFont, out _groundLabel);
            _biomeValue = CreateFact(position, "土地", PartPad + 132f, -92f, PartInner - 132f, _font);
            _areaValue = CreateFact(position, "エリア", PartPad, -140f, PartInner, _font);
            y -= 194f + PartGap;

            // 反応とサーチ
            RectTransform signal = CreatePart(canvas, "Signal", y, 134f);
            Text signalLabel = CreateText(signal, "Label", "反応", 15, MutedColor, TextAnchor.UpperLeft, _font);
            Place(signalLabel.rectTransform, PartPad, -10f, 120f, 22f);
            _signalText = CreateText(signal, "Value", "反応なし", 19, TextColor, TextAnchor.UpperRight, _font);
            Place(_signalText.rectTransform, PartPad + PartInner - 240f, -8f, 240f, 28f);
            _signalBars = new Image[3];
            float barWidth = (PartInner - 12f) / 3f;
            for (int b = 0; b < 3; b++)
            {
                _signalBars[b] = CreateImage(signal, "Bar " + b, OffColor, null);
                Place(_signalBars[b].rectTransform, PartPad + b * (barWidth + 6f), -44f, barWidth, 8f);
            }
            _searchButton = CreateButton(signal, "Search", new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.92f));
            RectTransform searchRect = (RectTransform)_searchButton.transform;
            Place(searchRect, PartPad, -66f, PartInner, 50f);
            _searchButton.onClick.AddListener(() => SearchAtCursor());
            BuildSearchLabel(searchRect);
            y -= 134f + PartGap;

            // 発見した地点（件数に合わせて下へ伸びる）
            _listTop = -y;
            _listPart = CreatePart(canvas, "Discovered", y, ListHeader);
            Text listLabel = CreateText(_listPart, "Label", "発見した地点", 15, MutedColor, TextAnchor.UpperLeft, _font);
            Place(listLabel.rectTransform, PartPad, -10f, 160f, 22f);
            _searchCount = CreateText(_listPart, "Searches", "", 15, MutedColor, TextAnchor.UpperRight, _font);
            Place(_searchCount.rectTransform, PartPad + PartInner - 110f, -10f, 110f, 22f);
            _listCount = CreateText(_listPart, "Count", "", 19, TextColor, TextAnchor.UpperRight, _numberFont);
            Place(_listCount.rectTransform, PartPad + PartInner - 110f - 12f - 90f, -8f, 90f, 28f);
            _listRoot = CreateRect(_listPart, "List");
            _listRoot.anchorMin = new Vector2(0f, 1f);
            _listRoot.anchorMax = new Vector2(0f, 1f);
            _listRoot.pivot = new Vector2(0f, 1f);
            _listRoot.sizeDelta = new Vector2(PartInner, 0f);
            _listRoot.anchoredPosition = new Vector2(PartPad, -ListHeader + 4f);
        }

        /// <summary>サーチボタンの中身。「サーチ」と、キーの名前を枠で囲んだ札を、中央に並べる。</summary>
        private void BuildSearchLabel(RectTransform button)
        {
            Text label = CreateText(button, "Label", "サーチ", 22, AccentTextColor, TextAnchor.MiddleLeft, _font, false);
            label.fontStyle = FontStyle.Bold;
            Text key = CreateText(button, "Key", "Space", 16, AccentTextColor, TextAnchor.MiddleCenter, _numberFont, false);
            float labelWidth = label.preferredWidth;
            float keyWidth = key.preferredWidth + 18f;
            float start = (PartInner - (labelWidth + 12f + keyWidth)) * 0.5f;
            Place(label.rectTransform, start, 0f, labelWidth, 50f);
            RectTransform cap = CreateRect(button, "Key Cap");
            Place(cap, start + labelWidth + 12f, -12f, keyWidth, 26f);
            AddFrame(cap, new Color(AccentTextColor.r, AccentTextColor.g, AccentTextColor.b, 0.85f));
            key.rectTransform.SetParent(cap, false);
            Stretch(key.rectTransform);
        }

        private void BuildTools(Transform canvas)
        {
            // 左下：描き方、雲、別の地域
            float x = Margin;
            _styleButton = CreateToolButton(canvas, "描き方：" + StyleName(_render.Style), ref x, 208f, out _styleLabel);
            _styleButton.onClick.AddListener(NextStyle);
            _cloudButton = CreateToolButton(canvas, "雲：" + CloudOpacityName[_cloudLevel], ref x, 184f, out _cloudLabel);
            _cloudButton.onClick.AddListener(NextCloudOpacity);
            _areaButton = CreateToolButton(canvas, "地名：" + AreaDisplayName[_areaLevel], ref x, 196f, out _areaLabel);
            _areaButton.onClick.AddListener(NextAreaDisplay);
            Text regionLabel;
            _regionButton = CreateToolButton(canvas, "別の地域", ref x, 124f, out regionLabel);
            _regionButton.onClick.AddListener(NextRegion);
        }

        private Button CreateToolButton(Transform canvas, string label, ref float x, float width, out Text text)
        {
            Button button = CreateButton(canvas, label, PartColor);
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(width, ToolHeight);
            rect.anchoredPosition = new Vector2(x, Margin);
            AddFrame(rect, LineColor);
            text = CreateText(rect, "Label", label, 18, TextColor, TextAnchor.MiddleCenter, _font);
            Stretch(text.rectTransform);
            x += width + 8f;
            return button;
        }

        private void BuildHints(Transform canvas)
        {
            // 右下：操作の案内。キーの名前は枠で囲む
            string[] keys = { "クリック", "矢印", "ホイール", "右ドラッグ", "Home" };
            string[] actions = { "位置", "微調整", "拡大・縮小", "移動", "全体" };
            Image strip = CreateImage(canvas, "Hints", PartColor, null);
            RectTransform rect = strip.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-Margin, Margin);
            AddFrame(rect, LineColor);

            float x = 14f;
            for (int k = 0; k < keys.Length; k++)
            {
                Text key = CreateText(rect, keys[k], keys[k], 15, TextColor, TextAnchor.MiddleCenter, _font);
                float keyWidth = key.preferredWidth + 14f;
                RectTransform cap = CreateRect(rect, keys[k] + " Cap");
                Place(cap, x, -9f, keyWidth, 24f);
                AddFrame(cap, new Color(1f, 1f, 1f, 0.55f));
                key.rectTransform.SetParent(cap, false);
                Stretch(key.rectTransform);
                x += keyWidth + 7f;

                Text action = CreateText(rect, actions[k], actions[k], 16, TextColor, TextAnchor.MiddleLeft, _font);
                float actionWidth = action.preferredWidth;
                Place(action.rectTransform, x, 0f, actionWidth, ToolHeight);
                x += actionWidth + (k < keys.Length - 1 ? 20f : 14f);
            }
            rect.sizeDelta = new Vector2(x, ToolHeight);
        }

        /// <summary>右上の札。拡大率と、地形が広域か細部か（作成中ならその経過）を出す。</summary>
        private void BuildDetailBadge(Transform canvas)
        {
            Image badge = CreateImage(canvas, "Detail Badge", PartColor, null);
            RectTransform rect = badge.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(DetailBadgeWidth, DetailBadgeHeight);
            rect.anchoredPosition = new Vector2(-Margin, -Margin);
            AddFrame(rect, LineColor);
            _zoomText = CreateText(rect, "Zoom", "", 18, TextColor, TextAnchor.UpperLeft, _numberFont);
            Place(_zoomText.rectTransform, PartPad, -9f, DetailBadgeWidth - PartPad * 2f, 26f);
            _detailText = CreateText(rect, "Terrain", "", 17, MutedColor, TextAnchor.UpperLeft, _font);
            Place(_detailText.rectTransform, PartPad, -33f, DetailBadgeWidth - PartPad * 2f, 24f);
            // 作成中だけ、札の下端を行き来する帯を出す
            Image track = CreateImage(rect, "Progress Track", OffColor, null);
            Place(track.rectTransform, PartPad, -DetailBadgeHeight + 7f, DetailBadgeWidth - PartPad * 2f, 3f);
            Image bar = CreateImage(track.rectTransform, "Progress Bar", AccentColor, null);
            _detailBar = bar.rectTransform;
            _detailBar.anchorMin = new Vector2(0f, 0f);
            _detailBar.anchorMax = new Vector2(0f, 1f);
            _detailBar.pivot = new Vector2(0f, 0.5f);
            _detailBar.sizeDelta = new Vector2(96f, 0f);
            track.gameObject.SetActive(false);
        }

        private void BuildMessage(Transform canvas)
        {
            // 知らせは、下端のボタンの列より上、画面の中央に出す
            Image strip = CreateImage(canvas, "Message", new Color(PartColor.r, PartColor.g, PartColor.b, 0.74f), null);
            RectTransform rect = strip.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(720f, 52f);
            rect.anchoredPosition = new Vector2(0f, Margin + ToolHeight + 14f);
            AddFrame(rect, LineColor);
            _messageGroup = strip.gameObject.AddComponent<CanvasGroup>();
            _messageGroup.blocksRaycasts = false;
            _messageGroup.interactable = false;
            _messageHideAt = -1f;
            _message = CreateText(rect, "Text", "", 23, TextColor, TextAnchor.MiddleCenter, _font);
            Stretch(_message.rectTransform);
        }

        /// <summary>左に重ねる部品の面。面の上のクリックは地図へ通さない。</summary>
        private RectTransform CreatePart(Transform parent, string name, float y, float height)
        {
            Image part = CreateImage(parent, name, PartColor, null);
            part.raycastTarget = true;
            Place(part.rectTransform, Margin, y, PartWidth, height);
            AddFrame(part.rectTransform, LineColor);
            return part.rectTransform;
        }

        /// <summary>
        /// 細い枠線を上下左右に 1 本ずつ置く。Outline は面そのものをずらして重ね描きするので、
        /// 半透明の面では内側まで濃くなる。枠は別の画像で描く。
        /// </summary>
        private void AddFrame(RectTransform target, Color color)
        {
            AddLine(target, "Line Top", color, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -1f), Vector2.zero);
            AddLine(target, "Line Bottom", color, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 1f));
            AddLine(target, "Line Left", color, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1f, -1f));
            AddLine(target, "Line Right", color, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(0f, -1f));
        }

        private void AddLine(RectTransform target, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform line = CreateImage(target, name, color, null).rectTransform;
            line.anchorMin = anchorMin;
            line.anchorMax = anchorMax;
            line.offsetMin = offsetMin;
            line.offsetMax = offsetMax;
        }

        private static RawImage CreateLayer(Transform parent, string name)
        {
            var created = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            created.transform.SetParent(parent, false);
            var layer = created.GetComponent<RawImage>();
            layer.raycastTarget = false;
            layer.enabled = false;
            Stretch(layer.rectTransform);
            return layer;
        }

        private static RegionCloudLayer CreateCloudLayer(Transform parent, string name)
        {
            var created = new GameObject(name, typeof(RectTransform), typeof(RegionCloudLayer));
            created.transform.SetParent(parent, false);
            var layer = created.GetComponent<RegionCloudLayer>();
            layer.raycastTarget = false;
            layer.enabled = false;
            Stretch(layer.rectTransform);
            return layer;
        }

        /// <summary>印に暗い縁を付ける。雲・雪・地図帳風の淡い地色の上では、縁のない印が見えなくなる。</summary>
        private static void AddDarkEdge(GameObject target)
        {
            Outline edge = target.AddComponent<Outline>();
            edge.effectColor = new Color(0f, 0f, 0f, 0.75f);
            edge.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var created = new GameObject(name, typeof(RectTransform));
            created.transform.SetParent(parent, false);
            return (RectTransform)created.transform;
        }

        private Image CreateImage(Transform parent, string name, Color color, Sprite sprite)
        {
            var created = new GameObject(name, typeof(RectTransform), typeof(Image));
            created.transform.SetParent(parent, false);
            var image = created.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>文字を作る。地図や雲が透ける面の上に置くので、既定では暗い影を付ける。</summary>
        private Text CreateText(Transform parent, string name, string value, int size, Color color, TextAnchor anchor, Font font, bool shadow = true)
        {
            var created = new GameObject(name, typeof(RectTransform), typeof(Text));
            created.transform.SetParent(parent, false);
            var text = created.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = value;
            if (shadow)
            {
                Shadow drop = created.AddComponent<Shadow>();
                drop.effectColor = ShadowColor;
                drop.effectDistance = new Vector2(1f, -1f);
            }
            return text;
        }

        private Button CreateButton(Transform parent, string name, Color background)
        {
            Image image = CreateImage(parent, name, background, null);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1.2f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
            button.colors = colors;
            var navigation = new Navigation();
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            return button;
        }

        /// <summary>一覧の 1 行。ふだんは面を持たず、ポインターを乗せた時だけ薄く明るくなる。</summary>
        private Button CreateRowButton(Transform parent, string name)
        {
            Image image = CreateImage(parent, name, Color.white, null);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.14f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.24f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            button.colors = colors;
            // 色の設定を変えると、Button は今の色（白）から 0.1 秒かけて移る。作った直後に白い帯が光らないよう、すぐ透明にする
            image.CrossFadeColor(colors.normalColor, 0f, true, true);
            var navigation = new Navigation();
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            return button;
        }

        /// <summary>親の左上を基準に置く（y は下向きが負）。</summary>
        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
