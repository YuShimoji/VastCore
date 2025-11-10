# UI Migration Notes (A3-1)

## 概要

- 目的: 旧 `NarrativeGen.UI` を `Vastcore.UI` に移行（GUID維持の最小差分）
- 方針: ファイル名やGUIDは変更せず、namespace のみを変更。シーン/Prefab参照を壊さない安全移行。

## 対象ファイル（namespace変更）

- `Assets/Scripts/UI/InGameDebugUI.cs` → `namespace Vastcore.UI`
- `Assets/Scripts/UI/ModernUIManager.cs` → `namespace Vastcore.UI`
- `Assets/Scripts/UI/ModernUIStyleSystem.cs` → `namespace Vastcore.UI`
  - 併せて `CreateAssetMenu` を `Vastcore/UI/...` に統一
- `Assets/Scripts/UI/PerformanceMonitor.cs` → `namespace Vastcore.UI`
- `Assets/Scripts/UI/RealtimeUpdateSystem.cs` → `namespace Vastcore.UI`
- `Assets/Scripts/UI/SliderBasedUISystem.cs` → `namespace Vastcore.UI`
- `Assets/Scripts/UI/SliderUIElement.cs` → `namespace Vastcore.UI`
- `Assets/Scripts/UI/TextClickHandler.cs` → `namespace Vastcore.UI`

## 保留（A3-2で検討）

- `Assets/Scripts/UI/MenuManager.cs`
  - `Vastcore.UI.Menus` への移行 or 既存 `TitleScreenManager` との役割分離を維持するか設計判断が必要

## 依存とアセンブリ

- `Assets/Scripts/UI/Vastcore.UI.asmdef`（rootNamespace: `Vastcore.UI`）に収容
- `Vastcore.UI` は `Vastcore.Core`/`Vastcore.Utils`/`Vastcore.Player`/`Unity.TextMeshPro` を参照

## テスト手順

1. Unityをフォーカス（もしくは再起動）。
2. 自動フラグで `Vastcore > Tools > UI Migration > Scan (Dry Run)` が実行され、`docs/04_reports/LEGACY_UI_MIGRATION_REPORT.md` が更新される。
3. コンソールにMissing Scriptや型解決エラーがないことを確認。
4. コアシーンで `ModernUIManager`/`InGameDebugUI` などの基本挙動を目視確認（表示・操作が従来通り）。

## 期待結果

- コード上の `NarrativeGen.UI` 該当が `MenuManager.cs` のみ（意図的保留）になる。
- アセット側のレポートからも、該当箇所が大幅に減少（移行進捗が定量化）。

## リスクと緩和

- MenuManagerの役割が重複・冗長になる可能性 → A3-2で統合/分割方針を設計・限定適用
- CreateAssetMenuのパス変更に伴う手動アセット運用の影響 → 既存アセットはGUID変化なし。新規作成時のみパスメニューが変更

## 次のアクション（A3-2 案）

- `MenuManager` の設計提案（`Vastcore.UI.Menus.MenuManager` へ移行 or `TitleScreenManager` へ機能委譲）
- ドライランレポートに基づく対象洗い出しと限定適用（自動置換は使わず、まずは最小変更）
