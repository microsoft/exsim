// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Text;

namespace UR.Graphing
{
	/// <summary>
	/// Abstract class that is used to represent a graph visitor
	/// </summary>
	public abstract class GraphVisitor<T>
	{
		/// <summary>
		/// Visits a vertex
		/// </summary>
		/// <param name="vertex">The vertex being visited</param>
		/// <returns>True if navigation of the graph should continue</returns>
		public abstract bool Visit(T vertex);

		/// <summary>
		/// Called after the vertex has been visited
		/// </summary>
		/// <param name="vertex">The vertex being visited</param>
		public virtual void PostVisit(T vertex)
		{
        }

        /// <summary>
        /// The navigator being used for this visitor
        /// </summary>
        public virtual GraphNavigator Navigator
        {
            get { return navigator; }
            set { navigator = value; }
        }
        private GraphNavigator navigator;
	}

	/// <summary>
	/// Abstract class that is used to represent a graph visitor
	/// </summary>
	public abstract class GraphVisitor : GraphVisitor<object>
	{
	}

    public delegate bool OnGraphVisit(Graph graph, object vertex);

    public class DelegateGraphVisitor : GraphVisitor
    {
        public DelegateGraphVisitor(OnGraphVisit visit = null, OnGraphVisit postVisit = null)
        {
            this.CbVisit = visit;
            this.CbPostVisit = postVisit;
        }

        public override bool Visit(object vertex)
        {
            if (this.CbVisit != null)
            {
                return this.CbVisit.Invoke(this.Navigator.Graph, vertex);
            }

            return true;
        }

        public override void PostVisit(object vertex)
        {
            if (this.CbPostVisit != null)
            {
                this.CbPostVisit.Invoke(this.Navigator.Graph, vertex);
            }
        }

        public OnGraphVisit CbVisit { get; private set; }
        public OnGraphVisit CbPostVisit { get; private set; }
    }

}
