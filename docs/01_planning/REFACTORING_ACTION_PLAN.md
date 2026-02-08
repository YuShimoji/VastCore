# Vastcore Terrain Engine - リファクタリングアクションプラン

**作成日**: 2025-10-27  
**目標**: 開発基盤の健全性スコア 69点 → 85点以上

---

## 🎯 概要

このアクションプランは、`PROJECT_HEALTH_ANALYSIS.md`で検出された課題に対する具体的な解決策を提供します。
各タスクには優先度、推定工数、実装手順が含まれています。

---

## 📋 Phase 1: 緊急対応 (1週間以内)

### Task 1.1: 廃止ファイルの削除 🔴
**優先度**: Critical  
**推定工数**: 30分  
**担当**: 開発者

#### 目的
CS0436警告の原因となっている重複定義ファイルを削除

#### 対象ファイル
- `Assets/Scripts/Generation/PrimitiveTerrainGenerator.cs`
- 対応する`.meta`ファイル

#### 手順
1. ファイルのバックアップを作成
2. 削除前に依存関係を確認
3. 削除実行
4. コンパイルエラーの確認
5. Gitコミット: `chore: Remove deprecated PrimitiveTerrainGenerator duplicate`

#### 成功基準
- CS0436警告の消失
- コンパイルエラー0件維持

---

### Task 1.2: PrimitiveTerrainObjectの統合テスト 🔴
**優先度**: Critical  
**推定工数**: 4時間  
**担当**: QA/開発者

#### 目的
新規実装したPrimitiveTerrainObjectの動作確認

#### 実装内容
```csharp
// Assets/Scripts/Testing/TestCases/PrimitiveTerrainObjectTestCase.cs
public class PrimitiveTerrainObjectTestCase : ITestCase
{
    public string TestName => "PrimitiveTerrainObject Integration Test";
    
    public IEnumerator RunTest(TestContext context)
    {
        // 1. プール取得テスト
        // 2. LOD更新テスト
        // 3. 統計情報テスト
        // 4. メモリ管理テスト
    }
}
```

#### 手順
1. テストケースクラスの作成
2. プール動作の検証
3. LODシステムの検証
4. 統計情報の正確性確認
5. VastcoreIntegrationTestManagerへの登録

#### 成功基準
- 全テストがパス
- LOD統計が正確に取得できる
- プールからの取得・返却が正常動作

---

## 📋 Phase 2: 基盤強化 (2-3週間)

### Task 2.1: ProBuilder API移行調査 🟡
**優先度**: High  
**推定工数**: 16時間  
**担当**: 技術リード

#### 目的
Subdivision機能の復元またはカスタム実装

#### 調査項目
1. **ProBuilder最新APIの調査**
   - 現行バージョンのSubdivision API
   - 代替メソッドの存在確認
   - Breaking Changesの詳細

2. **カスタム実装の検討**
   - Catmull-Clark細分割の実装難易度
   - パフォーマンスへの影響
   - 既存ライブラリの活用可能性

3. **影響範囲の特定**
   ```
   HighQualityPrimitiveGenerator.cs: 23箇所
   ├── GenerateCube: 3箇所
   ├── GenerateSphere: 3箇所
   ├── GenerateCylinder: 3箇所
   ├── GeneratePyramid: 3箇所
   ├── GenerateArch: 2箇所
   └── その他: 9箇所
   ```

#### 実装オプション
**Option A: ProBuilder API更新**
- 利点: 公式サポート、パフォーマンス保証
- 欠点: API依存、バージョン管理

**Option B: カスタム実装**
- 利点: 完全制御、依存関係削減
- 欠点: 開発工数大、保守負担

**Option C: 機能削除**
- 利点: 即座に実装可能
- 欠点: 品質低下

#### 推奨
Option A (ProBuilder API更新) を優先、失敗時はOption Cで暫定対応

---

### Task 2.2: SystemIntegrationTestCaseの実装 🟡
**優先度**: High  
**推定工数**: 6時間  
**担当**: 開発者

#### 目的
統合テストの完成とテストカバレッジ向上

#### 実装内容
```csharp
// Assets/Scripts/Testing/TestCases/SystemIntegrationTestCase.cs
public class SystemIntegrationTestCase : ITestCase
{
    public string TestName => "System Integration Test";
    
    public IEnumerator RunTest(TestContext context)
    {
        // 1. マネージャー間通信テスト
        yield return TestManagerCommunication(context);
        
        // 2. エラー回復フローテスト
        yield return TestErrorRecoveryFlow(context);
        
        // 3. メモリ圧迫時の動作テスト
        yield return TestMemoryPressure(context);
        
        // 4. パフォーマンス統合テスト
        yield return TestPerformanceIntegration(context);
    }
    
    private IEnumerator TestManagerCommunication(TestContext context)
    {
        // VastcoreSystemManager → RuntimeTerrainManager
        // RuntimeTerrainManager → PrimitiveTerrainManager
        // エラー発生 → ErrorHandler → ErrorRecovery
    }
}
```

#### 検証項目
- マネージャー初期化順序
- システム間のイベント伝播
- エラー回復のエンドツーエンド動作
- メモリ管理の統合動作

---

### Task 2.3: インターフェース設計の導入 🟡
**優先度**: High  
**推定工数**: 8時間  
**担当**: アーキテクト

#### 目的
依存関係の明確化と疎結合化

#### 設計内容
```csharp
// Assets/Scripts/Core/Interfaces/IManager.cs
public interface IManager
{
    void Initialize();
    void Shutdown();
    bool IsInitialized { get; }
    ManagerStatus GetStatus();
}

// Assets/Scripts/Core/Interfaces/IPoolable.cs
public interface IPoolable
{
    void OnSpawnFromPool();
    void OnReturnToPool();
    bool IsAvailable { get; }
}

// Assets/Scripts/Core/Interfaces/IRecoverable.cs
public interface IRecoverable<TParams, TResult>
{
    IEnumerator AttemptRecovery(TParams parameters, 
        System.Action<TResult> onSuccess, 
        System.Action onFailure);
}

// Assets/Scripts/Core/Interfaces/ILoggable.cs
public interface ILoggable
{
    string LogCategory { get; }
    VastcoreLogger.LogLevel MinimumLogLevel { get; set; }
}
```

#### 適用対象
- VastcoreSystemManager → IManager
- PrimitiveTerrainObject → IPoolable
- TerrainErrorRecovery → IRecoverable<TerrainGenerationParams, GameObject>
- 全マネージャークラス → ILoggable

#### 移行手順
1. インターフェースファイルの作成
2. 既存クラスへの実装
3. 依存注入パターンの導入
4. リファクタリングの検証

---

## 📋 Phase 3: 品質向上 (1ヶ月)

### Task 3.1: テストカバレッジ70%達成 🟡
**優先度**: High  
**推定工数**: 24時間  
**担当**: QA/開発者

#### カバレッジ目標
| モジュール | 現状 | 目標 | 必要テスト数 |
|-----------|------|------|-------------|
| Core | 60% | 80% | +15 tests |
| Terrain/Map | 40% | 70% | +20 tests |
| Generation | 50% | 70% | +12 tests |
| Error Recovery | 30% | 70% | +18 tests |

#### 追加テスト一覧
1. **VastcoreSystemManagerTests**
   - 初期化テスト
   - ヘルスチェックテスト
   - シャットダウンテスト

2. **RuntimeTerrainManagerTests**
   - タイル生成テスト
   - メモリ管理テスト
   - クリーンアップテスト

3. **PrimitiveMemoryManagerTests**
   - オブジェクト登録テスト
   - カリングテスト
   - メモリ圧迫テスト

4. **ErrorRecoveryTests**
   - フォールバック生成テスト
   - リトライロジックテスト
   - 緊急対応テスト

---

### Task 3.2: パフォーマンスプロファイリングと最適化 🟡
**優先度**: High  
**推定工数**: 12時間  
**担当**: パフォーマンスエンジニア

#### プロファイリング項目
1. **CPU使用率**
   - 地形生成の処理時間
   - プリミティブ配置の処理時間
   - LOD更新の処理時間

2. **メモリ使用量**
   - ヒープアロケーション
   - GCプレッシャー
   - プールの効率性

3. **描画パフォーマンス**
   - ドローコール数
   - バッチング効率
   - オーバードロー

#### 最適化戦略
```
優先度1: ホットパスの最適化
├── RuntimeTerrainManager.ProcessGenerationQueue
├── PrimitiveTerrainManager.UpdatePrimitivesAroundPlayer
└── LOD更新ループ

優先度2: メモリアロケーション削減
├── オブジェクトプールの拡張
├── Listのキャパシティ事前確保
└── string連結の最適化

優先度3: 並列化
├── 地形生成のJob System化
├── メッシュ生成のマルチスレッド化
└── Physics.RaycastのBatch化
```

#### 目標
- フレームタイム: 33ms以下 (30FPS) → 16ms以下 (60FPS)
- GC Allocation: 1MB/frame以下
- ドローコール: 500以下

---

## 📋 Phase 4: 高度な機能 (2-3ヶ月)

### Task 4.1: イベント駆動システムの導入 🟢
**優先度**: Medium  
**推定工数**: 10時間  
**担当**: アーキテクト

#### 設計
```csharp
// Assets/Scripts/Core/Events/EventBus.cs
public class EventBus : MonoBehaviour
{
    private static EventBus instance;
    private Dictionary<Type, List<Delegate>> eventHandlers;
    
    public static void Subscribe<T>(Action<T> handler) where T : IEvent
    public static void Unsubscribe<T>(Action<T> handler) where T : IEvent
    public static void Publish<T>(T eventData) where T : IEvent
}

// イベント定義
public interface IEvent { DateTime Timestamp { get; } }

public struct TerrainGeneratedEvent : IEvent
{
    public DateTime Timestamp { get; set; }
    public Vector2Int TileCoordinate { get; set; }
    public GameObject TerrainObject { get; set; }
}

public struct PrimitiveSpawnedEvent : IEvent
{
    public DateTime Timestamp { get; set; }
    public Vector3 Position { get; set; }
    public PrimitiveType Type { get; set; }
}

public struct ErrorOccurredEvent : IEvent
{
    public DateTime Timestamp { get; set; }
    public string ErrorType { get; set; }
    public Exception Exception { get; set; }
}
```

#### 適用箇所
- 地形生成完了 → TerrainGeneratedEvent
- プリミティブ配置 → PrimitiveSpawnedEvent
- エラー発生 → ErrorOccurredEvent
- メモリ警告 → MemoryWarningEvent

---

### Task 4.2: マルチスレッド化 🟢
**優先度**: Medium  
**推定工数**: 32時間  
**担当**: 上級開発者

#### 対象処理
1. **地形生成**
   - HeightMap計算
   - Noise計算
   - Mesh構築

2. **プリミティブ生成**
   - Mesh細分割
   - UV計算
   - Normal計算

3. **LOD計算**
   - 距離計算
   - レベル判定

#### 実装方法
```csharp
// Unity Job Systemの活用
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

[BurstCompile]
struct TerrainHeightJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> NoiseData;
    [WriteOnly] public NativeArray<float> Heights;
    public float HeightScale;
    
    public void Execute(int index)
    {
        Heights[index] = NoiseData[index] * HeightScale;
    }
}
```

#### 期待効果
- 地形生成速度: 2-4倍高速化
- フレームレート: 平均10-20FPS向上
- CPU使用率: メインスレッド負荷40%削減

---

## 📋 Phase 5: ドキュメント整備 (継続的)

### Task 5.1: APIリファレンスの作成 🟢
**優先度**: Medium  
**推定工数**: 16時間  
**担当**: テクニカルライター/開発者

#### 対象API
- VastcoreSystemManager
- RuntimeTerrainManager
- PrimitiveTerrainManager
- ErrorRecoveryシステム
- PoolSystem

#### 形式
```markdown
# API Reference: RuntimeTerrainManager

## Overview
実行時地形管理システム。プレイヤー位置に基づく動的タイル管理を提供。

## Public Methods

### SetDynamicGenerationEnabled(bool enabled)
動的生成の有効/無効を切り替え

**Parameters:**
- `enabled`: true=有効, false=無効

**Example:**
```csharp
RuntimeTerrainManager.Instance.SetDynamicGenerationEnabled(true);
```

### GetPerformanceStats()
パフォーマンス統計を取得

**Returns:** `PerformanceStats` - 統計情報

**Example:**
```csharp
var stats = manager.GetPerformanceStats();
Debug.Log($"Memory: {stats.currentMemoryUsageMB}MB");
```
```

---

### Task 5.2: トラブルシューティングガイドの作成 🟢
**優先度**: Medium  
**推定工数**: 8時間  
**担当**: サポートエンジニア

#### 内容
1. **一般的な問題と解決策**
   - メモリ不足
   - 生成速度低下
   - コンパイルエラー

2. **デバッグ手順**
   - ログの確認方法
   - プロファイラーの使用方法
   - テストの実行方法

3. **FAQ**
   - システム要件
   - パフォーマンスチューニング
   - カスタマイズ方法

---

## 📊 進捗管理

### マイルストーン
- **M1 (Week 1)**: Phase 1完了
- **M2 (Week 3)**: Phase 2完了
- **M3 (Week 6)**: Phase 3完了
- **M4 (Week 12)**: Phase 4完了
- **継続**: Phase 5

### KPI
- コンパイルエラー: 0件維持
- テストカバレッジ: 45% → 70%
- 健全性スコア: 69点 → 85点
- パフォーマンス: 30FPS → 60FPS

---

## 🚦 リスク管理

### 高リスク項目
1. **ProBuilder API移行**
   - リスク: API変更により移行不可
   - 対策: 早期調査、カスタム実装の準備

2. **マルチスレッド化**
   - リスク: デッドロック、レースコンディション
   - 対策: 段階的導入、徹底的なテスト

### 軽減策
- 週次レビュー実施
- バックアップブランチ維持
- 段階的デプロイ

---

**このアクションプランに従って、計画的にプロジェクトの健全性を向上させていきます。**
