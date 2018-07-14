// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Xml.Serialization;

namespace MSModel.Profiles
{
    public enum ExploitationPrimitiveType
    {
        Identity,
        WriteToRead,
        WriteToExecute,
        ReadToWrite,
        ReadToRead,
        ReadToExecute,
        ExecuteToExecute
    }

    public delegate void PrimitiveTransitionSuccessDelegate(SimulationContext context, ref Violation newViolation);

    public delegate Violation GetNextViolationDelegate(SimulationContext context);

    /// <summary>
    /// A primitive method of transitioning between memory safety violations.
    /// </summary>
    public class ExploitationPrimitive : Profile
    {
        public ExploitationPrimitive()
            : this(ExploitationPrimitiveType.Identity, "unknown", "unknown")
        {
            // Make XML serialization happy.
        }

        public ExploitationPrimitive(
            ExploitationPrimitiveType primitiveType,
            string symbol,
            string name
            )
        {
            this.PrimitiveType = primitiveType;
            this.Symbol = symbol;
            this.Name = name;

            this.ConstraintList = new List<Expression<Func<SimulationContext, bool>>>();
        }

        public ExploitationPrimitiveType PrimitiveType { get; private set; }

        public bool IsIdentity
        {
            get
            {
                return this.PrimitiveType == ExploitationPrimitiveType.Identity;
            }
        }

        /// <summary>
        /// A unique descriptor for the type of primitive.
        /// </summary>
        public virtual string PrimitiveDescriptor
        {
            get
            {
                return String.Format("{0}", this.PrimitiveType);
            }
        }

        /// <summary>
        /// Get the next violation once the primitive has succeeded.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual Violation GetNextViolation(SimulationContext context)
        {
            if (this.NextViolationDelegate != null)
            {
                return this.NextViolationDelegate(context);
            }
            else
            {
                throw new NotSupportedException("A next violation delegate must be specified.");
            }
        }

        /// <summary>
        /// Inherit parameter state from one violation to another (transitively enabled) violation.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public virtual void InheritParameterState(Violation from, Violation to)
        {
        }

        public MemoryAccessMethod? FromMethod
        {
            get
            {
                switch (this.PrimitiveType)
                {
                    case ExploitationPrimitiveType.ReadToExecute:
                    case ExploitationPrimitiveType.ReadToRead:
                    case ExploitationPrimitiveType.ReadToWrite:
                        return MemoryAccessMethod.Read;

                    case ExploitationPrimitiveType.WriteToExecute:
                    case ExploitationPrimitiveType.WriteToRead:
                        return MemoryAccessMethod.Write;

                    case ExploitationPrimitiveType.ExecuteToExecute:
                        return MemoryAccessMethod.Execute;

                    case ExploitationPrimitiveType.Identity:
                        return null;

                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public MemoryAccessMethod? ToMethod
        {
            get
            {
                switch (this.PrimitiveType)
                {
                    case ExploitationPrimitiveType.ReadToRead:
                    case ExploitationPrimitiveType.WriteToRead:
                        return MemoryAccessMethod.Read;

                    case ExploitationPrimitiveType.ReadToWrite:
                        return MemoryAccessMethod.Write;

                    case ExploitationPrimitiveType.ReadToExecute:
                    case ExploitationPrimitiveType.WriteToExecute:
                    case ExploitationPrimitiveType.ExecuteToExecute:
                        return MemoryAccessMethod.Execute;

                    case ExploitationPrimitiveType.Identity:
                        return null;

                    default:
                        throw new NotSupportedException();
                }
            }
        }

        [XmlIgnore]
        public GetNextViolationDelegate NextViolationDelegate;

        public event PrimitiveTransitionSuccessDelegate OnSuccess;

        public void NotifyOnSuccess(SimulationContext context, ref Violation newViolation)
        {
            if (this.OnSuccess != null)
            {
                this.OnSuccess(context, ref newViolation);
            }
        }

        [XmlIgnore]
        public List<Expression<Func<SimulationContext, bool>>> ConstraintList { get; private set; }

        public override IEnumerable<Profile> Children
        {
            get { yield break; }
        }

        public override ModelType ModelType
        {
            get { return MSModel.ModelType.Technique; }
        }

        protected void Update(
            Expression<Func<SimulationContext, bool>> constraints = null,
            GetNextViolationDelegate nextViolation = null,
            PrimitiveTransitionSuccessDelegate onSuccess = null)
        {
            if (constraints != null)
            {
                this.ConstraintList.Add(constraints);
            }

            if (nextViolation != null)
            {
                this.NextViolationDelegate = nextViolation;
            }

            if (onSuccess != null)
            {
                this.OnSuccess += onSuccess;
            }
        }
    }

    #region Fundamental primitives

    /// <summary>
    /// Write to memory primitive.
    /// </summary>
    public class WritePrimitive : ExploitationPrimitive
    {
        public WritePrimitive(
            ExploitationPrimitiveType primitiveType,
            string name,
            MemoryAddress writeAddress = null,
            Expression<Func<SimulationContext, bool>> constraints = null,
            GetNextViolationDelegate nextViolation = null,
            PrimitiveTransitionSuccessDelegate onSuccess = null
            )
            : base(primitiveType, "write", name)
        {
            this.WriteAddress = writeAddress;

            this.ConstraintList.Add(
                (context) =>
                    (
                        //
                        // This must be a memory write, to a destination address whose state is initialized and equal
                        // to the specified write address, and whose content can actually be corrupted.
                        //

                        (context.CurrentViolation.Method == MemoryAccessMethod.Write)

                        &&

                        (
                            (context.Global.AssumeContentInitializationPossible == true)

                            ||

                            (this.WriteAddress.IsImplicitlyInitialized)

                            ||

                            (context.CurrentViolation.ContentDstState == MemoryAccessParameterState.Controlled)

                            ||

                            (context.CurrentViolation.ContentDstState == MemoryAccessParameterState.Fixed)
                        )

                        &&

                        (context.AttackerFavorsEqual(context.CurrentViolation.Address, this.WriteAddress) == true)

                        &&

                        (context.CanCorruptMemoryAtAddress(this.WriteAddress) == true)
                    )
                );

            Update(constraints, nextViolation, onSuccess);
        }

        public MemoryAddress WriteAddress { get; private set; }
    }

    public class ReadPrimitive : ExploitationPrimitive
    {
        public ReadPrimitive(
            ExploitationPrimitiveType primitiveType,
            string name,
            MemoryAddress readAddress = null,
            Expression<Func<SimulationContext, bool>> constraints = null,
            GetNextViolationDelegate nextViolation = null,
            PrimitiveTransitionSuccessDelegate onSuccess = null
            )
            : base(primitiveType, "read", name)
        {
            this.ReadAddress = readAddress;

            this.ConstraintList.Add(
                (context) =>
                    (
                        //
                        // This must be a memory read, from a source address whose state is initialized and equal
                        // to the specified read address, and whose content can actually be read.
                        //

                        (context.CurrentViolation.Method == MemoryAccessMethod.Read)

                        &&

                        (
                            (context.Global.AssumeContentInitializationPossible == true)

                            ||

                            (this.ReadAddress.IsImplicitlyInitialized)

                            ||

                            (context.CurrentViolation.ContentSrcState == MemoryAccessParameterState.Controlled)

                            ||

                            (context.CurrentViolation.ContentSrcState == MemoryAccessParameterState.Fixed)
                        )

                        &&

                        (context.AttackerFavorsEqual(context.CurrentViolation.Address, this.ReadAddress) == true)

                        &&

                        (context.CanReadMemoryAtAddress(this.ReadAddress) == true)
                    )
                );

            Update(constraints, nextViolation, onSuccess);
        }

        public MemoryAddress ReadAddress { get; private set; }
    }

    /// <summary>
    /// Initializes the content of memory that is being read from.
    /// </summary>
    public class InitializeSourceContentPrimitive : ExploitationPrimitive
    {
        public InitializeSourceContentPrimitive(
            string name = "initialize content at address of read",
            MemoryAddress sourceAddress = null,
            MemoryAccessParameterState newContentState = MemoryAccessParameterState.Controlled,
            Expression<Func<SimulationContext, bool>> constraints = null,
            PrimitiveTransitionSuccessDelegate onSuccess = null
            )
            : base(ExploitationPrimitiveType.Identity, "initialize_source_content", name)
        {
            this.SourceAddress = sourceAddress;
            this.NewContentState = newContentState;

            this.ConstraintList.Add(
                (context) =>
                    (
                        (context.Global.AssumeContentInitializationPossible == false)

                        &&

                        (context.AttackerFavorsEqual(context.CurrentViolation.Method, MemoryAccessMethod.Read) == true)

                        &&

                        (
                            (context.AttackerFavorsEqual(context.CurrentViolation.ContentSrcState, MemoryAccessParameterState.Uninitialized) == true)

                            ||

                            (context.AttackerFavorsEqual(context.CurrentViolation.ContentSrcState, MemoryAccessParameterState.Unknown) == true)
                        )

                        &&

                        (context.AttackerFavorsEqual(context.CurrentViolation.Address, this.SourceAddress) == true)

                        &&

                        (context.CanReadMemoryAtAddress(this.SourceAddress) == true)
                    )
                );

            this.NextViolationDelegate = (context) =>
            {
                Violation v = context.CurrentViolation.CloneViolation();

                v.ContentSrcState = this.NewContentState;

                v.Address = this.SourceAddress;

                return v;
            };

            this.OnSuccess += onSuccess;

            if (constraints != null)
            {
                this.ConstraintList.Add(constraints);
            }
        }

        public MemoryAddress SourceAddress { get; private set; }

        public MemoryAccessParameterState NewContentState { get; private set; }
    }

    /// <summary>
    /// Initializes the content of memory that is being read from.
    /// </summary>
    public class InitializeExecutableContentPrimitive : ExploitationPrimitive
    {
        public InitializeExecutableContentPrimitive(
            string name = "initialize content at address of executed code",
            MemoryAddress codeAddress = null,
            MemoryAccessParameterState newContentState = MemoryAccessParameterState.Controlled,
            Expression<Func<SimulationContext, bool>> constraints = null,
            PrimitiveTransitionSuccessDelegate onSuccess = null
            )
            : base(ExploitationPrimitiveType.Identity, "initialize_executable_content", name)
        {
            this.CodeAddress = codeAddress;
            this.NewContentState = newContentState;

            this.ConstraintList.Add(
                (context) =>
                    (
                        (context.Global.AssumeContentInitializationPossible == false)

                        &&

                        (context.AttackerFavorsEqual(context.CurrentViolation.Method, MemoryAccessMethod.Execute) == true)

                        &&

                        (
                            (context.AttackerFavorsEqual(context.CurrentViolation.ContentSrcState, MemoryAccessParameterState.Uninitialized) == true)

                            ||

                            (context.AttackerFavorsEqual(context.CurrentViolation.ContentSrcState, MemoryAccessParameterState.Unknown) == true)
                        )

                        &&

                        (context.AttackerFavorsEqual(context.CurrentViolation.Address, this.CodeAddress) == true)

                        &&

                        (context.CanExecuteMemoryAtAddress(this.CodeAddress) == true)
                    )
                );

            this.NextViolationDelegate = (context) =>
            {
                Violation v = context.CurrentViolation.CloneViolation();

                v.ContentSrcState = this.NewContentState;

                v.Address = this.CodeAddress;

                return v;
            };

            this.OnSuccess += onSuccess;

            if (constraints != null)
            {
                this.ConstraintList.Add(constraints);
            }
        }

        public MemoryAddress CodeAddress { get; private set; }

        public MemoryAccessParameterState NewContentState { get; private set; }
    }

    /// <summary>
    /// Initializes the content of memory that is being written to.
    /// </summary>
    /// <remarks>
    /// This primitive is used in cases where it is necessary to initialize a destination address
    /// to content of a specific type prior to a memory write.
    /// </remarks>
    public class InitializeDestinationContentPrimitive : ExploitationPrimitive
    {
        public InitializeDestinationContentPrimitive(
            string name = "initialize content at destination address of write",
            MemoryAddress destinationAddress = null,
            MemoryAccessParameterState newContentState = MemoryAccessParameterState.Controlled,
            Expression<Func<SimulationContext, bool>> constraints = null,
            PrimitiveTransitionSuccessDelegate onSuccess = null
            )
            : base(ExploitationPrimitiveType.Identity, "initialize_destination_content", name)
        {
            this.DestinationAddress = destinationAddress;
            this.NewContentState = newContentState;

            this.ConstraintList.Add(
                (context) =>
                    (
                        (context.Global.AssumeContentInitializationPossible == false)

                        &&

                        (context.AttackerFavorsEqual(context.CurrentViolation.Method, MemoryAccessMethod.Write) == true)

                        &&

                        (
                            (context.AttackerFavorsEqual(context.CurrentViolation.ContentDstState, MemoryAccessParameterState.Uninitialized) == true)

                            ||

                            (context.AttackerFavorsEqual(context.CurrentViolation.ContentDstState, MemoryAccessParameterState.Unknown) == true)
                        )

                        &&

                        (context.AttackerFavorsEqual(context.CurrentViolation.Address, this.DestinationAddress) == true)

                        &&

                        (context.CanCorruptMemoryAtAddress(this.DestinationAddress) == true)
                    )
                );

            this.NextViolationDelegate = (context) =>
            {
                Violation v = context.CurrentViolation.CloneViolation();

                v.ContentDstState = this.NewContentState;

                v.Address = this.DestinationAddress;

                return v;
            };

            this.OnSuccess += onSuccess;

            if (constraints != null)
            {
                this.ConstraintList.Add(constraints);
            }
        }

        public MemoryAddress DestinationAddress { get; private set; }

        public MemoryAccessParameterState NewContentState { get; private set; }
    }

    /// <summary>
    /// A primitive for executing code once an execute violation has been reached.
    /// </summary>
    /// <remarks>
    /// Transitions from one execute violation to another execute violation.
    /// </remarks>
    public class CodeExecutionPrimitive : ExploitationPrimitive
    {
        public CodeExecutionPrimitive(
            string name = "execute code",
            MemoryAddress codeExecutionAddress = null,
            Expression<Func<SimulationContext, bool>> constraints = null,
            GetNextViolationDelegate nextViolation = null,
            PrimitiveTransitionSuccessDelegate onSuccess = null
            )
            : base(ExploitationPrimitiveType.ExecuteToExecute, "code_execution", name)
        {
            this.CodeExecutionAddress = codeExecutionAddress;

            this.ConstraintList.Add(
                (context) =>
                    (
                        (context.AttackerFavorsEqual(context.CurrentViolation.Method, MemoryAccessMethod.Execute) == true)

                        &&

                        (context.AttackerFavorsEqual(context.CurrentViolation.Address, this.CodeExecutionAddress) == true)

                        &&

                        (context.CanFindAddress(this.CodeExecutionAddress) == true)

                        &&

                        (context.CanExecuteMemoryAtAddress(this.CodeExecutionAddress) == true)
                    )
                );

            Update(constraints, nextViolation, onSuccess);
        }

        public MemoryAddress CodeExecutionAddress { get; private set; }

        public override string PrimitiveDescriptor
        {
            get { return String.Format("Execute code via {0}", this.Name); }
        }
    }

    #endregion

    #region Generic primitives for transitioning between violations

    public class ReadToReadPrimitive : ReadPrimitive
    {
        public ReadToReadPrimitive(
            MemoryAccessParameter controlledParameter,
            MemoryAddress controlledAddress,
            string name = null,
            Expression<Func<SimulationContext, bool>> constraints = null,
            GetNextViolationDelegate nextViolation = null,
            PrimitiveTransitionSuccessDelegate onSuccess = null
            )
            : base(
                ExploitationPrimitiveType.ReadToRead,
                name: (name != null) ? name : String.Format("read content from '{0}' that is used as '{1}' of read", controlledAddress, controlledParameter),
                readAddress: controlledAddress
                )
        {
            this.ControlledParameter = controlledParameter;
            this.ControlledAddress = controlledAddress;

            this.NextViolationDelegate = (context) =>
            {
                Violation v = context.CurrentViolation.NewTransitiveViolation(
                    MemoryAccessMethod.Read,
                    String.Format("read via content derived from '{0}'", controlledAddress)
                    );

                InheritParameterState(context.CurrentViolation, v);

                context.AttackerFavorsAssumeTrue(AssumptionName.CanTriggerMemoryRead);

                return v;
            };

            Update(constraints, nextViolation, onSuccess);
        }

        public override void InheritParameterState(Violation from, Violation to)
        {
            to.InheritParameterStateFromContent(from, this.ControlledParameter);
        }

        public override string PrimitiveDescriptor
        {
            get { return String.Format("{0} with controlled parameter {1}", this.PrimitiveType, this.ControlledParameter); }
        }

        /// <summary>
        /// The parameter that is controlled.
        /// </summary>
        public MemoryAccessParameter ControlledParameter { get; set; }

        /// <summary>
        /// The memory address that stores the content of the derived parameter.
        /// </summary>
        public MemoryAddress ControlledAddress { get; set; }
    }

    public class ReadToWritePrimitive : ReadPrimitive
    {
        public ReadToWritePrimitive(
            MemoryAccessParameter controlledParameter
            )
            : this(controlledParameter, controlledParameter.GetMemoryAddress(MemoryAccessMethod.Write))
        {
        }

        public ReadToWritePrimitive(
            MemoryAccessParameter controlledParameter,
            MemoryAddress controlledAddress,
            Expression<Func<SimulationContext, bool>> constraints = null,
            GetNextViolationDelegate nextViolation = null,
            PrimitiveTransitionSuccessDelegate onSuccess = null
            )
            : base(
                ExploitationPrimitiveType.ReadToWrite,
                name: String.Format("read content from '{0}' that is used as '{1}' of write", controlledAddress, controlledParameter),
                readAddress: controlledAddress
                )
        {
            this.ControlledParameter = controlledParameter;
            this.ControlledAddress = controlledAddress;

            this.NextViolationDelegate = (context) =>
            {
                Violation v = context.CurrentViolation.NewTransitiveViolation(
                    MemoryAccessMethod.Write,
                    String.Format("write via content derived from '{0}'", controlledAddress)
                    );

                InheritParameterState(context.CurrentViolation, v);

                context.AttackerFavorsAssumeTrue(AssumptionName.CanTriggerMemoryWrite);

                return v;
            };

            Update(constraints, nextViolation, onSuccess);
        }

        public override void InheritParameterState(Violation from, Violation to)
        {
            to.InheritParameterStateFromContent(from, this.ControlledParameter);
        }

        public override string PrimitiveDescriptor
        {
            get { return String.Format("{0} with controlled parameter {1}", this.PrimitiveType, this.ControlledParameter); }
        }

        /// <summary>
        /// The parameter that is controlled.
        /// </summary>
        public MemoryAccessParameter ControlledParameter { get; set; }

        /// <summary>
        /// The memory address that stores the content of the derived parameter.
        /// </summary>
        public MemoryAddress ControlledAddress { get; set; }
    }

    public class ReadToExecutePrimitive : ReadPrimitive
    {
        public ReadToExecutePrimitive(
            MemoryAddress controlTransferPointerAddress = null,
            ControlTransferMethod? controlTransferMethod = null,
            string name = null,
            Expression<Func<SimulationContext, bool>> constraints = null,
            GetNextViolationDelegate nextViolation = null,
            PrimitiveTransitionSuccessDelegate onSuccess = null
            )
            : base(
                ExploitationPrimitiveType.ReadToExecute,
                (name != null) ? name : "read content that is used as base of execute",
                controlTransferPointerAddress
                )
        {
            this.ControlTransferMethod = controlTransferMethod;

            this.NextViolationDelegate = (context) =>
            {
                Violation v = context.CurrentViolation.NewTransitiveViolation(
                    MemoryAccessMethod.Execute,
                    "execute with controlled base",
                    baseState: context.CurrentViolation.ContentSrcState,
                    contentSrcState: MemoryAccessParameterState.Unknown,
                    contentDstState: MemoryAccessParameterState.Nonexistant,
                    displacementState: MemoryAccessParameterState.Nonexistant,
                    extentState: MemoryAccessParameterState.Nonexistant
                    );

                v.InheritParameterStateFromContent(context.CurrentViolation);

                context.AttackerFavorsAssumeTrue(AssumptionName.CanTriggerMemoryExecute);

                return v;
            };

            this.ConstraintList.Add(
                (context) =>
                    (
                        // base verifies that read address is equal to pointer address.

                        //
                        // The current violation must be a read violation that leads to this type
                        // of control transfer.  No constraints are placed on being able to find
                        // desired code here, as this simply describes the constraints of going
                        // from a read to an execute.
                        //

                        (context.AttackerFavorsEqual(context.CurrentViolation.ControlTransferMethod, controlTransferMethod))
                    )
                );

            Update(constraints, nextViolation, onSuccess);
        }

        public override void InheritParameterState(Violation from, Violation to)
        {
            to.InheritParameterStateFromContent(from, MemoryAccessParameter.Base);
        }

        public override string PrimitiveDescriptor
        {
            get { return String.Format("{0} via control transfer method {1}", this.PrimitiveType, this.ControlTransferMethod); }
        }

        public ControlTransferMethod? ControlTransferMethod { get; private set; }
    }

    /// <summary>
    /// Fundamental primitives for transitioning from a write violation to another violation.
    /// </summary>
    public class WriteToReadPrimitive : WritePrimitive
    {
        public WriteToReadPrimitive(
            MemoryAccessParameter corruptedParameter
            )
            : this(corruptedParameter, corruptedParameter.GetMemoryAddress(MemoryAccessMethod.Read))
        {
        }

        public WriteToReadPrimitive(
            MemoryAccessParameter corruptedParameter,
            MemoryAddress writeAddress,
            string name = null,
            Expression<Func<SimulationContext, bool>> constraints = null,
            GetNextViolationDelegate nextViolation = null,
            PrimitiveTransitionSuccessDelegate onSuccess = null
            )
            : base(
                ExploitationPrimitiveType.WriteToRead,
                (name != null) ? name : String.Format("write content to '{0}' that is used as '{1}' of read", writeAddress, corruptedParameter),
                writeAddress
                )
        {
            this.CorruptedParameter = corruptedParameter;

            this.NextViolationDelegate = (context) =>
            {
                Violation v = context.CurrentViolation.NewTransitiveViolation(
                    MemoryAccessMethod.Read,
                    String.Format("read using content derived from '{0}'", this.WriteAddress)
                    );

                InheritParameterState(context.CurrentViolation, v);

                v.Address = this.WriteAddress;

                return v;
            };

            this.OnSuccess += (SimulationContext context, ref Violation v) =>
            {
                context.AttackerFavorsAssumeTrue(AssumptionName.CanTriggerMemoryRead);
            };

            Update(constraints, nextViolation, onSuccess);
        }

        public override void InheritParameterState(Violation from, Violation to)
        {
            to.InheritParameterStateFromContent(from, this.CorruptedParameter);
        }

        public override string PrimitiveDescriptor
        {
            get { return String.Format("{0} with corrupted parameter {1}", this.PrimitiveType, this.CorruptedParameter); }
        }

        /// <summary>
        /// The parameter that is corrupted.
        /// </summary>
        public MemoryAccessParameter CorruptedParameter { get; set; }
    }

    public class WriteToExecutePrimitive : WritePrimitive
    {
        public WriteToExecutePrimitive()
            : base(
                ExploitationPrimitiveType.WriteToExecute,
                name: "write to content of execute",
                writeAddress: MemoryAddress.AddressOfWritableCode
                )
        {
            this.ConstraintList.Add(
                (context) =>
                    (
                        context.AttackerFavorsAssumeTrue(AssumptionName.CanTriggerMemoryExecute)
                    )
                );

            Update(nextViolation: (context) =>
                {
                    Violation v = context.CurrentViolation.NewTransitiveViolation(
                        MemoryAccessMethod.Execute,
                        "execute with corrupted content",
                        baseState: MemoryAccessParameterState.Unknown,
                        contentSrcState: context.CurrentViolation.ContentSrcState,
                        contentDstState: MemoryAccessParameterState.Nonexistant,
                        displacementState: MemoryAccessParameterState.Nonexistant,
                        extentState: MemoryAccessParameterState.Nonexistant
                        );

                    InheritParameterState(context.CurrentViolation, v);

                    return v;
                });
        }

        public override void InheritParameterState(Violation from, Violation to)
        {
            to.InheritParameterStateFromContent(from, MemoryAccessParameter.Content);
        }

        public override string PrimitiveDescriptor
        {
            get { return String.Format("{0} @ {1}", this.PrimitiveType, this.WriteAddress); }
        }
    }

    #endregion
}
