using System;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    // Start is called before the first frame update
    public Action<Vector2> OnMoveInput;

    public Action<bool> OnSprintInput;
    public Action OnJumpInput;
    public Action OnClimbInput;
    public Action OnCancelClimb;
    public Action OnChangePOV;
    public Action OnCrouchInput;
    public Action OnGlideInput;
    public Action OnCancelGlide;
    public Action OnPunchInput;

    private void Update()
    {
        CheckMovementInput();
        CheckCancelInput();
        CheckChangePOVInput();
        CheckClimbInput();
        CheckCrouchInput();
        CheckGlideInput();
        CheckJumpInput();
        CheckMainMenuInput();
        CheckPunchInput();
        CheckSprintInput();
    }

    //=>All Inputs
    //Check Movement
    private void CheckMovementInput()
    {
        float verticalAxis = Input.GetAxis("Vertical");
        float horizontalAxis = Input.GetAxis("Horizontal");
        Vector2 inputAxis = new Vector2(horizontalAxis, verticalAxis);
        if (OnMoveInput != null){
            OnMoveInput (inputAxis);
        }
        //Debug.Log("Vertical Axis: " + verticalAxis);
        //Debug.Log("Horizontal Axis: "+ horizontalAxis);
    }

    //Check Sprint
    private void CheckSprintInput()
    {
        bool isHoldSprintInput = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (isHoldSprintInput){
            if (OnSprintInput != null){
            OnSprintInput(true);
            }
            
            Debug.Log("Sprinting");
        }
        else{
            if (OnSprintInput != null){
                OnSprintInput(false);
            }
            
            Debug.Log("Not Sprinting");
        }
    }

    //Check Jump
    private void CheckJumpInput()
    {
        bool isPressJumpInput = Input.GetKeyDown(KeyCode.Space);
        if (isPressJumpInput){
            if (OnJumpInput != null){
                OnJumpInput();
            }
        }
    }

    //Check Crouch
    private void CheckCrouchInput()
    {
        bool isPressCrouchInput = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
        if (isPressCrouchInput){
            OnCrouchInput();
            Debug.Log("Crouch");
        }
    }
    
    //Check POV
    private void CheckChangePOVInput()
    {
        bool isPressChangePOVInput = Input.GetKeyDown(KeyCode.Q);
        if (isPressChangePOVInput){
            if (OnChangePOV != null){
                OnChangePOV();
            }
            Debug.Log("Change POV");
        }
    }

    //Check Climb
    private void CheckClimbInput()
    {
        bool isPressClimbInput = Input.GetKeyDown(KeyCode.E);
        if (isPressClimbInput){
            OnClimbInput();
            Debug.Log("Climb");
        }
    }

    private void CheckGlideInput()
    {
        bool isPressGlideInput = Input.GetKeyDown(KeyCode.G);
        if (isPressGlideInput){
            if (OnGlideInput != null){
                OnGlideInput();
                Debug.Log("Glide");
            }
        }
    }

    //Check Cancel Climb
    private void CheckCancelInput()
    {
        bool isPressCancelInput = Input.GetKeyDown(KeyCode.C);
        if (isPressCancelInput){
            if (OnCancelClimb != null){
                OnCancelClimb();
                Debug.Log("Cancel Climb or Glide");
            }
            if (OnCancelGlide != null){
                OnCancelGlide();
                Debug.Log("Cancel Climb or Glide");
            }
        }
    }

    //Check Punch
    private void CheckPunchInput(){
        bool isPressPunchInput = Input.GetKeyDown(KeyCode.Mouse0);
        if (isPressPunchInput){
            OnPunchInput();
            Debug.Log("Punch");
        }
    }

    //Check Main Menu
    private void CheckMainMenuInput()
    {
        bool isPressMainMenuInput = Input.GetKeyDown(KeyCode.Escape);
        if (isPressMainMenuInput){
            Debug.Log("Back to Main Menu");
        }
    }
}
