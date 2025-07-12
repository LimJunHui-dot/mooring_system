using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MooringManager : MonoBehaviour
{
    public static MooringManager Instance;
    
    [Header("Control Settings")]
    public List<MooringLine> mooringLines;
    public float targetTension = 8000f;
    public float warningThreshold = 12000f;
    public float criticalThreshold = 14000f;
    public float adjustmentSpeed = 0.5f;
    
    [Header("Monitoring")]
    public bool autoControlEnabled = true;
    public float monitoringInterval = 0.1f;
    
    private Dictionary<MooringLine, TensionData> tensionHistory;
    private float lastMonitoringTime;
    
    [System.Serializable]
    public class TensionData
    {
        public float overTensionDuration;
        public int overTensionCount;
        public float maxTensionRecorded;
        public bool currentlyOverThreshold;
    }
    
    void Awake()
    {
        Instance = this;
        tensionHistory = new Dictionary<MooringLine, TensionData>();
        
        foreach (var line in mooringLines)
        {
            tensionHistory[line] = new TensionData();
        }
    }
    
    void Update()
    {
        if (Time.time - lastMonitoringTime >= monitoringInterval)
        {
            MonitorTensions();
            if (autoControlEnabled)
            {
                AutoAdjustTensions();
            }
            lastMonitoringTime = Time.time;
        }
    }
    
    void MonitorTensions()
    {
        foreach (var line in mooringLines)
        {
            if (line.isBroken) continue;
            
            var data = tensionHistory[line];
            
            // 최대 장력 기록 업데이트
            if (line.currentTension > data.maxTensionRecorded)
            {
                data.maxTensionRecorded = line.currentTension;
            }
            
            // 임계치 초과 모니터링
            if (line.currentTension >= warningThreshold)
            {
                if (!data.currentlyOverThreshold)
                {
                    data.overTensionCount++;
                    data.currentlyOverThreshold = true;
                }
                data.overTensionDuration += monitoringInterval;
                
                // 경고 발생
                TriggerWarning(line);
            }
            else
            {
                data.currentlyOverThreshold = false;
            }
        }
    }
    
    void AutoAdjustTensions()
    {
        var activeMoorings = mooringLines.Where(m => !m.isBroken).ToList();
        if (activeMoorings.Count == 0) return;
        
        // 전후 계류줄 그룹 (Bow-Stern)
        var bowSternLines = activeMoorings.Where(m => 
            m.name.Contains("Bow") || m.name.Contains("Stern")).ToList();
        
        // 좌우 계류줄 그룹 (Port-Starboard)
        var portStarboardLines = activeMoorings.Where(m => 
            m.name.Contains("Port") || m.name.Contains("Starboard")).ToList();
        
        // 그룹별 균형 조정
        if (bowSternLines.Count >= 2)
            BalanceLinePair(bowSternLines);
        
        if (portStarboardLines.Count >= 2)
            BalanceLinePair(portStarboardLines);
        
        // 전체 계류줄 장력 조정
        foreach (var line in activeMoorings)
        {
            float tensionError = targetTension - line.currentTension;
            
            if (Mathf.Abs(tensionError) > targetTension * 0.15f)
            {
                float adjustment = tensionError * adjustmentSpeed * Time.deltaTime * 0.001f;
                float newLength = line.restLength - adjustment;
                line.AdjustRestLength(newLength);
            }
        }
    }

    void BalanceLinePair(List<MooringLine> linePair)
    {
        if (linePair.Count != 2) return;
        
        var line1 = linePair[0];
        var line2 = linePair[1];
        
        float tensionDiff = line1.currentTension - line2.currentTension;
        
        // 20% 이상 차이 시 균형 조정
        if (Mathf.Abs(tensionDiff) > targetTension * 0.2f)
        {
            float adjustment = tensionDiff * 0.1f * Time.deltaTime * 0.001f;
            
            if (tensionDiff > 0)
            {
                line1.AdjustRestLength(line1.restLength + adjustment);
                line2.AdjustRestLength(line2.restLength - adjustment);
            }
            else
            {
                line1.AdjustRestLength(line1.restLength - adjustment);
                line2.AdjustRestLength(line2.restLength + adjustment);
            }
        }
    }
    
    void TriggerWarning(MooringLine line)
    {
        if (line.currentTension >= criticalThreshold)
        {
            Debug.LogError($"위험! 계류줄 {line.name} 임계 장력 초과: {line.currentTension:F0}N");
            // UI 경고 표시, 경보음 재생 등
            UIManager.Instance?.ShowCriticalAlert(line);
        }
        else
        {
            Debug.LogWarning($"경고! 계류줄 {line.name} 장력 증가: {line.currentTension:F0}N");
            UIManager.Instance?.ShowWarningAlert(line);
        }
    }
    
    public void OnMooringLineBreak(MooringLine brokenLine)
    {
        Debug.LogError($"계류줄 파단! {brokenLine.name}");
        UIManager.Instance?.ShowBreakageAlert(brokenLine);
        
        // 남은 계류줄들의 부하 재분배
        RedistributeLoad();
    }
    
    void RedistributeLoad()
    {
        // 파단된 계류줄의 부하를 남은 계류줄들에 재분배
        var activeMoorings = mooringLines.Where(m => !m.isBroken).ToList();
        foreach (var line in activeMoorings)
        {
            // 목표 장력을 약간 높여 추가 부하 감당
            float newTarget = targetTension * 1.2f;
            // 안전 범위 내에서 조정
            if (newTarget < warningThreshold * 0.8f)
            {
                targetTension = newTarget;
            }
        }
    }
}