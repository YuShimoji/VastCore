# Windsurf Autopilot Keymap Reference

オートパイロットで利用するホットキー／スイッチの一覧。エンドユーザーが `scripts/ahk/windsurf_autopilot.ahk` の `global` 設定を調整する際のリファレンスとして使用する。

## 1. グローバル設定と意味
| 変数 | 既定値 | 説明 |
| --- | --- | --- |
| `EnableKeyboardHotkeys` | `false` | true で F8/F9 を有効 (Windsurf アクティブ時のみ) |
| `EnableResetHotkey` | `true` | true で Shift+Esc を緊急リセットに割り当て |
| `RequireRightFirstForToggle` | `true` | 右→左の順序検知で誤爆を防止 |
| `RightFirstWindowMs` | `350` | 右押下→左押下の許容時間 (ms) |
| `AutoStartWhenWindsurfRunning` | `false` | true で Windsurf.exe 検出時に自動スタート |
| `AutoStartWhenWindsurfActive` | `false` | true で Windsurf が前面になった瞬間に自動スタート |
| `IdleThresholdMs` | `700` | ユーザー操作判定。これより短いと介入しやすくなる |
| `HudAnchorRightPx / HudAnchorTopPx` | `120 / 40` | HUD の右上からのオフセット |

## 2. ホットキー一覧
| 入力 | 有効条件 | 動作 |
| --- | --- | --- |
| **右クリック押下＋左クリック** | 常時 (Windsurf アクティブ) | AUTO ON/OFF トグル。`ClickToggleCooldownMs` で連打抑制 |
| **F8** | `EnableKeyboardHotkeys := true` | AutoStart。`IsWindsurfActive()` が true のときのみ受け付け |
| **F9** | `EnableKeyboardHotkeys := true` | AutoStop |
| **Shift + Esc** | `EnableResetHotkey := true` | AutoReset（状態リセット＋ AUTO OFF） |
| **Ctrl + PgDn** | `EnableTabCycle := true` かつ Run ボタン未検知 | TimerPatrol/MaybeCycleTabs から送信され、次スレッドへ移動 |
| **Alt + Enter** | AUTO ON で条件成立時 | システムが自動送信。`MinAltEnterIntervalMs`/`MaxAltEnterBurstCount` で制御 |
| **Ctrl + W** | 不要タブ検出時 | Gitタブなど一時ビューを自動クローズ |

## 3. Mouse Gesture Flow
1. 右クリックを押し続ける。
2. 350ms 以内に左クリックを押す。
3. HUD の色が変わり状態が切り替わる。

`ClickToggleCooldownMs`（既定 650ms）より短い間隔で連続トグルはできない。

## 4. タブ巡回ロジック
- `EnableTabCycle := true` の場合、Run ボタンが取得できないと `Ctrl+PgDn` を送信。
- `TabCycleMaxSteps` 回で打ち切り、`TabCycleOriginHwnd` を再アクティブ化。
- `TabCycleCooldownMs` 経過まで再巡回しない。

## 5. 調整の指針
- **キー衝突回避**: F8/F9 を使う場合は VS Code のデフォルト (F8=次の問題) との重複に注意。
- **自動開始**: デフォルトは OFF。チーム方針に合わせて `AutoStartWhenWindsurfRunning` を true にするなど、AI が常に走って良い場面でのみ有効化。
- **緊急停止**: Shift+Esc 以外に、タスクマネージャで AHK を落とす手段も周知しておく。

## 6. 参照
- ガイド: `Windsurf_AHK_Guide.md`
- 設計ドキュメント: `Windsurf_AHK_Design.md`
