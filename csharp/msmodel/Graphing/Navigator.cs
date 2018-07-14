// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;
using System.Collections.Generic;

using UR.Common;

namespace UR.Graphing
{
	/// <summary>
	/// The direction of enumeration
	/// </summary>
	public enum Direction
	{
		Forward,
		Backward,
		Random
	}

	/// <summary>
	/// Base class that is used to navigate a graph
	/// </summary>
	public abstract class GraphNavigator
	{
		/// <summary>
		/// Initializes the navigator and ties it to a particular graph
		/// </summary>
		/// <param name="graph">The graph being navigated</param>
		public GraphNavigator(Graph graph)
		{
			this.graph = graph;
			this.constraints = new List<NavigatorConstraint>();
		}

		/// <summary>
		/// Gets the next vertices to visit in the graph and stores them in the provided work list
		/// </summary>
		/// <param name="vertex">The vertex to get the next vertices from</param>
		/// <param name="wl">The work list to add the next edge items to</param>
		/// <returns>True if it's possible to navigate further, false if not</returns>
		public abstract bool GetNext(
			object vertex,
			Queue<EdgeItem> wl);

		/// <summary>
		/// Navigates a graph starting at a given focal vertex
		/// </summary>
		/// <param name="vertex">The vertex to start at</param>
		/// <param name="visitor">The visitor to call for each vertex</param>
		public virtual void Navigate(
			object rootVertex,
			GraphVisitor visitor)
		{
			Queue<EdgeItem> wl = new Queue<EdgeItem>();

			// Set the navigator being used for this visitor
			visitor.Navigator = this;

			// Add the focal vertex to the work list
			GetNext(rootVertex, wl);

			// Until the work list is empty...
			while (wl.Count > 0)
			{
				// Set the current edge item and target vertex
				this.currentEdgeItem = wl.Dequeue();
				this.currentVertex = GetDirectionOrientedTargetFromEdge(currentEdgeItem.Edge);

                try
                {

                    // If the visitor returns false, then we should stop our navigation
                    if (!visitor.Visit(currentVertex))
                        break;

                    // We've finished visiting...
                    visitor.PostVisit(currentVertex);
                }
                catch (Exception e)
                {
                    if (e is SkipChildrenException || e.InnerException is SkipChildrenException)
                    {
                        continue;
                    }
                }

				// Apply constraints to this vertex now that it's been visited
				ApplyConstraints(currentVertex);

				// If we fail to get the next elements, then we should stop our navigation
				if (!GetNext(currentVertex, wl))
					break;
			}
		}

		/// <summary>
		/// Adds the supplied constraint to the list of constraints for this navigator
		/// </summary>
		/// <param name="constraint">The constraint to add</param>
		public void AddConstraint(
			NavigatorConstraint constraint)
		{
			constraints.Add(constraint);
		}

		/// <summary>
		/// Checks to see if the supplied vertex meets the necessary constraints
		/// </summary>
		/// <param name="vertex">The vertex to check</param>
		/// <returns>True if the vertex meets the appropriate constraints</returns>
		public bool CheckConstraints(
			object vertex)
		{
			foreach (NavigatorConstraint constraint in constraints)
			{
				if (!constraint.CheckConstraint(graph, vertex))
					return false;
			}

			return true;
		}

		/// <summary>
		/// After a vertex has been visited, constraints are applied
		/// </summary>
		/// <param name="vertex"></param>
		public void ApplyConstraints(
			object vertex)
		{
			foreach (NavigatorConstraint constraint in constraints)
				constraint.ApplyConstraint(graph, vertex);
		}

		/// <summary>
		/// Get vertices based on which direction we are traversing
		/// </summary>
		/// <param name="vertex"></param>
		/// <returns></returns>
		public ICollection GetAdjacentVertices(object vertex)
		{
			if (Direction == Direction.Forward)
				return Graph.Successors(vertex);
			else
				return Graph.Predecessors(vertex);
		}

		/// <summary>
		/// Get the direction oriented target for an edge
		/// </summary>
		/// <param name="edge">The edge to get the target for (based on navigation direction)</param>
		/// <returns>The target vertex</returns>
		public object GetDirectionOrientedTargetFromEdge(Edge edge)
		{
			if (Direction == Direction.Forward)
				return edge.Target;
			else
				return edge.Source;
		}

		/// <summary>
		/// Get the direction oriented source for an edge
		/// </summary>
		/// <param name="edge">The edge to get the source for (based on navigation direction)</param>
		/// <returns>The source vertex</returns>
		public object GetDirectionOrientedSourceFromEdge(Edge edge)
		{
			if (Direction == Direction.Backward)
				return edge.Target;
			else
				return edge.Source;
		}

		/// <summary>
		/// The graph being operated on
		/// </summary>
		public Graph Graph
		{
			get { return graph; }
		}
		private Graph graph;

		/// <summary>
		/// The current edge item
		/// </summary>
		public EdgeItem CurrentEdgeItem
		{
			get { return currentEdgeItem; }
		}
		private EdgeItem currentEdgeItem;

		/// <summary>
		/// The current vertex being visited
		/// </summary>
		public object CurrentVertex
		{
			get { return currentVertex; }
		}
		private object currentVertex;

		/// <summary>
		/// The current edge stack
		/// </summary>
		public ICollection<Edge> CurrentEdgeStack
		{
			get
			{
				EdgeItem item = CurrentEdgeItem;
				List<Edge> stack = new List<Edge>();

				while (item != null)
				{
					stack.Insert(0, item.Edge);

					item = item.ParentEdgeItem;
				}

				return stack;
			}
		}

		/// <summary>
		/// Determines if the edge has already been encountered in the current stack
		/// </summary>
		/// <param name="edge">The edge to check</param>
		/// <returns>True if the edge has already been encountered</returns>
		protected bool HasTraversedInCurrentEdgeStack(Edge edge)
		{
			foreach (Edge currentEdge in CurrentEdgeStack)
			{
				if (currentEdge == edge)
					return true;
			}

			return false;
		}

		/// <summary>
		/// The direction of navigation
		/// </summary>
		public virtual Direction Direction
		{
			get { return Direction.Forward; }
		}

		/// <summary>
		/// The list of registered constraints for this navigator
		/// </summary>
		private List<NavigatorConstraint> constraints;
	}

    public class SkipChildrenException : Exception
    {
    }

	/// <summary>
	/// Navigates the graph by visiting successor vertices
	/// </summary>
	public class ForwardGraphNavigator : GraphNavigator
	{
		/// <summary>
		/// Navigates the graph from a focal vertex to successors.
		/// </summary>
		/// <param name="graph">The graph to navigate</param>
		/// <param name="vertex">The vertex to start at</param>
		/// <param name="visitor">The visitor to call at each vertex</param>
		public static void NavigateGraph(
			Graph graph,
			object vertex,
			GraphVisitor visitor)
		{
			GraphNavigator n = new ForwardGraphNavigator(graph);
			
			n.Navigate(vertex, visitor);
		}

		/// <summary>
		/// Initializes the navigator
		/// </summary>
		/// <param name="graph">The graph to tie the navigator to</param>
		public ForwardGraphNavigator(Graph graph)
			: base(graph)
		{
		}

		/// <summary>
		/// See base class description; visits successors
		/// </summary>
		/// <param name="vertex">The vertex to get the next vertices from</param>
		/// <param name="wl">The work list to add the next vertices to</param>
		/// <returns>True if it's possible to navigate further, false if not</returns>
		public override bool GetNext(
			object vertex,
			Queue<EdgeItem> wl)
		{
			foreach (Edge edge in Graph.Successors(vertex))
			{
				EdgeItem item = new EdgeItem(edge, CurrentEdgeItem);

				if (!CheckConstraints(edge.Target) ||
					item == CurrentEdgeItem ||
					wl.Contains(item) ||
					HasTraversedInCurrentEdgeStack(edge))
					continue;

				wl.Enqueue(item);
			}

			return true;
		}
	}

	/// <summary>
	/// Navigates the graph by visiting predecessor vertices
	/// </summary>
	public class BackwardGraphNavigator : GraphNavigator
	{
		/// <summary>
		/// Navigates the graph from a focal vertex to predecessors.
		/// </summary>
		/// <param name="graph">The graph to navigate</param>
		/// <param name="vertex">The vertex to start at</param>
		/// <param name="visitor">The visitor to call at each vertex</param>
		public static void NavigateGraph(
			Graph graph,
			object vertex,
			GraphVisitor visitor)
		{
			GraphNavigator n = new BackwardGraphNavigator(graph);

			n.Navigate(vertex, visitor);
		}

		/// <summary>
		/// Initializes the navigator
		/// </summary>
		/// <param name="graph">The graph to tie the navigator to</param>
		public BackwardGraphNavigator(Graph graph)
			: base(graph)
		{
		}

		/// <summary>
		/// See base class description; visits predecessors
		/// </summary>
		/// <param name="vertex">The vertex to get the next vertices from</param>
		/// <param name="wl">The work list to add the next vertices to</param>
		/// <returns>True if it's possible to navigate further, false if not</returns>
		public override bool GetNext(
			object vertex,
			Queue<EdgeItem> wl)
		{
			foreach (Edge edge in Graph.Predecessors(vertex))
			{
				EdgeItem item = new EdgeItem(edge, CurrentEdgeItem);

				if (!CheckConstraints(edge.Source) ||
					item == CurrentEdgeItem ||
					wl.Contains(item) ||
					HasTraversedInCurrentEdgeStack(edge))
					continue;

				wl.Enqueue(item);
			}

			return true;
		}

		/// <summary>
		/// Backward direction
		/// </summary>
		public override Direction Direction
		{
			get { return Direction.Backward; }
		}
	}

	/// <summary>
	/// Abstract base class that is used to enforce navigation constraints
	/// </summary>
	public abstract class NavigatorConstraint
	{
		/// <summary>
		/// Checks to see if the supplied vertex meets the necessary requirements to be visited
		/// </summary>
		/// <param name="graph">The graph to operate on</param>
		/// <param name="v">The vertex to check</param>
		/// <returns>True if the vertex meets the requirements, false if not</returns>
		public abstract bool CheckConstraint(
			Graph graph,
			object v);

		/// <summary>
		/// After a vertex passes a constraint, the navigator will give it a chance
		/// to apply the constraint in the future.
		/// </summary>
		/// <param name="graph">The graph to operate on</param>
		/// <param name="v">The vertex to apply the constraint to</param>
		public virtual void ApplyConstraint(
			Graph graph,
			object v)
		{
		}
	}

	/// <summary>
	/// Constrains traversal to nodes that have not been visisted already
	/// </summary>
	public class AlreadyVisistedConstraint : NavigatorConstraint
	{
		/// <summary>
		/// Initializes the constraint instance
		/// </summary>
		public AlreadyVisistedConstraint()
		{
			visited = new DefaultSet();
		}

		/// <summary>
		/// Checks to see if a vertex has already been visited
		/// </summary>
		/// <param name="graph">The graph being operated on</param>
		/// <param name="v">The vertex to check</param>
		/// <returns>True if the vertex has not been visited</returns>
		public override bool CheckConstraint(Graph graph, object v)
		{
			return !visited.Contains(v);
		}

		/// <summary>
		/// Marks a vertex as having been visited
		/// </summary>
		/// <param name="graph">The graph being operated on</param>
		/// <param name="v">The vertex to mark</param>
		public override void ApplyConstraint(Graph graph, object v)
		{
			visited.Add(v);
		}

		/// <summary>
		/// The set of vertices that have been visited
		/// </summary>
		private DefaultSet visited;
	}
}
