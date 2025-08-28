using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(FlowLeniaSimulation))]
public class FlowLeniaDisplay : MonoBehaviour
{
    public FlowLeniaSimulation sim;
    public RawImage view;

    [Header("Palette")]
    [Tooltip("If assigned, we render with a palette shader. If left empty, we fall back to raw grayscale.")]
    public Gradient palette;
    [Range(0.1f, 4f)] public float exposure = 1f;

    Material _matPalette;   // Hidden/Lenia/PaletteBlit
    Material _matRaw;       // Unlit/Texture fallback
    Texture2D _lut;

    void Awake()
    {
        if (sim == null) sim = GetComponent<FlowLeniaSimulation>();
        EnsureUI();
        EnsureMaterials();
        BuildLUTIfNeeded();
        // Do not Apply here; wait until Start (sim has allocated)
    }

    void Start()
    {
        SafeApply();
    }

    void OnValidate()
    {
        if (!Application.isPlaying) return;
        BuildLUTIfNeeded();
        SafeApply();
    }

    void EnsureUI()
    {
        if (view != null) return;

        // If you already have a RawImage in the scene, assign it in the Inspector.
        // Otherwise, create one full-screen.
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var canvasGO = new GameObject("LeniaViewCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = canvasGO.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvas = c;
        }

        var imageGO = new GameObject("Display", typeof(RawImage));
        imageGO.transform.SetParent(canvas.transform, false);
        view = imageGO.GetComponent<RawImage>();
        view.rectTransform.anchorMin = Vector2.zero;
        view.rectTransform.anchorMax = Vector2.one;
        view.rectTransform.offsetMin = Vector2.zero;
        view.rectTransform.offsetMax = Vector2.zero;
        view.color = Color.white;
    }

    void EnsureMaterials()
    {
        if (_matRaw == null)
            _matRaw = new Material(Shader.Find("Unlit/Texture"));

        if (_matPalette == null)
        {
            var sh = Shader.Find("Hidden/Lenia/PaletteBlit");
            if (sh != null) _matPalette = new Material(sh);
        }
    }

    void BuildLUTIfNeeded()
    {
        // If no gradient, clear LUT so we fall back to raw rendering.
        if (palette == null) { _lut = null; return; }

        const int N = 256;
        if (_lut == null || _lut.width != N)
        {
            _lut = new Texture2D(N, 1, TextureFormat.RGBA32, false, true)
            {
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

    void SafeApply()
    {
        if (view == null || sim == null) return;

        var tex = sim.CurrentTexture(0);
        if (tex == null)
        {
            // sim not ready yet; show black
            view.material = _matRaw;
            view.texture  = Texture2D.blackTexture;
            return;
        }

        // Choose material: palette if we have both shader & LUT; otherwise raw.
        bool usePalette = (_matPalette != null && _lut != null);
        if (usePalette)
        {
            view.material = _matPalette;
            view.texture  = tex;                     // RawImage still needs a texture
            _matPalette.SetTexture("_MainTex", tex); // shader reads _MainTex explicitly
            _matPalette.SetTexture("_LUT", _lut);
            _matPalette.SetFloat("_Exposure", exposure);
        }
        else
        {
            view.material = _matRaw;
            view.texture  = tex; // grayscale
        }
    }

    void Update()
    {
        SafeApply();
    }
}
