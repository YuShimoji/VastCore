# SG-1: Structure Generator Tab テスト検証計画

**作成日**: 2025-12-03  
**ステータス**: 検証準備中  
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
| RandomControlTab | ✅ 存在 | 有効 | ランダム化制御 |
| OperationsTab | ❌ **不在** | コメントアウト | CSG演算 |
| CompositionTab | ❌ **不在** | コメントアウト | 合成機能 |

### 1.2 FUNCTION_TEST_STATUS.md との乖離

**Composition Tab（FUNCTION_TEST_STATUS.mdに記載あり）:**
- Union, Intersection, Difference → **実装ファイル不在**
- Layered/Surface/Adaptive/Noise Blend → **実装ファイル不在**
- Morph, Volumetric Blend, Distance Field → **実装ファイル不在**

**結論**: FUNCTION_TEST_STATUS.mdの記載は過去の計画または別ブランチの実装を参照している可能性あり。

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

```
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

```
1. 上記と同様にセットアップ
2. Rotation Randomization セクションを展開
3. X(Pitch)/Y(Yaw)/Z(Roll) の範囲を設定
4. プレビューモードで確認
5. 適用後、各オブジェクトの回転が範囲内でランダム化されていることを確認
```

#### Scale Randomization テスト

```
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

```
1. オブジェクトを選択してプレビューモードをON
2. 「復元」ボタンで元の状態に戻ることを確認
3. 「適用」ボタンで変更が確定されることを確認
4. タブ切り替え時にプレビューが解除されることを確認
```

---

## 3. 未実装機能の対応計画

### 3.1 CompositionTab（未実装）

**FUNCTION_TEST_STATUS.mdに記載の機能:**
- CSG演算: Union, Intersection, Difference
- ブレンド: Layered, Surface, Adaptive, Noise
- 高度: Morph, Volumetric Blend, Distance Field

**対応方針:**
1. **短期**: FUNCTION_TEST_STATUS.mdを現状に合わせて更新（「未実装」と明記）
2. **中期**: CompositionTabのスケルトン実装を検討
3. **長期**: ProBuilder CSG機能との統合実装

### 3.2 OperationsTab（未実装）

**対応方針:**
- CompositionTabと同様に処理

### 3.3 Mesh Deformation（RandomControlTab）

**FUNCTION_TEST_STATUS.mdの記載:**
> Mesh Deformation - メッシュ頂点レベルの変形

**現状**: RandomControlTab には Transform レベルの変形のみ実装。メッシュ頂点変形は未実装。

**対応方針:**
1. DeformerTab（Deformパッケージ）での対応を推奨
2. または RandomControlTab に頂点変形機能を追加

---

## 4. テスト実行チェックリスト

### RandomControlTab

- [ ] Position Randomization - 相対モード
- [ ] Position Randomization - 絶対モード
- [ ] Rotation Randomization - 全軸
- [ ] Scale Randomization - Uniform モード
- [ ] Scale Randomization - Individual Axis モード
- [ ] Preview Mode - 復元機能
- [ ] Preview Mode - 適用機能
- [ ] Real-time Update - スライダー操作でのリアルタイム反映

### 合否基準

- エラー/例外が発生しない
- 指定範囲内でランダム化される
- Preview/復元が正常に動作する
- Undo/Redo が正常に動作する

---

## 5. 次のアクション

| 優先度 | アクション | 担当 | 期限 |
|--------|-----------|------|------|
| **高** | RandomControlTab の手動テスト実行 | 手動 | 次セッション |
| **高** | FUNCTION_TEST_STATUS.md の更新（実態との乖離修正） | 自動 | 今セッション |
| **中** | CompositionTab スケルトン作成検討 | 要決定 | 1週間 |
| **低** | Mesh Deformation 実装方針決定 | 要決定 | 2週間 |

---

**最終更新**: 2025-12-03  
**作成者**: Cascade AI Assistant
