using UnityEngine;

[RequireComponent(typeof(Animator))]
public class IKControlAB : MonoBehaviour
{
    private Animator _animator;

    [SerializeField] private bool _ikIsActive = false;
    [SerializeField] private Transform _rightHand;
    [SerializeField] private Transform _leftHand;
    [SerializeField] private Transform _head;
    [SerializeField] private Transform _avatarHead;
    [SerializeField] private Transform _avatarHips;
    [SerializeField] private float _headForwardShift;

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }

    //a callback for calculating IK
    private void OnAnimatorIK(int layerIndex)
    {
        if (!_animator) return;
        
        //if the IK is active, set the position and rotation directly to the goal. 
        if (_ikIsActive)
        {
            Vector3 headPosition = new Vector3();
            
            // Set the look target position, if one has been assigned
            if (_head != null)
            {
                headPosition = _head.position;
                _animator.SetLookAtWeight(1);
                _animator.SetLookAtPosition(headPosition + _head.forward * 10f);
            }
            
            // Set the armature position, if one has been assigned
            if (_head != null && _avatarHead != null && _avatarHips != null)
            {
                var yPosition = headPosition.y - (_avatarHead.position.y - _animator.bodyPosition.y);
                //Debug.Log("head: " + _head.rotation.eulerAngles.y + " body: " + _animator.bodyRotation.eulerAngles);
                _animator.bodyPosition = new Vector3(headPosition.x, yPosition, headPosition.z) - _head.forward * _headForwardShift;
                _animator.bodyRotation = Quaternion.AngleAxis(_head.rotation.eulerAngles.y, Vector3.up);
            }

            // Set the right hand target position and rotation, if one has been assigned
            if (_rightHand != null)
            {
                _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
                _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
                _animator.SetIKPosition(AvatarIKGoal.RightHand, _rightHand.position - _rightHand.forward * 0.1f);
                _animator.SetIKRotation(AvatarIKGoal.RightHand, _rightHand.rotation * Quaternion.Euler(0, 0, -90));
            }

            // Set the left hand target position and rotation, if one has been assigned
            if (_leftHand != null)
            {
                _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
                _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
                _animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHand.position - _leftHand.forward * 0.1f);
                _animator.SetIKRotation(AvatarIKGoal.LeftHand, _leftHand.rotation * Quaternion.Euler(0, 0, 90));
            }
        }

        //if the IK is not active, set the position and rotation of the hand and head back to the original position
        else
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0);
            _animator.SetLookAtWeight(0);
        }
    }
}