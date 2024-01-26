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
    public float NormalSpeed; // �÷��̾� �Ϲ� �ӵ�

    private float moveSpeed; // �÷��̾� �ӵ�
    public Rigidbody rb;
    Vector3 moveDirection; // �÷��̾��� ������ ����

    [Header("Jump")]
    public float jumpForce; // ���� ��
    public float jumpCooldown; // ���� ������ ����
    public float airMultiplier; // ���߿� ������ �� �÷��̾� �ӵ��� �������� ��.
    bool isReadyToJump;

    [Header("Ground Check")]
    public GameObject groundChecker; // ���� ��Ҵ��� Ȯ���ϱ� ���� ������Ʈ
    public float groundCheckDistance; // GroundChecker���� �Ÿ�
    bool isGrounded;
    public LayerMask whatIsGround;
    public float goundDrag; // ���� ����� �� �÷��̾� ���Ӱ�

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    public float crouchForce;
    private float normalYScale;

    [Header("SlopeHandle")]
    public float maxSlopeAngle;
    Collider[] slopeCollision;

    [Header("Slide")]
    public float slideCooldown; // �����̵� ��ٿ�
    public float maxSlideTime; // �����̵� �ִ� �ð�
    public float slideSpeed; // �����̵� �� �ӵ�
    public float slopeSlideMultiplier; // ���鿡���� �����̵��� �������� ��
    float slideTimer;
    public float slideYScale; // �����̵� �� y�� ũ��
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


        // �÷��̾ ���� ����ִ��� Ȯ��.
        isGrounded = CheckIfGrounded();

        if (isGrounded || isWallRunning)
            rb.drag = goundDrag; // ������ �ִ� ��� Drag �߰�.
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

        // ���� ���� ���� �� �߷� ����.
        rb.useGravity = !isOnSlope();

        // �� Ÿ�� �� �߷� ����
        rb.useGravity = !isWallRunning;
    }

    bool CheckIfGrounded()
    {
        return Physics.CheckSphere(groundChecker.transform.position, groundCheckDistance, whatIsGround);
    }

    // �÷��̾� Input ó�� �� ���� �ʱⰪ ����.
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
        if((wallLeft || wallRight) && !isGrounded && !isWallRunning && !isExitingWall) //  InputManager.Instance.verticalInput > 0 ���ǹ� ������
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

    // state, �׸��� �ӷ� ����.
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

    // �÷��̾� ������
    void AddForceToPlayer()
    {
        // �÷��̾ �����̴� ���� ����.
        moveDirection = transform.forward * InputManager.Instance.verticalInput + transform.right * InputManager.Instance.horizontalInput;

        if (isWallRunning)
        {
            // �� Ÿ�⸦ �ϴ� ���,
            rb.AddForce(wallRunningVector.normalized * moveSpeed * Time.deltaTime, ForceMode.Force);
        }
        else if (isOnSlope())
        {
            // ���鿡 �ִ� ���,
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * Time.deltaTime, ForceMode.Force);
        }
        else if (isGrounded)
        {
            // ������ �ִ� ���,
            rb.AddForce(moveDirection.normalized * moveSpeed * Time.deltaTime, ForceMode.Force);
        }
        else
        {
            // ���߿� �� �ִ� ���,
            rb.AddForce(moveDirection.normalized * moveSpeed * airMultiplier * Time.deltaTime, ForceMode.Force);
        }
    }

    // �÷��̾��� x, z�� �ӵ� ����
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
        // ��� �Ǵ� ���� ���ϴ� ������ ���,
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
