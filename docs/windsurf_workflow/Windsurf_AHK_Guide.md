# Windsurf Autopilot Guide

Windsurf IDE の自律走行を補助する AutoHotkey スクリプト（`scripts/ahk/windsurf_autopilot.ahk`）の利用ガイド。IDE オーケストレーション中でも参照しやすいよう「導入・操作・安全」ポイントを 1 枚にまとめています。

## 1. 概要
- **入力自動化**: Alt+Enter 送信、ファイル指示の投下、不要タブの自動クローズ。
- **マルチスレッド巡回**: Cascade の各スレッドを巡回し、オレンジ色の Run ボタンを検出してフォーカス。
- **ユーザー操作優先**: `A_TimeIdlePhysical` を監視し、マウス/キー操作中は介入を抑止。
- **安全トグル**: 右→左同時クリックまたは（任意設定の）F8/F9 で ON/OFF。

## 2. 導入手順
1. **管理者権限で実行**: AHK v1.1 をインストール後、`windsurf_autopilot.ahk` をダブルクリック。UAC プロンプトは「はい」。
2. **UTF-8(BOM) 保存**: 日本語は使っていないが、要件に合わせ編集時は UTF-8(BOM) を維持。
3. **設定値編集**: ファイル先頭の `global` 定義で挙動を調整（例: `AutoStartWhenWindsurfRunning := true`）。

## 3. 操作方法
| 操作 | 既定値 | 説明 |
| --- | --- | --- |
| 自動開始 | `AutoStartWhenWindsurfRunning := false` | true にすると Windsurf プロセス検知で自動 ON |
| 右→左クリック | 有効 | マウスのみで AUTO ON/OFF を切替。`RequireRightFirstForToggle` で制御 |
| F8/F9 | `EnableKeyboardHotkeys := false` | true にすれば F8=開始、F9=停止（Windsurf アクティブ時のみ） |
| Shift+Esc | `EnableResetHotkey := true` | 緊急停止＆状態リセット。不要なら false |

HUD は画面右上（`HudAnchorRightPx`, `HudAnchorTopPx`）に常駐し、状態を色で表示します。

## 4. Timers / 主な機能
- **TimerPulse**: オレンジ検知結果を基に Alt+Enter 送信。最小間隔、バースト上限、クールダウンを内蔵。
- **TimerPatrol**: 各 Windsurf ウィンドウを巡回し、Run ボタンを広範囲（右下中心）で PixelSearch。見つからない場合はタブ巡回を呼び出し。
- **TimerFileWatch**: `docs/inbox/next_instruction.txt` を監視し、内容を貼り付け→Alt+Enter→ファイル削除。
- **TimerCloseTabs**: `Commit/Index/Merge` 等の一時タブを自動で `Ctrl+W`。

## 5. タブ巡回（Cascade 対応）
- `EnableTabCycle := true` で有効。
- Run ボタンが見当たらない場合、`Ctrl+PgDn` を最大 `TabCycleMaxSteps` 回送信し、起点タブへ戻る。
- `TabCycleCooldownMs` により頻繁な切替を抑止。

## 6. 安全上の注意
- `IdleThresholdMs` を短くしすぎると手動操作と競合します。700ms〜1s を推奨。
- `ClickOrangeBeforeAltEnter` を false にすると、フォーカスだけ当てて Alt+Enter を送るモードになります。
- タブ巡回中も `IsSafeToAct()` を再チェックするため、ユーザー操作が入ったら即停止します。

## 7. トラブルシューティング
| 症状 | 対応 |
| --- | --- |
| HUD が現れない / `Invalid option` | 先頭の `HudAnchor*` を編集後、`Gui, Show` を直接計算せず変数を挟んでいるか確認 |
| Alt+Enter が連打される | `MinAltEnterIntervalMs` を上げる、`MaxAltEnterBurstCount` を下げる |
| Run ボタンを見失う | `SearchArea*` 比率を調整し、右下寄りのエリアを広げる |
| タブ巡回が止まらない | `TabCycleMaxSteps` を増減し、終了時に HUD が OFF になるか確認 |

## 8. 参考
- スクリプト: `scripts/ahk/windsurf_autopilot.ahk`
- キーマップ・設計資料: `Windsurf_AHK_Keymap.md`, `Windsurf_AHK_Design.md`
