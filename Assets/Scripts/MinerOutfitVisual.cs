using UnityEngine;

public sealed class MinerOutfitVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Transform hat;
    [SerializeField] private Transform pickaxe;

    public void Configure(SpriteRenderer bodyRenderer, Transform hatTransform, Transform pickaxeTransform)
    {
        body = bodyRenderer;
        hat = hatTransform;
        pickaxe = pickaxeTransform;
    }

    private void LateUpdate()
    {
        if (body == null) return;
        float direction = body.flipX ? -1f : 1f;
        if (hat != null) hat.localPosition = new Vector3(0.02f * direction, 0.31f, -0.01f);
        if (pickaxe != null)
        {
            pickaxe.localPosition = new Vector3(0.27f * direction, -0.02f, -0.02f);
            pickaxe.localScale = new Vector3(direction, 1f, 1f);
        }
    }
}
