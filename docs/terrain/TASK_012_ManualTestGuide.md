# TASK_012 手動テストガイド

**対象**: TerrainGenerationWindow プリセット管理機能  
**所要時間**: 約10-15分  
**前提条件**: Unity Editor上で動作確認

---

## 1. ウィンドウ起動確認

1. Unity Editorを起動
2. `Tools > Vastcore > Terrain > Terrain Generation (v0)` を選択
3. **確認**: ウィンドウが正常に開くこと
4. **確認**: 「Presets」セクションが表示されること（「Profile」セクションの上）

---

## 2. プリセット保存機能の確認

### 2.1 基本保存

1. ウィンドウ内で地形設定を変更:
   - `Seed = 123`
   - `Scale = 100`
   - `Octaves = 4`
   - `Persistence = 0.5`
   - `Lacunarity = 2.0`

2. 「Presets」セクションで「Save Current Settings as Preset」をクリック

3. ファイル保存ダイアログが表示される:
   - **注意**: Unity Editorには標準の入力ダイアログがないため、ファイル保存ダイアログを使用
   - ファイル名に `MountainTerrain` と入力（拡張子は不要）
   - 「Save」をクリック

4. **確認項目**:
   - `Assets/TerrainPresets/` フォルダが自動作成されること
   - `Assets/TerrainPresets/TerrainPreset_MountainTerrain.asset` が作成されること
   - 成功ダイアログが表示されること
   - プリセット一覧に「MountainTerrain」が表示されること

### 2.2 複数プリセット保存

1. 設定を変更（例: `Seed = 456`, `Scale = 50`）
2. 「Save Current Settings as Preset」をクリック
3. ファイル名に `PlainsTerrain` と入力して保存
4. **確認項目**:
   - `TerrainPreset_PlainsTerrain.asset` が作成されること
   - プリセット一覧に「MountainTerrain」と「PlainsTerrain」の両方が表示されること

### 2.3 既存プリセット上書き確認

1. 現在の設定を変更（例: `Seed = 789`）
2. 「Save Current Settings as Preset」をクリック
3. ファイル名に `MountainTerrain` と入力（既存のプリセット名）
4. **確認項目**:
   - 上書き確認ダイアログが表示されること
   - 「Overwrite」を選択すると既存プリセットが更新されること
   - 「Cancel」を選択すると保存がキャンセルされること

---

## 3. プリセット読み込み機能の確認

### 3.1 ドロップダウン選択

1. ウィンドウ内の設定を変更（例: `Seed = 999`）
2. 「Presets」セクションのドロップダウンで「MountainTerrain」を選択
3. **確認項目**:
   - ドロップダウンに保存済みプリセットが表示されること
   - 選択したプリセット名が表示されること

### 3.2 プリセット読み込み

1. ドロップダウンで「MountainTerrain」を選択
2. 「Load Preset」ボタンをクリック
3. **確認項目**:
   - ウィンドウ内の設定が元の値（`Seed = 123`, `Scale = 100`, `Octaves = 4`）に戻ること
   - すべての設定項目（Generation Mode, Size, HeightMap, Noise など）が正しく読み込まれること

### 3.3 別プリセット読み込み

1. ドロップダウンで「PlainsTerrain」を選択
2. 「Load Preset」ボタンをクリック
3. **確認項目**:
   - 設定が「PlainsTerrain」の値（`Seed = 456`, `Scale = 50`）に変更されること

---

## 4. プリセット削除機能の確認

1. ドロップダウンで「PlainsTerrain」を選択
2. 「Delete Preset」ボタンをクリック
3. **確認項目**:
   - 削除確認ダイアログが表示されること
   - 「Delete」を選択すると `TerrainPreset_PlainsTerrain.asset` が削除されること
   - 「Cancel」を選択すると削除がキャンセルされること
   - 削除後、プリセット一覧から「PlainsTerrain」が消えること

---

## 5. プリセット一覧更新機能の確認

1. Unity EditorのProjectウィンドウで `Assets/TerrainPresets/` フォルダを直接操作:
   - 新しい `TerrainGenerationProfile` アセットを手動で作成
   - または、既存のプリセットアセットを削除

2. TerrainGenerationWindowで「Refresh Preset List」ボタンをクリック

3. **確認項目**:
   - プリセット一覧が最新の状態に更新されること
   - 手動で追加したプリセットが表示されること
   - 手動で削除したプリセットが一覧から消えること

---

## 6. 既存機能との互換性確認

### 6.1 Profileセクションの動作確認

1. 「Profile」セクションを確認
2. **確認項目**:
   - 「Load From Profile」「Save To Profile」「Create New Profile」ボタンが正常に動作すること
   - 既存の `TerrainGenerationProfile` アセットが正常に読み込めること
   - プリセット機能とProfile機能が独立して動作すること

### 6.2 地形生成機能の動作確認

1. プリセットを読み込んだ後、「Actions」セクションで「Generate Preview」をクリック
2. **確認項目**:
   - プリセットから読み込んだ設定で地形が正常に生成されること
   - エラーが発生しないこと

---

## 7. エラーハンドリング確認

### 7.1 無効なプリセット名

1. 「Save Current Settings as Preset」をクリック
2. ファイル名に無効な文字を含む名前を入力（例: `Test/Preset`）
3. **確認項目**:
   - 無効な文字が自動的に削除されること
   - または、エラーメッセージが表示されること

### 7.2 空のプリセット名

1. 「Save Current Settings as Preset」をクリック
2. ファイル名を空にして保存を試行
3. **確認項目**:
   - 保存がキャンセルされること
   - または、エラーメッセージが表示されること

---

## 8. 完了確認チェックリスト

- [ ] プリセット保存機能が正常に動作する
- [ ] プリセット読み込み機能が正常に動作する
- [ ] プリセット削除機能が正常に動作する
- [ ] プリセット一覧更新機能が正常に動作する
- [ ] 既存のProfile機能が正常に動作する
- [ ] 地形生成機能が正常に動作する
- [ ] エラーハンドリングが適切に動作する
- [ ] `Assets/TerrainPresets/` フォルダが自動作成される
- [ ] プリセットアセットが正しい場所に保存される

---

## トラブルシューティング

### プリセットが表示されない

- 「Refresh Preset List」ボタンをクリック
- `Assets/TerrainPresets/` フォルダが存在することを確認
- プリセットアセットが `TerrainGenerationProfile` 型であることを確認

### プリセット読み込み時に設定が反映されない

- ウィンドウを閉じて再度開く
- Unity Editorを再コンパイル（スクリプトを再読み込み）

### ファイル保存ダイアログが表示されない

- Unity Editorのコンソールでエラーを確認
- ウィンドウを閉じて再度開く

---

## 検証完了後のアクション

手動テストが完了したら、以下を実施:

1. テスト結果を `docs/inbox/REPORT_TASK_012_TerrainGenerationWindow_PresetManagement.md` に追記
2. 問題がなければ、ブランチをpush:
   ```bash
   git push origin feature/TASK_012_terrain-window-preset-management
   ```
3. 必要に応じてPull Requestを作成
