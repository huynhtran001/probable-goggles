using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;

public class PlayerMovement : MonoBehaviour {
    [SerializeField] CinemachineConfiner cam;
    [SerializeField] GameManager manager;
    float horizontalMove = 0f;
    [SerializeField] float jumpSpeed = 10f;
    [SerializeField] float moveSpeed = 1000f;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] Animator anim;
    private SpriteRenderer sprite;
    bool grounded = true;
    [SerializeField] float fallSpeed = 2f;
    private float dist; // used for raycasting with offset
    [SerializeField] CapsuleCollider2D myCollider;
    [Tooltip("Ground check offset for raycast")][SerializeField] float offset = 0.41f;
    private bool isCrouching = false;
    private bool isClimbing = false;
    [SerializeField] int totalMidAirJumps = 1;
    [SerializeField] int midAirJumps = 0;
    [SerializeField] float initialGravity = 5f;
    [SerializeField] float climbSpeed = 5f;
    [SerializeField] float crouchSpeedModifier = .2f;

    private bool isAlive;
    private bool loseControl;

    [Tooltip("How quickly you lose momentum while airborne")]
    [Range(0, 1)]
    [SerializeField] float momentumDecreaseFactor = 0.985f;
    private Vector2 momentum = new Vector2(0, 0);


    // dashing ----------------------------------
    [SerializeField] float dashForce = 50f;
    private bool isDashing = false;
    [SerializeField] float dashDuration = 0.05f;
    [SerializeField] float postVerticalDash = .02f; // used when dashing downwards. allows for a 'graceful' fall after dashing is complete
    private float timeSinceLastDash;
    [SerializeField] float dashCooldown = .3f;
    private int currentDashes = 0;
    [SerializeField] int maxDashes = 1;
    [Range(0, 1)] [Tooltip("How far the player has to tilt the controller vertically before they can dash up or down")]
    [SerializeField] float minimumVerticalTilt = 0.4f;
    private int currentScene; // disable dash on first level
    private bool dontdash = false;


    // all sound and particle effects
    [SerializeField] AudioSource audioSource;
    [SerializeField] GameObject dashFX;
    [SerializeField] AudioClip dashSoundFX;

    [SerializeField] AudioClip jumpSFX;
    [SerializeField] GameObject midAirFX;
    [SerializeField] AudioClip midAirSFX;

    [SerializeField] GameObject winFX;
    [SerializeField] AudioClip winSoundFX;
    [Range(0, 1)] 
    [SerializeField] float winVolumeModifier = 0.2f;

    [SerializeField] AudioClip deathSFX;

    // death ------------------------------------
    [SerializeField] float deathKnockup = 10f;
    [Range(0, 1)]
    [SerializeField] float deathVolume = .5f;
    [SerializeField] float deathTimer = 3f;

    // Use this for initialization
    void Start ()
    {
        isAlive = true;
        loseControl = false;
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        myCollider = GetComponent<CapsuleCollider2D>();
        dist = myCollider.bounds.extents.y;
        initialGravity = rb.gravityScale;
        manager = FindObjectOfType<GameManager>();
        currentScene = SceneManager.GetActiveScene().buildIndex;
    }

    // Update is called once per frame
    private void Update()
    {
        Pause();
        // don't move if you're dead, talking to NPC, or dashing. pausing is okay though
        if (!isAlive || loseControl || isDashing) { return; }
        Jumping();
        Dash();
        CrouchCheck();
    }
    
    private void Pause()
    {
        if (CrossPlatformInputManager.GetButtonDown("Cancel"))
        {
            manager.PlayerPause(this);
        }
    }

    private void Jumping()
    {
        if (CrossPlatformInputManager.GetButtonDown("Jump"))
        {
            Jump();
        }

        if (CrossPlatformInputManager.GetButtonUp("Jump"))
        {
            if (rb.velocity.y > 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * fallSpeed);
            }
        }
    }

    void FixedUpdate ()
    {
        // don't move if you're dead, talking to NPC, or dashing
        if (!isAlive || loseControl || isDashing) { return; }

        // CheckingJoypadInput();
        AllMovements();
        Hazards();
    }


    // just a helper method to find controller inputs when plugged in
    private static void CheckingJoypadInput()
    {
        for (int i = 0; i < 20; i++)
        {
            if (Input.GetKeyDown("joystick 1 button " + i))
            {
                print("joystick 1 button " + i);
            }
        }
    }

    private void Hazards()
    {
        // if player touches hazards, die
        if (myCollider.IsTouchingLayers(LayerMask.GetMask("Hazards")))
        {
            // sets isAlive to false to prevent player from moving, sets anim to play death anim, collider turned off and velocity reset and added a small kick
            // to have the player bounce up slighty and fall off map
            isAlive = false;
            anim.SetBool("isDead", !isAlive);
            myCollider.enabled = false;
            rb.velocity = new Vector2(0, 0);
            rb.velocity = new Vector2(0, deathKnockup);
            // play death animation, sound fx, etc...
            audioSource.PlayOneShot(deathSFX, deathVolume);

            // respawn
            StartCoroutine(RespawnSequence());
        }
    }

    IEnumerator RespawnSequence()
    {
        yield return new WaitForSeconds(deathTimer);
        isAlive = true;
        myCollider.enabled = true;
        anim.SetBool("isDead", !isAlive);
        manager.Respawn(gameObject);
    }

    public void Respawn()
    {
        if (cam != null) cam.m_BoundingShape2D = null;
        manager.Respawn(gameObject);
    }

    public void SetVolume(float f)
    {
        audioSource.volume = f;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "goal")
        {
            Instantiate(winFX, collision.transform.position, Quaternion.identity);
            Destroy(collision.gameObject);
            audioSource.PlayOneShot(winSoundFX, winVolumeModifier); // current sfx is too loud
            manager.Win();
        }

        if (collision.tag == "boundary" && cam.m_BoundingShape2D != collision)
        {
            cam.m_BoundingShape2D = collision;
        }

        if (collision.tag == "NPC")
        {
            dontdash = true;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "boundary" && cam.m_BoundingShape2D != collision)
        {
            cam.m_BoundingShape2D = collision;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "NPC")
        {
            dontdash = false;
        }
    }


    private void AllMovements()
    {

        isGrounded();
        anim.SetBool("climbing", isClimbing);
        Climbing();
        PlayerMove();
    }

    private void Dash()
    {
        if (currentScene == 2 || dontdash) return;

        if (currentDashes >= maxDashes) return; // don't do anything until you've landed and regained more dashes

        // get the inputs for horiontal and vertical, then create a new vector2 with those times the dash force
        float horizontalInput = CrossPlatformInputManager.GetAxisRaw("Horizontal");
        float verticalInput = CrossPlatformInputManager.GetAxisRaw("Vertical");

        // if the player did not input any direction while trying to dash, it will dash where ever the character is facing.
        if (horizontalInput == 0 && verticalInput == 0)
        {
            if (sprite.flipX) // if sprite is currently flipped, it means the player is facing left
            {
                horizontalInput = -1;
            }
            else
            {
                horizontalInput = 1;
            }
        }

        // makes it so on a joypad, it dashes only in 8 directions instead of whereever the joypad is tilted. i.e., makes it "snap" to a direction
        if (horizontalInput > 0.2f) horizontalInput = 1f;
        else if (horizontalInput < -0.2f) horizontalInput = -1f;
        else horizontalInput = 0;

        if (verticalInput > minimumVerticalTilt) verticalInput = 1f;
        else if (verticalInput < -minimumVerticalTilt) verticalInput = -1f;
        else verticalInput = 0;

        timeSinceLastDash += Time.fixedDeltaTime;
        if (CrossPlatformInputManager.GetButtonDown("Dash") && timeSinceLastDash > dashCooldown)
        {
            currentDashes++;
            StartCoroutine(Dashing(horizontalInput, verticalInput));
        }
        
    }

    IEnumerator Dashing(float x, float y)
    {
        isDashing = true;
        rb.velocity = new Vector2(x, y) * dashForce;

        GameObject game = Instantiate(dashFX, transform.position, Quaternion.identity);
        Destroy(game, 0.3f);
        audioSource.clip = dashSoundFX;
        audioSource.Play();

        yield return new WaitForSeconds(dashDuration);
        if (y < 0)
        {
            // dashing downward, so just come to a sudden stop when finished dashing
            rb.velocity = new Vector2(0, 0);
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y * postVerticalDash); // the y component is multiplied by a small fraction to not have the player fall down immediately
                                                                            // gives the player a 'floaty' effect after dashing upwards
        }
        isDashing = false; // allows player to regain movement after dashing is complete
        timeSinceLastDash = 0;
    }

    public void ToggleControl()
    {
        loseControl = !loseControl;
    }

    private void Climbing()
    {
        if (!myCollider.IsTouchingLayers(LayerMask.GetMask("Climable"))) {
            isClimbing = false;
            anim.SetBool("climbing", isClimbing);
            rb.gravityScale = initialGravity;
            return;
        }
        
        float controlThrowVertical = CrossPlatformInputManager.GetAxisRaw("Vertical");
        if (controlThrowVertical > 0 && !isClimbing)
        {
            isClimbing = true;
            rb.velocity = new Vector2(0, 0);
            midAirJumps = 0;
            currentDashes = 0;
            // sets velocity to 0
            rb.gravityScale = 0;
        }

        if (controlThrowVertical > 0.2f) controlThrowVertical = 1;
        else if (controlThrowVertical < -0.2f) controlThrowVertical = -1;
        else controlThrowVertical = 0;

        if (isClimbing)
        {
            rb.velocity = new Vector2(0, controlThrowVertical * climbSpeed * Time.fixedDeltaTime);
            currentDashes = 0;
            anim.SetBool("climbing", isClimbing);
            anim.SetFloat("velocityY", Mathf.Abs(controlThrowVertical));
        }
        
    }
    
    void RaycastTest()
    {

        RaycastHit2D[] test = Physics2D.RaycastAll(rb.position, Vector2.right, 10);
        foreach (RaycastHit2D x in test)
        {
            if (!x.collider.Equals(myCollider))
            {
                Destroy(x.collider.gameObject);
                break;
            }
        }
    }

    void isGrounded()
    {

        //RaycastHit2D[] hits = Physics2D.RaycastAll(rb.position, Vector2.down, dist + offset);
        RaycastHit2D hit = Physics2D.Raycast(rb.position, Vector2.down, dist + offset);
        //Vector3 xyz = new Vector3(transform.position.x, transform.position.y - (dist + offset), transform.position.z);
        //Debug.DrawLine(transform.position, xyz, Color.red);
        //Collider2D couldBeGround = null;
        if (hit.collider != null)
        {
            grounded = true;
            currentDashes = 0;
            midAirJumps = 0;
            if (hit.collider.tag == "platformLauncher")
            {
                hit.collider.GetComponent<PlatformLauncher>().StartMoving();
            }
            else if (hit.collider.tag == "platform")
            {
                transform.SetParent(hit.transform);
            }
            Rigidbody2D a = hit.collider.GetComponent<Rigidbody2D>();
            if (a != null)
            {
                momentum = new Vector2(a.velocity.x, 0);
            }
            else // meaning there's no rigidbody on the thing you landed on, meaning it shouldn't carry any velocity
            {
                momentum = new Vector2(0, 0);
            }
        }

        else
        {
            grounded = false;
            transform.SetParent(null);
        }
        
        anim.SetBool("grounded", grounded);

    }

    void PlayerMove()
    {
        horizontalMove = CrossPlatformInputManager.GetAxisRaw("Horizontal");
        if (horizontalMove > 0.2f) horizontalMove = 1f;
        else if (horizontalMove < -0.2f) horizontalMove = -1f;
        else horizontalMove = 0f;
        horizontalMove *= moveSpeed;

        if (isClimbing) { return; }

        Flip(); // flips character depending on horizontal input
        JumpAndFall(); // jump and fall animation

        anim.SetFloat("speed", Mathf.Abs(horizontalMove));

        if (isCrouching && grounded)
        {
            horizontalMove *= crouchSpeedModifier;
        }

        if (!grounded) // slow velocity down while in air (X VELOCITY ONLY)
        {
            momentum *= momentumDecreaseFactor;
        }

        rb.velocity = new Vector2(horizontalMove * Time.fixedDeltaTime, rb.velocity.y) + momentum;

    }

    private void Jump()
    {
        if (!(midAirJumps < totalMidAirJumps || grounded)) return;
        float jump = jumpSpeed;
        audioSource.clip = jumpSFX;

        if (isClimbing)
        {
            isClimbing = false;
            rb.gravityScale = initialGravity;
            rb.velocity = new Vector2(rb.velocity.x, jump);
            audioSource.Play();
            return;
        }

        else if (!grounded && !isClimbing)
        {
            audioSource.clip = midAirSFX;
            midAirJumps++;
            Vector3 a = new Vector3(transform.position.x, transform.position.y - .5f);
            GameObject fx = Instantiate(midAirFX, a, Quaternion.identity);
            Destroy(fx, .5f);
        }

        rb.velocity = new Vector2(rb.velocity.x, jump);
        audioSource.Play();
    }

    private void CrouchCheck()
    {
        if (CrossPlatformInputManager.GetAxisRaw("Vertical") < -0.8f && grounded)
        {
            isCrouching = true;
            anim.SetBool("crouch", isCrouching);
        }
        else
        {
            isCrouching = false;
            anim.SetBool("crouch", isCrouching);
        }
        
    }

    void JumpAndFall ()
    {
        anim.SetFloat("velocityY", rb.velocity.y);
    }

    void Flip()
    {
        if (horizontalMove < 0)
        {
            sprite.flipX = true;
        }
        else if (horizontalMove > 0)
        {
            sprite.flipX = false;
        }
    }
}
