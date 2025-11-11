# Unity キャッシュクリーンアップ ガイド

## 目的
循環依存エラー解決後、Unityの内部キャッシュに古い依存関係情報が残っている場合があります。
このガイドでは、完全なキャッシュクリーンアップ手順を提供します。

## 症状
- アセンブリ定義ファイル(.asmdef)を修正したのにエラーが解消されない
- "One or more cyclic dependencies detected" エラーが継続
- "Failed to resolve assembly" エラーが発生
- Burst Compilerがアセンブリ解決に失敗

## クリーンアップ手順

### 手順 1: Unity Editor を完全に終了
```
1. Unity Editor のすべてのウィンドウを閉じる
2. タスクマネージャーで Unity.exe プロセスが残っていないか確認
3. 残っている場合は強制終了
```

### 手順 2: Library フォルダのクリーンアップ（推奨）

#### Option A: 完全削除（最も確実）
```powershell
# プロジェクトルートで実行
Remove-Item -Recurse -Force Library
```

**メリット**:
- すべてのキャッシュを完全にクリア
- 最も確実に問題を解決

**デメリット**:
- 再インポートに時間がかかる（大規模プロジェクトで10-30分）
- すべてのアセットが再処理される

#### Option B: 選択的削除（推奨）
```powershell
# ScriptAssemblies のみ削除（高速）
Remove-Item -Recurse -Force Library\ScriptAssemblies

# Bee ビルドキャッシュ削除（Unity 2021以降）
Remove-Item -Recurse -Force Library\Bee

# アセンブリキャッシュ削除
Remove-Item -Recurse -Force Library\PlayerDataCache\Win64
```

**メリット**:
- 高速（1-5分）
- アセット再インポート不要

**デメリット**:
- 一部のキャッシュが残る可能性がある

### 手順 3: Temp フォルダのクリーンアップ（オプション）
```powershell
Remove-Item -Recurse -Force Temp
```

### 手順 4: Unity Editor 再起動と再コンパイル

1. Unity Hub から Unity Editor を起動
2. プロジェクトを開く
3. 自動的にスクリプトの再コンパイルが開始される
4. Console ウィンドウでエラーを確認

### 手順 5: 手動再コンパイル（必要に応じて）

```
Unity Editor メニュー:
Assets → Reimport All
```

**注意**: 大規模プロジェクトでは非常に時間がかかります（30分以上）

## トラブルシューティング

### エラーが解消されない場合

#### 1. .csproj ファイルの再生成
```
Unity Editor メニュー:
Edit → Preferences → External Tools → Regenerate project files
```

#### 2. PackageCache のクリア
```powershell
Remove-Item -Recurse -Force Library\PackageCache
```

その後、Unity Editor を再起動。

#### 3. グローバルキャッシュのクリア（最終手段）
```powershell
# Unity Editor のグローバルキャッシュ
Remove-Item -Recurse -Force "$env:LOCALAPPDATA\Unity\cache"
```

### Burst Compiler 特有の問題

Burst Compiler が古いアセンブリ情報を保持している場合：

```
Unity Editor メニュー:
Jobs → Burst → Clear Burst Cache
```

または手動で：
```powershell
Remove-Item -Recurse -Force Library\BurstCache
```

## 推奨ワークフロー

### アセンブリ定義変更後の標準手順

1. **Unity Editor を閉じる**
2. **選択的削除を実行**:
   ```powershell
   Remove-Item -Recurse -Force Library\ScriptAssemblies
   Remove-Item -Recurse -Force Library\Bee
   ```
3. **Unity Editor を起動**
4. **コンパイル結果を確認**
5. **問題が残る場合は完全削除**:
   ```powershell
   Remove-Item -Recurse -Force Library
   ```

## 今回の修正後に実行すべきコマンド

```powershell
# Unity Editor を閉じてから実行

# 1. プロジェクトルートに移動
cd C:\Users\PLANNER007\VastCore\VastCore

# 2. スクリプトアセンブリキャッシュを削除（推奨）
Remove-Item -Recurse -Force Library\ScriptAssemblies

# 3. Beeキャッシュを削除（Unity 2021+）
Remove-Item -Recurse -Force Library\Bee

# 4. Unity Editor を起動してコンパイル確認
```

## 予防策

### 1. .gitignore の確認
Library フォルダが .gitignore に含まれていることを確認：
```gitignore
/Library/
/Temp/
/Obj/
/Build/
/Builds/
/Logs/
```

### 2. アセンブリ定義変更時のベストプラクティス
- 大きな変更前にバックアップ
- 一度に1つの循環依存を解消
- 各変更後にキャッシュクリア
- 段階的にコミット

### 3. CI/CD での自動クリーンアップ
```yaml
# Example GitHub Actions
- name: Clean Unity Cache
  run: |
    Remove-Item -Recurse -Force Library -ErrorAction SilentlyContinue
```

## FAQ

### Q: Library フォルダを削除しても安全ですか？
A: はい。Library フォルダは Unity が自動生成するキャッシュです。削除しても Assets や ProjectSettings には影響ありません。

### Q: どのくらいの頻度でクリーンアップすべきですか？
A: 通常は不要です。アセンブリ定義変更時や依存関係エラー発生時のみ実行してください。

### Q: ScriptAssemblies だけ削除すれば十分ですか？
A: 多くの場合は十分です。解決しない場合は Library 全体を削除してください。

### Q: Reimport All は必要ですか？
A: ScriptAssemblies のみ削除した場合は不要です。Library 全体を削除した場合は自動的に実行されます。

## 参考情報

- Unity Documentation: [Assembly Definition Files](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html)
- Unity Forum: [Clearing the Library Folder](https://forum.unity.com/threads/clearing-the-library-folder.506927/)
- Burst Compiler: [Troubleshooting](https://docs.unity3d.com/Packages/com.unity.burst@latest/index.html?subfolder=/manual/debugging-profiling.html)

## 今回の修正で解決される問題

- ✅ Terrain → Player → Terrain 循環依存
- ✅ Vastcore.UI アセンブリ解決エラー
- ✅ Burst Compiler のアセンブリ参照エラー
- ✅ Play mode 実行不可問題

## 次のステップ

1. **即座に実行**: 上記の推奨コマンドでキャッシュクリア
2. **Unity Editor 起動**: コンパイル成功を確認
3. **Play mode テスト**: プロジェクトが正常に動作するか確認
4. **問題が残る場合**: 完全削除（Library 全体）を実行

---

**最終更新**: 2025-11-11 (Commit: 597fcc9)
