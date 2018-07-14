// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;

namespace UR.Common
{
	/// <summary>
	/// The direction in which to traverse in a binary tree
	/// </summary>
	public enum TreeDirection
	{
		/// <summary>
		/// Go left
		/// </summary>
		Left,
		/// <summary>
		/// Go right
		/// </summary>
		Right,
		/// <summary>
		/// Go nowhere
		/// </summary>
		Found
	}

	/// <summary>
	/// Basic binary tree implementation
	/// </summary>
	public class BinaryTree : IEnumerable
	{
		/// <summary>
		/// Delegate routine that is used to compare two tree nodes when traversing
		/// </summary>
		/// <param name="root">The current node to check</param>
		/// <param name="needle">The instance being searched for</param>
		/// <returns>The direction that the traverser should take</returns>
		public delegate TreeDirection CompareTreeNodes(object root, object needle);

		/// <summary>
		/// A node in the binary tree, having a left and right path and an associated object
		/// </summary>
		public class Node
		{
			/// <summary>
			/// Initializes the node to a blank slate
			/// </summary>
			public Node()
			{
				this.Left = null;
				this.Right = null;
				this.Object = null;
			}

			/// <summary>
			/// The node to the left of this one
			/// </summary>
			public Node Left;
			/// <summary>
			/// The node to the right of this one
			/// </summary>
			public Node Right;
			/// <summary>
			/// The object represented by this node
			/// </summary>
			public object Object;
		}

		/// <summary>
		/// Initializes the binary tree using the supplied comparison routine
		/// </summary>
		/// <param name="compareRoutine"></param>
		public BinaryTree(CompareTreeNodes compareRoutine)
		{
			this.compareRoutine = compareRoutine;
		}

		/// <summary>
		/// Gets an enumerator that will traverse all of the elements in the tree
		/// </summary>
		/// <returns>An enumerator instance</returns>
		public IEnumerator GetEnumerator()
		{
			return new BinaryTreeEnumerator(this);
		}

		/// <summary>
		/// Searches the tree for the specified object using the compareRoutine that was
		/// passed in to the tree's constructor.
		/// </summary>
		/// <param name="key">The object key to search for</param>
		/// <returns>The object associated with the node that matches the key</returns>
		public object FindObject(object key)
		{
			bool finished = false;
			Node current = Root;

			// Keep going until we reach the end of our path through the tree or
			// we're done
			while (current != null && current.Object != null && !finished)
			{
				// Compare the current object with the supplied key
				switch (compareRoutine(current.Object, key))
				{
					case TreeDirection.Left:
						current = current.Left;
						break;
					case TreeDirection.Right:
						current = current.Right;
						break;
					default:
						finished = true;
						break;
				}
			}

			return (current != null) ? current.Object : null;
		}

		/// <summary>
		/// Populates the binary tree from an array that has been pre-sorted
		/// </summary>
		/// <param name="sorted">The sorted array to populate from</param>
		public void FromSortedList(IList sorted)
		{
			// If the array is empty, then just initialize a blank root node
			if (sorted.Count == 0)
				root = new Node();
			// Otherwise, populate it
			else
				root = NodeFromArray(sorted, 0, sorted.Count - 1);
		}

		/// <summary>
		/// Recursive function that populates each node of the tree from the array.
		/// </summary>
		/// <param name="sorted">The sorted array</param>
		/// <param name="lidx">The left node array index</param>
		/// <param name="ridx">The right node array index</param>
		/// <returns>The node associated the index between the left and right index, or null</returns>
		private Node NodeFromArray(IList sorted, int lidx, int ridx)
		{
			Node n = null;

			if (lidx <= ridx)
			{
				int nidx = (lidx + ridx) / 2;

				n = new Node();

				n.Object = sorted[nidx];
				n.Left   = NodeFromArray(sorted, lidx, nidx - 1);
				n.Right  = NodeFromArray(sorted, nidx + 1, ridx);
			}

			return n;
		}

		/// <summary>
		/// The root node in the tree
		/// </summary>
		public Node Root
		{
			get { return root; }
		}
		private Node root;

		/// <summary>
		/// The routine specified in the constructor that is used to find objects
		/// during FindObject
		/// </summary>
		private CompareTreeNodes compareRoutine;
	}

	/// <summary>
	/// Enumerator for nodes in a binary tree.  Enumerator is done in pre-order.
	/// </summary>
	public class BinaryTreeEnumerator : IEnumerator
	{
		/// <summary>
		/// Initializes the enumerator
		/// </summary>
		/// <param name="tree"></param>
		public BinaryTreeEnumerator(BinaryTree tree)
		{
			this.tree = tree;
			this.wl = null;
		}

		/// <summary>
		/// Moves to the next element in the tree
		/// </summary>
		/// <returns></returns>
		public bool MoveNext()
		{
			if (wl == null)
			{
				wl = new WorkList();

				if (tree.Root != null)
					wl.Add(tree.Root);
			}

			if (!wl.IsEmpty)
			{
				current = wl.NextItem() as BinaryTree.Node;

				if (current.Left != null)
					wl.Add(current.Left);
				if (current.Right != null)
					wl.Add(current.Right);
			}
			else
				current = null;

			return current != null;
		}

		/// <summary>
		/// Resets the work list used for enumeration
		/// </summary>
		public void Reset()
		{
			wl = null;
		}

		/// <summary>
		/// Returns the object associated with the current node in the tree
		/// </summary>
		public object Current
		{
			get { return current != null ? current.Object : null; }
		}
		private BinaryTree.Node current;

		/// <summary>
		/// Our work list that we use for enumeration
		/// </summary>
		private WorkList wl;
		/// <summary>
		/// The binary tree associated with this enumerator
		/// </summary>
		private BinaryTree tree;
	}
}
