# 旧→新 UI マッピング定義（A1 設計書）

## 目的
- 旧 `NarrativeGen.UI` から新 `Vastcore.UI` への移行ルールを定義する。
- GUIDやアセット参照を壊さず、最小差分（ファイル移動なし、namespace変更中心）で移行する。

## スコープ
- C# namespace の統一: `NarrativeGen.UI` → `Vastcore.UI`。
- 例外: `MenuManager` は `Vastcore.UI.Menus`（サブ名前空間）へ移行。
- CreateAssetMenu の `menuName` を `Vastcore/UI/...` に統一（新規追加時）。既存アセットのGUIDは保持。
- asmdef: `Vastcore.UI.asmdef`（rootNamespace=`Vastcore.UI`）を前提。必要なら将来の[A2]で調整。

## ルール体系
- Namespace 置換規則
  - 基本: `NarrativeGen.UI` → `Vastcore.UI`
  - ピンポイント: `NarrativeGen.UI.MenuManager` → `Vastcore.UI.Menus.MenuManager`
- クラス名の変更
  - 現時点ではクラス名自体は維持（名前空間のみ変更）。
- 属性（CreateAssetMenu）の調整
  - `menuName` 先頭を `Vastcore/UI/` に統一。
- ファイル/GUID
  - ファイルの移動は行わない。GUIDは保持。メタ参照破壊を避ける。

## JSON ルール形式（雛形）
- `version`: スキーマバージョン。
- `namespaceMappings[]`: { from, to, includePattern? }
- `classMappings[]`: { fromQualified, toQualified, note? }
- `attributeRules[]`: { attribute, fields: { fieldName: { replaceRegexFrom, replaceTo, setIfMissing? } } }
- `constraints`: { preserveGUID: true, moveFiles: false }
- `validation`: { compile: true, scenesSmoke: ["Assets/Scenes/Main.unity"] }

### 例（抜粋）
```json
{
  "version": "1.0",
  "namespaceMappings": [
    { "from": "NarrativeGen.UI", "to": "Vastcore.UI" }
  ],
  "classMappings": [
    { "fromQualified": "NarrativeGen.UI.MenuManager", "toQualified": "Vastcore.UI.Menus.MenuManager", "note": "サブ名前空間へ集約" }
  ],
  "attributeRules": [
    {
      "attribute": "UnityEngine.CreateAssetMenuAttribute",
      "fields": {
        "menuName": { "replaceRegexFrom": "^NarrativeGen/", "replaceTo": "Vastcore/" }
      }
    }
  ],
  "constraints": { "preserveGUID": true, "moveFiles": false },
  "validation": { "compile": true, "scenesSmoke": ["Assets/Scenes/Main.unity"] }
}
```

## 運用
- 手動レビュー優先（自動置換は[A2]以降で工具化）。
- テスト: コンパイル無エラー、コアシーンで UI 基本挙動を確認。

## 既知の例外/補足
- `MenuManager` は `Vastcore.UI.Menus` へ。その他は `Vastcore.UI`。
- 将来拡張: asmdef の参照と rootNamespace の整備、PlayMode テスト導入。
