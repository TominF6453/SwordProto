using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using System.Reflection;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;



#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Claymore {
	#region General Utils
	/// <summary>
	/// A static utilities class to hold additional general helper functions and
	/// extensions.
	/// </summary>
	public static class Utilities {
		// Random utility calls will use this one instance of System.Random.
		private static readonly System.Random getRandom = new();

		/// <summary>Remap a float from a given range to a new range.</summary>
		/// <param name="value">This float value to remap.</param>
		/// <param name="from1">The min of the original range.</param>
		/// <param name="from2">The max of the original range.</param>
		/// <param name="to1">The min of the new range.</param>
		/// <param name="to2">The max of the new range.</param>
		/// <returns>The value remapped to the new range.</returns>
		public static float Remap( this float value , float from1 , float from2 , float to1 , float to2 ) {
			return to1 + (value - from1) * (to2 - to1) / (from2 - from1);
		}

		/// <summary>Remap a float from a given range to a new range and clamp the value to that new range.</summary>
		/// <param name="value">This float value to remap.</param>
		/// <param name="from1">The min of the original range.</param>
		/// <param name="from2">The max of the original range.</param>
		/// <param name="to1">The min of the new range.</param>
		/// <param name="to2">The max of the new range.</param>
		/// <returns>The value remapped and clamped to the new range.</returns>
		public static float RemapClamped( this float value , float from1 , float from2 , float to1 , float to2 ) {
			return Mathf.Clamp( value.Remap( from1 , from2 , to1 , to2 ) , to1 , to2 );
		}

		/// <summary>Remap a float from 0-1 to the same position on an Ease Out Quartic function.</summary>
		/// <param name="x">The float value to remap.</param>
		/// <returns>The value remapped onto the ease out function.</returns>
		public static float EaseOutQuart( this float x ) {
			return 1f - Mathf.Pow( 1f - x , 4 );
		}

		/// <summary>Return the Quaternion difference between two rotations, such that multiplying Diff * From = To.</summary>
		/// <param name="to">The end rotation.</param>
		/// <param name="from">The start rotation.</param>
		/// <returns>The Quaternion rotation of From -> To.</returns>
		public static Quaternion Diff( this Quaternion to , Quaternion from ) {
			return to * Quaternion.Inverse( from );
		}

		/// <summary>Get the next value in an enum given an enum value. Ignores negative elements in the enum.</summary>
		/// <param name="value">The base enum value.</param>
		/// <returns>The next enum value after the base value.</returns>
		/// <exception cref="ArgumentException">If the given input is not an enum.</exception>
		public static T Next<T>( this T value ) where T : Enum {
			if ( !typeof( T ).IsEnum ) throw new ArgumentException( $"Argument {typeof( T ).FullName} is not an enum!" );

			T[] vals = (T[])Enum.GetValues(value.GetType());
			int ind = Array.IndexOf(vals, value) + 1; // Get the next index.
			if ( ind >= vals.Length ) ind = 0;
			return vals[ind];
		}

		/// <summary>Get the previous value in an enum given an enum value.</summary>
		/// <param name="value">The base enum value.</param>
		/// <returns>The previous enum value before the base value.</returns>
		/// <exception cref="ArgumentException">If the given input is not an enum.</exception>
		public static T Prev<T>( this T value ) where T : Enum {
			if ( !typeof( T ).IsEnum ) throw new ArgumentException( $"Argument {typeof( T ).FullName} is not an enum!" );

			T[] vals = (T[])Enum.GetValues(value.GetType());
			int ind = Array.IndexOf(vals, value) - 1; // Get the previous index.
			if ( ind < 0 ) ind = vals.Length - 1;
			return vals[ind];
		}

		/// <summary>Get a random integer value in some range.</summary>
		/// <param name="minInclusive">The minimum integer value to roll, inclusive.</param>
		/// <param name="maxExclusive">The maximum integer value to roll, exclusive.</param>
		/// <returns>A random integer between minInclusive and maxExclusive.</returns>
		public static int RandomInt( int minInclusive , int maxExclusive ) {
			// Found an implementation online that accounts for multi-threading, why not include just for safety?
			lock ( getRandom ) {
				return getRandom.Next( minInclusive , maxExclusive );
			}
		}

		/// <summary>Get a random float value in some range, using UnityEngine's native Random implementation.</summary>
		/// <param name="minInclusive">The minimum float value that can roll, inclusive.</param>
		/// <param name="maxInclusive">The maximum float value that can roll, inclusive.</param>
		/// <returns>A random float between minInclusive and maxInclusive.</returns>
		public static float RandomFloat( float minInclusive , float maxInclusive ) {
			return UnityEngine.Random.Range( minInclusive , maxInclusive );
		}

		/// <summary>Get a random item from a given list of items.</summary>
		/// <param name="lst">The list of items to randomly choose from.</param>
		/// <returns>A randomly selected item from the list.</returns>
		public static T RandomItem<T>( this List<T> lst ) {
			return lst[RandomInt( 0 , lst.Count() )];
		}
		public static T RandomItem<T>( this T[] array ) {
			return array[RandomInt( 0 , array.Length )];
		}

		/// <summary>
		/// Get a number of random items from a given list of items, with an option to ensure the items are different.
		/// Will return fewer items than requested if simply impossible to return the requested number.
		/// </summary>
		/// <param name="lst">The list of items to randomly choose from.</param>
		/// <param name="number">The amount of items to choose from the array.</param>
		/// <param name="exclusive">If true, the same item cannot be picked more than once.</param>
		/// <returns>A list containing the most possible selected items up to number.</returns>
		public static List<T> RandomItems<T>( this List<T> lst , int number , bool exclusive = false ) {
			// If the array is empty or we exclusively want more items than there are, return the list.
			if ( lst.Count() == 0 || lst.Count() < number && exclusive ) return lst;

			// Normal list generation.
			List<T> ret = new();
			while ( number > 0 ) {
				T item = RandomItem(lst); // Get a random item.
				if ( exclusive ) lst.Remove( item ); // If exclusive, remove item from pool.
				ret.Add( item ); // Add item to return list.
				number--; // Lower number.
			}

			return ret; // Return final list.
		}

		/// <summary>
		/// Generate a Vector3 that has the same value for all axes.
		/// </summary>
		/// <param name="value">The value to set all axes to.</param>
		/// <returns>The generated vector.</returns>
		public static Vector3 SameValueVector( float value ) {
			return new Vector3( value , value , value );
		}

		/// <summary>
		/// Returns the magnitude of this vector over the last frame. (Multiplied by delta time.)
		/// </summary>
		/// <returns>The magnitude of the vector multiplied by deltaTime.</returns>
		public static float MagnitudeThisFrame( this Vector3 vector ) {
			return vector.magnitude * Time.deltaTime;
		}

		/// <summary>
		/// Shorten a vector3 by some flat amount.
		/// </summary>
		/// <param name="magnitudeDelta">The change in magnitude between the original vector and the returned vector.</param>
		/// <returns>The original vector shortened in magnitude.</returns>
		public static Vector3 Shorten( this Vector3 vector , float magnitudeDelta ) {
			return vector - (vector.normalized * magnitudeDelta);
		}

		/// <summary>
		/// Rotate a given vector towards another given vector by some angle.
		/// </summary>
		/// <param name="vector">The starting vector to rotate. Will be modified.</param>
		/// <param name="target">The target vector to rotate towards.</param>
		/// <param name="angle">The maximum angle in degrees to rotate by.</param>
		/// <param name="magnitudeDelta">The maximum magnitude to change in the rotation. Defaults to 0.</param>
		public static void RotateTowards( this ref Vector3 vector , Vector3 target , float angle , float magnitudeDelta = 0 ) {
			vector = Vector3.RotateTowards( vector , target , angle.Deg2Rad() , magnitudeDelta );
		}

		/// <summary>
		/// Return the dot product between two vectors, normalized. This will not
		/// change the magnitudes of the given vectors.
		/// </summary>
		/// <param name="lhs">The lhs vector.</param>
		/// <param name="rhs">The rhs vector.</param>
		/// <returns>The dot product of the two vectors were they normalized.</returns>
		public static float NormalizedDot( this Vector3 lhs, Vector3 rhs ) {
			return Vector3.Dot( lhs.normalized , rhs.normalized );
		}

		/// <summary>
		/// Given a direction vector add a degree of random spread to it and return the new vector.
		/// </summary>
		/// <param name="original">The original direction vector.</param>
		/// <param name="angleInDegrees">The maximum spread angle for the new vector.</param>
		/// <returns>The resulting vector with spread applied.</returns>
		public static Vector3 AddSpread( this Vector3 original , float angleInDegrees ) {
			// Get a random rotation value.
			float randDeg = RandomFloat(-angleInDegrees, angleInDegrees);
			// Get a random rotation axis based on the plane with normal 'original'.
			Vector3 rotationAxis = original.normalized;
			// First rotation to bring it onto the plane defined by the normal.
			rotationAxis.RotateTowards( -rotationAxis , 90 );
			// Second rotation to random give it a different direction along the plane.
			rotationAxis = Quaternion.AngleAxis( RandomFloat( 0 , 180 ) , original.normalized ) * rotationAxis;
			// Finally, rotate the original Vector and return.
			original = Quaternion.AngleAxis( randDeg , rotationAxis ) * original;

			return original;
		}

		/// <summary>
		/// Return whether a given float value is within the range specified by a Vector2, range check is inclusive.
		/// </summary>
		/// <param name="value">The given float value.</param>
		/// <param name="range">The range of float values to check.</param>
		/// <returns>True if value is within the range (inclusive), false otherwise.</returns>
		public static bool WithinRange( this float value , Vector2 range ) {
			float min = Mathf.Min(range.x,range.y);
			float max = Mathf.Max(range.x,range.y);
			return value >= min && value <= max;
		}

		/// <summary>
		/// Linearly interpolate between the two values in a Vector2 by t.
		/// </summary>
		/// <param name="t">The interpolation value between the vector values.</param>
		/// <returns>The interpolated float result between the values in the vector.</returns>
		public static float RangeLerp( this Vector2 range , float t ) {
			return Mathf.Lerp( range.x , range.y , t );
		}

		/// <summary>
		/// Converts a rigidbody's angular velocity into local space.
		/// </summary>
		/// <returns>The given rigidbody's angular velocity transformed via its transform's rotation matrix. Local space.</returns>
		public static Vector3 LocalAngularVelocity( this Rigidbody rigidbody ) {
			return rigidbody.transform.InverseTransformDirection( rigidbody.angularVelocity );
		}

		/// <summary>
		/// Set a rigidbody's angular velocity in local space.
		/// </summary>
		/// <param name="velocity">The angular velocity to set, defined as usual, in radians per second.</param>
		public static void SetAngularVelocityRelative( this Rigidbody rigidbody , Vector3 velocity ) {
			rigidbody.angularVelocity = rigidbody.transform.TransformDirection( velocity );
		}

		/// <summary>
		/// Snap a given number to the closest of two values.
		/// </summary>
		/// <param name="value">The number to snap.</param>
		/// <param name="num1">The first number to potentially snap to.</param>
		/// <param name="num2">The second number to potentially snap to.</param>
		public static void SnapTo( this ref float value , float num1 , float num2 ) {
			if ( Mathf.Abs( value - num1 ) <= Mathf.Abs( value - num2 ) ) value = num1;
			else value = num2;
		}

		/// <summary>
		/// Smoothly move a given value towards the target value with a maximum delta.
		/// Like lerping but not percentage-based.
		/// </summary>
		/// <param name="value">The float value to modify.</param>
		/// <param name="target">The target value to smoothly move towards.</param>
		/// <param name="maxDelta">The maximum amount of change in value.</param>
		public static void SmoothTo( this ref float value , float target , float maxDelta ) {
			// If target is less than value, we're subtracting, make delta negative.
			if ( target < value ) maxDelta *= -1;
			// Move value to the closest of the target and the value + delta.
			value.SnapTo( target , value + maxDelta );
		}

		/// <summary>
		/// Smoothly move a given value towards the target value with a maximum delta.
		/// Like lerping but not percentage-based.
		/// Do not modify the input, but return the result.
		/// </summary>
		/// <param name="value">The float value to modify.</param>
		/// <param name="target">The target value to smoothly move towards.</param>
		/// <param name="maxDelta">The maximum amount of change in value.</param>
		/// <returns>The new value after the smooth step towards target.</returns>
		public static float SmoothTo( float value , float target , float maxDelta ) {
			// If target is less than value, we're subtracting, make delta negative.
			if ( target < value ) maxDelta *= -1;
			// Move value to the closest of the target and the value + delta.
			value.SnapTo( target , value + maxDelta );
			return value;
		}

		/// <summary>
		/// Get the closest of two numbers to a given value.
		/// </summary>
		/// <param name="value">The number to snap.</param>
		/// <param name="num1">The first number to potentially snap to.</param>
		/// <param name="num2">The second number to potentially snap to.</param>
		/// <returns>The closest of num1 and num2 to value.</returns>
		public static float SnapTo( float value , float num1 , float num2 ) {
			if ( Mathf.Abs( value - num1 ) <= Mathf.Abs( value - num2 ) ) return num1;
			else return num2;
		}

		/// <summary>
		/// Return whether or not str1 contains str2, case insensitive.
		/// </summary>
		/// <param name="str1">The containing string.</param>
		/// <param name="str2">The contained string to check for.</param>
		/// <returns>True if str2 is in str1, false otherwise.</returns>
		public static bool ContainsInsensitive( string str1 , string str2 ) => str1.ToLower().Contains( str2.ToLower() );

		// ---- DISTANCE CONVERSIONS ----
		public static float Feet2CM( this float ft ) => ft * 30.48f;
		public static float Feet2M( this float ft ) => ft * 0.3048f;
		public static float M2Feet( this float m ) => m * 3.2809f;

		// ---- SPEED CONVERSIONS ----
		public static float MPH2MpS( this float mph ) => 0.4470272686633884667f * mph;
		public static float MpS2MPH( this float mps ) => 2.237f * mps;

		// ---- ANGLE CONVERSIONS ----
		public static float Deg2Rad( this float deg ) => deg * (Mathf.PI / 180);
		public static float Rad2Deg( this float rad ) => rad * (180 / Mathf.PI);

		/// <summary>
		/// Returns a random element from an IList.
		/// </summary>
		/// <param name="collection">The IList we will get a random element from.</param>
		/// <param name="excluding">Optional. If set, will not return that element. Used to prevent repeats.</param>
		/// <returns>Returns a random element T</returns>
		public static T Random<T>( this IList<T> collection , T excluding = default( T ) ) {
			if ( collection == null ) return default( T );
			if ( excluding != null ) collection.Remove( excluding );
			if ( collection.Count == 0 ) return default( T );
			return collection[UnityEngine.Random.Range( 0 , collection.Count )];
		}

		/// <summary>
		/// Checks if there is a clear line of sight between transform and target in the transform.forward direction.
		/// This is a convenience method to replace a long raycast statement
		/// </summary>
		/// <param name="transform">The origin of the raycast.</param>
		/// <param name="target">The transform that we are trying to see. Must contain a collider.</param>
		/// <param name="ignoreTriggers">Optional. If set, raycast will ignore triggers.</param>
		/// <returns>bool: Returns true if transform can see target</returns>
		public static bool CanSee( this Transform transform , Transform target , LayerMask layerMask , bool ignoreTriggers = false ) {
			RaycastHit hit;

			if ( ignoreTriggers )
				Physics.Raycast( new Ray( transform.position , transform.forward ) , out hit , Mathf.Infinity , layerMask , QueryTriggerInteraction.UseGlobal );
			else
				Physics.Raycast( new Ray( transform.position , transform.forward ) , out hit , Mathf.Infinity , layerMask );

			if ( hit.transform == target ) return true;
			return false;
		}

		/// <summary>
		/// Conveniently gets a list of child transforms
		/// </summary>
		/// <param name="transform">The parent transform.</param>
		/// <param name="recursive">Optional. Get all children, recursively.</param>
		/// <param name="includeSelf">Optional. If set, includes the parent transform in the list.</param>
		/// <returns>Returns the new list of transforms. Returns an empty list instead on null.</returns>
		public static List<Transform> Children( this Transform transform , bool recursive = false , bool includeSelf = false ) {
			if ( transform == null ) return new List<Transform>();
			if ( transform.childCount == 0 && !includeSelf ) return new();
			if ( transform.childCount == 0 ) return new() { transform };

			List<Transform> children = new();
			if ( includeSelf ) children.Add( transform );

			foreach ( Transform child in transform ) {
				children.Add( child );
				if ( recursive ) children.AddRange( child.Children( recursive: true ) );
			}

			return children;
		}

		/// <summary>
		/// Checks for a clear path between two transforms. Uses a linecast and ignores transform orientation.
		/// A easy to read convenience method.
		/// </summary>
		/// <param name="transform">The origin of the linecast</param>
		/// <param name="target">The destination of the linecast</param>
		/// <param name="layerMask">A layermask that declares which layers we can hit</param>
		/// <param name="hitsTriggers">Determines if triggers are includes in the linecast check</param>
		/// <returns></returns>
		public static bool HasClearPath( this Transform transform , Transform target , LayerMask layerMask , QueryTriggerInteraction hitsTriggers = QueryTriggerInteraction.Ignore ) {
			return !Physics.Linecast( transform.position , target.position , layerMask , hitsTriggers );
		}

		/// <summary>
		/// Calculates the distance between two transforms in an ultra-readable way
		/// </summary>
		/// <param name="transform">The first transform.</param>
		/// <param name="target">A component for the second transform.</param>
		/// <returns>Returns the distance between the two transforms.</returns>
		public static float DistanceTo( this Transform transform , Component target ) {
			return Vector3.Distance( transform.position , target.transform.position );
		}

		/// <summary>
		/// Calculates the distance between a transform and a Vector3
		/// </summary>
		/// <param name="transform">The transform.</param>
		/// <param name="target">The position we are checking the distance to</param>
		/// <returns>Returns the distance between the two positions.</returns>
		public static float DistanceTo( this Transform transform , Vector3 target ) {
			return Vector3.Distance( transform.position , target );
		}

		/// <summary>
		/// Calculates the direction from transform to target in an ultra-readable way
		/// </summary>
		/// <param name="transform">The first transform.</param>
		/// <param name="target">A component for the second transform.</param>
		/// <param name="normalize">Optional: Should the return value be normaized?</param>
		/// <returns>Returns a normalized vector3 pointing from transform to target.</returns>
		public static Vector3 DirectionTo( this Transform transform , Component target , bool normalize = true ) {
			Vector3 result = target.transform.position - transform.position;
			if ( normalize ) result.Normalize();
			return result;
		}

		/// <summary>
		/// Calculates the direction from transform to target in an ultra-readable way
		/// </summary>
		/// <param name="transform">The first transform.</param>
		/// <param name="target">A component for the second transform.</param>
		/// <returns>Returns a normalized vector3 pointing from transform to target.</returns>
		public static Vector3 DirectionTo( this Transform transform , Vector3 target ) {
			return (target - transform.position).normalized;
		}
	}
	#endregion

	#region String Utils
	public static class StringUtils { // This class can be called from anywhere, and will contain useful methods to manipulate strings
									  // Formatting.
		public const string COMMAS = "#,##0";

		/// <summary>
		/// Make the name of an enum into a string, with spaces inbetween capitals.
		/// </summary>
		/// <param name="enumValue">The enum you want to convert to a string</param>
		/// <returns></returns>
		public static string EnumToProperName( Enum enumValue ) {
			string enumName = enumValue.ToString();
			System.Text.StringBuilder result = new System.Text.StringBuilder(enumName.Length * 2);

			// Loop through all characters
			for ( int i = 0; i < enumName.Length; i++ ) {
				// If there should be a space, add a space
				if ( i > 0 && char.IsUpper( enumName[i] ) && !char.IsUpper( enumName[i - 1] ) ) {
					result.Append( ' ' );
				}
				result.Append( enumName[i] );
			}

			return result.ToString();
		}

		/// <summary>
		/// Make the name of an enum into a string, with spaces inbetween capitals.
		/// </summary>
		/// <param name="number">The number you want to convert</param>
		/// <returns></returns>
		public static string PositiveVersionOfNumberToString<T>( T number ) where T : IConvertible {
			string numberString = Convert.ToString(number, System.Globalization.CultureInfo.InvariantCulture);
			return numberString.TrimStart( '-' );
		}


		/// <summary>
		/// Input a message, which may contain a value between {} to automatically add that value to it.
		/// <param name="message">The message you're checking for braces</param>
		/// <param name="context">The object where that value could reside (usually 'this')</param>
		/// </summary>
		public static string CheckStringForCodedValueAndReplace( string message , object context ) {
			// Use regular expressions to find the {}
			return System.Text.RegularExpressions.Regex.Replace( message , @"\{(\w+)\}" , match => {
				string memberName = match.Groups[1].Value;
				//UnityEngine.Debug.Log( $"Looking for: {memberName}" );

				// Check for field using reflection
				FieldInfo field = context.GetType().GetField(memberName,
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

				if ( field != null ) {
					object value = field.GetValue(context);
					//UnityEngine.Debug.Log( $"Found field value: {value}" );
					return value?.ToString() ?? string.Empty;
				}

				// If for some reason we change it to a property later on, it will still work
				PropertyInfo property = context.GetType().GetProperty(memberName,
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

				if ( property != null ) {
					object value = property.GetValue(context);
					//UnityEngine.Debug.Log( $"Found property value: {value}" );
					return value?.ToString() ?? string.Empty;
				}

				//UnityEngine.Debug.Log( $"Didn't find a special value for {memberName}, ignoring curly brackets" );
				return match.Value; // Return the original placeholder if member not found
			} );
		}

	}

	#endregion String Utils

	#region CoroutineUtils
	/// <summary>
	/// A static utilities class containing a generic way to run/activate coroutines
	/// and/or easily apply delays to certain actions.
	/// </summary>
	public static class CoroutineUtils {
		public class CoroutineHelper : MonoBehaviour {
			private static CoroutineHelper _instance;
			private static object _lock = new object();

			public static CoroutineHelper Instance {
				get {
					lock ( _lock ) {
						if ( _instance == null ) {
							_instance = FindFirstObjectByType<CoroutineHelper>();

							if ( FindObjectsByType<CoroutineHelper>( FindObjectsSortMode.None ).Length > 1 ) {
								Debug.LogError( "[Singleton] Something went really wrong " +
									" - there should never be more than 1 singleton!" +
									" Reopening the scene might fix it." );
								return _instance;
							}

							if ( _instance == null ) {
								GameObject singleton = new GameObject();
								_instance = singleton.AddComponent<CoroutineHelper>();
								singleton.name = "(singleton) " + typeof( CoroutineHelper ).ToString();

								DontDestroyOnLoad( singleton );

								Debug.Log( "[Singleton] An instance of " + typeof( CoroutineHelper ) +
									" is needed in the scene, so '" + singleton +
									"' was created." );
							} else {
								//Debug.Log("[Singleton] Using instance already created: " + _instance.gameObject.name);
							}
						}

						return _instance;
					}
				}
			}

			/// <summary>
			/// When Unity quits, it destroys objects in a random order.
			/// In principle, a Singleton is only destroyed when application quits.
			/// If any script calls Instance after it have been destroyed, 
			///   it will create a buggy ghost object that will stay on the Editor scene
			///   even after stopping playing the Application. Really bad!
			/// So, this was made to be sure we're not creating that buggy ghost object.
			/// </summary>
			protected void OnDestroy() {
				_instance = null;
			}

		}

		public static Coroutine StartCoroutine( IEnumerator enumerator , MonoBehaviour target = null ) {
			return target ? target.StartCoroutine( enumerator ) : CoroutineHelper.Instance.StartCoroutine( enumerator );
		}

		public static void StopCoroutine( Coroutine coroutine , MonoBehaviour target = null ) {
			if ( coroutine == null )
				return;

			if ( target )
				target.StopCoroutine( coroutine );
			else
				CoroutineHelper.Instance.StopCoroutine( coroutine );
		}
		public static void StopCoroutine( IEnumerator enumerator , MonoBehaviour target = null ) {
			if ( enumerator == null )
				return;

			if ( target )
				target.StopCoroutine( enumerator );
			else
				CoroutineHelper.Instance.StopCoroutine( enumerator );
		}

		public static Coroutine WaitAndDoLater( float delay , Action action , MonoBehaviour target = null ) {
			return StartCoroutine( WaitAndDoLaterRoutine( new WaitForSeconds( delay ) , action ) , target );
		}
		public static Coroutine WaitAndDoLater( CustomYieldInstruction delay , Action action , MonoBehaviour target = null ) {
			return StartCoroutine( WaitAndDoLaterRoutine( delay , action ) , target );
		}
		public static Coroutine WaitAndDoLater( YieldInstruction delay , Action action , MonoBehaviour target = null ) {
			return StartCoroutine( WaitAndDoLaterRoutine( delay , action ) , target );
		}

		private static IEnumerator WaitAndDoLaterRoutine( CustomYieldInstruction delay , Action action ) {
			yield return delay;
			action();
		}
		private static IEnumerator WaitAndDoLaterRoutine( YieldInstruction delay , Action action ) {
			yield return delay;
			action();
		}
	}
	#endregion

	#region Gizmos Utilties
#if UNITY_EDITOR
	public static class GizmoUtilities {
		/// <summary>
		/// Given typical details from a capsule collider, draw one in gizmos.
		/// Gracefully taken from: https://discussions.unity.com/t/drawing-capsule-gizmo/597344/9
		/// </summary>
		/// <param name="_pos">The root position of the capsule.</param>
		/// <param name="_rot">The world rotation of the capsule.</param>
		/// <param name="_radius">The radius of the capsule.</param>
		/// <param name="_height">The height of the capsule.</param>
		/// <param name="_color">The colour to draw in.</param>
		/// <param name="extraLines">If true, draws additional lines to add more definition to the capsule.</param>
		public static void DrawWireCapsule( Vector3 _pos , Quaternion _rot , float _radius , float _height , Color _color = default( Color ) , bool extraLines = false ) {
			if ( _color != default( Color ) )
				Handles.color = _color;
			Matrix4x4 angleMatrix = Matrix4x4.TRS(_pos, _rot, Handles.matrix.lossyScale);
			//Matrix4x4 angleMatrix = Matrix4x4.TRS(_pos,_rot, )
			using ( new Handles.DrawingScope( angleMatrix ) ) {
				var pointOffset = (_height - (_radius * 2)) / 2;

				//draw sideways
				Handles.DrawWireArc( Vector3.up * pointOffset , Vector3.left , Vector3.back , -180 , _radius );
				Handles.DrawLine( new Vector3( 0 , pointOffset , -_radius ) , new Vector3( 0 , -pointOffset , -_radius ) );
				Handles.DrawLine( new Vector3( 0 , pointOffset , _radius ) , new Vector3( 0 , -pointOffset , _radius ) );
				Handles.DrawWireArc( Vector3.down * pointOffset , Vector3.left , Vector3.back , 180 , _radius );
				//draw frontways
				Handles.DrawWireArc( Vector3.up * pointOffset , Vector3.back , Vector3.left , 180 , _radius );
				Handles.DrawLine( new Vector3( -_radius , pointOffset , 0 ) , new Vector3( -_radius , -pointOffset , 0 ) );
				Handles.DrawLine( new Vector3( _radius , pointOffset , 0 ) , new Vector3( _radius , -pointOffset , 0 ) );
				Handles.DrawWireArc( Vector3.down * pointOffset , Vector3.back , Vector3.left , -180 , _radius );
				//draw center
				Handles.DrawWireDisc( Vector3.up * pointOffset , Vector3.up , _radius );
				Handles.DrawWireDisc( Vector3.down * pointOffset , Vector3.up , _radius );

				// Extra lines.
				if ( extraLines ) {
					// duping center disc a few times
					Handles.DrawWireDisc( .25f * pointOffset * Vector3.up , Vector3.up , _radius );
					Handles.DrawWireDisc( .25f * pointOffset * Vector3.down , Vector3.up , _radius );
					Handles.DrawWireDisc( .5f * pointOffset * Vector3.up , Vector3.up , _radius );
					Handles.DrawWireDisc( .5f * pointOffset * Vector3.down , Vector3.up , _radius );
					Handles.DrawWireDisc( .75f * pointOffset * Vector3.up , Vector3.up , _radius );
					Handles.DrawWireDisc( .75f * pointOffset * Vector3.down , Vector3.up , _radius );
				}
			}
		}
		/// <summary>
		/// Given typical details from a capsule collider, draw one in gizmos.
		/// Gracefully taken from: https://discussions.unity.com/t/drawing-capsule-gizmo/597344/9
		/// </summary>
		/// <param name="capsule">The capsule collider to draw.</param>
		/// <param name="color">The colour to draw in.</param>
		public static void DrawWireCapsule( CapsuleCollider capsule , Color color = default , bool extraLines = false ) {
			DrawWireCapsule( capsule.transform.position , capsule.transform.rotation , capsule.radius * capsule.transform.lossyScale.x , capsule.height * capsule.transform.lossyScale.y , color , extraLines );
		}

		/// <summary>
		/// Given definition of a sphere, draw a wireframe sphere in gizmos.
		/// </summary>
		/// <param name="_pos">The root position of the sphere.</param>
		/// <param name="_radius">The radius of the sphere.</param>
		/// <param name="_color">The colour to draw the sphere in.</param>
		/// <param name="extraLines">If true, draws additional lines to add more definition to the sphere.</param>
		public static void DrawWireSphere( Vector3 _pos , float _radius , Color _color = default( Color ) , bool extraLines = false ) {
			if ( _color != default( Color ) )
				Handles.color = _color;
			Matrix4x4 angleMatrix = Matrix4x4.TRS(_pos, Quaternion.identity, Handles.matrix.lossyScale);
			//Matrix4x4 angleMatrix = Matrix4x4.TRS(_pos,_rot, )
			using ( new Handles.DrawingScope( angleMatrix ) ) {
				int lines = extraLines ? 8 : 16;
				float step = Mathf.PI / lines;
				for ( int i = 0; i < lines; i++ ) {
					Handles.DrawWireDisc( Vector3.zero , new( Mathf.Cos( i * step ) , Mathf.Sin( i * step ) , 0f ) , _radius );
				}
			}
		}

		/// <summary>
		/// Given a sphere collider, draw a wireframe representation in gizmos.
		/// </summary>
		/// <param name="sphere"></param>
		/// <param name="color"></param>
		/// <param name="extraLines"></param>
		public static void DrawWireSphere( SphereCollider sphere , Color color = default , bool extraLines = false ) {
			DrawWireSphere( sphere.transform.position , sphere.radius * sphere.transform.lossyScale.x , color , extraLines );
		}
	}
#endif
	#endregion

	#region Editor Utilities
#if UNITY_EDITOR
	public static class EditorUtilities {
		public static GUIStyle WordWrap {
			get {
				GUIStyle ret = new(EditorStyles.textField) {
					wordWrap = true
				};
				return ret;
			}
		}

		public static void ViewField( string title , string refString , float width = 200f ) {
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label( title , GUILayout.Width( width ) );
			GUILayout.Label( refString );
			EditorGUILayout.EndHorizontal();
		}
		public static void ViewSprite( Texture img ) {
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Box( img );
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
		}
		public static void InputField( string title , ref int referenceVal , float width = 200f ) {
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label( title , GUILayout.Width( width ) );
			referenceVal = EditorGUILayout.IntField( "" , referenceVal );
			EditorGUILayout.EndHorizontal();
		}
		public static void InputField( string title , ref float referenceVal , float width = 200f ) {
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label( title , GUILayout.Width( width ) );
			referenceVal = EditorGUILayout.FloatField( "" , referenceVal );
			EditorGUILayout.EndHorizontal();
		}
		public static void InputField( string title , ref string referenceString , float width = 200f , float height = 20 ) {
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label( title , GUILayout.Width( width ) );
			referenceString = EditorGUILayout.TextField( "" , referenceString , WordWrap , GUILayout.Height( height ) );
			EditorGUILayout.EndHorizontal();
		}
		public static void InputField( string title , ref bool referenceBool , float width = 200f ) {
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label( title , GUILayout.Width( width ) );
			referenceBool = EditorGUILayout.Toggle( "" , referenceBool );
			EditorGUILayout.EndHorizontal();
		}
		public static void InputField( string title , ref GameObject referenceObj , float width = 200f ) {
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label( title , GUILayout.Width( width ) );
			referenceObj = (GameObject)EditorGUILayout.ObjectField( "" , referenceObj , typeof( GameObject ) , false );
			EditorGUILayout.EndHorizontal();
		}
		public static void InputPrefabField<T>( string title , ref T referenceObj , float width = 200f ) where T : MonoBehaviour {
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label( title , GUILayout.Width( width ) );
			referenceObj = (T)EditorGUILayout.ObjectField( "" , referenceObj , typeof( T ) , false );
			EditorGUILayout.EndHorizontal();
		}
		public static void ValueField( string title , ref int[] values , float width = 200f ) {
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label( title , GUILayout.Width( width ) );
			GUILayout.FlexibleSpace();
			GUILayout.Label( string.Join( "," , values ) );
			EditorGUILayout.EndHorizontal();
		}
		public static void InputFieldEnum<T>( string title , ref T referenceEnum , float width = 200f ) where T : Enum {
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label( title , GUILayout.Width( width ) );
			referenceEnum = (T)EditorGUILayout.EnumPopup( "" , referenceEnum );
			EditorGUILayout.EndHorizontal();
		}
		public static void InputFieldFlags<T>( string title , ref T referenceEnum , float width = 200f ) where T : Enum {
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label( title , GUILayout.Width( width ) );
			referenceEnum = (T)EditorGUILayout.EnumFlagsField( "" , referenceEnum );
			EditorGUILayout.EndHorizontal();
		}
	}
#endif
	#endregion

	#region Additional Classes
	/// <summary>
	/// Effectively an implementation of C#'s Queue object but with a fixed size that automatically
	/// pops items if the maximum size is reached.
	/// </summary>
	public class FixedSizeQueue<T> {

		// The underlying Queue object.
		private Queue<T> baseQueue;

		public int Size { get; private set; }
		public int Count { get => baseQueue.Count; }
		public bool IsFull { get => Count == Size; }

		// Constructor
		public FixedSizeQueue( int size ) {
			baseQueue = new Queue<T>( size );
			Size = size;
		}

		// Queue "overrides"
		public void Enqueue( T item ) {
			if ( IsFull ) baseQueue.Dequeue();
			baseQueue.Enqueue( item );
		}

		public void Dequeue() {
			if ( Count == 0 ) return; // Nothing to dequeue.
			baseQueue.Dequeue();
		}

		public bool Contains( T item ) => baseQueue.Contains( item );
		public void CopyTo( T[] array , int index ) => baseQueue.CopyTo( array , index );
		public Queue<T>.Enumerator GetEnumerator() => baseQueue.GetEnumerator();
		public void Clear() => baseQueue.Clear();
		public T Peek() => baseQueue.Peek();
	}
	#endregion
}

#region Scene Referencing Utilties
namespace WingsOfIcaria.SceneManagement {
	/// <summary>
	/// Unity loads scenes at runtime either through index or absolute path. This makes managing scenes in Editor 
	/// unintuitive, it would be more intuitive to reference scenes at Editor time using their .scene assets.
	/// 
	/// SceneReference is effectively a property wrapper to allow editor referencing using .scene assets while storing
	/// scene paths to load at runtime, which lets us simply refer to the SceneReference instead of storing the raw string somewhere
	/// or worse, keeping a magical list of indices and hoping they stay correct.
	/// 
	/// Massively ripped from https://github.com/NibbleByte/UnitySceneReference, many thanks.
	/// </summary>
#if UNITY_EDITOR
	[InitializeOnLoad]
#endif
	[Serializable]
	public class SceneReference : ISerializationCallbackReceiver, IEquatable<SceneReference>, IComparable<SceneReference> {

		#region Variables
#if UNITY_EDITOR
		// The direct scene asset reference, will populate path from this.
		[SerializeField] SceneAsset sceneAsset;

		// When modified, can be marked dirty to save.
		[SerializeField] bool isDirty;
#endif
		// The string path for the scene, will be used at runtime to load scenes.
		[SerializeField] string scenePath;
		#endregion

		#region Getters
		public string ScenePath {
			get {
#if UNITY_EDITOR
				// Make sure the path reference is correct.
				UpdateReferences();
#endif
				return scenePath;
			}
			set {
				scenePath = value;

#if UNITY_EDITOR
				if ( string.IsNullOrEmpty( scenePath ) ) { // Clear scene asset value if scene path is empty.
					sceneAsset = null;
				} else { // Set scene asset value if scene path is not empty.
					sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>( scenePath );
					if ( sceneAsset == null ) {
						Debug.LogError( $"No SceneAsset found at {scenePath}!" );
					}
				}
#endif
			}
		}

		public string SceneName => Path.GetFileNameWithoutExtension( ScenePath );
		public int BuildIndex => SceneUtility.GetBuildIndexByScenePath( ScenePath );
		#endregion

		#region Other Overrides
		public override string ToString() => scenePath;

		public override int GetHashCode() => ScenePath?.GetHashCode() ?? 0;
		#endregion

		#region Constructors
		public SceneReference() { }

		public SceneReference( string path ) {
			ScenePath = path;
		}

		public SceneReference( SceneReference other ) {
			scenePath = other.scenePath;
#if UNITY_EDITOR
			// Editor only properties.
			sceneAsset = other.sceneAsset;
			isDirty = other.isDirty;
#endif

		}


#if UNITY_EDITOR
		static SceneReference() {
			AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
			AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
		}

		static bool reloadingAssemblies = false;
		private static void OnBeforeAssemblyReload() {
			reloadingAssemblies = true; // Prevent reference updating while assemblies are loading.
		}
		private static void OnAfterAssemblyReload() {
			reloadingAssemblies = false; // Allow reference updating after assemblies load.
		}
#endif

		#endregion

		#region Helpers
#if UNITY_EDITOR
		/// <summary>
		/// To be called in Editor during asset parameter updates, keep the string path matching the asset.
		/// </summary>
		private void UpdateReferences() {
			// Asset missing, get asset reference from path if exists.
			if ( sceneAsset == null && !string.IsNullOrEmpty( scenePath ) ) {
				// Look for a scene asset at the supplied path.
				SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
				if ( scene ) { // If an asset is found, set asset internally and set dirty.
					sceneAsset = scene;
					isDirty = true;
					EditorSceneManager.MarkAllScenesDirty();
				}
				// Scene asset present, ensure path points to this asset.
			} else if ( sceneAsset != null ) {
				// Get path from asset reference.
				string path = AssetDatabase.GetAssetPath(sceneAsset);
				// If the path is not empty and doesn't match the currently stored path, update references.
				if ( !string.IsNullOrEmpty( path ) && path != scenePath ) {
					scenePath = path;
					isDirty = true;
					EditorSceneManager.MarkAllScenesDirty();
				}
			}
		}

		/// <summary>
		/// Handler to be called on next update frame after deserialization.
		/// </summary>
		void OnAfterDeserializationHandler() {
			// Remove self from update.
			EditorApplication.update -= OnAfterDeserializationHandler;
			// Update references.
			UpdateReferences();
		}
#endif

		/// <summary>Load this SceneReference's stored scene via the path stored.</summary>
		/// <param name="mode">Specify which mode to load the scene, such as additive.</param>
		public void LoadThisScene( LoadSceneMode mode ) => UnityEngine.SceneManagement.SceneManager.LoadScene( scenePath , mode );

		#endregion

		#region IComparable Implementation
		public int CompareTo( SceneReference other ) => other == null ? 1 : ScenePath.CompareTo( other.ScenePath );
		#endregion

		#region IEquatable Implementation
		public override bool Equals( object obj ) => obj is SceneReference other && Equals( other );
		public bool Equals( SceneReference other ) => other != null && ScenePath == other.scenePath;
		public bool Equals( string path ) => ScenePath == path;
		#endregion

		#region ISerializationCallbackReceiver Implementation
		public void OnAfterDeserialize() {
#if UNITY_EDITOR
			// OnAfterDeserialize is called in the deserialization thread so we can't touch Unity API.
			// Wait for the next update frame to do it.
			EditorApplication.update += OnAfterDeserializationHandler;
#endif
		}

		public void OnBeforeSerialize() {
#if UNITY_EDITOR
			// Do not try to update scene references while assemblies are reloading.
			if ( reloadingAssemblies ) return;

			UpdateReferences();
#endif
		}
		#endregion
	}

	#region Custom Property Editor
#if UNITY_EDITOR
	/// <summary>
	/// Custom property drawer for SceneReference properties in inspector.
	/// </summary>
	[CustomPropertyDrawer( typeof( SceneReference ) )]
	[CanEditMultipleObjects]
	public class SceneReferencePropertyDrawer : PropertyDrawer {

		static GUIStyle ButtonStyle;
		static GUIContent AddToBuildList;
		static GUIContent RemoveFromBuildList;

		public override void OnGUI( Rect position , SerializedProperty property , GUIContent label ) {

			if ( ButtonStyle == null ) {
				// Define the button style for adding or removing scene from build list.
				ButtonStyle = new GUIStyle( EditorStyles.miniButtonRight ) {
					padding = new RectOffset( 4 , 4 , 4 , 4 ) ,
					fontStyle = FontStyle.Bold
				};

				// Define add and remove button content.
				AddToBuildList = new GUIContent( EditorGUIUtility.IconContent( "CreateAddNew" ).image , "Add Scene to Build List." );
				RemoveFromBuildList = new GUIContent( EditorGUIUtility.IconContent( "Toolbar Minus" ).image , "Remove Scene from Build List." );
			}

			var dirtyProperty = property.FindPropertyRelative("isDirty");
			if ( dirtyProperty.boolValue ) {
				// Force a change in the property to make it dirty.
				dirtyProperty.boolValue = false;
			}

			EditorGUI.BeginProperty( position , label , property );
			position = EditorGUI.PrefixLabel( position , GUIUtility.GetControlID( FocusType.Passive ) , label );

			// Setup rects for the asset and the button.
			Rect assetRect = position;
			assetRect.width -= 22f; // Padding and width of the button.

			Rect buttonRect = position;
			buttonRect.x += assetRect.width + 2f; // Padding.
			buttonRect.width = 20f;

			// Get the SceneAsset object property, and store current value incase it changes in the property field.
			var sceneAssetProperty = property.FindPropertyRelative("sceneAsset");
			bool hadReference = sceneAssetProperty.objectReferenceValue != null;

			EditorGUI.PropertyField( assetRect , sceneAssetProperty , new GUIContent() );

			// Check property field for any changes, also get most up to date build index value.
			int buildIndex = -1;
			if ( sceneAssetProperty.objectReferenceValue ) {
				if ( AssetDatabase.TryGetGUIDAndLocalFileIdentifier( sceneAssetProperty.objectReferenceValue , out string guid , out long _ ) ) {
					// Search through editor build settings for the index of a scene that has the same guid as this property.
					buildIndex = Array.FindIndex( EditorBuildSettings.scenes , x => x.guid.ToString() == guid );
				}
			} else if ( hadReference ) {
				// Had an object reference but no longer, clear path property.
				property.FindPropertyRelative( "scenePath" ).stringValue = "";
			}

			bool inBuildSettings = buildIndex != -1;
			GUIContent buttonContent = !inBuildSettings ? AddToBuildList : RemoveFromBuildList;

			Color prevBG = GUI.backgroundColor; // Store background colour to reset to after colouring the button.
			GUI.backgroundColor = !inBuildSettings ? Color.red : Color.green; // Red if not in build, green if in build.

			if ( GUI.Button( buttonRect , buttonContent , ButtonStyle ) && sceneAssetProperty.objectReferenceValue ) {
				// Different button behaviour whether it's in the build settings or not.
				if ( inBuildSettings ) {
					// Convert editor scenes to list to remove at the index, then convert to array and move back.
					List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
					scenes.RemoveAt( buildIndex );
					EditorBuildSettings.scenes = scenes.ToArray();
				} else {
					// Create new editor scene object and concatenate to build settings.
					EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[] {
						new( AssetDatabase.GetAssetPath( sceneAssetProperty.objectReferenceValue ), true )
					};

					EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat( scenes ).ToArray();
				}
			}

			// Reset GUI bg colour.
			GUI.backgroundColor = prevBG;

			EditorGUI.EndProperty();
		}
	}
#endif
	#endregion
}
#endregion