using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

[DefaultExecutionOrder(80)]
public class LeniaPainter : MonoBehaviour
{
    public LeniaSimulator sim;
    [Range(0.002f, 0.2f)] public float brushRadiusUV = 0.03f;
    [Range(0f,1f)] public float hardness = 0.5f;
    [Range(0f,1f)] public float opacity  = 1.0f;

    Material brushMat;

    void Start(){
        if(!sim){
            #if UNITY_2023_1_OR_NEWER
            sim = Object.FindFirstObjectByType<LeniaSimulator>();
            #else
            sim = Object.FindObjectOfType<LeniaSimulator>();
            #endif
        }
        var sh = Shader.Find("Hidden/BrushBlit");
        if(sh) brushMat = new Material(sh);
    }

    void Update(){
        if(!sim || brushMat==null || sim.EnvTex==null) return;
        if (!ShiftDown()) return;

        if (LMB() || RMB()){
            var mpos = GetMouseXY01();
            float target = LMB()? 0f : 1f; // wall/open
            PaintAtUV(mpos, target);
        }

        if (MinusPressed()) brushRadiusUV = Mathf.Max(0.002f, brushRadiusUV*0.8f);
        if (PlusPressed())  brushRadiusUV = Mathf.Min(0.2f,   brushRadiusUV*1.25f);
    }

    Vector2 GetMouseXY01(){
        #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var m = Mouse.current; if (m==null) return Vector2.zero;
        return new Vector2(m.position.x.ReadValue()/Screen.width, m.position.y.ReadValue()/Screen.height);
        #else
        return new Vector2(Input.mousePosition.x/Screen.width, Input.mousePosition.y/Screen.height);
        #endif
    }
    bool ShiftDown(){
        #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var k = Keyboard.current; return k!=null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed);
        #else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        #endif
    }
    bool LMB(){
        #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var m = Mouse.current; return m!=null && m.leftButton.isPressed;
        #else
        return Input.GetMouseButton(0);
        #endif
    }
    bool RMB(){
        #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var m = Mouse.current; return m!=null && m.rightButton.isPressed;
        #else
        return Input.GetMouseButton(1);
        #endif
    }
    bool MinusPressed(){
        #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var k = Keyboard.current; return k!=null && (k.minusKey.wasPressedThisFrame || (k.numpadMinusKey!=null && k.numpadMinusKey.wasPressedThisFrame));
        #else
        return Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.Underscore);
        #endif
    }
    bool PlusPressed(){
        #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var k = Keyboard.current; return k!=null && (k.equalsKey.wasPressedThisFrame || (k.numpadPlusKey!=null && k.numpadPlusKey.wasPressedThisFrame));
        #else
        return Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus);
        #endif
    }

    void PaintAtUV(Vector2 uv, float targetValue)
    {
        var env = sim.EnvTex;
        var tmp = RenderTexture.GetTemporary(env.descriptor);
        Graphics.Blit(env, tmp);

        brushMat.SetTexture("_MainTex", tmp);
        brushMat.SetVector("_BrushCenter", new Vector4(uv.x, uv.y, 0, 0));
        brushMat.SetFloat("_BrushRadius", brushRadiusUV);
        brushMat.SetFloat("_Hardness", hardness);
        brushMat.SetFloat("_Opacity", opacity);
        brushMat.SetFloat("_TargetValue", targetValue);

        Graphics.Blit(tmp, env, brushMat);
        RenderTexture.ReleaseTemporary(tmp);
    }
}
