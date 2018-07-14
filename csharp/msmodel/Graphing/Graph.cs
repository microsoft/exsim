// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections;
using System.Collections.Generic;

using UR.Common;

namespace UR.Graphing
{
	/// <summary>
	/// An abstract graph base class
	/// </summary>
	public abstract class Graph
	{
		/// <summary>
		/// A collection of edges
		/// </summary>
		public abstract ICollection Edges { get; }
		/// <summary>
		/// A collection of vertices
		/// </summary>
		public abstract ICollection Vertices { get; }

		/// <summary>
		/// A friendly label for the graph
		/// </summary>
		public virtual string Label
		{
			get { return label; }
			set { label = value; }
		}
		protected string label;

		/// <summary>
		/// A multi-value dictionary of annotations that associates vertices with markers
		/// </summary>
		private MultiValueDictionary annotations;

		/// <summary>
		/// Initializes the graph
		/// </summary>
		public Graph()
		{
			this.annotations = new MultiValueDictionary();
		}

		/// <summary>
		/// Adds a vertex to the graph
		/// </summary>
		/// <param name="vertex">The opaque vertex to add</param>
		/// <returns>The vertex instance if added, or null if it already exists in the graph</returns>
		public abstract object AddVertex(object vertex);
		/// <summary>
		/// Removes a vertex from the graph
		/// </summary>
		/// <param name="vertex">The opaque vertex to remove</param>
		/// <returns>True if the vertex is removed</returns>
		public abstract bool RemoveVertex(object vertex);
		/// <summary>
		/// Checks to see if the supplied object is a vertex in the graph
		/// </summary>
		/// <param name="vertex">The opaque vertex to check for</param>
		/// <returns>True if the vertex is found in the graph</returns>
		public abstract bool HasVertex(object vertex);

		/// <summary>
		/// Creates an edge instance between a source and target vertex.  The edge is
		/// not added to the graph.
		/// </summary>
		/// <param name="source">The source vertex</param>
		/// <param name="target">The target vertex</param>
		/// <returns>The vertex instance</returns>
		public virtual Edge CreateEdge(object source, object target)
		{
			return CreateEdge(source, target, null);
		}

		/// <summary>
		/// Creates an empty graph with the same type as the graph instance
		/// </summary>
		/// <returns>A new empty graph</returns>
		public virtual Graph CreateGraph()
		{
			return Activator.CreateInstance(
				GetType(),
				new object[] { }) as Graph;
		}

		/// <summary>
		/// Generates a sub-graph of this graph using an opaque graph generator
		/// </summary>
		/// <param name="generator">The generator to use</param>
		/// <returns>A graph derived from this graph</returns>
		public virtual Graph GenerateGraph(IGraphGenerator generator)
		{
			return generator.GenerateGraph(this);
		}

		/// <summary>
		/// Creates an edge instance between a source and target vertex.  The edge is
		/// not added to the graph.
		/// </summary>
		/// <param name="source">The source vertex</param>
		/// <param name="target">The target vertex</param>
		/// <param name="extension">An opaque extension to use with the edge</param>
		/// <returns>The vertex instance</returns>
		public abstract Edge CreateEdge(object source, object target, object extension);

		/// <summary>
		/// Adds an edge to the graph between the specified source and target vertex
		/// </summary>
		/// <param name="source">The source vertex</param>
		/// <param name="target">The target vertex</param>
		/// <returns>The edge instance</returns>
		public abstract Edge AddEdge(object source, object target);	
		/// <summary>
		/// Adds an edge to the graph between the specified source and target vertex
		/// </summary>
		/// <param name="edge">The edge instance to add</param>
		/// <returns>The edge instance</returns>
		public abstract Edge AddEdge(Edge edge);

		/// <summary>
		/// Removes an edge between two vertices
		/// </summary>
		/// <param name="source">The source vertex</param>
		/// <param name="target">The target vertex</param>
		/// <returns>True if the edge was removed</returns>
		public abstract bool RemoveEdge(object source, object target);
		/// <summary>
		/// Removes an edge from the graph
		/// </summary>
		/// <param name="edge">The edge instance to remove</param>
		/// <returns></returns>
		public abstract bool RemoveEdge(Edge edge);

		/// <summary>
		/// Gets the edge that described source -> target
		/// </summary>
		/// <param name="source">The source vertex</param>
		/// <param name="target">The target vertex</param>
		/// <returns>The edge instance associated with the two vertices</returns>
		public virtual Edge GetEdge(object source, object target)
		{
			foreach (Edge successorEdge in Successors(source))
			{
				if (successorEdge.Target == target)
					return successorEdge;
			}

			return null;
		}

		/// <summary>
		/// True if an edge exists between the source and target
		/// </summary>
		/// <param name="source">The source vertex</param>
		/// <param name="target">The target vertex</param>
		/// <returns>True if an edge exists between the source and target</returns>
		public bool HasEdge(object source, object target)
		{
			return GetEdge(source, target) != null;
		}

		/// <summary>
		/// Gets a collection of predecessors for a given vertex
		/// </summary>
		/// <param name="vertex">The vertex to get predecessors for</param>
		/// <returns>A collection of predecessor Edge instances</returns>
		public abstract ICollection Predecessors(object vertex);		
		/// <summary>
		/// Gets a collection of successors for a given vertex
		/// </summary>
		/// <param name="vertex">The vertex to get successors for</param>
		/// <returns>A collection of successor Edge instances</returns>
		public abstract ICollection Successors(object vertex);

		/// <summary>
		/// Adds all of the edges from the supplied graph to this graph
		/// </summary>
		/// <param name="graph">The graph to union with</param>
		public virtual void Union(Graph graph)
		{
			foreach (Edge edge in graph.Edges)
				AddEdge(edge);
		}

		/// <summary>
		/// The root nodes in the graph (having no predecessors)
		/// </summary>
		/// <remarks>
		/// O(n)
		/// </remarks>
		public virtual DefaultSet Roots
		{
			get
			{
				DefaultSet roots = new DefaultSet();

				foreach (object node in Vertices)
				{
					if (Predecessors(node).Count == 0)
						roots.Add(node);
				}

				return roots;
			}
		}

		/// <summary>
		/// Gets the root of the graph
		/// </summary>
		/// <remarks>
		/// If more than one root exists, an exception is thrown
		/// </remarks>
		public virtual object Root
		{
			get
			{
				object root = null;

				foreach (object node in Vertices)
				{
					if (Predecessors(node).Count == 0)
					{
						if (root != null)
							throw new InvalidGraphException("Graph has multiple roots");

						root = node;
					}
				}

				return root;
			}
		}

		/// <summary>
		/// The leaf nodes in the graph (having no successors)
		/// </summary>
		/// <remarks>
		/// O(n)
		/// </remarks>
		public virtual DefaultSet Leaves
		{
			get
			{
				DefaultSet roots = new DefaultSet();

				foreach (object node in Vertices)
				{
					if (Successors(node).Count == 0)
						roots.Add(node);
				}

				return roots;
			}
		}

		/// <summary>
		/// Gets the leaf of the graph
		/// </summary>
		/// <remarks>
		/// If more than one leaf exists, an exception is thrown
		/// </remarks>
		public virtual object Leaf
		{
			get
			{
				object leaf = null;

				foreach (object node in Vertices)
				{
					if (Successors(node).Count == 0)
					{
						if (leaf != null)
							throw new InvalidGraphException("Graph has multiple leaves");

						leaf = node;
					}
				}

				return leaf;
			}
		}

		/// <summary>
		/// Updates predecessor/successor information for an edge
		/// </summary>
		/// <param name="edge">The edge to update information for</param>
		/// <param name="remove">True if the edge is being removed</param>
		protected abstract void UpdatePredSucc(Edge edge, bool remove);

		/// <summary>
		/// Annotates a vertex using a specific marker that is associated with a given object
		/// </summary>
		/// <param name="vertex">The vertex</param>
		/// <param name="marker">The symbolic marker</param>
		/// <param name="obj">The object to associate with the annotation</param>
		public virtual void Annotate(object vertex, int marker, object obj)
		{
			annotations.Add(vertex, new Pair<int, object>(marker, obj));
		}

		/// <summary>
		/// De-annotates a vertex using a specific marker
		/// </summary>
		/// <param name="vertex">The vertex</param>
		/// <param name="marker">The symbolic marker</param>
		public virtual void Deannotate(object vertex, int marker)
		{
			annotations.Remove(vertex, marker);
		}

		/// <summary>
		/// De-annotates a vertex using a specific marker
		/// </summary>
		/// <param name="vertex">The vertex</param>
		/// <param name="marker">The symbolic marker</param>
		public virtual void Deannotate(object vertex)
		{
			annotations.RemoveAll(vertex);
		}

		/// <summary>
		/// Checks to see if a vertex has been annotated by a specific marker
		/// </summary>
		/// <param name="vertex">The vertex</param>
		/// <param name="marker">The symbolic marker</param>
		/// <returns>True if the vertex has been annotated by the marker</returns>
		public virtual bool IsAnnotated(object vertex, int marker)
		{
			return GetAnnotation(vertex, marker) != null;
		}

		/// <summary>
		/// Gets the object associated with a given marker of annotation
		/// </summary>
		/// <param name="vertex">The vertex</param>
		/// <param name="marker">The symbolic marker</param>
		/// <returns>The object that was associated with a given annotation</returns>
		public virtual object GetAnnotation(object vertex, int marker)
		{
			foreach (Pair<int, object> p in annotations.GetValues(vertex))
			{
				if (p.Object1 == marker)
					return p.Object2;
			}

			return null;
		}

		/// <summary>
		/// Returns the markers that have annotated a given vertex
		/// </summary>
		/// <param name="vertex">The vertex in question</param>
		/// <returns>The set of markers that have annotated this vertex</returns>
		public virtual Set GetAnnotationMarkers(object vertex)
		{
			DefaultSet set = new DefaultSet();

			foreach (Pair<int, object> p in annotations.GetValues(vertex))
				set.Add(p.Object1);

			return set;
		}

		/// <summary>
		/// Serializes the graph contents to a GraphML file
		/// </summary>
		/// <param name="path">The path to store the file in</param>
		/// <param name="custom">Custom display style information to apply to the serializer</param>
		public void ToGraphML(string path, CustomStyle custom)
		{
			GraphSerializer.ToGraphML(this, path, custom);
		}

		/// <summary>
		/// Serializes the graph contents to a GraphML file
		/// </summary>
		/// <param name="path">The path to store the file in</param>
		public void ToGraphML(string path)
		{
			ToGraphML(path, new DefaultGraphMLCustomStyle());
		}

		/// <summary>
		/// Traverses the graph visiting successors starting at a given vertex
		/// </summary>
		/// <param name="vertex">The focal vertex</param>
		public virtual void Navigate(
			object vertex,
			GraphVisitor visitor)
		{
			ForwardGraphNavigator.NavigateGraph(this, vertex, visitor);
		}

		/// <summary>
		/// Traverses the graph using a specific navigator starting at a given vertex
		/// </summary>
		/// <param name="vertex">The focal vertex</param>
		public virtual void Navigate(
			object vertex,
			GraphVisitor visitor,
			GraphNavigator navigator)
		{
			navigator.Navigate(vertex, visitor);
		}

		/// <summary>
		/// True if there are no vertices in the graph
		/// </summary>
		public bool IsEmpty
		{
			get { return Vertices.Count == 0; }
		}
	}

	/// <summary>
	/// An undirected graph
	/// </summary>
	public class UndirectedGraph : Graph
	{
		/// <summary>
		/// Initializes the undirected graph
		/// </summary>
		public UndirectedGraph()
		{
			vertices = new DefaultSet();
			edges = new DefaultSet();
			succs = new MultiValueDictionary();
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <param name="edge"></param>
		/// <returns></returns>
		public override Edge AddEdge(Edge edge)
		{
			AddVertex(edge.Source);
			AddVertex(edge.Target);

			if (edges.Add(edge))
			{
				UpdatePredSucc(edge, false);

				return edge;
			}

			return null;
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public override Edge AddEdge(object source, object target)
		{
			return AddEdge(CreateEdge(source, target));
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <param name="vertex"></param>
		/// <returns></returns>
		public override object AddVertex(object vertex)
		{
			if (vertices.Add(vertex))
				return vertex;

			return null;
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <param name="vertex"></param>
		/// <returns></returns>
		public override bool HasVertex(object vertex)
		{
			return vertices.Contains(vertex);
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <param name="extension"></param>
		/// <returns></returns>
		public override Edge CreateEdge(object source, object target, object extension)
		{
			return new UndirectedEdge(source, target, extension);
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <param name="edge"></param>
		/// <returns></returns>
		public override bool RemoveEdge(Edge edge)
		{
			UpdatePredSucc(edge, true);

			return edges.Remove(edge);
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public override bool RemoveEdge(object source, object target)
		{
			return edges.Remove(CreateEdge(source, target));
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <param name="vertex"></param>
		/// <returns></returns>
		public override bool RemoveVertex(object vertex)
		{
			RemoveAllEdgesToVertex(vertex);

			Deannotate(vertex);

			return vertices.Remove(vertex);
		}

		/// <summary>
		/// Removes all of the edges to a given vertex
		/// </summary>
		/// <param name="vertex">The vertex being removed</param>
		protected virtual void RemoveAllEdgesToVertex(object vertex)
		{
			WorkList wl = new WorkList(Successors(vertex));

			while (!wl.IsEmpty)
				RemoveEdge(wl.NextItem() as Edge);
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <param name="vertex"></param>
		/// <returns></returns>
		public override ICollection Predecessors(object vertex)
		{
			return succs.GetValues(vertex);
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <param name="vertex"></param>
		/// <returns></returns>
		public override ICollection Successors(object vertex)
		{
			return succs.GetValues(vertex);
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <param name="edge"></param>
		/// <param name="remove"></param>
		protected override void UpdatePredSucc(Edge edge, bool remove)
		{
			if (remove)
			{
				succs.Remove(edge.Source, edge);
				succs.Remove(edge.Target, edge);
			}
			else
			{
				succs.Add(edge.Source, edge);
				succs.Add(edge.Target, edge);
			}
		}

		/// <summary>
		/// The set of vertices
		/// </summary>
		protected DefaultSet vertices;
		/// <summary>
		/// The set of edges
		/// </summary>
		protected DefaultSet edges;
		/// <summary>
		/// A multi-value dictionary of successors
		/// </summary>
		protected MultiValueDictionary succs;

		/// <summary>
		/// See base class
		/// </summary>
		public override ICollection Edges
		{
			get { return edges; }
		}
		/// <summary>
		/// See base class
		/// </summary>
		public override ICollection Vertices
		{
			get { return vertices; }
		}

	}

	/// <summary>
	/// A directed graph
	/// </summary>
	public class DirectedGraph : UndirectedGraph
	{
		/// <summary>
		/// Initializes the directed graph
		/// </summary>
		public DirectedGraph()
		{
			succs = new MultiValueDictionary();
			preds = new MultiValueDictionary();
		}

		/// <summary>
		/// Creates a DirectedEdge, see base class.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <param name="extension"></param>
		/// <returns></returns>
		public override Edge CreateEdge(object source, object target, object extension)
		{
			return new DirectedEdge(source, target, extension);
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <param name="vertex"></param>
		/// <returns></returns>
		public override ICollection Predecessors(object vertex)
		{
			return preds.GetValues(vertex);
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <param name="vertex"></param>
		protected override void RemoveAllEdgesToVertex(object vertex)
		{
			WorkList wl = new WorkList(Predecessors(vertex));

			while (!wl.IsEmpty)
				RemoveEdge(wl.NextItem() as Edge);

			base.RemoveAllEdgesToVertex(vertex);
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <param name="edge"></param>
		/// <param name="remove"></param>
		protected override void UpdatePredSucc(Edge edge, bool remove)
		{
			if (remove)
			{
				preds.Remove(edge.Target, edge);
				succs.Remove(edge.Source, edge);
			}
			else
			{
				preds.Add(edge.Target, edge);
				succs.Add(edge.Source, edge);
			}
		}

		/// <summary>
		/// Vertices in pre-order (top to bottom)
		/// </summary>
		public IEnumerable<object> TopDownVertices
		{
			get
			{
				DefaultSet visited = new DefaultSet();
				WorkList wl = new WorkList();

				// Add all of our roots
				foreach (object vertex in Roots)
					wl.Add(vertex);

				// Walk the work list until we've processed all work items
				while (!wl.IsEmpty)
				{
					object vertex = wl.Dequeue();

					// Mark this vertex as having been visited
					visited.Add(vertex);

					// Return this vertex to the caller
					yield return vertex;

					// Add the successors to the work list
					foreach (Edge e in Successors(vertex))
					{
						if (visited.Contains(e.Target))
							continue;

						wl.Add(e.Target);
					}
				}
			}
		}

		/// <summary>
		/// Vertices in post-order (bottom to top)
		/// </summary>
		public IEnumerable<object> BottomUpVertices
		{
			get
			{
				DefaultSet visited = new DefaultSet();
				WorkList wl = new WorkList();

				// Add all of our leaves
				foreach (object vertex in Leaves)
					wl.Add(vertex);

				// Walk the work list until we've processed all work items
				while (!wl.IsEmpty)
				{
					object vertex = wl.Dequeue();

					// Mark this vertex as having been visited
					visited.Add(vertex);

					// Return this vertex to the caller
					yield return vertex;

					// Add the predecessors to the work list
					foreach (Edge e in Predecessors(vertex))
					{
						if (visited.Contains(e.Source))
							continue;

						wl.Add(e.Source);
					}
				}
			}
		}

		/// <summary>
		/// The mult-value relationship of vertex to predecessor edges
		/// </summary>
		protected MultiValueDictionary preds;
	}

	/// <summary>
	/// An abstract descriptor for a vertex that is used to determine if a vertex meets an arbitrary criteria
	/// </summary>
	public interface IVertexDescriptor
	{
		/// <summary>
		/// Checks to see if the supplied vertex matches the criteria of this descriptor
		/// </summary>
		/// <param name="vertex">The vertex to check</param>
		/// <returns>True if the vertex matches the criteria of this descriptor</returns>
		bool IsMatchingVertex(object vertex);
	}

	/// <summary>
	/// Generates a graph using the provided graph
	/// </summary>
	/// <remarks>
	/// The manner of the generation is entirely abstract.
	/// </remarks>
	public interface IGraphGenerator
	{
		Graph GenerateGraph(Graph graph);
	}

	/// <summary>
	/// An edge base class
	/// </summary>
	public class Edge
	{
		/// <summary>
		/// The opaque source vertex
		/// </summary>
		public object Source;
		/// <summary>
		/// The opaque target vertex
		/// </summary>
		public object Target;
		/// <summary>
		/// An opaque extension context
		/// </summary>
		public object Extension;

		/// <summary>
		/// Initializes the edge instance
		/// </summary>
		/// <param name="source">The opaque source vertex</param>
		/// <param name="target">The opaque target vertex</param>
		/// <param name="extension">The opaque extension context</param>
		public Edge(object source, object target, object extension)
		{
			this.Source = source;
			this.Target = target;
			this.Extension = extension;
		}

		/// <summary>
		/// The hash code is a combination of the target, source, and extension hashes
		/// </summary>
		/// <returns>The unique hash code</returns>
		public override int GetHashCode()
		{
			return Target.GetHashCode() + Source.GetHashCode() + ((Extension != null) ? Extension.GetHashCode() : 0);
		}

		/// <summary>
		/// Checks to see if the supplied object has the same Source, Target, and Extension context
		/// </summary>
		/// <param name="obj">The object to compare</param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			Edge other = obj as Edge;

			return ((other != null) &&
				(other.Source == Source) &&
				(other.Target == Target) &&
				(other.Extension == Extension));
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <returns>See base class</returns>
		public override string ToString()
		{
			return String.Format("{0} -> {1} [{2}]", Source, Target, Extension);
		}
	}

	/// <summary>
	/// An undirected edge
	/// </summary>
	public class UndirectedEdge : Edge
	{
		/// <summary>
		/// Initializes the undirected edge
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <param name="extension"></param>
		public UndirectedEdge(object source, object target, object extension)
			: base(source, target, extension)
		{
		}
	}

	/// <summary>
	/// A directed edge
	/// </summary>
	public class DirectedEdge : Edge
	{
		/// <summary>
		/// Initializes the directed edge
		/// </summary>
		/// <param name="source"></param>
		/// <param name="target"></param>
		/// <param name="extension"></param>
		public DirectedEdge(object source, object target, object extension)
			: base(source, target, extension)
		{
		}

		/// <summary>
		/// Derives the hash code taking into account that target and source are distinct
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return (String.Format("{0}|{1}|{2}",
				Source.GetHashCode(),
				Target.GetHashCode(),
				(Extension != null) ? Extension.GetHashCode() : 0)).GetHashCode();
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <param name="obj">See base class</param>
		/// <returns>See base class</returns>
		public override bool Equals(object obj)
		{
			return obj.GetHashCode() == GetHashCode();
		}
	}

	/// <summary>
	/// A work-list edge item entry
	/// </summary>
	public class EdgeItem
	{
		/// <summary>
		/// Initializes the edge item
		/// </summary>
		/// <param name="edge">The edge to associate with</param>
		public EdgeItem(Edge edge)
			: this(edge, null)
		{
		}

		/// <summary>
		/// Initializes the edge item
		/// </summary>
		/// <param name="edge">The edge to associate with</param>
		/// <param name="parentEdgeItem">The edge item's parent</param>
		public EdgeItem(Edge edge, EdgeItem parentEdgeItem)
		{
			this.edge = edge;
			this.parentEdgeItem = parentEdgeItem;
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <returns>See base class</returns>
		public override int GetHashCode()
		{
			return String.Format("{0}-{1}",
				edge.GetHashCode(),
				(parentEdgeItem != null) ? parentEdgeItem.GetHashCode() : 0).GetHashCode();
		}

		/// <summary>
		/// See base class
		/// </summary>
		/// <param name="obj">See base class</param>
		/// <returns>See base class</returns>
		public override bool Equals(object obj)
		{
			EdgeItem other = obj as EdgeItem;
			bool matchFound = false;

			if (other != null)
			{
				if ((ParentEdgeItem != null &&
					 other.ParentEdgeItem != null))
					matchFound = ParentEdgeItem.Edge == other.ParentEdgeItem.Edge;
				else if ((ParentEdgeItem == null &&
					other.ParentEdgeItem == null))
					matchFound = true;

				matchFound = matchFound && other.Edge == Edge;
			}

			return matchFound;
		}

		/// <summary>
		/// The edge being worked
		/// </summary>
		public Edge Edge
		{
			get { return edge; }
		}
		private Edge edge;

		/// <summary>
		/// The parent of this edge item
		/// </summary>
		public EdgeItem ParentEdgeItem
		{
			get { return parentEdgeItem; }
		}
		private EdgeItem parentEdgeItem;
	}
}
