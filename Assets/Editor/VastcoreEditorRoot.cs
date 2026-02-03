using UnityEngine;

namespace Vastcore.Editor
{
    /// <summary>
    /// Root editor class for Vastcore.Editor assembly
    /// Ensures the assembly compiles without "no scripts associated" warning
    /// </summary>
    public abstract class VastcoreEditorRoot : ScriptableObject
    {
        // Base class for editor functionality
        // This class ensures the Vastcore.Editor.Root assembly has associated scripts
    }
}
