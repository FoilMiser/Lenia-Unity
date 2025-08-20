using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
[RequireComponent(typeof(RectTransform))]
public class UIZoomPan : MonoBehaviour
{
    public float zoomSpeed = 0.1f, minZoom = 0.5f, maxZoom = 5f;
    public float panSpeed  = 1.0f;
    RectTransform rt; Vector2 lastMouse;
    void Awake(){ rt = GetComponent<RectTransform>(); }
    void Update(){
        #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var m = Mouse.current; if(m!=null){
            float scroll = m.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f){
                float s = rt.localScale.x * Mathf.Exp(scroll * 0.001f * (zoomSpeed*100f));
                s = Mathf.Clamp(s, minZoom, maxZoom);
                rt.localScale = new Vector3(s,s,1);
            }
            if (m.middleButton.isPressed){
                var delta = (Vector2)m.position.ReadValue() - lastMouse;
                rt.anchoredPosition += delta * panSpeed;
            }
            lastMouse = m.position.ReadValue();
        }
        #else
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f){
            float s = rt.localScale.x * Mathf.Exp(scroll * 0.1f * zoomSpeed);
            s = Mathf.Clamp(s, minZoom, maxZoom);
            rt.localScale = new Vector3(s,s,1);
        }
        if (Input.GetMouseButton(2)){
            Vector2 delta = (Vector2)Input.mousePosition - lastMouse;
            rt.anchoredPosition += delta * panSpeed;
        }
        lastMouse = Input.mousePosition;
        #endif
    }
}

