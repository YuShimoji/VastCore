using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 差し替えの音を読む入口（RegionMapSkin と同じ考え方）。
    /// <c>Assets/Resources/RegionMapSound/</c> に、場面の名前（RegionSoundCue の名前。大文字小文字を区別）の音のファイルを置くと、
    /// その場面だけ仮の音の代わりに鳴る。置いていない場面は、コードで作る仮の音のまま。手順は SP-021 6節。
    /// </summary>
    public static class RegionSoundBank
    {
        /// <summary>Resources から読む。置いていなければ null。</summary>
        public static AudioClip Load(string name)
        {
            return Resources.Load<AudioClip>(RegionSound.Folder + "/" + name);
        }

        /// <summary>
        /// 場面ごとに鳴らす音を決める。loader が返した音があればそれ（差し替え）、無ければ仮の音を作る。
        /// custom には、差し替えた場面だけが入る。
        /// </summary>
        public static Dictionary<RegionSoundCue, AudioClip> Resolve(Func<string, AudioClip> loader, HashSet<RegionSoundCue> custom)
        {
            var clips = new Dictionary<RegionSoundCue, AudioClip>();
            foreach (RegionSoundSpec spec in RegionSound.All)
            {
                AudioClip clip = loader != null ? loader(spec.FileName) : null;
                if (clip != null)
                {
                    if (custom != null) custom.Add(spec.Cue);
                }
                else
                {
                    clip = CreateClip(spec.Cue);
                }
                clips[spec.Cue] = clip;
            }
            return clips;
        }

        /// <summary>仮の音を AudioClip にする。名前は「仮_場面の名前」。</summary>
        public static AudioClip CreateClip(RegionSoundCue cue)
        {
            float[] samples = RegionSound.Render(cue);
            AudioClip clip = AudioClip.Create("仮_" + RegionSound.FileName(cue), samples.Length, 1, RegionSound.SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }

    /// <summary>鳴らした音の記録（確かめる時に読む）。</summary>
    public sealed class RegionSoundRecord
    {
        public RegionSoundCue Cue;
        /// <summary>動きの時計で、鳴らした時刻。</summary>
        public float Time;
        public string ClipName;
        public bool Custom;
        public float Volume;
        public float Pan;
        /// <summary>鳴らした後のフレームで、AudioSource が再生中で、再生位置が 0 より先へ進んでいるのを見た。</summary>
        public bool Confirmed;
        internal AudioSource Source;
        internal int Frames;
    }

    /// <summary>
    /// 探索画面の音を鳴らす（SP-021 軸 F）。時刻は探索画面の動きの時計（RegionMotion.Time）で受け取り、動きと同じ拍で鳴らす。
    /// 鳴らすのは、子に作った AudioSource の組（同時に Voices 個まで）。音を切ってある時と音量 0 の時は鳴らさない。
    /// </summary>
    public sealed class RegionSoundPlayer
    {
        public const int Voices = 6;
        private const int MaxRecords = 400;

        private sealed class Booking
        {
            public RegionSoundCue Cue;
            public float At;
            public float Pan;
        }

        private readonly Dictionary<RegionSoundCue, AudioClip> _clips;
        private readonly HashSet<RegionSoundCue> _custom = new HashSet<RegionSoundCue>();
        private readonly List<AudioSource> _sources = new List<AudioSource>();
        private readonly List<Booking> _pending = new List<Booking>();
        private readonly List<RegionSoundRecord> _records = new List<RegionSoundRecord>();
        private readonly List<RegionSoundRecord> _watching = new List<RegionSoundRecord>();
        private readonly Dictionary<RegionSoundCue, float> _lastAt = new Dictionary<RegionSoundCue, float>();
        private readonly int[] _played = new int[Enum.GetValues(typeof(RegionSoundCue)).Length];
        private readonly int[] _confirmed = new int[Enum.GetValues(typeof(RegionSoundCue)).Length];
        private readonly int[] _silenced = new int[Enum.GetValues(typeof(RegionSoundCue)).Length];
        private int _nextVoice;
        private float _now;

        /// <summary>false の時は鳴らさない（全部を切るスイッチ）。</summary>
        public bool Enabled = true;

        /// <summary>音量（0〜1）。0 の時は鳴らさない。</summary>
        public float Volume = 0.6f;

        public RegionSoundPlayer(GameObject host, Func<string, AudioClip> loader)
        {
            _clips = RegionSoundBank.Resolve(loader, _custom);
            if (host == null) return;
            for (int i = 0; i < Voices; i++)
            {
                AudioSource source = host.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                _sources.Add(source);
            }
        }

        /// <summary>まだ鳴らしていない予約があるか。</summary>
        public bool Pending
        {
            get { return _pending.Count > 0; }
        }

        /// <summary>差し替えた場面の名前（コンマ区切り）。無ければ空。</summary>
        public string CustomNames
        {
            get
            {
                var names = new List<string>();
                foreach (RegionSoundSpec spec in RegionSound.All)
                    if (_custom.Contains(spec.Cue)) names.Add(spec.FileName);
                return string.Join(",", names.ToArray());
            }
        }

        public bool IsCustom(RegionSoundCue cue)
        {
            return _custom.Contains(cue);
        }

        public AudioClip Clip(RegionSoundCue cue)
        {
            AudioClip clip;
            return _clips.TryGetValue(cue, out clip) ? clip : null;
        }

        /// <summary>鳴らした記録（新しい順ではなく、鳴らした順。古い物から消える）。</summary>
        public IReadOnlyList<RegionSoundRecord> Records
        {
            get { return _records; }
        }

        /// <summary>その場面を鳴らした回数。</summary>
        public int PlayedCount(RegionSoundCue cue)
        {
            return _played[(int)cue];
        }

        /// <summary>その場面で、鳴らした後に AudioSource の再生が進んだのを見た回数。</summary>
        public int ConfirmedCount(RegionSoundCue cue)
        {
            return _confirmed[(int)cue];
        }

        /// <summary>音を切ってある・音量 0 のために鳴らさなかった回数。</summary>
        public int SilencedCount(RegionSoundCue cue)
        {
            return _silenced[(int)cue];
        }

        /// <summary>at（動きの時計の秒）に鳴らすよう予約する。at が今以前なら次の Tick ですぐ鳴る。</summary>
        public void Schedule(RegionSoundCue cue, float at, float pan = 0f)
        {
            _pending.Add(new Booking { Cue = cue, At = at, Pan = pan });
        }

        /// <summary>予約を全部捨てる（地域を作り直した時）。</summary>
        public void ClearPending()
        {
            _pending.Clear();
        }

        /// <summary>
        /// 今の時刻（動きの時計）までの予約を鳴らし、前のフレームで鳴らした音が本当に進んでいるかを見る。
        /// 毎フレーム呼ぶ。
        /// </summary>
        public void Tick(float now)
        {
            _now = now;
            for (int w = _watching.Count - 1; w >= 0; w--)
            {
                RegionSoundRecord record = _watching[w];
                record.Frames++;
                AudioSource source = record.Source;
                if (source != null && source.clip != null && source.clip.name == record.ClipName && source.isPlaying && source.timeSamples > 0)
                {
                    record.Confirmed = true;
                    _confirmed[(int)record.Cue]++;
                    _watching.RemoveAt(w);
                }
                else if (record.Frames > 30)
                {
                    _watching.RemoveAt(w);
                }
            }
            if (_pending.Count == 0) return;
            // 予約した順に鳴らす（同じ時刻の物も、予約の順を保つ）
            for (int i = 0; i < _pending.Count; i++)
            {
                Booking p = _pending[i];
                if (p.At > now) continue;
                _pending.RemoveAt(i);
                i--;
                PlayNow(p.Cue, p.Pan);
            }
        }

        private void PlayNow(RegionSoundCue cue, float pan)
        {
            float last;
            if (_lastAt.TryGetValue(cue, out last) && _now - last < RegionSound.RepeatGuard) return;
            _lastAt[cue] = _now;
            if (!Enabled || Volume <= 0f)
            {
                _silenced[(int)cue]++;
                return;
            }
            AudioClip clip = Clip(cue);
            if (clip == null || _sources.Count == 0) return;
            AudioSource source = _sources[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _sources.Count;
            source.Stop();
            source.clip = clip;
            source.volume = Mathf.Clamp01(Volume);
            source.panStereo = Mathf.Clamp(pan, -1f, 1f);
            source.Play();
            _played[(int)cue]++;
            var record = new RegionSoundRecord
            {
                Cue = cue,
                Time = _now,
                ClipName = clip.name,
                Custom = _custom.Contains(cue),
                Volume = source.volume,
                Pan = source.panStereo,
                Source = source,
            };
            _records.Add(record);
            if (_records.Count > MaxRecords) _records.RemoveAt(0);
            _watching.Add(record);
        }

        /// <summary>作った仮の音を消す（差し替えの音は Resources の物なので消さない）。</summary>
        public void Dispose()
        {
            foreach (KeyValuePair<RegionSoundCue, AudioClip> pair in _clips)
                if (_custom.Contains(pair.Key) || pair.Value == null) continue;
                else if (Application.isPlaying) UnityEngine.Object.Destroy(pair.Value);
                else UnityEngine.Object.DestroyImmediate(pair.Value);
            _clips.Clear();
        }
    }
}
