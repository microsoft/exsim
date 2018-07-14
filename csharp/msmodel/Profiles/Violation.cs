// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Text;
using System.IO;
using System.ComponentModel;

using UR.Graphing;

namespace MSModel
{
    /// <summary>
    /// A memory safety violation.
    /// </summary>
    /// <remarks>
    /// A memory safety violation occurs as a result of an unsafe memory access.
    /// </remarks>
    public class Violation : Profile
    {
        public Violation()
        {
            Initialize();
        }

        public Violation(
            MemoryAccessMethod method,
            string name = null,
            MemoryAccessParameterState baseState = MemoryAccessParameterState.Unknown,
            MemoryAccessParameterState contentSrcState = MemoryAccessParameterState.Unknown,
            MemoryAccessParameterState contentDstState = MemoryAccessParameterState.Unknown,
            MemoryAccessParameterState displacementState = MemoryAccessParameterState.Unknown,
            MemoryAccessParameterState extentState = MemoryAccessParameterState.Unknown
            )
        {
            Initialize();

            this.Method = method;
            this.BaseState = baseState;
            this.ContentSrcState = contentSrcState;
            this.ContentDstState = contentDstState;
            this.DisplacementState = displacementState;
            this.ExtentState = extentState;
            this.Name = name;

            if (this.Method == MemoryAccessMethod.Execute)
            {
                this.DisplacementState = MemoryAccessParameterState.Nonexistant;
                this.ExtentState = MemoryAccessParameterState.Nonexistant;
                this.AddressingMode = MemoryAddressingMode.Absolute;
            }
            else if (this.Method == MemoryAccessMethod.Read)
            {
                this.ContentDstState = MemoryAccessParameterState.Nonexistant;
            }

            this.Guid = Guid.NewGuid();
        }

        public Violation(MemoryAccessParameterState defaultParameterState)
        {
            this.DefaultParameterState = defaultParameterState;

            Initialize();
        }

        protected Violation(XElement element, Profile parent)
            : base(element, parent)
        {
        }

        protected override void Initialize()
        {
            this.BaseState = this.DefaultParameterState;
            this.ContentSrcState = this.DefaultParameterState;
            this.ContentDstState = this.DefaultParameterState;
            this.DisplacementState = this.DefaultParameterState;
            this.ExtentState = this.DefaultParameterState;

            this.ChildViolationList = new List<Violation>();
            this.TransitiveViolations = new List<TransitiveViolation>();
            this.Assumptions = new HashSet<Assumption>();

            this.InheritDefaults();

            this.RecalibrationEvent += (target) =>
                {
                    //
                    // Inherit stack protection entropy bits and version.
                    //

                    if (target.Violation.FunctionStackProtectionEnabled == true)
                    {
                        target.Violation.FunctionStackProtectionEntropyBits = target.Application.DefaultStackProtectionEntropyBits;
                        target.Violation.FunctionStackProtectionVersion = target.Application.DefaultStackProtectionVersion;
                    }
                };
        }

        public override void FromXml(XElement element, Profile parent)
        {
            base.FromXml(element, parent);

            this.ChildViolationList =
                new List<Violation>(
                    from XElement e in element.Elements("Violation")
                    select new Violation(e, this)
                    );
        }

        public Violation CloneViolation()
        {
            Violation clone = this.Clone() as Violation;
            clone.Guid = Guid.NewGuid();
            clone.ChildViolationList = new List<Violation>(this.ChildViolationList);
            clone.Assumptions = new HashSet<Assumption>(this.Assumptions);
            clone.TransitiveViolations = new List<TransitiveViolation>();
            return clone;
        }

        public Violation NewTransitiveViolation(
            MemoryAccessMethod method,
            string name = null,
            MemoryAccessParameterState baseState = MemoryAccessParameterState.Unknown,
            MemoryAccessParameterState contentSrcState = MemoryAccessParameterState.Unknown,
            MemoryAccessParameterState contentDstState = MemoryAccessParameterState.Unknown,
            MemoryAccessParameterState displacementState = MemoryAccessParameterState.Unknown,
            MemoryAccessParameterState extentState = MemoryAccessParameterState.Unknown
            )
        {
            Violation v = new Violation(method, name, baseState, contentSrcState, contentDstState, displacementState, extentState);

            v.PreviousViolationObject = this;

            v.AccessRequirement = this.AccessRequirement;
            v.ExecutionDomain = this.ExecutionDomain;
            v.Locality = this.Locality;

            //
            // Inherit the function's stack protection settings by default.
            //

            v.FunctionStackProtectionEnabled = this.FunctionStackProtectionEnabled;
            v.FunctionStackProtectionEntropyBits = this.FunctionStackProtectionEntropyBits;
            v.FunctionStackProtectionVersion = this.FunctionStackProtectionVersion;

            return v;
        }

        /// <summary>
        /// The type of model this is.
        /// </summary>
        public override ModelType ModelType
        {
            get { return MSModel.ModelType.Violation; }
        }

        public override int GetHashCode()
        {
            return this.EquivalenceClass.GetHashCode();
        }

        /// <summary>
        /// Child elements.
        /// </summary>
        [Browsable(false), XmlIgnore]
        public override IEnumerable<Profile> Children
        {
            get { return this.ChildViolationList; }
        }

        private List<Violation> ChildViolationList { get; set; }

        public void AddTransitiveViolation(Violation violation, Transition transition = null)
        {
            this.TransitiveViolations.Add(
                new TransitiveViolation()
                {
                    Violation = violation,
                    TransitionDescriptor = new TransitionDescriptor(transition)
                });
        }

        /// <summary>
        /// The symbolic name for this violation.
        /// </summary>
        [
         Category("General"),
         Description("A symbolic name for the profile."),
         ReadOnly(true)
        ]
        public override string Symbol
        {
            get
            {
                if (this.symbol != null)
                {
                    return this.symbol;
                }

                StringBuilder builder = new StringBuilder();

                builder.AppendFormat("{0}", this.Method.GetAbbreviation());

                if (this.BaseState != MemoryAccessParameterState.Nonexistant)
                {
                    builder.AppendFormat("-b{0}", this.BaseState.GetAbbreviation());
                }

                if (this.ContentSrcState != MemoryAccessParameterState.Nonexistant)
                {
                    builder.AppendFormat("-c{0}", this.ContentSrcState.GetAbbreviation());
                }

                if (this.DisplacementState != MemoryAccessParameterState.Nonexistant)
                {
                    builder.AppendFormat("-d{0}", this.DisplacementState.GetAbbreviation());
                }

                if (this.ExtentState != MemoryAccessParameterState.Nonexistant)
                {
                    builder.AppendFormat("-e{0}", this.ExtentState.GetAbbreviation());
                }

                return builder.ToString();
            }
            set { this.symbol = value; }
        }
        private string symbol;

        public override string ToString()
        {
            return this.Symbol;
        }

        /// <summary>
        /// Violations that can be directly enabled by this violation.
        /// </summary>
        [
         Browsable(false),
         XmlArray("TransitiveViolations"),
         XmlArrayItem("TransitiveViolation")
        ]
        public List<TransitiveViolation> TransitiveViolations { get; private set; }

        /// <summary>
        /// All violations that are transitively reachable from this violation.
        /// </summary>
        /// <remarks>
        /// Does not protect against cycles.
        /// </remarks>
        [ 
         Browsable(false),
         XmlIgnore
        ]
        public IEnumerable<TransitiveViolation> AllTransitiveViolations
        {
            get
            {
                List<TransitiveViolation> all = new List<TransitiveViolation>(this.TransitiveViolations);

                foreach (TransitiveViolation v in this.TransitiveViolations)
                {
                    all.AddRange(v.Violation.TransitiveViolations);
                }

                return all;
            }
        }

        [
         Browsable(false),
         XmlIgnore
        ]
        public IEnumerable<TransitiveViolation> TransitiveExecuteViolations
        {
            get
            {
                return this.TransitiveViolations.Where(x => x.Violation.Method == MemoryAccessMethod.Execute);
            }
        }

        [
         Browsable(false),
         XmlIgnore
        ]
        public IEnumerable<TransitiveViolation> TransitiveReadViolations
        {
            get
            {
                return this.TransitiveViolations.Where(x => x.Violation.Method == MemoryAccessMethod.Read);
            }
        }

        [
         Browsable(false),
         XmlIgnore
        ]
        public IEnumerable<TransitiveViolation> TransitiveWriteViolations
        {
            get
            {
                return this.TransitiveViolations.Where(x => x.Violation.Method == MemoryAccessMethod.Write);
            }
        }

        /// <summary>
        /// The previous violation in a chain of violations.
        /// </summary>
        [
         Browsable(false),
         XmlIgnore
        ]
        public object PreviousViolationObject { get; set; }

        /// <summary>
        /// Inherits property values from the provided flaw.
        /// </summary>
        /// <param name="flaw">The flaw to inherit from.</param>
        public void InheritFromFlaw(Flaw flaw)
        {
            var defaults =
                from ProfilePropertyInfo flawProperty in flaw.Properties
                from ProfilePropertyInfo violationProperty in this.Properties
                where violationProperty.Name == flawProperty.Name
                where flawProperty.ValueString != null
                select new { DefaultValue = flawProperty.ValueString, ViolationProperty = violationProperty };

            foreach (var def in defaults)
            {
                def.ViolationProperty.Set(def.DefaultValue);
            }
        }

        private MemoryAccessParameterState DefaultParameterState { get; set; }

        #region Method and parameter properties

        /// <summary>
        /// The memory access method of the violation (read, write, or execute).
        /// </summary>
        [
         ProfileProperty,
         Category("Memory Access"),
         Description("The method by which memory is being accessed unsafely."),
         ReadOnly(true),
         XmlElement
        ]
        public MemoryAccessMethod Method { get; set; }

        [
         ProfileProperty,
         Category("Memory Access"),
         DisplayName("Base"),
         Description("The state of the base parameter that is used when accessing memory."),
         XmlElement
        ]
        public MemoryAccessParameterState BaseState { get; set; }

        /// <summary>
        /// The region type associated with the base address memory access parameter (stack, heap, etc).
        /// </summary>
        [
         ProfileProperty,
         Category("Memory Access"),
         Description("The memory region that the base parameter's value is within."),
         RefreshProperties(RefreshProperties.All),
         XmlElement
        ]
        public MemoryRegionType? BaseRegionType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// For memory writes, the content state refers to the value being written, not the content
        /// state of the destination memory address.
        /// 
        /// ContentDataType, however, refers to the type of the value that is stored in the destination
        /// memory address prior to the write.
        /// </remarks>
        [
         ProfileProperty,
         Category("Memory Access"),
         DisplayName("Content (src)"),
         Description("The state of the content that is being read from or written to memory."),
         RefreshProperties(RefreshProperties.All),
         XmlElement
        ]
        public MemoryAccessParameterState ContentSrcState { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Only applies to write violations.
        /// </remarks>
        [
         ProfileProperty,
         Category("Memory Access"),
         DisplayName("Content (dest)"),
         Description("Only applies to writes.  The state of the content that is stored in the destination memory address prior to a memory write."),
         RefreshProperties(RefreshProperties.All),
         XmlElement
        ]
        public MemoryAccessParameterState ContentDstState { get; set; }

        [
         ProfileProperty,
         Category("Memory Access"),
         Description("The data type that the memory content is being accessed as."),
         XmlElement
        ]
        public MemoryContentDataType? ContentDataType { get; set; }

        [
         ProfileProperty,
         Category("Memory Access"),
         Description("The name of the data type that the memory content is being accessed as."),
         XmlElement
        ]
        public string ContentDataTypeName { get; set; }

        [
         ProfileProperty,
         Category("Memory Access"),
         Description("The data type for the container of the memory content is being accessed as."),
         XmlElement
        ]
        public MemoryContentDataType? ContentContainerDataType { get; set; }

        [
         ProfileProperty,
         Category("Memory Access"),
         Description("The name of the data type for the container of the memory content is being accessed as."),
         XmlElement
        ]
        public string ContentContainerTypeName { get; set; }

        [
         ProfileProperty,
         Category("Memory Access"),
         DisplayName("Displacement"),
         Description("The state of the displacement parameter that is used when accessing memory."),
         RefreshProperties(RefreshProperties.All),
         XmlElement
        ]
        public MemoryAccessParameterState DisplacementState { get; set; }

        /// <summary>
        /// The initial displacement at which a memory access starts relative to some object.
        /// </summary>
        [
         ProfileProperty,
         Category("Memory Access"),
         Description("The initial offset of the displacement relative to the base object."),
         XmlElement
        ]
        public MemoryAccessOffset? DisplacementInitialOffset { get; set; }

        [
         ProfileProperty,
         Category("Memory Access"),
         DisplayName("Extent"),
         Description("The state of the extent parameter (number of bytes) for the memory access."),
         RefreshProperties(RefreshProperties.All),
         XmlElement
        ]
        public MemoryAccessParameterState ExtentState { get; set; }

        public void SetParameterState(MemoryAccessParameter parameter, MemoryAccessParameterState state)
        {
            switch (parameter)
            {
                case MemoryAccessParameter.Base:
                    this.BaseState = state;
                    break;
                case MemoryAccessParameter.Content:
                    this.ContentSrcState = state;
                    break;
                case MemoryAccessParameter.Displacement:
                    this.DisplacementState = state;
                    break;
                case MemoryAccessParameter.Extent:
                    this.ExtentState = state;
                    break;
                default:
                    break;
            }
        }

        public void InheritParameterStateFromContent(Violation from, params MemoryAccessParameter[] parameters)
        {
            foreach (MemoryAccessParameter parameter in parameters)
            {
                switch (parameter)
                {
                    case MemoryAccessParameter.Base:
                        this.BaseState = from.ContentSrcState;
                        break;

                    case MemoryAccessParameter.Content:
                        this.ContentSrcState = from.ContentSrcState;
                        break;

                    case MemoryAccessParameter.Displacement:
                        this.DisplacementState = from.ContentSrcState;
                        break;

                    case MemoryAccessParameter.Extent:
                        this.ExtentState = from.ContentSrcState;
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }

            if ((parameters.Contains(MemoryAccessParameter.Base)) ||
                (this.Method == MemoryAccessMethod.Execute))
            {
                this.AddressingMode = MemoryAddressingMode.Absolute;
            }
            else
            {
                this.AddressingMode = MemoryAddressingMode.Relative;
            }
        }

        [Browsable(false), XmlIgnore]
        public bool IsBaseControlled { get { return this.BaseState == MemoryAccessParameterState.Controlled; } }
        [Browsable(false), XmlIgnore]
        public bool IsBaseFixed { get { return this.BaseState == MemoryAccessParameterState.Fixed; } }
        [Browsable(false), XmlIgnore]
        public bool IsBaseUninitialized { get { return this.BaseState == MemoryAccessParameterState.Uninitialized; } }
        [Browsable(false), XmlIgnore]
        public bool IsBaseUnknown { get { return this.BaseState == MemoryAccessParameterState.Unknown; } }

        [Browsable(false), XmlIgnore]
        public bool IsContentControlled { get { return this.ContentSrcState == MemoryAccessParameterState.Controlled; } }
        [Browsable(false), XmlIgnore]
        public bool IsContentFixed { get { return this.ContentSrcState == MemoryAccessParameterState.Fixed; } }
        [Browsable(false), XmlIgnore]
        public bool IsContentUninitialized { get { return this.ContentSrcState == MemoryAccessParameterState.Uninitialized; } }
        [Browsable(false), XmlIgnore]
        public bool IsContentUnknown { get { return this.ContentSrcState == MemoryAccessParameterState.Unknown; } }

        [Browsable(false), XmlIgnore]
        public bool IsDisplacementControlled { get { return this.DisplacementState == MemoryAccessParameterState.Controlled; } }
        [Browsable(false), XmlIgnore]
        public bool IsDisplacementFixed { get { return this.DisplacementState == MemoryAccessParameterState.Fixed; } }
        [Browsable(false), XmlIgnore]
        public bool IsDisplacementUninitialized { get { return this.DisplacementState == MemoryAccessParameterState.Uninitialized; } }
        [Browsable(false), XmlIgnore]
        public bool IsDisplacementUnknown { get { return this.DisplacementState == MemoryAccessParameterState.Unknown; } }

        [Browsable(false), XmlIgnore]
        public bool IsExtentControlled { get { return this.ExtentState == MemoryAccessParameterState.Controlled; } }
        [Browsable(false), XmlIgnore]
        public bool IsExtentFixed { get { return this.ExtentState == MemoryAccessParameterState.Fixed; } }
        [Browsable(false), XmlIgnore]
        public bool IsExtentUninitialized { get { return this.ExtentState == MemoryAccessParameterState.Uninitialized; } }
        [Browsable(false), XmlIgnore]
        public bool IsExtentUnknown { get { return this.ExtentState == MemoryAccessParameterState.Unknown; } }

        /// <summary>
        /// The addressing mode of the memory access (absolute or relative).
        /// </summary>
        /// <remarks>
        /// An absolute addressing mode is used when there is no displacement or the displacement & extent are a constant.
        /// 
        /// A relative addressing mode is used if the displacement or extent are variable.
        /// </remarks>
        [
         ProfileProperty,
         Category("Memory Access"),
         Description("The addressing mode of the memory access (relative to a base, or absolute).  Only applies to read or write violations."),
         XmlElement
        ]
        public MemoryAddressingMode? AddressingMode { get; set; }

        /// <summary>
        /// The direction in which memory is being accessed (forward or reverse).
        /// </summary>
        [
         ProfileProperty,
         Category("Memory Access"),
         Description("The direction that memory is being accessed in.  Only applies to read or write violations."),
         XmlElement
        ]
        public MemoryAccessDirection? Direction { get; set; }

        /// <summary>
        /// For execute violations, this describes the method through which control is being transferred (function call, virtual method call, function return, etc).
        /// </summary>
        [
         ProfileProperty,
         Category("Memory Access"),
         Description("The method through which execution control is transferred.  Only applies to execute violations."),
         XmlElement
        ]
        public ControlTransferMethod? ControlTransferMethod { get; set; }

        /// <summary>
        /// The type of address that is being accessed.
        /// </summary>
        [
         ProfileProperty,
         Category("Memory Access"),
         Description("The memory address that is being accessed."),
         XmlIgnore
        ]
        public MemoryAddress Address
        {
            get
            {
                if (this.ContentDataType == null)
                {
                    return null;
                }
                else
                {
                    return new MemoryAddress(this.ContentDataType.Value, this.BaseRegionType);
                }
            }
            set
            {
                if (value == null)
                {
                    this.ContentDataType = null;
                    this.BaseRegionType = null;
                }
                else
                {
                    this.ContentDataType = value.DataType;
                    this.BaseRegionType = value.Region;
                }
            }
        }

        #endregion

        #region Vector properties

        [
         ProfileProperty,
         Category("Vector"),
         Description("The location the flaw can be triggered from."),
         XmlElement
        ]
        public Locality? Locality
        {
            get { return this.locality; }
            set { this.locality = value; }
        }
        private Locality? locality = MSModel.Locality.Unspecified;

        [
         ProfileProperty,
         Category("Vector"),
         Description("The minimum level of access required for the flaw to be triggered."),
         XmlElement
        ]
        public AccessRequirement? AccessRequirement
        {
            get { return this.accessRequirement; }
            set { this.accessRequirement = value; }
        }
        private AccessRequirement? accessRequirement = MSModel.AccessRequirement.Unspecified;

        [
         ProfileProperty,
         Category("Vector"),
         Description("The processor execution domain in which the flaw is triggered."),
         XmlElement
        ]
        public ExecutionDomain? ExecutionDomain
        {
            get { return this.executionDomain; }
            set { this.executionDomain = value; }
        }
        private ExecutionDomain? executionDomain = MSModel.ExecutionDomain.Unspecified;

        #endregion

        #region Location properties
        [
         ProfileProperty,
         Category("Location"),
         XmlElement
        ]
        public string CrashDumpFile { get; set; }

        [
         ProfileProperty,
         Category("Location"),
         XmlElement
        ]
        public string SourceFile { get; set; }

        [
         ProfileProperty,
         Category("Location"),
         XmlElement
        ]
        public string SourceLines { get; set; }

        [
         ProfileProperty,
         Category("Location"),
         XmlElement
        ]
        public string TraceFile { get; set; }

        [
         ProfileProperty,
         Category("Location"),
         XmlElement
        ]
        public string TracePositions { get; set; }
        #endregion

        #region Contextual properties

        /// <summary>
        /// True if stack protection is enabled for the function in which the violation occurs.
        /// </summary>
        [
         ProfileProperty,
         Category("Function"),
         DisplayName("StackProtectionEnabled"),
         Description("Stack protection (/GS) status for the function in which the violation is triggered."),
         XmlElement
        ]
        public bool? FunctionStackProtectionEnabled { get; set; }

        /// <summary>
        /// The version of the stack protection that has been enabled for the function in which the violation occurs.
        /// </summary>
        [
         ProfileProperty,
         Category("Function"),
         DisplayName("StackProtectionVersion"),
         Description("The version of the stack protection for the function in which the violation is triggered."),
         XmlElement
        ]
        public StackProtectionVersion? FunctionStackProtectionVersion { get; set; }

        /// <summary>
        /// The amount of entropy present in the stack protection cookie for the function.
        /// </summary>
        [
         ProfileProperty,
         Category("Function"),
         DisplayName("StackProtectionEntropyBits"),
         Description("The amount of entropy (in bits) for the stack protection's secret cookie."),
         XmlElement
        ]
        public uint? FunctionStackProtectionEntropyBits { get; set; }

        #endregion

        #region Assumptions

        [
         ProfileProperty,
         Category("Assumptions"),
         Description("Whether this violation directly leads to an execution violation."),
         XmlElement
        ]
        public bool? TransitiveExecuteListIsComplete { get; set; }

        [
         ProfileProperty,
         Category("Assumptions"),
         Description("Whether this violation directly leads to an execution violation."),
         XmlElement
        ]
        public bool? TransitiveReadListIsComplete { get; set; }

        [
         ProfileProperty,
         Category("Assumptions"),
         Description("Whether this violation directly leads to an execution violation."),
         XmlElement
        ]
        public bool? TransitiveWriteListIsComplete { get; set; }

        public void SetTransitiveViolationComplete(MemoryAccessMethod method, bool complete)
        {
            switch (method)
            {
                case MemoryAccessMethod.Read:
                    this.TransitiveReadListIsComplete = complete;
                    break;

                case MemoryAccessMethod.Write:
                    this.TransitiveWriteListIsComplete = complete;
                    break;

                case MemoryAccessMethod.Execute:
                    this.TransitiveExecuteListIsComplete = complete;
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        public bool IsTransitiveViolationComplete(MemoryAccessMethod method)
        {
            switch (method)
            {
                case MemoryAccessMethod.Read:
                    return this.TransitiveReadListIsComplete.GetValueOrDefault();

                case MemoryAccessMethod.Write:
                    return this.TransitiveWriteListIsComplete.GetValueOrDefault();

                case MemoryAccessMethod.Execute:
                    return this.TransitiveExecuteListIsComplete.GetValueOrDefault();

                default:
                    throw new NotSupportedException();
            }
        }

        // default assumptions

        [XmlIgnore]
        public HashSet<Assumption> Assumptions { get; set; }

        #endregion

        /// <summary>
        /// Checks if the supplied violation is the same as this one base on the parameters of the violation.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public bool IsSameAs(Violation v)
        {
            return this.EquivalenceClass == v.EquivalenceClass;
        }

        /// <summary>
        /// The equivalence class hash that this violation belongs to.
        /// </summary>
        /// <remarks>
        /// The equivalence class for a violation are determined by the properties
        /// of a violation that influence that set of subsequent violations that
        /// may be directly reached.
        /// </remarks>
        [
         Browsable(false),
         XmlElement
        ]
        public string EquivalenceClass
        {
            get
            {
                return ComputeSHA1(
                    this.Address,
                    this.AddressingMode,
                    this.BaseRegionType,
                    this.BaseState,
                    this.ContentContainerDataType,
                    this.ContentContainerTypeName,
                    this.ContentDataType,
                    this.ContentDataTypeName,
                    this.ContentSrcState,
                    this.ContentDstState,
                    this.ControlTransferMethod,
                    this.DisplacementState,
                    this.DisplacementInitialOffset,
                    this.ExecutionDomain,
                    this.ExtentState,
                    this.Locality,
                    this.Method,
                    this.FunctionStackProtectionEnabled
                    );
            }
        }

        public bool HasPreviousViolation(Violation v)
        {
            Violation current = this;

            while (current != null)
            {
                if (current.IsSameAs(v))
                {
                    return true;
                }

                current = current.PreviousViolationObject as Violation;
            }

            return false;
        }
    }

    public class TransitiveViolation
    {
        /// <summary>
        /// The violation that is transitively led to.
        /// </summary>
        [XmlElement("Violation")]
        public Violation Violation { get; set; }

        /// <summary>
        /// The transition that led to this violation.  May be null.
        /// </summary>
        [XmlIgnore]
        public TransitionDescriptor TransitionDescriptor { get; set; }
    }

    public class ViolationModel : Model<Violation>
    {
        public ViolationModel()
        {
            GenerateBaseViolations();
        }

        public ViolationModel(Stream stream) : base(stream)
        {
        }

        private void GenerateBaseViolations()
        {
            //
            // Generate base violation profiles for each memory access method.
            //

            this.Profiles = new List<Violation>();

            GenerateViolations(
                MemoryAccessMethod.Read,
                new[] { MemoryAccessParameter.Base, MemoryAccessParameter.Content, MemoryAccessParameter.Displacement, MemoryAccessParameter.Extent },
                new [] { MemoryAccessParameterState.Controlled, MemoryAccessParameterState.Fixed, MemoryAccessParameterState.Uninitialized, MemoryAccessParameterState.Unknown }
                );

            GenerateViolations(
                MemoryAccessMethod.Write,
                new[] { MemoryAccessParameter.Base, MemoryAccessParameter.Content, MemoryAccessParameter.Displacement, MemoryAccessParameter.Extent },
                new[] { MemoryAccessParameterState.Controlled, MemoryAccessParameterState.Fixed, MemoryAccessParameterState.Uninitialized, MemoryAccessParameterState.Unknown }
                );

            GenerateViolations(
                MemoryAccessMethod.Execute,
                new[] { MemoryAccessParameter.Base, MemoryAccessParameter.Content },
                new[] { MemoryAccessParameterState.Controlled, MemoryAccessParameterState.Fixed, MemoryAccessParameterState.Uninitialized, MemoryAccessParameterState.Unknown }
                );
        }

        /// <summary>
        /// Generates base violation profiles for a given method, set of parameters, and set of parameter states.
        /// </summary>
        /// <param name="method">The memory access method for the profiles.</param>
        /// <param name="parameters">The set of parameters.</param>
        /// <param name="parameterStates">The set of parameter states.</param>
        private void GenerateViolations(
            MemoryAccessMethod method, 
            IEnumerable<MemoryAccessParameter> parameters,
            IEnumerable<MemoryAccessParameterState> parameterStates
            )
        {
            int statesCount = parameterStates.Count();

            int permutationCount = (int)Math.Pow(statesCount, parameters.Count());

            int stateBits = (int)Math.Round(Math.Log(statesCount, 2));

            int stateMask = (int)Math.Pow(2, stateBits) - 1;
            
            for (int permutation = 0; permutation < permutationCount; permutation++)
            {
                int pindex = 0;

                Violation violation = new Violation()
                {
                    Method = method
                };

                foreach (MemoryAccessParameter p in Enum.GetValues(typeof(MemoryAccessParameter)))
                {
                    violation.SetParameterState(p, MemoryAccessParameterState.Nonexistant);
                }
                
                foreach (MemoryAccessParameter p in parameters)
                {
                    int s = (permutation >> (pindex * stateBits)) & stateMask;

                    if (s >= statesCount)
                    {
                        break;
                    }

                    violation.SetParameterState(p, parameterStates.ElementAt(s));

                    pindex++;
                }

                if (pindex != parameters.Count())
                {
                    continue;
                }

                this.Profiles.Add(violation);
            }
        }

        public IEnumerable<Violation> Violations
        {
            get
            {
                return this.Profiles;
            }
        }

        public IEnumerable<Violation> Writes
        {
            get
            {
                return this.Violations.Where(x => x.Method == MemoryAccessMethod.Write);
            }
        }

        public IEnumerable<Violation> Reads
        {
            get
            {
                return this.Violations.Where(x => x.Method == MemoryAccessMethod.Read);
            }
        }
    }

    public static class ViolationExtension
    {
        public static void InheritDefaults(this Violation violation)
        {
            violation.RecalibrationEvent += (target) =>
            {
                //
                // Inherit execution domain from the target application.
                //

                if (target.Violation.ExecutionDomain == null)
                {
                    if (target.Application.KernelApplication == true)
                    {
                        target.Violation.ExecutionDomain = MSModel.ExecutionDomain.Kernel;
                    }
                    else
                    {
                        target.Violation.ExecutionDomain = MSModel.ExecutionDomain.User;
                    }
                }

                //
                // Inherit stack protection settings from the target application.
                //

                if (target.Violation.FunctionStackProtectionEnabled == null)
                {
                    target.Violation.FunctionStackProtectionEnabled = target.Application.DefaultStackProtectionEnabled;
                    target.Violation.FunctionStackProtectionVersion = target.Application.DefaultStackProtectionVersion;
                    target.Violation.FunctionStackProtectionEntropyBits = target.Application.DefaultStackProtectionEntropyBits;
                }
            };
        }
    }
}
