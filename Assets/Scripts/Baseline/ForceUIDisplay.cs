namespace LeniaBeauty{using UnityEngine;using UnityEngine.UI;using UnityEngine.Rendering;public class ForceUIDisplay:MonoBehaviour{[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Apply(){// remove our experimental overlay if it exists
var bc=GameObject.Find("BeautyCanvas"); if(bc) Object.Destroy(bc);
// fix every RawImage so it shows with UI/Default, never magenta
var ris=GameObject.FindObjectsByType<RawImage>(FindObjectsInactive.Include,FindObjectsSortMode.None); int fixedCnt=0;
foreach(var ri in ris){Texture src=ri.texture;
// if RawImage.texture is empty, try to steal a real image from its material
if(src==null && ri.material){ if(ri.material.mainTexture) src=ri.material.mainTexture; if(src==null && ri.material.shader){ var sh=ri.material.shader; int pc=sh.GetPropertyCount(); for(int i=0;i<pc;i++){ if(sh.GetPropertyType(i)!=ShaderPropertyType.Texture) continue; string nm=sh.GetPropertyName(i).ToLowerInvariant(); if(nm.Contains("lut")||nm.Contains("palette")||nm.Contains("grad")) continue; var t=ri.material.GetTexture(sh.GetPropertyNameId(i)); if(t!=null){ src=t; break; } } } }
if(src!=null){ ri.texture=src; ri.material=null; fixedCnt++; } }
Debug.Log("[Baseline] UI display fixed on "+fixedCnt+" RawImage(s)");}}}
