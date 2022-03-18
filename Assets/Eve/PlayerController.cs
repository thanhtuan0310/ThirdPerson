using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    Vector2 moveDirection;
    Vector2 lookDirection;
    float jumpDirection;

    public float moveSpeed = 2;
    public float maxForwardSpeed = 8;
    float turnSpeed = 100;
    float desiredSpeed;
    float forwardSpeed;
    float jumpSpeed = 30000f;

    bool escapePressed = false;

    const float groundAccel = 5;
    const float groundDecel = 25;

    Animator anim;
    Rigidbody rb;

    bool onGround = true;

    public Transform weapon;
    public Transform hand;
    public Transform hip;

    public bool isDead = false;

    int health = 100;

    public void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.tag == "Bullet")
        {
            health -= 10;
            anim.SetTrigger("Hit");

            if (health <= 0)
            {
                isDead = true;
                anim.SetLayerWeight(1, 0);
                anim.SetBool("Dead", true);
            }
        }
    }

    public void PickupGun()
    {
        weapon.SetParent(hand);
        weapon.localPosition = new Vector3(0.054f, -0.029f, -0.011f);
        weapon.localRotation = Quaternion.Euler(-20.26f, 76.985f, 125.45f);
        weapon.localScale = new Vector3(1, 1, 1);
    }

    public void PutDownGun()
    {
        weapon.SetParent(hip);
        weapon.localPosition = new Vector3(0.0853f, 0.0509f, -0.0775f);
        weapon.localRotation = Quaternion.Euler(94.578f, 187.022f, 172.451f);
        weapon.localScale = new Vector3(1, 1, 1);
    }

    bool IsMoveInput
    {
        get { return !Mathf.Approximately(moveDirection.sqrMagnitude, 0f); }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveDirection = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookDirection = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        jumpDirection = context.ReadValue<float>();
    }

    bool firing = false;

    public void OnFire(InputAction.CallbackContext context)
    {
        firing = false;
        if ((int)context.ReadValue<float>() == 1)
        {
            anim.SetTrigger("Fire");
            firing = true;
        }
    }

    public void OnESC(InputAction.CallbackContext context)
    {
        if ((int)context.ReadValue<float>() == 1)
            escapePressed = true;
        else
            escapePressed = false;
    }

    public void OnArmed(InputAction.CallbackContext context)
    {
        anim.SetBool("Armed", !anim.GetBool("Armed"));
    }


    void Move(Vector2 direction)
    {
        float turnAmount = direction.x;
        float fDirection = direction.y;
        if (direction.sqrMagnitude > 1f)
            direction.Normalize();

        desiredSpeed = direction.magnitude * maxForwardSpeed * Mathf.Sign(fDirection);
        float acceleration = IsMoveInput ? groundAccel : groundDecel;

        forwardSpeed = Mathf.MoveTowards(forwardSpeed, desiredSpeed, acceleration * Time.deltaTime);

        anim.SetFloat("ForwardSpeed", forwardSpeed);

        this.transform.Rotate(0, turnAmount * turnSpeed * Time.deltaTime, 0);
    }

    bool readyJump = false;
    float jumpEffort = 0;
    void Jump(float direction)
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

        //Debug.Log("Jump Effort: " + jumpEffort);
    }

    public void Launch()
    {
        rb.AddForce(0, jumpSpeed * Mathf.Clamp(jumpEffort, 1, 3), 0);
        rb.AddForce(this.transform.forward * forwardSpeed * 1000);
        anim.SetBool("Launch", false);
        anim.applyRootMotion = false;
        onGround = false;
    }

    public void Land()
    {
        anim.SetBool("Land", false);
        anim.applyRootMotion = true;
        anim.SetBool("Launch", false);
        jumpEffort = 0;
    }

    // Start is called before the first frame update
    void Start()
    {
        anim = this.GetComponent<Animator>();
        rb = this.GetComponent<Rigidbody>();
    }

    float groundRayDist = 2f;

    public Transform spine;
    Vector2 lastLookDirection;

    float xSensitivity = 0.5f;
    float ySensitivity = 0.5f;

    void LateUpdate()
    {
        if (isDead) return;
        if (anim.GetBool("Armed"))
        {
            lastLookDirection += new Vector2(-lookDirection.y * ySensitivity, lookDirection.x * xSensitivity);
            lastLookDirection.x = Mathf.Clamp(lastLookDirection.x, -30, 30);
            lastLookDirection.y = Mathf.Clamp(lastLookDirection.y, -30, 60);

            spine.localEulerAngles = lastLookDirection;
        }
    }


    public LineRenderer laser;
    public GameObject crosshair;
    public GameObject crossLight;

    bool cursorIsLocked = true;

    public void UpdateCursorLock()
    {
        if (escapePressed)
            cursorIsLocked = false;

        if (cursorIsLocked)
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

    // Update is called once per frame
    void Update()
    {
        UpdateCursorLock();

        if (isDead) return;

        Move(moveDirection);
        Jump(jumpDirection);

        if (anim.GetBool("Armed"))
        {
            laser.gameObject.SetActive(true);
            //crosshair.gameObject.SetActive(true);
            crossLight.gameObject.SetActive(true);
            RaycastHit laserHit;
            Ray laserRay = new Ray(laser.transform.position, laser.transform.forward);
            if (Physics.Raycast(laserRay, out laserHit))
            {
                laser.SetPosition(1, laser.transform.InverseTransformPoint(laserHit.point));
                Vector3 crosshairLocation = Camera.main.WorldToScreenPoint(laserHit.point);
                //crosshair.transform.position = crosshairLocation;
                crossLight.transform.localPosition = new Vector3(0, 0, laser.GetPosition(1).z * 0.9f);

                if (firing && laserHit.collider.gameObject.tag == "Orb")
                {
                    laserHit.collider.gameObject.GetComponent<AIController>().BlowUp();
                }

            }
            else
            {
                //crosshair.gameObject.SetActive(false);
                crossLight.gameObject.SetActive(false);
            }
        }
        else
        {
            laser.gameObject.SetActive(false);
            //crosshair.gameObject.SetActive(false);
            crossLight.gameObject.SetActive(false);
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
