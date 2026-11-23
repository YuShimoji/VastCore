using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vastcore.RegionMap;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// 探索画面の音（SP-021 軸 F）のうち、再生しなくても決まる部分：場面と仮の音の対応、仮の音の形、差し替えの有無の判定、
    /// 予約の時刻と音を切った時の扱い。実際に鳴るかは探索シーンの再生（RegionExplorerSmoke の sound-result.txt）で見る。
    /// </summary>
    public class RegionSoundTests
    {
        [Test]
        public void Table_HasOneEntryPerScene_NamedAfterTheScene()
        {
            Array cues = Enum.GetValues(typeof(RegionSoundCue));
            Assert.AreEqual(cues.Length, RegionSound.All.Count);
            var names = new HashSet<string>();
            foreach (RegionSoundCue cue in cues)
            {
                RegionSoundSpec spec = RegionSound.Spec(cue);
                Assert.AreEqual(cue, spec.Cue);
                // 差し替えのファイル名は場面の名前そのもの（SP-021 6節の手順と同じ）
                Assert.AreEqual(cue.ToString(), spec.FileName);
                Assert.IsTrue(names.Add(spec.FileName));
                Assert.IsNotEmpty(spec.Scene);
                Assert.IsNotEmpty(spec.Timing);
                Assert.IsNotEmpty(spec.Notes);
                // 仮の音は短い（いちばん長い物でも 1 秒未満）
                Assert.Less(RegionSound.Length(cue), 1f, cue.ToString());
            }
        }

        [Test]
        public void Render_IsAudible_DoesNotClip_AndStartsAndEndsQuietly()
        {
            foreach (RegionSoundSpec spec in RegionSound.All)
            {
                float[] samples = RegionSound.Render(spec.Cue);
                // 長さ（秒）×標本の数。小数の丸めで 1 つずれることがある
                Assert.That(samples.Length, Is.EqualTo(Mathf.CeilToInt(RegionSound.Length(spec.Cue) * RegionSound.SampleRate)).Within(1), spec.FileName);
                float peak = 0f;
                foreach (float s in samples) peak = Mathf.Max(peak, Mathf.Abs(s));
                Assert.Greater(peak, 0.05f, spec.FileName + " が聞こえないほど小さい");
                Assert.LessOrEqual(peak, 0.9001f, spec.FileName);
                Assert.Less(Mathf.Abs(samples[0]), 0.02f, spec.FileName + " の始まり");
                Assert.Less(Mathf.Abs(samples[samples.Length - 1]), 0.02f, spec.FileName + " の終わり");
                CollectionAssert.AreEqual(samples, RegionSound.Render(spec.Cue), spec.FileName + " は毎回同じ音");
            }
        }

        [Test]
        public void Render_EveryPairOfScenesSoundsDifferent()
        {
            var rendered = new List<float[]>();
            foreach (RegionSoundSpec spec in RegionSound.All) rendered.Add(RegionSound.Render(spec.Cue));
            for (int a = 0; a < rendered.Count; a++)
            for (int b = a + 1; b < rendered.Count; b++)
            {
                float[] x = rendered[a];
                float[] y = rendered[b];
                int n = Math.Min(x.Length, y.Length);
                double difference = Math.Abs(x.Length - y.Length);
                for (int i = 0; i < n; i++) difference += Math.Abs(x[i] - y[i]);
                Assert.Greater(difference, 50.0, RegionSound.All[a].FileName + " と " + RegionSound.All[b].FileName);
            }
        }

        [Test]
        public void ToWav_Writes16BitMonoPcm()
        {
            float[] samples = RegionSound.Render(RegionSoundCue.Found);
            byte[] wav = RegionSound.ToWav(samples);
            Assert.AreEqual(44 + samples.Length * 2, wav.Length);
            Assert.AreEqual("RIFF", System.Text.Encoding.ASCII.GetString(wav, 0, 4));
            Assert.AreEqual("WAVE", System.Text.Encoding.ASCII.GetString(wav, 8, 4));
            Assert.AreEqual(1, BitConverter.ToInt16(wav, 22));
            Assert.AreEqual(RegionSound.SampleRate, BitConverter.ToInt32(wav, 24));
            Assert.AreEqual(16, BitConverter.ToInt16(wav, 34));
            Assert.AreEqual(samples.Length * 2, BitConverter.ToInt32(wav, 40));
        }

        [Test]
        public void Pan_FollowsTheCompassEastRightWestLeft()
        {
            Assert.Greater(RegionSound.Pan(CompassPoint.East), 0f);
            Assert.Greater(RegionSound.Pan(CompassPoint.NorthEast), 0f);
            Assert.Less(RegionSound.Pan(CompassPoint.West), 0f);
            Assert.Less(RegionSound.Pan(CompassPoint.SouthWest), 0f);
            Assert.AreEqual(0f, RegionSound.Pan(CompassPoint.North));
            Assert.AreEqual(0f, RegionSound.Pan(CompassPoint.South));
            Assert.AreEqual(0f, RegionSound.Pan(CompassPoint.None));
            Assert.Less(Mathf.Abs(RegionSound.Pan(CompassPoint.East)), 1f, "片耳だけにしない");
        }

        [Test]
        public void Resolve_UsesAPlacedSoundOnlyForItsScene()
        {
            AudioClip placed = AudioClip.Create("Found", 441, 1, RegionSound.SampleRate, false);
            var custom = new HashSet<RegionSoundCue>();
            Dictionary<RegionSoundCue, AudioClip> clips = RegionSoundBank.Resolve(name => name == "Found" ? placed : null, custom);
            try
            {
                CollectionAssert.AreEquivalent(new[] { RegionSoundCue.Found }, custom);
                Assert.AreSame(placed, clips[RegionSoundCue.Found]);
                foreach (RegionSoundSpec spec in RegionSound.All)
                {
                    if (spec.Cue == RegionSoundCue.Found) continue;
                    AudioClip clip = clips[spec.Cue];
                    Assert.IsNotNull(clip, spec.FileName);
                    Assert.AreEqual("仮_" + spec.FileName, clip.name);
                    Assert.AreEqual(RegionSound.Render(spec.Cue).Length, clip.samples);
                }
            }
            finally
            {
                foreach (AudioClip clip in clips.Values) UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void Resolve_WithNothingPlaced_IsAllTemporarySounds()
        {
            // 置き場所（Resources/RegionMapSound/）は空のフォルダで、今は差し替えが無い
            var custom = new HashSet<RegionSoundCue>();
            Dictionary<RegionSoundCue, AudioClip> clips = RegionSoundBank.Resolve(RegionSoundBank.Load, custom);
            try
            {
                Assert.IsEmpty(custom);
                Assert.AreEqual(RegionSound.All.Count, clips.Count);
            }
            finally
            {
                foreach (AudioClip clip in clips.Values) UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void Player_WaitsForTheScheduledTime_AndStaysSilentWhenSwitchedOff()
        {
            var player = new RegionSoundPlayer(null, null);
            try
            {
                player.Schedule(RegionSoundCue.SignalFaint, 1.0f);
                player.Tick(0.5f);
                Assert.IsTrue(player.Pending, "時刻の前は鳴らさない");
                player.Enabled = false;
                player.Tick(1.0f);
                Assert.IsFalse(player.Pending);
                Assert.AreEqual(1, player.SilencedCount(RegionSoundCue.SignalFaint), "音を切ってある時は鳴らさない");
                Assert.AreEqual(0, player.PlayedCount(RegionSoundCue.SignalFaint));

                player.Enabled = true;
                player.Volume = 0f;
                player.Schedule(RegionSoundCue.Found, 2.0f);
                player.Tick(2.0f);
                Assert.AreEqual(1, player.SilencedCount(RegionSoundCue.Found), "音量 0 の時も鳴らさない");

                player.ClearPending();
                player.Schedule(RegionSoundCue.Toggle, 3.0f);
                player.ClearPending();
                Assert.IsFalse(player.Pending);
            }
            finally
            {
                player.Dispose();
            }
        }
    }
}
