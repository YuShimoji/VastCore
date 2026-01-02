# Windsurf Autopilot Design Notes

本ドキュメントは `scripts/ahk/windsurf_autopilot.ahk` の設計方針・アルゴリズムをまとめ、将来の改修時に参照できるようにしたものです。

## 1. 目的とスコープ
- Windsurf IDE (Cascade) の自律ワークフローを無人化し、Orchestrator/Agent の進行を止めない。
- マクロ暴走・ユーザー操作の乗っ取りを防ぎつつ、Alt+Enter／タブ巡回／指示ファイル投入などを自動制御。

## 2. コンポーネント
### 2.1 HUD レイヤー
- GUI: `Gui, +AlwaysOnTop -Caption +ToolWindow +E0x20`
- 透過ウィンドウでクリックをブロックしない。
- `HudAnchorRightPx`, `HudAnchorTopPx` により画面右上へ固定。

### 2.2 トグル制御
- **Mouse Gesture**: 右クリック押下中に左クリック (`RequireRightFirstForToggle`)。
- **Keyboard**: 任意設定 (`EnableKeyboardHotkeys`) で F8/F9。
- **Reset**: Shift+Esc (任意設定)。設定OFFの場合、手動で AHK を終了する。

### 2.3 セーフティ
- `IsSafeToAct()` で `A_TimeIdlePhysical` と修飾キー押下を監視。
- `ResetAltEnterBurst()` で Alt+Enter 状態・タブ巡回状態を完全に初期化。
- タブ巡回中や PixelSearch 中も都度 `IsSafeToAct()` を再評価し、ユーザー操作が入れば即中止。

## 3. 主なタイマーのフロー
```
TimerPulse → (Idle判定) → (オレンジ最新状態確認) → Alt+Enter送信 or Skip
TimerPatrol → 右下重点PixelSearch → 見つからなければ MaybeCycleTabs()
TimerFileWatch → next_instruction.txt 反映 → Alt+Enter
TimerCloseTabs → Git系タブを閉じる
```

### 3.1 オレンジ検知
- 検索範囲を `SearchAreaLeftRatio~BottomRatio` でパラメトリックに定義。
- 最低幅・高さを `SearchAreaMinWidthPx/MinHeightPx` で保証し、GUI縮小環境でも検知。
- `PixelSearch` の許容誤差は 15 (`Fast RGB`)。色が変動する場合はここを調整する。

### 3.2 Alt+Enter 送信制御
- `MinAltEnterIntervalMs`, `MaxAltEnterBurstCount`, `AltEnterBurstCooldownMs` により、常時連打を防止。
- `ClickOrangeBeforeAltEnter` true で Run ボタンをクリックしてから送信。

### 3.3 タブ巡回アルゴリズム
1. TimerPatrol が Run ボタンを見つけられない場合 `MaybeCycleTabs()` を呼び出す。
2. `Ctrl+PgDn` を送信、`TabCycleSteps` をインクリメント。
3. `TabCycleMaxSteps` に達したら `TabCycleOriginHwnd` を再アクティブにし、`TabCycleCooldownMs` だけ巡回を停止。
4. 巡回途中でも Run ボタンを検知すればカウンタとクールダウンをリセット。

## 4. 今後の開発アイデア
1. **バックグラウンド送信モード**: WinActivate を使わずに対象ウィンドウへメッセージを送る（SendMessage/PostMessage への置換）。
2. **レイアウト学習**: PixelSearch を補助するため、Cascade 内部の UI 要素を OCR/画像テンプレートで補完し、オレンジ色以外のトリガも検討。
3. **巡回ポリシー拡張**: `Ctrl+[ / Ctrl+]` や「Cascade: Focus on Next Thread」コマンドを取り入れ、タブ巡回と履歴巡回を切り替える。
4. **Diagnostics パネル**: HUD 近くにミニログ（最後の Alt+Enter 時刻やタブ巡回回数）を表示するオプション。
5. **ユニットテスト／シミュレーション**: `AutoHotkey.dll` を用いたシミュレーション環境、または Powershell からのモック入力で回帰テストを自動化。

## 5. ファイル関連
- スクリプト本体: `scripts/ahk/windsurf_autopilot.ahk`
- ガイド: `Windsurf_AHK_Guide.md`
- キーマップ: `Windsurf_AHK_Keymap.md`
- 将来この設計ドキュメントを更新する場合は、上記 3 つの整合性も同時に確認する。
