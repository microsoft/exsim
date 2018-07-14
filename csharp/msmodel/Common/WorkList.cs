// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UR.Common
{
	/// <summary>
	/// A simple queue wrapper that can be used to simplify analysis
	/// </summary>
	public class WorkList : Queue
	{
		/// <summary>
		/// Initializes the work list
		/// </summary>
		public WorkList()
		{
		}

		/// <summary>
		/// Initializes and pre-populates the work list
		/// </summary>
		/// <param name="set">The collection of items to pre-populate the work list with</param>
		public WorkList(IEnumerable set)
		{
			foreach (object v in set)
				Add(v);
		}

		/// <summary>
		/// True if the work list has no work items.
		/// </summary>
		public bool IsEmpty
		{
			get { return Count == 0; }
		}

		/// <summary>
		/// Adds a work item to the work list
		/// </summary>
		/// <param name="v">The object to add</param>
		public void Add(object v)
		{
			Enqueue(v);
		}

		/// <summary>
		/// Adds a collection of items
		/// </summary>
		/// <param name="set">The set of items to add</param>
		public void Add(ICollection set)
		{
			foreach (object v in set)
				Add(v);
		}

		/// <summary>
		/// Proceeds to the next item in the work list
		/// </summary>
		/// <returns>A valid object if an item exists</returns>
		public object NextItem()
		{
			return Dequeue();
		}
	}

	/// <summary>
	/// A collection of work items that can be added to a work list
	/// </summary>
	public class WorkSet : List<object>
	{
	}
}