// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Linq.Expressions;

using UR.Graphing;

using MSModel.Profiles;

namespace MSModel
{
    public delegate Transition TransitionFactoryDelegate(Simulation simulation);
    
    /// <summary>
    /// A simulation contains a graph of techniques for transitioning between violations.
    /// </summary>
    public class Simulation
    {
        /// <summary>
        /// Initializes the simulation using a provided memory safety model.
        /// </summary>
        /// <param name="model">The memory safety model.</param>
        public Simulation(MemorySafetyModel model)
        {
            this.Model = model;
            this.CompleteGraph = new DirectedGraph();
            this.Transitions = new List<Transition>();
        }

        public Transition AddRootTransition(
            ExploitationTechnique technique,
            ExploitationPrimitive primitive
            )
        {
            return AddTransition(technique, primitive, true);
        }

        public Transition AddTransition(
            ExploitationTechnique technique, 
            ExploitationPrimitive primitive, 
            bool isRootTransition = false
            )
        {
            Transition transition;

            transition = new Transition(primitive, technique, isRootTransition)
            {
                Ordinal = ++this.TransitionOrdinalPool
            };

            this.Transitions.Add(transition);

            if (this.SkipUpdateGraph == false)
            {
                UpdateGraphs();
            }

            return transition;
        }

        private bool SkipUpdateGraph { get; set; }

        public void BeginAddTransition()
        {
            this.SkipUpdateGraph = true;
        }

        public void EndAddTransition()
        {
            this.SkipUpdateGraph = false;

            UpdateGraphs();
        }

        public List<TransitionChain> GetTransitionChains(MemoryAccessMethod method)
        {
            if (this.TransitionChains.ContainsKey(method))
            {
                return this.TransitionChains[method];
            }
            else
            {
                return new List<TransitionChain>();
            }
        }

        public Graph CompleteGraph { get; private set; }

        private void UpdateGraphs()
        {
            Graph graph = new DirectedGraph();

            this.TransitionChains = new Dictionary<MemoryAccessMethod, List<TransitionChain>>();

            foreach (Transition transition in this.RootTransitions)
            {
                //
                // Skip identity primitives for now.
                //

                if (transition.Primitive.IsIdentity)
                {
                    continue;
                }

                //
                // Use an open-ended violation of the method that the transition is from to ensure 
                // the constraints will match.
                //
                // TODO: also leverage expected parameter control?
                //

                Violation initialViolation = new Violation(
                    transition.Primitive.FromMethod.Value
                    );

                GlobalSimulationContext globalContext = new GlobalSimulationContext(
                    new Target()
                    {
                        Violation = initialViolation
                    });

                //
                // Assume that content initialization is possible.  Most transitions will require
                // that content has been initialized, and it is expected that a content initialization
                // primitive will have been used to accomplish this.  However, for the purpose of classification,
                // we do not want to select or imply any one initialization primitive.
                //

                globalContext.AssumeContentInitializationPossible = true;

                SimulationContext initialContext = new SimulationContext(globalContext);

                Simulator simulator = new Simulator(this, initialContext);

                //
                // Limit simulation to include only those primitives that are part of the same technique.
                //

                simulator.RestrictToTransitions(
                    this.Transitions.Where(x => x.Technique == transition.Technique)
                    );

                //
                // Add edges for contexts that reach a fixed point.
                //

                simulator.OnFixedPointReached += (context) =>
                    {
                        TransitionInformation contextRoot = context.VisitedTransitions.Where(x => x.Transition.Primitive.IsIdentity == false).FirstOrDefault();

                        if (contextRoot == null || contextRoot.Transition != transition)
                        {
                            return;
                        }

                        TransitionChain chain = new TransitionChain();

                        foreach (TransitionInformation ti in context.VisitedTransitions)
                        {
                            graph.AddEdge(
                                new DirectedEdge(
                                    ti.PreViolation,
                                    ti.PostViolation,
                                    ti.Transition.Primitive
                                    )
                                );

                            chain.Transitions.Add(ti);
                        }

                        if (!this.TransitionChains.ContainsKey(initialViolation.Method))
                        {
                            this.TransitionChains[initialViolation.Method] = new List<TransitionChain>();
                        }

                        this.TransitionChains[initialViolation.Method].Add(chain);
                    };

                //
                // Execute the simulation.
                //

                simulator.Run();
            }

            this.CompleteGraph = graph;
        }

        public void SaveAsDOT(string path)
        {
            this.CompleteGraph.ToGraphML(path);
        }

        public string Description { get; set; }
        public MemorySafetyModel Model { get; private set; }
        public Dictionary<MemoryAccessMethod, List<TransitionChain>> TransitionChains { get; private set; }
        public List<Transition> Transitions { get; set; }
        public IEnumerable<Transition> RootTransitions
        {
            get
            {
                return this.Transitions.Where(x => x.IsRootTransition);
            }
        }
        private int TransitionOrdinalPool { get; set; }

        public static Simulation GetAllTechniquesSimulation(MemorySafetyModel model)
        {
            List<ExploitationTechnique> techniques = new List<ExploitationTechnique>();

            var parameters = new MemoryAccessParameter[]
            {
                MemoryAccessParameter.Base,
                MemoryAccessParameter.Content,
                MemoryAccessParameter.Displacement,
                MemoryAccessParameter.Extent
            };

            var regions = Enum.GetValues(typeof(MemoryRegionType));

            //
            // Multi-region generic techniques.
            //

            foreach (MemoryRegionType region in regions)
            {
                //
                // Skip regions that are not relevant.
                //

                if (!MemoryAddress.WritableRegionTypes.Contains(region))
                {
                    continue;
                }

                //
                // r->r, r->w, and w->r techniques.
                //

                foreach (MemoryAccessParameter parameter in parameters)
                {
                    if (parameter != MemoryAccessParameter.Content)
                    {
                        techniques.Add(
                            new SimpleTechnique(
                                new ReadToReadPrimitive(parameter, parameter.GetMemoryAddress(MemoryAccessMethod.Read, region))
                                )
                            );
                    }

                    techniques.Add(
                        new SimpleTechnique(
                            new ReadToWritePrimitive(parameter, parameter.GetMemoryAddress(MemoryAccessMethod.Write, region))
                            )
                        );

                    techniques.Add(
                        new SimpleTechnique(
                            new WriteToReadPrimitive(parameter, parameter.GetMemoryAddress(MemoryAccessMethod.Read, region))
                            )
                        );
                }
            }

            techniques.Add(new CorruptFunctionPointer());
            techniques.Add(new CorruptCppObjectVirtualTablePointer());

            //
            // Stack-specific techniques.
            //

            techniques.Add(new CorruptStackReturnAddress());
            techniques.Add(new CorruptStackFramePointer());
            techniques.Add(new CorruptStackStructuredExceptionHandler());

            //
            // Heap-specific techniques.
            //


            //
            // Content initialization techniques.
            //

            techniques.Add(new HeapSpray());
            techniques.Add(new StackLocalVariableInitialization());
            techniques.Add(new LoadNonASLRImage());
            techniques.Add(new LoadNonASLRNonSafeSEHImage());

            //
            // Code execution techniques.
            //

            techniques.Add(new ExecuteControlledDataAsCode());
            techniques.Add(new ExecuteJITCode());
            techniques.Add(new ExecuteROPPayload.StageViaVirtualAllocOrProtect());
            techniques.Add(new ExecuteROPPayload.StageViaExecutableHeap());
            techniques.Add(new ExecuteROPPayload.StageViaDisableNX());
            techniques.Add(new ExecuteROPPayload.PureROP());

            techniques.Add(
                new SimpleTechnique(
                    new WriteToExecutePrimitive()
                    )
                );

            //
            // Add the transitions to the simulation.
            //

            Simulation simulation = new Simulation(model);

            simulation.BeginAddTransition();

            foreach (ExploitationTechnique technique in techniques)
            {
                technique.AddTransitionsToSimulation(simulation);
            }

            simulation.EndAddTransition();

            return simulation;
        }
    }
}
