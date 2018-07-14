# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
require 'sim'
require 'graph'

module ExSim

class Graph < DirectedGraph
	def to_dot_elabel(u,v,a)
		a
	end
end

s  = Simulator.new(SimulationContext.new)
sm = s.build

g = Graph.new

sm.states.values.each do |state|
	state.transitions.each_pair do |event, transition|
		g.add_edge(transition.origin_id, transition.destination_id, event.to_s)	
	end
end

g.to_dot_graph_file('sm.dot')

end
