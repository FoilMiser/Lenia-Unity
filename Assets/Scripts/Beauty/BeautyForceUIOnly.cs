using UnityEngine; using UnityEngine.UI;
public class BeautyForceUIOnly:MonoBehaviour{
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void T(){ var go=GameObject.Find("BeautyRawImage"); if(!go) return; var ri=go.GetComponent<RawImage>(); if(!ri) return; if(ri.texture==null && ri.material && ri.material.mainTexture) ri.texture=ri.material.mainTexture; ri.material=null; Debug.Log("[Beauty] Fallback to UI/Default (no custom shader).");}
}
