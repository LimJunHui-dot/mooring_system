using UnityEngine;

public class SmartMooringMonitor : MonoBehaviour
{
    [Header("모니터링 설정")]
    public bool showInputForces = true;
    public bool showTensionData = true;
    public bool enableDetailedMode = true;
    public float logInterval = 1.0f;
    
    [Header("실시간 데이터 (Inspector 확인용)")]
    [SerializeField] private float bowTension = 0f;
    [SerializeField] private float sternTension = 0f;
    [SerializeField] private float portTension = 0f;
    [SerializeField] private float starboardTension = 0f;
    [SerializeField] private Vector3 currentInputForce = Vector3.zero;
    
    private MooringLine[] mooringLines;
    private float timer = 0f;
    private int logCount = 0;
    
    void Start()
    {
        mooringLines = FindObjectsOfType<MooringLine>();
        
        Debug.Log("=== 🚢 스마트 계류줄 통합 모니터링 시작 ===");
        Debug.Log($"감지된 계류줄: {mooringLines.Length}개");
        Debug.Log($"입력 힘 표시: {(showInputForces ? "ON" : "OFF")}");
        Debug.Log($"장력 데이터 표시: {(showTensionData ? "ON" : "OFF")}");
        Debug.Log("═══════════════════════════════════════════");
    }
    
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= logInterval)
        {
            UpdateAllData();
            LogIntegratedData();
            timer = 0f;
            logCount++;
        }
    }
    
    void UpdateAllData()
    {
        // 계류줄 장력 데이터 수집
        bowTension = GetTensionByName("Bow");
        sternTension = GetTensionByName("Stern");
        portTension = GetTensionByName("Port");
        starboardTension = GetTensionByName("Starboard");
        
        // 키보드 입력 힘 수집
        try
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            currentInputForce = new Vector3(h, 0, v) * 1000f;
        }
        catch
        {
            currentInputForce = Vector3.zero;
        }
    }
    
    void LogIntegratedData()
    {
        Debug.Log($"=== 📊 통합 모니터링 #{logCount} (시간: {Time.time:F1}초) ===");
        
        // 키보드 입력 힘 표시
        if (showInputForces)
        {
            if (currentInputForce.magnitude > 0.1f)
            {
                Debug.Log($"⌨️ 입력 힘: {currentInputForce} (크기: {currentInputForce.magnitude:F1}N)");
                LogInputAnalysis();
            }
            else
            {
                Debug.Log($"⌨️ 입력 힘: 없음");
            }
        }
        
        // 계류줄 장력 표시
        if (showTensionData)
        {
            Debug.Log($"🔗 선수줄: {bowTension:F0}N | 선미줄: {sternTension:F0}N | 좌현줄: {portTension:F0}N | 우현줄: {starboardTension:F0}N");
            
            float totalTension = bowTension + sternTension + portTension + starboardTension;
            Debug.Log($"📈 총 장력: {totalTension:F0}N | 평균: {totalTension/4:F0}N");
        }
        
        // 상세 분석
        if (enableDetailedMode)
        {
            CheckDangerousConditions();
        }
        
        Debug.Log("─────────────────────────────────────────");
    }
    
    void LogInputAnalysis()
    {
        // 입력 방향별 예상 영향 분석
        if (currentInputForce.z > 0.1f)
            Debug.Log($"  ↗️ 전진 입력 → 선수줄 장력 증가 예상");
        else if (currentInputForce.z < -0.1f)
            Debug.Log($"  ↙️ 후진 입력 → 선미줄 장력 증가 예상");
        
        if (currentInputForce.x > 0.1f)
            Debug.Log($"  ➡️ 우측 입력 → 우현줄 장력 증가 예상");
        else if (currentInputForce.x < -0.1f)
            Debug.Log($"  ⬅️ 좌측 입력 → 좌현줄 장력 증가 예상");
    }
    
    void CheckDangerousConditions()
    {
        foreach (var line in mooringLines)
        {
            if (line != null)
            {
                float ratio = line.CurrentTension / line.breakingTension;
                if (ratio >= 0.9f)
                {
                    Debug.LogError($"🚨 위험: {GetLineKoreanName(line.name)} 극한 장력 {line.CurrentTension:F0}N");
                }
                else if (ratio >= 0.7f)
                {
                    Debug.LogWarning($"⚠️ 경고: {GetLineKoreanName(line.name)} 높은 장력 {line.CurrentTension:F0}N");
                }
            }
        }
    }
    
    float GetTensionByName(string namePattern)
    {
        foreach (var line in mooringLines)
        {
            if (line != null && line.name.Contains(namePattern))
            {
                return line.CurrentTension;
            }
        }
        return 0f;
    }
    
    string GetLineKoreanName(string name)
    {
        if (name.Contains("Bow")) return "선수줄";
        if (name.Contains("Stern")) return "선미줄";
        if (name.Contains("Port")) return "좌현줄";
        if (name.Contains("Starboard")) return "우현줄";
        return "계류줄";
    }
    
    // Inspector에서 실행 가능한 모드 전환
    [ContextMenu("개발 모드 (모든 정보 표시)")]
    public void SetDevelopmentMode()
    {
        showInputForces = true;
        showTensionData = true;
        enableDetailedMode = true;
        Debug.Log("🔧 개발 모드 활성화 - 모든 정보 표시");
    }
    
    [ContextMenu("운영 모드 (장력만 표시)")]
    public void SetOperationMode()
    {
        showInputForces = false;
        showTensionData = true;
        enableDetailedMode = false;
        Debug.Log("🏭 운영 모드 활성화 - 계류줄 장력만 표시");
    }
}