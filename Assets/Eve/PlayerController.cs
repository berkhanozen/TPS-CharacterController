using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    Rigidbody rb;
    Animator anim;

    Vector2 moveDirection;
    Vector2 lookDirection;
    float jumpDirection;
    float fireDirection;
    
    public float maxForwardSpeed = 10;
    float turnSpeed = 200;
    float desiredSpeed;
    float forwardSpeed;
    
    float jumpSpeed = 20000f;
    bool readyJump = false;
    bool onGround = true;
    float jumpEffort;

    int escapePressed = 0;
    bool cursorIsLocked = true;

    const float groundAccel = 8f;
    const float groundDecel = 10f;

    public Transform weapon;
    public Transform hand;
    public Transform hip;

    public LineRenderer laser;

    public GameObject crosshair;

    public Transform spine;
    Vector2 lastLookDirection;

    bool IsMoveInput
    {
        get 
        {
            return !Mathf.Approximately(moveDirection.sqrMagnitude, 0f);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveDirection = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        jumpDirection = context.ReadValue<float>();
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        fireDirection = context.ReadValue<float>();
        
        /*if( (int)context.ReadValue<float>() == 1 &&  anim.GetBool("Armed"))
            anim.SetTrigger("Fire2");*/
    }
    public void OnArmed(InputAction.CallbackContext context)
    {
        anim.SetBool("Armed", !anim.GetBool("Armed"));
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookDirection = context.ReadValue<Vector2>();
    }

    public void OnESC(InputAction.CallbackContext context)
    {
        if ((context.ReadValue<bool>() == true))
            escapePressed++;
    }

    void Move(Vector2 direction)
    {
        if(direction.sqrMagnitude > 1f)
            direction.Normalize();
        
        desiredSpeed = direction.magnitude * maxForwardSpeed * Mathf.Sign(direction.y);

        float acceleration = IsMoveInput ? groundAccel : groundDecel;
        forwardSpeed = Mathf.MoveTowards(forwardSpeed, desiredSpeed, acceleration * Time.deltaTime);
        anim.SetFloat("ForwardSpeed", forwardSpeed);
        transform.Rotate(0, direction.x * turnSpeed * Time.deltaTime, 0);
       
    }

    void Jump (float direction)
    {
        if (direction > 0 && onGround)
        {
            anim.SetBool("ReadyJump", true);
            readyJump = true;
            jumpEffort += Time.deltaTime;
        }
        else if(readyJump)
        {
            anim.SetBool("Launch", true);
            readyJump = false;
            anim.SetBool("ReadyJump", false);
        }
    }

    void Fire(float direction)
    {
        if (direction > 0 && anim.GetBool("Armed"))
            anim.SetBool("Fire", true);
        else
            anim.SetBool("Fire", false);
    }

    public void Launch()
    {
        rb.AddForce(0, jumpSpeed * Mathf.Clamp(jumpEffort, 1, 3), 0);
        anim.SetBool("Launch", false);
        anim.applyRootMotion = false;
    }

    public void Land()
    {
        anim.SetBool("Land", false);
        anim.applyRootMotion = true;
        anim.SetBool("Launch", false);
        jumpEffort = 0;
    }

    public void PullOutGun()
    {
        weapon.SetParent(hand);
        weapon.localPosition = new Vector3(0.052f, -0.034f, -0.008f);
        weapon.localRotation = Quaternion.Euler(-30f, 95f, -250.185f);
        weapon.localScale = new Vector3(1, 1, 1);
        laser.enabled = true;
    }

    public void PutDownGun()
    {
        weapon.SetParent(hip);
        weapon.localPosition = new Vector3(0.0847929f, 0.05373872f, -0.07203432f);
        weapon.localRotation = Quaternion.Euler(103.853f, 166.761f, -223.863f);
        weapon.localScale = new Vector3(1, 1, 1);
        laser.enabled = false;
    }

    public void UpdateCursorLock()
    { 

        if(escapePressed % 2 == 0)
            cursorIsLocked = true;
        else
            cursorIsLocked = false;

        if(cursorIsLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Start()
    {
        anim = this.GetComponent<Animator>();
        rb = this.GetComponent<Rigidbody>();
    }

    float groundRayDist = 2f;
    float xSens = 0.5f;
    float ySens = 0.3f;

    void LateUpdate()
    {
        lastLookDirection += new Vector2(-lookDirection.y * ySens, lookDirection.x * xSens);
        lastLookDirection.x = Mathf.Clamp(lastLookDirection.x, -5, 10);
        lastLookDirection.y = Mathf.Clamp(lastLookDirection.y, -30, 60);
        spine.localEulerAngles = lastLookDirection;
        
    }

    void Update()
    {
        UpdateCursorLock();
        Move(moveDirection);
        Jump(jumpDirection);
        Fire(fireDirection);

        if (anim.GetBool("Armed"))
        {
            laser.gameObject.SetActive(true);
            crosshair.gameObject.SetActive(true);
            RaycastHit laserHit;
            Ray laserRay = new Ray(laser.transform.position, laser.transform.forward);
            if (Physics.Raycast(laserRay, out laserHit))
            {
                crosshair.gameObject.SetActive(true);
                laser.SetPosition(1, laser.transform.InverseTransformPoint(laserHit.point));
                Vector3 crosshairlocation = Camera.main.WorldToScreenPoint(laserHit.point);
                crosshair.transform.position = crosshairlocation;
            }
            else
                crosshair.gameObject.SetActive(false);
        }
        else
        {
            laser.gameObject.SetActive(false);
            crosshair.gameObject.SetActive(false);
        }
            


        RaycastHit hit;
        Ray ray = new Ray(transform.position + Vector3.up * groundRayDist * 0.5f, -Vector3.up);
        if (Physics.Raycast(ray, out hit, groundRayDist))
        {
            if (!onGround)
            {
                onGround = true;
                anim.SetFloat("LandingVelocity", rb.velocity.magnitude);
                anim.SetBool("Land", true);
                anim.SetBool("Falling", false);
            }
        }
        else
        {
            onGround = false;
            anim.SetBool("Falling", true);
            anim.applyRootMotion = false;
        }
        Debug.DrawRay(transform.position + Vector3.up * groundRayDist * 0.5f, -Vector3.up * groundRayDist, Color.red);
    }
}
