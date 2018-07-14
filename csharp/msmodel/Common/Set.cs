// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;

namespace UR.Common
{
	/// <summary>
	/// Set join operator
	/// </summary>
	/// <param name="a">Set A</param>
	/// <param name="b">Set B</param>
	public delegate void SetJoinOperator(Set a, Set b);

	/// <summary>
	/// A static class containing default sets, like the empty set
	/// </summary>
	public static class DefaultSets
	{
		/// <summary>
		/// The generalized empty set
		/// </summary>
		public readonly static ICollection EmptySet = new ArrayList();
	}

	/// <summary>
	/// The base class for the different types of sets
	/// </summary>
	public abstract class Set : ICollection
	{
		/// <summary>
		/// Adds an object to the set
		/// </summary>
		/// <param name="member">The object to add</param>
		/// <returns>True if the object is not already a member of the set</returns>
		public abstract bool Add(object member);
		/// <summary>
		/// Clears the set
		/// </summary>
		public abstract void Clear();
		/// <summary>
		/// Removes an object from the set
		/// </summary>
		/// <param name="member">The object to remove</param>
		/// <returns>True if the object is removed from the set</returns>
		public abstract bool Remove(object member);
		/// <summary>
		/// Checks to see if the set contains the supplied member
		/// </summary>
		/// <param name="member">The object to check for</param>
		/// <returns>True if the object is a member of the set</returns>
		public abstract bool Contains(object member);

		/// <summary>
		/// Creates an intersection of two sets
		/// </summary>
		/// <param name="first">The first set</param>
		/// <param name="second">The second set</param>
		/// <returns>A set containing the common members of both sets</returns>
		public static Set Intersect(Set first, Set second)
		{
			// Create the resulting set as a 
			Set resultSet = Activator.CreateInstance(first.GetType()) as Set;
			Set focalSet;
			Set otherSet;

			// If the other set has a smaller count, then we will use that
			// as a focal set so as to reduce the number of iterations
			if (second.Count < first.Count)
			{
				otherSet = first;
				focalSet = second;
			}
			// Otherewise, our set is the focal set
			else
			{
				otherSet = second;
				focalSet = first;
			}

			// Go through each member in the focal set, checking to see if it is
			// also a member of the other set
			foreach (object member in focalSet)
			{
				// Is this a member of the other set?  If so, then it intersects.
				if (otherSet.Contains(member))
					resultSet.Add(member);
			}

			return resultSet;
		}

		/// <summary>
		/// Creates a union of two sets
		/// </summary>
		/// <param name="first">The first set</param>
		/// <param name="second">The second set</param>
		/// <returns>The resulting unioned set</returns>
		public static Set Union(Set first, Set second)
		{
			// Create the resulting set as a 
			Set resultSet = Activator.CreateInstance(first.GetType()) as Set;

			// Add the members from both the first and second set to perform
			// a set union
			foreach (object member in first)
				resultSet.Add(member);
			foreach (object member in second)
				resultSet.Add(member);

			return resultSet;
		}

		#region ICollection Members
		public abstract void CopyTo(Array array, int index);
		public abstract int Count { get; }
		public abstract bool IsSynchronized { get; }
		public abstract object SyncRoot { get; }
		#endregion

		#region IEnumerable Members
		public abstract IEnumerator GetEnumerator();
		#endregion
	}

	/// <summary>
	/// A default set uses a hash table internally so that only unique members can be contained within it
	/// </summary>
	public class DefaultSet : Set
	{
		/// <summary>
		/// Initializes the hash table
		/// </summary>
		public DefaultSet()
		{
			hash = new Hashtable();    
		}

		/// <summary>
		/// Adds a member to the default set if it does not already exist in the set
		/// </summary>
		/// <param name="member">The member instance to add</param>
		/// <returns>True if the member is added, false if it already exists in the set</returns>
		public override bool Add(object member)
		{
			if (hash.ContainsKey(member))
				return false;

			hash[member] = true;

			return true;
		}

		/// <summary>
		/// Removes all entries from the default set
		/// </summary>
		public override void Clear()
		{
			hash.Clear();
		}

		/// <summary>
		/// Removes a member from the set
		/// </summary>
		/// <param name="member">The member to remove</param>
		/// <returns>True if the member is removed, false if it doesn't exist in the set</returns>
		public override bool Remove(object member)
		{
			if (hash.ContainsKey(member) == false)
				return false;

			hash.Remove(member);

			return true;
		}

		/// <summary>
		/// Checks to see if the member exists in the set
		/// </summary>
		/// <param name="member">The member to check for</param>
		/// <returns>True if the member exists in the set</returns>
		public override bool Contains(object member)
		{
			return hash.ContainsKey(member);
		}

		/// <summary>
		/// Copyies members of the default set to the supplied array
		/// </summary>
		/// <param name="array">The array to copy to</param>
		/// <param name="index">The index to start copying at</param>
		public override void CopyTo(Array array, int index)
		{
			hash.CopyTo(array, index);
		}

		/// <summary>
		/// The number of members in the set
		/// </summary>
		public override int Count { get { return hash.Count; } }
		/// <summary>
		/// Pass along to the hash table
		/// </summary>
		public override bool IsSynchronized { get { return hash.IsSynchronized; } }
		/// <summary>
		/// Pass along to the hash table
		/// </summary>
		public override object SyncRoot { get { return hash.SyncRoot; } }

		/// <summary>
		/// Gets an enumerator for the members in the set
		/// </summary>
		/// <returns>An enumerator instance</returns>
		public override IEnumerator GetEnumerator()
		{
			return hash.Keys.GetEnumerator();
		}

		/// <summary>
		/// The hash table used to contain the members
		/// </summary>
		private Hashtable hash;
	}
}
