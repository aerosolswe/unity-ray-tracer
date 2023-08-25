using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class Movement : MonoBehaviour {

	// Movement velocity
	[HideInInspector]
	public Vector3 velocity = new Vector3();

	// Components
	private CollisionFlags collisionFlags;
	private CharacterController controller;

	// Variables to track velocity, jump length etc
    public float topVelocity 			= 0.0f;
	public float jumpLength 			= 0.0f;
	public float currentVelocity		= 0.0f;
	public float currentJumpLength		= 0.0f;

	// Ground movement speed
    public float moveSpeed 				= 7.0f;
	// Slow down acceleration when colliding
	public float slowDownSpeed			= 10f;
	// Ground acceleration
    public float runAcceleration 		= 14f;
	// Ground deacceleration
    public float runDeacceleration		= 10f;
	// Air acceleration
    public float airAcceleration 		= 0.15f;
	// Air deacceleration when counter strafing
    public float airDeacceleration 		= 2.0f;
	// Air control
    public float airControl 			= 1.0f;
	// Sidestrafe accelertion
    public float sideStrafeAcceleration = 50f;
	// Sidestrafe max speed
    public float sideStrafeSpeed 		= 1.0f;
	// Jump speed on the Y-Axis (0, 1, 0)
    public float jumpSpeed 				= 7.0f;  // The speed at which the character's up axis gains when hitting jump
	// Move speed scale
	public float moveScale 				= 1.0f;
	// Walk speed scale (moveSpeed * walkScale)
	public float walkScale 				= 0.4f;

	// Pull down factor
    public float gravity = 20.0f;
	// Ground friction
    public float friction = 6;  

	// Player heights (gets controller height)
	private float height;
	// Player crouch height
	public float crouchHeight = 0.75f;
	// Is the player walking?
	public bool isWalking;
	// Is the played crouching?
	public bool isCrouching;
	// If you release crouch button it checks if something is above
	private bool wantToStand;

	// Do we wish to jump?
    private bool wishJump;

	[Tooltip("Name of the Horizontal button in the input manager")]
	public string horizontalButton = "Horizontal";
	[Tooltip("Name of the Vertical button in the input manager")]
	public string verticalButton = "Vertical";

	[Tooltip("Name of the Jump button in the input manager")]
	public string jumpButton = "Jump";
	[Tooltip("Name of the Crouch button in the input manager")]
	public string crouchButton = "Crouch";
	[Tooltip("Name of the Walk button in the input manager")]
	public string walkButton = "Walk";

	// Initialize controller and height
    public void Start() {
        controller = GetComponent<CharacterController>();
		height = controller.height;
    }

	// If player holds jump button, queue jump
    private void QueueJump() {
		if(Input.GetButtonDown(jumpButton) && !wishJump)
            wishJump = true;
		if(Input.GetButtonUp(jumpButton))
            wishJump = false;
    }

	public void Update() {
		// Get input from player
		GetInput();

		// Queue jump
		QueueJump();

		if (controller.isGrounded) {
			// Ground movement
			GroundMove ();
		} else if (!controller.isGrounded) {
			// Air movement
			AirMovement();
			currentJumpLength += velocity.magnitude;
		}

		// Get collisions from moving
        collisionFlags = controller.Move(velocity * Time.deltaTime);

        // Calculate top velocity
        var udp = velocity;
        udp.y = 0.0f;
        currentVelocity = velocity.magnitude;
        if(currentVelocity > topVelocity)
            topVelocity = currentVelocity;

		// Calculate latest jump length
		if(controller.isGrounded) {
			if(currentJumpLength != 0) {
				jumpLength = currentJumpLength;
				currentJumpLength = 0;
			}
		}
			
    }
    
	// Calculates the air movement
    private void AirMovement() {
        Vector3 wishdir;
        float wishvel = airAcceleration;
        float accel;

        var scale = CmdScale();

		// Get direction from input
        wishdir = new Vector3(horizontal, 0, vertical);
        wishdir = transform.TransformDirection(wishdir);
        wishdir.y = 0;
        wishdir.Normalize();

		// Set the movement speed
        var wishspeed = wishdir.magnitude;
        wishspeed *= moveSpeed;

		// Normalize the direction
        wishdir.Normalize();
		// Scale the speed
        wishspeed *= scale;

		// If we are walking/Croucing multiply with walkscale
		if(isWalking || isCrouching)
			wishspeed *= walkScale;

		// Determine whether we should accelerate or deaccelerate
        var wishspeed2 = wishspeed;
        if(Vector3.Dot(velocity, wishdir) < 0)
            accel = airDeacceleration;
        else
            accel = airAcceleration;

		// Determine whether we are strafing or not
        if(vertical == 0 && horizontal != 0) {
            if(wishspeed > sideStrafeSpeed)
                wishspeed = sideStrafeSpeed;
            accel = sideStrafeAcceleration;
        }

		// Accelerate
        Accelerate(wishdir, wishspeed, accel);

		// Aircontrol
        if(airControl > 0)
            AirControl(wishdir, wishspeed2);

		// Apply gravity
		velocity.y -= gravity * Time.deltaTime;
    }

    private void AirControl(Vector3 wishdir, float wishspeed) {
        float yspeed;
        float speed;
        float dot;
        float k;
        int i;

		yspeed = velocity.y;
        velocity.y = 0;

        speed = velocity.magnitude;
        velocity.Normalize();

        dot = Vector3.Dot(velocity, wishdir);
        k = 32;
        k *= airControl * dot * dot * Time.deltaTime;

        // Change direction while slowing down
        if(dot > 0) {
            velocity.x = velocity.x * speed + wishdir.x * k;
            velocity.y = velocity.y * speed + wishdir.y * k;
            velocity.z = velocity.z * speed + wishdir.z * k;

            velocity.Normalize();
        }

        velocity.x *= speed;
		velocity.y = yspeed;
        velocity.z *= speed;

    }

    private void GroundMove() {
        Vector3 wishdir;
        Vector3 wishvel;


        if(!wishJump)
            ApplyFriction(1.0f);
        else
            ApplyFriction(0);

        var scale = CmdScale();

        wishdir = new Vector3(horizontal, 0, vertical);
        wishdir = transform.TransformDirection(wishdir);
        wishdir.y = 0;
        wishdir.Normalize();

        var wishspeed = wishdir.magnitude;
        wishspeed *= moveSpeed;

		if(isWalking || isCrouching)
			wishspeed *= walkScale;

        Accelerate(wishdir, wishspeed, runAcceleration);

        // Jumping
        if(wishJump) {
            velocity.y = jumpSpeed;
            wishJump = false;
		}

		velocity.y -= 1 * Time.deltaTime;
    }

	public void ApplyForce(Vector3 force) {
		velocity += force;
	}

    private void ApplyFriction(float scale) {
        Vector3 vec = velocity;
        float vel;
        float speed;
        float newspeed;
        float control;
        float drop;

        vec.y = 0.0f;
        speed = vec.magnitude;
        drop = 0.0f;

        /* Only if the player is on the ground then apply friction */
        if(controller.isGrounded) {
            control = speed < runDeacceleration ? runDeacceleration : speed;
			drop = control * friction * Time.deltaTime * scale;
        }

        newspeed = speed - drop;
        if(newspeed < 0)
            newspeed = 0;
        if(speed > 0)
            newspeed /= speed;

        velocity.x *= newspeed;
        velocity.z *= newspeed;
    }

    private void Accelerate(Vector3 wishdir, float wishspeed, float accel) {
        float addspeed;
        float accelspeed;
        float currentspeed;

        currentspeed = Vector3.Dot(velocity, wishdir);
        addspeed = wishspeed - currentspeed;
        if(addspeed <= 0)
            return;
        accelspeed = accel * Time.deltaTime * wishspeed;
        if(accelspeed > addspeed)
            accelspeed = addspeed;

        velocity.x += accelspeed * wishdir.x;
        velocity.z += accelspeed * wishdir.z;
    }

    private float CmdScale() {
        int max = 0;
        float total;
        float scale;

        max = (int)Mathf.Abs(vertical);
        if(Mathf.Abs(horizontal) > max)
            max = (int)Mathf.Abs(horizontal);
        if(max == 0)
            return 0;

        total = Mathf.Sqrt(vertical * vertical + horizontal * horizontal);
        scale = moveSpeed * max / (moveScale * total);

        return scale;
    }
    
    [HideInInspector]
    public float horizontal = 0;
    [HideInInspector]
    public float vertical = 0;

    private void GetInput() {

		// Mouse Input
		horizontal = Input.GetAxisRaw(horizontalButton);
        vertical = Input.GetAxisRaw(verticalButton);

		// Walking
		if(Input.GetButtonDown(walkButton)) {
			isWalking = true;
		}

		if(Input.GetButtonUp(walkButton)) {
			isWalking = false;
		}

		// Croucing
		if(Input.GetButtonDown(crouchButton)) {
			isCrouching = true;
			controller.height = crouchHeight;
			controller.center = controller.center - new Vector3(0, crouchHeight/2, 0);
		}

		if(Input.GetButtonUp(crouchButton)) {
			wantToStand = true;
		}

		// Checks if player can stand
		if(wantToStand) {
			InvokeRepeating("CanIStand", 0, 0.25f);
		}
	}

	// If nothing is above the player we can stand again
	private void CanIStand() {
		RaycastHit hit;
		bool hitSomething = Physics.Raycast(transform.position, Vector3.up, out hit, 1.25f);

		if(!hitSomething) {
			controller.height = height;
			controller.center = new Vector3(0, 0, 0);
			isCrouching = false;
			wantToStand = false;
			CancelInvoke("CanIStand");
		}
	}

    private void OnControllerColliderHit(ControllerColliderHit hit) {
		// If we hit our head, bounce down
		if (collisionFlags == CollisionFlags.Above) {
			velocity = new Vector3(velocity.x, -(gravity/10), velocity.z);
		}

		// If we collide with something, apply directional force
		if (collisionFlags == CollisionFlags.Sides) {
			float accel;
			Vector3 wishdir = hit.normal;

			var wishspeed = wishdir.magnitude;
			wishspeed *= slowDownSpeed;
			
			wishdir.Normalize();

			var wishspeed2 = wishspeed;
			if(Vector3.Dot(velocity, wishdir) < 0)
				accel = airDeacceleration;
			else
				accel = airAcceleration;

			if(vertical == 0 && horizontal != 0) {
				if(wishspeed > sideStrafeSpeed)
					wishspeed = sideStrafeSpeed;
				accel = sideStrafeAcceleration;
			}
			
			Accelerate(wishdir, wishspeed, accel);
		}
    }

}