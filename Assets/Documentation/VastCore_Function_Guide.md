# VastCore プロジェクト機能ガイド

## 概要
VastCoreはUnityベースの高度な地形生成・管理システムです。パフォーマンス最適化とモジュラーアーキテクチャを重視した設計となっています。

## 主な機能カテゴリ

### 🎯 コアシステム
- **Terrain Synthesizer**: 複数地形タイプの合成システム
- **Biome System**: 生態系ベースの地形生成
- **Primitive Terrain Manager**: 地形プリミティブの管理システム

### 🎨 地形生成
- **GPU Accelerated Generation**: GPUを使用した高速地形生成
- **Dynamic Material Blending**: 動的なテクスチャブレンド
- **LOD System**: 距離ベースの詳細度調整

### 🎮 プレイヤー制御
- **Advanced Player Controller**: 高度なプレイヤー制御システム
- **Input System Integration**: 新しいUnity Input System対応
- **Camera Effects**: スプリント時のFOV効果など

### 🔧 診断・監視
- **Vastcore Logger**: 強化されたログシステム
- **Performance Monitoring**: リアルタイムパフォーマンス監視
- **Debug UI**: インゲームデバッグインターフェース

---

## ハンズオンガイド

### Phase 1: 基本セットアップ

#### 1.1 Unityプロジェクトの確認
```
✅ Unityバージョン: 6000.2.2f1
✅ URPレンダリングパイプライン
✅ Input Systemパッケージ (1.14.2)
✅ Burstコンパイラ (1.8.24)
```

#### 1.2 コンパイル確認
1. Unityエディタでプロジェクトを開く
2. コンソールにエラーが表示されていないことを確認
3. `Assets/Scenes/` フォルダにサンプルシーンがあることを確認

### Phase 2: 地形生成テスト

#### 2.1 Terrain Synthesizer のテスト
```
操作手順:
1. ヒエラルキー > Create Empty > TerrainSynthesizer をアタッチ
2. Inspector で地形タイプ定義を設定
3. Context Menu > Generate Synthesized Terrain を実行
4. 地形が生成されることを確認
```

**期待される動作:**
- 異なる地形タイプ（山岳、丘陵、平野、谷、高地）が自然にブレンドされた地形
- 各タイプごとの固有特徴（山岳:高い標高、谷:低い標高など）

#### 2.2 デバッグ機能の確認
```
操作手順:
1. Context Menu > Debug Terrain Type Distribution を実行
2. コンソールに各タイプの分布情報が表示されることを確認
```

### Phase 3: プレイヤー制御テスト

#### 3.1 Player Controller のセットアップ
```
操作手順:
1. 3D Object > Capsule を作成
2. Rigidbody と CapsuleCollider をアタッチ
3. PlayerController スクリプトをアタッチ
4. カメラをシーンに配置
```

**設定パラメータ:**
- **Movement**: `moveForce = 70`, `maxSpeed = 15`, `inputSensitivity = 1.0`
- **Sprint**: `sprintMaxSpeed = 25`, `sprintKey = Key.LeftShift`, `sprintFov = 70`
- **Jump**: `jumpForce = 8`, `jumpKey = Key.Space`
- **Camera**: `cameraOffset = (0,5,-10)`, `enableCameraEffects = true`

#### 3.2 入力テスト
```
テスト項目:
✅ WASD/矢印キー: 移動
✅ Spaceキー: ジャンプ
✅ LeftShift: スプリント（速度上昇 + FOV効果）
✅ F1キー: デバッグUI表示
✅ F12キー: ログUI表示
```

### Phase 4: 診断システムテスト

#### 4.1 Vastcore Logger のテスト
```
操作手順:
1. シーンに VastcoreLogger を配置
2. ゲームを実行
3. F12キーでログUIを表示
4. ログメッセージが表示されることを確認
```

#### 4.2 デバッグUIのテスト
```
操作手順:
1. InGameDebugUI をシーンに配置
2. パラメータパネルを追加
3. F1キーでUIを表示
4. リアルタイムパラメータ調整が可能であることを確認
```

### Phase 5: パフォーマンス監視

#### 5.1 Performance Monitor のテスト
```
確認項目:
✅ FPS表示
✅ メモリ使用量表示
✅ 更新システムのパフォーマンス統計
✅ 60FPS目標時の安定性
```

---

## トラブルシューティング

### コンパイルエラー
**問題**: `The type or namespace name 'InputSystem' does not exist`
**解決**: `Vastcore.Utilities.asmdef` と `Vastcore.Player.asmdef` に `Unity.InputSystem` 参照を追加

**問題**: `The name 'VastcoreLogger' does not exist`
**解決**: `using Vastcore.Core;` を追加

### 地形生成の問題
**問題**: 地形が生成されない
**解決**: TerrainTypeDefinition が正しく設定されているか確認

### 入力が効かない
**問題**: キーボード入力が反応しない
**解決**: Input System が有効になっているか確認（Project Settings > Player > Active Input Handling）

---

## 拡張・カスタマイズガイド

### 新しい地形タイプの追加
1. `TerrainTypeDefinition.cs` に新しいタイプを追加
2. `TerrainSynthesizer.cs` の `GenerateTypeSpecificHeight` にロジックを実装
3. テクスチャとマテリアルを設定

### UIコンポーネントの拡張
1. `InGameDebugUI.cs` に新しいパラメータパネルを追加
2. `SliderBasedUISystem.cs` でUI要素を作成
3. コールバックで実際のシステムに接続

### パフォーマンス最適化
- Burstコンパイラが有効になっていることを確認
- GPU Terrain Generator を使用してCPU負荷を軽減
- LODシステムで描画負荷を最適化

---

## 次の開発ステップ

### 短期目標 (1-2週間)
- [ ] Deformシステムの統合テスト
- [ ] UI移行の完了
- [ ] パフォーマンスベンチマーク

### 中期目標 (1ヶ月)
- [ ] ネットワーク機能の追加
- [ ] 高度なAIエージェントの実装
- [ ] モバイルプラットフォーム対応

### 長期目標 (3ヶ月以上)
- [ ] VR/AR対応
- [ ] クラウドベースの地形生成
- [ ] マルチプレイヤー対応

---

*このガイドは継続的に更新されます。最新情報はプロジェクトドキュメントを確認してください。*
