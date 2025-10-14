using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EventFramework;
using Unity.VisualScripting;
using UnityEngine.InputSystem;

public class PlayerMovementComp : EventCompBase
{
    PrimaryPlayer m_player => GetParentDict() as PrimaryPlayer;
    // 输入
    private Vector2 m_moveVector;
    private CharacterController _controller;
    private bool m_jumpInput = false;
    // 几帧内需要记忆的数据
    private float m_currentSpeed = 0f;
    public Vector2 m_lookVector;
    // private float m_cinemachineTargetPitch;
    private float m_verticalVelocity;
    private float m_inSpaceStartTime = -1f;


    float GroundedRadius => m_player.GetComponent<CharacterController>().radius;
    float GroundedOffset => m_player.GetComponent<CharacterController>().height * 0.05f;

    private Vector3[] m_MoveData = new Vector3[3]; // 分别是CharacterController移动前的目标位置，CharacterController移动的数据，CharacterController移动后的目标位置

    public CameraTargetMono focusTargetMono { get; private set; }

    private Vector2 screenPosition;
    public bool screenPositionValid => screenPosition.x > 0 && screenPosition.y > 0;
    protected static Dictionary<string, UnionInt64> slotMap = new Dictionary<string, UnionInt64>
    {
    };

    // Start is called before the first frame update
    public PlayerMovementComp() : base(slotMap)
    {
        _controller = m_player.GetComponent<CharacterController>();
        ResetPosition(new Vector3(-5, 4, 0));

    }
    public void SetMoveVector(Vector2 moveVector)
    {
        m_moveVector = moveVector;
    }

    public override void CompStart()
    {
        base.CompStart();
        G.InputMgr.RegisterInput("Move", InputEventType.Actived | InputEventType.Deactivated, OnMove, m_player.gameobject);
        G.InputMgr.RegisterInput("Sprint", InputEventType.Deactivated | InputEventType.Started, OnSprint, m_player.gameobject);
        G.InputMgr.RegisterInput("Look", InputEventType.Actived | InputEventType.Deactivated, OnLook, m_player.gameobject);
        G.InputMgr.RegisterInput("Jump", InputEventType.Started, OnJump, m_player.gameobject);

    }

    public override void CompDestroy()
    {
        base.CompDestroy();
        G.UnRegisterInput("Move", OnMove);
        G.UnRegisterInput("Sprint", OnSprint);
        G.UnRegisterInput("Look", OnLook);
        G.UnRegisterInput("Jump", OnJump);
    }

    public void FixedUpdate_Movement()
    {
        GroundedCheck();
        JumpAndGravity();
        UpdateMove();
        ApplyFinalPosition();
        UpdateCameraTransform();
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(m_player.transform.position.x, m_player.transform.position.y + GroundedRadius - GroundedOffset, m_player.transform.position.z);
        if (!Physics.CheckSphere(spherePosition, GroundedRadius, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
        {
            m_player.stateComp.AddState(Const.StateConst.INSPACE);
            if (m_inSpaceStartTime < 0f)
            {
                m_inSpaceStartTime = Time.fixedTime;
            }
        }
        else
        {
            m_player.stateComp.RemoveState(Const.StateConst.INSPACE);
            m_inSpaceStartTime = -1f;
            // TODO: 这里有点问题，如果起跳高度太低，会跑到这里导致起跳被重置
            m_verticalVelocity = -2f;
        }
    }
    private void UpdateCameraTransform()
    {
        m_player.attrComp.GetValue("pitchRotateSpeed", out float pitchRotateSpeed);
        m_player.attrComp.GetValue("yawRotateSpeed", out float yawRotateSpeed);
        m_player.attrComp.GetValue("minPitchAngle", out float minPitchAngle);
        m_player.attrComp.GetValue("maxPitchAngle", out float maxPitchAngle);
        if (focusTargetMono != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(focusTargetMono.GetCenterPosition() - m_player.RenderCamera.transform.position, Vector3.up);
            float _pitch = Helpers.AngleNormalize180(targetRotation.eulerAngles.x);
            _pitch = Mathf.Clamp(_pitch, minPitchAngle, maxPitchAngle);
            _pitch = Mathf.LerpAngle(m_player.GetCameraPitch(), _pitch, 6f * Time.fixedDeltaTime);
            float yaw = targetRotation.eulerAngles.y;
            yaw = Mathf.LerpAngle(m_player.transform.eulerAngles.y, yaw, 6f * Time.fixedDeltaTime);

            m_player.SetCameraPitch(_pitch);
            m_player.SetYawSelf(yaw);

            return;
        }
        if (m_lookVector.sqrMagnitude >= 0.01f)
        {
            float deltaTimeMultiplier = G.InputMgr.IsCurrentDeviceMouse ? 1.0f : Time.fixedDeltaTime;
            float _pitch = m_player.GetCameraPitch() + m_lookVector.y * pitchRotateSpeed * deltaTimeMultiplier;
            float _rotationVelocity = m_lookVector.x * yawRotateSpeed * deltaTimeMultiplier;
            _pitch = Mathf.Clamp(_pitch, minPitchAngle, maxPitchAngle);
            m_player.SetCameraPitch(_pitch);
            m_player.RotateYawSelf(_rotationVelocity);
        }
        
    }

    private void UpdateMove()
    {
        m_player.attrComp.GetValue(m_player.stateComp.HasState(Const.StateConst.SPRINT) ? "maxSprintSpeed" : "maxSpeed", out float targetSpeed);
        targetSpeed *= m_moveVector.magnitude;

        if (m_currentSpeed < targetSpeed - 0.1f || m_currentSpeed > targetSpeed + 0.1f)
        {
            m_currentSpeed = Mathf.Lerp(m_currentSpeed, targetSpeed, Time.fixedDeltaTime * 10.0f);
            m_currentSpeed = Mathf.Round(m_currentSpeed * 1000f) / 1000f;
        }
        else
        {
            m_currentSpeed = targetSpeed;
        }

        bool speedZero = m_moveVector == Vector2.zero || m_currentSpeed < 0.1f;
        bool hasMoveState = m_player.stateComp.HasState(Const.StateConst.MOVE);
        if (speedZero)
        {
            if (hasMoveState)
            {
                m_player.stateComp.RemoveState(Const.StateConst.MOVE);
            }
            return;
        }
        else if (!hasMoveState && !m_player.stateComp.CanAddState(Const.StateConst.MOVE))
        {
            return;
        }

        Vector3 inputDirection = m_player.transform.right * m_moveVector.x + m_player.transform.forward * m_moveVector.y;
        m_player.stateComp.AddState(Const.StateConst.MOVE);
        if (m_player.IsFirstPerson)
        {
            m_MoveData[1] += inputDirection.normalized * (m_currentSpeed * Time.fixedDeltaTime);
        }
        else
        {
            // m_player.GetComponent<Rigidbody>().MovePosition(m_player.transform.position + new Vector3(m_moveVector.x, 0, m_moveVector.y) * Time.fixedDeltaTime * 0.5f);
        }
    }

    public void TrySelectFocusTarget(IEnumerable<CameraTargetMono> targetList)
    {
        CameraTargetMono _target = null;
        float priority = 1e9f;
        foreach (var target in targetList)
        {
            if (target.CanBeLocked && target.GetCenterOffset().sqrMagnitude < priority)
            {
                priority = target.GetCenterOffset().sqrMagnitude;
                _target = target;
            }
        }
        SetFocusTargetMono(_target);
    }

    public void SetFocusTargetMono(CameraTargetMono targetMono)
    {
        if (targetMono == focusTargetMono)
        {
            return; // 如果目标没有变化，则不做任何操作
        }
        focusTargetMono = targetMono;
        this.InVokeEvent("OnFocusTargetChanged", targetMono);
    }
    public void SetFocusPointScreenPosition(Vector2 v, bool valid = true)
    {
        if (!valid)
        {
            screenPosition = new Vector2(-1, -1);
        }
        else
        {
            screenPosition = v;
        }
    }
    public bool GetFocusPointScreenPos(out Vector2 v)
    {
        v = screenPosition;
        return screenPositionValid;
    }


    private void JumpAndGravity()
    {
        m_player.attrComp.GetValue("gravity", out float gravity);
        if (!m_player.stateComp.HasState(Const.StateConst.INSPACE))
        {

            // // stop our velocity dropping infinitely when grounded
            // if (m_verticalVelocity < 0.0f)
            // {
            //     m_verticalVelocity = -2f;
            // }
            // Jump
            if (m_jumpInput && !m_player.stateComp.HasState(Const.StateConst.JUMP) && m_player.stateComp.CanAddState(Const.StateConst.JUMP))
            {
                m_player.stateComp.AddState(Const.StateConst.JUMP);
                m_player.attrComp.GetValue("jumpHeight", out float jumpHeight);

                // the square root of H * -2 * G = how much velocity needed to reach desired height
                m_verticalVelocity = Mathf.Sqrt(jumpHeight * -0.7f * gravity);
            }
        }
        else
        {
            m_verticalVelocity += (Time.fixedTime - m_inSpaceStartTime) * gravity * Time.fixedDeltaTime * 1.3f;
            if (m_verticalVelocity < 0)
            {
                m_player.stateComp.RemoveState(Const.StateConst.JUMP);
            }
        }
        m_jumpInput = false;
        m_MoveData[1] += new Vector3(0.0f, m_verticalVelocity, 0.0f) * Time.fixedDeltaTime;
    }

    public void ResetPosition(Vector3 position)
    {
        Ray ray = new Ray(position + Vector3.up * 50, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 60f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
        {
            m_MoveData[2] = hitInfo.point + Vector3.up * (0.1f + m_player.GetComponent<CharacterController>().center.y);
        }
    }
    private void ApplyFinalPosition()
    {
        if (m_MoveData[2] != Vector3.zero)
        {
            _controller.enabled = false;
            m_player.transform.position = m_MoveData[2];
            _controller.enabled = true;
        }
        else
        {
            if (m_MoveData[0] != Vector3.zero)
            {
                m_player.transform.position = m_MoveData[0];
                Physics.SyncTransforms();
            }
            _controller.Move(m_MoveData[1]);
        }
        m_MoveData[0] = Vector3.zero;
        m_MoveData[1] = Vector3.zero;
        m_MoveData[2] = Vector3.zero;
    }

    private bool OnMove(InputActionArgs args)
    {
        if (args.eventType == InputEventType.Performed || args.eventType == InputEventType.Started)
        {
            var value = args.ReadValue<Vector2>();
            SetMoveVector(value);
        }
        else
        {
            SetMoveVector(Vector2.zero);
        }
        return true;
    }

    private bool OnSprint(InputActionArgs args)
    {
        if (args.eventType == InputEventType.Started)
        {
            m_player.stateComp.AddState(Const.StateConst.SPRINT);
        }
        else if (args.eventType == InputEventType.Canceled)
        {
            m_player.stateComp.RemoveState(Const.StateConst.SPRINT);
        }
        return true;
    }
    private bool OnLook(InputActionArgs args)
    {
        if (args.eventType == InputEventType.Performed || args.eventType == InputEventType.Started)
        {
            var value = args.ReadValue<Vector2>();
            m_lookVector = value;
        }
        else
        {
            m_lookVector = Vector2.zero;
        }
        return true;
    }
    private bool OnJump(InputActionArgs args)
    {
        if (args.eventType == InputEventType.Started)
        {
            m_jumpInput = true;
        }
        return true;
    }
    

}
