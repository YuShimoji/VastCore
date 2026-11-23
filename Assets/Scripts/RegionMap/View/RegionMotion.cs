using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 探索画面の動き（SP-021 軸 E）を時間で進める小さな仕組み。
    /// 状態（カーソルの位置・サーチの結果・発見）は即座に変わり、見た目だけがこの時計で後から追いつく。
    /// 入力は動きの終わりを待たない。同じ鍵の動きを始め直すと、前の動きは止まる（終わりの処理もしない）。
    /// 時間は案の秒数で渡し、Length（本人の回答では ×1.6）を掛けて進める。
    /// </summary>
    internal sealed class RegionMotion
    {
        private sealed class Tween
        {
            public object Key;
            public float Start;
            public float Duration;
            public Action<float> Apply;
            public Action Done;
        }

        private readonly List<Tween> _tweens = new List<Tween>();
        private Tween[] _work = new Tween[16];

        /// <summary>動きの時計（秒）。実時間か、撮影の時は 1 フレームごとの決まった刻みで進む。</summary>
        public float Time { get; private set; }

        /// <summary>案の秒数に掛ける倍率。</summary>
        public float Length = 1f;

        /// <summary>false の時、動きは始めずに終わりの姿をすぐ出す（動きなしの画面）。</summary>
        public bool Enabled = true;

        /// <summary>動いている物があるか。</summary>
        public bool Busy
        {
            get { return _tweens.Count > 0; }
        }

        public void Advance(float seconds)
        {
            Time += Mathf.Max(0f, seconds);
            int count = _tweens.Count;
            if (count == 0) return;
            if (_work.Length < count) _work = new Tween[count * 2];
            _tweens.CopyTo(_work);
            for (int i = 0; i < count; i++)
            {
                Tween tween = _work[i];
                _work[i] = null;
                // 途中で止められた動き（同じ鍵で始め直した等）は飛ばす
                if (!_tweens.Contains(tween)) continue;
                float t = (Time - tween.Start) / tween.Duration;
                if (t < 0f) continue;
                if (t >= 1f)
                {
                    _tweens.Remove(tween);
                    tween.Apply(1f);
                    if (tween.Done != null) tween.Done();
                }
                else
                {
                    tween.Apply(t);
                }
            }
        }

        /// <summary>
        /// 動きを始める。delay と seconds は案の秒数（Length を掛けて使う）。遅れの無い動きは始めの姿（apply(0)）をすぐ出す。
        /// 遅れのある動きは、遅れの間は何もしない（始まる前の姿は呼ぶ側が決める）。
        /// 動きが切ってある時は、終わりの姿（apply(1)）と done をすぐ行う。
        /// </summary>
        public void Play(object key, float delay, float seconds, Action<float> apply, Action done = null)
        {
            Cancel(key);
            if (!Enabled || seconds <= 0f)
            {
                apply(1f);
                if (done != null) done();
                return;
            }
            _tweens.Add(new Tween { Key = key, Start = Time + delay * Length, Duration = seconds * Length, Apply = apply, Done = done });
            if (delay <= 0f) apply(0f);
        }

        /// <summary>その鍵の動きを止める。終わりの姿も done も出さない。</summary>
        public void Cancel(object key)
        {
            if (key == null) return;
            for (int i = _tweens.Count - 1; i >= 0; i--)
                if (Equals(_tweens[i].Key, key)) _tweens.RemoveAt(i);
        }

        /// <summary>その鍵の動きを、終わりの姿まで進めて終える。</summary>
        public void Finish(object key)
        {
            for (int i = _tweens.Count - 1; i >= 0; i--)
            {
                if (i >= _tweens.Count || !Equals(_tweens[i].Key, key)) continue;
                Tween tween = _tweens[i];
                _tweens.RemoveAt(i);
                tween.Apply(1f);
                if (tween.Done != null) tween.Done();
            }
        }

        public bool IsPlaying(object key)
        {
            for (int i = 0; i < _tweens.Count; i++)
                if (Equals(_tweens[i].Key, key)) return true;
            return false;
        }

        /// <summary>すべての動きを終わりの姿にする（動きを切った時、地域を作り直した時）。</summary>
        public void FinishAll()
        {
            while (_tweens.Count > 0)
            {
                Tween tween = _tweens[_tweens.Count - 1];
                _tweens.RemoveAt(_tweens.Count - 1);
                tween.Apply(1f);
                if (tween.Done != null) tween.Done();
            }
        }

        // ---- 速さの変わり方（確認ページの cubic-bezier を近い式で置き換えた物） ----

        /// <summary>速く出て、ゆっくり止まる（cubic-bezier(.2,.7,.2,1) 相当）。</summary>
        public static float Out(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        /// <summary>より強く速く出て、長く減速する（波紋の広がり。cubic-bezier(.1,.6,.3,1) 相当）。</summary>
        public static float OutStrong(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 4f);
        }

        /// <summary>ゆっくり出て、速く終わる（閉じる時）。</summary>
        public static float In(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        /// <summary>少し行き過ぎて戻る（印が出る時。cubic-bezier(.3,1.4,.5,1) 相当）。</summary>
        public static float OutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float s = 1.9f;
            float u = t - 1f;
            return 1f + u * u * ((s + 1f) * u + s);
        }

        /// <summary>前半 0〜split で a→b、後半で b→c と、2 段で変わる値。</summary>
        public static float Keys(float t, float split, float a, float b, float c)
        {
            if (t <= split) return Mathf.Lerp(a, b, split > 0f ? t / split : 1f);
            return Mathf.Lerp(b, c, (t - split) / Mathf.Max(0.0001f, 1f - split));
        }
    }
}
