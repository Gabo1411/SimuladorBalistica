using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Convierte cualquier panel de UI en un menú desplegable.
/// Estructura esperada:
///   PanelRaíz (este GameObject)
///   ├── HeaderButton  (Button — siempre visible)
///   └── Content       (GameObject con los controles — se oculta/muestra)
/// </summary>
public class CollapsiblePanel : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Botón de cabecera que activa el toggle.")]
    public Button HeaderButton;

    [Tooltip("Contenedor con los controles internos del panel.")]
    public GameObject Content;

    [Tooltip("Texto del botón de cabecera.")]
    public TMP_Text HeaderLabel;

    [Header("Textos")]
    [Tooltip("Título cuando el panel está abierto.")]
    public string TitleOpen   = "▲ Controles";

    [Tooltip("Título cuando el panel está cerrado.")]
    public string TitleClosed = "▼ Controles";

    [Header("Estado inicial")]
    public bool StartsOpen = true;

    // ── Estado ─────────────────────────────────────────────────────────────────
    private bool _isOpen;

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (HeaderButton != null)
            HeaderButton.onClick.AddListener(Toggle);
    }

    private void Start()
    {
        // Aplicar estado inicial sin animación
        SetOpen(StartsOpen, animate: false);
    }

    // ── API Pública ────────────────────────────────────────────────────────────

    public void Toggle() => SetOpen(!_isOpen);

    public void SetOpen(bool open, bool animate = true)
    {
        _isOpen = open;

        if (Content != null)
            Content.SetActive(_isOpen);

        if (HeaderLabel != null)
            HeaderLabel.text = _isOpen ? TitleOpen : TitleClosed;

        if (animate)
            StartCoroutine(AnimateHeader());
    }

    public bool IsOpen => _isOpen;

    // ── Animación del botón ────────────────────────────────────────────────────

    private System.Collections.IEnumerator AnimateHeader()
    {
        if (HeaderButton == null) yield break;

        Transform t    = HeaderButton.transform;
        float     time = 0f;
        float     dur  = 0.1f;
        Vector3   from = _isOpen ? Vector3.one * 0.9f : Vector3.one;
        Vector3   to   = Vector3.one;

        while (time < dur)
        {
            time       += Time.deltaTime;
            t.localScale = Vector3.Lerp(from, to, time / dur);
            yield return null;
        }
        t.localScale = to;
    }
}
