using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla toda la interfaz de usuario del simulador:
/// - Sliders de ángulo y fuerza
/// - Dropdown de masa
/// - Botones Disparar / Reiniciar
/// - Panel de resultado con el reporte de tiro
/// - Actualización del predictor de trayectoria en tiempo real
/// </summary>
public class UIController : MonoBehaviour
{
    // ── Referencias UI ─────────────────────────────────────────────────────────
    [Header("Controles de Disparo")]
    public Slider    AngleSlider;
    public Slider    ForceSlider;
    public TMP_Dropdown MassDropdown;   // Opciones: Ligero (0.5), Mediano (1), Pesado (3)
    public Button    FireButton;
    public Button    ResetButton;

    [Header("Etiquetas de Valor")]
    public TMP_Text  AngleValueLabel;   // Muestra el valor actual del slider de ángulo
    public TMP_Text  ForceValueLabel;   // Muestra el valor actual del slider de fuerza
    public TMP_Text  MassValueLabel;    // Muestra la masa seleccionada

    [Header("Panel de Resultado")]
    public GameObject ResultPanel;      // Panel que se muestra al terminar el disparo
    public TMP_Text   ResultText;       // Texto del reporte de tiro
    public TMP_Text   ScoreText;        // Texto grande con la puntuación
    public TMP_Text   TotalScoreText;   // Puntuación acumulada total

    [Header("HUD Durante Vuelo")]
    public TMP_Text  FlightTimeLabel;   // Muestra el tiempo de vuelo en tiempo real
    public GameObject AimingPanel;      // Panel de controles (se oculta durante el vuelo)

    // ── Predictor de trayectoria ───────────────────────────────────────────────
    [Header("Trayectoria")]
    public TrajectoryPredictor TrajectoryPredictor;

    // ── Masas disponibles ──────────────────────────────────────────────────────
    private readonly float[] _massOptions = { 0.5f, 1f, 3f };
    private readonly string[] _massLabels = { "Ligero (0.5 kg)", "Mediano (1 kg)", "Pesado (3 kg)" };

    // ── Estado interno ─────────────────────────────────────────────────────────
    private bool  _isInFlight;
    private float _flightStartTime;

    // ──────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        SetupSliders();
        SetupDropdown();
        SetupButtons();

        // Estado inicial
        ResultPanel?.SetActive(false);
        AimingPanel?.SetActive(true);

        // Sincronizar valores iniciales con GameManager
        OnAngleChanged(AngleSlider.value);
        OnForceChanged(ForceSlider.value);
        OnMassChanged(0);
    }

    private void Update()
    {
        // Actualizar tiempo de vuelo en pantalla
        if (_isInFlight && FlightTimeLabel != null)
        {
            float elapsed = Time.time - _flightStartTime;
            FlightTimeLabel.text = $"Tiempo: {elapsed:F1}s";
        }
    }

    // ── Configuración de controles ─────────────────────────────────────────────

    private void SetupSliders()
    {
        if (AngleSlider != null)
        {
            AngleSlider.minValue = 0f;
            AngleSlider.maxValue = 90f;
            AngleSlider.value    = GameManager.Instance != null ? GameManager.Instance.AngleDegrees : 45f;
            AngleSlider.onValueChanged.AddListener(OnAngleChanged);
        }

        if (ForceSlider != null)
        {
            ForceSlider.minValue = 1f;
            ForceSlider.maxValue = 100f;
            ForceSlider.value    = GameManager.Instance != null ? GameManager.Instance.ForceMagnitude : 50f;
            ForceSlider.onValueChanged.AddListener(OnForceChanged);
        }
    }

    private void SetupDropdown()
    {
        if (MassDropdown == null) return;

        MassDropdown.ClearOptions();
        var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
        foreach (var label in _massLabels)
            options.Add(new TMP_Dropdown.OptionData(label));

        MassDropdown.AddOptions(options);
        MassDropdown.value = 1; // Mediano por defecto
        MassDropdown.onValueChanged.AddListener(OnMassChanged);
    }

    private void SetupButtons()
    {
        FireButton?.onClick.AddListener(OnFireButtonPressed);
        ResetButton?.onClick.AddListener(OnResetButtonPressed);
    }

    // ── Callbacks de controles ─────────────────────────────────────────────────

    private void OnAngleChanged(float value)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AngleDegrees = value;

        if (AngleValueLabel != null)
            AngleValueLabel.text = $"{value:F1}°";

        TrajectoryPredictor?.UpdateTrajectory();
    }

    private void OnForceChanged(float value)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ForceMagnitude = value;

        if (ForceValueLabel != null)
            ForceValueLabel.text = $"{value:F0}";

        TrajectoryPredictor?.UpdateTrajectory();
    }

    private void OnMassChanged(int index)
    {
        if (index < 0 || index >= _massOptions.Length) return;

        float mass = _massOptions[index];

        if (GameManager.Instance != null)
            GameManager.Instance.ProjectileMass = mass;

        if (MassValueLabel != null)
            MassValueLabel.text = $"{mass} kg";

        TrajectoryPredictor?.UpdateTrajectory();
    }

    private void OnFireButtonPressed()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.WaitingToShoot) return;

        _isInFlight = true;
        _flightStartTime = Time.time;

        // Ocultar panel de apuntado y predictor durante el vuelo
        AimingPanel?.SetActive(false);
        TrajectoryPredictor?.HideTrajectory();

        if (FlightTimeLabel != null) FlightTimeLabel.gameObject.SetActive(true);

        GameManager.Instance.RequestShot();
    }

    private void OnResetButtonPressed()
    {
        _isInFlight = false;
        GameManager.Instance?.ResetScene();
    }

    // ── API Pública ────────────────────────────────────────────────────────────

    /// <summary>Activa/desactiva el botón de disparo.</summary>
    public void SetFireButtonInteractable(bool interactable)
    {
        if (FireButton != null)
            FireButton.interactable = interactable;
    }

    /// <summary>Muestra el panel de resultado con los datos del último disparo.</summary>
    public void ShowResultPanel(ShotResult result)
    {
        _isInFlight = false;
        if (FlightTimeLabel != null) FlightTimeLabel.gameObject.SetActive(false);

        if (result == null) return;

        // Rellenar textos
        if (ResultText   != null) ResultText.text   = result.Report;
        if (ScoreText    != null) ScoreText.text     = $"{result.Score:F0} pts";
        if (TotalScoreText != null)
        {
            float total = GameManager.Instance?.ResultManager?.TotalScore() ?? 0f;
            TotalScoreText.text = $"Total: {total:F0} pts";
        }

        ResultPanel?.SetActive(true);

        // Animación de entrada (requiere Animator o se hace con código simple)
        AnimateResultPanel();
    }

    /// <summary>Oculta el panel de resultado.</summary>
    public void HideResultPanel()
    {
        ResultPanel?.SetActive(false);
        AimingPanel?.SetActive(true);
        TrajectoryPredictor?.ShowTrajectory();
    }

    // ── Animación simple del panel ─────────────────────────────────────────────

    private void AnimateResultPanel()
    {
        if (ResultPanel == null) return;

        // Escala desde 0.8 a 1 usando una corrutina simple
        StartCoroutine(ScalePanel(ResultPanel.transform, Vector3.one * 0.8f, Vector3.one, 0.25f));
    }

    private System.Collections.IEnumerator ScalePanel(Transform t, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            t.localScale = Vector3.Lerp(from, to, p);
            yield return null;
        }
        t.localScale = to;
    }
}
