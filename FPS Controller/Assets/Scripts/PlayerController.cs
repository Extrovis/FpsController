using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;

    //Gravity
    [SerializeField] private float extraGravity = 19f;

    //Look variables
    [SerializeField] private Transform playerCam;
    [SerializeField] private Transform orientation;
    [SerializeField] private float sensitivity;
    private float lookMultiplier = 1f;
    private float xRotation = 0f;

    //Movement variables
    [SerializeField] private float moveSpeed;
    [SerializeField] private float maxSpeed;

    //Acceleration variables
    [SerializeField] private float timeToAccelerate = 2.5f;
    private float accelerationSpeed;

    //Move input and move directions
    private float x, y;
    Vector3 moveDir;
    Vector3 slopeMoveDir;

    //Jump variables
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float jumpForce;
    private float checkRadius = 0.3f;
    private bool isGrounded;

    //Dealing with Slopes Variables
    [SerializeField] float height = 1.7f;
    [SerializeField] float heightPadding = 0.05f;
    [SerializeField] float maxSlopeAngle = 90f;
    public bool debug;
    public bool onSlope;

    float groundAngle;

    Vector3 forward;
    RaycastHit slopeHit;

    [SerializeField] float playerHeight = 1.7f;

    Vector3 trueMoveDir;

    //Wall run variables
    [SerializeField] private float wallRunGravity;
    [SerializeField] private float wallRunSpeed;
    [SerializeField] private float wallRunJumpForce;
    [SerializeField] private float wallRunJumpMultiplier;
    [SerializeField] private float maxWallDistance;
    [SerializeField] private float wallRunSpeedMultiplier;
    [SerializeField] private LayerMask whatIsWallRunnable;

    private bool isWallRight, isWallLeft;
    private bool isWallRunning;

    RaycastHit rightWall, leftWall;

    private void Awake()
    {
        accelerationSpeed = maxSpeed / timeToAccelerate;
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        Movement();
    }

    // Update is called once per frame
    void Update()
    {
        //Add gravity

        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");

        Look();

        //Slopes and Jumping
        //if (OnSlope(onSlope))
        //{
        CalculateForward();
        CalculateSlopeAngle();
        //}
        CheckGround();
        //GroundCheck();
        DrawDebugLines();

        CheckForWall();
        WallRunInput();

        if (!isGrounded && !isWallRunning)
        {
            ApplyGravity(extraGravity);
        }

        //Debug.Log(isGrounded);
        //Debug.Log(onSlope);

        if (Input.GetButtonDown("Jump")) Jump();
    }

    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.fixedDeltaTime * lookMultiplier;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.fixedDeltaTime * lookMultiplier;

        Vector3 rotation = playerCam.transform.eulerAngles;
        float desiredX = rotation.y + mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCam.localRotation = Quaternion.Euler(xRotation, desiredX, 0f);
        orientation.localRotation = Quaternion.Euler(0f, desiredX, 0f);
    }

    private void Movement()
    {
        //if (groundAngle >= maxSlopeAngle) return;
        //if(rb.velocity.magnitude >= maxSpeed) { return; }

        if (y > 0 || y < 0 || x > 0 || x < 0)
        {
            moveSpeed += accelerationSpeed * Time.fixedDeltaTime;
        }

        if (!isWallRunning)
        {
            moveSpeed = Mathf.Clamp(moveSpeed, 0f, maxSpeed);
        }else if (isWallRunning)
        {
            moveSpeed = Mathf.Clamp(moveSpeed, 0f, wallRunSpeed);
        }

        Vector3 moveDirX = playerCam.right;
        Vector3 moveDirY = playerCam.forward;

        moveDirX.y = 0f;
        moveDirY.y = 0f;

        moveDirX.Normalize();
        moveDirY.Normalize();

        forward.Normalize();

        if (!OnSlope(onSlope))
        {
            moveDir = (moveDirY * y + moveDirX * x).normalized * moveSpeed + Vector3.up * rb.velocity.y;
            trueMoveDir = moveDir;

        }
        else if (OnSlope(onSlope) && isGrounded)
        {
            slopeMoveDir = (forward * y + moveDirX * x).normalized * moveSpeed + Vector3.up * rb.velocity.y;
            trueMoveDir = slopeMoveDir;
        }

        rb.velocity = trueMoveDir;

    }

    void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, checkRadius, whatIsGround);
        Debug.Log(isGrounded);
    }

    void WallRunInput()
    {
        //if (Input.GetKey(KeyCode.A) && isWallLeft) StartWallRun();
        //if (Input.GetKey(KeyCode.D) && isWallRight) StartWallRun();

        if (isWallRight) StartWallRun();
        if (isWallLeft) StartWallRun();
    }

    void CheckForWall()
    {
        isWallRight = Physics.Raycast(transform.position, orientation.right, out rightWall, maxWallDistance, whatIsWallRunnable);
        isWallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWall, maxWallDistance, whatIsWallRunnable);

        if (!isWallLeft && !isWallRight) StopWallRun();
    }

    void StartWallRun()
    {
        //Debug.Log("wall running");
        rb.useGravity = false;
        isWallRunning = true;

        if (!isGrounded && isWallRunning)
        {
            ApplyGravity(wallRunGravity);
        }

        rb.AddForce(orientation.forward * wallRunSpeedMultiplier, ForceMode.Force);
        moveSpeed = wallRunSpeed;
    }

    void Jump()
    {
        if (isGrounded)
        {
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }
    }

    void StopWallRun()
    {
        //Debug.Log("stopped wall running");
        isWallRunning = false;
        rb.useGravity = true;
        //moveSpeed = maxSpeed;
    }

    private bool OnSlope(bool onSlope)
    {
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight, whatIsGround))
        {
            if(slopeHit.normal != Vector3.up)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    void CalculateForward()
    {
        if (!isGrounded)
        {
            forward = playerCam.forward;
            return;
        }

        forward = Vector3.Cross(orientation.right, slopeHit.normal);
    }

    void CalculateSlopeAngle()
    {
        if (!isGrounded)
        {
            groundAngle = 90f;
            return;
        }

        groundAngle = Vector3.Angle(slopeHit.normal, orientation.forward);
        groundAngle = Mathf.Clamp(groundAngle, -60f, 60f);
    }

    void CheckGround()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, height + heightPadding, whatIsGround))
        { 
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    void ApplyGravity(float gravity)
    {
        if (!isGrounded)
        {
            rb.AddForce(Vector3.down * gravity, ForceMode.Force);
        }
    }

    void DrawDebugLines()
    {
        if(!debug) return;

        Debug.DrawLine(transform.position, transform.position + forward * height * 1.2f, Color.blue);
        Debug.DrawLine(transform.position, transform.position - Vector3.up * height, Color.green);
    }

    /*private void OnCollisionEnter(Collision collInfo)
    {
        if(Mathf.Abs(Vector3.Dot(collInfo.contacts[0].normal, Vector3.up)) < 0.09f){
            isOnWall = true;
            jumpDir = transform.up + collInfo.contacts[0].normal;
            StartWallRun();
        }
        else
        {
            isOnWall = false;
        }
    }*/
}
