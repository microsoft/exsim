// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;

namespace UR.Common
{
	/// <summary>
	/// A simple 2-tuple class that can be used to generically represent a pair
	/// of object instances
	/// </summary>
	public class Pair : Pair<object, object>
	{
		public Pair(object o1, object o2)
			: base(o1, o2)
		{
		}
	}

	/// <summary>
	/// A simple 2-tuple class that can be used to generically represent a pair
	/// of object instances
	/// </summary>
	public class Pair<F, S>
	{
		/// <summary>
		/// The first object
		/// </summary>
		public F Object1;
		/// <summary>
		/// The second object
		/// </summary>
		public S Object2;

		/// <summary>
		/// Initializes the pair between o1 and o2
		/// </summary>
		/// <param name="o1">Object 1</param>
		/// <param name="o2">Object 2</param>
		public Pair(F o1, S o2)
		{
			Object1 = o1;
			Object2 = o2;
		}

		/// <summary>
		/// Checks to see if the supplied object equals this object
		/// </summary>
		/// <param name="obj">The object to compare</param>
		/// <returns>True if both object1 and object2 of the other pair are the same as this instance</returns>
		public override bool Equals(object obj)
		{
			Pair<F, S> other = obj as Pair<F, S>;

			return (other != null &&
				(object)other.Object1 == (object)Object1 && 
				(object)other.Object2 == (object)Object2);
		}

		/// <summary>
		/// Combines the hash code of object1 and object2
		/// </summary>
		/// <returns>Returns the pair's hash code</returns>
		public override int GetHashCode()
		{
			return Object1.GetHashCode() + Object2.GetHashCode();
		}
	}
}
