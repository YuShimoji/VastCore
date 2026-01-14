# SG-1: Structure Generator Tab テスト検証計画

**作成日**: 2025-12-03  
**最終更新**: 2025-12-12  
**ステータス**: SG-2 網羅的テスト実施中  
**目的**: Composition/Random Tab 未テスト機能の検証準備

---

## 1. 現状調査結果

### 1.1 実装状況の確認

| タブ名 | ファイル | 状態 | 備考 |
|--------|----------|------|------|
| GlobalSettingsTab | ✅ 存在 | 有効 | マテリアルパレット管理 |
| BasicStructureTab | ✅ 存在 | 有効 | 基本構造物生成 |
| AdvancedStructureTab | ✅ 存在 | 有効 | 高度構造物生成 |
| RelationshipTab | ✅ 存在 | 有効 | 空間関係制御 |
| ParticleDistributionTab | ✅ 存在 | 有効 | 配置パターン |
| DeformerTab | ✅ 存在 | 有効 | Deform統合（条件付きコンパイル） |
| RandomControlTab | ✅ 存在 | 有効 | ランダム化制御（Undo対応済み） |
| OperationsTab | ❌ **不在** | コメントアウト | CSG演算 |
| CompositionTab | ✅ 存在 | 有効 | UIスケルトン実装済み（CT-1で実装中） |

### 1.2 FUNCTION_TEST_STATUS.md との乖離（2025-12-12 更新）

**Composition Tab:**

- `CompositionTab.cs` が存在し、UIスケルトンは実装済み
- Union, Intersection, Difference → **コード実装済み**（ただし `#if HAS_PARABOX_CSG` により、Parabox.CSG 未導入環境では無効）
- Layered/Surface/Adaptive/Noise Blend → **UIのみ、コアロジック未実装**
- Morph, Volumetric Blend, Distance Field → **UIのみ、コアロジック未実装**

**結論**: FUNCTION_TEST_STATUS.md の「CSGコード実装済み（Parabox.CSG待ち）」という記載が現状と整合している。

---

## 2. RandomControlTab テスト計画

### 2.1 実装済み機能

| 機能 | 実装状況 | テスト状態 |
|------|----------|-----------|
| Position Randomization | ✅ 実装済み | 🟡 要検証 |
| Rotation Randomization | ✅ 実装済み | 🟡 要検証 |
| Scale Randomization | ✅ 実装済み | 🟡 要検証 |
| Preview Mode | ✅ 実装済み | 🟡 要検証 |
| Real-time Update | ✅ 実装済み | 🟡 要検証 |

### 2.2 テスト手順

#### Position Randomization テスト

```text
1. Unity Editor でシーンを開く
2. Tools > Vastcore > Structure Generator を開く
3. 「Random Control」タブを選択
4. シーン内で複数のGameObjectを選択
5. 「ランダム化を有効にする」をON
6. Position Randomization セクションを展開
7. X/Y/Z の範囲スライダーを調整
8. 「プレビューモード」をONにして変化を確認
9. 「Apply to Selected」をクリック
10. 結果を確認:
    - 各オブジェクトの位置が指定範囲内でランダム化されていること
    - 「相対位置を使用」がONの場合、元の位置からの相対移動であること
```

#### Rotation Randomization テスト

```text
1. 上記と同様にセットアップ
2. Rotation Randomization セクションを展開
3. X(Pitch)/Y(Yaw)/Z(Roll) の範囲を設定
4. プレビューモードで確認
5. 適用後、各オブジェクトの回転が範囲内でランダム化されていることを確認
```

#### Scale Randomization テスト

```text
1. 上記と同様にセットアップ
2. Scale Randomization セクションを展開
3. Uniform Scale モードでテスト:
   - 最小・最大スケールを設定
   - 全軸が同一のスケール値になることを確認
4. Individual Axis Control モードでテスト:
   - X/Y/Z それぞれの範囲を設定
   - 各軸が独立してランダム化されることを確認
```

#### Preview Mode テスト

```text
1. オブジェクトを選択してプレビューモードをON
2. 「復元」ボタンで元の状態に戻ることを確認
3. 「適用」ボタンで変更が確定されることを確認
4. タブ切り替え時にプレビューが解除されることを確認
```

---

## 3. 未実装機能の対応計画

### 3.1 CompositionTab（CT-1）

**FUNCTION_TEST_STATUS.mdに記載の機能:**

- CSG演算: Union, Intersection, Difference
- ブレンド: Layered, Surface, Adaptive, Noise
- 高度: Morph, Volumetric Blend, Distance Field

**現状（2025-12-12時点）:**

- CSG演算（Union/Intersection/Difference）は **コード実装済み** だが、`#if HAS_PARABOX_CSG` により Parabox.CSG 未導入環境では無効
- ブレンド/高度機能は **UIのみ（処理未実装）**

**対応方針:**

1. **最優先**: CSG依存方針の決定（ProBuilder内蔵CSGを第一候補 → 失敗理由を切り分け → フォールバック検討）
2. **短期**: Union の最小動作確認（結果メッシュ/Undo/元オブジェクトの非表示・削除）
3. **中期**: Intersection/Difference の動作確認、複数オブジェクト処理の検証
4. **長期**: Blend/高度機能（Morph/Volumetric/Distance Field）の設計・実装

### 3.2 OperationsTab（未実装）

**対応方針:**

- 現状は `StructureGeneratorWindow.cs` でコメントアウトされ、実装ファイルも不在
- 原則として CompositionTab（CT-1）に統合する前提で整理し、必要が出た場合のみ復活を検討

### 3.3 Mesh Deformation（RandomControlTab）

**FUNCTION_TEST_STATUS.mdの記載:**
> Mesh Deformation - メッシュ頂点レベルの変形

**現状**: RandomControlTab には Transform レベルの変形のみ実装。メッシュ頂点変形は未実装。

**対応方針:**

1. DeformerTab（Deformパッケージ）での対応を推奨
2. または RandomControlTab に頂点変形機能を追加

---

## 4. テスト実行チェックリスト

### RandomControlTab（SG-2 網羅的テスト）

#### Position Randomization
- [ ] **P-1**: 相対モード - 元位置からの相対移動が正しく機能する
- [ ] **P-2**: 絶対モード - 指定範囲内の絶対位置に配置される
- [ ] **P-3**: X/Y/Z 軸個別制御 - 各軸が独立して機能する

#### Rotation Randomization
- [ ] **R-1**: Pitch(X)/Yaw(Y)/Roll(Z) 個別制御が機能する
- [ ] **R-2**: 指定範囲内で回転がランダム化される

#### Scale Randomization
- [ ] **S-1**: Uniform モード - 全軸が同一スケール値になる
- [ ] **S-2**: Individual Axis モード - 各軸が独立してランダム化される
- [ ] **S-3**: 最小・最大制約が正しく適用される

#### Preview Mode
- [ ] **PV-1**: プレビューON時に変更がリアルタイム反映される
- [ ] **PV-2**: 「復元」ボタンで元の状態に戻る
- [ ] **PV-3**: 「適用」ボタンで変更が確定される
- [ ] **PV-4**: タブ切り替え時にプレビューが適切に処理される

#### Real-time Update
- [ ] **RT-1**: スライダー操作で即時にプレビューが更新される

#### Undo/Redo 対応（2025-12-05 実装済み）
- [ ] **U-1**: 適用後に Ctrl+Z で Undo が機能する
- [ ] **U-2**: Undo 後に Ctrl+Y で Redo が機能する
- [ ] **U-3**: 複数オブジェクト選択時も Undo/Redo が正常に動作する

### 合否基準

- エラー/例外が発生しない
- 指定範囲内でランダム化される
- Preview/復元が正常に動作する
- Undo/Redo が正常に動作する
- 複数オブジェクト選択時も安定動作する

---

## 5. 次のアクション（2025-12-12 更新）

| 優先度 | アクション | 担当 | 状態 |
|--------|-----------|------|------|
| **高** | RandomControlTab の網羅的手動テスト実行（SG-2） | ユーザー | 🟡 実施中 |
| ~~高~~ | ~~FUNCTION_TEST_STATUS.md の更新~~ | - | ✅ 完了（2025-12-05） |
| ~~中~~ | ~~CompositionTab スケルトン作成~~ | - | ✅ 完了（CT-1 UIスケルトン） |
| **高** | CT-1: CSG 依存方針決定 + Union 最小動作確認 | AI | 🟡 次フェーズ |
| **中** | RC-1: 高度機能実装（Adaptive/Preset/MeshDeform） | AI | ⏳ 準備中 |

---

## 6. テスト結果記録（SG-2 実施時に記入）

### テスト環境
- **Unity バージョン**: 6000.2.2f1
- **実施日**: ____-__-__
- **テスター**: ________

### 結果サマリ

| カテゴリ | 合格 | 不合格 | 備考 |
|----------|------|--------|------|
| Position | /3 | | |
| Rotation | /2 | | |
| Scale | /3 | | |
| Preview | /4 | | |
| Real-time | /1 | | |
| Undo/Redo | /3 | | |
| **合計** | /16 | | |

### 発見した問題

1. （テスト時に記入）

---

**最終更新**: 2025-12-12  
**作成者**: Cascade AI Assistant
