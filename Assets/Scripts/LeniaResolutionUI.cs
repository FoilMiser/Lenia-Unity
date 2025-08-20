using UnityEngine;
[DefaultExecutionOrder(55)]
public class LeniaResolutionUI : MonoBehaviour
{
    public LeniaSimulator sim;
    int[] sizes = new[]{512,1024,2048};
    void Start(){ if(!sim){
#if UNITY_2023_1_OR_NEWER
        sim = Object.FindFirstObjectByType<LeniaSimulator>();
#else
        sim = Object.FindFirstObjectByType<LeniaSimulator>();
#endif
    } }
    void OnGUI(){
        if(!sim) return;
        var r = new Rect(Screen.width-150, 8, 140, 26);
        foreach(var s in sizes){ if(GUI.Button(r,$"{s}²")){ sim.width=s; sim.height=s; } r.y+=28; }
        r.y+=6;
        GUI.Label(new Rect(r.x, r.y, 140, 20), $"Speed: {sim.stepsPerFrame}");
        r.y+=20;
        if(GUI.Button(new Rect(r.x, r.y, 64, 24), "-")) sim.SetStepsPerFrame(sim.stepsPerFrame-1);
        if(GUI.Button(new Rect(r.x+76, r.y, 64, 24), "+")) sim.SetStepsPerFrame(sim.stepsPerFrame+1);
    }
}

