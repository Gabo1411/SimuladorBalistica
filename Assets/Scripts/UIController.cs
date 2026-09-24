using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla toda la interfaz de usuario del simulador:
/// - Sliders de ángulo y fuerza
/// - Dropdown de masa del proyectil
/// - Botones Disparar / Reiniciar
/// - Panel de resultado con el reporte de tiro
/// - Actualización del predictor de trayectoria en tiempo real
/// </summary>
public class UIController : MonoBehaviour
{
    // ── Controles de disparo ───────────────────────────────────────────────────
    [Header("Controles de Disparo")]
    public Slider       AngleSlider;
    public Slider       ForceSlider;
    public TMP_Dropdown MassDropdown;
    public Button       FireButton;
    public Button       ResetButton;

    [Header("Etiquetas de Valor")]
    public TMP_Text  AngleValueLabel;
    public TMP_Text  ForceValueLabel;
    public TMP_Text  MassValueLabel;

    [Header("Panel de Resultado")]
    public GameObject ResultPanel;
    public TMP_Text   ResultText;
    public TMP_Text   ScoreText;
    public TMP_Text   TotalScoreText;

    [Header("HUD Durante Vuelo")]
    public TMP_Text   FlightTimeLabel;
    public GameObject AimingPanel;

    [Header("Trayectoria")]
    public TrajectoryPredictor TrajectoryPredictor;

    // ── Masas disponibles ──────────────────────────────────────────────────────
    private readonly float[]  _massOptions = { 0.5f, 1f, 3f };
    private readonly string[] _massLabels  = { "Ligero (0.5 kg)", "Mediano (1 kg)", "Pesado (3 kg)" };

    // ── Estado interno ─────────────────────────────────────────────────────────
    private bool  _isInFlight;
    private float _flightStartTime;

    // ──────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        SetupSliders();
        SetupDropdown();
        SetupButtons();

        ResultPanel?.SetActive(false);
        AimingPanel?.SetActive(true);

        OnAngleChanged(AngleSlider != null ? AngleSlider.value : 45f);
        OnForceChanged(ForceSlider != null ? ForceSlider.value : 50f);
        OnMassChanged(0);
    }

    private void Update()
    {
        if (_isInFlight && FlightTimeLabel != null)
        {
            float elapsed = Time.time - _flightStartTime;
            FlightTimeLabel.text = $"Tiempo: {elapsed:F1}s";
        }
    }

    // ── Setup ──────────────────────────────────────────────────────────────────

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
        MassDropdown.value = 1;
        MassDropdown.onValueChanged.AddListener(OnMassChanged);
    }

    private void SetupButtons()
    {
        FireButton?.onClick.AddListener(OnFireButtonPressed);
        ResetButton?.onClick.AddListener(OnResetButtonPressed);
    }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    private void OnAngleChanged(float value)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AngleDegrees = value;
        if (AngleValueLabel != null)
            AngleValueLabel.text = $"{value:F1}°";

        // Rotar el cañón en tiempo real
        GameManager.Instance?.Launcher?.UpdateBarrelRotation(value);

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

        _isInFlight      = true;
        _flightStartTime = Time.time;

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

    public void SetFireButtonInteractable(bool interactable)
    {
        if (FireButton != null) FireButton.interactable = interactable;
    }

    public void ShowResultPanel(ShotResult result)
    {
        _isInFlight = false;
        if (FlightTimeLabel != null) FlightTimeLabel.gameObject.SetActive(false);
        AimingPanel?.SetActive(true);

        if (result == null) return;

        if (ResultText     != null) ResultText.text  = result.Report;
        if (ScoreText      != null) ScoreText.text   = $"{result.Score:F0} pts";
        if (TotalScoreText != null)
        {
            float total = GameManager.Instance?.ResultManager?.TotalScore() ?? 0f;
            TotalScoreText.text = $"Total: {total:F0} pts";
        }

        ResultPanel?.SetActive(true);
        StartCoroutine(ScalePanel(ResultPanel.transform, Vector3.one * 0.8f, Vector3.one, 0.25f));
    }

    public void HideResultPanel()
    {
        ResultPanel?.SetActive(false);
        AimingPanel?.SetActive(true);
        TrajectoryPredictor?.ShowTrajectory();
    }

    // ── Animación ──────────────────────────────────────────────────────────────

    private System.Collections.IEnumerator ScalePanel(Transform t, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed      += Time.deltaTime;
            t.localScale  = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        t.localScale = to;
    }
}
