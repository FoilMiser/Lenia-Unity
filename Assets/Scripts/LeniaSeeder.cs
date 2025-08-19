using UnityEngine;
[DefaultExecutionOrder(100)]
public class LeniaSeeder : MonoBehaviour {
  public LeniaSimulator sim; public bool autoSeedOnPlay = true;
  [Range(0f,1f)] public float noiseDensity = 0.35f; public bool addOrbium = false;

  void Start(){
    if(!sim){
#if UNITY_2023_1_OR_NEWER
      sim = Object.FindFirstObjectByType<LeniaSimulator>();
#else
      sim = Object.FindObjectOfType<LeniaSimulator>();
#endif
    }
    if(autoSeedOnPlay){ SeedNoise(); if(addOrbium) SeedOrbium(); }
  }

  public void Clear(float value = 0f){
    if(!sim) return;
    int w=sim.width, h=sim.height; var tex=new Texture2D(w,h,TextureFormat.RFloat,false,true);
    var data=new Color[w*h]; for(int i=0;i<data.Length;i++) data[i]=new Color(value,0,0,0);
    tex.SetPixels(data); tex.Apply(false); sim.LoadSeedTexture(tex); Destroy(tex);
  }

  public void SeedNoise(){ if(sim) sim.ReSeedNoise(-1, noiseDensity); }

  public void SeedFewBlobs(int count = 80, float minRadiusUV = 0.007f, float maxRadiusUV = 0.02f, float amplitude = 1f){
    if(!sim) return; int w=sim.width, h=sim.height;
    var tex=new Texture2D(w,h,TextureFormat.RFloat,false,true); var data=new Color[w*h];
    for(int i=0;i<count;i++){
      float cx = Random.value*w, cy = Random.value*h;
      float R  = Mathf.Lerp(minRadiusUV, maxRadiusUV, Random.value) * Mathf.Min(w,h);
      float inv2sig2 = 1.0f/(2.0f*R*R);
      for(int y=Mathf.Max(0,(int)(cy-3*R)); y<Mathf.Min(h,(int)(cy+3*R)); y++)
      for(int x=Mathf.Max(0,(int)(cx-3*R)); x<Mathf.Min(w,(int)(cx+3*R)); x++){
        float dx=x-cx, dy=y-cy; float g = Mathf.Exp(-(dx*dx+dy*dy)*inv2sig2);
        int idx=y*w+x; float v = Mathf.Clamp01(data[idx].r + amplitude*g);
        data[idx] = new Color(v,0,0,0);
      }
    }
    tex.SetPixels(data); tex.Apply(false); sim.LoadSeedTexture(tex); Destroy(tex);
  }

  public void SeedOrbium(){
    if(!sim) return; int w=sim.width, h=sim.height; var tex=new Texture2D(w,h,TextureFormat.RFloat,false,true);
    var data=new Color[w*h]; float cx=(w-1)/2f, cy=(h-1)/2f; float R=Mathf.Min(w,h)*0.12f;
    for(int y=0;y<h;y++) for(int x=0;x<w;x++){
      float dx=x-cx, dy=y-cy; float r=Mathf.Sqrt(dx*dx+dy*dy);
      float ring=Mathf.Exp(-0.5f*Mathf.Pow((r-R)/(0.22f*R),2f));
      data[y*w+x] = new Color(Mathf.Clamp01(ring),0,0,0);
    }
    tex.SetPixels(data); tex.Apply(false); sim.LoadSeedTexture(tex); Destroy(tex);
  }
}
