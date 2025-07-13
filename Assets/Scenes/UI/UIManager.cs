using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    
    [Header("UI References")]
    public Text[] tensionDisplays;
    public Slider[] tensionGauges;
    public Text warningText;
    public Image warningPanel;
    public Button autoControlToggle;
    public Slider windStrengthSlider;
    public Slider waveHeightSlider;
    
    [Header("Alert Settings")]
    public AudioSource alertSound;
    public Color normalColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color criticalColor = Color.red;
    
    private MooringManager mooringManager;
    
    void Awake()
    {
        Instance = this;
    }
    
    void Start()
    {
        mooringManager = MooringManager.Instance;
        SetupUI();
    }
    
    void SetupUI()
    {
        // 자동 제어 토글 설정
        autoControlToggle.onClick.AddListener(() => {
            mooringManager.autoControlEnabled = !mooringManager.autoControlEnabled;
            UpdateToggleText();
        });
        
        // 환경 제어 슬라이더 설정
        windStrengthSlider.onValueChanged.AddListener(value => {
            var envForces = FindObjectOfType<EnvironmentalForces>();
            if (envForces) envForces.windStrength = value;
        });
        
        waveHeightSlider.onValueChanged.AddListener(value => {
            var envForces = FindObjectOfType<EnvironmentalForces>();
            if (envForces) envForces.waveHeight = value;
        });
    }
    
    void Update()
    {
        UpdateTensionDisplays();
    }
    
    void UpdateTensionDisplays()
    {
        for (int i = 0; i < mooringManager.mooringLines.Count && i < tensionDisplays.Length; i++)
        {
            var line = mooringManager.mooringLines[i];
            
            if (line.isBroken)
            {
                tensionDisplays[i].text = $"계류줄 {i+1}: 파단됨";
                tensionDisplays[i].color = Color.red;
                tensionGauges[i].value = 0;
            }
            else
            {
                tensionDisplays[i].text = $"계류줄 {i+1}: {line.currentTension:F0}N";
                
                float ratio = line.currentTension / line.breakingTension;
                tensionGauges[i].value = ratio;
                
                // 색상 설정
                if (ratio > 0.9f)
                    tensionDisplays[i].color = criticalColor;
                else if (ratio > 0.7f)
                    tensionDisplays[i].color = warningColor;
                else
                    tensionDisplays[i].color = normalColor;
            }
        }
    }
    
    public void ShowWarningAlert(MooringLine line)
    {
        warningText.text = $"경고: {line.name} 장력 증가 감지";
        warningPanel.color = warningColor;
        StartCoroutine(FlashWarning());
    }
    
    public void ShowCriticalAlert(MooringLine line)
    {
        warningText.text = $"위험: {line.name} 임계 장력 초과!";
        warningPanel.color = criticalColor;
        alertSound.Play();
        StartCoroutine(FlashWarning());
    }
    
    public void ShowBreakageAlert(MooringLine line)
    {
        warningText.text = $"계류줄 파단 발생: {line.name}";
        warningPanel.color = Color.red;
        alertSound.Play();
    }
    
    System.Collections.IEnumerator FlashWarning()
    {
        for (int i = 0; i < 3; i++)
        {
            warningPanel.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            warningPanel.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    void UpdateToggleText()
    {
        var text = autoControlToggle.GetComponentInChildren<Text>();
        text.text = mooringManager.autoControlEnabled ? "자동제어: ON" : "자동제어: OFF";
    }
}