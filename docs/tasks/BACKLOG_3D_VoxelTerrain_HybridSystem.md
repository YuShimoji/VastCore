# BACKLOG: ハイブリッド・ボクセル地形生成システム（3D地形）

Status: BACKLOG  
Created: 2026-01-04T12:08:38+09:00  
Priority: Medium（既存2Dシステムの安定化後に着手推奨）  
Tier: 3（新規機能 / 大規模アーキテクチャ変更）  
Estimated Effort: Large（Phase 1-5で段階的実装）

## 背景 / 目的

現状のVastCoreは**2Dハイトマップ地形**（`TerrainGenerator` / `HeightMapGenerator`）を基盤としているが、以下の制約がある：

- **洞窟・オーバーハング・浮遊島**が表現できない
- **破壊・編集**が困難（ハイトマップは2Dデータのため）

本タスクは、**Marching Cubesアルゴリズム**を用いた**3Dボクセル地形システム**を構築し、既存の2Dハイトマップシステムと**ハイブリッドに統合**することを目的とする。

### 統合方針（既存システムとの共存）

1. **段階的移行**: 既存の`TerrainGenerator`は維持し、新システムは**別コンポーネント**として追加
2. **共通インターフェース**: 両システムが`TerrainGenerationProfile`を参照可能にする（将来的）
3. **エディタ統合**: `TerrainGenerationWindow`に「3D Voxel Mode」タブを追加（v0は2D専用のまま）

## 参照（SSOT）

- **設計書**: `Assets/Docs/3DTerrainDesignDoc_1.0`
- **既存2Dシステム**: `docs/terrain/TerrainGenerationV0_Spec.md`
- **既存実装**: `Assets/MapGenerator/Scripts/TerrainGenerator.cs`
- **SSOT**: `docs/Windsurf_AI_Collab_Rules_latest.md`

## 技術要件（必須）

### Core Algorithm
- **Marching Cubes** (Look-up Table方式)
- **Unity Job System** (`IJobParallelFor`) + **Burst Compiler**（必須）
- **Unity.Mathematics** (`float3`, `int3`, `math.lerp`)
- **Native Collections** (`NativeArray`, `NativeHashMap`) によるGC回避

### Rendering
- **Triplanar Mapping Shader**（UV展開不要）
  - Top: 草/土
  - Side: 岩
  - Bottom: 暗い土
- Standard Surface / URP / HDRP 対応

### データ構造
- **Chunk System**: 16x16x16 または 32x32x32 単位
- **無限地形**: プレイヤー位置に応じた動的生成・破棄（Object Pooling）
- **LOD**: 距離に応じたサンプリング解像度の可変

## コア・アーキテクチャ

### 密度関数（Density Function）

空間座標 $P(x, y, z)$ における密度 $D$ を計算する。  
**定義**: $D > 0$ を「土（Solid）」、$D < 0$ を「空気（Air）」とする。

#### 基本式

$$D_{final} = \text{SmoothSubtract}(\text{SmoothUnion}(D_{base}, D_{stamp}), D_{cave})$$

#### 各項の詳細

1. **ベース地形** ($D_{base}$):
   $$D_{base} = \text{HeightMap}(x, z) - y$$
   - 既存の2Dハイトマップを活用
   - $y$ がハイトマップより低い場所ほど正の値（土）

2. **洞窟ノイズ** ($D_{cave}$):
   $$D_{cave} = \text{PerlinNoise3D}(x \cdot s, y \cdot s, z \cdot s) - \text{Threshold}$$
   - しきい値を超えたノイズの塊を「空気」として引き算

3. **SDFスタンプ** ($D_{stamp}$):
   - ワールド座標をスタンプのローカル空間へ変換
   - SDF値（3Dテクスチャサンプリング or 数式）を取得
   - `SmoothUnion` で地形とスタンプの境界を滑らかに溶接

### 法線計算

密度場の勾配（Gradient）から法線を算出：

$$N = \text{normalize}(\nabla D(x, y, z))$$

実装は近傍点（$\epsilon$）との差分（Central Difference）を用いる。

## より優れたアプローチの検討

### 1. Dual Contouring の検討（Marching Cubes の代替）

**Marching Cubes の課題**:
- エッジのシャープさが失われる（特に90度の角）
- チャンク境界での連続性が保証されない

**Dual Contouring の利点**:
- シャープなエッジを保持
- チャンク境界での連続性が自然
- SDFスタンプとの相性が良い

**推奨**: Phase 1ではMarching Cubesで実装し、Phase 5（最適化）でDual Contouringへの移行を検討。

### 2. 既存2Dシステムとの統合強化

**現行設計**: 2Dと3Dは別コンポーネント

**改善案**: 
- `TerrainGenerationProfile`に「Hybrid Mode」フラグを追加
- 2Dハイトマップを3D密度場のベースとして直接使用
- エディタ上で「2D Mode / 3D Mode / Hybrid Mode」を切替可能に

**実装優先度**: Medium（既存システムの安定化後に検討）

### 3. Compute Shader の活用検討

**現行設計**: Job System + Burst

**Compute Shader の利点**:
- GPU並列化による更なる高速化
- 大規模チャンク（64x64x64以上）での優位性

**課題**:
- メインスレッドとの同期コスト
- デバッグの難しさ

**推奨**: Phase 1-4はJob Systemで実装し、Phase 5でCompute Shaderへの部分移行を検討（密度計算のみ）。

### 4. メモリ効率化（Sparse Voxel Octree）

**現行設計**: 密なグリッド（Dense Grid）

**Sparse Voxel Octree の利点**:
- 空気領域のメモリを節約
- 大規模地形でのメモリ使用量削減

**課題**:
- 実装複雑度の増加
- チャンク境界での統合が困難

**推奨**: Phase 5（最適化）で検討。初期は密グリッドで実装。

## 実装フェーズ（Roadmap）

### Phase 1: 高速化基盤の確立（Core Foundation）

**目標**: Job System + Burstを用いたMarching Cubesの最小実装

**検証**: 単純な「球体」が1つのチャンク内に描画されること

**重要**: Lookup Table（Edge/Tri）は `static readonly` 配列として定義し、Jobから参照可能にすること

**成果物**:
- `VoxelChunk.cs` (MonoBehaviour)
- `MarchingCubesJob.cs` (struct, `IJobParallelFor`)
- `DensityCalculationJob.cs` (struct, Burst対応)
- `MarchingCubesTables.cs` (static readonly 配列)

### Phase 2: ハイブリッド密度関数の実装（Density Logic）

**目標**: 2Dハイトマップと3Dノイズの合成

**検証**:
- `pos.y - noise(x, z)` で波打つ地面ができる
- そこから `noise3d(x, y, z)` を引き算し、チーズのような穴空き地形ができる

**成果物**:
- `DensityFunctions.cs` (Burst対応の静的メソッド)
- 既存`HeightMapGenerator`との統合ロジック

### Phase 3: 法線とシェーディング（Rendering）

**目標**: 見た目の品質向上

**実装**:
- 密度勾配によるスムーズな法線計算
- Triplanar Shaderの作成（Shader Graph可）

**成果物**:
- `VoxelNormalCalculationJob.cs`
- `TriplanarVoxelShader.shader` または Shader Graph

### Phase 4: SDFスタンプシステム（Features）

**目標**: 任意の形状の配置

**実装**:
- まずは数式（Sphere/Box）でのスタンプ配置機能
- `Texture3D` のサンプリング処理への置き換え
- `SmoothMin` 関数による滑らかな結合

**成果物**:
- `SDFStampData.cs` (ScriptableObject)
- `SDFStampSystem.cs`

### Phase 5: 最適化と物理（Polish）

**目標**: ゲームとしての実用化

**実装**:
- 無限地形ローディング（チャンク管理）
- `MeshCollider` の非同期生成
- LODの実装（遠景のチャンクは頂点数を減らす）
- Dual Contouring / Compute Shader への移行検討

**成果物**:
- `VoxelWorld.cs` (Manager)
- `VoxelChunkPool.cs`
- LODシステム

## 開発開始用プロンプト（AIへの指示用）

Phase 1の開発を開始する際は、以下のプロンプトを使用：

```markdown
# Role
あなたはUnity (C#) のエキスパートエンジニアです。
特にDOTS (Data-Oriented Technology Stack)、Job System、Compute Shader、Procedural Generationに精通しています。

# Project Goal
Unity 2022 LTS以降をターゲットとし、**「Marching Cubesアルゴリズムを用いたハイブリッド・ボクセル地形生成システム」**の基盤を構築します。
最終的にはハイトマップと3Dノイズ、SDFスタンプを合成しますが、まずは**Phase 1**として以下の要件を満たすミニマムな実装を行ってください。

# Phase 1 Requirements
1.  **Algorithm:** Marching Cubes (Lookup Table方式)
2.  **Optimization (Critical):**
    * 計算負荷が高いため、必ず **Unity Job System (`IJobParallelFor`)** と **Burst Compiler** を使用すること。
    * メインスレッドでの計算は禁止。
    * 数学ライブラリは `Unity.Mathematics` (`float3`, `int3`) を使用。
3.  **Density Function (Test):**
    * 今回はテスト用として、チャンク中心に配置された「球体 (Sphere SDF)」を表示するロジックにすること。
    * `density = radius - distance(pos, center)`
4.  **Output:**
    * `VoxelChunk` (MonoBehaviour): チャンク管理とメッシュ生成のトリガー。
    * `MarchingCubesJob` (struct): メッシュ生成ロジック。
    * `DensityCalculationJob` (struct): 密度計算ロジック。
    * `MarchingCubesTables` (class): トライアングル・エッジテーブルの定義。

# Constraints
* ガベージコレクション（GC）を発生させないよう、`NativeArray` を適切に使用・Disposeすること。
* コードは省略せず、コンパイル可能な状態で記述すること。
```

## 既存システムとの統合ポイント

### 1. TerrainGenerationProfile の拡張

将来的に`TerrainGenerationProfile`に以下を追加：

```csharp
[System.Serializable]
public class VoxelTerrainSettings
{
    public bool UseVoxelMode = false;
    public int ChunkSize = 32;
    public float IsoLevel = 0f;
    public float CaveNoiseScale = 10f;
    public float CaveThreshold = 0.3f;
    // ... その他
}
```

### 2. TerrainGenerationWindow の拡張

`TerrainGenerationWindow`に「3D Voxel Mode」タブを追加（v0は2D専用のまま維持）。

### 3. 既存HeightMapGeneratorとの統合

Phase 2で、既存の`HeightMapGenerator.GenerateFromHeightMap`の結果を3D密度場のベースとして使用。

## 停止条件

- 既存の2Dハイトマップシステムに破壊的変更が必要になる
- Unity 2022.3 LTS未満への対応が必要になる
- Burst Compiler / Job System が使用できない環境での実装が必須になる

## 関連タスク

- `TASK_010`: TerrainGenerationWindow(v0) 機能改善（完了）
- `TASK_011`: HeightMapGenerator 決定論・チャネル・UV対応（完了）
- （将来）`TASK_XXX`: Phase 1実装
- （将来）`TASK_XXX`: Phase 2実装（既存2Dシステム統合）

## メモ

- 本タスクは**大規模な新規機能**のため、既存の2Dシステムが安定化してから着手することを推奨
- Phase 1-5は**段階的に実装**し、各Phaseで動作確認を行う
- より優れたアプローチ（Dual Contouring / Compute Shader）はPhase 5で検討
