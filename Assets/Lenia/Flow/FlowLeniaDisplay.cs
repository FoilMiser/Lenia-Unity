using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(FlowLeniaSimulation))]
public class FlowLeniaDisplay : MonoBehaviour
{
    public FlowLeniaSimulation sim;
    public RawImage view;

    [Header("Display")]
    [Tooltip("Which channel to visualize (0-based).")]
    public int channel = 0;

    [Header("Palette")]
    [Tooltip("If assigned and the palette shader is found, renders with a LUT; otherwise uses grayscale.")]
    public Gradient palette;
    [Range(0.1f, 4f)] public float exposure = 1f;

    [Header("Pan/Zoom (requires shader props _Pan/_Zoom)")]
    public Vector2 pan = Vector2.zero;
    [Min(0.05f)] public float zoom = 1f;

    [Header("Debug")]
    public bool showOverlay = false;

    // Materials & LUT
    Material  _matPalette;   // Hidden/Lenia/PaletteBlit
    Material  _matRaw;       // Unlit/Texture fallback
    Texture2D _lut;          // 256x1 palette strip

    // Shader property IDs
    static readonly int ID_MainTex  = Shader.PropertyToID("_MainTex");
    static readonly int ID_LUT      = Shader.PropertyToID("_LUT");
    static readonly int ID_Exposure = Shader.PropertyToID("_Exposure");
    static readonly int ID_Pan      = Shader.PropertyToID("_Pan");
    static readonly int ID_Zoom     = Shader.PropertyToID("_Zoom");

    // Caches to reduce redundant sets
    RenderTexture _lastTex;
    bool   _lastUsingPalette = false;
    float  _lastExposure = -1f;
    Vector2 _lastPan;
    float   _lastZoom = -1f;

    void Reset()
    {
        EnsureSim();
        EnsureUI();
        EnsureMaterials();
        BuildLUTIfNeeded();
    }

    void Awake()
    {
        EnsureSim();
        EnsureUI();
        EnsureMaterials();
        BuildLUTIfNeeded();
    }

    void Start() => SafeApply(force: true);

    void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        EnsureSim();
        EnsureMaterials();
        BuildLUTIfNeeded();
        if (Application.isPlaying) SafeApply(force: true);
    }

    void Update() => SafeApply();

    // -------- Helpers --------

    void EnsureSim()
    {
        if (sim) return;
        sim = GetComponent<FlowLeniaSimulation>() ?? GetComponentInParent<FlowLeniaSimulation>();
#if UNITY_2023_1_OR_NEWER
        if (!sim) sim = Object.FindFirstObjectByType<FlowLeniaSimulation>();
        if (!sim) sim = Object.FindAnyObjectByType<FlowLeniaSimulation>();
#else
#pragma warning disable 618
        if (!sim) sim = Object.FindObjectOfType<FlowLeniaSimulation>();
#pragma warning restore 618
#endif
    }

    void EnsureUI()
    {
        if (view) return;

        // Prefer an existing RawImage under this object
        view = GetComponentInChildren<RawImage>();
        if (view) return;

        // Create a simple overlay canvas + RawImage
        var canvas = GetComponentInChildren<Canvas>();
        if (!canvas)
        {
            var canvasGO = new GameObject("LeniaViewCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = canvasGO.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvas = c;
            canvasGO.transform.SetParent(transform, false);
        }

        var imageGO = new GameObject("Display", typeof(RawImage));
        imageGO.transform.SetParent(canvas.transform, false);
        view = imageGO.GetComponent<RawImage>();
        var rt = view.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        view.color = Color.white;
    }

    void EnsureMaterials()
    {
        if (_matRaw == null)
        {
            var sh = Shader.Find("Unlit/Texture");
            if (sh) _matRaw = new Material(sh) { hideFlags = HideFlags.DontSave };
        }
        if (_matPalette == null)
        {
            var sh = Shader.Find("Hidden/Lenia/PaletteBlit");
            if (sh) _matPalette = new Material(sh) { hideFlags = HideFlags.DontSave };
        }
    }

    void BuildLUTIfNeeded()
    {
        if (palette == null) { _lut = null; return; }

        const int N = 256;
        if (_lut == null || _lut.width != N)
        {
            _lut = new Texture2D(N, 1, TextureFormat.RGBA32, false, true)
            {
                name = "LeniaPaletteLUT",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }

        for (int i = 0; i < N; i++)
        {
            float t = i / (float)(N - 1);
            _lut.SetPixel(i, 0, palette.Evaluate(t));
        }
        _lut.Apply(false, false);
    }

    public void SafeApply(bool force = false)
    {
        if (!view) { EnsureUI(); if (!view) return; }
        if (!sim)  EnsureSim();
        EnsureMaterials();

        // If no sim yet, show black (not white) and keep trying.
        if (!sim)
        {
            if (_matRaw != null) view.material = _matRaw;
            view.texture  = Texture2D.blackTexture;
            _lastTex = null; _lastUsingPalette = false;
            return;
        }

        // Acquire the texture to show
        int ch = Mathf.Clamp(channel, 0, Mathf.Max(0, sim.channels - 1));
        var tex = sim.CurrentTexture(ch);

        // Sim not allocated yet → black fallback, try again next frame
        if (!tex)
        {
            if (_matRaw != null) view.material = _matRaw;
            view.texture  = Texture2D.blackTexture;
            _lastTex = null; _lastUsingPalette = false;
            return;
        }

        bool canPalette = (_matPalette != null && _lut != null && palette != null);
        bool usePalette = canPalette;

        if (force || usePalette != _lastUsingPalette || view.material == null)
        {
            view.material = usePalette ? _matPalette : _matRaw;
            _lastUsingPalette = usePalette;
            _lastTex = null;          // force rebind
            _lastExposure = -1f;
            _lastZoom = -1f;
            _lastPan = new Vector2(999, 999);
        }

        if (force || view.texture != tex)
        {
            // RawImage also wants its main texture set
            view.texture = tex;
            _lastTex = tex;
        }

        if (usePalette)
        {
            _matPalette.SetTexture(ID_MainTex, tex);
            _matPalette.SetTexture(ID_LUT, _lut);

            if (force || !Mathf.Approximately(exposure, _lastExposure))
            {
                _matPalette.SetFloat(ID_Exposure, Mathf.Max(0.1f, exposure));
                _lastExposure = exposure;
            }

            if (_matPalette.HasProperty(ID_Pan) && (force || pan != _lastPan))
            {
                _matPalette.SetVector(ID_Pan, pan);
                _lastPan = pan;
            }

            float z = Mathf.Max(0.05f, zoom);
            if (_matPalette.HasProperty(ID_Zoom) && (force || !Mathf.Approximately(z, _lastZoom)))
            {
                _matPalette.SetFloat(ID_Zoom, z);
                _lastZoom = z;
            }
        }
        else
        {
            // Grayscale fallback already handled by Unlit/Texture
            view.color = Color.white;
        }
    }

#if UNITY_EDITOR
    void OnGUI()
    {
        if (!Application.isPlaying || !showOverlay) return;
        var tex = sim ? sim.CurrentTexture(Mathf.Clamp(channel, 0, Mathf.Max(0, sim.channels - 1))) : null;
        string status = $"Sim={(sim ? "yes" : "no")}  " +
                        $"Tex={(tex ? $"{tex.width}x{tex.height}" : "NULL")}  " +
                        $"Mat={(view && view.material ? view.material.shader.name : "NULL")}  " +
                        $"Palette={(palette != null ? "yes" : "no")}";
        GUI.Label(new Rect(10, 10, 900, 22), status);
    }
#endif

    void OnDestroy()
    {
        if (Application.isPlaying) return; // let Unity clean up at runtime
        if (_matRaw)     DestroyImmediate(_matRaw);
        if (_matPalette) DestroyImmediate(_matPalette);
        if (_lut)        DestroyImmediate(_lut);
    }
}