using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using BM = global::BoundaryMode;

[DisallowMultipleComponent]
[RequireComponent(typeof(FlowLeniaSimulation))]
[RequireComponent(typeof(FlowLeniaDisplay))]
public class FlowLeniaControl : MonoBehaviour
{
    public FlowLeniaSimulation sim;
    public FlowLeniaDisplay    display;

    [Header("View Controls")]
    [Tooltip("Pan speed in UV units/sec at zoom = 1")]
    public float panSpeed = 0.6f;
    [Tooltip("Scroll-wheel multiplier per notch (>1 zoom in)")]
    public float zoomStep = 1.12f;
    public float minZoom = 0.25f, maxZoom = 12f;

    [Header("Seeding (Starting Mass)")]
    [Range(1, 32)]     public int   seedCount  = 3;
    [Range(0.01f,.5f)] public float sigmaNorm  = 0.08f;  // fraction of min(width,height)
    [Range(0f,   5f)]  public float startMass  = 1.0f;
    public bool centerFirst = true;

    float _savedDt = 1f;
    bool  _paused  = false;

    // mouse-drag pan
    bool _dragging;
    Vector2 _dragStartMouse, _dragStartPan;

    void Awake()
    {
        if (!sim)     sim     = GetComponent<FlowLeniaSimulation>() ?? GetComponentInParent<FlowLeniaSimulation>();
        if (!display) display = GetComponent<FlowLeniaDisplay>()    ?? GetComponentInParent<FlowLeniaDisplay>();
    }

    void Update()
    {
        if (!sim || !display) return;

        // ----- Pause / step -----
        if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
        if (_paused && Input.GetKeyDown(KeyCode.Period)) StepOnce();

        // ----- Pan: WASD / arrows -----
        float z   = Mathf.Max(0.05f, display.zoom);
        float spd = panSpeed / z;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    display.pan += Vector2.up    * spd * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  display.pan += Vector2.down  * spd * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  display.pan += Vector2.left  * spd * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) display.pan += Vector2.right * spd * Time.unscaledDeltaTime;

        // ----- Pan: middle-mouse drag -----
        if (Input.GetMouseButtonDown(2))
        {
            _dragging = true;
            _dragStartMouse = (Vector2)Input.mousePosition;
            _dragStartPan   = display.pan;
        }
        if (Input.GetMouseButtonUp(2)) _dragging = false;
        if (_dragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - _dragStartMouse;
            // screen pixels -> UV-ish; 1000 px ≈ 1 UV at zoom=1 (tweak if you like)
            display.pan = _dragStartPan + (delta / (1000f * z));
        }

        // ----- Zoom: mouse wheel + Q/E -----
        float scroll = Input.mouseScrollDelta.y;
        if (Input.GetKey(KeyCode.Q)) scroll +=  1f * Time.unscaledDeltaTime * 5f;
        if (Input.GetKey(KeyCode.E)) scroll += -1f * Time.unscaledDeltaTime * 5f;

        if (Mathf.Abs(scroll) > 0f)
        {
            float newZoom = display.zoom * Mathf.Pow(zoomStep, scroll);
            display.zoom = Mathf.Clamp(newZoom, minZoom, maxZoom);
        }

        // ----- Parameter nudges -----
        if (Input.GetKeyDown(KeyCode.LeftBracket))  sim.temperature = Mathf.Max(0f, sim.temperature - 0.05f);
        if (Input.GetKeyDown(KeyCode.RightBracket)) sim.temperature += 0.05f;
        if (Input.GetKeyDown(KeyCode.Minus))        sim.dt = Mathf.Max(0.01f, sim.dt - 0.05f);
        if (Input.GetKeyDown(KeyCode.Equals))       sim.dt += 0.05f;

        // ----- Cycle boundary -----
        if (Input.GetKeyDown(KeyCode.B))
            sim.boundary = (BM)(((int)sim.boundary + 1) % 3);

        // ----- Toggle mass diagnostics -----
        if (Input.GetKeyDown(KeyCode.M))
            sim.computeMassEachFrame = !sim.computeMassEachFrame;

        // ----- Reseed presets -----
        if (Input.GetKeyDown(KeyCode.Alpha1)) { startMass = 0.5f; sigmaNorm = 0.05f; seedCount = 2; Reseed(); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { startMass = 1.0f; sigmaNorm = 0.08f; seedCount = 3; Reseed(); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { startMass = 2.0f; sigmaNorm = 0.12f; seedCount = 5; Reseed(); }
        if (Input.GetKeyDown(KeyCode.R))      { Reseed(); }

        // ----- Reset view -----
        if (Input.GetKeyDown(KeyCode.Home))
        {
            display.pan  = Vector2.zero;
            display.zoom = 1f;
        }
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

    // ------- Seeding -------
    public void Reseed()
    {
        if (!sim || sim.channels < 1) return;

        float sigmaPx = Mathf.Max(1f, sigmaNorm * Mathf.Min(sim.width, sim.height));

        // Prefer public helpers if present
        var clear    = sim.GetType().GetMethod("ClearState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var seedBoth = sim.GetType().GetMethod("SeedBoth",   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (clear != null && seedBoth != null)
        {
            clear.Invoke(sim, new object[] { 0f });
            seedBoth.Invoke(sim, new object[] { seedCount, sigmaPx, startMass, true });
            return;
        }

        // Fallback: seed current buffer
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
