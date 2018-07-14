// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.IO;

namespace UR.Graphing
{
	public class GraphSerializer
	{
		public static void ToGraphML(Graph graph, string path, CustomStyle custom)
		{
			File.Delete(path);

			using (FileStream fstream = File.OpenWrite(path))
			{
				GraphMLFileFormat ml = new GraphMLFileFormat(graph, new StreamWriter(fstream), custom);

				ml.Generate();

				fstream.Close();
			}
		}
	}

	public abstract class CustomStyle
	{
		public abstract void WriteEdgeAttributes(GraphFileFormat format, Edge e);
		public abstract void WriteVertexAttributes(GraphFileFormat format, object v);
	}

	public abstract class GraphFileFormat
	{
		public GraphFileFormat(Graph graph, StreamWriter writer, CustomStyle custom)
		{
			this.graph = graph;
			this.writer = writer;
			this.custom = custom;
		}

		public virtual void Generate()
		{
			BeginGraph();
			BeginVertices();
			foreach (object v in graph.Vertices)
			{
				if (v is Graph)
					GenerateSubGraph(v as Graph);
				else
					SerializeVertex(v);
			}
			EndVertices();
			BeginEdges();
			foreach (Edge e in graph.Edges)
				SerializeEdge(e);
			EndEdges();
			EndGraph();
			writer.Flush();
		}

		public virtual void GenerateSubGraph(Graph sub)
		{
			BeginSubGraph(sub);
			foreach (object v in sub.Vertices)
			{
				if (v is Graph)
					GenerateSubGraph(v as Graph);
				else
					SerializeVertex(v);
			}
			EndSubGraph(sub);
		}

		public virtual void BeginGraph()
		{
		}

		public virtual void BeginSubGraph(Graph sub)
		{
		}

		public virtual void BeginVertices()
		{
		}

		public virtual void SerializeVertex(object vertex)
		{
		}

		public virtual void EndVertices()
		{
		}

		public virtual void BeginEdges()
		{
		}

		public virtual void SerializeEdge(Edge edge)
		{
		}

		public virtual void EndEdges()
		{
		}

		public virtual void EndGraph()
		{
		}

		public virtual void EndSubGraph(Graph sub)
		{
		}

		public Graph Graph
		{
			get { return graph; }
		}
		private Graph graph;

		public StreamWriter Writer
		{
			get { return writer; }
		}
		private StreamWriter writer;

		protected CustomStyle custom;
	}

	public class GraphMLFileFormat : GraphFileFormat
	{
		public abstract class GraphMLCustomStyle : CustomStyle
		{
			public override void WriteEdgeAttributes(GraphFileFormat format, Edge e)
			{
				WriteGraphMLEdge(format, e);
			}

			public override void WriteVertexAttributes(GraphFileFormat format, object v)
			{
				WriteGraphMLVertex(format, v);
			}

			public abstract void WriteGraphMLEdge(GraphFileFormat format, Edge e);
			public abstract void WriteGraphMLVertex(GraphFileFormat format, object v);
		}

		public GraphMLFileFormat(Graph graph, StreamWriter writer, CustomStyle custom)
			: base(graph, writer, custom)
		{
		}

		public override void BeginGraph()
		{
			Writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
			Writer.WriteLine("<graphml xmlns=\"http://graphml.graphdrawing.org/xmlns/graphml\"");
			Writer.WriteLine(" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" ");
			Writer.WriteLine(" xsi:schemaLocation=\"http://graphml.graphdrawing.org/xmlns/graphml ");
			Writer.WriteLine("  http://www.yworks.com/xml/schema/graphml/1.0/ygraphml.xsd\" ");
			Writer.WriteLine("  xmlns:y=\"http://www.yworks.com/xml/graphml\">");
			Writer.WriteLine("  <key id=\"d0\" for=\"node\" yfiles.type=\"nodegraphics\"/>");
			Writer.WriteLine("  <key id=\"d1\" for=\"edge\" yfiles.type=\"edgegraphics\"/>");
			Writer.WriteLine("  <graph id=\"G\" edgedefault=\"{0}\">",
				(Graph is DirectedGraph) ? "directed" : "undirected");
		}

		public override void BeginSubGraph(Graph sub)
		{
			string label = sub.Label != null ? sub.Label : "Graph" + sub.ToString();
			Writer.WriteLine("<node id=\"gn{1}\"><data key=\"d0\"><y:ShapeNode><y:ShapeNode><y:Geometry width=\"{1}\"/><y:NodeLabel modelPosition=\"tl\" fontFamily=\"Lucida Console\" fontSize=\"10\">{1}</y:NodeLabel></y:ShapeNode>{0}</y:NodeLabel></y:ShapeNode></data><graph id=\"{1}\" edgedefault=\"{2}\">",
				label,
				label.Length * 8,
				sub.GetHashCode(),
				sub is DirectedGraph ? "directed" : "undirected");
		}

		public override void BeginVertices()
		{
		}

		public override void SerializeVertex(object vertex)
		{
			Writer.WriteLine("  <node id=\"n{0}\">", vertex.GetHashCode());
			if (custom != null)
				custom.WriteVertexAttributes(this, vertex);
			Writer.WriteLine("  </node>");
		}

		public override void EndVertices()
		{
		}

		public override void BeginEdges()
		{
		}

		public override void SerializeEdge(Edge edge)
		{
			Writer.WriteLine("  <edge id=\"e{0}::{1}\" source=\"n{0}\" target=\"n{1}\">", 
				edge.Source.GetHashCode(),
				edge.Target.GetHashCode());
			if (custom != null)
				custom.WriteEdgeAttributes(this, edge);
			Writer.WriteLine("  </edge>");
		}

		public override void EndEdges()
		{
		}

		public override void EndGraph()
		{
			Writer.WriteLine("  </graph>");
			Writer.WriteLine("</graphml>");
		}

		public override void EndSubGraph(Graph sub)
		{
			Writer.WriteLine("</graph></node>");
		}
	}


	public class DefaultGraphMLCustomStyle : GraphMLFileFormat.GraphMLCustomStyle
	{
		public override void WriteGraphMLEdge(GraphFileFormat format, Edge e)
		{
			string str = "";

			if (e.Extension != null)
				str = e.Extension.ToString().Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

			format.Writer.WriteLine("<data key=\"d1\"><y:PolyLineEdge><y:BendStyle smoothed=\"true\"/><y:LineStyle type=\"line\" color=\"#000000\"/><y:EdgeLabel modelName=\"six_pos\" fontFamily=\"Lucida Console\" fontSize=\"10\">{0}</y:EdgeLabel><y:Arrows target=\"standard\"/></y:PolyLineEdge></data>",
				str);
		}

		public override void WriteGraphMLVertex(GraphFileFormat format, object v)
		{
			string str = "";

			if (v != null && v.ToString() != null)
				str = v.ToString().Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

			format.Writer.WriteLine("<data key=\"d0\"><y:ShapeNode><y:Geometry width=\"{1}\"/><y:NodeLabel modelPosition=\"tl\" fontFamily=\"Lucida Console\" fontSize=\"10\">{0}</y:NodeLabel></y:ShapeNode></data>",
				str,
				str.Length * 8);
		}
	}
}
