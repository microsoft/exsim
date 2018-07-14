// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UR.Common;

namespace UR.Graphing.Generators
{
	/// <summary>
	/// Generates a depth-first spanning tree from the supplied rooted graph
	/// </summary>
	/// <remarks>
	/// Implemented in reference to algorithm from "Advanced Compiler Design & Implementation" pg 180
	/// 
	/// Needs some double checking as to producing the correct results.  While the algorithm
	/// mirrors the book's description, the output does not seem accurate.  For instance, it
	/// produces a cross edge from 3->5 in the example.  However, this should be a forward edge
	/// as 3 is an ancestor of 5.  This happens because the 1->6 edge is taken before the 1->2 edge
	/// causing 3's pre number to be greater than 5's pre number.
	/// </remarks>
	public class DepthFirstSpanningTreeGenerator
		: IGraphGenerator
	{
		/// <summary>
		/// Initializer
		/// </summary>
		public DepthFirstSpanningTreeGenerator()
			: this(false)
		{
		}

		/// <summary>
		/// Initializer
		/// </summary>
		/// <param name="onlyTreeEdges">True if only tree edges should be added</param>
		public DepthFirstSpanningTreeGenerator(bool onlyTreeEdges)
		{
			this.OnlyTreeEdges = onlyTreeEdges;
		}

		/// <summary>
		/// Generates a depth-first spanning tree from the supplied rooted graph
		/// </summary>
		/// <param name="graph">The rooted graph to analyze</param>
		/// <returns>The resultant depth-first spanning tree as a graph</returns>
		public Graph GenerateGraph(Graph graph)
		{
			State state = new State(graph);

			GenerateGraph(
				state,
				graph.Root);

			return state.SubGraph;
		}

		/// <summary>
		/// Recursive routine that generates the depth-first spanning tree
		/// </summary>
		/// <param name="state">The state being operated upon</param>
		private void GenerateGraph(
			State state,
			object current)
		{
			// Flag this vertex as having been visited and set its pre number
			state.Visited.Add(current);
			state.Pre[current] = state.PreNumber;

			foreach (Edge successorEdge in state.ParentGraph.Successors(current))
			{
				object successor = successorEdge.Target;

				// If we haven't visited this vertex, we will visit it and
				// create a tree edge between the current vertex and the successor
				if (!state.Visited.Contains(successor))
				{
					GenerateGraph(
						state,
						successor);

					state.SubGraph.AddEdge(
						new DepthFirstSpanningTreeEdge.Tree(
							current,
							successor));
				}
				else if (!OnlyTreeEdges)
				{
					// If the current vertex has a pre number that is lower than the successor,
					// then there is a forward edge between the current and the successor
					if (state.Pre[current] < state.Pre[successor])
						state.SubGraph.AddEdge(
							new DepthFirstSpanningTreeEdge.Forward(
								current,
								successor));
					// If the successor vertex does not yet have a post number assigned to it
					// then we have a backward edge leading from the current vertex to the 
					// successor
					else if (!state.Post.ContainsKey(successor))
						state.SubGraph.AddEdge(
							new DepthFirstSpanningTreeEdge.Backward(
								current,
								successor));
					// Otherwise, we have a cross edge between the two vertices because they
					// cross paths multiple times
					else
						state.SubGraph.AddEdge(
							new DepthFirstSpanningTreeEdge.Cross(
								current,
								successor));
				}
			}

			// Set the vertex's post number
			state.Post[current] = state.PostNumber;
		}

		/// <summary>
		/// Only tree edges should be added to the graph
		/// </summary>
		public bool OnlyTreeEdges { get; set; }

		/// <summary>
		/// Internal state needed to build the spanning tree
		/// </summary>
		internal class State
		{
			/// <summary>
			/// Initializes the state instance
			/// </summary>
			/// <param name="parentGraph">The graph being operated upon</param>
			public State(Graph parentGraph)
			{
				Pre = new Dictionary<object, int>();
				Post = new Dictionary<object, int>();
				Visited = new DefaultSet();
				ParentGraph = parentGraph;
				SubGraph = ParentGraph.CreateGraph();

				preNumber = 1;
				postNumber = 1;
			}

			public Dictionary<object, int> Pre { get; private set; }
			public Dictionary<object, int> Post { get; private set; }
			public DefaultSet Visited { get; private set; }
			public Graph SubGraph { get; private set; }
			public Graph ParentGraph { get; private set; }
			public int PreNumber
			{
				get
				{
					preNumber++;

					return preNumber - 1;
				}
			}
			private int preNumber;
			public int PostNumber
			{
				get
				{
					postNumber++;

					return postNumber - 1;
				}
			}
			private int postNumber;
		}
	}

	public class DepthFirstSpanningTreeEdge
		: DirectedEdge
	{
		public class Tree
			: DepthFirstSpanningTreeEdge
		{
			public Tree(object source, object target)
				: base(source, target)
			{
			}
		}
		public class Forward
			: DepthFirstSpanningTreeEdge
		{
			public Forward(object source, object target)
				: base(source, target)
			{
			}
		}
		public class Backward
			: DepthFirstSpanningTreeEdge
		{
			public Backward(object source, object target)
				: base(source, target)
			{
			}
		}
		public class Cross
			: DepthFirstSpanningTreeEdge
		{
			public Cross(object source, object target)
				: base(source, target)
			{
			}
		}

		public DepthFirstSpanningTreeEdge(object source, object target)
			: base(source, target, null)
		{
		}
	}

}
