using UnityEngine;
using System.Collections.Generic;

public class ShipBuoyancy : MonoBehaviour
{
    [Header("Buoyancy Settings")]
    public float waterLevel = 0f;
    public float waterDensity = 1000f;
    public float buoyancyForceMultiplier = 2f;
    public List<Transform> buoyancyPoints;

    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // 부력 포인트가 설정되지 않았다면 자동 생성
        if (buoyancyPoints.Count == 0)
        {
            CrateBuoyancyPoints();
        }
    }

    void CrateBuoyancyPoints()
    {
        // 선박 하단에 여러 부력 포인트 생성
        for (int i = 0; i < 8; i++)
        {
            GameObject point = new GameObject($"BuoyancyPoint_{i}");
            point.transform.SetParent(transform);

            float x = (i % 2 == 0) ? -1f : 1f;
            float z = -2f + (i / 2) * 1.3f;
            point.transform.localPosition = new Vector3(x, -0.5f, z);

            buoyancyPoints.Add(point.transform);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        foreach (Transform point in buoyancyPoints)
        {
            if (point.position.y < waterLevel)
            {
                float submersionDepth = waterLevel - point.position.y;
                float buoyancyForce = waterDensity * Mathf.Abs(Physics.gravity.y) * submersionDepth * buoyancyForceMultiplier;

                rb.AddForceAtPosition(Vector3.up * buoyancyForce, point.position);

                // 물의 저항 추가
                Vector3 pointVelocity = rb.GetPointVelocity(point.position);
                Vector3 dragForce = -pointVelocity * 0.5f * submersionDepth;
                rb.AddForceAtPosition(dragForce, point.position);
            }
        }
    }
}
