using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    public enum Movementstate
    {
        walking,
        crouching,
        air
    }

    public Movementstate state;

    [Header("Movement")]
    public float NormalSpeed;
    private float moveSpeed; // �÷��̾� �ӵ�
    public Rigidbody rb;
    Vector3 moveDirection; // �÷��̾��� ������ ����

    public float jumpForce; // ���� ��
    public float jumpCooldown; // ���� ������ ����
    public float airMultiplier; // ���߿� ������ �� �÷��̾� �ӵ��� �������� ��.
    bool isReadyToJump;

    [Header("Ground Check")]
    public GameObject groundChecker; // ���� ��Ҵ��� Ȯ���ϱ� ���� ������Ʈ
    public float groundCheckDistance; // ������ �Ÿ�
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
    public float maxSlideTime;
    public float slideForce;
    public float slideTimer;





    private void Start()
    {
        isReadyToJump = true;
        normalYScale = transform.localScale.y;
    }
    void Update()
    {
        // rotate player
        transform.rotation = Quaternion.Euler(0f, InputManager.Instance.yRotation, 0f);

        // move player
        MovePlayer();
        LimitFlatVelocity();
        HandleState();

        // jump player
        if (Input.GetKey(InputManager.Instance.jumpKeyCode) && isReadyToJump && isGrounded)
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

        rb.useGravity = !isOnSlope();
    }

    void HandleState()
    {
        if (Input.GetKey(InputManager.Instance.crouchKeyCode))
        {
            state = Movementstate.crouching;
            moveSpeed = crouchSpeed;
        }
        if (isGrounded)
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
    void MovePlayer()
    {
        moveDirection = transform.forward * InputManager.Instance.verticalInput + transform.right * InputManager.Instance.horizontalInput;

        // ���鿡 �ִ� ���,
        if (isOnSlope())
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * Time.deltaTime, ForceMode.Force);
        }
        else if (isGrounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * Time.deltaTime, ForceMode.Force);
        else
            rb.AddForce(moveDirection.normalized * moveSpeed * airMultiplier * Time.deltaTime, ForceMode.Force);

        // �÷��̾ ���� ����ִ��� Ȯ��.
        isGrounded = Physics.CheckSphere(groundChecker.transform.position, groundCheckDistance, whatIsGround);

        if (isGrounded)
            rb.drag = goundDrag;
        else
            rb.drag = 0;
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

    Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeCollision[0].gameObject.transform.up).normalized;
    }
}
