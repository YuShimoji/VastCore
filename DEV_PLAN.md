# Vastcore 開発計画 & ドキュメント

## 1. プロジェクトコンセプト

このプロジェクトの目的は、ゲームプレイの舞台（ダンジョン）を作ることではなく、**広大な景観に映える、ユニークで巨大な人工構造物をプロシージャルに生成する**ことである。

生成される構造物は、ミニマルでありながら、単体で芸術的価値を持つような、印象的な景観の一部となることを目指す。初期の参考画像に見られるような、多様な幾何学形状の組み合わせによって生まれる、ユニークで複雑な形状の自動生成が中核となる。

---

## 2. 6段階開発計画（2024年12月更新）

### Phase 1: 基本関係性システム ✅ **完了**
- 構造物間の9種類の関係性（OnTop, Inside, OnSide, Around等）
- 自動位置計算とBounds対応配置
- RelationshipTabによるUI管理
- 実装完了日: 2024-12-XX

### Phase 2: 形状制御システム ✅ **完了**
- 高度な形状制御パラメータ（ツイスト、テーパー、ベンド等）
- Boolean演算制御（面選択、体積閾値、減衰制御）
- 高度加工システム（表面処理、エッジ処理、風化効果）
- 8つの記念碑タイプ実装
- 実装完了日: 2024-12-XX

### Phase 3: Deformシステム統合 ⏳ **次回実装予定**
- Unity Asset StoreのDeformパッケージ統合
- 20種類以上のDeformer対応（Bend, Twist, Noise, Spherify等）
- 高度な変形マスクシステム
- アニメーション変形対応
- 予定工数: 7-10レスポンス

### Phase 4: パーティクル的配置システム ✅ **完了**
- 8種類の配置パターン（Linear, Circular, Spiral, Grid, Random, Fractal, Voronoi, Organic）
- 衝突回避システム（最小距離制御）
- ハイトマップ地形適合システム
- 回転・スケール制御とプリセット対応
- 実装完了日: 2024-12-XX

### Phase 5: 高度合成システム ⏳ **次回実装予定**
- モデル合成とブレンド機能
- LOD生成とメッシュ最適化
- ライティング焼き込みシステム
- 体積ブレンドと距離フィールド合成
- 予定工数: 6-8レスポンス

### Phase 6: ランダム制御システム ⏳ **未実装**
- 制御されたランダム性（ControlledRandom, BlendShapeRandom）
- ランダムシード管理
- パラメータ制約システム
- ランダムプリセット管理
- 予定工数: 5-7レスポンス

---

## 3. 開発ツール概要

### 3.1. Structure Generator (`StructureGeneratorWindow.cs`)

構造物を生成・加工するためのメインツール。5つのタブで構成。

#### **Basicタブ: 基本形状の生成**
シンプルな形状とプリミティブ生成機能。

| 機能 | パラメータ | 説明 |
| :--- | :--- | :--- |
| **Basic Structure Types** | Cube, Cylinder, Sphere, Pyramid, Torus, Arch, Wall | 7つの基本形状から選択 |
| **Parameters** | Scale (1-100), Count (1-10), Material | スケール、生成数、マテリアル設定 |
| **Quick Generation** | ワンクリック生成ボタン | 各形状の即座生成 |

#### **Advancedタブ: 高度な構造物生成**
Phase 2で実装された高度な形状制御システム。

| 機能 | パラメータ | 説明 |
| :--- | :--- | :--- |
| **Monument Types** | 8種類の記念碑タイプ | GeometricMonolith, TwistedTower等 |
| **Shape Control System** | ツイスト、テーパー、ベンド | 高度な形状変形制御 |
| **Boolean Parameters** | 面選択、体積閾値、減衰制御 | Boolean演算の詳細制御 |
| **Advanced Processing** | 表面処理、エッジ処理、風化効果 | 高度な表面加工 |

#### **Operationsタブ: CSG演算** ✅ **修正完了**
pb_CSGライブラリを使用したBoolean演算機能。

| 機能 | パラメータ | 説明 |
| :--- | :--- | :--- |
| **Boolean Operations** | Union, Subtract, Intersect | 2つのメッシュの合成 |
| **Advanced Boolean** | 面選択、オフセット制御 | 高度なBoolean制御 |
| **Array Duplication** | Linear, Circular配置 | オブジェクトの配列複製 |
| **Status** | ✅ **安定化完了** | NullReferenceException修正済み |

#### **Relationshipsタブ: 構造物関係性管理**
Phase 1で実装された関係性システム。

| 機能 | パラメータ | 説明 |
| :--- | :--- | :--- |
| **Relationship Types** | 9種類の関係性 | OnTop, Inside, Around等 |
| **Automatic Placement** | 距離・角度制御 | 自動位置計算 |
| **Preview System** | リアルタイムプレビュー | Scene viewでのギズモ表示 |

#### **Distributionタブ: パーティクル様配置システム**
Phase 4で実装された高度な配置制御システム。

| 機能 | パラメータ | 説明 |
| :--- | :--- | :--- |
| **Distribution Patterns** | 8種類の配置パターン | Linear, Circular, Spiral, Grid等 |
| **Advanced Controls** | 衝突回避、ハイトマップ対応 | 高度な配置制御 |
| **Rotation & Scale** | ランダム回転・スケール | 自然な配置バリエーション |
| **Prefab Support** | プリセット対応 | Prefab配置と記念碑生成 |

### 3.2. Heightmap Terrain Generator (`HeightmapTerrainGeneratorWindow.cs`)

ハイトマップ画像からUnityのTerrainを生成・加工するためのツール。詳細は別途記載。

---

## 4. 技術的成果

### 4.1. ProBuilder API互換性
- ProBuilder 4.x以降のAPI完全対応
- 破壊的変更への対応完了
- CSGシステムの復旧（pb_CSGライブラリ使用）

### 4.2. Boolean操作システム修正完了 ✅
- **NullReferenceException完全修正**
- ProBuilderMeshの複雑な初期化処理を削除
- 標準的なMeshFilter/MeshRendererのみ使用
- 包括的なエラーハンドリング実装
- 段階的フォールバック機能

### 4.3. システム統合
- 5つのタブシステムによる機能分離
- Undoシステム完全対応
- マテリアル管理システム

### 4.4. エラーハンドリング
- 包括的なtry-catch構造
- ユーザーフレンドリーなエラーメッセージ
- デバッグログシステム

---

## 5. 現在の開発フェーズと次のステップ

**現在フェーズ:** Phase 4完了、Boolean操作修正完了、Phase 5準備段階

### 📊 システム状況
```
Structure Generator Window
├── Basic Tab          ✅ 動作中
├── Advanced Tab       ✅ 動作中  
├── Operations Tab     ✅ 修正完了
├── Relationships Tab  ✅ 動作中
└── Distribution Tab   ✅ 動作中
```

**直近のステップ:**
1. **Phase 5: 高度合成システムの実装**
   - モデル合成とブレンド機能
   - LOD生成とメッシュ最適化
   - ライティング焼き込みシステム
   - 体積ブレンドと距離フィールド合成

2. **システム統合テスト**
   - 全タブ間の連携テスト
   - 大規模構造物生成テスト
   - パフォーマンス測定

**中期目標:**
- Phase 6: ランダム制御システム
- Phase 3: Deformシステム統合（パッケージ導入後）
- 全システムの統合と最適化

---

## 6. ドキュメント

- `ADVANCED_STRUCTURE_DESIGN_DOCUMENT.md`: 6段階開発計画の詳細設計
- `DEV_LOG.md`: 開発作業の詳細ログ
- `Documentation/StructureGenerator_JA.md`: 構造ジェネレータの使用方法
- `CSG_INTEGRATION_LOG.md`: CSGシステム統合記録
- `Documentation/HeightmapTerrainGenerator_JA.md`: 地形ジェネレータの詳細な仕様と使い方。
- `Documentation/PlayerController_JA.md`: プレイヤーコントローラーの仕様。 