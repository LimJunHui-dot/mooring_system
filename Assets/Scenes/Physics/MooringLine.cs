using UnityEngine;

public class MooringLine : MonoBehaviour
{
    [Header("Connection Points")]
    public Transform shipMooringPoint;
    public Transform dockMooringPoint;

    [Header("Physical Properties")]
    public float restLength = 15f; // 윈치로 조절되는 초기 길이
    public float springConstant = 2000f; // 계류줄 강성 (N/m)
    public float breakingTension = 15000f; // 파단 임계 장력
    public float dampingCoefficient = 50f; // 감쇠 계수

    [Header("Visual")]
    public LineRenderer lineRenderer;


    [Header("Debug Info")]
    [SerializeField] public float currentTension = 0f;
    [SerializeField] public bool isBroken = false;

    // 색상 시스템용 변수
    private Color baseColor;
    private Material lineMaterial;

    private Rigidbody shipRb;
    private float lastLength;

    void Start()
    {
        // 필수 컴포넌트 확인
        if (shipMooringPoint == null || dockMooringPoint == null)
        {
            Debug.LogError($"{name}: 계류 포인트가 설정되지 않았습니다!");
            enabled = false;
            return;
        }

        shipRb = shipMooringPoint.GetComponentInParent<Rigidbody>();
        if (shipRb == null)
        {
            Debug.LogError($"{name}: 선박에 Rigidbody가 없습니다.");
            enabled = false;
            return;
        }

        // LineRenderer 자동 설정(핵심 수정 부분)
        SetupLineRenderer();


        lastLength = Vector3.Distance(shipMooringPoint.position, dockMooringPoint.position);
        Debug.Log($"{name} 계류줄 초기화 완료 - 초기 길이 : {lastLength:F1}m, 기본 색상: {baseColor}");
    }

    void FixedUpdate()
    {
        if (isBroken || shipRb == null) return;

        UpdateVisualization();
        CalculateAndApplyTension();
    }

    void UpdateVisualization()
    {
        // 계류줄 위치 업데이트
        lineRenderer.SetPosition(0, shipMooringPoint.position);
        lineRenderer.SetPosition(1, dockMooringPoint.position);

        // 장력 기반 색상 결정
        Color displayColor = GetTensionBasedColor();

        // Material 색상 업데이트
        if (lineMaterial != null)
        {
            lineMaterial.color = displayColor;
        }

        // 선 굵기도 장력에 따른 색상 변경
        float tensionRatio = currentTension / breakingTension;
        float lineWidth = Mathf.Lerp(0.1f, 0.3f, tensionRatio);
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
    }

    Color GetTensionBasedColor()
    {
        if (isBroken)
        {
            return Color.gray; // 파단 상태
        }

        float tensionRatio = currentTension / breakingTension;

        // 위험 단계별 색상 결정
        if (tensionRatio >= 0.9f)
        {
            // 극위험: 빨간색으로 완전 변경
            return Color.red;
        }
        else if (tensionRatio >= 0.7f)
        {
            // 경고: 노란색으로 변경
            return Color.yellow;
        }
        else if (tensionRatio >= 0.5f)
        {
            // 주의 : 기본 색상을 약간 밝게
            return Color.Lerp(baseColor, Color.white, 0.3f);
        }
        else
        {
            // 정상: 기본 색상 유지
            return baseColor;
        }
    }

    void CalculateAndApplyTension()
    {
        Vector3 connectionVector = dockMooringPoint.position - shipMooringPoint.position;
        float currentLength = connectionVector.magnitude;
        Vector3 forceDirection = connectionVector.normalized;

        // Hooke의 법칙에 따른 장력 계산
        if (currentLength > restLength)
        {
            float extension = currentLength - restLength;
            float springForce = springConstant * extension;

            // 감쇠력 계산 (속도에 비례)
            float lengthChangeRate = (currentLength - lastLength) / Time.fixedDeltaTime;
            float dampingForce = dampingCoefficient * lengthChangeRate;

            currentTension = springForce + dampingForce;

            // 선박에 장력 적용
            Vector3 tensionForce = forceDirection * currentTension;
            shipRb.AddForceAtPosition(tensionForce, shipMooringPoint.position);

            // 파단 검사
            if (currentTension >= breakingTension)
            {
                BreakMooringLine();
            }
        }
        else
        {
            currentTension = 0f;
        }

        lastLength = currentLength;
    }

    void BreakMooringLine()
    {
        isBroken = true;
        lineRenderer.enabled = false;
        Debug.LogWarning($"계류줄 {name} 파단 발생! 장력: {currentTension:F0}N");

        // 파단 이벤트 발생
        MooringManager.Instance?.OnMooringLineBreak(this);
    }

    public void AdjustRestLength(float newLength)
    {
        restLength = Mathf.Clamp(newLength, 5f, 25f);
    }

    // 외부 접근용 프로퍼티
    public float CurrentTension => currentTension;
    public bool IsBroken => isBroken;
    public float RestLength => restLength;

    void SetupLineRenderer()
    {
        // 기존 LineRenderer 확인 후 자동 생성
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            Debug.Log($"{name}: LineRenderer 자동 생성됨");
        }

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.15f;
        lineRenderer.endWidth = 0.15f;
        lineRenderer.useWorldSpace = true;

        // 전용 Material 생성
        lineMaterial = new Material(Shader.Find("Sprites/Default"));

        // 계류줄별 고유 색상 설정
        if (gameObject.name.Contains("Bow"))
            baseColor = new Color(0.8f, 0.2f, 0.2f);  // 진한 빨강 (선수줄)
        else if (gameObject.name.Contains("Stern"))
            baseColor = new Color(0.2f, 0.2f, 0.8f);  // 진한 파랑 (선미줄)
        else if (gameObject.name.Contains("Port"))
            baseColor = new Color(0.2f, 0.8f, 0.2f);  // 진한 초록 (좌현줄)
        else if (gameObject.name.Contains("Starboard"))
            baseColor = new Color(0.8f, 0.8f, 0.2f);  // 진한 노랑 (우현줄)
        else
            baseColor = Color.white;
        
        lineMaterial.color = baseColor;
        lineRenderer.material = lineMaterial;
    }
}