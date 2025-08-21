using UnityEngine;

[CreateAssetMenu(menuName="Lenia/Growth Profile")]
public class LeniaGrowthProfile : ScriptableObject
{
    [Range(0f,1f)] public float mu = 0.15f;
    [Range(0.005f,0.5f)] public float sigma = 0.015f;
    [Range(0.001f,1f)] public float dt = 0.1f;  // integration step

    // CPU helper for previews; GPU uses the same formula in the compute shader
    public float Evaluate(float u)
    {
        float g = Mathf.Exp(-0.5f * (u - mu)*(u - mu) / (sigma*sigma + 1e-8f));
        return g * 2f - 1f;
    }
}
