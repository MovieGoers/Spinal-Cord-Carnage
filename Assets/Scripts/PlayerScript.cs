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
        wallRunning,
        air
    }

    private Movementstate state;

    public Text speedText;

    [Header("Movement")]
    public float NormalSpeed; // 플레이어 일반 속도

    private float moveSpeed; // 플레이어 속도
    public Rigidbody rb;
    Vector3 moveDirection; // 플레이어의 움직임 방향

    [Header("Jump")]
    public float jumpForce; // 점프 힘
    public float jumpCooldown; // 점프 사이의 간격
    public float airMultiplier; // 공중에 떠있을 때 플레이어 속도에 곱해지는 값.
    bool isReadyToJump;

    [Header("Ground Check")]
    public GameObject groundChecker; // 땅에 닿았는지 확인하기 위한 오브젝트
    public float groundCheckDistance; // GroundChecker와의 거리
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
    public float slideCooldown; // 슬라이딩 쿨다운
    public float maxSlideTime; // 슬라이딩 최대 시간
    public float slideSpeed; // 슬라이딩 시 속도
    public float slopeSlideMultiplier; // 경사면에서의 슬라이딩에 곱해지는 값
    float slideTimer;
    public float slideYScale; // 슬라이드 시 y축 크기
    bool isSliding; 
    bool isReadyToSlide;

    [Header("WallRunning")]
    public LayerMask whatIsWall;
    public float wallRunSpeed;
    public float wallCheckDistance;
    public float wallJumpUpForce;
    public float wallJumpSideForce;
    public float wallExitTime;
    float wallExitTimer;
    RaycastHit leftWallHit;
    RaycastHit rightWallHit;
    bool wallLeft;
    bool wallRight;
    bool isWallRunning;
    bool isExitingWall;
    Vector3 wallRunningVector;
    Vector3 wallNormalVector;

    private void Start()
    {
        isReadyToJump = true;
        isReadyToSlide = true;
        normalYScale = transform.localScale.y;
    }
    void Update()
    {
        speedText.text =  rb.velocity.magnitude.ToString("#.##");

        // rotate player
        transform.rotation = Quaternion.Euler(0f, InputManager.Instance.yRotation, 0f);


        // 플레이어가 땅에 닿아있는지 확인.
        isGrounded = CheckIfGrounded();

        if (isGrounded || isWallRunning)
            rb.drag = goundDrag; // 지형에 있는 경우 Drag 추가.
        else
            rb.drag = 0;

        CheckForWall();

        HandleInput();
        ChangeStateAndSpeed();
        AddForceToPlayer();
        LimitFlatVelocity();


        if (isSliding && isGrounded)
        {
            HandleSlide();
        }

        if(isWallRunning && !isGrounded)
        {
            HandleWallRunning();
        }

        // 경사면 위에 있을 때 중력 제거.
        rb.useGravity = !isOnSlope();

        // 벽 타기 시 중력 제거
        rb.useGravity = !isWallRunning;
    }

    bool CheckIfGrounded()
    {
        return Physics.CheckSphere(groundChecker.transform.position, groundCheckDistance, whatIsGround);
    }

    // 플레이어 Input 처리 및 세팅 초기값 설정.
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
        if (Input.GetKeyDown(InputManager.Instance.crouchKeyCode) && isGrounded)
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * crouchForce, ForceMode.Impulse);
        }

        if (Input.GetKeyUp(InputManager.Instance.crouchKeyCode))
        {
            transform.localScale = new Vector3(transform.localScale.x, normalYScale, transform.localScale.z);
        }

        // slide Player
        if (Input.GetKeyDown(InputManager.Instance.crouchKeyCode) && moveDirection.magnitude > 0.01f && isReadyToSlide && isGrounded)
        {
            StartSlide();
            isReadyToSlide = false;
            Invoke(nameof(ResetSlide), slideCooldown);
        }

        if (Input.GetKeyUp(InputManager.Instance.crouchKeyCode) && isSliding)
        {
            EndSlide();
        }

        // WallRun Player
        if((wallLeft || wallRight) && !isGrounded && !isWallRunning && !isExitingWall) //  InputManager.Instance.verticalInput > 0 조건문 미포함
        {
            StartWallRunning();
        }

        if (isExitingWall)
        {
            if (isWallRunning)
                EndWallRunning();

            if (wallExitTimer > 0)
                wallExitTimer -= Time.deltaTime;

            if (wallExitTimer <= 0)
                isExitingWall = false;
        }

        if(Input.GetKeyDown(InputManager.Instance.jumpKeyCode) && isWallRunning)
        {
            EndWallRunning();
            WallJump();
        }
    }

    // state, 그리고 속력 변경.
    void ChangeStateAndSpeed()
    {
        if (isWallRunning)
        {
            state = Movementstate.wallRunning;
            moveSpeed = wallRunSpeed;
        }
        else if (isSliding)
        {
            state = Movementstate.sliding;
            if (!isOnSlope() || rb.velocity.y > -0.1f)
                moveSpeed = slideSpeed;
            else
                moveSpeed = slideSpeed * slopeSlideMultiplier;
            
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
    void AddForceToPlayer()
    {
        // 플레이어가 움직이는 방향 벡터.
        moveDirection = transform.forward * InputManager.Instance.verticalInput + transform.right * InputManager.Instance.horizontalInput;

        if (isWallRunning)
        {
            // 벽 타기를 하는 경우,
            rb.AddForce(wallRunningVector.normalized * moveSpeed * Time.deltaTime, ForceMode.Force);
        }
        else if (isOnSlope())
        {
            // 경사면에 있는 경우,
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * Time.deltaTime, ForceMode.Force);
        }
        else if (isGrounded)
        {
            // 평지에 있는 경우,
            rb.AddForce(moveDirection.normalized * moveSpeed * Time.deltaTime, ForceMode.Force);
        }
        else
        {
            // 공중에 떠 있는 경우,
            rb.AddForce(moveDirection.normalized * moveSpeed * airMultiplier * Time.deltaTime, ForceMode.Force);
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

    void HandleSlide()
    {
        // 평면 또는 위로 향하는 방향일 경우,
        if(!isOnSlope() || rb.velocity.y > -0.1f)
            slideTimer -= Time.deltaTime;

        if (slideTimer <= 0)
            EndSlide();
    }

    void EndSlide()
    {
        isSliding = false;

        if(!Input.GetKey(InputManager.Instance.crouchKeyCode))
            transform.localScale = new Vector3(transform.localScale.x, normalYScale, transform.localScale.z);
    }

    void ResetSlide()
    {
        isReadyToSlide = true;
    }

    void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, transform.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -transform.right, out leftWallHit, wallCheckDistance, whatIsWall);
    }

    void StartWallRunning()
    {
        isWallRunning = true;
    }

    void HandleWallRunning()
    {
        if (wallRight)
            wallNormalVector = rightWallHit.normal;
        else
            wallNormalVector = leftWallHit.normal;

        wallRunningVector = Vector3.Cross(wallNormalVector, transform.up);

        if ((transform.forward - wallRunningVector.normalized).magnitude > 1)
        {
            wallRunningVector *= -1;
        }
    }

    void EndWallRunning()
    {
        isWallRunning = false;
    }

    void WallJump()
    {
        isExitingWall = true;
        wallExitTimer = wallExitTime;
        Vector3 wallJumpForce = transform.up * wallJumpUpForce + wallNormalVector * wallJumpSideForce;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(wallJumpForce, ForceMode.Impulse);
    }
}
