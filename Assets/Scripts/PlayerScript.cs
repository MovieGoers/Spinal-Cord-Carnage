using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerScript : MonoBehaviour
{
    public enum Movementstate
    {
        walking,
        crouching,
        sliding,
        air
    }

    private Movementstate state;

    public Text speedText;

    [Header("Movement")]
    public float NormalSpeed; // 플레이어 일반 속도

    private float moveSpeed; // 플레이어 속도
    public Rigidbody rb;
    Vector3 moveDirection; // 플레이어의 움직임 방향

    public float jumpForce; // 점프 힘
    public float jumpCooldown; // 점프 사이의 간격
    public float airMultiplier; // 공중에 떠있을 때 플레이어 속도에 곱해지는 값.
    bool isReadyToJump;

    [Header("Ground Check")]
    public GameObject groundChecker; // 땅에 닿았는지 확인하기 위한 오브젝트
    public float groundCheckDistance; // 땅과의 거리
    bool isGrounded;
    public LayerMask whatIsGround;
    public float goundDrag; // 땅에 닿았을 때 플레이어 감속값

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    public float crouchForce;
    private float normalYScale;

    [Header("SlopeHandle")]
    public float maxSlopeAngle;
    Collider[] slopeCollision;

    [Header("Slide")]
    public float maxSlideTime;
    public float slideSpeed;
    float slideTimer;
    public float slideYScale;
    bool isSliding;





    private void Start()
    {
        isReadyToJump = true;
        normalYScale = transform.localScale.y;
    }
    void Update()
    {
        speedText.text =  rb.velocity.magnitude.ToString("#.##");
        // rotate player
        transform.rotation = Quaternion.Euler(0f, InputManager.Instance.yRotation, 0f);

        // move player
        MovePlayer();
        LimitFlatVelocity();
        HandleState();
        HandleInput();

        // 경사면 위에 있을 때 중력 제거.
        rb.useGravity = !isOnSlope();
    }

    void HandleInput()
    {
        // jump player
        if (Input.GetKeyDown(InputManager.Instance.jumpKeyCode) && isReadyToJump && isGrounded)
        {
            Jump();
            isReadyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // crouch player
        if (Input.GetKeyDown(InputManager.Instance.crouchKeyCode))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * crouchForce, ForceMode.Impulse);
        }

        if (Input.GetKeyUp(InputManager.Instance.crouchKeyCode))
        {
            transform.localScale = new Vector3(transform.localScale.x, normalYScale, transform.localScale.z);
        }

        //slide player
        if (Input.GetKeyDown(InputManager.Instance.slideKeyCode) && moveDirection.magnitude > 0.001f)
        {
            StartSlide();
        }

        if(Input.GetKeyUp(InputManager.Instance.slideKeyCode) && isSliding)
        {
            EndSlide();
        }
    }

    void HandleState()
    {
        if (isSliding)
        {
            state = Movementstate.sliding;
            moveSpeed = slideSpeed;
        }
        else if (Input.GetKey(InputManager.Instance.crouchKeyCode))
        {
            state = Movementstate.crouching;
            moveSpeed = crouchSpeed;
        }
        else if (isGrounded)
        {
            state = Movementstate.walking;
            moveSpeed = NormalSpeed;
        }
        else
        {
            state = Movementstate.air;
        }
    }

    // 플레이어 움직임
    void MovePlayer()
    {
        // 플레이어가 움직이는 방향 벡터.
        moveDirection = transform.forward * InputManager.Instance.verticalInput + transform.right * InputManager.Instance.horizontalInput;

        // 경사면에 있는 경우,
        if (isOnSlope())
        {
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * Time.deltaTime, ForceMode.Force);
        }
        else if (isGrounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * Time.deltaTime, ForceMode.Force);
        else
            rb.AddForce(moveDirection.normalized * moveSpeed * airMultiplier * Time.deltaTime, ForceMode.Force);

        // 플레이어가 땅에 닿아있는지 확인.
        isGrounded = Physics.CheckSphere(groundChecker.transform.position, groundCheckDistance, whatIsGround);

        if (isGrounded)
            rb.drag = goundDrag;
        else
            rb.drag = 0;

        if (isSliding && isGrounded)
        {
            Slide();
        }
    }

    // 플레이어의 x, z축 속도 제한
    void LimitFlatVelocity()
    {
        Vector3 flatVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.x);

        if(flatVelocity.magnitude > moveSpeed)
        {
            Vector3 limitedVelocity = rb.velocity.normalized * moveSpeed;
            rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
        }
    }

    void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    void ResetJump()
    {
        isReadyToJump = true;
    }

    bool isOnSlope()
    {
        slopeCollision = Physics.OverlapSphere(groundChecker.transform.position, groundCheckDistance);
        if(slopeCollision.Length != 0)
        {
            float angle = Vector3.Angle(Vector3.up, slopeCollision[0].gameObject.transform.up);
            if (angle < maxSlopeAngle && angle > 5f)
                return true;
        }
        return false;
    }

    Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeCollision[0].gameObject.transform.up).normalized;
    }

    void StartSlide()
    {
        isSliding = true;

        transform.localScale = new Vector3(transform.localScale.x, slideYScale, transform.localScale.z);
        rb.AddForce(Vector3.down * crouchForce, ForceMode.Impulse);

        slideTimer = maxSlideTime;
    }

    void Slide()
    {
        // 평면 또는 위로 향하는 방향일 경우,
        if(!isOnSlope() || rb.velocity.y > -0.1f)
        {
            rb.AddForce(moveDirection.normalized * slideSpeed * Time.deltaTime, ForceMode.Force);

            slideTimer -= Time.deltaTime;
        }
        else
        {
            // 경사면일 경우, 경사면에서의 방향 벡터를 가지고 슬라이딩 사용.
            rb.AddForce(GetSlopeMoveDirection(moveDirection).normalized * slideSpeed * Time.deltaTime, ForceMode.Force);
        }

        if (slideTimer <= 0)
            EndSlide();
    }

    void EndSlide()
    {
        isSliding = false;

        transform.localScale = new Vector3(transform.localScale.x, normalYScale, transform.localScale.z);
    }
}
