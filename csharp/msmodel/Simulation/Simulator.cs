// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace MSModel
{
    /// <summary>
    /// A delegate that is called to determine if the simulation has finished successfully.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public delegate bool IsSimulationCompleteDelegate(SimulationContext context);

    public delegate void FixedPointReachedDelegate(SimulationContext context);

    public delegate void OnTransitionDelegate(TransitionInformation transitionInfo);

    public class Simulator
    {
        public event OnTransitionDelegate OnTransition;

        public static bool Debug = false;

        static Simulator()
        {
            if (Environment.GetEnvironmentVariable("MSMDBG") == "1")
            {
                Debug = true;
            }
        }

        /// <summary>
        /// Creates a simulator with a desired end state of arbitrary code execution.
        /// </summary>
        /// <param name="simulations">The simulations to execute.</param>
        /// <param name="initialContext">The initial simulation context.</param>
        /// <returns>A simulator instance.</returns>
        public static Simulator CreateDesiredCodeExecutionSimulator(Simulation simulation, SimulationContext initialContext)
        {
            return CreateWithDesiredEndState(
                simulation,
                initialContext,
                (context) => 
                    { 
                        return context.IsAssumedTrue(AssumptionName.CanExecuteDesiredCode); 
                    }
                );
        }

        /// <summary>
        /// Creates a simulator with a desired end state of code execution.
        /// </summary>
        /// <param name="simulations">The simulations to execute.</param>
        /// <param name="initialContext">The initial simulation context.</param>
        /// <returns>A simulator instance.</returns>
        public static Simulator CreateCodeExecutionSimulator(Simulation simulation, SimulationContext initialContext)
        {
            return CreateWithDesiredEndState(
                simulation,
                initialContext, 
                (context) =>
                    {
                        return context.IsAssumedTrue(AssumptionName.CanExecuteCode);
                    }
                );
        }

        /// <summary>
        /// Creates a simulator with a provided desired end state.
        /// </summary>
        /// <param name="simulations">The simulations to execute.</param>
        /// <param name="initialContext">The initial simulation context.</param>
        /// <param name="endState">The desired end state of the simulation.</param>
        /// <returns>A simulator instance.</returns>
        public static Simulator CreateWithDesiredEndState(Simulation simulation, SimulationContext initialContext, IsSimulationCompleteDelegate checkForSuccessDelegate)
        {
            Simulator simulator = new Simulator(simulation, initialContext);
            simulator.IsSimulationComplete = checkForSuccessDelegate;
            return simulator;
        }

        /// <summary>
        /// Initializes the simulator with a provided simulation and initial context.
        /// </summary>
        /// <param name="simulation">The simulation to execute.</param>
        /// <param name="initialContext">The initial simulation context.</param>
        public Simulator(Simulation simulation, SimulationContext initialContext)
        {
            this.Simulation = simulation;
            this.InitialContext = initialContext;
        }

        /// <summary>
        /// Runs the simulation until completion.
        /// </summary>
        public void Run()
        {
            this.InitialContext.CurrentViolation = this.InitialContext.Target.Violation;

            this.WorkUnitQueue = new Queue<WorkUnit>();

            AddWorkUnits(this.InitialContext);

            while (this.WorkUnitQueue.Count > 0)
            {
                WorkUnit unit = this.WorkUnitQueue.Dequeue();

                ProcessWorkUnit(unit);
            }
        }

        public void RunOnce(OnTransitionDelegate onTransition = null)
        {
            this.InitialContext.CurrentViolation = this.InitialContext.Target.Violation;

            this.WorkUnitQueue = new Queue<WorkUnit>();

            this.IsSimulationComplete = (context) =>
            {
                return true;
            };

            SimulationContext newContext = this.InitialContext.CloneCast();

            WorkUnit unit = new WorkUnit()
            {
                Context = newContext
            };

            this.OnTransition += onTransition;

            ProcessWorkUnit(unit);
        }

        public void RestrictToTransitions(IEnumerable<Transition> restrictedTransitionList)
        {
            this.RestrictedTransitions = new List<Transition>(restrictedTransitionList);
        }

        public void RestrictToRootTransitions(IEnumerable<Transition> restrictToRootTransitionList)
        {
            this.RestrictedRootTransitions = new List<Transition>(restrictToRootTransitionList);    
        }

        private void AddWorkUnits(SimulationContext parentContext, Violation newViolation = null)
        {
            Violation currentViolation = parentContext.CurrentViolation;

            //
            // Establish the contexts that should be simulated against.
            //

            SimulationContext newContext;

            if (newViolation != null)
            {
                newContext = parentContext.CloneCast();
                newContext.CurrentViolation = newViolation;
                newContext.ParentContext = parentContext;

                //
                // Have we reached a fixed point?
                //

                //if (newContext.HasPreviousContext(newContext))
                //{
                //    return;
                //}
            }
            else
            {
                newContext = parentContext;
                newContext.ParentContext = null;
            }

            //
            // Inherit the initial assumptions for the current violation.
            //

            foreach (Assumption assumption in newContext.CurrentViolation.Assumptions)
            {
                newContext.Assume(assumption);
            }

            this.WorkUnitQueue.Enqueue(new WorkUnit()
            {
                Context = newContext
            });
        }

        private void ProcessWorkUnit(WorkUnit unit)
        {
            Violation currentViolation = unit.Context.CurrentViolation;
            IEnumerable<Transition> transitions = null;

            if (this.RestrictedRootTransitions != null)
            {
                if (unit.Context.RootChecked == false)
                {
                    TransitionInformation root = unit.Context.VisitedTransitions.Where(x => x.Transition.Primitive.IsIdentity == false).FirstOrDefault();

                    if (root != null)
                    {
                        //
                        // Do not process any further if this transition is not a valid root.
                        //

                        if (this.RestrictedRootTransitions.Contains(root.Transition) == false)
                        {
                            return;
                        }

                        unit.Context.RootChecked = true;
                    }
                }
            }

            if (this.RestrictedTransitions != null)
            {
                transitions = this.RestrictedTransitions;
            }
            else
            {
                transitions = this.Simulation.Transitions;
            }

            bool newPathAdded = false;

            foreach (Transition transition in transitions)
            {
                //
                // Skip transitions that we have already visited.
                //

                if (unit.Context.VisitedTransitions.Any(x => x.Transition.Ordinal == transition.Ordinal))
                {
                    continue;
                }

                TransitionInformation info;
                
                info = ProcessTransitionForWorkUnit(unit, transition);

                if (info != null)
                {
                    newPathAdded = true;
                }
            }

            if (newPathAdded == false)
            {
                if (this.OnFixedPointReached != null && unit.Context.VisitedTransitions.Count > 0)
                {
                    this.OnFixedPointReached(unit.Context);
                }
            }
        }

        private TransitionInformation ProcessTransitionForWorkUnit(WorkUnit unit, Transition transition)
        {
            SimulationContext activeContext = unit.Context.CloneCast();
            Violation newViolation = null;

            TransitionInformation transitionInfo = new TransitionInformation()
            {
                PreViolation = activeContext.CurrentViolation,
                Transition = transition
            };

            try
            {
                activeContext.AddVisitedTransition(transitionInfo);

                activeContext.CurrentTransition = transition;

                transition.Evaluate(activeContext);

                newViolation = (Violation)transition.OnSuccess(activeContext);

                //
                // Associate the transitive violation that is enabled by the current violation.
                //

                if (newViolation != null)
                {
                    activeContext.CurrentViolation.AddTransitiveViolation(newViolation);

                    transitionInfo.PostViolation = newViolation;
                }
                else
                {
                    transitionInfo.PostViolation = transitionInfo.PreViolation;
                }
            }
            catch (ConstraintNotSatisfied ex)
            {
                activeContext.FailureReason = String.Format("Constraint not satisfied on transition '{0}': {1}", transitionInfo.Transition, ex.Message);
                activeContext.Exploitability = 0;
            }

            if (activeContext.Exploitability == 0)
            {
                if (activeContext.Global.TrackImpossible)
                {
                    activeContext.Global.AddCompletedSimulationContext(activeContext);
                }

                return null;
            }

            if (this.OnTransition != null)
            {
                this.OnTransition(transitionInfo);
            }

            if (Debug)
            {
                Console.WriteLine("Visited transitions with hash {0},{1},{2} = {3}:",
                    unit.Context.AssumptionsEquivalenceClass,
                    unit.Context.TransitionsEquivalenceClass,
                    unit.Context.ViolationsEquivalenceClass,
                    unit.Context.InvariantEquivalenceClass
                );
                foreach (TransitionInformation i in activeContext.VisitedTransitions)
                {
                    Console.WriteLine("\t{0} --> {1} --> {2}",
                        i.PreViolation,
                        i.Transition.Label,
                        i.PostViolation);
                }
                Console.WriteLine("-------");
            }
            
            //
            // If the simulation has reached a completion state, log it as successful and return.
            //

            if (this.IsSimulationComplete != null && this.IsSimulationComplete(activeContext))
            {
                activeContext.Global.AddCompletedSimulationContext(activeContext);
                return transitionInfo;
            }

            //
            // Add new work units based on the state that we have transitioned to.
            //

            AddWorkUnits(activeContext, newViolation);

            return transitionInfo;
        }

        private object InitialState
        {
            get
            {
                return this.InitialContext.CurrentViolation;
            }
        }

        /// <summary>
        /// Checks to see if a simulation has reached a completion state.
        /// </summary>
        public IsSimulationCompleteDelegate IsSimulationComplete { get; set; }

        /// <summary>
        /// Called when a fixed point is reached for a context.
        /// </summary>
        public event FixedPointReachedDelegate OnFixedPointReached;

        /// <summary>
        /// The 
        /// </summary>
        public Simulation Simulation { get; private set; }

        /// <summary>
        /// The initial simulation context that simulation starts at.
        /// </summary>
        public SimulationContext InitialContext { get; private set; }

        /// <summary>
        /// The queue of simulation work units.
        /// </summary>
        private Queue<WorkUnit> WorkUnitQueue { get; set; }

        /// <summary>
        /// The set of transitions that simulation is restricted to.
        /// </summary>
        private List<Transition> RestrictedTransitions { get; set; }

        /// <summary>
        /// The set of transitions that the root of a simulation is restricted to.
        /// </summary>
        private List<Transition> RestrictedRootTransitions { get; set; }

        /// <summary>
        /// A work unit within the simulator.
        /// </summary>
        internal class WorkUnit
        {
            /// <summary>
            /// The simulation context for this work unit.
            /// </summary>
            public SimulationContext Context { get; set; }
        }
    }
}
