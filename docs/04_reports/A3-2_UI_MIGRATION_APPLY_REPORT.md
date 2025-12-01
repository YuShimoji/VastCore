# A3-2: UI Migration Apply Report (HUD-equivalent overlays)

- Scope: `Assets/Scripts/UI/`
  - InGameDebugUI.cs
  - SliderBasedUISystem.cs
  - SliderUIElement.cs
  - ModernUIStyleSystem.cs
- Date: 2025-10-06
- Author: Automation (Cascade)

## 1. Preview (Dry)

- Heuristics
  - Legacy UI detection: `UnityEngine.UI`, IMGUI `OnGUI()`
  - Text rendering: `TextMeshProUGUI`, `TextMeshPro`
  - UITK check: `UnityEngine.UIElements`, `UIDocument`
  - Namespace migration: `NarrativeGen.UI` → `Vastcore.UI.*`

- Findings
  - Namespaces
    - `InGameDebugUI.cs` → `namespace Vastcore.UI`
    - `SliderBasedUISystem.cs` → `namespace Vastcore.UI`
    - `SliderUIElement.cs` → `namespace Vastcore.UI`
    - `ModernUIStyleSystem.cs` → `namespace Vastcore.UI`
  - UI tech
    - uGUI (`UnityEngine.UI`) 使用あり（パネル、ボタン、スライダ、レイアウト等）
    - TextMeshProUGUI 使用あり
    - IMGUI (`OnGUI`) 未使用
    - UITK (`UIElements`/`UIDocument`) 未使用
  - Migration signals
    - `NarrativeGen.UI` の参照なし
    - 現在の命名規則は `Vastcore.UI` で整合済み

## 2. Apply

- Changes: なし（namespace 変更不要、API 置換なし）
- Rationale: 現時点の HUD 相当 UI は `Vastcore.UI` 命名に統一済み。uGUI 利用は許容範囲で、移行対象（NarrativeGen 系）も存在せず。

## 3. Verify (Compile & Quick Runtime)

- Compile (Editor)
  1. Unity Editor でプロジェクトを開く
  2. スクリプト再コンパイルを待機 → エラーなしを確認

- Quick Runtime Check
  1. `InGameDebugUI` が初期化されるシーンを開く（なければ空シーンに `SliderBasedUISystem` と `RealtimeUpdateSystem` を追加）
  2. Play モードに入る
  3. 確認ポイント:
     - デバッグパネルが表示され、スライダ等が操作可能
     - `F1` トグル、最小化/復帰が動作
     - パラメータ更新がスロットリングされて反映される

- Expected outcome: 動作に変更なし（ドキュメントのみの差分）

## 4. Risk & Notes

- Tier: 1（ドキュメントのみ）
- 今後の移行候補
  - uGUI → UITK 置換の技術検証（別スプリント）
  - 既存 `SliderBasedUISystem` のプレハブ化/アセット化

## 5. Artifacts

- Report: `Documentation/QA/A3-2_UI_MIGRATION_APPLY_REPORT.md`
- Branch: feature (see PR)
