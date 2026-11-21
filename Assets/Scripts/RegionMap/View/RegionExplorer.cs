using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 自動生成した地域の鳥瞰図を表示し、位置を指定してサーチする探索画面。
    /// 空のシーンにこのコンポーネントを 1 つ置けば動く（画面部品は起動時に組み立てる）。
    /// 操作: 地図を左クリックでカーソル移動、サーチボタンか Space でサーチ、ホイールで拡大、右ドラッグで移動。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RegionExplorer : MonoBehaviour
    {
        [SerializeField] private RegionSpec _spec = new RegionSpec();
        [SerializeField] private RegionRenderOptions _render = new RegionRenderOptions();

        [Tooltip("オンにすると、サーチしなくてもカーソル位置の反応を常に表示する。")]
        [SerializeField] private bool _liveSignal;

        [Tooltip("雲が地図の幅を横切るのにかかる秒数。0 にすると雲は動かない。")]
        [SerializeField] private float _cloudCrossSeconds = 420f;

        private const float PanelWidth = 560f;
        private const float MapSize = 1016f;
        private const float Gap = 32f;
        private const float MaxZoom = 8f;

        private static readonly Color PanelColor = new Color32(18, 22, 28, 235);
        private static readonly Color BoxColor = new Color32(34, 40, 48, 255);
        private static readonly Color TextColor = new Color32(236, 233, 226, 255);
        private static readonly Color MutedColor = new Color32(150, 156, 162, 255);
        private static readonly Color AccentColor = new Color32(232, 170, 70, 255);
        private static readonly Color AccentTextColor = new Color32(40, 28, 8, 255);
        private static readonly Color OffColor = new Color32(62, 68, 76, 255);

        // 雲の濃さの段（ボタンで順に切り替える）
        private static readonly float[] CloudOpacity = { 1f, 0.45f, 0f };
        private static readonly string[] CloudOpacityName = { "濃い", "薄い", "なし" };
        // 雲が流れる向き（東へ、少し南へ）。x は東、y は北が正
        private static readonly Vector2 CloudWind = new Vector2(1f, -0.22f);

        private sealed class Generated
        {
            public RegionData Data;
            public byte[] Pixels;
            // 雲と影の層。地域が変わらない描き直しでは null（前の層をそのまま使う）
            public byte[] CloudPixels;
            public byte[] ShadowPixels;
            public int CloudSize;
        }

        private Task<Generated> _task;
        private RegionData _data;
        private ExplorationSession _session;
        private Texture2D _texture;
        private Texture2D _cloudTexture;
        private Texture2D _shadowTexture;
        private Font _font;
        private Sprite _ring;
        private Sprite _dot;
        private Sprite _crosshair;

        private RawImage _map;
        private RawImage _clouds;
        private RawImage _cloudShadows;
        private RectTransform _mapRect;
        private RectTransform _cursor;
        private RectTransform _range;
        private RectTransform _markerRoot;
        private RectTransform _listRoot;
        private readonly List<RectTransform> _markers = new List<RectTransform>();
        private readonly List<RegionSite> _markerSites = new List<RegionSite>();

        private Text _title;
        private Text _subtitle;
        private Text _coordinates;
        private Text _detail;
        private Text _signalText;
        private Text _listTitle;
        private Text _message;
        private CanvasGroup _messageGroup;
        private float _messageHideAt;
        private Text _styleLabel;
        private Text _cloudLabel;
        private Image[] _signalBars;
        private Button _searchButton;
        private Button _regionButton;
        private Button _styleButton;
        private Button _cloudButton;
        private int _cloudLevel;
        private Vector2 _cloudDrift;
        // 生成中の工程名。別スレッドから書き、Update で読んで知らせに出す
        private volatile string _phase;
        private string _phaseShown;
        private string _phaseMessage = string.Empty;

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

        /// <summary>雲の濃さの段（0 濃い、1 薄い、2 なし）。</summary>
        /// <summary>雲の層が流れた量（地図の幅を 1 とした割合。1 で一周して 0 へ戻る）。</summary>
        public Vector2 CloudDrift { get { return _cloudDrift; } }

        public int CloudLevel
        {
            get { return _cloudLevel; }
        }

        private void Awake()
        {
            _font = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI", "Meiryo UI", "Meiryo", "MS Gothic", "Arial" }, 28);
            _ring = RegionMapTexture.CreateRingSprite(128, 3f, false);
            _dot = RegionMapTexture.CreateRingSprite(32, 0f, true);
            _crosshair = RegionMapTexture.CreateCrosshairSprite(96, 9f, 4f, 2.5f);
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
            if (_shadowTexture != null) Destroy(_shadowTexture);
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
        }

        // ---- 外から呼べる操作（ボタンとテストの両方が使う） ----

        /// <summary>カーソルを地図座標（0〜100）へ動かす。</summary>
        public void MoveCursor(float mapX, float mapY)
        {
            _cursorMap = new Vector2(Mathf.Clamp(mapX, 0f, RegionData.MapUnits), Mathf.Clamp(mapY, 0f, RegionData.MapUnits));
            RefreshReadout();
        }

        /// <summary>カーソル位置でサーチする。</summary>
        public SearchResult SearchAtCursor()
        {
            if (!IsReady) return new SearchResult();
            SearchResult result = _session.Search(_cursorMap.x, _cursorMap.y);
            _lastSignal = result.Signal;
            switch (result.Signal)
            {
                case SearchSignal.Found:
                    ShowMessage("「" + result.Site.Name + "」を発見", 6f);
                    AddDiscovered(result.Site);
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

        /// <summary>地図の下端に知らせを出す。seconds が 0 以下なら、次の知らせまで出したままにする。</summary>
        private void ShowMessage(string text, float seconds)
        {
            _message.text = text;
            _messageGroup.alpha = 1f;
            _messageHideAt = seconds > 0f ? Time.unscaledTime + seconds : -1f;
        }

        /// <summary>時間が来た知らせを薄くして消す。出したままだと地図の南端が読めない。</summary>
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
                byte[] shadowPixels = null;
                Task clouds = Task.Run(() =>
                {
                    float[] field = RegionClouds.BuildField(cloudSize, spec.Seed, cloudCover);
                    RegionClouds.RenderLayers(field, cloudSize, out cloudPixels, out shadowPixels);
                });
                RegionData data = RegionGenerator.Generate(spec, phase => _phase = phase);
                _phase = "地図を描く";
                byte[] pixels = RegionMapRenderer.Render(data, options);
                clouds.Wait();
                return new Generated { Data = data, Pixels = pixels, CloudSize = cloudSize, CloudPixels = cloudPixels, ShadowPixels = shadowPixels };
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
            _map.color = Color.white;

            if (generated.CloudPixels != null)
            {
                if (_cloudTexture != null) Destroy(_cloudTexture);
                if (_shadowTexture != null) Destroy(_shadowTexture);
                _cloudTexture = RegionMapTexture.Create(generated.CloudPixels, generated.CloudSize, true);
                _shadowTexture = RegionMapTexture.Create(generated.ShadowPixels, generated.CloudSize, true);
                _clouds.texture = _cloudTexture;
                _cloudShadows.texture = _shadowTexture;
                _cloudDrift = Vector2.zero;
            }

            if (!sameRegion)
            {
                _session = new ExplorationSession(_data);
                _lastSignal = SearchSignal.None;
                _zoom = 1f;
                _center = new Vector2(0.5f, 0.5f);
                _cursorMap = new Vector2(50f, 50f);
                ClearDiscovered();
                _title.text = "地域 seed " + _data.Spec.Seed;
                _subtitle.text = string.Format("{0:0} km 四方 ・ 陸 {1:0}% ・ 最高 {2:0} m", _data.SizeKm, _data.LandFraction * 100f, _data.MaxHeight);
                ShowMessage("地図をクリックして位置を決め、サーチで調べる", 8f);
            }
            else
            {
                ShowMessage("描き方：" + StyleName(_render.Style), 3f);
            }
            SetButtons(true);
            RefreshCloudLayers();
            RefreshReadout();
        }

        // ---- 入力 ----

        private void HandlePointer()
        {
            Vector2 local;
            _hovering = RectTransformUtility.ScreenPointToLocalPointInRectangle(_mapRect, Input.mousePosition, null, out local)
                && Mathf.Abs(local.x) <= MapSize * 0.5f && Mathf.Abs(local.y) <= MapSize * 0.5f;

            if (_hovering)
            {
                _hoverMap = LocalToMap(local);
                float wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.01f) ZoomAt(local, wheel > 0f ? 1.25f : 0.8f);
                if (Input.GetMouseButtonDown(0)) MoveCursor(_hoverMap.x, _hoverMap.y);
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
                    float width = 1f / _zoom;
                    _center -= new Vector2(delta.x, delta.y) / MapSize * width;
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

            float step = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 0.1f : 0.5f;
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
            _zoom = Mathf.Clamp(_zoom * factor, 1f, MaxZoom);
            Vector2 after = LocalToUv(local);
            _center += before - after;
            ClampView();
        }

        private void ClampView()
        {
            float half = 0.5f / _zoom;
            _center.x = Mathf.Clamp(_center.x, half, 1f - half);
            _center.y = Mathf.Clamp(_center.y, half, 1f - half);
        }

        // ---- 座標の換算（地図座標は Y が南向き、テクスチャは V が上向き） ----

        private Vector2 LocalToUv(Vector2 local)
        {
            float width = 1f / _zoom;
            return new Vector2(
                _center.x - width * 0.5f + (local.x / MapSize + 0.5f) * width,
                _center.y - width * 0.5f + (local.y / MapSize + 0.5f) * width);
        }

        private Vector2 LocalToMap(Vector2 local)
        {
            Vector2 uv = LocalToUv(local);
            return new Vector2(Mathf.Clamp01(uv.x) * RegionData.MapUnits, Mathf.Clamp01(1f - uv.y) * RegionData.MapUnits);
        }

        private Vector2 MapToLocal(Vector2 map)
        {
            float width = 1f / _zoom;
            float u = map.x / RegionData.MapUnits;
            float v = 1f - map.y / RegionData.MapUnits;
            return new Vector2(
                ((u - (_center.x - width * 0.5f)) / width - 0.5f) * MapSize,
                ((v - (_center.y - width * 0.5f)) / width - 0.5f) * MapSize);
        }

        // ---- 表示の更新 ----

        private void RefreshOverlay()
        {
            float width = 1f / _zoom;
            _map.uvRect = new Rect(_center.x - width * 0.5f, _center.y - width * 0.5f, width, width);

            // 雲は風下へ少しずつずらす。影は雲より南東（u は東、v は北が正なので +u, -v）に落ちる
            if (_cloudCrossSeconds > 0f)
            {
                _cloudDrift += CloudWind * (Time.deltaTime / _cloudCrossSeconds);
                _cloudDrift.x = Mathf.Repeat(_cloudDrift.x, 1f);
                _cloudDrift.y = Mathf.Repeat(_cloudDrift.y, 1f);
            }
            var cloudView = new Rect(_map.uvRect.x - _cloudDrift.x, _map.uvRect.y - _cloudDrift.y, width, width);
            _clouds.uvRect = cloudView;
            _cloudShadows.uvRect = new Rect(cloudView.x - RegionClouds.ShadowOffset, cloudView.y + RegionClouds.ShadowOffset, width, width);
            _cursor.anchoredPosition = MapToLocal(_cursorMap);
            _range.anchoredPosition = _cursor.anchoredPosition;
            float diameter = _session.FoundRadius / RegionData.MapUnits * MapSize * _zoom * 2f;
            _range.sizeDelta = new Vector2(diameter, diameter);
            for (int m = 0; m < _markers.Count; m++)
            {
                RegionSite site = _markerSites[m];
                _markers[m].anchoredPosition = MapToLocal(new Vector2(site.MapX, site.MapY));
            }
        }

        /// <summary>雲と影の層の見え方を、描き方と濃さの段に合わせる。雲があるのは衛星写真の時だけ。</summary>
        private void RefreshCloudLayers()
        {
            bool satellite = _render.Style == RegionMapStyle.Satellite;
            float opacity = satellite && _cloudTexture != null ? CloudOpacity[_cloudLevel] : 0f;
            _clouds.enabled = opacity > 0f;
            _cloudShadows.enabled = opacity > 0f;
            _clouds.color = new Color(1f, 1f, 1f, opacity);
            _cloudShadows.color = new Color(1f, 1f, 1f, opacity);
            _cloudLabel.text = satellite ? "雲：" + CloudOpacityName[_cloudLevel] : "雲：衛星写真のみ";
            _cloudButton.interactable = satellite && _task == null;
        }

        private void RefreshReadout()
        {
            if (_data == null) return;
            Vector2 at = _hovering ? _hoverMap : _cursorMap;
            _coordinates.text = string.Format("X {0:00.0}　Y {1:00.0}", at.x, at.y);
            float height = _data.HeightAtMap(at.x, at.y);
            string ground = height > 0f ? string.Format("標高 {0:0} m", height) : string.Format("水深 {0:0} m", -height);
            _detail.text = (_hovering ? "ポインター位置 ・ " : "カーソル位置 ・ ") + ground + " ・ " + BiomeName(_data.BiomeAtMap(at.x, at.y));

            SearchSignal signal = _liveSignal ? _session.Sense(_cursorMap.x, _cursorMap.y).Signal : _lastSignal;
            int level = (int)signal;
            for (int b = 0; b < _signalBars.Length; b++) _signalBars[b].color = b < level ? AccentColor : OffColor;
            _signalText.text = SignalName(signal);
            _listTitle.text = string.Format("発見した地点 {0} / {1}　（サーチ {2} 回）", _session.Discovered.Count, _session.TotalSites, _session.SearchCount);
        }

        private void AddDiscovered(RegionSite site)
        {
            RectTransform marker = CreateImage(_markerRoot, "Marker " + site.Id, AccentColor, _dot).rectTransform;
            marker.sizeDelta = new Vector2(18f, 18f);
            RectTransform halo = CreateImage(marker, "Halo", AccentColor, _ring).rectTransform;
            halo.sizeDelta = new Vector2(40f, 40f);
            _markers.Add(marker);
            _markerSites.Add(site);

            int row = _markers.Count - 1;
            Button entry = CreateButton(_listRoot, site.Name, BoxColor, TextColor, 20, TextAnchor.MiddleLeft);
            RectTransform rect = (RectTransform)entry.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 24f);
            rect.anchoredPosition = new Vector2(0f, -row * 25f);
            entry.onClick.AddListener(() => FocusSite(site));
        }

        private void FocusSite(RegionSite site)
        {
            MoveCursor(site.MapX, site.MapY);
            _center = new Vector2(site.MapX / RegionData.MapUnits, 1f - site.MapY / RegionData.MapUnits);
            ClampView();
            ShowMessage(string.Format("{0}　X {1:0.0}　Y {2:0.0}", site.Name, site.MapX, site.MapY), 4f);
        }

        private void ClearDiscovered()
        {
            for (int m = 0; m < _markers.Count; m++) Destroy(_markers[m].gameObject);
            _markers.Clear();
            _markerSites.Clear();
            for (int c = _listRoot.childCount - 1; c >= 0; c--) Destroy(_listRoot.GetChild(c).gameObject);
        }

        private void SetButtons(bool enabled)
        {
            _searchButton.interactable = enabled;
            _regionButton.interactable = enabled;
            _styleButton.interactable = enabled;
            _cloudButton.interactable = enabled && _render.Style == RegionMapStyle.Satellite;
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

            RectTransform root = CreateRect(canvasObject.transform, "Root");
            root.sizeDelta = new Vector2(PanelWidth + Gap + MapSize, MapSize);

            BuildPanel(root);
            BuildMap(root);
        }

        private void BuildPanel(RectTransform root)
        {
            Image panel = CreateImage(root, "Panel", PanelColor, null);
            RectTransform rect = panel.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(PanelWidth, 0f);
            rect.anchoredPosition = Vector2.zero;

            const float pad = 28f;
            float inner = PanelWidth - pad * 2f;
            float y = -pad;

            _title = CreateText(rect, "Title", "地域", 34, TextColor, TextAnchor.UpperLeft);
            Place(_title.rectTransform, pad, y, inner, 44f);
            y -= 46f;
            _subtitle = CreateText(rect, "Subtitle", "", 21, MutedColor, TextAnchor.UpperLeft);
            Place(_subtitle.rectTransform, pad, y, inner, 30f);
            y -= 44f;

            RectTransform positionBox = CreateBox(rect, "Position", pad, y, inner, 138f);
            _coordinates = CreateText(positionBox, "Coordinates", "X --.-　Y --.-", 42, TextColor, TextAnchor.UpperLeft);
            Place(_coordinates.rectTransform, 18f, -14f, inner - 36f, 56f);
            _detail = CreateText(positionBox, "Detail", "", 21, MutedColor, TextAnchor.UpperLeft);
            Place(_detail.rectTransform, 18f, -84f, inner - 36f, 34f);
            y -= 138f + 16f;

            RectTransform signalBox = CreateBox(rect, "Signal", pad, y, inner, 96f);
            Text signalLabel = CreateText(signalBox, "Label", "反応", 21, MutedColor, TextAnchor.UpperLeft);
            Place(signalLabel.rectTransform, 18f, -12f, 120f, 30f);
            _signalText = CreateText(signalBox, "Value", "反応なし", 21, TextColor, TextAnchor.UpperRight);
            Place(_signalText.rectTransform, inner - 18f - 240f, -12f, 240f, 30f);
            _signalBars = new Image[3];
            float barWidth = (inner - 36f - 16f) / 3f;
            for (int b = 0; b < 3; b++)
            {
                _signalBars[b] = CreateImage(signalBox, "Bar " + b, OffColor, null);
                Place(_signalBars[b].rectTransform, 18f + b * (barWidth + 8f), -56f, barWidth, 20f);
            }
            y -= 96f + 16f;

            _searchButton = CreateButton(rect, "サーチ（Space）", AccentColor, AccentTextColor, 30, TextAnchor.MiddleCenter);
            Place((RectTransform)_searchButton.transform, pad, y, inner, 84f);
            _searchButton.onClick.AddListener(() => SearchAtCursor());
            y -= 84f + 16f;

            // 下端のボタンは 2 段（上: 描き方と雲、下: 別の地域を生成）
            float bottomButtons = 52f;
            float listHeight = MapSize + y - pad - bottomButtons * 2f - 12f - 16f;
            RectTransform listBox = CreateBox(rect, "Discovered", pad, y, inner, listHeight);
            _listTitle = CreateText(listBox, "Title", "発見した地点 0 / 0", 21, MutedColor, TextAnchor.UpperLeft);
            Place(_listTitle.rectTransform, 18f, -12f, inner - 36f, 30f);
            _listRoot = CreateRect(listBox, "List");
            Place(_listRoot, 18f, -48f, inner - 36f, listHeight - 60f);

            float half = (inner - 12f) * 0.5f;
            float upperRow = -MapSize + pad + bottomButtons * 2f + 12f;
            _styleButton = CreateButton(rect, "描き方：" + StyleName(_render.Style), BoxColor, TextColor, 21, TextAnchor.MiddleCenter);
            Place((RectTransform)_styleButton.transform, pad, upperRow, half, bottomButtons);
            _styleButton.onClick.AddListener(NextStyle);
            _styleLabel = _styleButton.GetComponentInChildren<Text>();
            _cloudButton = CreateButton(rect, "雲：" + CloudOpacityName[_cloudLevel], BoxColor, TextColor, 21, TextAnchor.MiddleCenter);
            Place((RectTransform)_cloudButton.transform, pad + half + 12f, upperRow, half, bottomButtons);
            _cloudButton.onClick.AddListener(NextCloudOpacity);
            _cloudLabel = _cloudButton.GetComponentInChildren<Text>();
            _regionButton = CreateButton(rect, "別の地域を生成", BoxColor, TextColor, 21, TextAnchor.MiddleCenter);
            Place((RectTransform)_regionButton.transform, pad, -MapSize + pad + bottomButtons, inner, bottomButtons);
            _regionButton.onClick.AddListener(NextRegion);
        }

        private void BuildMap(RectTransform root)
        {
            RectTransform frame = CreateRect(root, "Map");
            frame.anchorMin = new Vector2(1f, 0.5f);
            frame.anchorMax = new Vector2(1f, 0.5f);
            frame.pivot = new Vector2(1f, 0.5f);
            frame.sizeDelta = new Vector2(MapSize, MapSize);
            frame.anchoredPosition = Vector2.zero;
            frame.gameObject.AddComponent<RectMask2D>();

            var mapObject = new GameObject("Image", typeof(RectTransform), typeof(RawImage));
            mapObject.transform.SetParent(frame, false);
            _map = mapObject.GetComponent<RawImage>();
            _map.color = new Color32(14, 18, 24, 255);
            _mapRect = _map.rectTransform;
            _mapRect.sizeDelta = new Vector2(MapSize, MapSize);

            // 地図の上に、雲の影、雲の順で重ねる（印とカーソルはその上）
            _cloudShadows = CreateLayer(frame, "Cloud Shadows");
            _clouds = CreateLayer(frame, "Clouds");

            _markerRoot = CreateRect(frame, "Markers");
            _range = CreateImage(frame, "Search Range", new Color(1f, 1f, 1f, 0.85f), _ring).rectTransform;
            _range.GetComponent<Image>().raycastTarget = false;
            AddDarkEdge(_range.gameObject);

            // 十字は 1 枚の画像（表示の 2 倍の細かさで作り、縮めて出す）
            _cursor = CreateImage(frame, "Cursor", Color.white, _crosshair).rectTransform;
            _cursor.sizeDelta = new Vector2(48f, 48f);

            Image strip = CreateImage(frame, "Message", new Color32(12, 15, 20, 215), null);
            RectTransform stripRect = strip.rectTransform;
            stripRect.anchorMin = new Vector2(0.5f, 0f);
            stripRect.anchorMax = new Vector2(0.5f, 0f);
            stripRect.pivot = new Vector2(0.5f, 0f);
            stripRect.sizeDelta = new Vector2(760f, 64f);
            stripRect.anchoredPosition = new Vector2(0f, 28f);
            _messageGroup = strip.gameObject.AddComponent<CanvasGroup>();
            _messageGroup.blocksRaycasts = false;
            _messageGroup.interactable = false;
            _messageHideAt = -1f;
            _message = CreateText(stripRect, "Text", "", 26, TextColor, TextAnchor.MiddleCenter);
            Stretch(_message.rectTransform);
        }

        private static RawImage CreateLayer(Transform parent, string name)
        {
            var created = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            created.transform.SetParent(parent, false);
            var layer = created.GetComponent<RawImage>();
            layer.raycastTarget = false;
            layer.enabled = false;
            layer.rectTransform.sizeDelta = new Vector2(MapSize, MapSize);
            return layer;
        }

        /// <summary>白い印に暗い縁を付ける。雲・雪・地図帳風の淡い地色の上では、白だけの印が見えなくなる。</summary>
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

        private RectTransform CreateBox(RectTransform parent, string name, float x, float y, float width, float height)
        {
            Image box = CreateImage(parent, name, BoxColor, null);
            Place(box.rectTransform, x, y, width, height);
            return box.rectTransform;
        }

        private Text CreateText(Transform parent, string name, string value, int size, Color color, TextAnchor anchor)
        {
            var created = new GameObject(name, typeof(RectTransform), typeof(Text));
            created.transform.SetParent(parent, false);
            var text = created.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private Button CreateButton(Transform parent, string label, Color background, Color foreground, int size, TextAnchor anchor)
        {
            Image image = CreateImage(parent, label, background, null);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
            button.colors = colors;
            var navigation = new Navigation();
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            Text text = CreateText(image.transform, "Label", label, size, foreground, anchor);
            Stretch(text.rectTransform);
            if (anchor == TextAnchor.MiddleLeft) text.rectTransform.offsetMin = new Vector2(8f, 0f);
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
