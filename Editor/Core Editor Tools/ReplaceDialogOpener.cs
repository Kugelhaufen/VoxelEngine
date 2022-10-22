using UnityEditor;

#if UNITY_EDITOR
namespace VoxelEngine.EditorTools
{
    internal static class ReplaceDialogOpener
    {
        internal static bool AskIfReplace(string assetName)
        {
            return EditorUtility.DisplayDialog("Replace existing asset?", "The Asset '" + assetName + "' already exists. Do you want to replace it?", "Yes", "No");
        }
    }
}
#endif