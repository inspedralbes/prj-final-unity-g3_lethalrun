using UnityEngine;
using Cinemachine;
using System.Collections;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;
        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;
        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;
        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;
        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;
        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;
        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private Vector3 _initialCameraPosition;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        // Variables para el cambio de cámara y primera persona
        public CinemachineVirtualCamera virtualCamera1;
        public CinemachineVirtualCamera virtualCamera2;
        private bool isFirstPerson = false;

        // Variable para el hueso de la médula espinal
        public Transform spineTransform;

        private bool _isAttacking = false;
        private int _animIDIsAttacking;


        private bool IsCurrentDeviceMouse => _playerInput.currentControlScheme == "KeyboardMouse";

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            _initialCameraPosition = CinemachineCameraTarget.transform.localPosition;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();



            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            Move();
            HandleAttack();

            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                SwitchCamera();
            }
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDIsAttacking = Animator.StringToHash("IsAttacking");

        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }
        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;

                // Restricciones SOLO para primera persona
                if (isFirstPerson)
                {
                    if (_input.move == Vector2.zero)
                    {
                        // Rango más estrecho y simétrico solo en primera persona cuando está quieto
                        _cinemachineTargetPitch = Mathf.Clamp(_cinemachineTargetPitch + _input.look.y * deltaTimeMultiplier, -20f, 35f);
                    }
                    else
                    {
                        // Rango normal en primera persona cuando se mueve
                        _cinemachineTargetPitch = Mathf.Clamp(_cinemachineTargetPitch + _input.look.y * deltaTimeMultiplier, -20f, 35f);
                    }
                }
                else
                {
                    // En tercera persona, mantener el rango original completo
                    _cinemachineTargetPitch = Mathf.Clamp(_cinemachineTargetPitch + _input.look.y * deltaTimeMultiplier, BottomClamp, TopClamp);
                }
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Añadir un pequeño offset solo para primera persona
            float cameraOffset = isFirstPerson ? 0.1f : 0f;
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride + cameraOffset,
                _cinemachineTargetYaw,
                0.0f
            );

            if (isFirstPerson)
            {
                transform.rotation = Quaternion.Euler(0, _cinemachineTargetYaw, 0);
            }

            if (spineTransform != null)
            {
                float spineRotationX = Mathf.Clamp(_cinemachineTargetPitch, -45f, 45f);
                spineTransform.localRotation = Quaternion.Euler(spineRotationX, 0f, 0f);
            }
        }


        private void HandleAttack()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame && !_isAttacking)
            {
                _isAttacking = true;
                _animator.SetBool(_animIDIsAttacking, true);

                // Aquí puedes añadir lógica adicional del ataque, como detección de colisiones

                StartCoroutine(ResetAttackState());
            }
        }

        private IEnumerator ResetAttackState()
        {
            // Espera a que la animación de ataque termine
            yield return new WaitForSeconds(_animator.GetCurrentAnimatorStateInfo(0).length);

            _isAttacking = false;
            _animator.SetBool(_animIDIsAttacking, false);
        }


        private void Move()
        {
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

                if (!isFirstPerson)
                {
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
            }

            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // Ajustar la posición de la cámara SOLO en primera persona
            if (isFirstPerson)
            {
                if ((_input.sprint && _speed > MoveSpeed) || (_input.move != Vector2.zero && _speed > 0))
                {
                    // Ajustar la posición de la cámara hacia adelante y arriba cuando se mueve
                    CinemachineCameraTarget.transform.localPosition = Vector3.Lerp(
                        CinemachineCameraTarget.transform.localPosition,
                        _initialCameraPosition + new Vector3(0.1f, 0.024f, 0.444f), // Mover un poco más hacia adelante
                        Time.deltaTime * 5f
                    );
                }
                else
                {
                    // Volver a la posición inicial de la cámara
                    CinemachineCameraTarget.transform.localPosition = Vector3.Lerp(
                        CinemachineCameraTarget.transform.localPosition,
                        _initialCameraPosition,
                        Time.deltaTime * 5f
                    );
                }
            }
            else
            {
                // En tercera persona, mantener siempre la posición inicial de la cámara
                CinemachineCameraTarget.transform.localPosition = Vector3.Lerp(
                    CinemachineCameraTarget.transform.localPosition,
                    _initialCameraPosition,
                    Time.deltaTime * 5f
                );
            }

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }
        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }

                    // Ajustar la posición de la cámara SOLO en primera persona durante el salto
                    if (isFirstPerson)
                    {
                        CinemachineCameraTarget.transform.localPosition = Vector3.Lerp(
                            CinemachineCameraTarget.transform.localPosition,
                            _initialCameraPosition + new Vector3(0.1f, 0.5f, 0.444f), // Subir un poco más la cámara
                            Time.deltaTime * 10f
                        );
                    }
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                _input.jump = false;

                // Ajustar la posición de la cámara SOLO en primera persona durante la caída
                if (isFirstPerson)
                {
                    CinemachineCameraTarget.transform.localPosition = Vector3.Lerp(
                        CinemachineCameraTarget.transform.localPosition,
                        _initialCameraPosition + new Vector3(0.1f, 0.3f, 0.444f), // Ajustar altura durante la caída
                        Time.deltaTime * 5f
                    );
                }
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }


        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }

        }


        private void SwitchCamera()
        {
            isFirstPerson = !isFirstPerson;
            virtualCamera1.gameObject.SetActive(!isFirstPerson);
            virtualCamera2.gameObject.SetActive(isFirstPerson);
        }
    }
}