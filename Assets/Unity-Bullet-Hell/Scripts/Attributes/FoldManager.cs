using UnityEditor;
#if UNITY_EDITOR
public static class FoldManager
{
    public static bool GetFold(string name)
    {
        if (!EditorPrefs.HasKey(name)) {        
            EditorPrefs.SetBool(name, false);            
        }

        return EditorPrefs.GetBool(name);
    }

    public static void SetFold(string name, bool value)
    {
        EditorPrefs.SetBool(name, value);        
    }
}
#endif