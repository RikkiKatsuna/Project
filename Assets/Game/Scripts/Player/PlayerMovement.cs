using System.Collections;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerMovement : MonoBehaviour
{
    //=> Serialize Field
    //Movement
    [SerializeField]
    private float _walkSpeed;
    [SerializeField]
    private float _sprintSpeed;
    [SerializeField]
    private float _jumpForce;
    [SerializeField]
    private float _walkSprintTransition;

    //Managers
    [SerializeField]
    private InputManager _input;
    [SerializeField]
    private CameraManager _cameraManager;

    //Rotations
    [SerializeField]
    private float _rotationSmoothTime = 0.1f;

    //Detectors
    [SerializeField]
    private Transform _groundDetector;
    [SerializeField]
    private float _detectorRadius;
    [SerializeField]
    private LayerMask _groundLayer;

    //Steps
    [SerializeField]
    private Vector3 _upperStepOffset;
    [SerializeField]
    private float _stepCheckerDistance;
    [SerializeField]
    private float _stepForce;

    //Climbs
    [SerializeField]
    private Transform _climbDetector;
    [SerializeField]
    private float _climbCheckDistance;
    [SerializeField]
    private LayerMask _climbableLayer;
    [SerializeField]
    private Vector3 _climbOffset;
    [SerializeField]
    private float _climbSpeed;


    //Camera
    [SerializeField]
    private Transform _cameraTransform;
    
    //Crouch
    [SerializeField]
    private float _crouchSpeed;
    private CapsuleCollider _collider;

    //Glide
    [SerializeField]
    private float _glideSpeed;
    [SerializeField]
    private float _airDrag;
    [SerializeField]
    private Vector3 _glideRotationSpeed;
    [SerializeField]
    private float _minGlideRotationX;
    [SerializeField]
    private float _maxGlideRotationX;

    //Attack
    private bool _isPunching;
    private int _combo = 0;
    [SerializeField]
    private float _resetComboInterval;
    private Coroutine _resetCombo;
    [SerializeField]
    private Transform _hitDetector;
    [SerializeField]
    private float _hitDetectorRadius;
    [SerializeField]
    private LayerMask _hitLayer;


    //=> Non-Serialize Field
    //Others
    private Rigidbody _rigidbody;
    private float _speed;
    private float _rotationSmoothVelocity;
    private bool _isGrounded;
    private PlayerStance _playerStance;
    private Animator _animator;

    //Void Methods
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
        _speed = _walkSpeed;
        _playerStance = PlayerStance.Stand;
        HideAndLockCursor();
        _collider = GetComponent<CapsuleCollider>();
    }
    private void Start()
    {
        _input.OnMoveInput += Move;
        _input.OnSprintInput += Sprint;
        _input.OnJumpInput += Jump;
        _input.OnClimbInput += StartClimb;
        _input.OnCancelClimb += CancelClimb;
        _cameraManager.OnChangePerspective += ChangePerspective;
        _input.OnCrouchInput += Crouch;
        _input.OnGlideInput += StartGlide;
        _input.OnCancelGlide += CancelGlide;
        _input.OnPunchInput += Punch;  
    }
    private void OnDestroy()
    {
        _input.OnMoveInput -= Move;
        _input.OnSprintInput -= Sprint;
        _input.OnJumpInput -= Jump;
        _input.OnClimbInput -= StartClimb;
        _input.OnCancelClimb -= CancelClimb;  
        _cameraManager.OnChangePerspective -= ChangePerspective;
        _input.OnCrouchInput -= Crouch;  
        _input.OnGlideInput -= StartGlide;
        _input.OnCancelGlide -= CancelGlide;
        _input.OnPunchInput -= Punch;    
    }
    private void Update()
    {
        CheckIsGrounded();
        CheckStep();
        Glide();
    }

    //Misc
    private void HideAndLockCursor(){
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    //=>Move
    private void Move(Vector2 axisDirection){
        Vector3 movementDirection = Vector3.zero;
        bool isPlayerStanding = _playerStance == PlayerStance.Stand;
        bool isPlayerClimbing = _playerStance == PlayerStance.Climb;
        bool isPlayerCrouch = _playerStance == PlayerStance.Crouch;
        bool isPlayerGliding = _playerStance == PlayerStance.Glide;
        if (isPlayerStanding || isPlayerCrouch && !_isPunching){

            switch(_cameraManager.CameraState){
                case CameraState.ThirdPerson:
                    if (axisDirection.magnitude >= 0.1){
                        float rotationAngle = Mathf.Atan2(axisDirection.x, axisDirection.y) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
                        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref _rotationSmoothVelocity, _rotationSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                        movementDirection = Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;
                        _rigidbody.AddForce(movementDirection * _speed * Time.deltaTime );
                    }
                    break;
                case CameraState.FirstPerson:
                    transform.rotation = Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f);
                    Vector3 verticalDirection = axisDirection.y * transform.forward;
                    Vector3 horizontalDirection = axisDirection.x * transform.right;
                    movementDirection = verticalDirection + horizontalDirection;
                    _rigidbody.AddForce(movementDirection * _speed * Time.deltaTime);
                    break;
                default:
                    break;
            }
            Vector3 velocity = new Vector3(_rigidbody.velocity.x, 0, _rigidbody.velocity.z);
            _animator.SetFloat("Velocity", velocity.magnitude * axisDirection.magnitude);
            _animator.SetFloat("VelocityZ", velocity.magnitude * axisDirection.y);
            _animator.SetFloat("VelocityX", velocity.magnitude * axisDirection.x);

            
        }
        else if (isPlayerClimbing){
           Vector3 horizontal = axisDirection.x * transform.right;
           Vector3 vertical = axisDirection.y * transform.up;
           movementDirection = horizontal + vertical;
           _rigidbody.AddForce(movementDirection * _climbSpeed * Time.deltaTime);
           Vector3 velocity = new Vector3(_rigidbody.velocity.x, _rigidbody.velocity.y, 0);
           _animator.SetFloat("ClimbVelocityY", velocity.magnitude * axisDirection.y);
           _animator.SetFloat("ClimbVelocityX", velocity.magnitude * axisDirection.x); 
        }
        else if (isPlayerGliding){
            Vector3 rotationDegree = transform.rotation.eulerAngles;
            rotationDegree.x += _glideRotationSpeed.x * axisDirection.y * Time.deltaTime;
            rotationDegree.x = Mathf.Clamp(rotationDegree.x, _minGlideRotationX, _maxGlideRotationX);
            rotationDegree.z += _glideRotationSpeed.z * axisDirection.x * Time.deltaTime;
            rotationDegree.y += _glideRotationSpeed.y * axisDirection.x * Time.deltaTime;
            transform.rotation = Quaternion.Euler(rotationDegree);
        }
    }

    //=>Sprint
    private void Sprint(bool isSprint)
    {
        if (isSprint){
            if (_speed < _sprintSpeed){
                _speed = _speed + _walkSprintTransition * Time.deltaTime;
            }
            
        }
        else{
            if (_speed > _walkSpeed){
                _speed = _speed - _walkSprintTransition * Time.deltaTime;
            }

        }
    }

    //=>Jump
    private void Jump(){
        if (_isGrounded){
            Vector3 jumpDirection = Vector3.up;
            _rigidbody.AddForce(jumpDirection * _jumpForce * Time.deltaTime);
            _animator.SetTrigger("Jump");
        }
        
        
    }

    //=>Collisions & Stairs
    private void CheckIsGrounded()
    {
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayer);
        _animator.SetBool("IsGrounded", _isGrounded);
        if (_isGrounded){
            CancelGlide();
        }
    }
    private void CheckStep(){
        bool isHitLowerStep = Physics.Raycast(_groundDetector.position, transform.forward, _stepCheckerDistance);
        bool isHitUpperStep = Physics.Raycast(_groundDetector.position + _upperStepOffset, transform.forward, _stepCheckerDistance);
        if (isHitLowerStep && !isHitUpperStep){
            _rigidbody.AddForce(0, _stepForce * Time.deltaTime, 0);
        }
    }

    //=>Climb
    private void StartClimb(){
        bool isInFrontofClimbingWall = Physics.Raycast(_climbDetector.position, transform.forward, out RaycastHit hit, _climbCheckDistance, _climbableLayer);
        bool isNotClimbing = _playerStance != PlayerStance.Climb;
        if (isInFrontofClimbingWall && _isGrounded && isNotClimbing){
            Vector3 offset = (transform.forward * _climbOffset.z) + (Vector3.up * _climbOffset.y);
            transform.position = hit.point - offset;
            _playerStance = PlayerStance.Climb;
            _rigidbody.useGravity = false;
            _speed = _climbSpeed;
            _cameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
            _cameraManager.SetTPSFieldofView(70);
            _animator.SetBool("IsClimbing", true);
            _collider.center = Vector3.up * 1.4f;
        }
    }
    private void CancelClimb(){
        if (_playerStance == PlayerStance.Climb){
            _playerStance = PlayerStance.Stand;
            _rigidbody.useGravity = true;
            transform.position -= transform.forward * 1f;
            _speed = _walkSpeed;
            _cameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
            _cameraManager.SetTPSFieldofView(40);
            _animator.SetBool("IsClimbing", false);
            _collider.center = Vector3.up * 0.9f;
        }
    }

    //=>Change Perspective
    private void ChangePerspective(){
        _animator.SetTrigger("ChangePerspective");
    }

    //=>Crouch
    private void Crouch(){
        if (_playerStance == PlayerStance.Stand){
            _playerStance = PlayerStance.Crouch;
            _animator.SetBool("IsCrouch", true);
            _speed = _crouchSpeed;
            _collider.height = 1.3f;
            _collider.center = Vector3.up * 0.66f;
        }
        else if(_playerStance == PlayerStance.Crouch){
            _playerStance = PlayerStance.Stand;
            _animator.SetBool("IsCrouch", false);
            _speed = _walkSpeed;
            _collider.height = 1.8f;
            _collider.center = Vector3.up * 0.9f;
        }
    }

    private void Glide(){
        if (_playerStance == PlayerStance.Glide){
            Vector3 playerRotation = transform.rotation.eulerAngles;
            float lift = playerRotation.x;
            Vector3 upForce = transform.up * (lift + _airDrag);
            Vector3 forwardForce = transform.forward * _glideSpeed;
            Vector3 totalForce = upForce + forwardForce;
            _rigidbody.AddForce(totalForce * Time.deltaTime);
        }
    }
    private void StartGlide(){
        if (_playerStance != PlayerStance.Glide && !_isGrounded){
            _playerStance = PlayerStance.Glide;
            _animator.SetBool("Glide", true);
        }
    }

    private void CancelGlide(){
        if (_playerStance == PlayerStance.Glide){
            _playerStance = PlayerStance.Stand;
            _animator.SetBool("Glide", false);
        }
    }

    private void Punch(){
        if (!_isPunching && _playerStance == PlayerStance.Stand){
            _isPunching = true;
            if (_combo < 3){
                _combo = _combo + 1;
            }
            else {
                _combo = 1;
            }
            _animator.SetInteger("Combo", _combo);
            _animator.SetTrigger("Punch");
        }
    }
    private void EndPunch(){
        _isPunching = false;
        if (_resetCombo != null){
            StopCoroutine(_resetCombo);
        } 
        _resetCombo = StartCoroutine(ResetCombo());
    }

    private IEnumerator ResetCombo(){
        yield return new WaitForSeconds(_resetComboInterval);
        _combo = 0;
    }

    private void Hit(){
        Collider[] hitObjects = Physics.OverlapSphere(_hitDetector.position, _hitDetectorRadius, _hitLayer);
        for (int i = 0; i < hitObjects.Length; i++){
            if (hitObjects[i].gameObject != null){
                Destroy(hitObjects[i].gameObject);
            }
        }
    }
}
