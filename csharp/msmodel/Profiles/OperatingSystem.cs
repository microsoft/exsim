// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace MSModel
{
    /// <summary>
    /// A model of operating system profiles.
    /// </summary>
    public class OperatingSystemModel : Model<OperatingSystem>
    {
        /// <summary>
        /// Default initializer.
        /// </summary>
        public OperatingSystemModel()
        {
        }

        /// <summary>
        /// Constructs an operating system using profiles from the supplied stream.
        /// </summary>
        /// <param name="stream">The strema to deserialize profiles from.</param>
        public OperatingSystemModel(Stream stream)
            : base(stream)
        {
        }

        /// <summary>
        /// Creates an instance of an operating system profile given a provided XML element.
        /// </summary>
        /// <param name="element">The XML element to deserialize.</param>
        /// <param name="parent">The parent operating system profile (if any).</param>
        /// <returns>An operating system profile.</returns>
        protected override OperatingSystem CreateProfileInstance(XElement element, OperatingSystem parent)
        {
            return Profile.CreateInstance<OperatingSystem>(element, parent);
        }

        /// <summary>
        /// The collection of operating system profiles in the model.
        /// </summary>
        public IEnumerable<OperatingSystem> OperatingSystems
        {
            get
            {
                return this.Profiles;
            }
        }
    }

    /// <summary>
    /// An operating system profile.
    /// </summary>
    public class OperatingSystem : Profile
    {
        /// <summary>
        /// Default initializer.
        /// </summary>
        public OperatingSystem()
        {
            this.MemoryRegionNXPolicy = new ProfilePropertyDictionary<MemoryRegion, MitigationPolicy>();
            this.MemoryRegionASLRPolicy = new ProfilePropertyDictionary<MemoryRegion, MitigationPolicy>();
            this.MemoryRegionASLREntropyBits = new ProfilePropertyDictionary<MemoryRegion, uint>();
            this.UserHeapPolicy = new ProfilePropertyDictionary<HeapFeature, MitigationPolicy>();
            this.Features = new HashSet<FeatureSet>();

            this.InheritDefaults();
        }

        /// <summary>
        /// Initializes this profile using the provided XML element.
        /// </summary>
        /// <param name="element">The XML element to deserialize.</param>
        /// <param name="parent">The parent profile (if any).</param>
        public override void FromXml(XElement element, Profile parent)
        {
            base.FromXml(element, parent);

            this.ChildOperatingSystems =
                new List<OperatingSystem>(
                    from XElement e in element.Elements("OperatingSystem")
                    select CreateInstance(e, this)
                    );
        }

        public override object Clone()
        {
            OperatingSystem os = base.Clone() as OperatingSystem;

            os.MemoryRegionNXPolicy = new ProfilePropertyDictionary<MemoryRegion, MitigationPolicy>(this.MemoryRegionNXPolicy);
            os.MemoryRegionASLRPolicy = new ProfilePropertyDictionary<MemoryRegion, MitigationPolicy>(this.MemoryRegionASLRPolicy);
            os.MemoryRegionASLREntropyBits = new ProfilePropertyDictionary<MemoryRegion, uint>(this.MemoryRegionASLREntropyBits);
            os.UserHeapPolicy = new ProfilePropertyDictionary<HeapFeature, MitigationPolicy>(this.UserHeapPolicy);

            return os;
        }

        public override ModelType ModelType
        {
            get { return MSModel.ModelType.OperatingSystem; }
        }

        /// <summary>
        /// Child operating system profiles.
        /// </summary>
        public override IEnumerable<Profile> Children
        {
            get { return this.ChildOperatingSystems; }
        }

        /// <summary>
        /// Child operating system profiles.
        /// </summary>
        public IEnumerable<OperatingSystem> ChildOperatingSystems { get; set; }

        /// <summary>
        /// True if this operating system is compatible with the provided hardware.
        /// </summary>
        /// <param name="hardware">The hardware to test.</param>
        /// <returns>True if this operating system is compatible.</returns>
        public virtual bool IsCompatibleWith(Hardware hardware)
        {
            return true;
        }

        /// <summary>
        /// The feature sets supported by this operating system.
        /// </summary>
        public HashSet<FeatureSet> Features { get; set; }

        /// <summary>
        /// The number of virtual address bits supported by the operating system.
        /// </summary>
        [ProfileProperty]
        public uint? AddressBits { get; set; }

        /// <summary>
        /// Memory region NX policies.
        /// </summary>
        [ProfileProperty]
        public ProfilePropertyDictionary<MemoryRegion, MitigationPolicy> MemoryRegionNXPolicy { get; set; }

        /// <summary>
        /// Memory region ASLR policies.
        /// </summary>
        [ProfileProperty]
        public ProfilePropertyDictionary<MemoryRegion, MitigationPolicy> MemoryRegionASLRPolicy { get; set; }

        /// <summary>
        /// Memory region ASLR entropy bits.
        /// </summary>
        [ProfileProperty]
        public ProfilePropertyDictionary<MemoryRegion, uint> MemoryRegionASLREntropyBits { get; set; }

        /// <summary>
        /// Default stack protection enablement status for applications that run on this operating system.
        /// </summary>
        [ProfileProperty]
        public bool? DefaultStackProtectionEnabled { get; set; }

        /// <summary>
        /// Default stack protection version for applications that enable stack protection.
        /// </summary>
        [ProfileProperty]
        public StackProtectionVersion DefaultStackProtectionVersion { get; set; }

        /// <summary>
        /// Default amount of entropy bits for stack protection cookies.
        /// </summary>
        [ProfileProperty]
        public uint? DefaultStackProtectionEntropyBits { get; set; }

        #region Kernel mode properties

        /// <summary>
        /// Kernel supervisor mode execution prevention policy.
        /// </summary>
        [ProfileProperty]
        public MitigationPolicy? KernelSMEPPolicy { get; set; }

        /// <summary>
        /// Kernel NULL dereference prevention policy.
        /// </summary>
        [ProfileProperty]
        public MitigationPolicy? KernelNullDereferencePreventionPolicy { get; set; }

        #endregion


        #region User mode properties

        /// <summary>
        /// User mode heap mitigation policies.
        /// </summary>
        [ProfileProperty]
        public ProfilePropertyDictionary<HeapFeature, MitigationPolicy> UserHeapPolicy { get; set; }

        #endregion

        public enum FeatureSet
        {
            WindowsXPSP2,
            WindowsVistaRTM,
            WindowsVistaSP1,
            Windows7,
            Windows8
        }

        /// <summary>
        /// A Windows operating system.
        /// </summary>
        public class Windows : OperatingSystem
        {
            /// <summary>
            /// Default initializer.
            /// </summary>
            public Windows()
            {
                this.KernelPoolPolicies = new ProfilePropertyDictionary<HeapFeature, MitigationPolicy>();

                this.InheritWindowsDefaults();
            }

            #region kernel

            /// <summary>
            /// Kernel mode pool mitigation policies.
            /// </summary>
            [ProfileProperty]
            public ProfilePropertyDictionary<HeapFeature, MitigationPolicy> KernelPoolPolicies { get; set; }

            #endregion

            #region user

            /// <summary>
            /// User mode SEHOP policy.
            /// </summary>
            [ProfileProperty]
            public MitigationPolicy? UserSEHOPPolicy { get; set; }

            /// <summary>
            /// User mode Safe SEH policy.
            /// </summary>
            [ProfileProperty]
            public MitigationPolicy? UserSafeSEHPolicy { get; set; }

            /// <summary>
            /// User mode high entropy bottom up randomization ASLR policy.
            /// </summary>
            [ProfileProperty]
            public MitigationPolicy? UserASLRPolicyBottomUpHighEntropy { get; set; }

            /// <summary>
            /// User mode heap support for encoding the unhandled exception filter pointer.
            /// </summary>
            [ProfileProperty]
            public bool? UserPointerEncodingUEF { get; set; }

            /// <summary>
            /// User mode heap support for encoding the PEB fast lock routine.
            /// </summary>
            [ProfileProperty]
            public bool? UserPointerEncodingPEBFastLockRoutine { get; set; }

            /// <summary>
            /// User mode heap support for encoding the commint routine in the heap.
            /// </summary>
            [ProfileProperty]
            public bool? UserPointerEncodingHeapCommitRoutine { get; set; }

            #endregion

            public void RefreshBottomUpEntropyBits()
            {
                this.MemoryRegionASLREntropyBits[MemoryRegion.UserForceRelocatedImageBase] = this.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocBU];
                this.MemoryRegionASLREntropyBits[MemoryRegion.UserForceRelocatedImageCode] = this.MemoryRegionASLREntropyBits[MemoryRegion.UserForceRelocatedImageBase];
                this.MemoryRegionASLREntropyBits[MemoryRegion.UserForceRelocatedImageData] = this.MemoryRegionASLREntropyBits[MemoryRegion.UserForceRelocatedImageBase];

                this.MemoryRegionASLREntropyBits[MemoryRegion.UserThreadStack] = this.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocBU] + 9;
                this.MemoryRegionASLREntropyBits[MemoryRegion.UserProcessHeap] = this.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocBU];
                this.MemoryRegionASLREntropyBits[MemoryRegion.UserJITCode] = this.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocBU];
            }

            /// <summary>
            /// Windows NT 2000.
            /// </summary>
            public class NT2000 : Windows
            {
                public override bool IsCompatibleWith(Hardware hardware)
                {
                    return base.IsCompatibleWith(hardware) && hardware.ArchitectureFamily == ArchitectureFamily.I386;
                }

                /// <summary>
                /// 2000 SP4.
                /// </summary>
                public class SP4 : NT2000
                {
                }
            }

            /// <summary>
            /// Windows XP.
            /// </summary>
            public class XP : Windows
            {
                public override bool IsCompatibleWith(Hardware hardware)
                {
                    return base.IsCompatibleWith(hardware) && (
                        hardware.ArchitectureFamily == ArchitectureFamily.I386 ||
                        hardware.ArchitectureFamily == ArchitectureFamily.AMD64
                        );
                }

                /// <summary>
                /// Default initializer.
                /// </summary>
                public XP()
                    : this(0)
                {
                }

                /// <summary>
                /// Initializes Windows XP with a specific service pack number.
                /// </summary>
                /// <param name="servicePack">The service pack of XP.</param>
                protected XP(int servicePack)
                {
                    if (servicePack >= 2)
                    {
                        this.InheritWindowsXPSP2Features();
                    }
                }

                /// <summary>
                /// XP RTM.
                /// </summary>
                public class RTM : XP
                {
                    public RTM() : base(0) { }
                }

                /// <summary>
                /// XP SP1.
                /// </summary>
                public class SP1 : XP
                {
                    public SP1() : base(1) { }
                }

                /// <summary>
                /// XP SP2.
                /// </summary>
                public class SP2 : XP
                {
                    public SP2() : base(2) { }
                }

                /// <summary>
                /// XP SP3.
                /// </summary>
                public class SP3 : XP
                {
                    public SP3() : base(3) { }
                }
            }

            /// <summary>
            /// Windows Server 2003.
            /// </summary>
            public class Server2003 : Windows
            {
                public override bool IsCompatibleWith(Hardware hardware)
                {
                    return base.IsCompatibleWith(hardware) && (
                        hardware.ArchitectureFamily == ArchitectureFamily.I386 ||
                        hardware.ArchitectureFamily == ArchitectureFamily.AMD64
                        );
                }

                /// <summary>
                /// Default initializer.
                /// </summary>
                public Server2003()
                    : this(0)
                {
                }

                /// <summary>
                /// Initializes Server 2003 with a specific service pack number.
                /// </summary>
                /// <param name="servicePack">The service pack number.</param>
                protected Server2003(int servicePack)
                {
                    if (servicePack >= 1)
                    {
                        this.InheritWindowsXPSP2Features();
                    }

                    this.InheritWindowsServerPolicies();
                }

                /// <summary>
                /// Server 2003 RTM.
                /// </summary>
                public class RTM : Server2003
                {
                    public RTM() : base(0) { }
                }

                /// <summary>
                /// Server 2003 SP1.
                /// </summary>
                public class SP1 : Server2003
                {
                    public SP1() : base(1) { }
                }

                /// <summary>
                /// Server 2003 SP2.
                /// </summary>
                public class SP2 : Server2003
                {
                    public SP2() : base(2) { }
                }
            }

            /// <summary>
            /// Windows Server 2003 R2.
            /// </summary>
            public class Server2003R2 : Windows
            {
                public override bool IsCompatibleWith(Hardware hardware)
                {
                    return base.IsCompatibleWith(hardware) && (
                        hardware.ArchitectureFamily == ArchitectureFamily.I386 ||
                        hardware.ArchitectureFamily == ArchitectureFamily.AMD64 ||
                        hardware.ArchitectureFamily == ArchitectureFamily.IA64
                        );
                }

                /// <summary>
                /// Default initializer.
                /// </summary>
                public Server2003R2()
                    : this(0)
                {
                }

                /// <summary>
                /// Initializes Server 2003 R2 with a specific service pack number.
                /// </summary>
                /// <param name="servicePack">The service pack number.</param>
                protected Server2003R2(int servicePack)
                {
                }

                /// <summary>
                /// Server 2003 R2 RTM.
                /// </summary>
                public class RTM : Server2003R2
                {
                    public RTM() : base(0) { }
                }
            }

            /// <summary>
            /// Windows Vista.
            /// </summary>
            public class Vista : Windows
            {
                public override bool IsCompatibleWith(Hardware hardware)
                {
                    return base.IsCompatibleWith(hardware) && (
                        hardware.ArchitectureFamily == ArchitectureFamily.I386 ||
                        hardware.ArchitectureFamily == ArchitectureFamily.AMD64
                        );
                }

                /// <summary>
                /// Default initializer.
                /// </summary>
                public Vista()
                    : this(0)
                {
                }

                /// <summary>
                /// Initializes Windows Vista with a specific service pack number.
                /// </summary>
                /// <param name="servicePack">The service pack number.</param>
                protected Vista(int servicePack)
                {
                    if (servicePack >= 1)
                    {
                        this.InheritWindowsVistaSP1Features();
                    }
                    else
                    {
                        this.InheritWindowsVistaRTMFeatures();
                    }
                }

                /// <summary>
                /// Vista RTM.
                /// </summary>
                public class RTM : Vista
                {
                    public RTM() : base(0) { }
                }

                /// <summary>
                /// Vista SP1.
                /// </summary>
                public class SP1 : Vista
                {
                    public SP1() : base(1) { }
                }

                /// <summary>
                /// Vista SP2.
                /// </summary>
                public class SP2 : Vista
                {
                    public SP2() : base(2) { }
                }
            }

            /// <summary>
            /// Windows Server 2008.
            /// </summary>
            public class Server2008 : Windows
            {
                public override bool IsCompatibleWith(Hardware hardware)
                {
                    return base.IsCompatibleWith(hardware) && (
                        hardware.ArchitectureFamily == ArchitectureFamily.I386 ||
                        hardware.ArchitectureFamily == ArchitectureFamily.AMD64 ||
                        hardware.ArchitectureFamily == ArchitectureFamily.IA64
                        );
                }

                /// <summary>
                /// Default initializer.
                /// </summary>
                public Server2008()
                    : this(0)
                {
                }

                /// <summary>
                /// Initializes Server 2008 to a specific service pack number.
                /// </summary>
                /// <param name="servicePack">The service pack number.</param>
                protected Server2008(int servicePack)
                {
                    this.InheritWindowsVistaSP1Features();
                    this.InheritWindowsServerPolicies();
                }

                /// <summary>
                /// Server 2008 RTM.
                /// </summary>
                public class RTM : Server2008
                {
                    public RTM() : base(0) { }
                }

                /// <summary>
                /// Server 2008 SP1.
                /// </summary>
                public class SP1 : Server2008
                {
                    public SP1() : base(1) { }
                }
            }

            /// <summary>
            /// Windows 7.
            /// </summary>
            public class Seven : Windows
            {
                public override bool IsCompatibleWith(Hardware hardware)
                {
                    return base.IsCompatibleWith(hardware) && (
                        hardware.ArchitectureFamily == ArchitectureFamily.I386 ||
                        hardware.ArchitectureFamily == ArchitectureFamily.AMD64
                        );
                }

                /// <summary>
                /// Default initializer.
                /// </summary>
                public Seven()
                    : this(0)
                {
                }

                /// <summary>
                /// Initializes Windows 7 with a specific service pack number.
                /// </summary>
                /// <param name="servicePack">The service pack number.</param>
                protected Seven(int servicePack)
                {
                    this.InheritWindowsSevenFeatures();
                }

                /// <summary>
                /// Windows 7 RTM.
                /// </summary>
                public class RTM : Seven
                {
                    public RTM() : base(0) { }
                }

                /// <summary>
                /// Windows 7 SP1.
                /// </summary>
                public class SP1 : Seven
                {
                    public SP1() : base(1) { }
                }
            }

            /// <summary>
            /// Windows Server 2008 R2.
            /// </summary>
            public class Server2008R2 : Windows
            {
                public override bool IsCompatibleWith(Hardware hardware)
                {
                    return base.IsCompatibleWith(hardware) && (
                        hardware.ArchitectureFamily == ArchitectureFamily.AMD64 ||
                        hardware.ArchitectureFamily == ArchitectureFamily.IA64
                        );
                }

                /// <summary>
                /// Default initializer.
                /// </summary>
                public Server2008R2()
                    : this(0)
                {
                }

                /// <summary>
                /// Initializes Server 2008 R2 with a specific service pack number.
                /// </summary>
                /// <param name="servicePack">The service pack number.</param>
                protected Server2008R2(int servicePack)
                {
                    this.InheritWindowsSevenFeatures();
                    this.InheritWindowsServerPolicies();
                }

                /// <summary>
                /// Server 2008 R2 RTM.
                /// </summary>
                public class RTM : Server2008R2
                {
                    public RTM() : base(0) { }
                }

                /// <summary>
                /// Server 2008 R2 SP1.
                /// </summary>
                public class SP1 : Server2008R2
                {
                    public SP1() : base(1) { }
                }
            }

            /// <summary>
            /// Windows 8.
            /// </summary>
            public class Eight : Windows
            {
                public override bool IsCompatibleWith(Hardware hardware)
                {
                    return base.IsCompatibleWith(hardware) && (
                        hardware.ArchitectureFamily == ArchitectureFamily.I386 ||
                        hardware.ArchitectureFamily == ArchitectureFamily.AMD64 ||
                        hardware.ArchitectureFamily == ArchitectureFamily.ARM
                        );
                }

                /// <summary>
                /// Default initializer.
                /// </summary>
                public Eight()
                    : this(0)
                {
                }

                /// <summary>
                /// Initializes Windows 8 with a specific service pack number.
                /// </summary>
                /// <param name="servicePack">The service pack number.</param>
                protected Eight(int servicePack)
                {
                    this.InheritWindowsEightFeatures();
                }

                /// <summary>
                /// Windows 8 RTM.
                /// </summary>
                public class RTM : Eight
                {
                    public RTM() : base(0) { }
                }
            }

            /// <summary>
            /// Windows Server 2012.
            /// </summary>
            public class Server2012 : Windows
            {
                public override bool IsCompatibleWith(Hardware hardware)
                {
                    return base.IsCompatibleWith(hardware) && (
                        hardware.ArchitectureFamily == ArchitectureFamily.AMD64
                        );
                }

                /// <summary>
                /// Default initializer.
                /// </summary>
                public Server2012()
                    : this(0)
                {
                }

                /// <summary>
                /// Initializes Server 2012 with a specific service pack number.
                /// </summary>
                /// <param name="servicePack">The service pack number.</param>
                protected Server2012(int servicePack)
                {
                    this.InheritWindowsEightFeatures();
                    this.InheritWindowsServerPolicies();
                }

                /// <summary>
                /// Server 2012 RTM.
                /// </summary>
                public class RTM : Server2012
                {
                    public RTM() : base(0) { }
                }

            }
        }
    }

    /// <summary>
    /// Extension methods for operating system profiles.
    /// </summary>
    public static class OperatingSystemDefaultExtensions
    {
        public static bool IsWindows(this OperatingSystem os)
        {
            return os is OperatingSystem.Windows;
        }

        /// <summary>
        /// Inherit the default operating system settings.
        /// </summary>
        /// <param name="os">This operating system.</param>
        public static void InheritDefaults(this OperatingSystem os)
        {
            foreach (MemoryRegion r in Enum.GetValues(typeof(MemoryRegion)))
            {
                os.MemoryRegionNXPolicy[r] = MitigationPolicy.NotSupported;
                os.MemoryRegionASLRPolicy[r] = MitigationPolicy.NotSupported;
                os.MemoryRegionASLREntropyBits[r] = 0;
            }

            foreach (HeapFeature f in Enum.GetValues(typeof(HeapFeature)))
            {
                os.UserHeapPolicy[f] = MitigationPolicy.NotSupported;
            }

            os.KernelSMEPPolicy = MitigationPolicy.NotSupported;

            os.KernelNullDereferencePreventionPolicy = MitigationPolicy.NotSupported;

            os.DefaultStackProtectionEnabled = false;
            os.DefaultStackProtectionVersion = StackProtectionVersion.NotSupported;

            os.RecalibrationEvent += (target) =>
                {
                    //
                    // Inherit address bits from hardware (if not specified).
                    //

                    if (target.OperatingSystem.AddressBits == null)
                    {
                        target.OperatingSystem.AddressBits = target.Hardware.AddressBits;
                    }

                    if (target.OperatingSystem.DefaultStackProtectionEnabled == true &&
                        target.OperatingSystem.DefaultStackProtectionEntropyBits == null)
                    {
                        if (target.OperatingSystem.AddressBits == 32)
                        {
                            target.OperatingSystem.DefaultStackProtectionEntropyBits = 32;
                        }
                        else if (target.OperatingSystem.AddressBits == 64)
                        {
                            target.OperatingSystem.DefaultStackProtectionEntropyBits = 64;
                        }
                    }

                    //
                    // Turn off SMEP if the hardware has disabled it or does not support it.
                    //

                    if (target.OperatingSystem.KernelSMEPPolicy != null &&
                        target.OperatingSystem.KernelSMEPPolicy.Value.IsSupported() &&
                        target.Hardware.SMEPPolicy != null && 
                        target.Hardware.SMEPPolicy.Value.IsOff())
                    {
                        target.OperatingSystem.KernelSMEPPolicy = MitigationPolicy.Off;
                    }

                    //
                    // Turn off NX if the hardware has disabled it or does not support it.
                    //

                    if (target.Hardware.NXPolicy != null && target.Hardware.NXPolicy.Value.IsOff())
                    {
                        foreach (MemoryRegion region in Enum.GetValues(typeof(MemoryRegion)))
                        {
                            try
                            {
                                if (target.OperatingSystem.MemoryRegionNXPolicy[region].IsSupported())
                                {
                                    target.OperatingSystem.MemoryRegionNXPolicy[region] = MitigationPolicy.Off;
                                }
                            }
                            catch (KeyNotFoundException)
                            {
                            }
                        }
                    }
                };
        }

        /// <summary>
        /// Inherit the default Windows operating system settings.
        /// </summary>
        /// <param name="os"></param>
        public static void InheritWindowsDefaults(this OperatingSystem.Windows os)
        {
            foreach (HeapFeature f in Enum.GetValues(typeof(HeapFeature)))
            {
                os.KernelPoolPolicies[f] = MitigationPolicy.NotSupported;
            }

            os.UserSEHOPPolicy = MitigationPolicy.NotSupported;
            os.UserSafeSEHPolicy = MitigationPolicy.NotSupported;

            os.UserASLRPolicyBottomUpHighEntropy = MitigationPolicy.NotSupported;

            os.UserPointerEncodingUEF = false;
            os.UserPointerEncodingPEBFastLockRoutine = false;
            os.UserPointerEncodingHeapCommitRoutine = false;
        }

        /// <summary>
        /// Inherit the Windows XP Service Pack 2 feature set.
        /// </summary>
        /// <param name="os"></param>
        public static void InheritWindowsXPSP2Features(this OperatingSystem.Windows os)
        {
            os.Features.Add(OperatingSystem.FeatureSet.WindowsXPSP2);

            foreach (MemoryRegion region in MemoryRegions.UserMode)
            {
                os.MemoryRegionNXPolicy[region] = MitigationPolicy.OptIn;
            }

            os.UserSafeSEHPolicy = MitigationPolicy.On;

            os.UserHeapPolicy[HeapFeature.HeapFreeSafeUnlinking] = MitigationPolicy.On;

            os.UserPointerEncodingUEF = true;
            os.UserPointerEncodingPEBFastLockRoutine = true;

            os.DefaultStackProtectionEnabled = true;
            os.DefaultStackProtectionVersion = StackProtectionVersion.GS_VC7;

            os.RecalibrationEvent += (target) =>
                {
                    OperatingSystem.Windows wos = target.OperatingSystem as OperatingSystem.Windows;

                    wos.DefaultStackProtectionEntropyBits = 16;

                    //
                    // PEB/TEB randomization is not in place for Wow64 processes.
                    //

                    if (wos.AddressBits == 32 || target.Application.AddressBits == 32)
                    {
                        wos.MemoryRegionASLRPolicy[MemoryRegion.UserPEB] = MitigationPolicy.On;
                        wos.MemoryRegionASLRPolicy[MemoryRegion.UserTEB] = MitigationPolicy.On;

                        wos.MemoryRegionASLREntropyBits[MemoryRegion.UserPEB] = 4;
                        wos.MemoryRegionASLREntropyBits[MemoryRegion.UserTEB] = 4;
                    }

                    wos.MemoryRegionNXPolicy[MemoryRegion.KernelThreadStack] = MitigationPolicy.On;
                    wos.MemoryRegionNXPolicy[MemoryRegion.KernelInitialThreadStack] = MitigationPolicy.On;
                    wos.MemoryRegionNXPolicy[MemoryRegion.KernelHyperspace] = MitigationPolicy.On;

                    if (wos.AddressBits == 64)
                    {
                        wos.MemoryRegionNXPolicy[MemoryRegion.KernelPCR] = MitigationPolicy.On;
                        wos.MemoryRegionNXPolicy[MemoryRegion.KernelPagedPool] = MitigationPolicy.On;
                        wos.MemoryRegionNXPolicy[MemoryRegion.KernelSessionPool] = MitigationPolicy.On;
                        wos.MemoryRegionNXPolicy[MemoryRegion.KernelDriverImage] = MitigationPolicy.On;
                    }
                };
        }

        /// <summary>
        /// Inherit the Windows Vista RTM feature set.
        /// </summary>
        /// <param name="os"></param>
        public static void InheritWindowsVistaRTMFeatures(this OperatingSystem.Windows os)
        {
            os.InheritWindowsXPSP2Features();

            os.Features.Add(OperatingSystem.FeatureSet.WindowsVistaRTM);

            os.MemoryRegionASLRPolicy[MemoryRegion.UserExeImageBase] = MitigationPolicy.OptIn;
            os.MemoryRegionASLRPolicy[MemoryRegion.UserDllImageBase] = MitigationPolicy.OptIn;
            os.MemoryRegionASLRPolicy[MemoryRegion.UserThreadStack] = MitigationPolicy.OptIn;
            os.MemoryRegionASLRPolicy[MemoryRegion.UserProcessHeap] = MitigationPolicy.On;

            os.MemoryRegionASLREntropyBits[MemoryRegion.UserExeImageBase] = 8;
            os.MemoryRegionASLREntropyBits[MemoryRegion.UserExeImageCode] = 8;
            os.MemoryRegionASLREntropyBits[MemoryRegion.UserExeImageData] = 8;
            os.MemoryRegionASLREntropyBits[MemoryRegion.UserDllImageBase] = 8;
            os.MemoryRegionASLREntropyBits[MemoryRegion.UserDllImageCode] = 8;
            os.MemoryRegionASLREntropyBits[MemoryRegion.UserDllImageData] = 8;
            os.MemoryRegionASLREntropyBits[MemoryRegion.UserThreadStack] = 14;
            os.MemoryRegionASLREntropyBits[MemoryRegion.UserProcessHeap] = 5;

            os.UserHeapPolicy[HeapFeature.HeapBlockHeaderCookies] = MitigationPolicy.On;
            os.UserHeapPolicy[HeapFeature.HeapBlockHeaderEncryption] = MitigationPolicy.On;
            os.UserHeapPolicy[HeapFeature.HeapEncodeCommitRoutineWithPointerKey] = MitigationPolicy.On;
            os.UserHeapPolicy[HeapFeature.HeapTerminateOnCorruption] = MitigationPolicy.OptIn;

            os.UserPointerEncodingHeapCommitRoutine = true;

            os.DefaultStackProtectionEnabled = true;
            os.DefaultStackProtectionVersion = StackProtectionVersion.GS_VC81;

            //
            // Heap termination is OptOut for 64-bit processes by default.
            //

            os.RecalibrationEvent += (target) =>
                {
                    OperatingSystem.Windows wos = target.OperatingSystem as OperatingSystem.Windows;

                    if (wos.AddressBits == 64)
                    {
                        wos.DefaultStackProtectionEntropyBits = 48;
                    }
                    else
                    {
                        wos.DefaultStackProtectionEntropyBits = 32;
                    }

                    if (wos.AddressBits == 64 && target.Application.AddressBits == 64)
                    {
                        wos.UserHeapPolicy[HeapFeature.HeapTerminateOnCorruption] = MitigationPolicy.OptOut;
                    }
                };
        }

        /// <summary>
        /// Inherit the Windows Vista Service Pack 1 feature set.
        /// </summary>
        /// <param name="os"></param>
        public static void InheritWindowsVistaSP1Features(this OperatingSystem.Windows os)
        {
            os.InheritWindowsVistaRTMFeatures();

            os.Features.Add(OperatingSystem.FeatureSet.WindowsVistaSP1);

            os.MemoryRegionASLRPolicy[MemoryRegion.KernelExeImage] = MitigationPolicy.On;
            os.MemoryRegionASLRPolicy[MemoryRegion.KernelDriverImage] = MitigationPolicy.On;

            os.MemoryRegionASLREntropyBits[MemoryRegion.KernelExeImage] = 5;
            os.MemoryRegionASLREntropyBits[MemoryRegion.KernelDriverImage] = 4;

            os.UserSEHOPPolicy = MitigationPolicy.OptIn;
        }

        /// <summary>
        /// Inherit the Windows 7 feature set.
        /// </summary>
        /// <param name="os"></param>
        public static void InheritWindowsSevenFeatures(this OperatingSystem.Windows os)
        {
            os.InheritWindowsVistaSP1Features();

            os.Features.Add(OperatingSystem.FeatureSet.Windows7);

            os.KernelPoolPolicies[HeapFeature.HeapFreeSafeUnlinking] = MitigationPolicy.On;

            os.RecalibrationEvent += (target) =>
            {
                OperatingSystem.Windows wos = target.OperatingSystem as OperatingSystem.Windows;

                if (wos.AddressBits == 64)
                {
                    os.MemoryRegionASLREntropyBits[MemoryRegion.KernelDriverImage] = 8;
                }
                else if (wos.AddressBits == 32)
                {
                    os.MemoryRegionASLREntropyBits[MemoryRegion.KernelDriverImage] = 6;
                }
            };
        }

        /// <summary>
        /// Inherit the Windows 8 feature set.
        /// </summary>
        /// <param name="os"></param>
        public static void InheritWindowsEightFeatures(this OperatingSystem.Windows os)
        {
            os.InheritWindowsSevenFeatures();

            os.Features.Add(OperatingSystem.FeatureSet.Windows8);

            os.MemoryRegionNXPolicy[MemoryRegion.KernelPageTablePages] = MitigationPolicy.On;
            os.MemoryRegionNXPolicy[MemoryRegion.KernelPagedPool] = MitigationPolicy.On;
            os.MemoryRegionNXPolicy[MemoryRegion.KernelNonPagedPool] = MitigationPolicy.On;
            os.MemoryRegionNXPolicy[MemoryRegion.KernelInitialThreadStack] = MitigationPolicy.On;
            os.MemoryRegionNXPolicy[MemoryRegion.KernelPCR] = MitigationPolicy.On;
            os.MemoryRegionNXPolicy[MemoryRegion.KernelSharedUserData] = MitigationPolicy.On;
            os.MemoryRegionNXPolicy[MemoryRegion.KernelSystemPTE] = MitigationPolicy.On;
            os.MemoryRegionNXPolicy[MemoryRegion.KernelSystemCache] = MitigationPolicy.On;
            os.MemoryRegionNXPolicy[MemoryRegion.KernelPFNDatabase] = MitigationPolicy.On;
            os.MemoryRegionNXPolicy[MemoryRegion.KernelHALReserved] = MitigationPolicy.On;

            os.MemoryRegionASLRPolicy[MemoryRegion.UserVirtualAllocBU] = MitigationPolicy.OptIn;
            os.MemoryRegionASLRPolicy[MemoryRegion.UserVirtualAllocTD] = MitigationPolicy.On;
            os.MemoryRegionASLRPolicy[MemoryRegion.UserPEB] = MitigationPolicy.On;
            os.MemoryRegionASLRPolicy[MemoryRegion.UserTEB] = MitigationPolicy.On;
            os.MemoryRegionASLRPolicy[MemoryRegion.UserForceRelocatedImageBase] = MitigationPolicy.OptIn;
            os.MemoryRegionASLRPolicy[MemoryRegion.UserForceRelocatedImageCode] = MitigationPolicy.OptIn;
            os.MemoryRegionASLRPolicy[MemoryRegion.UserForceRelocatedImageBase] = MitigationPolicy.OptIn;

            os.UserASLRPolicyBottomUpHighEntropy = MitigationPolicy.OptIn;

            os.KernelSMEPPolicy = MitigationPolicy.On;
            os.KernelNullDereferencePreventionPolicy = null; // reset to null, On vs. OptOut decided based on HW.

            os.KernelPoolPolicies[HeapFeature.KernelPoolQuotaPointerEncoding] = MitigationPolicy.On;
            os.KernelPoolPolicies[HeapFeature.KernelPoolLookasideListCookie] = MitigationPolicy.On;

            os.UserHeapPolicy[HeapFeature.HeapAllocationOrderRandomization] = MitigationPolicy.On;
            os.UserHeapPolicy[HeapFeature.HeapPreventFreeHeapBase] = MitigationPolicy.On;
            os.UserHeapPolicy[HeapFeature.HeapBusyBlockIntegrityCheck] = MitigationPolicy.On;
            os.UserHeapPolicy[HeapFeature.HeapSegmentReserveGuardPage] = MitigationPolicy.On;
            os.UserHeapPolicy[HeapFeature.HeapEncodeCommitRoutineWithPointerKey] = MitigationPolicy.NotSupported;
            os.UserHeapPolicy[HeapFeature.HeapEncodeCommitRoutineWithGlobalKey] = MitigationPolicy.On;

            os.DefaultStackProtectionEnabled = true;
            os.DefaultStackProtectionVersion = StackProtectionVersion.GS_VC10;

            //
            // The NULL dereference mitigation is OptOut on x86 and on for all other architectures.
            //

            os.RecalibrationEvent += (target) =>
                {
                    OperatingSystem.Windows wos = target.OperatingSystem as OperatingSystem.Windows;

                    if (wos.KernelNullDereferencePreventionPolicy == null)
                    {
                        if (target.Hardware.ArchitectureFamily == ArchitectureFamily.I386)
                        {
                            wos.KernelNullDereferencePreventionPolicy = MitigationPolicy.OptOut;
                        }
                        else
                        {
                            wos.KernelNullDereferencePreventionPolicy = MitigationPolicy.On;
                        }
                    }
                };

            //
            // Set ASLR entropy bits.
            //

            os.RecalibrationEvent += (target) =>
                {
                    OperatingSystem.Windows wos = target.OperatingSystem as OperatingSystem.Windows;

                    if (wos.AddressBits == 64 && target.Application.AddressBits == 64)
                    {
                        wos.MemoryRegionASLREntropyBits[MemoryRegion.UserDllImageBase] = (target.IsAssumedTrue(AssumptionName.ApplicationLoadsDllBelow4GB)) ? (uint)14 : 19;
                        wos.MemoryRegionASLREntropyBits[MemoryRegion.UserExeImageBase] = (target.IsAssumedTrue(AssumptionName.ApplicationLoadsExeBelow4GB) == true) ? (uint)8 : 17;
                        wos.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocTD] = 17;
                    }
                    else
                    {
                        wos.MemoryRegionASLREntropyBits[MemoryRegion.UserDllImageBase] = 8;
                        wos.MemoryRegionASLREntropyBits[MemoryRegion.UserExeImageBase] = 8;
                        wos.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocTD] = 8;
                    }

                    wos.MemoryRegionASLREntropyBits[MemoryRegion.UserDllImageCode] = wos.MemoryRegionASLREntropyBits[MemoryRegion.UserDllImageCode];
                    wos.MemoryRegionASLREntropyBits[MemoryRegion.UserDllImageData] = wos.MemoryRegionASLREntropyBits[MemoryRegion.UserDllImageData];
                    wos.MemoryRegionASLREntropyBits[MemoryRegion.UserExeImageCode] = wos.MemoryRegionASLREntropyBits[MemoryRegion.UserExeImageCode];
                    wos.MemoryRegionASLREntropyBits[MemoryRegion.UserExeImageData] = wos.MemoryRegionASLREntropyBits[MemoryRegion.UserExeImageData];

                    wos.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocBU] = 8;
                    wos.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocBUHE] = 24;

                    if (wos.AddressBits == 64 && wos.UserASLRPolicyBottomUpHighEntropy != null && wos.UserASLRPolicyBottomUpHighEntropy.Value.IsOn())
                    {
                        wos.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocBU] = wos.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocBUHE];
                    }

                    wos.MemoryRegionASLREntropyBits[MemoryRegion.UserForceRelocatedImageBase] = wos.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocBU];
                    wos.MemoryRegionASLREntropyBits[MemoryRegion.UserForceRelocatedImageCode] = wos.MemoryRegionASLREntropyBits[MemoryRegion.UserForceRelocatedImageBase];
                    wos.MemoryRegionASLREntropyBits[MemoryRegion.UserForceRelocatedImageData] = wos.MemoryRegionASLREntropyBits[MemoryRegion.UserForceRelocatedImageBase];

                    wos.MemoryRegionASLREntropyBits[MemoryRegion.UserThreadStack] = wos.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocBU];
                    wos.MemoryRegionASLREntropyBits[MemoryRegion.UserProcessHeap] = wos.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocBU];
                    wos.MemoryRegionASLREntropyBits[MemoryRegion.UserJITCode] = wos.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocBU];

                    wos.MemoryRegionASLREntropyBits[MemoryRegion.UserTEB] = wos.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocTD];
                    wos.MemoryRegionASLREntropyBits[MemoryRegion.UserPEB] = wos.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocTD];
                };

            //
            // Enable mitigations by default for ARM.
            //

            os.RecalibrationEvent += (target) =>
                {
                    OperatingSystem.Windows wos = target.OperatingSystem as OperatingSystem.Windows;

                    if (target.Hardware.ArchitectureFamily == ArchitectureFamily.ARM)
                    {
                        foreach (MemoryRegion region in Enum.GetValues(typeof(MitigationPolicy)))
                        {
                            wos.MemoryRegionNXPolicy[region] = MitigationPolicy.On;
                        }

                        wos.MemoryRegionASLRPolicy[MemoryRegion.UserExeImageBase] = MitigationPolicy.On;
                        wos.MemoryRegionASLRPolicy[MemoryRegion.UserDllImageBase] = MitigationPolicy.On;
                        wos.MemoryRegionASLRPolicy[MemoryRegion.UserVirtualAllocBU] = MitigationPolicy.On;
                        wos.MemoryRegionASLRPolicy[MemoryRegion.UserThreadStack] = MitigationPolicy.On;
                        wos.MemoryRegionASLRPolicy[MemoryRegion.UserForceRelocatedImageBase] = MitigationPolicy.Off;
                        wos.UserASLRPolicyBottomUpHighEntropy = MitigationPolicy.Off;

                        wos.UserSEHOPPolicy = MitigationPolicy.NotSupported;

                        wos.UserHeapPolicy[HeapFeature.HeapTerminateOnCorruption] = MitigationPolicy.OptOut;
                    }
                };
        }

        /// <summary>
        /// Inherit default Windows server policies.
        /// </summary>
        /// <param name="os"></param>
        public static void InheritWindowsServerPolicies(this OperatingSystem.Windows os)
        {
            foreach (MemoryRegion region in Enum.GetValues(typeof(MemoryRegion)))
            {
                if (os.MemoryRegionNXPolicy[region] == MitigationPolicy.OptIn)
                {
                    os.MemoryRegionNXPolicy[region] = MitigationPolicy.OptOut;
                }
            }

            if (os.UserSEHOPPolicy != MitigationPolicy.NotSupported)
            {
                os.UserSEHOPPolicy = MitigationPolicy.On;
            }
        }
    }
}
