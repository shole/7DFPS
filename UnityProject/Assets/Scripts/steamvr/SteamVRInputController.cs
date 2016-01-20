using UnityEngine;
using System.Collections;

// Require a character controller to be attached to the same game object
public class SteamVRInputController : MonoBehaviour {
    private CharacterMotor motor;

    private float forward_input_delay = 10.0f;
    private float old_vert_axis = 0.0f;
    private bool running = false;

    private SteamVR_Controller.Device Controller;

    // Use this for initialization
    void Awake() {
        motor = transform.parent.GetComponent<CharacterMotor>();
    }

    // Update is called once per frame
    void Update() {
        if (Controller == null) {
            Controller = SteamVR_Controller.Input((int)GetComponent<SteamVR_TrackedObject>().index);
            return;
        }

        // Get the input vector from kayboard or analog stick
        Vector3 directionVector;

        bool hydraJump = false;
        Vector2 touchpadaxis = Controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_Axis0);
        directionVector = new Vector3(touchpadaxis.x, 0, touchpadaxis.y);
        hydraJump = Controller.GetPress(Valve.VR.EVRButtonId.k_EButton_Grip);

        if (old_vert_axis < 0.9f && Input.GetAxis("Vertical") >= 0.9f) {
            if (forward_input_delay < 0.4f && !transform.parent.GetComponent<AimScript>().IsAiming()) {
                motor.SetRunning(Mathf.Clamp((0.4f - forward_input_delay) / 0.2f, 0.01f, 1.0f));
                running = true;
            }
            forward_input_delay = 0.0f;
        }
        forward_input_delay += Time.deltaTime;
        if (forward_input_delay > 0.4f || transform.parent.GetComponent<AimScript>().IsAiming()) {
            motor.SetRunning(0.0f);
            running = false;
        }
        if (running) {
            directionVector.z = 1.0f;
        }
        old_vert_axis = Input.GetAxis("Vertical");

        if (directionVector != Vector3.zero) {
            // Get the length of the directon vector and then normalize it
            // Dividing by the length is cheaper than normalizing when we already have the length anyway
            var directionLength = directionVector.magnitude;
            directionVector = directionVector / directionLength;

            // Make sure the length is no bigger than 1
            directionLength = Mathf.Min(1, directionLength);

            // Make the input vector more sensitive towards the extremes and less sensitive in the middle
            // This makes it easier to control slow speeds when using analog sticks
            directionLength = directionLength * directionLength;

            // Multiply the normalized direction vector by the modified length
            directionVector = directionVector * directionLength;
        }

        // Apply the direction to the CharacterMotor
        motor.inputMoveDirection = transform.rotation * directionVector;
        motor.inputJump = Input.GetButton("Jump") || hydraJump;
    }
}