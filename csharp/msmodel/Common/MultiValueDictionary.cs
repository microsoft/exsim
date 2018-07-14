// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UR.Common
{
	/// <summary>
	/// A multi-value dictionary associates keys with multiple values
	/// </summary>
	public class MultiValueDictionary
	{
		/// <summary>
		/// Initializes the dictionary
		/// </summary>
		public MultiValueDictionary()
		{
			values = new Dictionary<object,List<object>>();
		}

		/// <summary>
		/// Adds a value association to a given key
		/// </summary>
		/// <param name="key">The key</param>
		/// <param name="value">The value to associate</param>
		/// <returns>True if the value does not already exist</returns>
		public virtual bool Add(object key, object value)
		{
			if (!values.ContainsKey(key))
				values.Add(key, new List<object>());

			List<object> ary = values[key];

			if (ary.Contains(value))
				return false;

			ary.Add(value);

			return true;
		}

		/// <summary>
		/// Removes a value association from a key
		/// </summary>
		/// <param name="key">The key</param>
		/// <param name="value">The value to disassociate</param>
		/// <returns>True if the value is successfully removed</returns>
		public virtual bool Remove(object key, object value)
		{
			if (!values.ContainsKey(key))
				return false;

			List<object> ary = values[key];

			if (!ary.Contains(value))
				return false;

			ary.Remove(value);

			return true;
		}

		/// <summary>
		/// Removes all values associated with a given key
		/// </summary>
		/// <param name="key">The key to use</param>
		/// <returns>True if all values are removed</returns>
		public virtual bool RemoveAll(object key)
		{
			if (!values.ContainsKey(key))
				return false;

			values.Remove(key);

			return true;
		}

		/// <summary>
		/// A collection of keys contained within the dictionary
		/// </summary>
		public virtual ICollection Keys
		{
			get { return values.Keys; }
		}

		/// <summary>
		/// Gets a collection of values associated with a key
		/// </summary>
		/// <param name="key">The key to get the values of</param>
		/// <returns>A collection containing zero or more values for a key</returns>
		public virtual ICollection GetValues(object key)
		{
            if (!values.ContainsKey(key))
                return DefaultSets.EmptySet;

			ICollection collection = values[key] as ICollection;

			// If no values were found, returned the default empty set
			if (collection == null)
				collection = DefaultSets.EmptySet;

			return collection;
		}

		/// <summary>
		/// Checks to see if the supplied key is in the dictionary
		/// </summary>
		/// <param name="key">The key to check for</param>
		/// <returns>True if the key is in the dictionary</returns>
		public virtual bool ContainsKey(object key)
		{
			return values.ContainsKey(key);
		}

		/// <summary>
		/// The hash table that associates keys to value lists
		/// </summary>
        private Dictionary<object, List<object>> values;
	}
}
