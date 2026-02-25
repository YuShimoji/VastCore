using NUnit.Framework;
using UnityEngine;
using Vastcore.Generation;
using Vastcore.Generation.Map;
using PrimitiveType = Vastcore.Generation.PrimitiveTerrainGenerator.PrimitiveType;

namespace Vastcore.Testing.EditMode.GenerationTests
{
    public class PrimitiveGeneratorFactoryEditModeTests
    {
        [Test]
        public void CreateGenerator_ForSupportedTypes_ReturnsMatchingGenerator()
        {
            var cube = PrimitiveGeneratorFactory.CreateGenerator(PrimitiveType.Cube);
            var sphere = PrimitiveGeneratorFactory.CreateGenerator(PrimitiveType.Sphere);
            var cylinder = PrimitiveGeneratorFactory.CreateGenerator(PrimitiveType.Cylinder);
            var pyramid = PrimitiveGeneratorFactory.CreateGenerator(PrimitiveType.Pyramid);

            Assert.AreEqual(PrimitiveType.Cube, cube.PrimitiveType);
            Assert.AreEqual(PrimitiveType.Sphere, sphere.PrimitiveType);
            Assert.AreEqual(PrimitiveType.Cylinder, cylinder.PrimitiveType);
            Assert.AreEqual(PrimitiveType.Pyramid, pyramid.PrimitiveType);
        }

        [Test]
        public void GetDefaultScale_ForSupportedTypes_ReturnsPositiveScale()
        {
            var cubeScale = PrimitiveGeneratorFactory.GetDefaultScale(PrimitiveType.Cube);
            var sphereScale = PrimitiveGeneratorFactory.GetDefaultScale(PrimitiveType.Sphere);

            Assert.That(cubeScale, Is.EqualTo(new Vector3(100f, 100f, 100f)));
            Assert.That(sphereScale.x, Is.GreaterThan(0f));
            Assert.That(sphereScale.y, Is.GreaterThan(0f));
            Assert.That(sphereScale.z, Is.GreaterThan(0f));
        }
    }
}
