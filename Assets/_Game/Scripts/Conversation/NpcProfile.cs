using System;
using UnityEngine;

// PROTOTYPE CODE: само авторската гледна точка; няма поле за истина по случая.
[CreateAssetMenu(menuName = "Detective Game/NPC Profile")]
public sealed class NpcProfile : ScriptableObject
{
    public string personId;
    public string displayName;
    [TextArea] public string role;
    [TextArea] public string speakingStyle;
    public NpcKnowledge[] knowledge = Array.Empty<NpcKnowledge>();
}

[Serializable]
public sealed class NpcKnowledge
{
    public string id;
    public string category;
    [TextArea] public string text;
    // Празно = достъпно. Непразно = само след външно проверено действие.
    public string requiredMaterialId;
}
