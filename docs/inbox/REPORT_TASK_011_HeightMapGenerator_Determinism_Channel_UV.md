# REPORT_TASK_011: HeightMapGenerator 改善（決定論/チャンネル/UV/反転）

**Status**: COMPLETED  
**Date**: 2025-01-11  
**Owner**: Worker

## 実装内容

### 1. HeightMap のチャンネル対応

`HeightMapGenerator.GenerateFromHeightMap` に `GetChannelValue` メソッドを追加し、`HeightMapChannel` に従って `R/G/B/A/Luminance` を選択して高さ値にできるようにしました。

**変更ファイル**:
- `Assets/MapGenerator/Scripts/HeightMapGenerator.cs`

**実装詳細**:
```csharp
private static float GetChannelValue(Color color, HeightMapChannel channel)
{
    return channel switch
    {
        HeightMapChannel.R => color.r,
        HeightMapChannel.G => color.g,
        HeightMapChannel.B => color.b,
        HeightMapChannel.A => color.a,
        HeightMapChannel.Luminance => color.grayscale,
        _ => color.grayscale
    };
}
```

### 2. UV Offset / UV Tiling 対応

`GenerateFromHeightMap` メソッドで、UV座標の計算時に `UVOffset` と `UVTiling` を適用するようにしました。

**実装詳細**:
- UV座標計算時に `u * generator.UVTiling.x + generator.UVOffset.x` を適用
- テクスチャの繰り返しを考慮して `(u % 1f)` で正規化

### 3. Invert 対応

`InvertHeight=true` の場合、高さ値を `h = 1 - h` で反転する処理を追加しました。

**実装詳細**:
```csharp
if (generator.InvertHeight)
{
    height = 1f - height;
}
```

### 4. Seed の決定論（最小実装）

Noise モードで Seed から決定論的な Offset を生成し、同一Seedで同一結果が得られるようにしました。

**実装詳細**:
- `GetDeterministicOffsetFromSeed` メソッドを追加
- `System.Random(seed)` を使用して決定論的なオフセットを生成
- 生成されたオフセットを既存の `Offset` に加算

```csharp
private static Vector2 GetDeterministicOffsetFromSeed(int seed)
{
    System.Random rng = new System.Random(seed);
    float offsetX = (float)(rng.NextDouble() * 1000.0);
    float offsetY = (float)(rng.NextDouble() * 1000.0);
    return new Vector2(offsetX, offsetY);
}
```

### 5. TerrainGenerator の Profile 連携

`TerrainGenerator` に Channel/UV/Invert/Seed のプロパティを追加し、`LoadFromProfile` と `SaveToProfile` でこれらの値を読み書きできるようにしました。

**変更ファイル**:
- `Assets/MapGenerator/Scripts/TerrainGenerator.cs`

**追加プロパティ**:
- `HeightMapChannel`
- `UVOffset`
- `UVTiling`
- `InvertHeight`
- `Seed`

## 変更ファイル一覧

1. `Assets/MapGenerator/Scripts/HeightMapGenerator.cs`
   - Channel 対応の `GetChannelValue` メソッド追加
   - UV Offset/Tiling 対応
   - Invert 対応
   - Seed の決定論実装

2. `Assets/MapGenerator/Scripts/TerrainGenerator.cs`
   - Channel/UV/Invert/Seed プロパティ追加
   - `LoadFromProfile` でこれらの値を読み込む処理追加
   - `SaveToProfile` でこれらの値を保存する処理追加

## 検証手順

### 1. Channel 対応の検証

1. Unity Editor で `TerrainGenerationWindow` を開く
2. `Generation Mode` を `HeightMap` に設定
3. `HeightMap Texture` を設定
4. `Channel` を `R`, `G`, `B`, `A`, `Luminance` に変更して生成
5. 各チャンネルで異なる結果が得られることを確認

### 2. UV Offset/Tiling の検証

1. `UV Offset` を変更して生成
2. 地形のパターンがオフセットされることを確認
3. `UV Tiling` を変更して生成
4. 地形のパターンが繰り返されることを確認

### 3. Invert の検証

1. `Invert Height` を `true` に設定して生成
2. 高さが反転されることを確認（高い部分が低く、低い部分が高くなる）

### 4. Seed の決定論の検証

1. `Generation Mode` を `Noise` に設定
2. `Seed` を `123` に設定して生成
3. 再度 `Seed` を `123` に設定して生成
4. 2回の生成結果が同一であることを確認
5. `Seed` を `456` に変更して生成
6. 異なる結果が得られることを確認

### 5. NoiseAndHeightMap モードの検証

1. `Generation Mode` を `NoiseAndHeightMap` に設定
2. 生成が正常に動作することを確認
3. HeightMap の Channel/UV/Invert 設定が反映されることを確認

## 互換性

- 既存のアセット/シーンとの互換性は維持されています
- デフォルト値は既存の動作と一致するように設定されています
  - `HeightMapChannel`: `Luminance`（既存の `grayscale` と同等）
  - `UVOffset`: `Vector2.zero`
  - `UVTiling`: `Vector2.one`
  - `InvertHeight`: `false`
  - `Seed`: `0`

## 注意事項

- UV Tiling の実装では、テクスチャの繰り返しを `% 1f` で処理しています
- Seed の決定論は `System.Random` を使用した最小実装です
- 既存の `Offset` プロパティは Seed から生成されたオフセットに加算されます

## 今後の改善案

- Seed の決定論をより高度な方法に改善（ハッシュ関数の使用など）
- UV Tiling の繰り返しモード（Clamp/Wrap/Mirror）の追加
- パフォーマンス最適化（チャンネル選択の最適化など）

