using System;
using UnityEngine;
using Random = System.Random;

namespace Vastcore.ModernCity.Editor
{
    internal static class CityProps
    {
        internal static void Bake(CityKitAssets kit, CityPalette p)
        {
            void Prop(string name, Action<CityModel> author) => kit.Bake(CityKitAssets.Props, name, author);
            BakeStreet(Prop, p);
            kit.Bake(CityKitAssets.Props, "Street tree", m => Tree(m, p, 37, 1f), m => Tree(m, p, 37, 1f, distant: true));
            kit.Bake(CityKitAssets.Props, "Courtyard tree", m => Tree(m, p, 197, 0.77f),
                m => Tree(m, p, 197, 0.77f, distant: true));
            BakeFurniture(Prop, p);
            BakeRetail(Prop, p);
            BakeService(Prop, p);
        }

        private static void BakeStreet(Action<string, Action<CityModel>> prop, CityPalette p)
        {
            prop("Bollard", m =>
            {
                m.Tube(p.Stone, Vector3.zero, Vector3.up * 0.79f, 0.085f, 12);
                m.Tube(p.Metal, Vector3.up * 0.69f, Vector3.up * 0.74f, 0.088f, 12);
                m.CollisionBox(new Vector3(0, 0.38f, 0), new Vector3(0.17f, 0.76f, 0.17f));
            });
            prop("Street lamp", m =>
            {
                m.Frustum(p.Dark, Vector3.zero, Vector3.up * 4.2f, 0.075f, 0.046f);
                m.Tube(p.Dark, new Vector3(0, 4.2f, 0), new Vector3(0, 4.48f, 0.8f), 0.044f);
                Box(m, p.Dark, 0, 4.46f, 0.92f, 0.2f, 0.105f, 0.55f);
                Box(m, p.Light, 0, 4.398f, 0.934f, 0.16f, 0.035f, 0.44f);
                m.CollisionBox(new Vector3(0, 0.95f, 0), new Vector3(0.16f, 1.9f, 0.16f));
            });
            prop("Street bench", m =>
            {
                for (int i = 0; i < 5; i++)
                    Box(m, p.Timber, 0, 0.475f, (i - 2) * 0.103f, 1.83f, 0.056f, 0.094f);
                for (int i = 0; i < 3; i++)
                    Box(m, p.Timber, 0, 0.68f + i * 0.11f, -0.245f, 1.83f, 0.093f, 0.062f);
                foreach (float x in new[] { -0.78f, 0.78f })
                {
                    Box(m, p.Dark, x, 0.24f, 0, 0.052f, 0.48f, 0.44f);
                    Box(m, p.Dark, x, 0.68f, -0.265f, 0.05f, 0.48f, 0.06f);
                }
                m.CollisionBox(new Vector3(0, 0.25f, 0), new Vector3(1.85f, 0.5f, 0.53f));
            });
            prop("Street shelter", m =>
            {
                m.BeveledBox(p.Dark, new Vector3(0, 2.66f, 0), new Vector3(3.9f, 0.15f, 1.95f), 0.028f);
                Box(m, p.Light, 0, 2.574f, -0.3f, 2.52f, 0.023f, 0.085f);
                Box(m, p.Blue, 0, 0.99f, 0.64f, 3.35f, 1.98f, 0.082f, true);
                Box(m, p.Timber, 0, 0.48f, 0.41f, 3.02f, 0.09f, 0.54f, true);
                foreach (float x in new[] { -1.73f, 1.73f })
                    Box(m, p.Metal, x, 1.3f, 0.62f, 0.095f, 2.6f, 0.092f, true);
            });
            prop("Limestone planter", m =>
            {
                Box(m, p.Stone, 0, 0.25f, 0, 1.26f, 0.5f, 0.59f, true);
                Box(m, p.Soil, 0, 0.504f, 0, 1.14f, 0.025f, 0.49f);
                for (int i = -1; i <= 1; i++)
                    Foliage(m, p, new Vector3(i * 0.31f, 0.52f, 0), 0.39f);
            });
            prop("Round planter", m => Planter(m, p, 0.31f, 0.57f, 0.72f, p.Dark, true));
            prop("Bike rack", m =>
            {
                foreach (float x in new[] { -0.52f, 0f, 0.52f })
                {
                    m.Tube(p.Metal, new Vector3(x, 0, -0.29f), new Vector3(x, 0.66f, -0.29f), 0.027f);
                    m.Tube(p.Metal, new Vector3(x, 0.66f, -0.29f), new Vector3(x, 0.66f, 0.29f), 0.027f);
                    m.Tube(p.Metal, new Vector3(x, 0, 0.29f), new Vector3(x, 0.66f, 0.29f), 0.027f);
                }
                m.CollisionBox(new Vector3(0, 0.34f, 0), new Vector3(1.11f, 0.68f, 0.64f));
            });
            prop("City bicycle", m => Bicycle(m, p));
            prop("Signal pole", m =>
            {
                m.Tube(p.Metal, Vector3.zero, Vector3.up * 3.45f, 0.066f);
                Box(m, p.Dark, 0.29f, 2.93f, 0, 0.32f, 0.92f, 0.22f);
                Box(m, p.Dark, 0.12f, 3.08f, 0, 0.3f, 0.082f, 0.09f);
                for (int i = 0; i < 3; i++)
                {
                    float y = 2.67f + i * 0.26f;
                    var face = new Vector3(0.29f, y, 0.113f);
                    m.Tube(i == 0 ? p.LeafLight : i == 1 ? p.Yellow : p.Red, face, face + Vector3.forward * 0.038f, 0.095f, 16);
                    Box(m, p.Dark, 0.29f, y + 0.108f, 0.155f, 0.24f, 0.042f, 0.13f);
                }
                m.CollisionBox(new Vector3(0, 0.92f, 0), new Vector3(0.14f, 1.84f, 0.14f));
            });
            prop("Utility cabinet", m =>
            {
                Box(m, p.Blue, 0, 0.7f, 0, 0.89f, 1.4f, 0.36f, true);
                Box(m, p.Metal, 0, 0.055f, 0, 0.94f, 0.11f, 0.4f);
                Box(m, p.Dark, 0.33f, 0.79f, 0.19f, 0.035f, 0.12f, 0.026f);
                Grille(m, p.Dark, -0.14f, 0.95f, 0.182f, 0.36f, 6);
            });
            prop("Litter bin", m =>
            {
                m.Frustum(p.Dark, Vector3.zero, Vector3.up * 0.79f, 0.2f, 0.25f, 16);
                m.Tube(p.Metal, Vector3.up * 0.79f, Vector3.up * 0.88f, 0.252f, 16);
                Box(m, p.Rubber, 0, 0.75f, 0.155f, 0.31f, 0.16f, 0.14f);
                m.CollisionBox(new Vector3(0, 0.43f, 0), new Vector3(0.48f, 0.86f, 0.48f));
            });
            prop("Manhole", m =>
            {
                m.Tube(p.Metal, Vector3.zero, Vector3.up * 0.014f, 0.385f, 22);
                m.Tube(p.Dark, Vector3.up * 0.014f, Vector3.up * 0.018f, 0.348f, 22);
                for (int i = -5; i <= 5; i++)
                    Box(m, p.Metal, 0, 0.02f, i * 0.053f, 0.54f - Mathf.Abs(i) * 0.048f, 0.008f, 0.017f);
            });
            prop("Storm drain", m =>
            {
                Box(m, p.Dark, 0, 0.006f, 0, 0.61f, 0.012f, 0.33f);
                for (int i = -5; i <= 5; i++)
                    Box(m, p.Metal, i * 0.053f, 0.015f, 0, 0.032f, 0.016f, 0.29f);
            });
            prop("Sedan", m => Vehicle(m, p, false));
            prop("Delivery van", m => Vehicle(m, p, true));
            prop("Steel bin pair", m =>
            {
                Box(m, p.Metal, 0, 0.52f, 0, 0.82f, 1.04f, 0.44f, true);
                Box(m, p.Blue, -0.21f, 1.07f, 0, 0.4f, 0.07f, 0.44f);
                Box(m, p.Olive, 0.21f, 1.07f, 0, 0.4f, 0.07f, 0.44f);
                foreach (float x in new[] { -0.21f, 0.21f })
                    Box(m, p.Dark, x, 0.88f, 0.224f, 0.24f, 0.12f, 0.02f);
            });
        }

        private static void BakeFurniture(Action<string, Action<CityModel>> prop, CityPalette p)
        {
            prop("Reception desk", m =>
            {
                Box(m, p.Timber, 0, 0.52f, 0, 2.33f, 1.04f, 0.75f, true);
                Box(m, p.Dark, 0, 0.054f, 0, 2.21f, 0.108f, 0.71f);
                Box(m, p.Stone, 0, 1.05f, 0, 2.43f, 0.07f, 0.83f);
                for (int i = -15; i <= 15; i++)
                    Box(m, p.Timber, i * 0.073f, 0.53f, 0.394f, 0.036f, 0.92f, 0.045f);
            });
            prop("Lobby directory", m =>
            {
                m.BeveledBox(p.Blue, new Vector3(0, 1.28f, 0), new Vector3(2.76f, 2.56f, 0.19f), 0.033f, true);
                Box(m, p.Dark, 0, 0.076f, -0.02f, 2.66f, 0.15f, 0.24f);
                Box(m, p.Light, -0.57f, 1.91f, -0.109f, 1.18f, 0.037f, 0.031f);
                for (int i = 0; i < 3; i++)
                    Box(m, p.Paint, -0.65f + i * 0.11f, 1.66f - i * 0.2f, -0.106f, 1.02f - i * 0.18f, 0.031f, 0.024f);
                for (int i = 0; i < 11; i++)
                    Box(m, p.Timber, 0.348f + i * 0.08f, 1.28f, -0.107f, 0.04f, 2.37f, 0.081f);
            });
            prop("Office desk", m =>
            {
                Table(m, p, 1.34f, 0.66f, 0.74f, p.Paint);
                Box(m, p.Paint, 0.38f, 0.3f, 0, 0.37f, 0.6f, 0.49f);
                for (int i = 0; i < 3; i++)
                    Box(m, p.Dark, 0.38f, 0.15f + 0.18f * i, 0.253f, 0.11f, 0.019f, 0.029f);
            });
            prop("Meeting table", m => Table(m, p, 2.7f, 1.21f, 0.75f, p.Timber));
            prop("Low coffee table", m => Table(m, p, 0.95f, 0.52f, 0.41f, p.Timber));
            prop("Office chair", m =>
            {
                Box(m, p.Rubber, 0, 0.46f, 0, 0.51f, 0.12f, 0.49f);
                m.Box(p.Dark, new Vector3(0, 0.79f, -0.21f), new Vector3(0.46f, 0.53f, 0.07f),
                    Quaternion.Euler(-8, 0, 0));
                m.Tube(p.Metal, Vector3.up * 0.087f, Vector3.up * 0.45f, 0.039f);
                for (int i = 0; i < 5; i++)
                {
                    var foot = Quaternion.Euler(0, i * 72, 0) * new Vector3(0, 0.085f, 0.26f);
                    m.Tube(p.Dark, Vector3.up * 0.11f, foot, 0.024f);
                    m.Ellipsoid(p.Rubber, foot - Vector3.up * 0.049f, new Vector3(0.034f, 0.039f, 0.034f));
                }
                foreach (float x in new[] { -0.273f, 0.273f })
                {
                    Box(m, p.Dark, x, 0.57f, -0.04f, 0.033f, 0.23f, 0.042f);
                    Box(m, p.Rubber, x, 0.675f, 0.02f, 0.06f, 0.04f, 0.27f);
                }
                m.CollisionBox(new Vector3(0, 0.45f, 0), new Vector3(0.54f, 0.9f, 0.5f));
            });
            prop("Visitor chair", m => Chair(m, p, p.Olive, 0.51f));
            prop("Cafe chair", m => Chair(m, p, p.Timber, 0.43f));
            prop("Lobby sofa", m => Sofa(m, p, 1.8f, p.Textile));
            prop("Lounge chair", m => Sofa(m, p, 0.81f, p.Olive));
            prop("Sideboard", m =>
            {
                Box(m, p.Timber, 0, 0.43f, 0, 1.54f, 0.76f, 0.46f, true);
                Box(m, p.Dark, 0, 0.057f, 0, 1.46f, 0.114f, 0.41f);
                for (int i = -1; i <= 1; i++)
                {
                    Box(m, p.Timber, i * 0.506f, 0.43f, 0.242f, 0.493f, 0.71f, 0.026f);
                    Box(m, p.Brass, i * 0.506f, 0.69f, 0.266f, 0.11f, 0.022f, 0.025f);
                }
            });
            prop("Monitor", m => Display(m, p, 0.54f, 0.32f));
            prop("Keyboard", m =>
            {
                Box(m, p.Dark, 0, 0.015f, 0, 0.44f, 0.03f, 0.145f);
                for (int col = -6; col <= 6; col++)
                for (int row = -2; row <= 2; row++)
                    Box(m, p.Metal, col * 0.029f, 0.033f, row * 0.022f, 0.02f, 0.01f, 0.015f);
            });
            prop("Computer tower", m =>
            {
                Box(m, p.Dark, 0, 0.21f, 0, 0.2f, 0.42f, 0.39f);
                Grille(m, p.Metal, 0, 0.17f, 0.198f, 0.13f, 4);
                Box(m, p.Light, 0.054f, 0.364f, 0.202f, 0.017f, 0.018f, 0.011f);
            });
            prop("Laser printer", m =>
            {
                Box(m, p.Paint, 0, 0.158f, 0, 0.57f, 0.316f, 0.43f, true);
                Box(m, p.Dark, 0, 0.175f, 0.225f, 0.41f, 0.066f, 0.035f);
                Box(m, p.White, 0, 0.158f, 0.25f, 0.26f, 0.013f, 0.14f);
                Box(m, p.Screen, 0.15f, 0.322f, 0.065f, 0.14f, 0.022f, 0.08f);
            });
            prop("Document binders", m =>
            {
                for (int i = 0; i < 5; i++)
                {
                    float x = i * 0.058f - 0.115f;
                    Box(m, i % 2 == 0 ? p.Blue : p.Dark, x, 0.155f, 0, 0.052f, 0.31f, 0.235f);
                    Box(m, p.White, x, 0.22f, 0.119f, 0.031f, 0.077f, 0.007f);
                }
            });
            prop("Desk lamp", m =>
            {
                m.Tube(p.Dark, Vector3.zero, Vector3.up * 0.035f, 0.1f);
                var bend = new Vector3(-0.067f, 0.34f, -0.06f);
                var tip = new Vector3(0, 0.42f, 0.145f);
                m.Tube(p.Brass, Vector3.up * 0.036f, bend, 0.018f);
                m.Tube(p.Brass, bend, tip, 0.018f);
                m.Frustum(p.Dark, tip + Vector3.down * 0.07f, tip, 0.114f, 0.061f, 16);
                m.Tube(p.Light, tip + Vector3.down * 0.073f, tip + Vector3.down * 0.067f, 0.105f, 16);
            });
            prop("Desk plant", m => Planter(m, p, 0.092f, 0.13f, 0.28f, p.Paint, false));
            prop("Floor mat", m => Box(m, p.Rubber, 0, 0.008f, 0, 1.68f, 0.016f, 1.18f));
            prop("Floor lamp", m =>
            {
                m.Tube(p.Dark, Vector3.zero, Vector3.up * 0.045f, 0.18f, 16);
                m.Tube(p.Dark, Vector3.zero, Vector3.up * 1.48f, 0.024f);
                m.Frustum(p.Textile, Vector3.up * 1.29f, Vector3.up * 1.62f, 0.218f, 0.152f, 16);
                m.Tube(p.Light, Vector3.up * 1.286f, Vector3.up * 1.29f, 0.198f, 16);
            });
            prop("Pendant light", m =>
            {
                m.Tube(p.Dark, new Vector3(0, -0.52f, 0), Vector3.zero, 0.017f);
                m.Frustum(p.Dark, new Vector3(0, -0.68f, 0), new Vector3(0, -0.5f, 0), 0.24f, 0.107f, 16);
                m.Tube(p.Light, new Vector3(0, -0.684f, 0), new Vector3(0, -0.68f, 0), 0.226f, 16);
            });
            prop("Linear ceiling light", m =>
            {
                Box(m, p.Dark, 0, -0.035f, 0, 1.39f, 0.07f, 0.21f);
                Box(m, p.Light, 0, -0.077f, 0, 1.32f, 0.026f, 0.176f);
            });
            prop("Timber ceiling slats", m =>
            {
                for (int i = -7; i <= 7; i++)
                    Box(m, p.Timber, i * 0.247f, -0.042f, 0, 0.106f, 0.086f, 3.91f);
            });
            prop("Whiteboard", m =>
            {
                Box(m, p.Metal, 0, 0.49f, 0, 1.59f, 0.98f, 0.045f);
                Box(m, p.Paint, 0, 0.49f, 0.031f, 1.51f, 0.9f, 0.02f);
                Box(m, p.Metal, 0, 0.03f, 0.063f, 1.41f, 0.035f, 0.098f);
                Box(m, p.Blue, 0.43f, 0.055f, 0.084f, 0.12f, 0.021f, 0.02f);
            });
            prop("Wall clock", m =>
            {
                m.Tube(p.Dark, Vector3.zero, Vector3.forward * 0.046f, 0.2f, 24);
                m.Tube(p.Paint, Vector3.forward * 0.046f, Vector3.forward * 0.052f, 0.183f, 24);
                for (int i = 0; i < 12; i++)
                {
                    var q = Quaternion.Euler(0, 0, i * 30);
                    m.Box(p.Dark, new Vector3(0, 0, 0.058f) + q * new Vector3(0, 0.152f, 0),
                        new Vector3(0.012f, 0.022f, 0.01f), q);
                }
                m.Tube(p.Dark, Vector3.forward * 0.065f, new Vector3(-0.059f, 0.072f, 0.065f), 0.008f);
                m.Tube(p.Dark, Vector3.forward * 0.065f, new Vector3(0.11f, 0.033f, 0.065f), 0.006f);
            });
            prop("Water cooler", m =>
            {
                Box(m, p.Paint, 0, 0.46f, 0, 0.36f, 0.92f, 0.33f, true);
                Box(m, p.Dark, 0, 0.69f, 0.169f, 0.24f, 0.22f, 0.024f);
                Box(m, p.Metal, 0, 0.604f, 0.19f, 0.25f, 0.035f, 0.11f);
                m.Frustum(p.Blue, Vector3.up * 0.91f, Vector3.up * 1.21f, 0.12f, 0.14f, 16);
                m.Tube(p.Blue, Vector3.up * 1.194f, Vector3.up * 1.32f, 0.14f, 16);
            });
        }

        private static void BakeRetail(Action<string, Action<CityModel>> prop, CityPalette p)
        {
            prop("Shop counter", m =>
            {
                Box(m, p.Timber, 0, 0.469f, 0, 1.69f, 0.938f, 0.58f, true);
                Box(m, p.Dark, 0, 0.97f, 0, 1.76f, 0.065f, 0.64f);
                Box(m, p.Stone, 0, 0.469f, 0.301f, 1.55f, 0.75f, 0.027f);
                Box(m, p.Brass, 0, 0.15f, 0.33f, 1.57f, 0.027f, 0.032f);
            });
            prop("Product shelving", m =>
            {
                Shelving(m, p, p.Timber, 1.49f, 1.94f, 0.38f);
                for (int row = 0; row < 4; row++)
                for (int col = 0; col < 6; col++)
                {
                    float x = (col - 2.5f) * 0.222f;
                    float size = 0.16f + 0.024f * ((col + row) % 4);
                    float y = 0.16f + row * 0.5f;
                    Material goods = (col + row) % 3 == 0 ? p.Blue : (col + 2 * row) % 3 == 1 ? p.Card : p.Olive;
                    Box(m, goods, x, y + size * 0.5f, 0.041f, 0.14f, size, 0.18f);
                    Box(m, p.White, x, y + size * 0.48f, 0.134f, 0.091f, 0.043f, 0.009f);
                }
            });
            prop("Glass display case", m =>
            {
                Box(m, p.Dark, 0, 0.33f, 0, 1.34f, 0.66f, 0.6f, true);
                Box(m, p.Stone, 0, 0.674f, 0, 1.32f, 0.047f, 0.57f);
                m.Box(p.Glass, new Vector3(0, 0.905f, 0), new Vector3(1.34f, 0.43f, 0.6f));
                for (int i = -1; i <= 1; i++)
                    Box(m, p.Card, i * 0.38f, 0.745f, 0.1f, 0.25f, 0.11f, 0.28f);
            });
            prop("Point of sale", m =>
            {
                Box(m, p.Dark, 0, 0.041f, 0, 0.28f, 0.082f, 0.27f);
                Display(m, p, 0.29f, 0.201f);
            });
            prop("Round cafe table", m =>
            {
                m.Tube(p.Dark, Vector3.zero, Vector3.up * 0.041f, 0.258f, 18);
                m.Tube(p.Dark, Vector3.up * 0.041f, Vector3.up * 0.696f, 0.055f);
                m.Tube(p.Timber, Vector3.up * 0.698f, Vector3.up * 0.739f, 0.36f, 22);
                m.CollisionBox(new Vector3(0, 0.374f, 0), new Vector3(0.71f, 0.74f, 0.71f));
            });
            prop("Bistro stool", m =>
            {
                m.Tube(p.Dark, Vector3.zero, Vector3.up * 0.033f, 0.192f, 16);
                m.Tube(p.Dark, Vector3.up * 0.033f, Vector3.up * 0.69f, 0.034f);
                m.Tube(p.Timber, Vector3.up * 0.69f, Vector3.up * 0.734f, 0.174f, 16);
                m.CollisionBox(new Vector3(0, 0.36f, 0), new Vector3(0.35f, 0.72f, 0.35f));
            });
            prop("Espresso machine", m =>
            {
                Box(m, p.Metal, 0, 0.228f, -0.055f, 0.64f, 0.456f, 0.39f);
                Box(m, p.Dark, 0, 0.414f, 0.1f, 0.6f, 0.064f, 0.29f);
                Box(m, p.Metal, 0, 0.045f, 0.09f, 0.63f, 0.09f, 0.45f);
                foreach (float x in new[] { -0.156f, 0.156f })
                {
                    m.Tube(p.Brass, new Vector3(x, 0.324f, 0.144f), new Vector3(x, 0.397f, 0.144f), 0.055f);
                    m.Tube(p.Dark, new Vector3(x, 0.348f, 0.153f), new Vector3(x + 0.11f, 0.348f, 0.27f), 0.018f);
                }
            });
            prop("Cup stack", m =>
            {
                for (int stack = 0; stack < 3; stack++)
                for (int cup = 0; cup < 4 + stack; cup++)
                {
                    var pos = new Vector3((stack - 1) * 0.104f, cup * 0.031f, stack * -0.034f);
                    m.Frustum(p.Paint, pos, pos + Vector3.up * 0.06f, 0.033f, 0.045f, 12);
                }
            });
            prop("Bottle tray", m =>
            {
                Box(m, p.Timber, 0, 0.019f, 0, 0.37f, 0.038f, 0.24f);
                for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    var basePoint = new Vector3(i * 0.1f, 0.038f, j * 0.072f);
                    m.Tube(p.Olive, basePoint, basePoint + Vector3.up * 0.15f, 0.032f);
                    m.Frustum(p.Olive, basePoint + Vector3.up * 0.15f, basePoint + Vector3.up * 0.19f, 0.032f, 0.016f);
                    m.Tube(p.Yellow, basePoint + Vector3.up * 0.19f, basePoint + Vector3.up * 0.207f, 0.017f);
                }
            });
            prop("Sandwich board", m =>
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    var tilt = Quaternion.Euler(-side * 12, 0, 0);
                    m.Box(p.Timber, new Vector3(0, 0.471f, side * 0.128f), new Vector3(0.52f, 0.91f, 0.06f), tilt, true);
                    m.Box(p.Dark, new Vector3(0, 0.475f, side * 0.162f), new Vector3(0.446f, 0.7f, 0.027f), tilt);
                }
                for (int i = 0; i < 4; i++)
                    Box(m, p.White, 0, 0.65f - 0.083f * i, 0.143f + 0.019f * i, 0.27f - i * 0.029f, 0.02f, 0.016f);
            });
            prop("Cafe sign", m => ShopSign(m, p, "CAFE"));
            prop("Office sign", m => ShopSign(m, p, "OFFICE"));
            prop("Shop sign", m => ShopSign(m, p, "SHOP"));
            prop("Service sign", m => ShopSign(m, p, "SERVICE"));
        }

        private static void BakeService(Action<string, Action<CityModel>> prop, CityPalette p)
        {
            prop("Loading stripes", m =>
            {
                foreach (float side in new[] { -1f, 1f })
                    for (int i = 0; i < 5; i++)
                        m.Box(p.Yellow, new Vector3(side * 1.03f, 0, -0.65f + i * 0.34f),
                            new Vector3(0.33f, 0.006f, 0.084f), Quaternion.Euler(0, side * 42, 0));
            });
            prop("Spare storage rack", m => Shelving(m, p, p.Metal, 1.53f, 1.97f, 0.48f));
            prop("Workshop bench", m =>
            {
                Table(m, p, 1.57f, 0.57f, 0.92f, p.Timber);
                Box(m, p.Metal, 0, 1.294f, -0.26f, 1.56f, 0.73f, 0.067f);
                for (int i = -5; i <= 5; i++)
                    Box(m, p.Dark, i * 0.123f, 1.345f, -0.224f, 0.021f, 0.12f, 0.019f);
            });
            prop("Pallet", m =>
            {
                for (int i = -2; i <= 2; i++)
                    Box(m, p.Timber, i * 0.209f, 0.125f, 0, 0.17f, 0.045f, 0.81f);
                foreach (float z in new[] { -0.32f, 0f, 0.32f })
                    Box(m, p.Timber, 0, 0.055f, z, 0.99f, 0.11f, 0.095f);
                m.CollisionBox(new Vector3(0, 0.075f, 0), new Vector3(1, 0.15f, 0.81f));
            });
            prop("Shipping carton", m =>
            {
                Box(m, p.Card, 0, 0.235f, 0, 0.64f, 0.47f, 0.48f, true);
                Box(m, p.Textile, 0, 0.47f, 0, 0.072f, 0.011f, 0.483f);
                Box(m, p.White, -0.147f, 0.31f, 0.243f, 0.16f, 0.093f, 0.007f);
            });
            prop("Hand truck", m =>
            {
                Box(m, p.Metal, 0, 0.067f, 0.19f, 0.45f, 0.06f, 0.42f, true);
                foreach (float x in new[] { -0.205f, 0.205f })
                {
                    m.Tube(p.Red, new Vector3(x, 0.08f, 0), new Vector3(x, 1.15f, -0.26f), 0.027f);
                    m.Tube(p.Rubber, new Vector3(x - 0.045f, 0.13f, -0.04f),
                        new Vector3(x + 0.045f, 0.13f, -0.04f), 0.127f, 14);
                }
                m.Tube(p.Dark, new Vector3(-0.21f, 1.15f, -0.26f), new Vector3(0.21f, 1.15f, -0.26f), 0.041f);
            });
            prop("Fire extinguisher", m =>
            {
                m.Tube(p.Red, Vector3.zero, Vector3.up * 0.49f, 0.092f, 16);
                m.Frustum(p.Red, Vector3.up * 0.49f, Vector3.up * 0.55f, 0.092f, 0.046f, 16);
                Box(m, p.Dark, 0, 0.576f, 0, 0.128f, 0.028f, 0.071f);
                Box(m, p.White, 0, 0.32f, 0.085f, 0.081f, 0.102f, 0.028f);
                m.Tube(p.Rubber, new Vector3(-0.043f, 0.549f, 0), new Vector3(-0.125f, 0.19f, 0.038f), 0.014f);
            });
            prop("Breaker panel", m =>
            {
                Box(m, p.Metal, 0, 0.43f, 0, 0.56f, 0.86f, 0.14f);
                Box(m, p.Dark, 0.16f, 0.43f, 0.082f, 0.066f, 0.12f, 0.032f);
                Box(m, p.Yellow, -0.087f, 0.59f, 0.075f, 0.125f, 0.096f, 0.012f);
            });
            prop("Mop bucket", m =>
            {
                m.Frustum(p.Yellow, Vector3.up * 0.035f, Vector3.up * 0.34f, 0.16f, 0.21f, 14);
                m.Tube(p.Dark, Vector3.up * 0.324f, Vector3.up * 0.358f, 0.215f, 14);
                m.Tube(p.Timber, new Vector3(0, 0.15f, 0), new Vector3(0.18f, 1.23f, 0.076f), 0.018f);
                Box(m, p.Dark, -0.095f, 0.052f, 0, 0.35f, 0.103f, 0.25f);
            });
            prop("Roof air conditioner", m => Condenser(m, p, false));
            prop("Wall condenser", m => Condenser(m, p, true));
            prop("Roof duct", m =>
            {
                Box(m, p.Metal, 0, 0.28f, 0, 1.6f, 0.56f, 0.64f, true);
                foreach (float x in new[] { -0.76f, 0f, 0.76f })
                    Box(m, p.Dark, x, 0.29f, 0, 0.048f, 0.586f, 0.667f);
            });
            prop("Exhaust cowl", m =>
            {
                m.Tube(p.Metal, Vector3.zero, Vector3.up * 0.68f, 0.22f, 16);
                m.Tube(p.Dark, Vector3.up * 0.65f, Vector3.up * 0.71f, 0.269f, 16);
                m.Frustum(p.Metal, Vector3.up * 0.72f, Vector3.up * 0.91f, 0.36f, 0.05f, 16);
            });
            prop("Roof antenna", m =>
            {
                Box(m, p.Concrete, 0, 0.124f, 0, 0.41f, 0.248f, 0.41f);
                m.Tube(p.Metal, Vector3.zero, Vector3.up * 2.16f, 0.024f);
                m.Tube(p.Metal, new Vector3(-0.73f, 1.87f, 0), new Vector3(0.73f, 1.87f, 0), 0.019f);
                for (int i = -3; i <= 3; i++)
                    m.Tube(p.Metal, new Vector3(i * 0.18f, 1.87f, -0.23f),
                        new Vector3(i * 0.18f, 1.87f, 0.23f), 0.012f);
            });
            prop("Service pipe bundle", m =>
            {
                for (int i = -1; i <= 1; i++)
                {
                    var turn = new Vector3(i * 0.15f, 0.31f, 0.51f);
                    m.Tube(p.Metal, new Vector3(turn.x, turn.y, -0.54f), turn, 0.047f);
                    m.Tube(p.Metal, turn, turn + Vector3.up * 0.56f, 0.047f);
                    m.Tube(i == 0 ? p.Red : p.Yellow, turn + Vector3.up * 0.35f, turn + Vector3.up * 0.374f, 0.052f);
                }
            });
            prop("Waste cart", m =>
            {
                Box(m, p.Olive, 0, 0.52f, 0, 0.69f, 0.83f, 0.55f, true);
                Box(m, p.Dark, 0, 0.948f, 0, 0.75f, 0.11f, 0.61f);
                foreach (float x in new[] { -0.279f, 0.279f })
                    m.Tube(p.Rubber, new Vector3(x - 0.043f, 0.11f, -0.2f),
                        new Vector3(x + 0.043f, 0.11f, -0.2f), 0.1f, 12);
            });
        }

        private static void Box(CityModel model, Material mat, float x, float y, float z,
            float width, float height, float depth, bool solid = false) =>
            model.Box(mat, new Vector3(x, y, z), new Vector3(width, height, depth), solid);

        private static void Table(CityModel m, CityPalette p, float width, float depth, float height, Material top)
        {
            Box(m, top, 0, height - 0.027f, 0, width, 0.054f, depth);
            foreach (float x in new[] { -width / 2 + 0.065f, width / 2 - 0.065f })
            foreach (float z in new[] { -depth / 2 + 0.052f, depth / 2 - 0.052f })
                Box(m, p.Dark, x, (height - 0.05f) / 2, z, 0.055f, height - 0.05f, 0.055f);
            m.CollisionBox(new Vector3(0, height / 2, 0), new Vector3(width, height, depth));
        }

        private static void Chair(CityModel m, CityPalette p, Material finish, float width)
        {
            Box(m, finish, 0, 0.451f, 0, width, 0.078f, 0.43f);
            foreach (float x in new[] { -width / 2 + 0.045f, width / 2 - 0.045f })
            foreach (float z in new[] { -0.156f, 0.156f })
                m.Tube(p.Dark, new Vector3(x * 1.18f, 0, z * 1.16f), new Vector3(x, 0.431f, z), 0.019f, 8);
            m.Box(finish, new Vector3(0, 0.69f, -0.201f), new Vector3(width - 0.035f, 0.34f, 0.061f),
                Quaternion.Euler(-8, 0, 0));
            m.CollisionBox(new Vector3(0, 0.42f, 0), new Vector3(width, 0.84f, 0.46f));
        }

        private static void Sofa(CityModel m, CityPalette p, float width, Material fabric)
        {
            Box(m, p.Dark, 0, 0.08f, 0, width - 0.12f, 0.16f, 0.59f);
            Box(m, fabric, 0, 0.249f, 0, width, 0.29f, 0.73f);
            Box(m, fabric, 0, 0.512f, -0.28f, width, 0.56f, 0.19f);
            foreach (float x in new[] { -width / 2 + 0.07f, width / 2 - 0.07f })
                Box(m, fabric, x, 0.369f, 0.015f, 0.14f, 0.4f, 0.68f);
            int cushions = width > 1.5f ? 3 : 1;
            float span = (width - 0.3f) / cushions;
            for (int i = 0; i < cushions; i++)
            {
                float x = (i + 0.5f - cushions / 2f) * span;
                Box(m, p.Textile, x, 0.418f, 0.066f, span - 0.024f, 0.097f, 0.515f);
            }
            m.CollisionBox(new Vector3(0, 0.35f, 0), new Vector3(width, 0.7f, 0.75f));
        }

        private static void Display(CityModel m, CityPalette p, float width, float height)
        {
            Box(m, p.Dark, 0, 0.025f, 0, width * 0.54f, 0.05f, 0.18f);
            Box(m, p.Dark, 0, 0.14f, 0, 0.068f, 0.265f, 0.057f);
            Box(m, p.Dark, 0, 0.145f + height * 0.53f, 0.017f, width, height, 0.041f);
            Box(m, p.Screen, 0, 0.145f + height * 0.53f, 0.041f, width - 0.036f, height - 0.026f, 0.008f);
        }

        private static void Shelving(CityModel m, CityPalette p, Material board, float width, float height, float depth)
        {
            Box(m, board, 0, height * 0.5f, -depth * 0.5f, width, height, 0.052f);
            foreach (float x in new[] { -width / 2, width / 2 })
                Box(m, p.Dark, x, height * 0.5f, 0, 0.052f, height, depth);
            for (int i = 0; i < 5; i++)
                Box(m, board, 0, 0.13f + i * (height - 0.13f) / 4, 0, width, 0.058f, depth);
            m.CollisionBox(new Vector3(0, height * 0.5f, 0), new Vector3(width + 0.052f, height, depth + 0.052f));
        }

        private static void Grille(CityModel m, Material material, float x, float y, float z, float width, int count)
        {
            for (int i = 0; i < count; i++)
                Box(m, material, x, y + i * 0.043f, z, width, 0.018f, 0.017f);
        }

        private static void Condenser(CityModel m, CityPalette p, bool wall)
        {
            float width = wall ? 0.91f : 1.42f;
            Box(m, p.Metal, 0, 0.46f, 0, width, 0.81f, 0.41f, true);
            var fan = new Vector3(wall ? 0.12f : 0.27f, 0.457f, 0.207f);
            m.Tube(p.Dark, fan, fan + Vector3.forward * 0.033f, 0.272f, 20);
            m.Tube(p.Metal, fan + Vector3.forward * 0.034f, fan + Vector3.forward * 0.048f, 0.054f, 12);
            for (int i = 0; i < 12; i++)
                m.Box(p.Metal, fan + Vector3.forward * 0.04f, new Vector3(0.51f, 0.012f, 0.016f),
                    Quaternion.Euler(0, 0, i * 15));
            if (!wall)
                Grille(m, p.Dark, -0.4f, 0.274f, 0.211f, 0.33f, 8);
        }

        private static void Planter(CityModel m, CityPalette p, float radius, float height, float plant,
            Material pot, bool solid)
        {
            m.Frustum(pot, Vector3.zero, Vector3.up * height, radius * 0.77f, radius, 16);
            m.Tube(p.Soil, Vector3.up * height, Vector3.up * (height + 0.003f), radius * 0.89f, 16);
            Foliage(m, p, Vector3.up * height, plant);
            if (solid)
                m.CollisionBox(Vector3.up * height / 2, new Vector3(radius * 2, height, radius * 2));
        }

        private static void Foliage(CityModel m, CityPalette p, Vector3 origin, float height)
        {
            for (int i = 0; i < 7; i++)
            {
                float phase = i * 2.39f;
                var spread = new Vector3(Mathf.Cos(phase) * 0.43f, 0.55f + i * 0.058f, Mathf.Sin(phase) * 0.43f);
                var tip = origin + spread * height;
                m.Tube(p.Leaf, origin, tip, height * 0.015f, 6);
                m.Ellipsoid(i % 3 == 0 ? p.LeafLight : p.Leaf, tip,
                    new Vector3(0.17f, 0.22f, 0.18f) * height, 8, 4);
            }
        }

        private static void Tree(CityModel m, CityPalette p, int seed, float scale, bool distant = false)
        {
            var random = new Random(seed);
            Vector3 trunk = new Vector3(0.12f, 3.17f, 0.15f) * scale;
            m.Box(p.Metal, new Vector3(0, 0.008f, 0), new Vector3(1.33f, 0.016f, 1.33f));
            m.Box(p.Soil, new Vector3(0, 0.019f, 0), new Vector3(1.19f, 0.02f, 1.19f));
            m.Frustum(p.Bark, Vector3.zero, trunk, 0.139f * scale, 0.055f * scale, 12);
            for (int i = 0; i < 25; i++)
            {
                float angle = (float)(i * 2.4 + random.NextDouble() * 0.34);
                float height = (float)(3 + random.NextDouble() * 2.46);
                float reach = (float)(0.55 + random.NextDouble() * 1.33) * (1.2f - (height - 3) * 0.19f);
                var branch = new Vector3(Mathf.Cos(angle) * reach, height, Mathf.Sin(angle) * reach) * scale;
                Vector3 fork = trunk * (0.69f + i % 4 * 0.105f);
                m.Frustum(p.Bark, fork, branch, 0.043f * scale, 0.013f * scale, distant ? 5 : 8);
                for (int leaf = 0; leaf < 94; leaf++)
                {
                    float theta = (float)random.NextDouble() * 2 * Mathf.PI;
                    float spread = Mathf.Sqrt((float)random.NextDouble());
                    var centre = branch + new Vector3(
                        Mathf.Cos(theta) * spread * 0.72f,
                        ((float)random.NextDouble() - 0.5f) * (1 - spread * 0.51f),
                        Mathf.Sin(theta) * spread * 0.65f) * scale;
                    float leafSize = (0.087f + (float)random.NextDouble() * 0.077f) * scale;
                    float tilt = (float)random.NextDouble() - 0.5f;
                    // Select from the original leaves so both meshes keep the same outline and branch positions.
                    if (distant && leaf % 6 != 0)
                        continue;
                    if (distant)
                        leafSize *= 1.95f;
                    Vector3 spine = new Vector3(Mathf.Cos(theta + 0.83f), tilt * 1.6f,
                        Mathf.Sin(theta + 0.83f)) * leafSize;
                    Vector3 side = new Vector3(spine.z, spine.x * 0.3f, -spine.x) * 0.48f;
                    m.Quad(leaf % 4 == 0 ? p.LeafLight : p.Leaf,
                        centre - spine, centre + side, centre + spine, centre - side);
                }
            }
            if (!distant)
                m.CollisionBox(new Vector3(0, 1.05f, 0), new Vector3(0.27f, 2.1f, 0.27f) * scale);
        }

        private static void Bicycle(CityModel m, CityPalette p)
        {
            for (int end = -1; end <= 1; end += 2)
            {
                var wheel = new Vector3(0, 0.336f, end * 0.59f);
                for (int i = 0; i < 20; i++)
                {
                    var a = Quaternion.Euler(i * 18, 0, 0) * new Vector3(0, 0.303f, 0);
                    var b = Quaternion.Euler((i + 1) * 18, 0, 0) * new Vector3(0, 0.303f, 0);
                    m.Tube(p.Rubber, wheel + a, wheel + b, 0.025f, 6);
                    if (i % 2 == 0)
                        m.Tube(p.Metal, wheel, wheel + a, 0.005f, 6);
                }
            }
            var seat = new Vector3(0, 0.89f, -0.23f);
            var crank = new Vector3(0, 0.42f, -0.14f);
            var fork = new Vector3(0, 0.88f, 0.42f);
            m.Tube(p.Olive, seat, crank, 0.029f);
            m.Tube(p.Olive, seat, fork, 0.027f);
            m.Tube(p.Olive, crank, fork, 0.032f);
            m.Tube(p.Dark, fork, new Vector3(0, 0.336f, 0.59f), 0.026f);
            m.Tube(p.Olive, crank, new Vector3(0, 0.336f, -0.59f), 0.027f);
            Box(m, p.Rubber, 0, 0.925f, -0.23f, 0.14f, 0.043f, 0.24f);
            m.Tube(p.Dark, fork, new Vector3(0, 1.0f, 0.4f), 0.021f);
            m.Tube(p.Dark, new Vector3(-0.22f, 1, 0.4f), new Vector3(0.22f, 1, 0.4f), 0.018f);
        }

        private static void Vehicle(CityModel m, CityPalette p, bool van)
        {
            float length = van ? 4.88f : 4.37f;
            float width = van ? 1.94f : 1.8f;
            float shell = van ? 0.93f : 0.7f;
            Material paint = van ? p.Paint : p.Blue;
            Box(m, paint, 0, shell, 0, width, van ? 1.08f : 0.61f, length, true);
            Box(m, p.Dark, 0, 0.346f, 0, width - 0.075f, 0.16f, length + 0.06f);
            float roof = van ? 2.18f : 1.45f;
            float mid = van ? 1.48f : 1.0f;
            float rear = van ? -length * 0.48f : -1.38f;
            float front = van ? 1.9f : 1.07f;
            float half = width * 0.465f;
            float inset = half * 0.89f;
            Box(m, paint, 0, roof - 0.022f, (rear + front - 0.53f) * 0.5f,
                inset * 2, 0.067f, front - rear - 0.55f);
            m.Quad(p.Screen, new Vector3(half, mid, front), new Vector3(inset, roof, front - 0.54f),
                new Vector3(-inset, roof, front - 0.54f), new Vector3(-half, mid, front));
            m.Quad(van ? paint : p.Screen, new Vector3(-half, mid, rear), new Vector3(-inset, roof, rear + 0.25f),
                new Vector3(inset, roof, rear + 0.25f), new Vector3(half, mid, rear));
            foreach (float side in new[] { -1f, 1f })
            {
                float x = side * half;
                float upper = side * inset;
                var a = new Vector3(x, mid, rear);
                var b = new Vector3(upper, roof, rear + 0.25f);
                var c = new Vector3(upper, roof, front - 0.54f);
                var d = new Vector3(x, mid, front);
                if (side < 0)
                    m.Quad(van ? paint : p.Screen, a, b, c, d);
                else
                    m.Quad(van ? paint : p.Screen, d, c, b, a);
                Box(m, paint, x, mid - 0.029f, 0, 0.069f, 0.065f, length - 0.095f);
                foreach (float z in new[] { -length * 0.31f, length * 0.29f })
                {
                    var tyre = new Vector3(side * (width / 2 - 0.093f), 0.328f, z);
                    var edge = tyre + Vector3.right * (0.132f * side);
                    m.Tube(p.Rubber, tyre, edge, 0.328f, 18);
                    m.Tube(p.Metal, edge, edge + Vector3.right * (0.018f * side), 0.178f, 14);
                }
                Box(m, p.Light, side * half * 0.77f, shell, length * 0.5f + 0.013f, 0.29f, 0.141f, 0.038f);
                Box(m, p.Red, side * half * 0.79f, shell, -length * 0.5f - 0.013f, 0.27f, 0.13f, 0.041f);
            }
        }

        private static void ShopSign(CityModel m, CityPalette p, string word)
        {
            Box(m, p.Dark, 0, 0.163f, 0, word.Length * 0.174f + 0.16f, 0.32f, 0.064f);
            for (int index = 0; index < word.Length; index++)
            {
                string glyph = word[index] switch
                {
                    'A' => "010101111101101",
                    'C' => "011100100100011",
                    'E' => "111100110100111",
                    'F' => "111100110100100",
                    'H' => "101101111101101",
                    'I' => "111010010010111",
                    'O' => "111101101101111",
                    'P' => "110101110100100",
                    'R' => "110101110101101",
                    'S' => "011100010001110",
                    'V' => "101101101101010",
                    _ => throw new ArgumentOutOfRangeException(nameof(word))
                };
                for (int row = 0; row < 5; row++)
                for (int column = 0; column < 3; column++)
                {
                    if (glyph[row * 3 + column] == '0')
                        continue;
                    float x = (word.Length * 0.5f - index - 0.5f) * 0.174f - (column - 1) * 0.04f;
                    Box(m, p.Light, x, 0.242f - row * 0.04f, 0.037f, 0.041f, 0.041f, 0.014f);
                }
            }
        }
    }
}
