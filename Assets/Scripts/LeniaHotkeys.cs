using UnityEngine;
[DefaultExecutionOrder(50)]
public class LeniaHotkeys : MonoBehaviour
{
    public LeniaSimulator sim;
    void Start(){ if(!sim) sim = FindObjectOfType<LeniaSimulator>(); }
    void Update(){
        if(!sim) return;
        if (Input.GetKeyDown(KeyCode.Space)) sim.TogglePause();
        if (Input.GetKeyDown(KeyCode.LeftBracket))  sim.SetStepsPerFrame(sim.stepsPerFrame-1);
        if (Input.GetKeyDown(KeyCode.RightBracket)) sim.SetStepsPerFrame(sim.stepsPerFrame+1);
    }
}
