using System;
using System.Collections.Generic;
using System.IO;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 探索画面で音が鳴る場面（SP-021 軸 F）。名前は、差し替えの音のファイル名（拡張子の前）にもなる。
    /// </summary>
    public enum RegionSoundCue
    {
        /// <summary>カーソルの移動（クリック・矢印キー・一覧の行）。十字が滑り始める時。</summary>
        CursorMove,
        /// <summary>サーチの開始。</summary>
        SearchStart,
        /// <summary>反応なし。</summary>
        SignalNone,
        /// <summary>弱い反応。遠征の規則では方角の向きへ左右に寄せる。</summary>
        SignalFaint,
        /// <summary>強い反応。遠征の規則では方角の向きへ左右に寄せる。</summary>
        SignalStrong,
        /// <summary>発見。</summary>
        Found,
        /// <summary>遠征の規則で、発見した地点が拠点になる（日数が戻り、手掛かりが増える）。</summary>
        CampMade,
        /// <summary>一覧の行から、発見カードを開く。</summary>
        CardOpen,
        /// <summary>Esc で発見カードを閉じる。</summary>
        CardClose,
        /// <summary>手掛かりを詳しく読む。</summary>
        ClueRead,
        /// <summary>日数が足りず、サーチや読むことができない。</summary>
        Refused,
        /// <summary>遠征の終わり（日数が尽きた）。</summary>
        ExpeditionEnd,
        /// <summary>遠征の終わり（すべての地点を見つけた）。</summary>
        ExpeditionComplete,
        /// <summary>下端のボタン（描き方・雲・地名・音）と、遠征の「もう一度」。</summary>
        Toggle,
        /// <summary>「別の地域」を押した。</summary>
        NextRegion,
        /// <summary>地域の生成が終わり、地図が出た。</summary>
        RegionReady,
        /// <summary>拡大した所の細部の地形ができて、切り替わった。</summary>
        DetailReady,
    }

    /// <summary>合成音の 1 つの音（波形・高さ・長さ）。</summary>
    public enum RegionSoundWave
    {
        Sine,
        Triangle,
        /// <summary>白色雑音を、高さ（Hz）を切れ目とする低域通過に通した音。</summary>
        Noise,
    }

    /// <summary>合成音の中の 1 つの音。始まり・長さは秒、高さは始めから終わりへ指数で移る。</summary>
    public sealed class RegionSoundNote
    {
        public RegionSoundWave Wave;
        public float Start;
        public float Seconds;
        public float FromHz;
        public float ToHz;
        public float Gain;
        /// <summary>立ち上がりの秒。その後は終わりへ向けて減る。</summary>
        public float Attack = 0.004f;

        public RegionSoundNote(RegionSoundWave wave, float start, float seconds, float fromHz, float toHz, float gain)
        {
            Wave = wave;
            Start = start;
            Seconds = seconds;
            FromHz = fromHz;
            ToHz = toHz;
            Gain = gain;
        }
    }

    /// <summary>1 つの場面の、鳴る時刻と仮の音の作り。</summary>
    public sealed class RegionSoundSpec
    {
        public RegionSoundCue Cue;
        /// <summary>差し替えの音のファイル名（拡張子の前）。</summary>
        public string FileName;
        /// <summary>鳴る場面の説明（確認ページの表と同じ）。</summary>
        public string Scene;
        /// <summary>動きのどの時刻に合わせるか（確認ページの表と同じ）。</summary>
        public string Timing;
        /// <summary>仮の音の言葉での説明。</summary>
        public string Sketch;
        public RegionSoundNote[] Notes;
    }

    /// <summary>
    /// 場面と仮の音の対応表、仮の音の合成、WAV への書き出し。UnityEngine を使わない（テストと書き出しから呼ぶ）。
    /// 音はすべて「仮」。方向と素材は本人が決める（SP-021 9節）。中立な短い合成音にとどめ、場面を聞き分けられる程度の違いだけを付ける。
    /// 置き換えは、Resources/RegionMapSound/ に同じ名前の音を置く（RegionSoundBank）。
    /// </summary>
    public static class RegionSound
    {
        public const int SampleRate = 44100;

        /// <summary>差し替えの音を置く Resources 下のフォルダ。</summary>
        public const string Folder = "RegionMapSound";

        /// <summary>同じ場面を、この秒より短い間隔では鳴らし直さない（矢印キーを続けて押した時など）。</summary>
        public const float RepeatGuard = 0.045f;

        private static readonly RegionSoundSpec[] Specs = BuildSpecs();

        /// <summary>すべての場面（enum の順）。</summary>
        public static IReadOnlyList<RegionSoundSpec> All
        {
            get { return Specs; }
        }

        public static RegionSoundSpec Spec(RegionSoundCue cue)
        {
            int i = (int)cue;
            if (i < 0 || i >= Specs.Length) throw new ArgumentOutOfRangeException(nameof(cue));
            return Specs[i];
        }

        public static string FileName(RegionSoundCue cue)
        {
            return Spec(cue).FileName;
        }

        /// <summary>仮の音の長さ（秒）。いちばん遅く終わる音の終わり。</summary>
        public static float Length(RegionSoundCue cue)
        {
            float end = 0f;
            foreach (RegionSoundNote note in Spec(cue).Notes) end = Math.Max(end, note.Start + note.Seconds);
            return end;
        }

        /// <summary>
        /// 遠征の規則の方角を、左右の寄せ（-1 左〜1 右）にする。東が右、西が左。北・南と方角なしは真ん中。
        /// 寄せは最大 0.6（片耳だけにしない）。
        /// </summary>
        public static float Pan(CompassPoint direction)
        {
            switch (direction)
            {
                case CompassPoint.East: return 0.6f;
                case CompassPoint.NorthEast:
                case CompassPoint.SouthEast: return 0.42f;
                case CompassPoint.West: return -0.6f;
                case CompassPoint.NorthWest:
                case CompassPoint.SouthWest: return -0.42f;
                default: return 0f;
            }
        }

        /// <summary>仮の音を合成する（1 チャンネル、SampleRate、-1〜1）。</summary>
        public static float[] Render(RegionSoundCue cue)
        {
            return Render(Spec(cue).Notes);
        }

        public static float[] Render(RegionSoundNote[] notes)
        {
            float end = 0f;
            foreach (RegionSoundNote note in notes) end = Math.Max(end, note.Start + note.Seconds);
            int length = Math.Max(1, (int)Math.Ceiling(end * SampleRate));
            var samples = new float[length];
            foreach (RegionSoundNote note in notes) Mix(samples, note);
            // 重なって 1 を超える所が無いよう、強さの最大を 0.9 までに収める
            float peak = 0f;
            for (int i = 0; i < samples.Length; i++) peak = Math.Max(peak, Math.Abs(samples[i]));
            if (peak > 0.9f)
            {
                float scale = 0.9f / peak;
                for (int i = 0; i < samples.Length; i++) samples[i] *= scale;
            }
            return samples;
        }

        private static void Mix(float[] samples, RegionSoundNote note)
        {
            int start = (int)Math.Round(note.Start * SampleRate);
            int count = (int)Math.Round(note.Seconds * SampleRate);
            if (count <= 1) return;
            double phase = 0.0;
            double low = 0.0;
            // 雑音は決まった種から作る（毎回同じ音になる）
            uint state = 0x9E3779B9u ^ (uint)(note.FromHz * 131f) ^ (uint)(note.Seconds * 7919f);
            float attack = Math.Max(0.001f, note.Attack) * SampleRate;
            // 端の 3 ms はゆっくり 0 へ寄せる（始まりと終わりの「ぷつっ」を防ぐ）
            float edge = 0.003f * SampleRate;
            for (int i = 0; i < count; i++)
            {
                int at = start + i;
                if (at >= samples.Length) break;
                double x = i / (double)(count - 1);
                double hz = note.FromHz * Math.Pow(note.ToHz / Math.Max(1f, note.FromHz), x);
                double value;
                switch (note.Wave)
                {
                    case RegionSoundWave.Triangle:
                        phase += hz / SampleRate;
                        phase -= Math.Floor(phase);
                        value = 1.0 - 4.0 * Math.Abs(phase - 0.5);
                        break;
                    case RegionSoundWave.Noise:
                        state ^= state << 13;
                        state ^= state >> 17;
                        state ^= state << 5;
                        double white = state / (double)uint.MaxValue * 2.0 - 1.0;
                        double k = 1.0 - Math.Exp(-2.0 * Math.PI * hz / SampleRate);
                        low += k * (white - low);
                        value = low * 1.8;
                        break;
                    default:
                        phase += hz / SampleRate;
                        phase -= Math.Floor(phase);
                        value = Math.Sin(2.0 * Math.PI * phase);
                        break;
                }
                // 立ち上がりの後は、2 乗の曲線で終わりへ減る
                double envelope = i < attack ? i / attack : Math.Pow(1.0 - (i - attack) / Math.Max(1.0, count - attack), 2.0);
                if (i < edge) envelope *= i / edge;
                if (count - 1 - i < edge) envelope *= (count - 1 - i) / edge;
                samples[at] += (float)(value * envelope * note.Gain);
            }
        }

        /// <summary>16 bit・1 チャンネルの WAV にする（確認ページで聞くための書き出し）。</summary>
        public static byte[] ToWav(float[] samples, int sampleRate = SampleRate)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                int data = samples.Length * 2;
                writer.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
                writer.Write(36 + data);
                writer.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E', (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
                writer.Write(data);
                foreach (float sample in samples)
                    writer.Write((short)Math.Round(Math.Max(-1f, Math.Min(1f, sample)) * 32767f));
                writer.Flush();
                return stream.ToArray();
            }
        }

        private static RegionSoundNote N(RegionSoundWave wave, float start, float seconds, float fromHz, float toHz, float gain, float attack = 0.004f)
        {
            return new RegionSoundNote(wave, start, seconds, fromHz, toHz, gain) { Attack = attack };
        }

        // 時刻は動きの案の秒（RegionExplorer が動きの長さ ×1.6 を掛けて鳴らす）。仮の音の中の時間は掛けない
        private static RegionSoundSpec[] BuildSpecs()
        {
            const RegionSoundWave Sine = RegionSoundWave.Sine;
            const RegionSoundWave Tri = RegionSoundWave.Triangle;
            const RegionSoundWave Noise = RegionSoundWave.Noise;
            var list = new List<RegionSoundSpec>
            {
                Make(RegionSoundCue.CursorMove, "カーソルの移動（クリック・矢印キー・一覧の行）", "十字が滑り始める時（0 秒）",
                    "とても短く小さい高めの点", N(Sine, 0f, 0.03f, 1600f, 1500f, 0.16f, 0.002f)),
                Make(RegionSoundCue.SearchStart, "サーチの開始", "サーチした時（0 秒）。波紋が出始める",
                    "短く上がる音", N(Sine, 0f, 0.09f, 600f, 900f, 0.34f)),
                Make(RegionSoundCue.SignalNone, "反応なし", "反応の文字が出る時（0.2 秒）",
                    "低く短く下がる音", N(Tri, 0f, 0.12f, 240f, 200f, 0.3f)),
                Make(RegionSoundCue.SignalFaint, "弱い反応（遠征の規則では方角の向きへ左右に寄せる）", "反応の棒が点き始める時（0.2 秒）",
                    "中くらいの高さの音 1 つ", N(Sine, 0f, 0.22f, 520f, 520f, 0.36f)),
                Make(RegionSoundCue.SignalStrong, "強い反応（遠征の規則では方角の向きへ左右に寄せる）", "反応の棒が点き始める時（0.2 秒）。波紋 2 つに合わせて 2 回",
                    "弱い反応より高い音を 2 回", N(Sine, 0f, 0.12f, 700f, 700f, 0.36f), N(Sine, 0.17f, 0.2f, 700f, 700f, 0.36f)),
                Make(RegionSoundCue.Found, "発見", "反応の棒が点き始める時（0.12 秒）。波紋は 0 秒から",
                    "2 つの音が上へ重なる", N(Sine, 0f, 0.35f, 523f, 523f, 0.3f), N(Sine, 0.12f, 0.45f, 784f, 784f, 0.3f), N(Tri, 0f, 0.3f, 262f, 262f, 0.12f)),
                Make(RegionSoundCue.CampMade, "拠点ができる（遠征の規則の発見。日数が戻り、手掛かりが増える）", "一覧の行と手掛かりが滑り込む時（0.5 秒）",
                    "低めの柔らかい音 2 つ", N(Tri, 0f, 0.3f, 262f, 262f, 0.28f, 0.01f), N(Tri, 0.1f, 0.35f, 392f, 392f, 0.26f, 0.01f)),
                Make(RegionSoundCue.CardOpen, "一覧の行から発見カードを開く（発見の時は発見の音だけ）", "カードが開き始める時（0.46 秒）",
                    "明るくなる短いさっという音", N(Noise, 0f, 0.14f, 800f, 3000f, 0.22f, 0.02f)),
                Make(RegionSoundCue.CardClose, "Esc で発見カードを閉じる（サーチで閉じる時は鳴らさない）", "カードが閉じ始める時（0 秒）",
                    "暗くなる短いさっという音", N(Noise, 0f, 0.11f, 2500f, 700f, 0.18f, 0.01f)),
                Make(RegionSoundCue.ClueRead, "手掛かりを詳しく読む", "詳しい文が出始める時（0 秒）",
                    "紙をめくるような短い音と、小さな高い音", N(Noise, 0f, 0.05f, 3000f, 2000f, 0.2f, 0.002f), N(Sine, 0.04f, 0.08f, 990f, 990f, 0.22f)),
                Make(RegionSoundCue.Refused, "日数が足りずにサーチ・読むことができない", "押した時（0 秒）",
                    "低く鈍い音を 2 回", N(Tri, 0f, 0.09f, 180f, 180f, 0.34f), N(Tri, 0.11f, 0.12f, 150f, 150f, 0.34f)),
                Make(RegionSoundCue.ExpeditionEnd, "遠征の終わり（日数が尽きた）", "結果が開き始める時（サーチで終わった時は 0.46 秒）",
                    "2 つの音が下へ", N(Sine, 0f, 0.3f, 494f, 494f, 0.3f), N(Sine, 0.22f, 0.55f, 392f, 392f, 0.3f)),
                Make(RegionSoundCue.ExpeditionComplete, "遠征の終わり（すべての地点を見つけた）", "結果が開き始める時（サーチで終わった時は 0.46 秒）",
                    "3 つの音が上へ", N(Sine, 0f, 0.3f, 523f, 523f, 0.26f), N(Sine, 0.12f, 0.3f, 659f, 659f, 0.26f), N(Sine, 0.24f, 0.6f, 784f, 784f, 0.26f)),
                Make(RegionSoundCue.Toggle, "下端のボタン（描き方・雲・地名・音）と、遠征の「もう一度」", "押した時（0 秒）",
                    "ごく短いかちっという音", N(Tri, 0f, 0.04f, 1100f, 1000f, 0.24f, 0.002f)),
                Make(RegionSoundCue.NextRegion, "「別の地域」を押した", "押した時（0 秒）。生成に約 10 秒",
                    "長めのさあっという音", N(Noise, 0f, 0.35f, 400f, 1600f, 0.2f, 0.08f)),
                Make(RegionSoundCue.RegionReady, "地域ができて地図が出た（起動時と別の地域）", "地図が出た時（0 秒）",
                    "ゆっくり立ち上がる長めの音", N(Sine, 0f, 0.6f, 440f, 440f, 0.24f, 0.08f), N(Sine, 0.05f, 0.55f, 660f, 660f, 0.1f, 0.08f)),
                Make(RegionSoundCue.DetailReady, "拡大した所の細部の地形ができて切り替わった（×4 以上）", "細部ができた時（0 秒）。絵は 0.4 秒かけて替わる",
                    "小さく高い音 2 つ", N(Sine, 0f, 0.12f, 1320f, 1320f, 0.15f), N(Sine, 0.06f, 0.18f, 1760f, 1760f, 0.15f)),
            };
            return list.ToArray();
        }

        private static RegionSoundSpec Make(RegionSoundCue cue, string scene, string timing, string sketch, params RegionSoundNote[] notes)
        {
            return new RegionSoundSpec { Cue = cue, FileName = cue.ToString(), Scene = scene, Timing = timing, Sketch = sketch, Notes = notes };
        }
    }
}
