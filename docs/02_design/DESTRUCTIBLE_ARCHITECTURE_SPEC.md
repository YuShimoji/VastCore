# Destructible Architecture 仕様書

- **最終更新日時**: 2026-03-08
- **ステータス**: Draft
- **難易度**: 中〜高
- **前提**: Phase C 完了

---

## 1. 目的

生成された構造物（ArchitecturalGenerator / CompoundArchitecturalGenerator 出力）を
ランタイムで部分的に破壊可能にする。DensityGrid ベースのボリューメトリック表現と
VolumetricStreamingController の自動再構築パイプラインを活用する。

---

## 2. 既存インフラ

| コンポーネント | 状態 | 利用可否 |
|--------------|------|---------|
| DensityGrid | 動作中 | float[] + trilinear sampling。破壊計算の基盤 |
| DensityGridPool | 動作中 | GC 軽減のためのプーリング |
| VolumetricStreamingController | 動作中 | dirty-region tracking + 6ms/frame budget で自動再構築 |
| MarchingCubesMeshExtractor | 動作中 | CPU Marching Tetrahedra。~12-15ms@resolution33 |
| ChunkSeamProcessor | 動作中 | チャンク境界のシームレス接合 |
| DeformMask (Box/Sphere/Cylinder) | 動作中 | 空間フォールオフ計算 (#if DEFORM_AVAILABLE ゲート内) |
| DeformAnimation | 動作中 | 時間ベース強度カーブ |
| StructureGenerators | 動作中 | ProBuilderMesh / GameObject 出力 (非ボリューメトリック) |

**欠けているもの**:
- Mesh → DensityGrid 変換
- 密度減算 API
- Job System / Burst (プロジェクト全体でゼロ)

---

## 3. アーキテクチャ

```
StructureGenerator → Mesh
        |
        v  (Phase D1: 変換)
    MeshVoxelizer
        |
        v
    DensityGrid (float[])
        |
        v  (Phase D2: 破壊)
    DensitySubtractor
    (SubtractSphere / SubtractBox)
        |
        v  (自動)
    VolumetricStreamingController.MarkDirty()
        |
        v  (自動)
    MarchingCubesMeshExtractor → 再構築メッシュ
```

---

## 4. フェーズ分割

### Phase D1: 密度減算 API (3h)
- DensityGrid に以下メソッド追加:
  ```csharp
  void SubtractSphere(Vector3 center, float radius, float falloff)
  void SubtractBox(Bounds bounds, float falloff)
  ```
- DeformMask のフォールオフ計算を standalone ユーティリティに抽出
  - `#if DEFORM_AVAILABLE` ゲートを除去
  - `SpatialFalloff.Sphere()`, `SpatialFalloff.Box()` として公開

### Phase D2: ランタイム破壊ブラシ (3h)
- DestructionBrush MonoBehaviour:
  - トリガー: OnCollisionEnter / マウスクリック / API呼び出し
  - 処理: SubtractSphere(hitPoint, radius, falloff) + MarkDirty(bounds)
- VolumetricStreamingController が自動的にメッシュ再生成

### Phase D3: Mesh → Voxel 変換 (8h)
- MeshVoxelizer クラス:
  - 入力: Mesh (ProBuilder/標準)
  - 出力: DensityGrid
  - 手法: レイキャスト方式 (Physics.Raycast で内外判定)
  - 解像度: 構造物サイズから自動算出
- StructureGenerator 出力を DestructibleStructure コンポーネントでラップ

### Phase D4: 破片生成 (6h, オプション)
- 破壊時に破片メッシュを Instantiate
- Rigidbody 付与で物理落下
- 一定時間後に Destroy

### Phase D5: 構造健全性 (将来, 高難度)
- 連結成分解析で浮遊部分を検出
- 支持を失った部分が崩壊
- パフォーマンス: Job System 前提

---

## 5. Deform パッケージとの関係

**Deform パッケージ依存を削除する方針**を推奨。
理由:
- DeformMask のフォールオフ数学は自己完結している
- DeformAnimation のカーブも独立で使える
- 密度減算方式は Deform の「メッシュ変形」とは根本的にアプローチが異なる
- `#if DEFORM_AVAILABLE` ゲートの管理コストを排除

---

## 6. パフォーマンス考慮

| 処理 | 現状 | 目標 |
|------|------|------|
| MarchingCubes@res33 | ~12-15ms | <3ms (Burst 化後) |
| DensityGrid 参照 | managed float[] | NativeArray<float> (Phase D 最適化) |
| チャンク再構築/フレーム | 1 | 2-3 (6ms budget 内) |

Phase D（最適化フェーズ）の Job System / Burst 導入と同期すると効果的。

---

## 7. リスク

- **Mesh → Voxel 変換精度**: 薄い壁や複雑な形状でアーティファクト発生の可能性
  - 対策: 解像度を構造物の最小フィーチャーサイズに合わせる
- **パフォーマンス**: Burst 未導入の現状では大規模破壊は非現実的
  - 対策: Phase D1-D2 を先行し、小規模破壊でプロトタイプ
- **ProBuilder メッシュの非凸性**: レイキャスト内外判定が複雑になる場合がある
  - 対策: 凸分解 or 符号付き距離場方式を検討

---

## 8. 完了条件

- [ ] Phase D1: DensityGrid.SubtractSphere() が密度値を正しく減算すること
- [ ] Phase D2: ランタイムでクリック → テレイン破壊 → メッシュ再生成が動作すること
- [ ] Phase D3: 任意の Mesh を DensityGrid に変換し、破壊可能にできること
