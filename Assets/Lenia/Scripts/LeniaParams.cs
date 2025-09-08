using UnityEngine;

[CreateAssetMenu(fileName="LeniaParams", menuName="Lenia/Params", order=0)]
public class LeniaParams : ScriptableObject
{
    // TODO: paste full LeniaParams.cs here
    // Temporary minimal fields so it compiles:
    public int width = 1024, height = 1024;
    [Range(1,4)] public int channels = 1;
    public bool wrap = true;
    public float dt = 0.1f;
    public int stepsPerFrame = 1;
}
