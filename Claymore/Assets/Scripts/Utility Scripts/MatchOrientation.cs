using UnityEngine;

namespace Claymore {
    public class MatchOrientation : MonoBehaviour {

        // Vars
        [SerializeField] Transform target;

        // Params
        [SerializeField] float maxDegreesPerSecond = 360f;
        [SerializeField] bool easeIn = true;
        [SerializeField] float easeInForce = 0.5f;

        private float DeltaRotation { get => easeIn ? Mathf.Min( maxDegreesPerSecond , Quaternion.Angle( target.rotation , this.transform.rotation ) * easeInForce ) : maxDegreesPerSecond; }

		private void Update() {
			if ( target ) {
                this.transform.rotation = Quaternion.RotateTowards( this.transform.rotation , target.transform.rotation , DeltaRotation * Time.deltaTime );
            }
		}

        public void InstantMatch() {
            if ( target ) {
                this.transform.rotation = target.transform.rotation;
            }
        }
	}
}