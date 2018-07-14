// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Linq.Expressions;

namespace MSModel
{
    /// <summary>
    /// The modes under which simulation can occur.
    /// </summary>
    public enum SimulationMode
    {
        /// <summary>
        /// In cases where a condition is unknown, favor the attacker.
        /// </summary>
        FavorAttack,

        /// <summary>
        /// In cases where a condition is unknown, favor the defense.
        /// </summary>
        FavorDefense,

        /// <summary>
        /// Only include public techniques during the simulation.
        /// </summary>
        PublicOnly,

        /// <summary>
        /// Normal simulation, nothing to see here.
        /// </summary>
        Normal
    }

    public class GlobalSimulationContext
    {
        public GlobalSimulationContext(Target target)
        {
            this.Target = target;
            this.TrackEquivalentOnly = false;
            this.TrackMinimalOnly = false;
            this.SimulationGroups = new Dictionary<string, SimulationContextCollection>();

            this.AllowImpossible = false;
            this.TrackImpossible = true; // for now
            this.Debug = false;
            this.Modes = new SimulationMode[] { SimulationMode.FavorAttack };
            this.AssumptionIDPool = 0;

            this.IgnoreEquivalentExecutes = false;
            this.IgnoreEquivalentReads = true;
            this.IgnoreEquivalentWrites = true;

            this.AssumeContentInitializationPossible = false;

            target.Recalibrate();
        }

        internal void AddCompletedSimulationContext(SimulationContext simulation)
        {
            if (this.TrackMinimalOnly && simulation.IsMinimal == false)
            {
                return;
            }

            string eqid = simulation.EquivalenceID;
            bool add = false;

            if (!this.SimulationGroups.ContainsKey(eqid))
            {
                this.SimulationGroups[eqid] = new SimulationContextCollection();
                add = true;
            }

            this.SimulationGroups[eqid].EquivalentMemberCount++;

            if (this.TrackEquivalentOnly == false || add == true)
            {
                this.SimulationGroups[eqid].AddSimulation(simulation);
            }
        }

        public IEnumerable<SimulationContext> CompletedSimulationContexts
        {
            get
            {
                return this.SimulationGroups.Values.SelectMany(x => x.SimulationContexts);
            }
        }

        public long SimulationCount
        {
            get
            {
                return this.SimulationGroups.Values.Sum(x => x.EquivalentMemberCount);
            }
        }

        public Target Target { get; private set; }

        public bool TrackEquivalentOnly { get; set; }
        public bool TrackMinimalOnly { get; set; }

        public bool AllowImpossible { get; set; }
        public bool TrackImpossible { get; set; }
        public bool Debug { get; set; }
        public SimulationMode[] Modes { get; set; }
        internal long AssumptionIDPool { get; set; }

        public bool IgnoreEquivalentReads { get; set; }
        public bool IgnoreEquivalentWrites { get; set; }
        public bool IgnoreEquivalentExecutes { get; set; }

        public bool AssumeContentInitializationPossible { get; set; }

        private Dictionary<string, SimulationContextCollection> SimulationGroups { get; set; }

        public string Description
        {
            get
            {
                StringWriter writer = new StringWriter();

                writer.WriteLine("{0} simulations recorded", this.SimulationCount);
                writer.WriteLine();
                writer.WriteLine("CHARACTERISTICS");
                writer.WriteLine("---------------");
                writer.WriteLine();
                if (this.CompletedSimulationContexts.Count() > 0)
                {
                    writer.WriteLine("Exploitability(avg) = {0}", this.CompletedSimulationContexts.Average(x => x.Exploitability));
                    writer.WriteLine("Exploitability(max) = {0}", this.CompletedSimulationContexts.Max(x => x.Exploitability));
                    writer.WriteLine("Exploitability(min) = {0}", this.CompletedSimulationContexts.Min(x => x.Exploitability));
                    writer.WriteLine();
                }

                writer.WriteLine();
                writer.WriteLine("CONFIGURATION");
                writer.WriteLine("-------------");
                writer.WriteLine();
                writer.WriteLine(this.Target.Description);

                if (this.Target.InitialAssumptions.Count > 0)
                {
                    writer.WriteLine();
                    writer.WriteLine("INITIAL ASSUMPTIONS");
                    writer.WriteLine("-------------------");
                    writer.WriteLine();

                    foreach (Assumption assumption in this.Target.InitialAssumptions)
                    {
                        writer.WriteLine("{0} = {1}",
                            assumption.ToString().PadRight(40),
                            assumption.Probability);
                    }
                }

                writer.WriteLine();
                writer.WriteLine("SIMULATIONS");
                writer.WriteLine("-----------");
                writer.WriteLine();

                foreach (SimulationContext simulationContext in this.CompletedSimulationContexts.OrderByDescending(x => x.Exploitability))
                {
                    writer.WriteLine(simulationContext.Description);
                }

                return writer.ToString();
            }
        }
    }

    public delegate double EvaluateAssumptionDelegate(SimulationContext context, Assumption assumption);

    public class SimulationContext : ICloneable
    {
        public SimulationContext(GlobalSimulationContext global)
        {
            this.Global = global;

            this.Exploitability = 1.0;

            this.VisitedTransitions = new List<TransitionInformation>();
            this.Assumptions = new Dictionary<Assumption, Assumption>();

            foreach (Assumption assumption in global.Target.InitialAssumptions)
            {
                assumption.ID = ++global.AssumptionIDPool;

                this.Assumptions[assumption] = assumption;
            }
        }

        public object Clone()
        {
            SimulationContext clone = (SimulationContext)this.MemberwiseClone();

            clone.VisitedTransitions = new List<TransitionInformation>(this.VisitedTransitions);
            clone.Assumptions = new Dictionary<Assumption, Assumption>(this.Assumptions);

            return clone;
        }

        public SimulationContext CloneCast()
        {
            return (SimulationContext)this.Clone();
        }

        public Target Target
        {
            get { return this.Global.Target; }
        }

        public double Exploitability { get; set; }

        public bool Failed
        {
            get { return this.Exploitability == 0; }
        }

        public string FailureReason { get; set; }

        public string Description
        {
            get
            {
                StringWriter writer = new StringWriter();

                writer.WriteLine("===============");
                writer.WriteLine();

                writer.WriteLine("Simulation exploitability: {0}", this.Exploitability);

                if (this.Failed)
                {
                    writer.WriteLine("Simulation failed        : {0}", this.FailureReason);
                }

                writer.WriteLine();
                writer.WriteLine("Transitions");
                writer.WriteLine();

                foreach (TransitionInformation transitionInfo in this.VisitedTransitions)
                {
                    writer.WriteLine(" {0} -> {1} -> {2}", 
                        transitionInfo.PreViolation.ToString().PadRight(50),
                        transitionInfo.Transition.ToString().PadRight(35),
                        transitionInfo.PostViolation
                        );
                }

                writer.WriteLine();
                writer.WriteLine("Assumptions");
                writer.WriteLine();

                foreach (Assumption assumption in this.Assumptions.Values.OrderBy(x => x.ID))
                {
                    writer.WriteLine(" [{0}] {1} = {2}", 
                        (assumption.Transition != null ? assumption.Transition.ToString() : "invariant").ToString().PadRight(50),
                        assumption.Probability,
                        assumption.ToString());
                }

                return writer.ToString();
            }
        }

        public bool RootChecked { get; set; }

        internal List<TransitionInformation> VisitedTransitions { get; set; }
        public Dictionary<Assumption, Assumption> Assumptions { get; protected set; }

        public SimulationContext ParentContext { get; set; }
        public Violation CurrentViolation 
        {
            get { return this.currentViolation; }
            set 
            {
                this.currentViolation = value;
                ResetViolationsEquivalenceClass();
            }
        }
        private Violation currentViolation;
        public Transition CurrentTransition { get; set; }

        public GlobalSimulationContext Global { get; private set; }

        public bool IsMinimal
        {
            get 
            { 
                return true; 
            }
        }

        public void AddVisitedTransition(TransitionInformation transition)
        {
            this.VisitedTransitions.Add(transition);
            ResetTransitionsClass();
        }

        public bool HasPreviousContext(SimulationContext context)
        {
            SimulationContext current = this.ParentContext;

            while (current != null)
            {
                /*
                Console.WriteLine("{0} == {1} / {2} == {3}",
                    current.InvariantEquivalenceClass,
                    this.InvariantEquivalenceClass,
                    current.CurrentViolation,
                    this.CurrentViolation);
                 */

                if (current.InvariantEquivalenceClass == this.InvariantEquivalenceClass)
                {
                    return true;
                }

                current = current.ParentContext;
            }

            return false;
        }

        public string AssumptionsEquivalenceClass
        {
            get
            {
                if (this.assumptionEquivalenceClass == null)
                {
                    var assumptions = this.Assumptions.Values.SelectMany(x => x.NameString).OrderBy(x => x).Distinct();

                    this.assumptionEquivalenceClass = Profile.ComputeSHA1(assumptions.ToArray());
                }

                return this.assumptionEquivalenceClass;
            }
        }
        private string assumptionEquivalenceClass;

        private void ResetAssumptionEquivalenceClass()
        {
            this.assumptionEquivalenceClass = null;
            this.invariantEquivalenceClass = null;
        }

        public string TransitionsEquivalenceClass
        {
            get
            {
                if (this.transitionsEquivalenceClass == null)
                {
                    var transitions = this.VisitedTransitions.SelectMany(x => x.Transition.Ordinal.ToString()).Distinct();

                    this.transitionsEquivalenceClass = Profile.ComputeSHA1(transitions.ToArray());
                }

                return this.transitionsEquivalenceClass;
            }
        }
        private string transitionsEquivalenceClass;

        private void ResetTransitionsClass()
        {
            this.transitionsEquivalenceClass = null;
        }

        public string ViolationsEquivalenceClass
        {
            get
            {
                if (this.violationsEquivalenceClass == null)
                {
                    List<string> equivaleneClasses = new List<string>();

                    Violation current = this.CurrentViolation;

                    while (current != null)
                    {
                        equivaleneClasses.Insert(0, current.EquivalenceClass);
                        current = current.PreviousViolationObject as Violation;
                    }

                    this.violationsEquivalenceClass = Profile.ComputeSHA1(equivaleneClasses.Distinct().ToArray());
                }

                return this.violationsEquivalenceClass;
            }
        }
        private string violationsEquivalenceClass;

        private void ResetViolationsEquivalenceClass()
        {
            this.violationsEquivalenceClass = null;
            this.invariantEquivalenceClass = null;
        }

        public string InvariantEquivalenceClass
        {
            get
            {
                if (this.invariantEquivalenceClass == null)
                {
                    this.invariantEquivalenceClass = Profile.ComputeSHA1(this.AssumptionsEquivalenceClass, this.ViolationsEquivalenceClass);
                }

                return this.invariantEquivalenceClass;
            }
        }
        private string invariantEquivalenceClass;

        public string EquivalenceID
        {
            get 
            {
                return String.Format("{0}-{1}", this.VisitedTransitions.GetHashCode(), this.Exploitability);
            }
        }

        public bool ModeIsFavorAttack
        {
            get { return this.Global.Modes.Contains(SimulationMode.FavorAttack); }
        }

        public bool ModeIsFavorDefense
        {
            get { return this.Global.Modes.Contains(SimulationMode.FavorDefense); }
        }

        public bool ModeIsPublicOnly
        {
            get { return this.Global.Modes.Contains(SimulationMode.PublicOnly); }
        }

        public bool ModeIsNormal
        {
            get { return this.Global.Modes.Contains(SimulationMode.Normal); }
        }

        public bool AttackerFavorsEqual(object x, object y)
        {
            bool? result = (x == null || y == null) ? null : new bool?(x.Equals(y));

            bool value;

            if (result == null)
            {
                if (this.ModeIsFavorAttack)
                {
                    value = true;
                }
                else if (this.ModeIsFavorDefense)
                {
                    value = false;
                }
                else
                {
                    throw new InvalidOperationException("An invalid simulation mode was detected.  Attack or defense favor must be selected.");
                }
            }
            else
            {
                value = result.Value;
            }

            return value;
        }

        public bool AttackerFavorsTrue()
        {
            if (this.ModeIsFavorAttack)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool AttackerFavorsFalse()
        {
            if (this.ModeIsFavorAttack)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool AssumeIsTrue(Assumption assumption, EvaluateAssumptionDelegate evaluate = null)
        {
            if (evaluate == null)
            {
                evaluate = (context, a) =>
                {
                    return 1.0;
                };
            }

            return Assume(assumption, evaluate).Probability > 0;
        }

        public bool AssumeIsTrue(AssumptionName name, EvaluateAssumptionDelegate evaluate = null)
        {
            if (evaluate == null)
            {
                evaluate = (context, a) =>
                    {
                        return 1.0;
                    };
            }

            return Assume(name, evaluate).Probability > 0;
        }

        public bool AssumeIsFalse(AssumptionName name, EvaluateAssumptionDelegate evaluate)
        {
            if (evaluate == null)
            {
                evaluate = (context, a) =>
                {
                    return 0.0;
                };
            }

            return Assume(name, evaluate).Probability == 0;
        }

        public Assumption Assume(AssumptionName name, EvaluateAssumptionDelegate evaluate)
        {
            return Assume<Assumption>(name, evaluate);
        }

        public Assumption Assume<T>(AssumptionName name, EvaluateAssumptionDelegate evaluate) where T : Assumption, new()
        {
            Assumption assumption = new T();

            assumption.Name = name;

            return Assume(assumption, evaluate);
        }

        public Assumption Assume(Assumption assumption, EvaluateAssumptionDelegate evaluate)
        {
            try
            {
                return this.Assumptions[assumption];
            }
            catch (KeyNotFoundException)
            {
            }

            assumption.Transition = this.CurrentTransition;
            assumption.ID = ++this.Global.AssumptionIDPool;

            assumption.Probability = evaluate(this, assumption);

            this.Assumptions[assumption] = assumption;

            ResetAssumptionEquivalenceClass();

            this.Exploitability *= assumption.Probability;

            return assumption;
        }

        public void Assume(AssumptionName name, double probability = 1.0)
        {
            Assume(new Assumption(name, probability));
        }

        public void Assume(Assumption assumption)
        {
            if (HasAssumption(assumption))
            {
                return;
            }

            assumption.Transition = this.CurrentTransition;
            assumption.ID = ++this.Global.AssumptionIDPool;

            this.Assumptions[assumption] = assumption;

            //
            // Factor the probability of this assumption into the total probability.
            //

            this.Exploitability *= assumption.Probability;
        }

        public bool HasAssumption(AssumptionName name)
        {
            return HasAssumption(new Assumption(name));
        }

        public bool HasAssumption(Assumption assumption)
        {
            if (this.Assumptions.ContainsKey(assumption))
            {
                Assumption existingAssumption = this.Assumptions[assumption];
                existingAssumption.Used = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ForgetAssumption(AssumptionName name)
        {
            ForgetAssumption(new Assumption(name));
        }

        public void ForgetAssumption(Assumption assumption)
        {
            if (this.Assumptions.ContainsKey(assumption))
            {
                this.Assumptions.Remove(assumption);
            }
        }

        public bool IsAssumedTrueOrUndefined(AssumptionName name)
        {
            return IsAssumedTrueOrUndefined(new Assumption(name));
        }

        public bool IsAssumedTrueOrUndefined(Assumption assumption)
        {
            if (this.Assumptions.ContainsKey(assumption))
            {
                this.Assumptions[assumption].Used = true;
                return this.Assumptions[assumption].Probability > 0;
            }
            else
            {
                return true;
            }
        }

        public bool IsAssumedTrue(AssumptionName name)
        {
            return IsAssumedTrue(new Assumption(name));
        }

        public bool IsAssumedTrue(Assumption assumption)
        {
            if (this.Assumptions.ContainsKey(assumption))
            {
                this.Assumptions[assumption].Used = true;
                return this.Assumptions[assumption].Probability > 0;
            }
            else
            {
                return false;
            }
        }

        public bool AttackerFavorsAssumeTrue(AssumptionName name)
        {
            return AttackerFavorsAssumeTrue(new Assumption(name));
        }

        public bool AttackerFavorsAssumeTrue(Assumption assumption)
        {
            if (this.Assumptions.ContainsKey(assumption))
            {
                this.Assumptions[assumption].Used = true;
                return this.Assumptions[assumption].Probability > 0;
            }

            if (this.ModeIsFavorDefense)
            {
                assumption.Probability = 0;
            }
            else
            {
                assumption.Probability = 1;
            }

            assumption.Used = true;

            Assume(assumption);

            return true;
        }
    }

    public class SimulationContextCollection
    {
        public SimulationContextCollection()
        {
            this.EquivalentMemberCount = 0;
            this.SimulationContextList = new List<SimulationContext>();
        }

        public void AddSimulation(SimulationContext context)
        {
            this.SimulationContextList.Add(context);
        }

        public IEnumerable<SimulationContext> SimulationContexts
        {
            get { return this.SimulationContextList; }
        }

        public long EquivalentMemberCount { get; set; }

        private List<SimulationContext> SimulationContextList { get; set; }
    }

    public static class SimulationContextConstraintExtensions
    {
        public static bool CanCorruptMemoryAtAddress(this SimulationContext context, MemoryAddress address)
        {
            if (address == null)
            {
                return true;
            }

            Assumption assumption = new Assumption.CanCorruptMemoryAtAddress(address);

            if (context.IsAssumedTrue(assumption))
            {
                return true;
            }

            bool canCorrupt = true;

            if (context.IsAssumedTrue(AssumptionName.CanCorruptMemoryAtAddressListComplete))
            {
                canCorrupt = false;
                goto Exit;
            }

            //
            // If this address corresponds to a memory region that does not exist in our current
            // execution domain, then it is not possible to corrupt it.
            //

            if ((context.CurrentViolation.ExecutionDomain != null) &&
                (
                 (context.CurrentViolation.ExecutionDomain == ExecutionDomain.Kernel)
                 &&
                 (address.IsKernelAddress == false)
                )
                ||
                (
                 (context.CurrentViolation.ExecutionDomain != ExecutionDomain.Kernel)
                 &&
                 (address.IsKernelAddress == true)
                )
               )
            {
                canCorrupt = false;
                goto Exit;
            }

            if (context.IsAbsolute())
            {
                canCorrupt =
                    (
                        context.CanPositionContentAtAbsoluteAddress(address)
                        ||
                        context.CanFindAddress(address)
                    );
            }
            else
            {
                canCorrupt =
                    (
                        (
                        (context.CanPositionContentAtRelativeAddress(address) == true)
                        ||
                        (context.CanDetermineDisplacementToAddress(address) == true)
                        )
                        &&
                        (context.CanCorruptMemoryAtRelativeAddress(address) == true)
                    );
            }

Exit:
            assumption.Probability = Assumption.BooleanProbability(canCorrupt);

            if (assumption.Probability == 0)
            {
                throw new ConstraintNotSatisfied(String.Format("Attacker cannot corrupt memory at {0}.", address));
            }

            context.Assume(assumption);

            return true;
        }

        public static bool CanReadMemoryAtAddress(this SimulationContext context, MemoryAddress address)
        {
            Assumption assumption = new Assumption.CanReadMemoryAtAddress(address);

            if (context.IsAssumedTrue(assumption))
            {
                return true;
            }

            bool canRead = true;

            //
            // If this address corresponds to a memory region that does not exist in our current
            // execution domain, then it is not possible to read it.
            //

            if ((context.CurrentViolation.ExecutionDomain != null) &&
                (
                 (context.CurrentViolation.ExecutionDomain == ExecutionDomain.Kernel)
                 &&
                 (address.IsKernelAddress == false)
                )
                ||
                (
                 (context.CurrentViolation.ExecutionDomain != ExecutionDomain.Kernel)
                 &&
                 (address.IsKernelAddress == true)
                )
               )
            {
                canRead = false;
            }

            if (canRead != false)
            {
                if (context.IsAbsolute())
                {
                    canRead =
                        (
                         (context.CurrentViolation.IsBaseControlled == false)
                         ||
                         (context.CanPositionContentAtAbsoluteAddress(address))
                         ||
                         context.CanFindAddress(address)
                        );
                }
                else
                {
                    canRead =
                        (
                         context.CanPositionContentAtRelativeAddress(address)
                         ||
                         context.CanDetermineDisplacementToAddress(address)
                        );
                }
            }

            assumption.Probability = Assumption.BooleanProbability(canRead);

            if (assumption.Probability == 0)
            {
                throw new ConstraintNotSatisfied(String.Format("Attacker cannot read memory at {0}.", address));
            }

            context.Assume(assumption);

            return true;
        }

        public static bool CanExecuteMemoryAtAddress(this SimulationContext context, MemoryAddress address)
        {
            if (address.DataType == MemoryContentDataType.AttackerControlledData)
            {
                return context.CanExecuteData();
            }

            //
            // TODO
            //

            return true;
        }

        public static bool CanPositionContentAtAbsoluteAddress(this SimulationContext context, MemoryAddress address)
        {
            Assumption assumption = new Assumption.CanPositionAtDesiredAbsoluteAddress(address);

            if (context.IsAssumedTrue(assumption))
            {
                return true;
            }

            //
            // TODO
            //

            context.Assume(assumption);

            return true;
        }

        public static bool CanPositionContentAtRelativeAddress(this SimulationContext context, MemoryAddress address)
        {
            Assumption assumption = new Assumption.CanPositionAtDesiredRelativeAddress(address);

            if (context.IsAssumedTrue(assumption))
            {
                return true;
            }

            //
            // TODO
            //

            //#if false
            //                result = 
            //                    (
            //                     (
            //                      (context.CurrentViolation.Direction == MemoryAccessDirection.Forward)
            //                      &&
            //                      (
            //                       (
            //                        (context.CurrentViolation.InitialDisplacement == MemoryAccessDisplacement.PostAdjacent)
            //                        && (context.CanPositionPostAdjacent(address)
            //                        && (context.CanDetermineDisplacementToAddress(address)
            //                       )
            //                       ||
            //                       (
            //                        (context.CurrentViolation.InitialDisplacement == MemoryAccessDisplacement.PostNonadjacent)
            //                        && (context.CanPositionPostNonadjacent(address)
            //                        && (context.CanDetermineDisplacementToAddress(address)
            //                       )
            //                      )
            //                     )
            //                     ||
            //                     (
            //                      (context.CurrentViolation.Direction == MemoryAccessDirection.Reverse)
            //                      &&
            //                      (
            //                       (
            //                        (context.CurrentViolation.InitialDisplacement == MemoryAccessDisplacement.PreAdjacent)
            //                        && (context.CanPositionPreAdjacent(address)
            //                        && (context.CanDetermineDisplacementToAddress(address)
            //                       )
            //                       ||
            //                       (
            //                        (context.CurrentViolation.InitialDisplacement == MemoryAccessDisplacement.PreNonadjacent)
            //                        && (context.CanPositionPreNonadjacent(address)
            //                        && (context.CanDetermineDisplacementToAddress(address)
            //                       )
            //                      )
            //                     )
            //                    );
            //#endif

            context.Assume(assumption);

            return true;
        }

        public static bool CanCorruptMemoryAtRelativeAddress(this SimulationContext context, MemoryAddress address)
        {
            return true;
#if false
            if (
                (context.CurrentViolation.IsDisplacementControlled)
                &&
                (
                 (context.AttackerFavorsEqual(context.CurrentViolation.DisplacementInitialOffset, MemoryAccessOffset.PostNonAdjacent))
                 ||
                 (context.AttackerFavorsEqual(context.CurrentViolation.DisplacementInitialOffset, MemoryAccessOffset.PostNonAdjacent))
                )
               )
            {
                return true;
            }
            else
            {
                //
                // Attacker does not control displacement, must be able to perform linear corruption.  As such,
                // we will assume for now that the regions must be of the same type
                //

                if (
                    (
                     (context.CurrentViolation.BaseRegionType == null)
                     ||
                     (context.AttackerFavorsEqual(context.CurrentViolation.BaseRegionType, address.Region))
                    )
                    &&
                    (
                     (context.CurrentViolation.Address == null)
                     ||
                     (context.AttackerFavorsEqual(context.CurrentViolation.Address.Region, address.Region))
                    )
                   )
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
#endif
        }

        public static bool IsMemoryWrite(this SimulationContext context)
        {
            return context.AttackerFavorsEqual(context.CurrentViolation.Method, MemoryAccessMethod.Write);
        }

        public static bool IsAbsolute(this SimulationContext context)
        {
            if (context.CurrentViolation.IsBaseControlled)
            {
                return true;
            }
            else
            {
                return context.AttackerFavorsEqual(context.CurrentViolation.AddressingMode, MemoryAddressingMode.Absolute);
            }
        }

        public static bool IsRelative(this SimulationContext context)
        {
            return IsAbsolute(context) == false;
        }

        public static bool AssumeIsStackProtectionCookieCorrupted(this SimulationContext context)
        {
            return context.AssumeIsTrue(
                new Assumption.CanCorruptMemoryAtAddress(MemoryAddress.AddressOfStackProtectionCookie),
                (ctx, assumption) =>
                {
                    Violation v = ctx.CurrentViolation;

                    //
                    // Not a memory write?
                    //

                    if (ctx.IsMemoryWrite() == false)
                    {
                        return Assumption.BooleanProbability(false);
                    }

                    //
                    // GS not enabled?  Then no.
                    //

                    if (ctx.AttackerFavorsEqual(v.FunctionStackProtectionEnabled, false))
                    {
                        return Assumption.BooleanProbability(false);
                    }

                    //
                    // If this is an absolute write, then assume that we can avoid corrupting the cookie.
                    //

                    if (ctx.IsAbsolute())
                    {
                        return Assumption.BooleanProbability(ctx.AttackerFavorsFalse());
                    }

                    // 
                    // If this is a forward, relative, write with a controlled displacement, then assume that
                    // we can avoid corrupting the cookie.
                    //

                    if ((ctx.AttackerFavorsEqual(v.Direction, MemoryAccessDirection.Forward))
                        && (ctx.AttackerFavorsEqual(v.AddressingMode, MemoryAddressingMode.Relative))
                        && (ctx.AttackerFavorsEqual(v.DisplacementState, MemoryAccessParameterState.Controlled)))
                    {
                        // TODO: assume that we can start writing after GS

                        return Assumption.BooleanProbability(ctx.AttackerFavorsFalse());
                    }

                    //
                    // If this is a reverse write, the extent is controlled, and the buffer is in a parent frame, 
                    // then assume that we can avoid corrupting the cookie.
                    //

                    if ((ctx.AttackerFavorsEqual(v.Direction, MemoryAccessDirection.Reverse))
                        && (ctx.AttackerFavorsEqual(v.ExtentState, MemoryAccessParameterState.Controlled)))
                    {
                        // TODO: assume buffer is in parent frame.

                        return Assumption.BooleanProbability(ctx.AttackerFavorsFalse());
                    }

                    //
                    // Otherwise, the attacker favors that the GS cookie is not corrupted.
                    //

                    return Assumption.BooleanProbability(ctx.AttackerFavorsFalse());
                });

        }

        public static bool CanTriggerException(this SimulationContext context)
        {
            return context.AttackerFavorsAssumeTrue(AssumptionName.CanTriggerException);
        }

        /// <summary>
        /// Maps the supplied memory address into its containing regions.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static IEnumerable<MemoryRegion> MapAddressToMemoryRegions(this SimulationContext context, MemoryAddress address)
        {
            List<MemoryRegion> regions;

            if (context.Target.Application.KernelApplication == true)
            {
                throw new NotSupportedException();
            }

            switch (address.Region)
            {
                case MemoryRegionType.ImageCodeSegmentNtdll:
                case MemoryRegionType.ImageCodeSegment:
                case MemoryRegionType.ImageDataSegment:
                    regions = new List<MemoryRegion>(MemoryRegions.UserImageBaseRegions);
                    break;

                case MemoryRegionType.Stack:
                    regions = new List<MemoryRegion>();
                    regions.Add(MemoryRegion.UserThreadStack);
                    break;

                case MemoryRegionType.Heap:
                    regions = new List<MemoryRegion>();
                    regions.Add(MemoryRegion.UserProcessHeap);
                    break;

                case MemoryRegionType.JITCode:
                    regions = new List<MemoryRegion>();
                    regions.Add(MemoryRegion.UserJITCode);
                    break;

                case MemoryRegionType.Any:
                    regions = new List<MemoryRegion>();
                    regions.Add(MemoryRegion.UserThreadStack);
                    regions.Add(MemoryRegion.UserProcessHeap);

                    // other writable regions -- need to be data type specific here
                    break;

                default:
                    throw new NotSupportedException();

            }

            return regions;
        }

        public static bool CanDetermineDisplacementToAddress(this SimulationContext context, MemoryAddress address)
        {
            Assumption assumption = new Assumption.CanDetermineDisplacementToAddress(address);

            if (context.IsAssumedTrue(assumption))
            {
                return true;
            }

            //
            // TODO: logic for probability of determining displacement.
            //

            context.Assume(assumption);

            return true;
        }

        public static bool CanFindAddress(this SimulationContext context, MemoryAddress address)
        {
            Assumption assumption = new Assumption.CanFindAddress(address);

            if (context.HasAssumption(assumption))
            {
                return true;
            }

            //
            // Handle pseudo addresses first.
            //

            switch (address.Region)
            {
                case MemoryRegionType.ImageCodeSegment:
                    if (context.IsAssumedTrue(AssumptionName.CanLoadNonASLRImage) == true)
                    {
                        assumption.Probability = 1;
                        goto Exit;
                    }
                    break;

                case MemoryRegionType.Heap:
                case MemoryRegionType.Any:
                    switch (address.DataType)
                    {
                        case MemoryContentDataType.CppVirtualTablePointer:
                        case MemoryContentDataType.FunctionPointer:
                        case MemoryContentDataType.Data:
                        case MemoryContentDataType.WriteBasePointer:
                        case MemoryContentDataType.WriteDisplacement:
                        case MemoryContentDataType.WriteExtent:
                        case MemoryContentDataType.WriteContent:
                        case MemoryContentDataType.ReadBasePointer:
                        case MemoryContentDataType.ReadContent:
                        case MemoryContentDataType.ReadDisplacement:
                        case MemoryContentDataType.ReadExtent:
                        case MemoryContentDataType.AttackerControlledData:
                            if (context.AttackerFavorsEqual(context.Target.Application.CanInitializeContentViaHeapSpray, true) == true)
                            {
                                assumption.Probability = 1;
                                goto Exit;
                            }
                            break;

                        case MemoryContentDataType.AttackerControlledCode:
                            if ((context.AttackerFavorsEqual(context.Target.Application.CanInitializeCodeViaJIT, true) == true)
                                ||
                                ((context.CanExecuteData() && (context.AttackerFavorsEqual(context.Target.Application.CanInitializeContentViaHeapSpray, true) == true))))
                            {
                                assumption.Probability = 1;
                                goto Exit;
                            }
                            break;
                            
                        case MemoryContentDataType.Code:
                            break;

                        default:
                            throw new NotSupportedException(String.Format("Find address on {0} is not supported", address));
                    }
                    break;

                default:
                    break;
            }
            
            //
            // Get the set of memory regions that may contain this memory address.
            //

            IEnumerable<MemoryRegion> regions;

            try
            {
                regions = new List<MemoryRegion>(context.MapAddressToMemoryRegions(address));
            }
            catch (NotSupportedException)
            {
                regions = new List<MemoryRegion>();
            }

            if (regions.Count() > 0)
            {
                //
                // Find the region with the least entropy and use that to compute the probability that the provided
                // address could be found by the attacker.
                //

                uint minEntropyBits;

                try
                {
                    minEntropyBits =
                        (
                         from MemoryRegion region in regions
                         select new { Region = region, EntropyBits = context.Target.OperatingSystem.MemoryRegionASLREntropyBits[region] }
                        ).Min(x => x.EntropyBits);
                }
                catch (KeyNotFoundException)
                {
                    minEntropyBits = 0;
                }

                assumption.Probability = 1 / Math.Pow(2, minEntropyBits);
            }
            else
            {
                assumption.Probability = 0;
            }

Exit:
            context.Assume(assumption);

            if (assumption.Probability == 0)
            {
                throw new ConstraintNotSatisfied(String.Format("Attacker is not able to find the address of {0}.", address));
            }

            return true;
        }

        /// <summary>
        /// Checks if the attacker can determine the value of the stack protection cookie.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool CanDetermineStackProtectionCookie(this SimulationContext context)
        {
            Assumption assumption = new Assumption(AssumptionName.CanDetermineStackProtectionCookie);

            if (context.HasAssumption(assumption))
            {
                return true;
            }

            //
            // Compute the probability of an attacker being able to guess the cookie value.
            //

            uint entropyBits =  context.CurrentViolation.FunctionStackProtectionEntropyBits.GetValueOrDefault();

            assumption.Probability = 1 / Math.Pow(2, entropyBits);

            context.Assume(assumption);

            return true;
        }

        /// <summary>
        /// Determines if stack-based SEH is used by the target.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool IsStackSEHUsed(this SimulationContext context)
        {
            return
                (
                 (context.Target.OperatingSystem.IsWindows()) &&
                 (
                  (context.Target.Hardware.ArchitectureFamily == ArchitectureFamily.I386) ||
                  (context.Target.Hardware.ArchitectureFamily == ArchitectureFamily.AMD64)
                 ) &&
                 (context.Target.Application.AddressBits == 32)
                );
        }

        /// <summary>
        /// Checks if the attacker can bypass SafeSEH.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool CanBypassSAFESEH(this SimulationContext context)
        {
            if (context.Target.OperatingSystem.IsWindows() == false || context.Target.IsX86Application() == false)
            {
                return false;
            }

            Assumption assumption = new Assumption(AssumptionName.CanBypassSafeSEH);

            if (context.HasAssumption(assumption))
            {
                return context.Assumptions[assumption].IsTrue;
            }

            OperatingSystem.Windows wos = context.Target.OperatingSystem as OperatingSystem.Windows;
            Application.Windows wapp = context.Target.Application as Application.Windows;

            bool value =
                (
                 (context.AttackerFavorsEqual(wos.UserSafeSEHPolicy.IsOn(), false)) ||
                 (context.CanLoadNonSafeSEHImage())
                );

            assumption.IsTrue = value;

            context.Assume(assumption);

            return value;
        }

        /// <summary>
        /// Checks if the attacker can bypass SEHOP.
        /// </summary>
        /// <remarks>
        /// SEHOP can be bypassed if:
        /// 
        ///   - It is not enabled
        ///   
        ///   OR
        ///   
        ///   - The attacker can find the base address of NTDLL (for the FinalExceptionHandler) and they
        ///     know the address of the stack/attacker controlled data (for maintaining the integrity of the chain).
        /// </remarks>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool CanBypassSEHOP(this SimulationContext context)
        {
            if (context.Target.OperatingSystem.IsWindows() == false || context.Target.IsX86Application() == false)
            {
                return false;
            }

            Assumption assumption = new Assumption(AssumptionName.CanBypassSEHOP);

            if (context.HasAssumption(assumption))
            {
                return context.Assumptions[assumption].IsTrue;
            }

            OperatingSystem.Windows wos = context.Target.OperatingSystem as OperatingSystem.Windows;
            Application.Windows wapp = context.Target.Application as Application.Windows;
            
            bool value =
                (
                 (context.AttackerFavorsEqual(wapp.UserSEHOPPolicy.IsOn(), false) == true) 
                 
                 ||

                 (context.CanFindAddress(MemoryAddress.AddressOfNtdllImageBase))
                );

            assumption.IsTrue = value;

            context.Assume(assumption);

            return value;
        }

        /// <summary>
        /// Checks if the attacker can execute from data memory regions (such as the heap).
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool CanExecuteData(this SimulationContext context)
        {
            if (context.Target.Application.KernelApplication == true)
            {
                throw new NotImplementedException();
            }

            return
                (
                 (context.IsAssumedTrue(AssumptionName.CanExecuteData)) ||
                 (context.Target.Application.MemoryRegionNXPolicy[MemoryRegion.UserProcessHeap].IsOff())
                );
        }

        public static bool CanLoadNonASLRImage(this SimulationContext context)
        {
            Assumption assumption = new Assumption(AssumptionName.CanLoadNonASLRImage);

            if (context.HasAssumption(assumption))
            {
                return context.Assumptions[assumption].IsTrue;
            }

            //
            // If this is a kernel application, then it is not possible to load a non-ASLR image.
            //
            // If force relocation of images in user mode is not enabled, then it is possible to load a non-ASLR image.
            //
            // Otherwise, it is not possible.
            //

            bool value;

            if (context.Target.Application.KernelApplication == true)
            {
                value = false;
            }
            else if (context.AttackerFavorsEqual(context.Target.Application.MemoryRegionASLRPolicy[MemoryRegion.UserForceRelocatedImageCode].IsOn(), false))
            {
                value = true;
            }
            else
            {
                value = false;
            }

            return value;
        }

        public static bool CanLoadNonSafeSEHImage(this SimulationContext context)
        {
            Assumption assumption = new Assumption(AssumptionName.CanLoadNonASLRNonSafeSEHImage);

            if (context.HasAssumption(assumption))
            {
                return context.Assumptions[assumption].IsTrue;
            }

            bool value = context.AttackerFavorsTrue();

            if ((context.Target.OperatingSystem.IsWindows() == false)
                || (context.Target.IsX86Application() == false))
            {
                value = false;
            }

            return value;
        }

        public static bool CanInitializeContentViaHeapSpray(this SimulationContext context)
        {
            Assumption assumption = new Assumption(AssumptionName.CanInitializeContentViaHeapSpray);

            if (context.HasAssumption(assumption))
            {
                return context.Assumptions[assumption].IsTrue;
            }

            return context.AttackerFavorsEqual(context.Target.Application.CanInitializeContentViaHeapSpray, true);
        }

        public static bool CanInitializeCodeViaJIT(this SimulationContext context)
        {
            Assumption assumption = new Assumption(AssumptionName.CanInitializeCodeViaJIT);

            if (context.HasAssumption(assumption))
            {
                return context.Assumptions[assumption].IsTrue;
            }

            return context.AttackerFavorsEqual(context.Target.Application.CanInitializeCodeViaJIT, true);
        }
    }
}
