using UnityEditor;
using UnityEngine;

public static class NpcConversationTestMenu
{
    [MenuItem("Tools/Detective Game/NPC/Check Boundaries")]
    public static async void Boundaries()
    {
        // Клонинг: тестът никога не променя авторския asset.
        var profile = Object.Instantiate(Resources.Load<NpcProfile>("NpcPrototype/TestNpc"));
        try { await NpcConversationChecks.Boundaries(profile); }
        catch (System.Exception error) { Debug.LogException(error); }
        finally { Object.DestroyImmediate(profile); }
    }

    [MenuItem("Tools/Detective Game/NPC/Check Local Model")]
    public static async void Live()
    {
        try { await NpcConversationChecks.Live(Resources.Load<NpcProfile>("NpcPrototype/TestNpc")); }
        catch (System.Exception error) { Debug.LogException(error); }
    }
}
