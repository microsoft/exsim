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
	/// Generates a sub-graph containing all of the strongly connected components using Tarjan's algorithm
	/// </summary>
	/// <remarks>
	/// </remarks>
	public class SCC
		: IGraphGenerator
	{
		/// <summary>
		/// Generates a sub-graph containing all of the strongly connected components
		/// </summary>
		/// <param name="graph">The graph to analyze</param>
		/// <returns>A sub-graph containing the strongly connected components</returns>
		public Graph GenerateGraph(Graph graph)
		{
			State state = new State(graph);

			foreach (object v in graph.Vertices)
			{
				if (!state.DepthFirstIndex.ContainsKey(v))
					FindSccs(state,	v);
			}

			return state.SubGraph;
		}

		/// <summary>
		/// Generates a sub-graph containing all of the strongly connected components
		/// </summary>
		/// <param name="state"></param>
		/// <param name="current"></param>
		private void FindSccs(
			State state,
			object current)
		{
			int currentIndex = state.CurrentIndex;

			// Assign this vertex's depth first index
			state.DepthFirstIndex[current] = currentIndex;
			state.DepthFirstLowLink[current] = currentIndex;

			// Push the vertex onto the node stack
			state.NodeStack.Push(current);

			// Walk the successors of this vertex
			foreach (Edge successorEdge in state.ParentGraph.Successors(current))
			{
				object successor = successorEdge.Target;

				// If we haven't visited this vertex then do so now
				if (!state.DepthFirstIndex.ContainsKey(successor))
				{
					FindSccs(state, successor);

					// Set the lowlink of the current vertex to the minimum of
					// the lowlink between itself and the successor
					state.DepthFirstLowLink[current] =
						Math.Min(
							state.DepthFirstLowLink[current],
							state.DepthFirstLowLink[successor]);
				}
				// If the successor is in the node stack
				else if (state.NodeStack.Contains(successor))
				{
					state.DepthFirstLowLink[current] =
						Math.Min(
							state.DepthFirstLowLink[current],
							state.DepthFirstIndex[successor]);
				}
			}

			// If this vertex is the root of the SCC, then create a sub graph containing
			// all of the elements
			if (state.DepthFirstIndex[current] == state.DepthFirstLowLink[current])
			{
				Dictionary<int, object> sccGroup = new Dictionary<int, object>();
				
				while (true)
				{
					object scc = state.NodeStack.Pop();

					sccGroup[state.DepthFirstIndex[scc]] = scc;

					if (scc == current)
						break;
				}

				// Ignore SCC groups with only one element
				if (sccGroup.Count > 1)
				{
					// Create edges for each item
					foreach (KeyValuePair<int, object> kv in sccGroup)
					{
						// If there's no entry for the next depth first number, then we
						// need to create a wrapping edge
						if (!sccGroup.ContainsKey(kv.Key + 1))
							state.SubGraph.AddEdge(
								kv.Value,
								sccGroup[state.DepthFirstLowLink[kv.Value]]);
						else
							state.SubGraph.AddEdge(
								kv.Value,
								sccGroup[kv.Key + 1]);
					}
				}
			}
		}

		/// <summary>
		/// Internal state used by the SCC generator
		/// </summary>
		internal class State
		{
			public State(Graph graph)
			{
				this.DepthFirstIndex = new Dictionary<object, int>();
				this.DepthFirstLowLink = new Dictionary<object, int>();
				this.NodeStack = new Stack<object>();
				this.ParentGraph = graph;
				this.SubGraph = graph.CreateGraph();
			}

			public Dictionary<object, int> DepthFirstIndex { get; private set; }
			public Dictionary<object, int> DepthFirstLowLink { get; private set; }
			public Stack<object> NodeStack { get; private set; }
			public Graph ParentGraph { get; private set; }
			public Graph SubGraph { get; private set; }
			public int CurrentIndex
			{
				get
				{
					currentIndex++;

					return currentIndex - 1;
				}
			}
			private int currentIndex = 1;
		}
	}
}
