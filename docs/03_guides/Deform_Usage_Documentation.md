# Deformシステム使用ドキュメント

## 概要

VastCoreプロジェクトに統合されたDeformシステムは、高度なメッシュ変形機能を地形生成システムに提供します。このドキュメントでは、Deformシステムの使用方法、API、ベストプラクティスについて説明します。

## システムアーキテクチャ

### 主要コンポーネント

- **VastcoreDeformManager**: Deformシステム全体を管理するシングルトンマネージャー
- **PrimitiveTerrainObject**: 地形オブジェクトに変形機能を統合
- **DeformationPreset**: 再利用可能な変形設定を定義
- **Deformable**: Unity Deformパッケージのコアコンポーネント

### 依存関係

- Deformパッケージ (v1.2.2)
- Burstコンパイラー (1.8.24+)
- Mathematicsライブラリ (1.2.1+)

## 基本的な使用方法

### 1. 地形オブジェクトの変形有効化

```csharp
// PrimitiveTerrainObjectコンポーネントを取得
var terrainObj = primitive.GetComponent<PrimitiveTerrainObject>();

// Deform機能を有効化
terrainObj.UpdateDeformSettings(true, VastcoreDeformManager.DeformQualityLevel.High);
```

### 2. ノイズ変形の適用

```csharp
// 基本的なノイズ変形
terrainObj.ApplyNoiseDeformation(intensity: 0.1f, frequency: 1f);

// アニメーション付き変形
terrainObj.ApplyNoiseDeformationAnimated(
    targetIntensity: 0.2f,
    targetFrequency: 1.5f,
    duration: 2f
);
```

### 3. ディスプレイス変形の適用

```csharp
// テクスチャを使用したディスプレイス変形
terrainObj.ApplyDisplaceDeformation(strength: 0.5f, displaceMap: myTexture);

// アニメーション付き変形
terrainObj.ApplyDisplaceDeformationAnimated(
    targetStrength: 1f,
    displaceMap: myTexture,
    duration: 1.5f
);
```

### 4. 地形タイプ固有の変形

```csharp
// 自動的に地形タイプに適した変形を適用
terrainObj.ApplyTerrainSpecificDeformation();
```

## プリセットシステム

### プリセットの作成

```csharp
// デフォルトプリセットを作成
var preset = DeformationPreset.CreateDefaultPreset(GenerationPrimitiveType.Crystal);

// カスタム設定
preset.noiseIntensity = 0.15f;
preset.noiseFrequency = 2.0f;

// アセットとして保存
AssetDatabase.CreateAsset(preset, "Assets/DeformationPresets/CrystalPreset.asset");
```

### プリセットの適用

```csharp
// プリセットを適用
terrainObj.ApplyDeformationPreset(myPreset);
```

## エディタツール

### Deformation Editor Window

1. **Vastcore > Deformation Editor** メニューから開く
2. シーン内の地形オブジェクトを選択
3. リアルタイムプレビューを有効化
4. スライダーでパラメータを調整
5. プリセットの作成・適用

### Deformation Brush Tool

1. **Vastcore > Deformation Brush Tool** メニューから開く
2. ブラシツールを有効化
3. シーン内でクリックして変形を適用
4. マウスホイールでブラシサイズ調整
5. Shiftキーで変形を消去

## 高度な機能

### パフォーマンス最適化

```csharp
// 品質レベル設定
terrainObj.UpdateDeformSettings(true, VastcoreDeformManager.DeformQualityLevel.Medium);

// Deformマネージャーの統計取得
var stats = VastcoreDeformManager.Instance.GetStats();
Debug.Log($"Active deformables: {stats.managedDeformablesCount}");
```

### Undo/Redoサポート

すべての変形操作はUnityのUndoシステムに対応しています：

- `Ctrl+Z` / `Cmd+Z`: 操作を元に戻す
- `Ctrl+Y` / `Cmd+Y`: 操作をやり直す

### 変形のクリア

```csharp
// すべての変形をクリア
terrainObj.ClearAllDeformers();

// 特定のタイプの変形を削除
terrainObj.RemoveDeformer<NoiseDeformer>();
```

## 地形タイプ別推奨設定

| 地形タイプ | ノイズ強度 | 周波数 | 推奨品質 |
|-----------|-----------|--------|----------|
| Crystal | 0.15 | 2.0 | High |
| Boulder | 0.20 | 1.5 | High |
| Mesa | 0.08 | 0.8 | Medium |
| Spire | 0.10 | 1.2 | High |
| Formation | 0.12 | 0.7 | Medium |

## テストとベンチマーク

### 自動テストの実行

```csharp
// DeformIntegrationTestコンポーネントをシーンに配置
var testComponent = FindObjectOfType<DeformIntegrationTest>();

// パフォーマンスベンチマーク
testComponent.RunPerformanceBenchmark();

// メモリ使用量テスト
testComponent.RunMemoryBenchmark();

// 互換性テスト
testComponent.RunCompatibilityTest();
```

### ベンチマーク結果の解釈

- **パフォーマンスオーバーヘッド**: 通常5-15%程度
- **メモリ使用量**: 変形あたり約50-200KB追加
- **互換性**: LOD、プーリング、コライダーシステムと完全互換

## トラブルシューティング

### 一般的な問題

**Q: Deformが動作しない**
A: DEFORM_AVAILABLEシンボルが定義されているか確認してください。

**Q: パフォーマンスが低下する**
A: 品質レベルをMediumまたはLowに設定するか、フレーム分散を有効化してください。

**Q: 変形が適用されない**
A: PrimitiveTerrainObjectにDeformableコンポーネントがアタッチされているか確認してください。

### デバッグ情報

```csharp
// Deformマネージャーの統計を表示
var stats = VastcoreDeformManager.Instance.GetStats();
Debug.Log($"Deform Stats: {stats.managedDeformablesCount} objects, {stats.queuedRequestsCount} queued");

// LOD情報を表示
var lodInfo = terrainObj.GetLODInfo();
Debug.Log($"LOD: {lodInfo.currentLOD}, Distance: {lodInfo.distance:F1}m");
```

## APIリファレンス

### PrimitiveTerrainObject

| メソッド | 説明 |
|---------|------|
| `ApplyNoiseDeformation(float, float)` | ノイズ変形を適用 |
| `ApplyDisplaceDeformation(float, Texture2D)` | ディスプレイス変形を適用 |
| `ApplyTerrainSpecificDeformation()` | 地形タイプ固有の変形を適用 |
| `ApplyDeformationPreset(DeformationPreset)` | プリセットを適用 |
| `ClearAllDeformers()` | すべての変形をクリア |
| `UpdateDeformSettings(bool, QualityLevel)` | Deform設定を更新 |

### VastcoreDeformManager

| メソッド | 説明 |
|---------|------|
| `RegisterDeformable(object, QualityLevel)` | Deformableを登録 |
| `UnregisterDeformable(object)` | Deformableの登録解除 |
| `QueueDeformation(object, QualityLevel, float)` | 変形をキューに追加 |
| `GetStats()` | 統計情報を取得 |

## 今後の拡張

- GPUアクセラレーションの強化
- より詳細なプリセットシステム
- ランタイム変形アニメーション
- ネットワーク同期対応

## バージョン履歴

- **v1.0.0**: 基本Deform統合
- **v1.1.0**: プリセットシステム追加
- **v1.2.0**: エディタツール統合
- **v1.3.0**: パフォーマンス最適化

---

このドキュメントは継続的に更新されます。質問やフィードバックは開発チームまでお問い合わせください。
