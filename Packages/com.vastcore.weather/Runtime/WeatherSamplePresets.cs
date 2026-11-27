using UnityEngine;

namespace Vastcore.Weather
{
    public enum WeatherSampleSite
    {
        StreetThunderstorm,
        MountainValleyFog,
        CumulonimbusAcrossSea,
        CumulonimbusBeyondRidge,
        MorningGlory,
        RidgeCloudBands,
        GreatPlainsSupercell
    }

    public static class WeatherSamplePresets
    {
        public static WeatherPreset Create(WeatherSampleSite site)
        {
            var preset = ScriptableObject.CreateInstance<WeatherPreset>();
            preset.name = site.ToString();
            preset.hideFlags = HideFlags.DontSave;
            preset.VolumeSteps = 32;

            switch (site)
            {
                case WeatherSampleSite.StreetThunderstorm:
                    Street(preset);
                    break;
                case WeatherSampleSite.MountainValleyFog:
                    Fog(preset);
                    break;
                case WeatherSampleSite.CumulonimbusAcrossSea:
                    Cumulonimbus(preset, false);
                    break;
                case WeatherSampleSite.CumulonimbusBeyondRidge:
                    Cumulonimbus(preset, true);
                    break;
                case WeatherSampleSite.MorningGlory:
                    Roll(preset);
                    break;
                case WeatherSampleSite.RidgeCloudBands:
                    CloudBands(preset);
                    break;
                case WeatherSampleSite.GreatPlainsSupercell:
                    Supercell(preset);
                    break;
            }
            return preset;
        }

        private static void Street(WeatherPreset preset)
        {
            Vector3 motion = new Vector3(10f, 0f, -13f);
            WeatherVolume lower = Form(WeatherShape.Anvil, new Vector3(-340f, 1720f, 1200f),
                new Vector3(4700f, 1250f, 3900f), 4f, 300f, 0xAEBCCA, 0x424C5B);
            WeatherVolume tower = Form(WeatherShape.Tower, new Vector3(-890f, 3800f, 2250f),
                new Vector3(3400f, 5700f, 3400f), 3.2f, 420f, 0xBCCBDD, 0x455366);
            lower.DriftMetersPerSecond = motion;
            tower.DriftMetersPerSecond = motion;
            preset.Volumes = new[] { tower, lower };
            preset.Rain = new WeatherRainCell
            {
                Intensity = 1f,
                CenterAtGroundMeters = new Vector3(-415f, 0f, 562f),
                RadiusMeters = new Vector2(133f, 310f),
                DriftMetersPerSecond = motion,
                WindMetersPerSecond = new Vector2(2.9f, -1.3f),
                CloudBaseMeters = 1320f,
                StartSeconds = 6f,
                DryOutSeconds = 145f,
                LightningIntervalSeconds = 14f,
                CurtainExtinctionPerKilometer = 3.3f,
                CurtainColor = Tone(0x879AA9)
            };
            preset.AerialHaze = Tone(0x9AAAB8);
            preset.HazePerKilometer = 0.11f;
        }

        private static void Fog(WeatherPreset preset)
        {
            WeatherVolume bank = Form(WeatherShape.ValleyFog, new Vector3(325f, 207f, 3560f),
                new Vector3(6700f, 445f, 11700f), 1.7f, 380f, 0xDDD7CB, 0x829DAF);
            bank.FlowRadiansPerSecond = 0.016f;
            WeatherVolume further = Form(WeatherShape.ValleyFog, new Vector3(350f, 299f, 11300f),
                new Vector3(10400f, 624f, 13100f), 1.30f, 620f, 0xDFDBD4, 0x91A4B3);
            further.FlowRadiansPerSecond = 0.011f;
            preset.Volumes = new[] { further, bank };
            preset.AerialHaze = Tone(0xB3B8B6);
            preset.HazePerKilometer = 0.018f;
        }

        private static void Cumulonimbus(WeatherPreset preset, bool ridge)
        {
            float z = ridge ? 26900f : 36800f;
            var tower = Form(WeatherShape.Tower, new Vector3(-1900f, 7460f, z),
                new Vector3(14400f, 14500f, 11600f), 1.65f, 1080f, 0xFFF9EB, 0x8399B3);
            tower.FlowRadiansPerSecond = 0.024f;
            var anvil = Form(WeatherShape.Anvil, new Vector3(4750f, 12200f, z + 960f),
                new Vector3(21100f, 2800f, 10700f), 1.25f, 1050f, 0xFFFAEB, 0x8C9DB2);
            anvil.FlowRadiansPerSecond = 0.034f;
            preset.Volumes = new[] { anvil, tower };
            preset.Rain = new WeatherRainCell
            {
                Intensity = ridge ? 0.18f : 0.58f,
                CenterAtGroundMeters = new Vector3(-1100f, 15f, z - 950f),
                RadiusMeters = new Vector2(2100f, 2700f),
                CloudBaseMeters = 1950f,
                CurtainExtinctionPerKilometer = 0.72f,
                CurtainColor = Tone(0x859CAD)
            };
            preset.AerialHaze = Tone(0xA6C2D3);
            preset.HazePerKilometer = ridge ? 0.014f : 0.011f;
        }

        private static void Roll(WeatherPreset preset)
        {
            var lead = Form(WeatherShape.RollCloud, new Vector3(0f, 675f, 2650f),
                new Vector3(106000f, 1120f, 1820f), 3.20f, 260f, 0xFFE2C6, 0x536987);
            var follower = Form(WeatherShape.RollCloud, new Vector3(-3710f, 654f, 7550f),
                new Vector3(94000f, 940f, 1420f), 2.3f, 274f, 0xF5D5C0, 0x617694);
            lead.YawDegrees = -26f;
            follower.YawDegrees = -26f;
            lead.DriftMetersPerSecond = new Vector3(5.26f, 0f, -10.79f);
            follower.DriftMetersPerSecond = lead.DriftMetersPerSecond;
            lead.FlowRadiansPerSecond = 0.12f;
            follower.FlowRadiansPerSecond = 0.095f;
            preset.Volumes = new[] { follower, lead };
            preset.AerialHaze = Tone(0xB5ADAB);
            preset.HazePerKilometer = 0.040f;
        }

        private static void CloudBands(WeatherPreset preset)
        {
            var lower = Form(WeatherShape.Lenticular, new Vector3(-2300f, 1990f, 8180f),
                new Vector3(7800f, 880f, 2830f), 2.55f, 415f, 0xFFE1C9, 0x8A8FAD);
            var middle = Form(WeatherShape.Lenticular, new Vector3(300f, 2900f, 11000f),
                new Vector3(9400f, 1010f, 2950f), 2.0f, 530f, 0xF4CDBB, 0x7E8EAD);
            var upper = Form(WeatherShape.Lenticular, new Vector3(2950f, 4190f, 14500f),
                new Vector3(8200f, 1050f, 2800f), 2.05f, 510f, 0xF8CEB8, 0x7990AC);
            lower.YawDegrees = -5f;
            middle.YawDegrees = 15f;
            upper.YawDegrees = 22f;
            lower.FlowRadiansPerSecond = 0.13f;
            middle.FlowRadiansPerSecond = -0.18f;
            upper.FlowRadiansPerSecond = 0.065f;
            preset.Volumes = new[] { upper, middle, lower };
            preset.AerialHaze = Tone(0xACA9AD);
            preset.HazePerKilometer = 0.021f;
        }

        private static void Supercell(WeatherPreset preset)
        {
            var updraft = Form(WeatherShape.RotatingUpdraft, new Vector3(-4660f, 6000f, 22900f),
                new Vector3(15800f, 11900f, 12600f), 2.15f, 940f, 0x9EAAB7, 0x2F3946);
            var wall = Form(WeatherShape.Anvil, new Vector3(-7240f, 1960f, 21600f),
                new Vector3(10600f, 2840f, 7100f), 3.05f, 735f, 0x8998A7, 0x2C3744);
            var anvil = Form(WeatherShape.Anvil, new Vector3(2800f, 10580f, 24600f),
                new Vector3(23600f, 2900f, 13000f), 1.7f, 1150f, 0xC9CDD0, 0x4A5C70);
            Vector3 drift = new Vector3(9f, 0f, -4f);
            anvil.DriftMetersPerSecond = drift;
            anvil.FlowRadiansPerSecond = 0.047f;
            updraft.DriftMetersPerSecond = drift;
            updraft.FlowRadiansPerSecond = 0.17f;
            wall.DriftMetersPerSecond = drift;
            wall.FlowRadiansPerSecond = 0.062f;
            preset.Volumes = new[] { anvil, updraft, wall };
            preset.Rain = new WeatherRainCell
            {
                Intensity = 0.96f,
                CenterAtGroundMeters = new Vector3(4880f, 0f, 23750f),
                RadiusMeters = new Vector2(5650f, 5150f),
                DriftMetersPerSecond = drift,
                WindMetersPerSecond = new Vector2(12f, -3.0f),
                CloudBaseMeters = 7150f,
                CurtainExtinctionPerKilometer = 0.62f,
                CurtainColor = Tone(0x5B7281),
                LightningIntervalSeconds = 17f
            };
            preset.AerialHaze = Tone(0xA3A79F);
            preset.HazePerKilometer = 0.011f;
            preset.VolumeSteps = 36;
        }

        private static WeatherVolume Form(WeatherShape shape, Vector3 center, Vector3 size,
            float extinction, float detail, int light, int shade)
        {
            return new WeatherVolume
            {
                Shape = shape,
                CenterMeters = center,
                SizeMeters = size,
                ExtinctionPerKilometer = extinction,
                DetailSizeMeters = detail,
                SunlitColor = Tone(light),
                ShadeColor = Tone(shade)
            };
        }

        internal static Color Tone(int rgb) =>
            new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
    }
}
