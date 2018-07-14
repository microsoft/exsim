# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
#!/usr/bin/env ruby

require 'rgl'

#
# Common directed graph.  RGL didn't provide enough graphing features, so we
# have to re-invent the wheel.
#
class DirectedGraph

	class Edge
		def initialize(u, v, a)
			@u = u
			@v = v
			@a = a
		end
		def eql?(other)
			self.u == other.u and self.v == other.v and self.a == other.a
		end
		def hash
			to_s.hash
		end
		def to_s
			"#{u}->#{v} [#{a}]"
		end
		attr_reader :u, :v, :a
	end

	def initialize(edge_class = Set)
		@edge_class = edge_class
		@pedges = {}
		@sedges = {}
		@verts  = Set.new
	end

	def add_vertex(v)
		@verts.add(v)
	end

	def add_edge(u, v, annotation = nil)
		add_vertex(u)
		add_vertex(v)

		edge = Edge.new(u, v, annotation)

		(@sedges[u] ||= @edge_class.new) << edge
		(@pedges[v] ||= @edge_class.new) << edge
	end
	def each_vertex(&b)
		vertices.each(&b)
	end

	def each_edge(&b)
		@sedges.each { |u, usucc|
			usucc.each { |einfo|
				b.call(u, einfo.v)
			}
		}
	end

	def each_edge_annotations(&b)
		@sedges.each { |u, usucc|
			usucc.each { |einfo|
				b.call(u, einfo.v, einfo.a)
			}
		}
	end

	def vertices
		@verts
	end

	def successors(u)
		(@sedges[u] || []).map { |edge|
			edge.v
		}
	end

	def successors_wa(u)
		(@sedges[u] || []).map { |edge|
			[ edge.v, edge.a ]
		}
	end


	def predecesors(v)
		(@pedges[v] || []).map { |edge|
			edge.u
		}
	end

	def predecesors_wa(v)
		(@pedges[v] || []).map { |edge|
			[ edge.u, edge.a ]
		}
	end

	def to_dot_graph(params = {})
		fd = params['fd']

		fd << "digraph { " if fd

		graph = DOT::DOTDigraph.new(params)

		each_vertex { |v|
			name  = v.to_s
			label = to_dot_vlabel(v)
			color = to_dot_vcolor(v)

			name  = name.gsub('\\', '\\\\\\\\')
			label = label.gsub('\\', '\\\\\\\\') if label

			node = DOT::DOTNode.new(
				'name'     => '"' + name + '"',
				'fontsize' => 12,
				'label'    => label,
				'color'    => color)

			if fd 
				fd.puts node.to_s
			else
				graph << node
			end
		}

		each_edge_annotations { |u, v, a|
			label = to_dot_elabel(u, v, a)
			color = to_dot_ecolor(u, v, a)

			from  = u.to_s.gsub('\\', '\\\\\\\\')
			to    = v.to_s.gsub('\\', '\\\\\\\\')
			label = label.gsub('\\', '\\\\\\\\') if label

			edge = DOT::DOTDirectedEdge.new(
				'from'     => '"' + from + '"',
				'to'       => '"' + to + '"',
				'fontsize' => 12,
				'label'    => label,
				'color'    => color)

			if fd
				fd.puts edge.to_s
			else
				graph << edge
			end
		}
		
		fd << "} " if fd

		graph
	end

	# Saves the graph to a file
	def to_dot_graph_file(path)
		File.open(path, "w") { |fd|
			to_dot_graph('fd' => fd)
		}
	end

private

	def to_dot_vlabel(v)
		v.to_s
	end
	
	def to_dot_vcolor(v)
		nil
	end

	def to_dot_elabel(u, v, a)
		nil
	end

	def to_dot_ecolor(u, v, a)
		nil
	end

end
