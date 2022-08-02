using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement")]
    [SerializeField] private float _runSpeed;
    [SerializeField] private float _airMobilityMultiplier;
    [Range(0.0f, 0.3f)] [SerializeField] private float _movementSmoothing;

    [Header("Jump")]
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _jumpBufferTime;
    [SerializeField] private float _coyoteJumpTime;

    [Header("Roll")]
    [SerializeField] private float _rollSpeed;
    [SerializeField] private float _rollBufferTime;
    
    [Header("Collision")]
    [SerializeField] private Transform _groundCheckBack;
    [SerializeField] private Transform _groundCheckMiddle;
    [SerializeField] private Transform _groundCheckFront;
    [SerializeField] private Transform _topCheckFrontOut;
    [SerializeField] private Transform _topCheckFrontIn;
    [SerializeField] private float _groundCheckDistance;
    [SerializeField] private LayerMask _whatIsGround;

    [Header("Physics")]
    [SerializeField] private float _gravity;
    [SerializeField] private float _linearDrag;
    [SerializeField] private float _fallMultiplier;


    // Input variables
    private float _horizontalMoveInput;
    private bool _jumpInput = false;

    // Player components
    private Rigidbody2D _rigidBody2D;
    private Animator _animator;

    // Player state variables
    private Vector3 _velocity = Vector3.zero;
    private bool _isFacingRight = true;
    private bool _isGrounded = true;
    private float _jumpTimer;
    private float _coyoteTimer;
    private bool _coyoteUsable;
    private bool _isRolling;
    private float _rollTimer;
    private bool _adjustingPosition;
    private Vector3 _velocityTmp;

    // Animation
    private int _horizontalSpeedHash = Animator.StringToHash("HorizontalSpeed");
    private int _verticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private int _isGroundedHash = Animator.StringToHash("IsGrounded");
    private int _isRollingHash = Animator.StringToHash("IsRolling");

    // Functions
    private bool IsGrounded => CheckGroundCollision(_groundCheckFront.position, Vector2.down) ||
                                CheckGroundCollision(_groundCheckMiddle.position, Vector2.down) ||
                                CheckGroundCollision(_groundCheckBack.position, Vector2.down);
    private bool IsGroundAhead => CheckGroundCollision(_groundCheckFront.position, Vector2.down);
    private bool CanMove => !_isRolling;
    private bool CanRoll => _rollTimer > Time.time && _isGrounded && !_isRolling;
    private bool CanJump => _jumpTimer > Time.time && !_isRolling && ((_isGrounded) || (_coyoteUsable && _coyoteTimer > Time.time));

    private bool CheckGroundCollision(Vector3 origin, Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, _groundCheckDistance, _whatIsGround);
        return hit.collider != null;
    }

    private void Awake()
    {
        _rigidBody2D = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        var grounded = IsGrounded;
        
        // Left ground this frame
        if(_isGrounded && !grounded)
        {
            _coyoteTimer = Time.time + _coyoteJumpTime;
        }

        // Landed this frame
        if(!_isGrounded && grounded)
        {
            _coyoteUsable = true;
        }

        _isGrounded = grounded;
    }

    private void FixedUpdate()
    {
        // Horizontal movement
        if(CanMove)
        {
            // Flips player if necessary
            if(((_horizontalMoveInput > 0 && !_isFacingRight) || (_horizontalMoveInput < 0 && _isFacingRight)))
            {
                Flip();
            }

            var targetHorizontalVelocity = _horizontalMoveInput * _runSpeed;
            if(!_isGrounded)
            {
                if(Mathf.Sign(targetHorizontalVelocity) != Mathf.Sign(_rigidBody2D.velocity.x))
                {
                    targetHorizontalVelocity *= _airMobilityMultiplier;
                }
                else
                {
                    var sign = Mathf.Sign(targetHorizontalVelocity);
                    targetHorizontalVelocity = Mathf.Max(Mathf.Abs(_rigidBody2D.velocity.x), Mathf.Abs(targetHorizontalVelocity * _airMobilityMultiplier));
                    targetHorizontalVelocity *= sign;
                }
            }

            _rigidBody2D.velocity = Vector3.SmoothDamp(_rigidBody2D.velocity, new Vector2(targetHorizontalVelocity, _rigidBody2D.velocity.y), ref _velocity, _movementSmoothing);
        }

        // Roll
        if(CanRoll)
        {
            _isRolling = true;
            _rollTimer = 0;
            if(IsGroundAhead)
            {
                _rigidBody2D.velocity = new Vector2(_rollSpeed * (_isFacingRight ? 1 : -1), 0.0f);
            }
        }
        else if(_isRolling)
        {
            if(!IsGroundAhead)
            {
                _rigidBody2D.velocity = Vector2.zero;
            }
        }

        // Jump
        if(CanJump)
        {
            _jumpTimer = 0;
            _coyoteTimer = 0;
            _coyoteUsable = false;
            _rigidBody2D.velocity = new Vector2(_rigidBody2D.velocity.x, 0);
            _rigidBody2D.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
        }

        AdjustPhysics();
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        _animator.SetFloat(_horizontalSpeedHash, Mathf.Abs(_rigidBody2D.velocity.x));
        _animator.SetFloat(_verticalSpeedHash, _rigidBody2D.velocity.y);
        _animator.SetBool(_isGroundedHash, _isGrounded);
        _animator.SetBool(_isRollingHash, _isRolling);
    }

    private void AdjustPhysics()
    {
        if(_isGrounded)
        {
            _rigidBody2D.drag = _isRolling ? 0.0f : _linearDrag;
        }
        else
        {
            _rigidBody2D.drag = _linearDrag * 0.15f;
            _rigidBody2D.gravityScale = _gravity;
            if(_rigidBody2D.velocity.y < 0)
            {
                _rigidBody2D.gravityScale = _gravity * _fallMultiplier;
            }
            else if(_rigidBody2D.velocity.y > 0 && !_jumpInput)
            {
                _rigidBody2D.gravityScale = _gravity * _fallMultiplier / 2;
            }
        }
    }

    private void Flip()
	{
		// Switch the way the player is labelled as facing.
		_isFacingRight = !_isFacingRight;

		// Multiply the player's x local scale by -1.
		Vector3 scale = transform.localScale;
		scale.x *= -1;
		transform.localScale = scale;
	}

    private void OnHorizontalMoveInput(InputAction.CallbackContext context)
    {
        _horizontalMoveInput = context.ReadValue<float>();
    }

    private void OnJumpPressed(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            _jumpInput = true;
            _jumpTimer = Time.time + _jumpBufferTime;
        }
        else if(context.canceled)
        {
            _jumpInput = false;
        }
    }

    private void OnRollPressed(InputAction.CallbackContext context)
    {
        if(context.performed)
        {
            if(!_isRolling)
            {
                _rollTimer = Time.time + _rollBufferTime;
            }
        }
    }

    private void OnRollMovementFinished()
    {
        var newVelocity = _rigidBody2D.velocity;
        newVelocity.x = 0.0f;
        _rigidBody2D.velocity = newVelocity;
    }

    public void OnRollAnimationFinished()
    {
        _isRolling = false;
    }
}
