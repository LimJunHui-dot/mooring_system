using UnityEngine;

public class EnvironmentalForces : MonoBehaviour
{
    [Header("Wind Settings")]
    public Vector3 windDirection = Vector3.right;
    public float windStrength = 300f;

    [Header("Wave Settings")]
    public float waveHeight = 1f;
    public float waveFrequency = 0.5f;


    [Header("Current Settings")]
    public Vector3 currentDirection = Vector3.forward;
    public float currentStrength = 150f;

    private Rigidbody rb;
    private float waveOffset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        waveOffset = Random.Range(0f, 100f);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // 바람 효과
        rb.AddForce(windDirection.normalized * windStrength);

        // 조류 효과
        rb.AddForce(currentDirection.normalized * currentStrength);

        // 파도 효과(사인파 기반)
        float waveForce = Mathf.Sin((Time.fixedTime + waveOffset) * waveFrequency * 2 * Mathf.PI) * waveHeight * rb.mass;
        rb.AddForce(Vector3.up * waveForce);

        // 파도에 의한 회전력
        float waveTorque = Mathf.Cos((Time.fixedTime + waveOffset) * waveFrequency * 2 * Mathf.PI) * waveHeight * 0.5f;
        rb.AddTorque(Vector3.forward * waveTorque);

        // 키보드 입력 처리(오류 방지)
        HandleKeyboardInputSafely();
    }

    void HandleKeyboardInputSafely()
    {
        try
        {
            // 키보드 제어 추가(테스트용)
            float horizontal = Input.GetAxis("Horizontal"); // A, D키
            float vertical = Input.GetAxis("Vertical"); // W, S키

            if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
            {
                Vector3 inputForce = new Vector3(horizontal, 0, vertical) * 1000f;
                rb.AddForce(inputForce);
                Debug.Log($"키보드 입력: {inputForce}");
            }
        }
        catch (System.InvalidOperationException)
        {
            // Input System 충돌 오류 무시하고 계속 진행
            Debug.LogWarning("Input System 충돌 감지 - Player Settings에서 Input Handling을 변경하세요");
        }
        
    }
}
