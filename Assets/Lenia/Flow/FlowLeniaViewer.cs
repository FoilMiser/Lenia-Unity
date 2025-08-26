using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class FlowLeniaViewer : MonoBehaviour
{
    public FlowLeniaSimulation sim;
    public RawImage target;
    public bool showTestCardIfNull = true;

    Texture2D _testCard;
    bool _loggedBound, _loggedNull;

    void Awake()      { TryBind(); }
    void OnEnable()   { TryBind(); }
    void OnValidate() { TryBind(); }

    void TryBind()
    {
        if (target == null) target = GetComponent<RawImage>();
        if (sim == null)    sim    = Object.FindFirstObjectByType<FlowLeniaSimulation>();
    }

    void EnsureTestCard()
    {
        if (_testCard != null) return;
        const int N = 8;
        _testCard = new Texture2D(N, N, TextureFormat.RGBA32, false, true);
        for (int y = 0; y < N; y++)
        for (int x = 0; x < N; x++)
        {
            bool a = (((x / 2) + (y / 2)) % 2) == 0;
            _testCard.SetPixel(x, y, a ? new Color(0.15f,0.15f,0.15f,1f) : new Color(0.85f,0.85f,0.85f,1f));
        }
        _testCard.Apply(false);
    }

    [ContextMenu("Bind Now (Viewer)")]
    public void BindNow()
    {
        TryBind();
        if (target == null || sim == null) return;

        sim.EnsureInitialized();
        var tex = sim.CurrentTexture;

        if (tex != null)
        {
            target.texture = tex;
            if (!_loggedBound) { Debug.Log("[Viewer] Bound sim RT " + tex.width + "x" + tex.height); _loggedBound = true; }
        }
        else if (showTestCardIfNull)
        {
            EnsureTestCard();
            target.texture = _testCard;
            if (!_loggedNull) { Debug.LogWarning("[Viewer] Sim RT was null. Showing test card."); _loggedNull = true; }
        }
    }

    void Update()
    {
        TryBind();
        if (target == null || sim == null) return;

        sim.EnsureInitialized();
        var tex = sim.CurrentTexture;

        if (tex != null)
        {
            if (target.texture != tex) target.texture = tex;
            _loggedNull = false;
        }
        else if (showTestCardIfNull)
        {
            EnsureTestCard();
            if (target.texture != _testCard) target.texture = _testCard;
            if (!_loggedNull) { Debug.LogWarning("[Viewer] sim.CurrentTexture is NULL (showing checker)."); _loggedNull = true; }
        }
    }
}
