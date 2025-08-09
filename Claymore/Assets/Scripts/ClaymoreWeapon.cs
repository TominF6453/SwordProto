using UnityEngine;
using UnityEngine.Diagnostics;

namespace Claymore {

	#region Enums
	public enum ESwordEmbedState { None, Wall, Ground, Ceiling, Enemy };
	#endregion

	[RequireComponent( typeof( Rigidbody ) )]
	[RequireComponent( typeof( Animator ) )]
	public class ClaymoreWeapon : MonoBehaviour {
		#region Statics & Constants
		#endregion

		#region Inspector Vars
		[Header("References")]
		[SerializeField] MatchOrientation orientationMatchRef;
		[SerializeField] Transform embedPoint;
		//[SerializeField] Collider swordCollider;

		[Header( "Parameters" )]
		[SerializeField] float throwForceScalar = 10f;
		[SerializeField] float throwTime = 0.7f;
		#endregion

		#region Local Vars
		Rigidbody rb;
		Animator animator;
		PlayerController parentPlayerController;
		Transform localParent;

		bool wasThrown;
		bool isEmbedded;
		Vector3 storedThrowPos;
		Quaternion storedThrowRot;
		float embedDistance;

		public ESwordEmbedState embedState;
		#endregion

		#region Parameters/Getters
		#endregion

		#region Mono Implementation
		private void Awake() {
			// Initialize some local vars.
			rb = GetComponent<Rigidbody>();
			animator = GetComponent<Animator>();
			parentPlayerController = GetComponentInParent<PlayerController>();
			localParent = transform.parent;

			// Embed distance.
			embedDistance = Vector3.Distance( transform.position , embedPoint.position );

			// Make sure rigidbody is kinematic.
			rb.isKinematic = true;
		}

		private void FixedUpdate() {
			if ( wasThrown ) {
				wasThrown = false;
				transform.SetPositionAndRotation( storedThrowPos, storedThrowRot );
				rb.AddForce( throwForceScalar * transform.forward , ForceMode.Acceleration );
				//swordCollider.isTrigger = false;
			}
		}
		#endregion

		#region Events
		private void OnCollisionEnter( Collision collision ) {
			// Checking if it's the wall geometry layer.
			int collisionLayer = collision.collider.gameObject.layer;

			// DEBUG
			//Debug.LogWarning( $"Contact point position: {collision.GetContact(0).point} vs sword position: {transform.position}" );
			//Debug.LogWarning( $"Contact point normal: {collision.GetContact( 0 ).normal}" );
			//Debug.LogWarning( $"How many contact points? {collision.contactCount}" );

			switch (collisionLayer) {
				case 6: // Floor geo
					//Debug.Log( "Hit floor geo." );
					embedState = ESwordEmbedState.Ground;
					EmbedSword( collision.GetContact( 0 ).point , collision.GetContact( 0 ).normal , collision.transform );
					break;
				case 7: // Wall geo
					//Debug.Log( "Hit wall geo." );
					embedState = ESwordEmbedState.Wall;
					EmbedSword( collision.GetContact( 0 ).point , collision.GetContact( 0 ).normal, collision.transform );
					break;
				case 10: // Ceiling geo
					//Debug.Log( "Hit ceiling geo." );
					embedState = ESwordEmbedState.Ceiling;
					EmbedSword( collision.GetContact( 0 ).point , collision.GetContact( 0 ).normal , collision.transform );
					break;
				case 11: // Enemy
					//Debug.Log( "Hit enemy." );
					embedState = ESwordEmbedState.Enemy;
					EmbedSword( collision.GetContact( 0 ).point , collision.GetContact( 0 ).normal , collision.transform );
					break;
			}
		}

		/// <summary>
		/// Called from the claymore animator, unparent self and send as projectile.
		/// </summary>z
		public void AnimatorThrowWeapon() {
			// Disable animator
			//animator.speed = 0f;
			animator.enabled = false;

			// Instantly match camera orientation.
			if ( orientationMatchRef != null ) orientationMatchRef.InstantMatch();

			// Unparent self. Storing locations since the "maintain parent" doesn't seem to work right.
			storedThrowPos = transform.position;
			storedThrowRot = transform.rotation;
			transform.SetParent( null, true );

			// Launch with rigidbody kinematics.
			rb.isKinematic = false;
			wasThrown = true;
			//rb.AddForce( throwForceScalar * transform.forward , ForceMode.Acceleration );

			// Set coroutine to return animator speed after delay.
			//CoroutineUtils.WaitAndDoLater( throwTime, () => animator.speed = 1f );
			CoroutineUtils.WaitAndDoLater( throwTime , () => animator.enabled = !isEmbedded );
		}

		/// <summary>
		/// Called from the claymore animator, parent self back for return to hand animation.
		/// </summary>
		public void AnimatorReturnWeapon( ) {
			// Re-parent to original
			transform.SetParent( localParent );

			// Make sure we are kinematic.
			rb.isKinematic = true;
		}
        #endregion

        #region Helpers

		private void EmbedSword( Vector3 worldPoint, Vector3 worldNormal, Transform hitObject ) {
			// We are embedded.
			isEmbedded = true;

			// Disable rigidbody.
			rb.isKinematic = true;

			// Set parent.
			transform.SetParent( hitObject , true );

			// Set position and rotation.
			transform.position = worldPoint + (embedDistance * worldNormal);
			transform.forward = -worldNormal;

			// We've embedded, player controller will do some sick movement.
			parentPlayerController.EmbedSwordSetup();
		}

		public void ReleaseSword() {
			isEmbedded = false;
			transform.SetParent( localParent , true );
			embedState = ESwordEmbedState.None;
		}
        #endregion

        #region Coroutines
        #endregion

    }
}