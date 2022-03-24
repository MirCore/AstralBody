using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public sealed class AstralBodyLocomotion : MonoBehaviour
{
    [Header("ActionBased Continuous Move and Turn Providers")]
    [SerializeField] private ActionBasedContinuousMoveProvider _continuousMoveProvider;
    [SerializeField] private ActionBasedContinuousTurnProvider _continuousTurnProvider;
    
    [Header("Input Action References")]
    [SerializeField] private InputActionReference _toggleReference;
    [SerializeField] private InputActionReference _resetAstralBodyPositionReference;
    [SerializeField] private InputActionReference _turnReference;
    [SerializeField] private InputActionReference _moveReference;
    
    [Header("AstralBody and FirstPerson References")]
    [SerializeField] private Camera _astralBodyCamera;
    [SerializeField] private XROrigin _astralBodyXROrigin;
    private GameObject _abCamera;
    private Transform _abCameraTransform;
    private Transform _abXROrigin;
    [Space]
    [SerializeField] private Camera _firstPersonCamera;
    [SerializeField] private XROrigin _firstPersonXROrigin;
    private GameObject _fpCamera;
    private Transform _fpCameraTransform;
    private Transform _fpXROrigin;
    
    [Header("Turn and MoveSpeed in AstralBody Mode")]
    [SerializeField] private float _astralTurnSpeed = 240;
    [SerializeField] private float _astralMoveSpeed = 5;
    private float _fpMoveSpeed;

    [Header("Distance between AstralBody and FirstPerson")]
    [SerializeField] private int _distance = 20;
    
    [Header("Start in AstralBody Mode")]
    [SerializeField] private bool _astralMode = false;
    

    private void Awake()
    {
        _toggleReference.action.started += Toggle;
        _abCamera = _astralBodyCamera.gameObject;
        _abXROrigin = _astralBodyXROrigin.transform;
        _abCameraTransform = _abCamera.transform;
        _fpCamera = _firstPersonCamera.gameObject;
        _fpXROrigin = _firstPersonXROrigin.transform;
        _fpCameraTransform = _fpCamera.transform;
        
        // copy FirstPerson MoveSpeed
        _fpMoveSpeed = _continuousMoveProvider.moveSpeed;
        
        // Swap Modes at Awake (in case its necessary)
        SwapModes();
    }

    private void OnDestroy()
    {
        _toggleReference.action.started -= Toggle;
    }

    private void Update()
    {
        if(!_astralMode) return;
        
        // rotate AB around FP position
        float turnAmount = GetTurnAmount(_turnReference.action?.ReadValue<Vector2>() ?? Vector2.zero);
        _abXROrigin.RotateAround(_fpCameraTransform.position, Vector3.up, turnAmount);
        

        if ( _resetAstralBodyPositionReference.action != null && _resetAstralBodyPositionReference.action.IsPressed())
        {
            ResetAbPosition();
        }
        
         // turn FP towards move direction
        Vector3 moveDirection = ComputeDesiredMove(_moveReference.action?.ReadValue<Vector2>() ?? Vector2.zero);
        if (moveDirection == Vector3.zero) return;
        float rotation = Quaternion.LookRotation(moveDirection).eulerAngles.y;
        rotation -= _fpCameraTransform.localRotation.eulerAngles.y;
        rotation -= _fpXROrigin.rotation.eulerAngles.y;
        _fpXROrigin.RotateAround(_fpCameraTransform.position, Vector3.up, rotation);
    }

    private void Toggle(InputAction.CallbackContext context)
    {
        // Toggle Astral Mode 
        _astralMode = !_astralMode;

        SwapModes();
    }

    private void ResetAbPosition()
    {
        // Set AB position relative to FP position an Camera forward
        Vector3 forward = _abCameraTransform.forward.normalized;
        _abXROrigin.position = _fpXROrigin.position - new Vector3(forward.x, 0, forward.z) * _distance;
    }

    private void SwapModes()
    {
        // Toggle active Camera
        //_fpCamera.SetActive(!_astralMode);
        _abCamera.SetActive(_astralMode);

        if (_astralMode)
        {
            // copy FirstPerson MoveSpeed
            _fpMoveSpeed = _continuousMoveProvider.moveSpeed;

            // Disable ContinuousTurnProvider
            _continuousTurnProvider.enabled = false;

            // Adjust MoveSpeed and forwardSource
            _continuousMoveProvider.moveSpeed = _astralMoveSpeed;
            _continuousMoveProvider.forwardSource = _abCameraTransform;

            // Set AB position relative to FP position an Camera forward
            ResetAbPosition();   
            
            _abXROrigin.LookAt(_fpCameraTransform);
            Quaternion rotation = _abXROrigin.localRotation * _fpXROrigin.localRotation * Quaternion.Inverse(_fpCameraTransform.rotation);
            _abXROrigin.localRotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);    
        }
        else
        {
            // Enable ContinuousTurnProvider
            _continuousTurnProvider.enabled = true;

            // Adjust MoveSpeed and forwardSource
            _continuousMoveProvider.moveSpeed = _fpMoveSpeed;
            _continuousMoveProvider.forwardSource = null;
        }
    }

    /// <summary>
    /// Copied from ContinuousTurnProviderBase
    /// Calculates Turn Amount
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private float GetTurnAmount(Vector2 input)
    {
        if (input == Vector2.zero)
            return 0f;

        var cardinal = CardinalUtility.GetNearestCardinal(input);
        switch (cardinal)
        {
            case Cardinal.North:
            case Cardinal.South:
                break;
            case Cardinal.East:
            case Cardinal.West:
                return input.magnitude * (Mathf.Sign(input.x) * _astralTurnSpeed * Time.deltaTime);
            default:
                Assert.IsTrue(false, $"Unhandled {nameof(Cardinal)}={cardinal}");
                break;
        }

        return 0f;
    }

    /// <summary>
    /// Function copied from ContinuousMoveProviderBase, shortened to needs
    /// Calculates Move Vector
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private Vector3 ComputeDesiredMove(Vector2 input)
        {
            if (input == Vector2.zero)
                return Vector3.zero;
            var inputMove = Vector3.ClampMagnitude(new Vector3(input.x, 0f, input.y), 1f);

            var originTransform = _fpXROrigin;
            var originUp = originTransform.up;

            // Determine frame of reference for what the input direction is relative to
            var forwardSourceTransform = _abCameraTransform;
            var inputForwardInWorldSpace = forwardSourceTransform.forward;
            if (Mathf.Approximately(Mathf.Abs(Vector3.Dot(inputForwardInWorldSpace, originUp)), 1f))
            {
                inputForwardInWorldSpace = -forwardSourceTransform.up;
            }

            var inputForwardProjectedInWorldSpace = Vector3.ProjectOnPlane(inputForwardInWorldSpace, originUp);
            var forwardRotation = Quaternion.FromToRotation(originTransform.forward, inputForwardProjectedInWorldSpace);
            
            var translationInRigSpace = forwardRotation * inputMove * (_astralMoveSpeed * Time.deltaTime);
            var translationInWorldSpace = originTransform.TransformDirection(translationInRigSpace);

            return translationInWorldSpace;
        }

}
