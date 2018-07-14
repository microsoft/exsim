// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;

namespace MSModel
{
    /// <summary>
    /// A model for application profiles.
    /// </summary>
    public class ApplicationModel : Model<Application>
    {
        /// <summary>
        /// Default initializer.
        /// </summary>
        public ApplicationModel()
        {
        }

        /// <summary>
        /// Initializes the model using application profiles from the provided stream.
        /// </summary>
        /// <param name="stream">The stream to deserialize from.</param>
        public ApplicationModel(Stream stream)
            : base(stream)
        {
        }

        /// <summary>
        /// Creates an instance of an application from the supplied XML element.
        /// </summary>
        /// <param name="element">The XML element.</param>
        /// <param name="parent">The parent profile.</param>
        /// <returns></returns>
        protected override Application CreateProfileInstance(XElement element, Application parent)
        {
            return Profile.CreateInstance<Application>(element, parent);
        }

        /// <summary>
        /// The list of applications in the model.
        /// </summary>
        public IEnumerable<Application> Applications
        {
            get
            {
                return this.Profiles;
            }
        }
    }

    /// <summary>
    /// An application.
    /// </summary>
    public class Application : Profile
    {
        /// <summary>
        /// Default initializer.
        /// </summary>
        public Application()
        {
            this.MemoryRegionASLRPolicy = new ProfilePropertyDictionary<MemoryRegion, MitigationPolicy>();
            this.MemoryRegionNXPolicy = new ProfilePropertyDictionary<MemoryRegion, MitigationPolicy>();
            this.UserHeapPolicy = new ProfilePropertyDictionary<HeapFeature, MitigationPolicy>();

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

            this.ChildApplications =
                new List<Application>(
                    from XElement e in element.Elements("Application")
                    select CreateInstance(e, this)
                    );
        }

        public override ModelType ModelType
        {
            get { return MSModel.ModelType.Application; }
        }

        public override object Clone()
        {
            Application app = base.Clone() as Application;

            app.MemoryRegionASLRPolicy = new ProfilePropertyDictionary<MemoryRegion, MitigationPolicy>(this.MemoryRegionASLRPolicy);
            app.MemoryRegionNXPolicy = new ProfilePropertyDictionary<MemoryRegion, MitigationPolicy>(this.MemoryRegionNXPolicy);
            app.UserHeapPolicy = new ProfilePropertyDictionary<HeapFeature, MitigationPolicy>(this.UserHeapPolicy);

            return app;
        }

        /// <summary>
        /// Child application profiles.
        /// </summary>
        public override IEnumerable<Profile> Children
        {
            get { return this.ChildApplications; }
        }

        /// <summary>
        /// True if this application is compatible with the provided hardware and operating system.
        /// </summary>
        /// <param name="hardware">The hardware to test.</param>
        /// <param name="os">The operating system to test.</param>
        /// <returns>True if this application is compatible.</returns>
        public virtual bool IsCompatibleWith(Hardware hardware, OperatingSystem os)
        {
            return true;
        }

        /// <summary>
        /// Child application profiles.
        /// </summary>
        public IEnumerable<Application> ChildApplications { get; set; }

        /// <summary>
        /// The number of virtual address bits supported by this application.
        /// </summary>
        [ProfileProperty]
        public uint? AddressBits { get; set; }

        /// <summary>
        /// True if this is a kernel mode application (kernel, device driver, etc).
        /// </summary>
        [ProfileProperty]
        public bool? KernelApplication { get; set; }

        #region Mitigation policies

        /// <summary>
        /// ASLR policies for this application.
        /// </summary>
        [ProfileProperty]
        public ProfilePropertyDictionary<MemoryRegion, MitigationPolicy> MemoryRegionASLRPolicy { get; set; }

        /// <summary>
        /// NX policies for this application.
        /// </summary>
        [ProfileProperty]
        public ProfilePropertyDictionary<MemoryRegion, MitigationPolicy> MemoryRegionNXPolicy { get; set; }

        /// <summary>
        /// User mode heap policies for this application.
        /// </summary>
        [ProfileProperty]
        public ProfilePropertyDictionary<HeapFeature, MitigationPolicy> UserHeapPolicy { get; set; }

        /// <summary>
        /// NX is permanently enabled.
        /// </summary>
        [ProfileProperty]
        public bool? NXPermanent { get; set; }

        /// <summary>
        /// Heap frontend version used by the application.
        /// </summary>
        [ProfileProperty]
        public HeapAllocator? HeapAllocator { get; set; }

        /// <summary>
        /// The default state of the stack protection feature for code in this application.
        /// </summary>
        [ProfileProperty]
        public bool? DefaultStackProtectionEnabled { get; set; }

        /// <summary>
        /// The default version of the stack protection feature.
        /// </summary>
        [ProfileProperty]
        public StackProtectionVersion? DefaultStackProtectionVersion { get; set; }

        /// <summary>
        /// The default number of entropy bits in a stack cookie.
        /// </summary>
        [ProfileProperty]
        public uint? DefaultStackProtectionEntropyBits { get; set; }

        #endregion

        #region Contextual properties

        [ProfileProperty]
        public bool? RestrictAutomaticRestarts { get; set; }

        [ProfileProperty]
        public bool? CanInitializeContentViaHeapSpray { get; set; }

        [ProfileProperty]
        public bool? CanInitializeCodeViaJIT { get; set; }

        #endregion

        /// <summary>
        /// A Windows application.
        /// </summary>
        public class Windows : Application
        {
            public Windows()
            {
                this.RecalibrationEvent += (target) =>
                    {
                        OperatingSystem.Windows wos = target.OperatingSystem as OperatingSystem.Windows;
                        Windows wapp = target.Application as Windows;

                        wapp.UserSEHOPPolicy = wapp.UserSEHOPPolicy.EffectivePolicy(wos.UserSEHOPPolicy);
                    };
            }

            /// <summary>
            /// Windows applications are only compatible with a Windows operating system.
            /// </summary>
            /// <param name="hardware"></param>
            /// <param name="os"></param>
            /// <returns></returns>
            public override bool IsCompatibleWith(Hardware hardware, OperatingSystem os)
            {
                return (os is OperatingSystem.Windows) == true;
            }

            [ProfileProperty]
            public MitigationPolicy? UserSEHOPPolicy { get; set; }

            [ProfileProperty]
            public MitigationPolicy? UserASLRPolicyBottomUpHighEntropy { get; set; }

            /// <summary>
            /// A Windows kernel application.
            /// </summary>
            public class Kernel : Windows
            {
                public Kernel()
                {
                    this.RecalibrationEvent += (target) =>
                        {
                            this.AddressBits = target.OperatingSystem.AddressBits;
                        };
                }
            }

            public class Inbox : Windows
            {
            }

            public class Svchost : Windows
            {
            }

            public class AppX : Windows
            {
            }

            public class IE : Windows
            {
                public IE()
                {
                    this.InheritIEDefaults();
                }

                public class IE6 : IE
                {
                }

                public class IE7 : IE
                {
                }

                public class IE8 : IE
                {
                }

                public class IE9 : IE
                {
                    public IE9()
                    {
                        this.RecalibrationEvent += (target) =>
                            {
                                if (target.OperatingSystem.Features.Contains(OperatingSystem.FeatureSet.Windows7))
                                {
                                    Application.Windows wapp = target.Application as Application.Windows;

                                    if (target.Application.AddressBits == 32 && target.Application.KernelApplication == false)
                                    {
                                        wapp.UserSEHOPPolicy = MitigationPolicy.On;
                                    }
                                }
                            };
                    }
                }

                public class IE10 : IE
                {
                    public class Modern : IE10
                    {
                    }

                    public IE10()
                    {
                        this.RecalibrationEvent += (target) =>
                            {
                                //
                                // We assume that spraying is not possible in 64-bit versions of IE when running
                                // on Windows 8.
                                //

                                if (target.OperatingSystem.Features.Contains(OperatingSystem.FeatureSet.Windows8))
                                {
                                    OperatingSystem.Windows wos = target.OperatingSystem as OperatingSystem.Windows;
                                    Application.Windows wapp = target.Application as Application.Windows;

                                    if (wapp.AddressBits == 64)
                                    {
                                        wapp.CanInitializeContentViaHeapSpray = false;

                                        wapp.UserASLRPolicyBottomUpHighEntropy = MitigationPolicy.On;

                                        wos.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocBU] = wos.MemoryRegionASLREntropyBits[MemoryRegion.UserVirtualAllocBUHE];

                                        wos.RefreshBottomUpEntropyBits();
                                    }

                                    wapp.MemoryRegionASLRPolicy[MemoryRegion.UserForceRelocatedImageBase] = MitigationPolicy.On;
                                    wapp.MemoryRegionASLRPolicy[MemoryRegion.UserForceRelocatedImageCode] = MitigationPolicy.On;
                                    wapp.MemoryRegionASLRPolicy[MemoryRegion.UserForceRelocatedImageData] = MitigationPolicy.On;
                                }

                                if (target.OperatingSystem.Features.Contains(OperatingSystem.FeatureSet.Windows7))
                                {
                                    Application.Windows wapp = target.Application as Application.Windows;

                                    if (target.Application.AddressBits == 32 && target.Application.KernelApplication == false)
                                    {
                                        wapp.UserSEHOPPolicy = MitigationPolicy.On;
                                    }
                                }
                            };
                    }
                }
            }

            public class Office : Windows
            {
                public class Office2003 : Office
                {
                }

                public class Office2007 : Office
                {
                }

                public class Office2010 : Office
                {
                }

                public class Office15 : Office
                {
                }
            }
        }
    }

    public static class ApplicationExtension
    {
        public static bool IsWindows(this Application app)
        {
            return app is Application.Windows;
        }

        public static void InheritDefaults(this Application app)
        {
            app.RecalibrationEvent += (target) =>
                {
                    //
                    // Inherit address bits from operating system (if not specified).
                    //

                    if (target.Application.AddressBits == null)
                    {
                        target.Application.AddressBits = target.OperatingSystem.AddressBits;
                    }

                    //
                    // Inherit OS NX, ASLR, and heap policies.
                    //

                    foreach (MemoryRegion region in Enum.GetValues(typeof(MemoryRegion)))
                    {
                        MitigationPolicy osPolicy = MitigationPolicy.NotSupported;
                        MitigationPolicy appPolicy = MitigationPolicy.NotSupported;

                        target.OperatingSystem.MemoryRegionASLRPolicy.TryGetValue(region, out osPolicy);
                        target.Application.MemoryRegionASLRPolicy.TryGetValue(region, out appPolicy);
                        target.Application.MemoryRegionASLRPolicy[region] = appPolicy.EffectivePolicy(osPolicy);

                        target.OperatingSystem.MemoryRegionNXPolicy.TryGetValue(region, out osPolicy);
                        target.Application.MemoryRegionNXPolicy.TryGetValue(region, out appPolicy);
                        target.Application.MemoryRegionNXPolicy[region] = appPolicy.EffectivePolicy(osPolicy);
                    }

                    foreach (HeapFeature feature in Enum.GetValues(typeof(HeapFeature)))
                    {
                        MitigationPolicy osPolicy = MitigationPolicy.NotSupported;
                        MitigationPolicy appPolicy = MitigationPolicy.NotSupported;

                        target.OperatingSystem.UserHeapPolicy.TryGetValue(feature, out osPolicy);
                        target.Application.UserHeapPolicy.TryGetValue(feature, out appPolicy);
                        target.Application.UserHeapPolicy[feature] = appPolicy.EffectivePolicy(osPolicy);
                    }

                    //
                    // Inherit default stack protection settings.
                    //

                    if (target.Application.DefaultStackProtectionEnabled == null)
                    {
                        target.Application.DefaultStackProtectionEnabled = target.OperatingSystem.DefaultStackProtectionEnabled;
                        target.Application.DefaultStackProtectionVersion = target.OperatingSystem.DefaultStackProtectionVersion;
                        target.Application.DefaultStackProtectionEntropyBits = target.OperatingSystem.DefaultStackProtectionEntropyBits;
                    }
                };
        }

        public static void InheritWindowsDefaults(this Application.Windows wos)
        {

        }

        public static void InheritIEDefaults(this Application.Windows.IE ie)
        {
            ie.CanInitializeCodeViaJIT = false;
            ie.CanInitializeContentViaHeapSpray = true;
        }
    }
}
