using UnityEngine;using UnityEngine.UI;
[RequireComponent(typeof(LeniaSimulation))]
public class LeniaDisplay:MonoBehaviour{
 public RawImage target; public Material blitMaterial; [Range(0.2f,3f)]public float gamma=1f;[Range(0.2f,3f)]public float exposure=1f; public bool invert=false;
 LeniaSimulation sim;
 void Awake(){sim=GetComponent<LeniaSimulation>();EnsureUI();EnsureMaterial();}
 void EnsureUI(){if(target!=null)return;var canvasGO=GameObject.Find("LeniaCanvas");if(canvasGO==null){canvasGO=new GameObject("LeniaCanvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvasGO.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;}var imgGO=new GameObject("View",typeof(RawImage));imgGO.transform.SetParent(canvasGO.transform,false);var ri=imgGO.GetComponent<RawImage>();ri.rectTransform.anchorMin=Vector2.zero;ri.rectTransform.anchorMax=Vector2.one;ri.rectTransform.offsetMin=Vector2.zero;ri.rectTransform.offsetMax=Vector2.zero;target=ri;}
 void EnsureMaterial(){if(blitMaterial==null)blitMaterial=new Material(Shader.Find("Hidden/Lenia/BlitRFloat")); if(target!=null)target.material=blitMaterial;}
 void LateUpdate(){if(sim!=null&&target!=null){target.texture=sim?sim.CurrentTexture:null;} if(blitMaterial!=null){blitMaterial.SetFloat("_Gamma",Mathf.Max(0.0001f,gamma));blitMaterial.SetFloat("_Exposure",Mathf.Max(0f,exposure));blitMaterial.SetFloat("_Invert",invert?1f:0f);} }
}

