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

        [Tooltip("サーチの規則。Expedition＝拠点から日数を使って足を伸ばし、手掛かりを読んで探す（SP-021 軸 H）。Signal＝反応の強さだけ（前の規則）。")]
        [SerializeField] private SearchRules _rules = SearchRules.Expedition;

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
        // 遠征の規則の部品（左の「遠征の残り」と、右の「手掛かり」）
        private const float SignalHeight = 134f;
        private const float ExpeditionHeight = 118f;
        private const int MaxClueCards = 4;
        // 右の手掛かりの部品が覆う幅。遠征の規則では、地図の東端もこの幅だけ内側へ引き出せる
        private const float RightInset = Margin + PartWidth + Margin;
        private const float EndWidth = 560f;
        private const float EndHeight = 236f;

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

        // 遠征の規則（RegionExpedition）
        private RegionExpedition _expedition;
        private CompassPoint _lastDirection = CompassPoint.None;
        private Vector2 _lastSearchMap;
        private ExpeditionCost _cursorCost;
        private Vector2 _cursorCostAt = new Vector2(-1f, -1f);
        private int _cursorCostVersion = -1;
        private int _clueVersion = -1;
        private Sprite _triangle;
        private RectTransform _expeditionPart;
        private RectTransform _signalPart;
        private float _partsTop;
        private Text _daysValue;
        private Text _daysOf;
        private Text _daysNote;
        private RectTransform _daysFill;
        private Text _costText;
        private Text _searchLabel;
        private RectTransform _searchKeyCap;
        private RectTransform _cluePart;
        private Text _clueHeader;
        private readonly List<GameObject> _clueItems = new List<GameObject>();
        private RegionRingGraphic _campRing;
        private RectTransform _campLine;
        private RectTransform _startCamp;
        private RectTransform _arrow;
        private RectTransform _endPanel;
        private Text _endTitle;
        private Text _endStats;

        public RegionData Region
        {
            get { return _data; }
        }

        public ExplorationSession Session
        {
            get { return _session; }
        }

        /// <summary>今のサーチの規則。</summary>
        public SearchRules Rules
        {
            get { return _rules; }
        }

        /// <summary>遠征の規則の状態（日数・拠点・手掛かり）。地域を作るまでは null。</summary>
        public RegionExpedition Expedition
        {
            get { return _expedition; }
        }

        /// <summary>直前のサーチで出た方角（強い・弱い反応の時）。</summary>
        public CompassPoint LastDirection
        {
            get { return _lastDirection; }
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
            _triangle = CreateTriangleSprite(64);
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
            if (_rules == SearchRules.Expedition) return SearchOnExpedition();
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

        /// <summary>遠征の規則でサーチする。日数を使い、反応に方角が付き、発見した地点が拠点になる。</summary>
        private SearchResult SearchOnExpedition()
        {
            if (_expedition.IsOver)
            {
                ShowEnd();
                return new SearchResult();
            }
            ExpeditionStep step = _expedition.Search(_cursorMap.x, _cursorMap.y);
            if (!step.Done)
            {
                ShowMessage(step.Refusal, 4f);
                return new SearchResult();
            }
            HideCard();
            SearchResult result = step.Result;
            _lastSignal = result.Signal;
            _lastDirection = step.Direction;
            _lastSearchMap = _cursorMap;
            string days = "（" + step.DaysSpent + " 日）";
            string toward = RegionCompass.Label(step.Direction);
            switch (result.Signal)
            {
                case SearchSignal.Found:
                    AddDiscovered(result.Site);
                    ShowCard(result.Site);
                    ExpeditionReward reward = step.Reward;
                    int opened = reward != null ? reward.OpenedClues.Count : 0;
                    ShowMessage("拠点になった。日数 +" + (reward != null ? reward.Days : 0) + (opened > 0 ? "・手掛かりが " + opened + " 件読める" : string.Empty), 5f);
                    break;
                case SearchSignal.Strong:
                    ShowMessage("強い反応。" + toward + "のすぐ近くに何かある" + days, 4f);
                    break;
                case SearchSignal.Faint:
                    ShowMessage("弱い反応。" + toward + "の方に何かある" + days, 4f);
                    break;
                default:
                    ShowMessage("反応なし" + days, 3f);
                    break;
            }
            if (step.EndedNow) ShowEnd();
            RefreshClues();
            RefreshReadout();
            return result;
        }

        /// <summary>サーチの規則を替える。発見済みと日数は消して、はじめからにする。</summary>
        public void SetRules(SearchRules rules)
        {
            _rules = rules;
            if (_session != null) RestartExpedition();
            ApplyRules();
            if (IsReady) ShowMessage(rules == SearchRules.Expedition ? "規則：遠征（日数・拠点・手掛かり）" : "規則：反応だけ（前の規則）", 4f);
        }

        /// <summary>同じ地域で、遠征をはじめからやり直す。</summary>
        public void RestartExpedition()
        {
            if (_session == null) return;
            if (_expedition != null) _expedition.Begin();
            else _session.Reset();
            ClearDiscovered();
            _lastSignal = SearchSignal.None;
            _lastDirection = CompassPoint.None;
            HideEnd();
            _clueVersion = -1;
            RefreshClues();
            RefreshReadout();
        }

        /// <summary>手掛かりを詳しく読む（日数を使う）。</summary>
        public bool ReadClue(RegionClue clue)
        {
            if (_expedition == null || _rules != SearchRules.Expedition) return false;
            bool done = _expedition.ReadClue(clue);
            ShowMessage(done ? "手掛かりを読んだ（" + _expedition.ReadDays + " 日）" : "日数が足りない", 3f);
            if (_expedition.IsOver) ShowEnd();
            RefreshClues();
            RefreshReadout();
            return done;
        }

        /// <summary>遠征の終わりの結果を画面の中央に出す。</summary>
        private void ShowEnd()
        {
            if (_expedition == null) return;
            _endTitle.text = _expedition.Completed ? "すべての地点を見つけた" : "遠征の終わり";
            _endStats.text = "発見 " + _session.Discovered.Count + " / " + _session.TotalSites
                + "　・　サーチ " + _session.SearchCount + " 回　・　拠点 " + _expedition.Camps.Count + "\n"
                + "使った日数 " + _expedition.DaysUsed + "　・　発見で戻った日数 " + _expedition.DaysGained + "　・　残り " + _expedition.DaysLeft + " 日";
            _endPanel.gameObject.SetActive(true);
            _arrow.gameObject.SetActive(false);
        }

        private void HideEnd()
        {
            if (_endPanel != null) _endPanel.gameObject.SetActive(false);
        }

        /// <summary>規則に合わせて、遠征の部品と地図の上の印を出し入れし、左の部品を並べ直す。</summary>
        private void ApplyRules()
        {
            bool expedition = _rules == SearchRules.Expedition;
            _expeditionPart.gameObject.SetActive(expedition);
            _cluePart.gameObject.SetActive(expedition && _data != null);
            if (!expedition)
            {
                HideEnd();
                SetSearchLabel("サーチ");
                if (_task == null && _data != null) _searchButton.interactable = true;
            }
            LayoutParts();
            if (_data != null) ClampView();
        }

        /// <summary>左の部品を上から並べる（遠征の規則では、反応の部品の上に「遠征の残り」を入れる）。</summary>
        private void LayoutParts()
        {
            float y = _partsTop;
            if (_rules == SearchRules.Expedition)
            {
                _expeditionPart.anchoredPosition = new Vector2(Margin, y);
                y -= ExpeditionHeight + PartGap;
            }
            _signalPart.anchoredPosition = new Vector2(Margin, y);
            y -= SignalHeight + PartGap;
            _listTop = -y;
            _listPart.anchoredPosition = new Vector2(Margin, y);
            LayoutList();
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
                _expedition = new RegionExpedition(_session);
                _lastSignal = SearchSignal.None;
                _lastDirection = CompassPoint.None;
                _clueVersion = -1;
                _cursorCostVersion = -1;
                HideEnd();
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
                ShowMessage(_rules == SearchRules.Expedition
                    ? "野営地から " + _expedition.StartDays + " 日の遠征。手掛かりを読み、地図をクリックしてサーチする"
                    : "地図をクリックして位置を決め、サーチで調べる", 8f);
            }
            else
            {
                ShowMessage("描き方：" + StyleName(_render.Style), 3f);
            }
            _cursor.gameObject.SetActive(true);
            _range.gameObject.SetActive(true);
            SetButtons(true);
            ApplyRules();
            RefreshCloudLayers();
            RefreshAreaLayers();
            RefreshClues();
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
            // （遠征の規則では、右の手掛かりの部品の左端まで入れる。部品の下の地点を引き出して触れる）
            float right = ClueShown() ? RightInset : 0f;
            float open = view.x - LeftInset - right;
            float west = -_center.x * side;
            if (side <= open) west = -view.x * 0.5f + LeftInset + (open - side) * 0.5f;
            else west = Mathf.Clamp(west, view.x * 0.5f - right - side, -view.x * 0.5f + LeftInset);
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
            float right = view.x * 0.5f - (ClueShown() ? RightInset + 60f : 140f);
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

        /// <summary>右の手掛かりの部品を出しているか（遠征の規則の時）。</summary>
        private bool ClueShown()
        {
            return _rules == SearchRules.Expedition && _cluePart != null && _cluePart.gameObject.activeSelf;
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
            RefreshExpeditionMarks(side, diameter * 0.5f);
            PlaceCard();
            RefreshLabelCover();
        }

        /// <summary>
        /// 遠征の印。出発の野営地、カーソルにいちばん近い拠点の輪（この中は移動の日数が 0）と、その拠点からカーソルへの線、
        /// 直前のサーチの方角の矢印（サーチした位置の輪の外に出す）。
        /// </summary>
        private void RefreshExpeditionMarks(float side, float rangeRadius)
        {
            bool on = _rules == SearchRules.Expedition && _expedition != null;
            bool live = on && !_expedition.IsOver;
            _startCamp.gameObject.SetActive(on);
            if (on) _startCamp.anchoredPosition = MapToLocal(new Vector2(_expedition.StartCamp.MapX, _expedition.StartCamp.MapY));

            ExpeditionCost cost = live ? CursorCost() : null;
            bool ring = cost != null && cost.Camp != null;
            _campRing.gameObject.SetActive(ring);
            _campLine.gameObject.SetActive(ring && cost.CampDistance > _session.FoundRadius);
            if (ring)
            {
                Vector2 camp = MapToLocal(new Vector2(cost.Camp.MapX, cost.Camp.MapY));
                float radius = _expedition.CampStep / RegionData.MapUnits * side;
                _campRing.rectTransform.anchoredPosition = camp;
                _campRing.rectTransform.sizeDelta = new Vector2(radius * 2f, radius * 2f);
                // 線は拠点の印の外から、カーソルの輪の外まで
                Vector2 to = _cursor.anchoredPosition;
                Vector2 span = to - camp;
                float length = span.magnitude;
                Vector2 unit = length > 0.001f ? span / length : Vector2.right;
                float trimmed = Mathf.Max(0f, length - 18f - rangeRadius);
                _campLine.anchoredPosition = camp + unit * 18f;
                _campLine.sizeDelta = new Vector2(trimmed, 2f);
                _campLine.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(span.y, span.x) * Mathf.Rad2Deg);
            }

            bool arrow = live && _lastDirection != CompassPoint.None;
            _arrow.gameObject.SetActive(arrow);
            if (arrow)
            {
                float degrees = RegionCompass.Degrees(_lastDirection);
                float radians = degrees * Mathf.Deg2Rad;
                var unit = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                _arrow.anchoredPosition = MapToLocal(_lastSearchMap) + unit * (rangeRadius + 10f);
                _arrow.localRotation = Quaternion.Euler(0f, 0f, degrees);
            }
        }

        /// <summary>カーソル位置でサーチした時の日数。カーソルと遠征の状態が変わった時だけ数え直す。</summary>
        private ExpeditionCost CursorCost()
        {
            if (_expedition == null) return null;
            if (_cursorCost == null || _cursorCostAt != _cursorMap || _cursorCostVersion != _expedition.Version)
            {
                _cursorCost = _expedition.CostAt(_cursorMap.x, _cursorMap.y);
                _cursorCostAt = _cursorMap;
                _cursorCostVersion = _expedition.Version;
            }
            return _cursorCost;
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
            bool expedition = _rules == SearchRules.Expedition && _expedition != null;
            string toward = expedition && !_liveSignal ? RegionCompass.Label(_lastDirection) : string.Empty;
            _signalText.text = SignalName(signal) + (toward.Length > 0 ? "・" + toward : string.Empty);
            _listCount.text = _session.Discovered.Count + " / " + _session.TotalSites;
            _searchCount.text = "サーチ " + _session.SearchCount + " 回";
            if (expedition) RefreshExpeditionReadout();
        }

        /// <summary>遠征の残りの日数、カーソル位置でサーチした時の日数と内訳、サーチボタンの文字。</summary>
        private void RefreshExpeditionReadout()
        {
            RegionExpedition ex = _expedition;
            _daysValue.text = ex.DaysLeft.ToString(CultureInfo.InvariantCulture);
            float valueWidth = _daysValue.preferredWidth;
            _daysValue.rectTransform.sizeDelta = new Vector2(valueWidth, _daysValue.rectTransform.sizeDelta.y);
            _daysOf.rectTransform.anchoredPosition = new Vector2(PartPad + valueWidth + 6f, _daysOf.rectTransform.anchoredPosition.y);
            _daysOf.text = "日　／ 始め " + ex.StartDays + " 日";
            _daysNote.text = "発見で +" + ex.FindDays + " 日　・　拠点 " + ex.Camps.Count;
            float fill = Mathf.Clamp01(ex.DaysLeft / (float)Mathf.Max(1, ex.StartDays));
            _daysFill.sizeDelta = new Vector2(PartInner * fill, 0f);

            if (ex.IsOver)
            {
                _costText.text = "遠征は終わった。下の結果から、もう一度か別の地域へ";
                SetSearchLabel("遠征の終わり");
                if (_task == null) _searchButton.interactable = false;
                return;
            }
            ExpeditionCost cost = CursorCost();
            var parts = new List<string>(cost.Parts.Count);
            for (int p = 0; p < cost.Parts.Count; p++) parts.Add(cost.Parts[p].Label + " " + cost.Parts[p].Days);
            bool enough = cost.Total <= ex.DaysLeft;
            _costText.text = "カーソル位置は " + cost.Total + " 日＝" + string.Join(" ＋ ", parts.ToArray()) + (enough ? string.Empty : "（足りない）");
            _costText.color = enough ? MutedColor : AccentLightColor;
            SetSearchLabel("サーチ　" + cost.Total + " 日");
            if (_task == null) _searchButton.interactable = enough;
        }

        /// <summary>サーチボタンの文字を替え、キーの札と一緒に中央へ並べ直す。</summary>
        private void SetSearchLabel(string text)
        {
            if (_searchLabel == null || _searchLabel.text == text) return;
            _searchLabel.text = text;
            float labelWidth = _searchLabel.preferredWidth;
            float keyWidth = _searchKeyCap.sizeDelta.x;
            float start = (PartInner - (labelWidth + 12f + keyWidth)) * 0.5f;
            Place(_searchLabel.rectTransform, start, 0f, labelWidth, 50f);
            Place(_searchKeyCap, start + labelWidth + 12f, -12f, keyWidth, 26f);
        }

        // ---- 手掛かり ----

        /// <summary>右の手掛かりの部品を、遠征の状態が変わった時だけ作り直す。</summary>
        private void RefreshClues()
        {
            if (_cluePart == null) return;
            bool show = _rules == SearchRules.Expedition && _expedition != null;
            if (_cluePart.gameObject.activeSelf != show) _cluePart.gameObject.SetActive(show);
            if (!show || _clueVersion == _expedition.Version) return;
            _clueVersion = _expedition.Version;

            for (int i = 0; i < _clueItems.Count; i++) Destroy(_clueItems[i]);
            _clueItems.Clear();
            List<RegionClue> open = _expedition.OpenClues();
            _clueHeader.text = "手掛かり（読める " + open.Count + " 件）";
            float y = -40f;
            int shown = Mathf.Min(MaxClueCards, open.Count);
            for (int c = 0; c < shown; c++)
            {
                RegionClue clue = open[c];
                if (c > 0)
                {
                    Image rule = CreateImage(_cluePart, "Clue Rule", LineColor, null);
                    Place(rule.rectTransform, PartPad, y, PartInner, 1f);
                    _clueItems.Add(rule.gameObject);
                    y -= 9f;
                }
                Text title = CreateText(_cluePart, "Clue Title", clue.Title, 19, TextColor, TextAnchor.UpperLeft, clue.Title.Contains("?") ? _numberFont : _font);
                Place(title.rectTransform, PartPad, y, PartInner, 26f);
                _clueItems.Add(title.gameObject);
                y -= 28f;
                y = AddClueText(clue.NameLine + "\n" + clue.PlaceLine, MutedColor, y);
                if (clue.State == ClueState.Read)
                {
                    y = AddClueText(clue.DetailLine, AccentLightColor, y);
                }
                else
                {
                    y -= 2f;
                    Button read = CreateButton(_cluePart, "Clue Read", new Color(PartColor.r, PartColor.g, PartColor.b, 0.2f));
                    RectTransform rect = (RectTransform)read.transform;
                    Text label = CreateText(rect, "Label", "詳しく読む　" + _expedition.ReadDays + " 日", 14, AccentLightColor, TextAnchor.MiddleCenter, _font);
                    float width = label.preferredWidth + 24f;
                    Place(rect, PartPad, y, width, 24f);
                    Stretch(label.rectTransform);
                    AddFrame(rect, new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.85f));
                    RegionClue target = clue;
                    read.onClick.AddListener(() => ReadClue(target));
                    read.interactable = _expedition.DaysLeft >= _expedition.ReadDays && !_expedition.IsOver;
                    _clueItems.Add(read.gameObject);
                    y -= 30f;
                }
                y -= 6f;
            }
            string foot = open.Count == 0
                ? "読める手掛かりは無い。発見すると、近くの地点の手掛かりが読める"
                : open.Count > shown ? "ほか " + (open.Count - shown) + " 件（先に読めた順）" : "発見すると、近くの地点の手掛かりが増える";
            Text footer = CreateText(_cluePart, "Clue Footer", foot, 13, MutedColor, TextAnchor.UpperLeft, _font, false);
            Place(footer.rectTransform, PartPad, y - 2f, PartInner, 20f);
            FitText(footer, 10, 13);
            _clueItems.Add(footer.gameObject);
            y -= 28f;
            _cluePart.sizeDelta = new Vector2(PartWidth, -y);
        }

        /// <summary>手掛かりの文を、部品の幅で折り返して置き、次の行の位置を返す。</summary>
        private float AddClueText(string value, Color color, float y)
        {
            Text text = CreateText(_cluePart, "Clue Text", value, 14, color, TextAnchor.UpperLeft, _font);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.lineSpacing = 1.05f;
            Place(text.rectTransform, PartPad, y, PartInner, 20f);
            float height = Mathf.Ceil(text.preferredHeight) + 2f;
            text.rectTransform.sizeDelta = new Vector2(PartInner, height);
            _clueItems.Add(text.gameObject);
            return y - height - 2f;
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
            // 右の手掛かりの部品に掛かる時は、印の左側へ出す
            float rightEdge = view.x * 0.5f - (ClueShown() ? RightInset - Margin + PartGap : Margin);
            bool right = at.x + 64f + CardWidth <= rightEdge;
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
            BuildEndPanel(canvasObject.transform);
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
            BuildExpeditionMarks(_viewRect);
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

        /// <summary>遠征の印（拠点の輪、拠点からカーソルへの線、出発の野営地、方角の矢印）。発見した地点の印より下に置く。</summary>
        private void BuildExpeditionMarks(RectTransform parent)
        {
            var ringObject = new GameObject("Camp Range", typeof(RectTransform), typeof(RegionRingGraphic));
            ringObject.transform.SetParent(parent, false);
            _campRing = ringObject.GetComponent<RegionRingGraphic>();
            _campRing.raycastTarget = false;
            _campRing.Thickness = 1.5f;
            _campRing.Rim = 1f;
            _campRing.RimColor = new Color(0f, 0f, 0f, 0.35f);
            _campRing.color = new Color(1f, 1f, 1f, 0.55f);
            ringObject.SetActive(false);

            _campLine = CreateImage(parent, "Camp Line", new Color(AccentLightColor.r, AccentLightColor.g, AccentLightColor.b, 0.8f), null).rectTransform;
            _campLine.pivot = new Vector2(0f, 0.5f);
            AddDarkEdge(_campLine.gameObject);
            _campLine.gameObject.SetActive(false);

            // 出発の野営地：上向きの白い三角と、その右の小さな名前
            _startCamp = CreateRect(parent, "Start Camp");
            Image tent = CreateImage(_startCamp, "Mark", Color.white, _triangle);
            tent.rectTransform.sizeDelta = new Vector2(24f, 24f);
            tent.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            AddDarkEdge(tent.gameObject);
            Text campName = CreateText(_startCamp, "Name", "野営地", 15, TextColor, TextAnchor.MiddleLeft, _font);
            Outline edge = campName.gameObject.AddComponent<Outline>();
            edge.effectColor = new Color(0f, 0f, 0f, 0.9f);
            edge.effectDistance = new Vector2(1.2f, -1.2f);
            Place(campName.rectTransform, 0f, 0f, 80f, 22f);
            campName.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            campName.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            campName.rectTransform.pivot = new Vector2(0f, 0.5f);
            campName.rectTransform.anchoredPosition = new Vector2(16f, 0f);
            _startCamp.gameObject.SetActive(false);

            // 方角の矢印：軸と三角の頭。右向きに作り、方位の角度だけ回す
            _arrow = CreateRect(parent, "Direction");
            _arrow.pivot = new Vector2(0f, 0.5f);
            Image shaft = CreateImage(_arrow, "Shaft", AccentColor, null);
            shaft.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            shaft.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            shaft.rectTransform.pivot = new Vector2(0f, 0.5f);
            shaft.rectTransform.sizeDelta = new Vector2(30f, 5f);
            shaft.rectTransform.anchoredPosition = Vector2.zero;
            AddDarkEdge(shaft.gameObject);
            Image head = CreateImage(_arrow, "Head", AccentColor, _triangle);
            head.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            head.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            head.rectTransform.pivot = new Vector2(0f, 0.5f);
            head.rectTransform.sizeDelta = new Vector2(24f, 24f);
            head.rectTransform.anchoredPosition = new Vector2(24f, 0f);
            AddDarkEdge(head.gameObject);
            _arrow.gameObject.SetActive(false);
        }

        /// <summary>右向きの三角（矢印の頭と、上へ回して野営地の印に使う）。4×4 の点で縁をなめらかにする。</summary>
        private static Sprite CreateTriangleSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            float tip = size - 3f;
            float back = 3f;
            float half = size * 0.5f - 3f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < 4; sy++)
                    {
                        for (int sx = 0; sx < 4; sx++)
                        {
                            float px = x + (sx + 0.5f) / 4f;
                            float py = y + (sy + 0.5f) / 4f - size * 0.5f;
                            if (px < back || px > tip) continue;
                            float allowed = half * (tip - px) / (tip - back);
                            if (Mathf.Abs(py) <= allowed) inside++;
                        }
                    }
                    pixels[x + y * size] = new Color32(255, 255, 255, (byte)(inside * 255 / 16));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
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
            _partsTop = y;

            // 遠征の残り（遠征の規則の時だけ。位置は LayoutParts が決める）
            _expeditionPart = CreatePart(canvas, "Expedition", y, ExpeditionHeight);
            Text daysLabel = CreateText(_expeditionPart, "Label", "遠征の残り", 15, MutedColor, TextAnchor.UpperLeft, _font);
            Place(daysLabel.rectTransform, PartPad, -10f, 140f, 22f);
            _daysNote = CreateText(_expeditionPart, "Note", "", 15, MutedColor, TextAnchor.UpperRight, _font);
            Place(_daysNote.rectTransform, PartPad + PartInner - 230f, -10f, 230f, 22f);
            _daysValue = CreateText(_expeditionPart, "Days", "--", 40, TextColor, TextAnchor.LowerLeft, _numberFont);
            Place(_daysValue.rectTransform, PartPad, -28f, 60f, 46f);
            _daysOf = CreateText(_expeditionPart, "Days Of", "日", 16, MutedColor, TextAnchor.LowerLeft, _font);
            Place(_daysOf.rectTransform, PartPad + 66f, -30f, 220f, 40f);
            Image daysTrack = CreateImage(_expeditionPart, "Days Track", OffColor, null);
            Place(daysTrack.rectTransform, PartPad, -80f, PartInner, 6f);
            _daysFill = CreateImage(daysTrack.rectTransform, "Days Fill", AccentColor, null).rectTransform;
            _daysFill.anchorMin = new Vector2(0f, 0f);
            _daysFill.anchorMax = new Vector2(0f, 1f);
            _daysFill.pivot = new Vector2(0f, 0.5f);
            _daysFill.sizeDelta = new Vector2(PartInner, 0f);
            _costText = CreateText(_expeditionPart, "Cost", "", 14, MutedColor, TextAnchor.UpperLeft, _font);
            Place(_costText.rectTransform, PartPad, -92f, PartInner, 20f);
            FitText(_costText, 10, 14);

            // 反応とサーチ
            RectTransform signal = CreatePart(canvas, "Signal", y, SignalHeight);
            _signalPart = signal;
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
            y -= SignalHeight + PartGap;

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

            BuildCluePart(canvas);
            _expeditionPart.gameObject.SetActive(_rules == SearchRules.Expedition);
            LayoutParts();
        }

        /// <summary>右の手掛かりの部品。右上の札の下に置き、中身は RefreshClues が作る。</summary>
        private void BuildCluePart(Transform canvas)
        {
            _cluePart = CreatePart(canvas, "Clues", 0f, 120f);
            _cluePart.anchorMin = new Vector2(1f, 1f);
            _cluePart.anchorMax = new Vector2(1f, 1f);
            _cluePart.pivot = new Vector2(1f, 1f);
            _cluePart.anchoredPosition = new Vector2(-Margin, -(Margin + DetailBadgeHeight + PartGap));
            _clueHeader = CreateText(_cluePart, "Label", "手掛かり", 15, MutedColor, TextAnchor.UpperLeft, _font);
            Place(_clueHeader.rectTransform, PartPad, -10f, 240f, 22f);
            Text provisional = CreateText(_cluePart, "Provisional", "文は仮", 14, MutedColor, TextAnchor.UpperRight, _font);
            Place(provisional.rectTransform, PartPad + PartInner - 100f, -10f, 100f, 22f);
            _cluePart.gameObject.SetActive(false);
        }

        /// <summary>遠征の終わりの結果。画面の中央に出す。</summary>
        private void BuildEndPanel(Transform canvas)
        {
            Image panel = CreateImage(canvas, "Expedition End", new Color(CardColor.r, CardColor.g, CardColor.b, 0.9f), null);
            panel.raycastTarget = true;
            _endPanel = panel.rectTransform;
            _endPanel.anchorMin = new Vector2(0.5f, 0.5f);
            _endPanel.anchorMax = new Vector2(0.5f, 0.5f);
            _endPanel.pivot = new Vector2(0.5f, 0.5f);
            _endPanel.sizeDelta = new Vector2(EndWidth, EndHeight);
            _endPanel.anchoredPosition = new Vector2((LeftInset - RightInset) * 0.5f, 40f);
            AddFrame(_endPanel, AccentColor);
            Text eyebrow = CreateText(_endPanel, "Eyebrow", "遠征の結果", 15, AccentLightColor, TextAnchor.UpperLeft, _font);
            Place(eyebrow.rectTransform, 24f, -16f, EndWidth - 48f, 22f);
            _endTitle = CreateText(_endPanel, "Title", "遠征の終わり", 30, TextColor, TextAnchor.UpperLeft, _font);
            Place(_endTitle.rectTransform, 24f, -40f, EndWidth - 48f, 42f);
            _endStats = CreateText(_endPanel, "Stats", "", 17, TextColor, TextAnchor.UpperLeft, _font);
            _endStats.lineSpacing = 1.25f;
            Place(_endStats.rectTransform, 24f, -92f, EndWidth - 48f, 60f);

            float buttonWidth = (EndWidth - 48f - 12f) * 0.5f;
            Button again = CreateButton(_endPanel, "Again", new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.92f));
            RectTransform againRect = (RectTransform)again.transform;
            Place(againRect, 24f, -170f, buttonWidth, 44f);
            Text againLabel = CreateText(againRect, "Label", "同じ地域でもう一度", 18, AccentTextColor, TextAnchor.MiddleCenter, _font, false);
            againLabel.fontStyle = FontStyle.Bold;
            Stretch(againLabel.rectTransform);
            again.onClick.AddListener(RestartExpedition);

            Button other = CreateButton(_endPanel, "Other Region", PartColor);
            RectTransform otherRect = (RectTransform)other.transform;
            Place(otherRect, 24f + buttonWidth + 12f, -170f, buttonWidth, 44f);
            AddFrame(otherRect, LineColor);
            Text otherLabel = CreateText(otherRect, "Label", "別の地域", 18, TextColor, TextAnchor.MiddleCenter, _font);
            Stretch(otherLabel.rectTransform);
            other.onClick.AddListener(NextRegion);
            _endPanel.gameObject.SetActive(false);
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
            // 遠征の規則では「サーチ　3 日」のように文字が替わる（SetSearchLabel が並べ直す）
            _searchLabel = label;
            _searchKeyCap = cap;
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
