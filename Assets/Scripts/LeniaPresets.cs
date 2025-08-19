using UnityEngine;
[DefaultExecutionOrder(60)]
public class LeniaPresets : MonoBehaviour
{
    public LeniaSimulator sim; public LeniaSeeder seeder;
    void Start(){
        if(!sim){
            #if UNITY_2023_1_OR_NEWER
            sim = Object.FindFirstObjectByType<LeniaSimulator>();
            #else
            sim = Object.FindObjectOfType<LeniaSimulator>();
            #endif
        }
        if(!seeder){
            #if UNITY_2023_1_OR_NEWER
            seeder = Object.FindFirstObjectByType<LeniaSeeder>();
            #else
            seeder = Object.FindObjectOfType<LeniaSeeder>();
            #endif
        }
    }

    void Update(){
        if(!sim) return;
        if (Input.GetKeyDown(KeyCode.Alpha1)){ Apply(0.15f,0.016f,24,0.30f,0.10f); if(seeder){ seeder.noiseDensity=0.25f; seeder.SeedNoise(); } }
        if (Input.GetKeyDown(KeyCode.Alpha2)){ Apply(0.14f,0.028f,24,0.30f,0.08f); if(seeder){ seeder.noiseDensity=0.35f; seeder.SeedNoise(); } }
        if (Input.GetKeyDown(KeyCode.Alpha3)){ Apply(0.10f,0.020f,18,0.45f,0.10f); if(seeder){ seeder.noiseDensity=0.30f; seeder.SeedNoise(); } }
        if (Input.GetKeyDown(KeyCode.Alpha4)){ Apply(0.18f,0.012f,28,0.25f,0.08f); if(seeder){ seeder.noiseDensity=0.20f; seeder.SeedNoise(); } }
    }
    void Apply(float mu,float sigma,int R,float beta,float dt){ sim.ApplyPreset(mu,sigma,R,beta,dt); }
}
