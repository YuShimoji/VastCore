using System;
using UnityEngine;

namespace Vastcore.Editor.DesignerCockpit
{
    [Serializable]
    public class RandomTransformRecipe
    {
        public Vector3 positionMin = new Vector3(-2f, 0f, -2f);
        public Vector3 positionMax = new Vector3(2f, 0f, 2f);
        public Vector3 rotationMin = Vector3.zero;
        public Vector3 rotationMax = new Vector3(0f, 360f, 0f);
        public Vector3 scaleMin = new Vector3(0.8f, 0.8f, 0.8f);
        public Vector3 scaleMax = new Vector3(1.2f, 1.2f, 1.2f);
        public bool useRelativePosition = true;
        public bool useUniformScale = true;
    }

    public class VastCoreDesignerSession : ScriptableObject
    {
        public string sessionName = "Designer Session";
        public int seed = 12345;
        [TextArea(3, 8)] public string notes = "Local designer session.";
        public string lastSavedUtc = "Unsaved";
        public RandomTransformRecipe randomTransform = new RandomTransformRecipe();

        [TextArea(2, 5)] public string randomControlStatusNote = "Available in cockpit as deterministic random transform. RandomControlTab remains separately unverified.";
        [TextArea(2, 5)] public string compositionStatusNote = "CompositionTab is visible from status only. CSG verification is pending unless run manually.";
        [TextArea(2, 5)] public string deformStatusNote = "Deform tab availability depends on package symbols and has not been executed by this cockpit.";
        [TextArea(2, 5)] public string terrainStatusNote = "Terrain parameter types are status-checked only. Terrain generation is not executed by this cockpit.";
        [TextArea(2, 5)] public string topologyStatusNote = "Topology/DualGrid is a future dashboard slot only.";

        public void ResetDefaults()
        {
            sessionName = "Designer Session";
            seed = 12345;
            notes = "Local designer session.";
            lastSavedUtc = "Unsaved";
            randomTransform = new RandomTransformRecipe();
            randomControlStatusNote = "Available in cockpit as deterministic random transform. RandomControlTab remains separately unverified.";
            compositionStatusNote = "CompositionTab is visible from status only. CSG verification is pending unless run manually.";
            deformStatusNote = "Deform tab availability depends on package symbols and has not been executed by this cockpit.";
            terrainStatusNote = "Terrain parameter types are status-checked only. Terrain generation is not executed by this cockpit.";
            topologyStatusNote = "Topology/DualGrid is a future dashboard slot only.";
        }
    }
}
