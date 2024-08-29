using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Serializable]
    public struct Stats
    {
        public float TopSpeed;
        public float Acceleration;
        public float ReverseSpeed;
        public float ReverseAcceleration;
        public float AccelerationCurve;
        public float Braking;
        public float CoastingDrag;
        public float Grip;
        public float Steer;
        public float AddedGravity;

    }
    public PlayerController.Stats baseStats = new PlayerController.Stats
    {
        TopSpeed            = 8f,
        Acceleration        = 4f,
        AccelerationCurve   = 4f,
        Braking             = 10f,
        ReverseAcceleration = 5f,
        ReverseSpeed        = 5f,
        Steer               = 1f,
        CoastingDrag        = 4f,
        Grip                = .95f,
        AddedGravity        = 1f,
    };
    public float GroundPercent;
    public float AirPercent;

    public float turnInput;

    private Rigidbody Rigidbody;

    public WheelCollider FrontLeftWheel;
    public WheelCollider FrontRightWheel;
    public WheelCollider RearLeftWheel;
    public WheelCollider RearRightWheel;

    public Transform CenterOfMass;
    public float AirborneReorientationCoefficient = 3.0f;
    public float SuspensionHeight = 0.2f;
    public float SuspensionSpring = 20000.0f;
    public float SuspensionDamp = 500.0f;
    public float WheelsPositionVerticalOffset = 0.0f;

    public LayerMask GroundLayers = Physics.DefaultRaycastLayers;

    const float k_NullInput = 0.01f;
    const float k_NullSpeed = 0.01f;
    Vector3 m_VerticalReference = Vector3.up;

    PlayerController.Stats m_FinalStats;

    Quaternion m_LastValidRotation;
    Vector3 m_LastValidPosition;
    Vector3 m_LastCollisionNormal;
    bool m_HasCollision;

    void UpdateSuspensionParams(WheelCollider wheel)
    {
        wheel.suspensionDistance = SuspensionHeight;
        wheel.center = new Vector3(0.0f, WheelsPositionVerticalOffset, 0.0f);
        JointSpring spring = wheel.suspensionSpring;
        spring.spring = SuspensionSpring;
        spring.damper = SuspensionDamp;
        wheel.suspensionSpring = spring;
    }

    void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        // Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        m_FinalStats = baseStats;
        UpdateSuspensionParams(FrontLeftWheel);
        UpdateSuspensionParams(FrontRightWheel);
        UpdateSuspensionParams(RearLeftWheel);
        UpdateSuspensionParams(RearRightWheel);
    }

    void FixedUpdate()
    {
        UpdateSuspensionParams(FrontLeftWheel);
        UpdateSuspensionParams(FrontRightWheel);
        UpdateSuspensionParams(RearLeftWheel);
        UpdateSuspensionParams(RearRightWheel);

        Rigidbody.centerOfMass = transform.InverseTransformPoint(CenterOfMass.position);

        int groundedCount = 0;
        if (FrontLeftWheel.isGrounded && FrontLeftWheel.GetGroundHit(out WheelHit hit))
            groundedCount++;
        if (FrontRightWheel.isGrounded && FrontRightWheel.GetGroundHit(out hit))
            groundedCount++;
        if (RearLeftWheel.isGrounded && RearLeftWheel.GetGroundHit(out hit))
            groundedCount++;
        if (RearRightWheel.isGrounded && RearRightWheel.GetGroundHit(out hit))
            groundedCount++;

        // calculate how grounded and airborne we are
        GroundPercent = (float) groundedCount / 4.0f;
        AirPercent = 1 - GroundPercent;
        
        float moveInput = Input.GetAxis("Vertical"); // W/S 키로 전후진 입력받음
        turnInput = Input.GetAxis("Horizontal"); // A/D 키로 좌우 회전 입력받음
        Debug.Log(moveInput);
        MoveVehicle(moveInput, turnInput);
        GroundAirbourne();

    }

    void GroundAirbourne()
    {
        // while in the air, fall faster
        if (AirPercent >= 1)
        {
            Rigidbody.velocity += Physics.gravity * Time.fixedDeltaTime; // * m_FinalStats.AddedGravity;
        }
    }

    void MoveVehicle(float accelerate, float turnInput)
{
    float accelInput = accelerate;
    float accelerationCurveCoeff = 5;
    Vector3 localVel = transform.InverseTransformVector(Rigidbody.velocity);

    bool accelDirectionIsFwd = accelInput >= 0;
    bool localVelDirectionIsFwd = localVel.z >= 0;

    // 현재 진행 방향에 따른 최대 속도와 가속력 설정
    float maxSpeed = localVelDirectionIsFwd ? m_FinalStats.TopSpeed : m_FinalStats.ReverseSpeed;
    float accelPower = accelDirectionIsFwd ? m_FinalStats.Acceleration : m_FinalStats.ReverseAcceleration;

    float currentSpeed = Rigidbody.velocity.magnitude;
    float accelRampT = currentSpeed / maxSpeed;
    float multipliedAccelerationCurve = m_FinalStats.AccelerationCurve * accelerationCurveCoeff;
    float accelRamp = Mathf.Lerp(multipliedAccelerationCurve, 1, accelRampT * accelRampT);

    float finalAccelPower = accelPower;
    float finalAcceleration = finalAccelPower * accelRamp;

    // 차량의 회전 각도 계산
    float turningPower = turnInput * m_FinalStats.Steer;
    Quaternion turnAngle = Quaternion.AngleAxis(turningPower, transform.up);

    // 현재 속도에 따른 이동 방향 설정
    Vector3 fwd = transform.forward;
    Vector3 movement = fwd * accelInput * finalAcceleration * ((m_HasCollision || GroundPercent > 0.0f) ? 1.0f : 0.0f);

    // 최대 속도를 초과하지 않도록 이동량 제한
    bool wasOverMaxSpeed = currentSpeed >= maxSpeed;
    if (wasOverMaxSpeed) 
        movement *= 0.0f;

    Vector3 newVelocity = Rigidbody.velocity + movement * Time.fixedDeltaTime;
    newVelocity.y = Rigidbody.velocity.y; // 수직 속도는 유지

    // 회전 적용 후 속도 벡터 업데이트
    if (GroundPercent > 0.0f && !wasOverMaxSpeed)
    {
        newVelocity = turnAngle * newVelocity; // 회전 각도 적용
        newVelocity = Vector3.ClampMagnitude(newVelocity, maxSpeed); // 최대 속도 제한
    }

    // Coasting: 가속 입력이 없을 때 서서히 감속
    if (Mathf.Abs(accelInput) < k_NullInput && GroundPercent > 0.0f)
    {
        newVelocity = Vector3.MoveTowards(newVelocity, new Vector3(0, Rigidbody.velocity.y, 0), Time.fixedDeltaTime * m_FinalStats.CoastingDrag);
    }

    // 최종 속도 적용
    Rigidbody.velocity = newVelocity;

    // 차량의 회전 처리 (회전만 적용)
    if (GroundPercent > 0.0f)
    {
        Rigidbody.MoveRotation(Rigidbody.rotation * turnAngle);
    }
    bool validPosition = false;
    validPosition = GroundPercent > 0.7f && !m_HasCollision && Vector3.Dot(m_VerticalReference, Vector3.up) > 0.9f;

    // 공중에 있을 때나 지면에 거의 닿지 않았을 때의 회전 처리
    if (GroundPercent < 0.7f)
    {
        Rigidbody.angularVelocity = new Vector3(0.0f, Rigidbody.angularVelocity.y * 0.98f, 0.0f);
        Vector3 finalOrientationDirection = Vector3.ProjectOnPlane(transform.forward, m_VerticalReference);
        finalOrientationDirection.Normalize();
        if (finalOrientationDirection.sqrMagnitude > 0.0f)
        {
            Rigidbody.MoveRotation(Quaternion.Lerp(Rigidbody.rotation, Quaternion.LookRotation(finalOrientationDirection, m_VerticalReference), Mathf.Clamp01(AirborneReorientationCoefficient * Time.fixedDeltaTime)));
        }
    }
    else if (validPosition)
    {
        m_LastValidPosition = transform.position;
        m_LastValidRotation.eulerAngles = new Vector3(0.0f, transform.rotation.y, 0.0f);
    }
}




}