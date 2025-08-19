using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(RawImage))]
public class LeniaDisplayBasic : MonoBehaviour {
  public LeniaSimulator sim; RawImage ri;
  void Awake(){ ri = GetComponent<RawImage>(); }
  void Update(){ if (sim && sim.CurrentState) ri.texture = sim.CurrentState; }
}
