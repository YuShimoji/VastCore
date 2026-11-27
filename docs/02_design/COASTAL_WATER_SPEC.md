# DS-013 水環境と海岸サンプル

地上視点に、沖の波の高さ、岸で崩れる水、陸に残る水が一続きに見える水環境を作る。2026-11-28 本人選択「波面と岸の表現を共用し、地形・波の条件を手軽に差し替えられるようにする（推奨）」。

| 作業 | 所有先 | 完了条件 |
|---|---|---|
| 水面・接触帯のパッケージ | `Packages/com.vastcore.water` | 波高・周期・向き・色をプリセットで切り替え、岸の形と海況を別々に設定できる |
| 浜と海岸道路 | `Assets/Environments/CoastalWater` | Unity で波の接近、砕波と遡上、護岸から道路へ届く水を確認。護岸を高くしたときの比較を同じ波で撮る |
| 素材と比較 | 海岸サンプルの地表材・固定視点 | 完成想定図・平面水・波面と接触・別の岸での絵を比較し、形／素材／時間の不足を次回の素材選定へ反映する |
| 水中の雛形 | `UnderwaterTemplate.prefab` | 水面・水中視点・海底の基準点と、後続が受け取る座標を残す |

## 見え方の基準

- 浜は沖の輪郭、中景の短い崩れ、足元の濡れた砂と薄い泡を読む。低い視点と見下ろしで遡上の端が岸に沿うことを確かめる。[USGS の遡上写真](https://www.usgs.gov/media/images/waves-flow-a-beach-after-a-wave-break)・[NPS の波と海底勾配](https://home.nps.gov/articles/coastal-processes-waves.htm)
- 道路は波の接近、壁を越える高い飛沫、下がって舗装を横切る水、引いた後の路面が順に読めること。車線と護岸の高さを尺度とする。[Natural Resources Wales の実写、25–26頁](https://naturalresources.wales/media/1936/wales-coastal-flooding-review-phase-1.pdf#page=25)・[Aberystwyth の護岸写真](https://www.geograph.org.uk/photo/5643923)
- 平面水に泡を重ねると遠景は軽い。局所の流体を収録して再生する方式は巻き波や水塊に有力で、岸ごとの合わせ込みが必要。今回は地形と波を組み替えるため、沖のうねりを頂点変形、細波を法線、遡上と越波を同じ位相に従う岸の描画部品で組む。[GPU Gems 1 §1.2](https://developer.nvidia.com/gpugems/gpugems/part-i-natural-effects/chapter-1-effective-water-simulation-physical-models)

## Unity での確認と差し替え

Unity 6000.3.6f1、URP 17.3。`Vastcore > Water > Open Coastal Water Sample` または `Assets/Environments/CoastalWater/CoastalWater.unity` を開いて Play。`1` 砂礫浜、`2` 海岸道路、`3` 岬のカーブ、`N` 次の岸、`Tab` 海況、`C` 近／中／遠、`B` 平面水、`Space` 停止。`B` は同じ視点・光・色で波面と岸の効果を比較する。

1. 海況は `Create > Vastcore > Water > Sea State` または `Presets` の複製で作る。振幅（波頂と谷の差の半分、m）、波長（m）、周期（秒）、岸へ向かう軸からの入射角、細波、泡、水色と素材を設定し、サンプルの `Sea States` に登録する。用意した海況は穏やか・うねり・嵐の3種。
2. 岸は `Sites` の設定を複製して `Shore` を編集する。単位は m、+X が陸、+Z が岸沿い、+Y が上。`Offset` の横軸は Z、縦軸は海岸線の X。砂浜は斜度と砕波位置、護岸は天端高・幅、受け側の高さ・最大幅を渡す。水は平面座標の X 方向で岸に沿った距離を扱う。
3. 別の環境では `Shoreline` と `SeaState` を `CoastalWaterRenderer` に渡し、環境側で `ReceivingY` と一致する地表を用意する。波面と接触に同じ時計を使う。海岸線が Z に対して複数の X を持つ地形への対応は複数区間をつないで検証する。
4. 水中は `UnderwaterTemplate.prefab` のルートを `SeaLevel` に置く。`Waterline`、深さ 3 m の `Observation Anchor`、深さ 12 m の `Seabed Anchor` が後続の水面・カメラ／ボリューム・海底の基準。水中材・光・生物・移動は後続で扱う。

再撮影には Unity の `-batchmode -quit -executeMethod Vastcore.CoastalWater.Editor.CoastalWaterReview.CaptureFromCommandLine -coastalOutput <出力先>` を使う。`-coastalFrames` を付けると目の高さの連番を 20 fps で出す。固定の近中遠3視点を3地点で比較。浜と海岸道路では主光源 60／38／20／10° の近景も撮る。

## 素材の拡充と確認結果

1. 地表は手元の CC0 素材（ambientCG の Ground054・Asphalt010・Grass005 と既存の風化岩）を流用した。最初の Unity 画像は、沖に周期的な細波の筋、浜と路面に大きな泡の輪、壁と路面の間に空隙が出た。
2. 微細法線と泡密度を `scripts/bake-coastal-water.py` で別々に焼き、出所・実寸・取り込み設定を `Assets/Art/Textures/NAT/coastal_water/` に揃えた。波の形を保ったまま模様の大きさを更新でき、泡は砕波・遡上・路面で共用できる。壁面の落水を追加し、動画で水塊が大きく尖った箇所は接触部の分割と高さ、飛沫の散り方を調整した。
3. 同じ波・視点・時刻で護岸の天端を 1.88 m から 6.9 m に上げると路面への新しい水膜が消える。岬のカーブでは同じ波の波頂が岸沿い 15 m 離れた点に約 0.73 秒ずれて届く。Unity Play Mode で岸と海況、平面比較、視点、停止のキー操作を確認した。
4. RTX 4060 Ti、PC・1600×900、カメラ描画＋同期読戻しの中央値で平面比 +0.06〜0.44 ms。場面生成は約 18〜38 ms。テクスチャ検査で追加した2枚の指摘はゼロ。全体の厳格検査には既存の重複・テクセル密度の警告20件が残る。

[完成想定図・Unity の静止画と動画](<D:/AI-Runs/VastCore/coastal-water/index.html>)。完成想定図と比べ、巻き込む波頂の透け、飛沫の奥行き、礫を回り込む水に差が残る。次の地形では先に海面高・岸の断面と目の高さの動画で「接近→衝突→広がり→引き」の順を確認し、形・動き・材質のうち不足が観測されたものを足す。任意の生成海岸の取り込み（岸の曲線データと受け側標高を渡す）と水中の演出は後続。

最初の比較までの着手時見込みは30分。成果物の時刻では、完成想定図からUnity初回撮影まで約25分、最終撮影まで約40分。初回の待ちには別のUnity撮影と重なった時間を含む。
