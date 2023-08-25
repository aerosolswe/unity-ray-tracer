using UnityEngine;

public class MouseLook : MonoBehaviour {

	[Tooltip("Will rotate on the X-Axis")]
	public Transform parent;

	// Mouse sensitivity on the X-Axis
	public float sensitivityX = 5f;
	// Mouse sensitivity on the Y-Axis
	public float sensitivityY = 5f;

	// Minimum and maximum X angle
	public float minimumX = -360f;
	public float maximumX = 360f;

	// Minimum and maximum Y angle
	public float minimumY = -60f;
	public float maximumY = 60f;

	public KeyCode lockKey = KeyCode.Tab;

	private float rotationX = 0F;
	private float rotationY = 0F;

	private Quaternion originalRotation;

	private bool mouseLocked = false;

	void Start () {
		originalRotation = transform.localRotation;
	}

	void Update () {
		// Toggles mouselock for rapid testing
		if(Input.GetKeyDown(lockKey))
			ToggleMouseLock(!mouseLocked);

		// Mouse input
		rotationX += Input.GetAxisRaw("Mouse X") * sensitivityX;
		rotationY += Input.GetAxisRaw("Mouse Y") * sensitivityY;

		// Clamp the angles
		rotationX = ClampAngle (rotationX, minimumX, maximumX);
		rotationY = ClampAngle (rotationY, minimumY, maximumY);

		// Convert angle-axis to quaternions
		Quaternion xQuaternion = Quaternion.AngleAxis (rotationX, Vector3.up);
		Quaternion yQuaternion = Quaternion.AngleAxis (rotationY, Vector3.left);

		// Apply new rotations on this & parent transforms
		if(mouseLocked) {
			transform.localRotation = originalRotation * yQuaternion;
			parent.localRotation = originalRotation * xQuaternion;
		}
	}

	// Toggles mouse state (hidden & visible)
	public void ToggleMouseLock(bool hideMouse) {
		if(hideMouse) {
			mouseLocked = true;
			Cursor.lockState = CursorLockMode.Locked;
		} else {
			mouseLocked = false;
			Cursor.lockState = CursorLockMode.None;
		}

	}

	// Clamps an angle within min and max
	public float ClampAngle (float angle, float min, float max) {
		if (angle < -360F)
			angle += 360F;
		if (angle > 360F)
			angle -= 360F;
		
		return Mathf.Clamp (angle, min, max);
	}
}
