// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSModel
{
    public enum AssumptionName
    {
        Unknown,

        CanTriggerMemoryWrite,
        CanTriggerMemoryRead,
        CanTriggerMemoryExecute,

        CanTriggerFunctionReturn,
        CanTriggerFunctionPointerCall,
        CanTriggerVirtualMethodCall,
        CanTriggerException,

        CanFindAddress,
        CanPositionAtDesiredAbsoluteAddress,
        CanPositionAtDesiredRelativeAddress,
        CanDetermineDisplacementToAddress,

        CanCorruptMemoryAtAddress,
        CanCorruptMemoryAtAddressListComplete, // no other corruptions are possible other than those already assumed.
        CanReadMemoryAtAddress,

        CanExecuteCode,
        CanExecuteData,
        CanExecuteControlledCode,
        CanExecuteDesiredCode,  // attacker can execute their desired payload (whether in controlled code or not)
        CanBypassNX,

        CanInitializeContentViaHeapSpray,
        CanInitializeContentViaStackOverlappingLocal,
        CanInitializeCodeViaJIT,

        /// <summary>
        /// An assumption about an attacker's ability to discover the stack protection cookie value for a function.
        /// </summary>
        CanDetermineStackProtectionCookie,

        CanLoadNonASLRImage,
        CanLoadNonASLRNonSafeSEHImage,

        ApplicationLoadsNonASLRDll,
        ApplicationLoadsNonASLRExe,
        ApplicationLoadsNonSafeSEHDll,
        ApplicationLoadsNonSafeSEHExe,
        ApplicationLoadsNonASLRNonSafeSEHDll,
        ApplicationLoadsNonASLRNonSafeSEHExe,

        ApplicationLoadsDllBelow4GB,
        ApplicationLoadsExeBelow4GB,

        CanBypassSafeSEH,
        CanBypassSEHOP,

        CanFindStackPivotGadget,
        CanFindRequiredROPGadgets, 
        
        CanFindRequiredROPGadgetsInImageCode,
        CanFindRequiredROPGadgetsInJITCode,

        IsROPGadgetImageVersionKnown,
        IsJITEngineVersionKnown,

        CanPivotStackPointer,

        CanProtectDataAsCode,

    }

    public class Assumption
    {
        public static double BooleanProbability(bool tf)
        {
            return (tf == true) ? 1.0 : 0.0;
        }

        public Assumption()
        {
            this.Name = AssumptionName.Unknown;
            this.Probability = 1.0;
        }

        public Assumption(AssumptionName name, double probability = 1.0)
        {
            this.Name = name;
            this.Probability = probability;
        }

        public AssumptionName Name { get; set; }

        /// <summary>
        /// The string version of this assumption name.
        /// </summary>
        /// <remarks>
        /// This is used as a unique key for assumptions.
        /// </remarks>
        public virtual string NameString
        {
            get
            {
                return this.Name.ToString();
            }
        }

        /// <summary>
        /// True if the probabiliy is greater than zero.
        /// </summary>
        public bool IsTrue
        {
            get
            {
                return this.Probability > 0;
            }
            set
            {
                this.Probability = (value == true) ? 1.0 : 0.0;
            }
        }

        /// <summary>
        /// The probability that this assumption is true.
        /// </summary>
        public double Probability { get; set; }

        /// <summary>
        /// The transition the assumption occurred during.
        /// </summary>
        public Transition Transition { get; set; }

        /// <summary>
        /// True if this assumption has been used (e.g. via a call to IsAssumed).
        /// </summary>
        public bool Used { get; set; }

        /// <summary>
        /// The unique (order inducing) identifier for this assumption.
        /// </summary>
        public long ID { get; set; }

        /// <summary>
        /// This assumption was explicitly provided by the user.
        /// </summary>
        public bool Explicit { get; set; }

        public override int GetHashCode()
        {
            return this.NameString.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is Assumption && obj != null && (obj as Assumption).NameString == this.NameString;
        }

        public override string ToString()
        {
            return this.NameString;
        }

        /// <summary>
        /// An assumption regarding an attacker's ability to find an address.
        /// </summary>
        public class CanFindAddress : Assumption
        {
            public CanFindAddress(MemoryAddress address, double probability = 1.0)
                : base(AssumptionName.CanFindAddress, probability)
            {
                this.Address = address;
            }

            public override string NameString
            {
                get
                {
                    return String.Format("{0}({1})", this.Name, this.Address);
                }
            }

            public MemoryAddress Address { get; private set; }
        }

        public class CanPositionAtDesiredAbsoluteAddress : Assumption
        {
            public CanPositionAtDesiredAbsoluteAddress(MemoryAddress address, double probability = 1.0)
                : base(AssumptionName.CanPositionAtDesiredAbsoluteAddress, probability)
            {
                this.Address = address;
            }

            public override string NameString
            {
                get
                {
                    return String.Format("{0}({1})", this.Name, this.Address);
                }
            }

            public MemoryAddress Address { get; private set; }
        }

        public class CanPositionAtDesiredRelativeAddress : Assumption
        {
            public CanPositionAtDesiredRelativeAddress(MemoryAddress address, double probability = 1.0)
                : base(AssumptionName.CanPositionAtDesiredRelativeAddress, probability)
            {
                this.Address = address;
            }

            public override string NameString
            {
                get
                {
                    return String.Format("{0}({1})", this.Name, this.Address);
                }
            }

            public MemoryAddress Address { get; private set; }
        }

        public class CanDetermineDisplacementToAddress : Assumption
        {
            public CanDetermineDisplacementToAddress(MemoryAddress address, double probability = 1.0)
                : base(AssumptionName.CanDetermineDisplacementToAddress, probability)
            {
                this.Address = address;
            }

            public override string NameString
            {
                get
                {
                    return String.Format("{0}({1})", this.Name, this.Address);
                }
            }

            public MemoryAddress Address { get; private set; }
        }

        public class CanCorruptMemoryAtAddress : Assumption
        {
            public CanCorruptMemoryAtAddress(MemoryAddress address, double probability = 1.0)
                : base(AssumptionName.CanCorruptMemoryAtAddress, probability)
            {
                this.Address = address;
            }

            public override string NameString
            {
                get
                {
                    return String.Format("{0}({1})", this.Name, this.Address);
                }
            }

            public MemoryAddress Address { get; private set; }
        }

        public class CanReadMemoryAtAddress : Assumption
        {
            public CanReadMemoryAtAddress(MemoryAddress address, double probability = 1.0)
                : base(AssumptionName.CanReadMemoryAtAddress, probability)
            {
                this.Address = address;
            }

            public override string NameString
            {
                get
                {
                    return String.Format("{0}({1})", this.Name, this.Address);
                }
            }

            public MemoryAddress Address { get; private set; }
        }

        public abstract class FavoredOutcome : Assumption
        {
            public FavoredOutcome(AssumptionName name, string constraintString, bool favoredTrue)
                : base(name)
            {
                this.ConstraintString = constraintString;
                this.FavoredTrue = favoredTrue;
            }

            public string ConstraintString { get; private set; }
            public bool FavoredTrue { get; private set; }

            public override string NameString
            {
                get
                {
                    return String.Format("{0}({1} == '{2}')", this.Name, this.FavoredTrue, this.ConstraintString);
                }
            }
        }
    }
}
