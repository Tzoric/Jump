using UnityEngine;

[CreateAssetMenu(menuName = "Jump/Character Outfit", fileName = "CharacterOutfit")]
public sealed class CharacterOutfitDefinition : ScriptableObject
{
    [SerializeField] private string characterIdentity = "Main Hero";
    [SerializeField] private string outfitId = "miner";
    [SerializeField] private Sprite animationSheet;
    [SerializeField] private Sprite handTool;

    public string CharacterIdentity => characterIdentity;
    public string OutfitId => outfitId;
    public Sprite AnimationSheet => animationSheet;
    public Sprite HandTool => handTool;

    public void Configure(string identity, string id, Sprite sheet, Sprite tool)
    {
        characterIdentity = string.IsNullOrWhiteSpace(identity) ? "Main Hero" : identity;
        outfitId = string.IsNullOrWhiteSpace(id) ? "default" : id;
        animationSheet = sheet;
        handTool = tool;
    }
}
