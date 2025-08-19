using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

namespace Claymore {
	public class PlayerController : MonoBehaviour {

		#region Statics & Constants

		// Claymore Animator Params
		const string WEAPON_ATTACKING = "Attacking";
		const string WEAPON_IDLE_FLOURISH = "Idle";
		const string WEAPON_BLOCKING = "Blocking";
		const string WEAPON_THROW = "Throw";
		const string WEAPON_RESET = "ReturnSword";
		const string WEAPON_EMBED = "Embed";

		// Input stuff
		const float BASE_CAM_SENSITIVITY = 0.1f;
		#endregion

		#region Inspector Vars
		[Header("References")]
		[SerializeField] Rigidbody rbody;

		[SerializeField] GameObject cameraParent;

		[SerializeField] ClaymoreWeapon claymoreObj;
		[SerializeField] Animator claymoreAnimator;

		[SerializeField] SphereCollider sphereCastGroundTarget;

		[SerializeField] VisualEffect warpSlamVFXPrefab;

		[Header("Input Action References")]
		[SerializeField] InputActionReference primaryAttackAction;
		[SerializeField] InputActionReference altAttackAction;
		[SerializeField] InputActionReference moveAction;
		[SerializeField] InputActionReference lookAction;
		[SerializeField] InputActionReference jumpAction;

		[Header("Input Parameters")]
		[SerializeField] float cameraSensitivity = 1;
		[SerializeField] bool invertCamera = true;
		
		[Header( "Parameters" )]
		[SerializeField] float maxSpeed;
		[SerializeField] float jumpForce;
		[SerializeField] float airAccelScalar;

		[SerializeField] float cameraMaxPitchDegrees = 85;

		[SerializeField] Vector3 playerGravityAccel = new(0, -5, 0);

		[SerializeField] AnimationCurve warpPositionCurve;
		#endregion

		#region Local Vars
		// Movement
		Vector3 velocityVector = new();
		Vector2 moveVector;
		bool willJump;

		float curCameraPitch = 0;
		float curCameraAngle;

		// Claymore movement stuff
		bool swordEmbeddedMovement = false;
		Vector3 endPos, startPos;
		float travelTime;
		VisualEffect mostRecentVFX;

		// Input
		bool isAttacking, isBlocking, isMoving, isLooking;

		#endregion

		#region Parameters/Getters
		public Vector3 FlattenedCameraForward { get {
				Vector3 projectedVector = cameraParent.transform.forward;
				projectedVector.y = 0;
				return projectedVector.normalized;
			} }

		public Vector3 FlattenedCameraRight { get {
				Vector3 projectedVector = cameraParent.transform.right;
				projectedVector.y = 0;
				return projectedVector.normalized;
			} }

		public Vector3 PlayerLateralMovement { get {
				Vector3 retVector = rbody.linearVelocity;
				retVector.y = 0;
				return retVector;
			}
		}

		private bool IsGrounded { get {
				if ( Physics.SphereCast(transform.position, sphereCastGroundTarget.radius, Vector3.down,
						out RaycastHit info, Vector3.Distance( transform.position, sphereCastGroundTarget.transform.position ), 1 << 7)) {
					// TODO: Do we need to check for anything else? Ground material or something?
					return true;
				}
				return false;
			} }

		private ESwordEmbedState GetSwordEmbedState { get => claymoreObj.embedState; }
		#endregion

		#region Mono Implementation
		private void Start() {
			// Input handling
			AddInputListeners();

			// Hide and lock mouse cursor.
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;

			// Set rigidbody values.
			rbody.maxLinearVelocity = Mathf.Infinity;
			rbody.maxDepenetrationVelocity = Mathf.Infinity;

			// Get initial cam angle.
			curCameraAngle = cameraParent.transform.rotation.eulerAngles.y;

		}

		private void FixedUpdate() {
			// Sword warp movement.
			if ( swordEmbeddedMovement ) {
				DoSwordWarpMovement();
				return;
			}

			// Normal movement
			DoMovement();
		}

		//private void Update() { }

		private void OnDestroy() {
			// Input handling
			RemoveInputListeners();
		}
		#endregion

		#region Input Events
		/// PRIMARY ATTACK
		private void OnPrimaryAttackPressed( InputAction.CallbackContext context ) {
			// TODO: Implement damage features.

			// Set bool.
			isAttacking = true;

			// Animator trigger.
			if ( claymoreAnimator ) {
				// Check if we're already blocking, because then we throw instead of attack.
				if ( isBlocking ) {
					claymoreAnimator.SetTrigger( WEAPON_THROW );
				} else {
					claymoreAnimator.SetBool( WEAPON_ATTACKING , isAttacking );
				}
			}
		}
		private void OnPrimaryAttackReleased( InputAction.CallbackContext context ) {
			// TODO: Implement damage features.

			// Set bool.
			isAttacking = false;

			// Animator trigger.
			if ( claymoreAnimator ) {
				claymoreAnimator.SetBool( WEAPON_ATTACKING , isAttacking );
			}
		}

		/// ALT ATTACK (Block)
		private void OnAltAttackPressed( InputAction.CallbackContext context ) {
			// TODO: Implement block features.

			// Set bool.
			isBlocking = true;

			// Animator trigger.
			if ( claymoreAnimator ) {
				claymoreAnimator.SetBool( WEAPON_BLOCKING , isBlocking );
			}
		}
		private void OnAltAttackReleased( InputAction.CallbackContext context ) {
			// TODO: Implement block features.

			// Set bool.
			isBlocking = false;

			// Animator trigger.
			if ( claymoreAnimator ) {
				claymoreAnimator.SetBool( WEAPON_BLOCKING , isBlocking );
			}
		}

		/// JUMP
		private void OnJumpPressed( InputAction.CallbackContext context ) {
			// Set willJump to jump on next call of movement handler. TODO: Additional logic to prevent setting in edge cases?
			willJump = true;
		}
		private void OnJumpReleased( InputAction.CallbackContext context ) {
			// Simply don't jump.
			willJump = false;
		}

		/// MOVE
		private void OnMovePressed( InputAction.CallbackContext context ) {
			// Update the input move vectors for movement to check.
			moveVector = context.ReadValue<Vector2>();

			// Set bool.
			isMoving = true;
		}
		private void OnMoveReleased( InputAction.CallbackContext context ) {
			// Zero out the input move vector.
			moveVector *= 0;

			// Set bool.
			isMoving = false;
		}

		/// LOOK 
		private void OnLookPressed( InputAction.CallbackContext context ) {
			Vector2 lookVectors = context.ReadValue<Vector2>();

			// Adjust camera based on lookVectors supplied.
			curCameraAngle += lookVectors.x * BASE_CAM_SENSITIVITY * cameraSensitivity;
			curCameraPitch += lookVectors.y * BASE_CAM_SENSITIVITY * cameraSensitivity * (invertCamera ? -1 : 1);
			// Clamp camera pitch.
			curCameraPitch = Mathf.Clamp( curCameraPitch , -cameraMaxPitchDegrees , cameraMaxPitchDegrees );

			// Set angle of camera.
			cameraParent.transform.rotation = Quaternion.Euler( curCameraPitch , curCameraAngle , 0 );

			// Set bool.
			isLooking = true;
		}
		private void OnLookReleased( InputAction.CallbackContext context ) {
			// Do nothing

			// Set bool.
			isLooking = false;
		}

		#region Input Action Listener Setting
		void AddInputListeners() {
			if ( primaryAttackAction ) {
				primaryAttackAction.action.Enable();
				primaryAttackAction.action.performed += OnPrimaryAttackPressed;
				primaryAttackAction.action.canceled += OnPrimaryAttackReleased;
			}

			if ( altAttackAction ) {
				altAttackAction.action.Enable();
				altAttackAction.action.performed += OnAltAttackPressed;
				altAttackAction.action.canceled += OnAltAttackReleased;
			}

			if ( moveAction ) {
				moveAction.action.Enable();
				moveAction.action.performed += OnMovePressed;
				moveAction.action.canceled += OnMoveReleased;
			}

			if ( lookAction ) {
				lookAction.action.Enable();
				lookAction.action.performed += OnLookPressed;
				lookAction.action.canceled += OnLookReleased;
			}

			if ( jumpAction ) {
				jumpAction.action.Enable();
				jumpAction.action.performed += OnJumpPressed;
				jumpAction.action.canceled += OnJumpReleased;
			}
		}

		void RemoveInputListeners() {
			if ( primaryAttackAction ) {
				primaryAttackAction.action.performed -= OnPrimaryAttackPressed;
				primaryAttackAction.action.canceled -= OnPrimaryAttackReleased;
			}

			if ( altAttackAction ) {
				altAttackAction.action.performed -= OnAltAttackPressed;
				altAttackAction.action.performed -= OnAltAttackReleased;
			}

			if ( moveAction ) {
				moveAction.action.performed -= OnMovePressed;
				moveAction.action.canceled -= OnMoveReleased;
			}

			if ( lookAction ) {
				lookAction.action.performed -= OnLookPressed;
				lookAction.action.canceled -= OnLookReleased;
			}

			if ( jumpAction ) {
				jumpAction.action.performed -= OnJumpPressed;
				jumpAction.action.canceled -= OnJumpReleased;
			}
		}
		#endregion
		#endregion

		#region Events
		#endregion

		#region Helpers
		/// <summary>
		/// Handle all the frame by frame movement handling.
		/// </summary>
		private void DoMovement() {
			// Always apply gravity.
			rbody.linearVelocity += playerGravityAccel * Time.fixedDeltaTime;

			// Basic forward & right add forces.
			if ( IsGrounded ) {
				//rbody.AddForce( groundAccelScalar * moveVector.y * Time.fixedDeltaTime * FlattenedCameraForward, ForceMode.Acceleration );
				//rbody.AddForce( groundAccelScalar * moveVector.x * Time.fixedDeltaTime * FlattenedCameraRight, ForceMode.Acceleration );

				// While grounded, set velocity directly.
				velocityVector = (maxSpeed * moveVector.y * FlattenedCameraForward) + (maxSpeed * moveVector.x * FlattenedCameraRight) + new Vector3(0, rbody.linearVelocity.y, 0);

				// If jumping, overwrite y value for velocity vector.
				if ( willJump ) {
					//rbody.AddForce( jumpForce * Vector3.up , ForceMode.VelocityChange );
					velocityVector.y = jumpForce;
					willJump = false;
				}

				// Set rigidbody velocity.
				rbody.linearVelocity = velocityVector;
			} else {
				// Add velocity in a direction as long as it's not a similar direction to current trajectory.
				Vector3 desiredAirMovement = (moveVector.y * airAccelScalar * FlattenedCameraForward) + ( moveVector.x * airAccelScalar * FlattenedCameraRight);
				if ( desiredAirMovement.NormalizedDot(PlayerLateralMovement) < 0.8f ) {
					rbody.linearVelocity += desiredAirMovement;
				}
			}
		}

		/// <summary>
		/// Handle the movement handling for sword warping.
		/// </summary>
		private void DoSwordWarpMovement() {
			// This is in fixed update so
			travelTime += Time.fixedDeltaTime;

			Vector3 nextFramePos = Vector3.Lerp( startPos, claymoreObj.transform.position, warpPositionCurve.Evaluate(travelTime + Time.fixedDeltaTime ) );

			//rbody.linearVelocity = Vector3.zero;
			//rbody.MovePosition( Vector3.Lerp(startPos, claymoreObj.transform.position, warpPositionCurve.Evaluate(travelTime) ) );
			//transform.position = Vector3.Lerp( startPos , endPos , warpPositionCurve.Evaluate( travelTime ) );
			rbody.linearVelocity = (nextFramePos - transform.position) / Time.fixedDeltaTime;

			if ( travelTime >= 1 ) {
				// Cancel warp movement.
				swordEmbeddedMovement = false;

				// Rigidbody normal.
				rbody.isKinematic = false;
				rbody.linearVelocity = Vector3.zero;

				// Animator.
				claymoreAnimator.enabled = true;
				claymoreAnimator.SetTrigger( WEAPON_RESET );

				// Weapon.
				claymoreObj.ReleaseSword();

				// VFX.
				if ( mostRecentVFX ) {
					mostRecentVFX.SetFloat( "ParticleBlastScalar" , GetSwordEmbedState == ESwordEmbedState.Ground ? 8f : 1.4f );
					mostRecentVFX.SetFloat( "ParticleBlastRadius" , 3.2f );
					mostRecentVFX.Play();
					Destroy( mostRecentVFX.gameObject , 6);
				}

				// TODO: Launch player/enable special actions.

			}
		}

		/// <summary>
		/// Called from the sword after it has embedded in something after a throw, will change movement
		/// behaviour and dictate the actions the player can take afterwards.
		/// </summary>
		/// <param name="embedState">The embed state the sword is in, dictates possible behaviours.</param>
		public void EmbedSwordSetup( ) {
			swordEmbeddedMovement = true;
			travelTime = 0f;
			startPos = transform.position;
			//endPos = claymoreObj.transform.position;
			//rbody.isKinematic = true;
			claymoreAnimator.SetTrigger( WEAPON_EMBED );

			// TODO: Different potential behaviours based on embed state.
		}

		/// <summary>
		/// Instantiate and prep VFX for slamming to a desired position and rotation, called
		/// from the sword when it has an impact point.
		/// </summary>
		/// <param name="position">The world position to move the vfx object to.</param>
		/// <param name="newForward">The new forward direction to set for the vfx object.</param>
		public void MoveSlamVFX( Vector3 position, Vector3 newForward ) {
			if ( warpSlamVFXPrefab ) {
				mostRecentVFX = Instantiate( warpSlamVFXPrefab , position , Quaternion.LookRotation( newForward ) );
				mostRecentVFX.SetFloat( "ParticleBlastScalar" , GetSwordEmbedState == ESwordEmbedState.Ground ? 4f : 0.6f );
				mostRecentVFX.SetFloat( "ParticleBlastRadius" , .8f );
				mostRecentVFX.Play();
			}
		}
		#endregion

		#region Coroutines
		#endregion

	}
}