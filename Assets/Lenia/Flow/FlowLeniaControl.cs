using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using BM = global::BoundaryMode;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;            // New Input System
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(FlowLeniaSimulation))]
[RequireComponent(typeof(FlowLeniaDisplay))]
public class FlowLeniaControl : MonoBehaviour
{
    public FlowLeniaSimulation sim;
    public FlowLeniaDisplay    display;

    [Header("View Controls")]
    [Tooltip("Pan speed in UV units per second at zoom = 1")]
    public float panSpeed = 0.6f;
    [Tooltip("Scroll-wheel multiplier per notch (>1 zoom in)")]
    public float zoomStep = 1.12f;
    public float minZoom = 0.25f, maxZoom = 12f;

    [Header("Seeding (Starting Mass)")]
    [Range(1, 32)]    public int   seedCount  = 3;
    [Range(0.01f,.5f)]public float sigmaNorm = 0.08f; // fraction of min(width,height)
    [Range(0f, 5f)]   public float startMass  = 1.0f;
    public bool centerFirst = true;

    float _savedDt = 1f;
    bool  _paused  = false;

    void Awake()
    {
        if (!sim)     sim     = GetComponent<FlowLeniaSimulation>();
        if (!display) display = GetComponent<FlowLeniaDisplay>();
    }

    void Update()
    {
        if (!sim || !display) return;
        if (!Application.isFocused) return; // ignore input if game view not focused

        // ---- keys (new + old input supported) ----
        bool key_pause    = GetKeyDown_Space();
        bool key_step     = GetKeyDown_Period();
        bool key_bndry    = GetKeyDown(KeyCode.B, Key.period);   // cycle boundary
        bool key_massInfo = GetKeyDown(KeyCode.M, Key.m);

        bool key_dtDec    = GetKeyDown(KeyCode.Minus,  Key.minus);
        bool key_dtInc    = GetKeyDown(KeyCode.Equals, Key.equals);
        bool key_tempDec  = GetKeyDown(KeyCode.LeftBracket,  Key.leftBracket);
        bool key_tempInc  = GetKeyDown(KeyCode.RightBracket, Key.rightBracket);

        bool key_seed1    = GetKeyDown(KeyCode.Alpha1, Key.digit1);
        bool key_seed2    = GetKeyDown(KeyCode.Alpha2, Key.digit2);
        bool key_seed3    = GetKeyDown(KeyCode.Alpha3, Key.digit3);
        bool key_reseed   = GetKeyDown(KeyCode.R, Key.r);
        bool key_home     = GetKeyDown(KeyCode.Home, Key.home);

        // Pause / Step
        if (key_pause) TogglePause();
        if (_paused && key_step) StepOnce();

        // Pan (WASD / arrows)
        Vector2 move = GetMove2D();
        if (move.sqrMagnitude > 0f)
        {
            float z = Mathf.Max(0.05f, display.zoom);
            display.pan += (move * panSpeed * Time.unscaledDeltaTime) / z;
        }

        // Zoom (mouse wheel)
        float scrollNotches = GetScrollNotches();
        if (Mathf.Abs(scrollNotches) > 0f)
        {
            float z = display.zoom * Mathf.Pow(zoomStep, scrollNotches);
            display.zoom = Mathf.Clamp(z, minZoom, maxZoom);
        }

        // Parameter nudges
        if (key_tempDec) sim.temperature = Mathf.Max(0f, sim.temperature - 0.05f);
        if (key_tempInc) sim.temperature += 0.05f;
        if (key_dtDec)   sim.dt = Mathf.Max(0.01f, sim.dt - 0.05f);
        if (key_dtInc)   sim.dt += 0.05f;

        // Cycle boundary mode
        if (key_bndry)
            sim.boundary = (BM)(((int)sim.boundary + 1) % 3);

        // Toggle mass diagnostics
        if (key_massInfo)
            sim.computeMassEachFrame = !sim.computeMassEachFrame;

        // Reseed hotkeys (starting mass presets)
        if (key_seed1) { startMass = 0.5f; sigmaNorm = 0.05f; seedCount = 2; Reseed(); }
        if (key_seed2) { startMass = 1.0f; sigmaNorm = 0.08f; seedCount = 3; Reseed(); }
        if (key_seed3) { startMass = 2.0f; sigmaNorm = 0.12f; seedCount = 5; Reseed(); }
        if (key_reseed) Reseed();

        // Reset view
        if (key_home) { display.pan = Vector2.zero; display.zoom = 1f; }
    }

    // ---- Input helpers ----
    bool GetKeyDown_Space() => GetKeyDown(KeyCode.Space, Key.space);
    bool GetKeyDown_Period()=> GetKeyDown(KeyCode.Period, Key.period);

    bool GetKeyDown(KeyCode legacy, Key modern)
    {
        #if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            var kc = kb[modern];
            if (kc != null && kc.wasPressedThisFrame) return true;
        }
        #else
        if (UnityEngine.Input.GetKeyDown(legacy)) return true;
        #endif
        return false;
    }

    Vector2 GetMove2D()
    {
        #if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        float x = 0, y = 0;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    y += 1f;
        }
        return new Vector2(x, y);
        #else
        return new Vector2(UnityEngine.Input.GetAxisRaw("Horizontal"),
                           UnityEngine.Input.GetAxisRaw("Vertical"));
        #endif
    }

    float GetScrollNotches()
    {
        #if ENABLE_INPUT_SYSTEM
        var m = Mouse.current;
        if (m == null) return 0f;
        // Input System typically reports 120 per notch on Windows.
        return (m.scroll.ReadValue().y) / 120f;
        #else
        return UnityEngine.Input.mouseScrollDelta.y;
        #endif
    }

    // ---- Pause / Step ----
    void TogglePause()
    {
        if (!_paused) { _savedDt = sim.dt; sim.dt = 0f; _paused = true; }
        else          { sim.dt = Mathf.Max(0.01f, _savedDt); _paused = false; }
    }

    void StepOnce()
    {
        float old = sim.dt;
        sim.dt = Mathf.Max(0.01f, _savedDt > 0f ? _savedDt : 0.1f);
        sim.Step();
        sim.dt = 0f;
    }

    // ------- Seeding (seeds BOTH ping-pong buffers if possible) -------
    public void Reseed()
    {
        if (!sim || sim.channels < 1) return;

        float sigmaPx = Mathf.Max(1f, sigmaNorm * Mathf.Min(sim.width, sim.height));

        MethodInfo clear    = sim.GetType().GetMethod("ClearState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo seedBoth = sim.GetType().GetMethod("SeedBoth",   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (clear != null && seedBoth != null)
        {
            clear.Invoke(sim, new object[] { 0f });
            seedBoth.Invoke(sim, new object[] { seedCount, sigmaPx, startMass, true });
            return;
        }

        // Fallback: try to touch hidden ping-pong arrays
        var fA   = sim.GetType().GetField("_stateA", BindingFlags.Instance | BindingFlags.NonPublic);
        var fB   = sim.GetType().GetField("_stateB", BindingFlags.Instance | BindingFlags.NonPublic);
        var fPng = sim.GetType().GetField("_pong",   BindingFlags.Instance | BindingFlags.NonPublic);

        RenderTexture[] A = fA != null ? (RenderTexture[])fA.GetValue(sim) : null;
        RenderTexture[] B = fB != null ? (RenderTexture[])fB.GetValue(sim) : null;

        if (A != null && B != null)
        {
            for (int c = 0; c < sim.channels; c++)
            {
                if (A[c]) Graphics.Blit(Texture2D.blackTexture, A[c]);
                if (B[c]) Graphics.Blit(Texture2D.blackTexture, B[c]);
            }
            for (int k = 0; k < Mathf.Max(1, seedCount); k++)
            {
                float nx = (centerFirst && k == 0) ? 0.5f : UnityEngine.Random.value;
                float ny = (centerFirst && k == 0) ? 0.5f : UnityEngine.Random.value;
                for (int c = 0; c < sim.channels; c++)
                {
                    if (A[c]) SeedGaussian(A[c], nx, ny, sigmaPx, startMass);
                    if (B[c]) SeedGaussian(B[c], nx, ny, sigmaPx, startMass);
                }
            }
            if (fPng != null) fPng.SetValue(sim, false);
            return;
        }

        var tex = sim.CurrentTexture(0);
        if (tex) SeedGaussian(tex, 0.5f, 0.5f, sigmaPx, startMass);
    }

    static void SeedGaussian(RenderTexture rt, float nx, float ny, float sigmaPx, float amp)
    {
        if (!rt) return;

        var tmp = RenderTexture.active;
        RenderTexture.active = rt;

        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RFloat, false, true);
        var data = tex.GetRawTextureData<float>();

        float cx = nx * rt.width, cy = ny * rt.height;
        float twoSigma2 = 2f * sigmaPx * sigmaPx;

        for (int y = 0; y < rt.height; y++)
        {
            int row = y * rt.width;
            float dy = y - cy; float dy2 = dy * dy;
            for (int x = 0; x < rt.width; x++)
            {
                float dx = x - cx;
                data[row + x] = amp * Mathf.Exp(-(dx*dx + dy2) / twoSigma2);
            }
        }

        tex.LoadRawTextureData(data); tex.Apply(false);
        Graphics.Blit(tex, rt);
        UnityEngine.Object.DestroyImmediate(tex);
        RenderTexture.active = tmp;
    }
}
