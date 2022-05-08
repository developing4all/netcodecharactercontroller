using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using Unity.Netcode;

namespace UnityStandardAssets.Characters.ThirdPerson
{
    [RequireComponent(typeof(NetThirdPersonCharacter), typeof(PlayerInput))]
    public class NetThirdPersonUserControl : NetworkBehaviour
    {
        public Transform m_FollowTarget;
        public Transform m_LookTarget;
        private NetThirdPersonCharacter m_Character; // A reference to the NetThirdPersonCharacter on the object
        private Transform m_Cam;                  // A reference to the main camera in the scenes transform
        private Vector3 m_Move;
        private bool m_Jump;                      // the world-relative desired move direction, calculated from the camForward and user input.
        private bool m_Crouch;

        // The new input system
        public PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction jumpAction;
        private InputAction crouchAction;


        private CinemachineVirtualCameraBase m_VCam;

        public override void OnNetworkSpawn()
        {
            //PlayerInput playerInput = GetComponent<PlayerInput>();
            if (!IsClient || !IsOwner)
            {
                playerInput.enabled = false;
                enabled = false;
                return;
            }

            m_Cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
            m_VCam = m_Cam.GetComponent<CinemachineBrain>().ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCameraBase>(); // this is fine since it only happens once.
            m_VCam.Follow = m_FollowTarget;
            m_VCam.LookAt = m_LookTarget;

            // Added new input support
            playerInput.enabled = true;
            moveAction = playerInput.actions["Move"];
            lookAction = playerInput.actions["Look"];
            sprintAction = playerInput.actions["Sprint"];
            jumpAction = playerInput.actions["Jump"];
            crouchAction = playerInput.actions["Crouch"];
        }

        private void Start()
        {
            m_Character = GetComponent<NetThirdPersonCharacter>();
            m_Crouch = false;
        }

        private void Update()
        {
            // Prevent some issues if we run more than one client on the same machine
            // by not getting input from non focussed game windows
            if(!playerInput.enabled || !Application.isFocused ) return;

            if (!m_Jump)
            {
                m_Jump = jumpAction.triggered;
            }
            CheckCrouch();
        }

        private void CheckCrouch()
        {
            bool crouch = crouchAction.triggered;

            // Are we already crouching? then stand up
            if(crouch && m_Crouch)
            {
                m_Crouch = false;
            }
            // Otherwise crouch
            else if(crouch)
            {
                m_Crouch = true;
            }
        }
        
        // Fixed update is called in sync with physics
        private void FixedUpdate()
        {
            // Prevent some issues if we run more than one client on the same machine
            // by not getting input from non focussed game windows
            if(!playerInput.enabled || !Application.isFocused) return;

            // read inputs
            Vector2 moveIn = moveAction.ReadValue<Vector2>();
            m_Move.x = moveIn.x;
            m_Move.z = moveIn.y;

            m_Move.Normalize();

            // calculate move direction to pass to character
            if (m_Cam != null)
            {
                // calculate camera relative direction to move:
                //m_CamForward = Vector3.Scale(m_Cam.forward, new Vector3(1, 0, 1)).normalized;
                m_Move = m_Move.z * m_Cam.forward + m_Move.x * m_Cam.right;
            }
            else
            {
                // we use world-relative directions in the case of no main camera
                m_Move = m_Move.z * Vector3.forward + m_Move.x * Vector3.right;
            }
            
            /* vvvvv fix this later vvvvv */
            m_Move *= 0.5f;
#if !MOBILE_INPUT
            // walk speed multiplier
            if (sprintAction.IsPressed()) m_Move *= 2f;
#endif

            // pass all parameters to the character control script
            m_Character.Move(m_Move.x, m_Move.z, m_Crouch, m_Jump);
            m_Jump = false;
        }
    }
}
