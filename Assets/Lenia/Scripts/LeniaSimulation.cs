using UnityEngine;
[DisallowMultipleComponent]
public class LeniaSimulation:MonoBehaviour{
 [Header("Grid")][Min(8)]public int width=512;[Min(8)]public int height=512;[Range(1,64)]public int radius=13;
 [Header("Lenia Params")][Range(0f,1f)]public float mu=0.147f;[Range(0.001f,0.2f)]public float sigma=0.014f;[Range(0,2)]public float dt=1f;[Range(0,8)]public float timeScale=1f;
 [Header("Run State")]public bool paused=false;[Tooltip("Random seed (0 = use time-based)")]public int seed=0;
 [Header("GPU")]public ComputeShader leniaCS;
 RenderTexture A,B;int kStep;bool useA=true;
 public Texture CurrentTexture=>useA?A:B;
 void OnEnable(){if(leniaCS==null)Debug.LogWarning("Assign Lenia2D.compute to 'leniaCS'");Init();}
 void OnDisable(){ReleaseRTs();}void OnDestroy(){ReleaseRTs();}
 void OnValidate(){width=Mathf.Max(8,width);height=Mathf.Max(8,height);radius=Mathf.Clamp(radius,1,64);}
 void Update(){if(!Application.isPlaying)return; if(Input.GetKeyDown(KeyCode.R))Reseed(); if(!paused)Step(Time.deltaTime*timeScale);}
 public void Reseed(){Init();}
 void Init(){ReleaseRTs();A=MakeRT(width,height);B=MakeRT(width,height);kStep=leniaCS?leniaCS.FindKernel("Step"):-1;Random.InitState(seed!=0?seed:System.Environment.TickCount);var tmp=new Texture2D(width,height,TextureFormat.RFloat,false,true);var cols=new Color[width*height];for(int i=0;i<cols.Length;i++)cols[i]=new Color(Random.value,0,0,0);tmp.SetPixels(cols);tmp.Apply(false,false);Graphics.Blit(tmp,A);Destroy(tmp);useA=true;}
 RenderTexture MakeRT(int w,int h){var rt=new RenderTexture(w,h,0,RenderTextureFormat.RFloat,RenderTextureReadWrite.Linear);rt.enableRandomWrite=true;rt.wrapMode=TextureWrapMode.Repeat;rt.filterMode=FilterMode.Bilinear;rt.Create();return rt;}
 void ReleaseRTs(){if(A!=null){A.Release();DestroyImmediate(A);A=null;}if(B!=null){B.Release();DestroyImmediate(B);B=null;}}
 public void Step(float delta){if(!leniaCS||kStep<0)return;var src=useA?A:B;var dst=useA?B:A;leniaCS.SetInt("_Width",width);leniaCS.SetInt("_Height",height);leniaCS.SetInt("_R",radius);leniaCS.SetFloat("_Mu",mu);leniaCS.SetFloat("_Sigma",sigma);leniaCS.SetFloat("_Dt",dt*delta);leniaCS.SetTexture(kStep,"_Src",src);leniaCS.SetTexture(kStep,"_Dst",dst);uint gx,gy,gz;leniaCS.GetKernelThreadGroupSizes(kStep,out gx,out gy,out gz);int tx=Mathf.CeilToInt(width/(float)gx);int ty=Mathf.CeilToInt(height/(float)gy);leniaCS.Dispatch(kStep,tx,ty,1);useA=!useA;}
}

