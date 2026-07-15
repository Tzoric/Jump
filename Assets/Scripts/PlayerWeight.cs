using UnityEngine;

public sealed class PlayerWeight : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float baseWeight = 1f;
    [SerializeField, Min(0f)] private float carriedWeight;
    [SerializeField, Min(0.1f)] private float weightMultiplier = 1f;
    [SerializeField, Min(0.1f)] private float gravityMultiplier = 1f;

    public float BaseWeight => baseWeight;
    public float CarriedWeight => carriedWeight;
    public float ApparentWeight => Mathf.Max(0.1f,
        (baseWeight + carriedWeight) * weightMultiplier * gravityMultiplier);

    public void SetCarriedWeight(float value)
    {
        carriedWeight = Mathf.Max(0f, value);
    }

    public void AddCarriedWeight(float amount)
    {
        carriedWeight = Mathf.Max(0f, carriedWeight + amount);
    }

    public void SetWeightMultiplier(float multiplier)
    {
        weightMultiplier = Mathf.Max(0.1f, multiplier);
    }

    public void SetGravityMultiplier(float multiplier)
    {
        gravityMultiplier = Mathf.Max(0.1f, multiplier);
    }

    public void ResetPowerUpModifiers()
    {
        weightMultiplier = 1f;
        gravityMultiplier = 1f;
    }
}
