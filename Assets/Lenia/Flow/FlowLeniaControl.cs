using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;   // New Input System
using BM = global::BoundaryMode;

[DisallowMultipleComponent]
[RequireComponent(typeof(FlowLeniaSimulation))]
[RequireComponent(typeof(FlowLeniaDisplay))]
public class FlowLeniaControl : MonoBehaviour
{
    public FlowLeniaSimulation sim;
    public FlowLeniaDisplay    display;

    [Header("Presets")]
    public FlowLeniaPresetLibrary presetLibrary;
    public bool reseedOnPresetApply = true;
    [Min(0)] public int currentPresetIndex = 0;

    [Header("View Controls")]
    public float panSpeed = 0.6f;
    public float zoomStep = 1.12f;
    public float minZoom = 0.25f, maxZoom = 12f;

    [Header("Seeding (Starting Mass)")]
    [Range(1, 32)]   public int   seedCount  = 3;
    [Range(0.01f, .5f)] public float sigmaNorm = 0.08f; // fraction of min(width,height)
    [Range(0f, 5f)]  public float startMass  = 1.0f;
    public bool centerFirst = true;

    float _savedDt = 1f;
    bool  _paused  = false;

    void Awake()
    {
        if (!sim)     sim     = GetComponent<FlowLeniaSimulation>();
        if (!display) display = GetComponent<FlowLeniaDisplay>();
    }

    void OnEnable()
    {
        // Auto-apply initial preset if provided
        if (presetLibrary != null && presetLibrary.Count > 0)
        {
            currentPresetIndex = Mathf.Clamp(currentPresetIndex, 0, presetLibrary.Count - 1);
            ApplyPresetIndex(currentPresetIndex);
        }
    }

    void OnValidate()
    {
        if (display)
        {
            display.zoom = Mathf.Clamp(display.zoom, Mathf.Max(0.05f, minZoom), Mathf.Max(minZoom, maxZoom));
        }
    }

    void Update()
    {
        if (!sim || !display) return;

        var kb = Keyboard.current;
        var ms = Mouse.current;
        if (kb == null) return;

        // Pause / single-step
        if (kb.spaceKey.wasPressedThisFrame) TogglePause();
        if (_paused && kb.periodKey.wasPressedThisFrame) StepOnce();

        // Preset cycling: , and .  ('.' steps when paused, cycles when running)
        if (kb.commaKey.wasPressedThisFrame) CyclePreset(-1);
        if (!_paused && kb.periodKey.wasPressedThisFrame) CyclePreset(+1);

        // Direct preset: F1..F9
        if (presetLibrary != null && presetLibrary.Count > 0)
        {
            if (kb.f1Key.wasPressedThisFrame) ApplyPresetIndex(0);
            if (kb.f2Key.wasPressedThisFrame) ApplyPresetIndex(1);
            if (kb.f3Key.wasPressedThisFrame) ApplyPresetIndex(2);
            if (kb.f4Key.wasPressedThisFrame) ApplyPresetIndex(3);
            if (kb.f5Key.wasPressedThisFrame) ApplyPresetIndex(4);
            if (kb.f6Key.wasPressedThisFrame) ApplyPresetIndex(5);
            if (kb.f7Key.wasPressedThisFrame) ApplyPresetIndex(6);
            if (kb.f8Key.wasPressedThisFrame) ApplyPresetIndex(7);
            if (kb.f9Key.wasPressedThisFrame) ApplyPresetIndex(8);
        }

        // Pan (WASD / arrows)
        Vector2 move = Vector2.zero;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  move.x -= 1;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    move.y += 1;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  move.y -= 1;
        if (move.sqrMagnitude > 0f)
        {
            float z = Mathf.Max(0.05f, display.zoom);
            display.pan += (move * panSpeed * Time.unscaledDeltaTime) / z;
        }

        // Zoom (mouse wheel)
        if (ms != null)
        {
            float scroll = ms.scroll.ReadValue().y;  // ~120 per notch on Windows
            if (Mathf.Abs(scroll) > 0f)
            {
                float steps = scroll / 120f;
                float z = display.zoom * Mathf.Pow(zoomStep, steps);
                display.zoom = Mathf.Clamp(z, minZoom, maxZoom);
            }
        }

        // Parameter nudges
        if (kb.leftBracketKey.wasPressedThisFrame)  sim.temperature = Mathf.Max(0f, sim.temperature - 0.05f);
        if (kb.rightBracketKey.wasPressedThisFrame) sim.temperature += 0.05f;
        if (kb.minusKey.wasPressedThisFrame)        sim.dt = Mathf.Max(0.01f, sim.dt - 0.05f);
        if (kb.equalsKey.wasPressedThisFrame)       sim.dt += 0.05f;

        // Cycle boundary mode
        if (kb.bKey.wasPressedThisFrame)
            sim.boundary = (BM)(((int)sim.boundary + 1) % 3);

        // Toggle mass diagnostics
        if (kb.mKey.wasPressedThisFrame)
            sim.computeMassEachFrame = !sim.computeMassEachFrame;

        // Reseed hotkeys (starting mass presets)
        if (kb.digit1Key.wasPressedThisFrame) { startMass = 0.5f; sigmaNorm = 0.05f; seedCount = 2; Reseed(); }
        if (kb.digit2Key.wasPressedThisFrame) { startMass = 1.0f; sigmaNorm = 0.08f; seedCount = 3; Reseed(); }
        if (kb.digit3Key.wasPressedThisFrame) { startMass = 2.0f; sigmaNorm = 0.12f; seedCount = 5; Reseed(); }
        if (kb.rKey.wasPressedThisFrame)      { Reseed(); }

        // Reset view
        if (kb.homeKey.wasPressedThisFrame)
        {
            display.pan  = Vector2.zero;
            display.zoom = 1f;
        }
    }

    void CyclePreset(int delta)
    {
        if (presetLibrary == null || presetLibrary.Count == 0) return;
        int n = presetLibrary.Count;
        currentPresetIndex = (currentPresetIndex + delta % n + n) % n;
        ApplyPresetIndex(currentPresetIndex);
    }

    public void ApplyPresetIndex(int i)
    {
        if (presetLibrary == null || presetLibrary.Count == 0) return;
        i = Mathf.Clamp(i, 0, presetLibrary.Count - 1);
        presetLibrary.ApplyPreset(i, sim, display);
        if (reseedOnPresetApply) Reseed();
        currentPresetIndex = i;
    }

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

        // Prefer dedicated public helpers if present
        var clear    = sim.GetType().GetMethod("ClearState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var seedBoth = sim.GetType().GetMethod("SeedBoth",   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (clear != null && seedBoth != null)
        {
            clear.Invoke(sim, new object[] { 0f });
            seedBoth.Invoke(sim, new object[] { seedCount, sigmaPx, startMass, centerFirst });
            return;
        }

        // Fallback: seed the currently visible buffer only
        var tex = sim.CurrentTexture(0);
        if (tex) SeedGaussian(tex, 0.5f, 0.5f, sigmaPx, startMass);
    }

    static void SeedGaussian(RenderTexture rt, float nx, float ny, float sigmaPx, float amp)
    {
        if (!rt) return;

        var tmp = RenderTexture.active;
        RenderTexture.active = rt;

        var tex  = new Texture2D(rt.width, rt.height, TextureFormat.RFloat, false, true);
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
