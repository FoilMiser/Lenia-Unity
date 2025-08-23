#if UNITY_EDITOR
using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine;using UnityEngine.UI;using System.IO;
public static class LeniaBootstrap{
 [InitializeOnLoadMethod] static void CreateSceneIfMissing(){
  const string key="LeniaMinimalSceneCreated"; if(EditorPrefs.GetBool(key,false))return;
  var scenePath="Assets/Lenia/Scenes/LeniaMinimal.unity"; Directory.CreateDirectory("Assets/Lenia/Scenes"); Directory.CreateDirectory("Assets/Lenia/Materials");
  var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  var go=new GameObject("Lenia"); var sim=go.AddComponent<LeniaSimulation>(); var disp=go.AddComponent<LeniaDisplay>();
  var guids=AssetDatabase.FindAssets("Lenia2D t:ComputeShader"); if(guids!=null&&guids.Length>0){var path=AssetDatabase.GUIDToAssetPath(guids[0]); var cs=AssetDatabase.LoadAssetAtPath<ComputeShader>(path); sim.leniaCS=cs;}
  var shader=Shader.Find("Hidden/Lenia/BlitRFloat"); if(shader!=null){var mat=new Material(shader); var matPath="Assets/Lenia/Materials/LeniaBlit.mat"; AssetDatabase.CreateAsset(mat,matPath); disp.blitMaterial=AssetDatabase.LoadAssetAtPath<Material>(matPath);}
  var canvasGO=new GameObject("LeniaCanvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)); canvasGO.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
  var imgGO=new GameObject("View",typeof(RawImage)); imgGO.transform.SetParent(canvasGO.transform,false); var ri=imgGO.GetComponent<RawImage>(); ri.rectTransform.anchorMin=Vector2.zero; ri.rectTransform.anchorMax=Vector2.one; ri.rectTransform.offsetMin=Vector2.zero; ri.rectTransform.offsetMax=Vector2.zero; disp.target=ri; if(disp.blitMaterial!=null)ri.material=disp.blitMaterial;
  EditorSceneManager.SaveScene(scene,scenePath); AssetDatabase.Refresh(); EditorPrefs.SetBool(key,true); EditorSceneManager.OpenScene(scenePath);
 }}
#endif

