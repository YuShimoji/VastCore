# Unity Test Runner 設計・セットアップ

本ドキュメントでは、Unity Test Runner（EditMode/PlayMode）のセットアップ、推奨テスト構成、運用手順を示します。

## 前提
- `Assets/Tests/Runtime/Vastcore.Testing.asmdef` に `optionalUnityReferences: ["TestAssemblies"]` を設定済み。
- Deform 統合用に `versionDefines` を設定し、`com.beans.deform` 存在時に `DEFORM_AVAILABLE` が定義されるようにしました。
- Deform 型参照が必要なアセンブリでは `references: ["Deform"]` を付与しています。

## ディレクトリ構成（推奨）
```
Assets/
  Tests/
    Runtime/
      Vastcore.Testing.asmdef
      VastcoreIntegrationTestManager.cs
      TestCases/
        TerrainGenerationTestCase.cs
        PrimitiveGenerationTestCase.cs
        SystemIntegrationTestCase.cs
        PerformanceTestCase.cs
        MemoryManagementTestCase.cs
        ...
    EditMode/
      Vastcore.EditMode.Tests.asmdef (任意)
      EditorSpecs/
        AsmdefIntegrityTests.cs
        NamingConventionTests.cs
```

## 実行方法
- Unity Editor: Window > General > Test Runner から PlayMode（Runtime）テストを実行
- CLI（CI 向け）:
  - Unity Test Runner CLI を用いて PlayMode テストを実行（CI では `-runTests -testPlatform PlayMode` を付与）

## テスト設計（Mermaid）
```mermaid
graph LR
  A[環境初期化: VastcoreIntegrationTestManager] --> B[地形生成テスト]
  B --> C[プリミティブ生成テスト]
  C --> D[UI 連携テスト]
  D --> E[システム統合テスト]
  E --> F[パフォーマンス/高負荷テスト]
  F --> G[メモリ管理/リーク検知]
  G --> H[最終検証/結果集計]
```

## 失敗時の典型パターンと対処
- CS1626 (try/catch 内での yield): コルーチンの `yield` は try/catch の外へ移動
- CS1061 (メソッド未定義): API リグレッションに合わせて呼び出し元を修正
- CS0618 (Obsolete): `FindObjectOfType` → `FindFirstObjectByType` に置換
- 条件付きコンパイルが効かない: `asmdef.versionDefines` と `references` の整合性確認（該当アセンブリのみ）

## 成果物
- テスト結果は `VastcoreIntegrationTestManager.LogTestResults()` により Editor Console に集計表示
- 必要に応じて `TestLogger.SaveLogsToFile(path)` でログをファイル出力

## 今後の強化
- CI での PlayMode テスト自動実行とレポート出力（XML/HTML）
- シーンごとの Missing Script チェックと自動修復処理
- Deform 統合テストの安定化（asmdef と defineConstraints の最終整合性確認）
