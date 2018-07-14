// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MSModel
{
    public class Target
    {
        public static IEnumerable<Target> CreateTargets(
            IEnumerable<Hardware> hardwareList,
            IEnumerable<OperatingSystem> operatingSystemList,
            IEnumerable<Application> applicationList,
            IEnumerable<Violation> violationList,
            IEnumerable<Assumption> assumptions = null)
        {
            if (assumptions != null)
            {
                foreach (Assumption assumption in assumptions)
                {
                    assumption.Explicit = true;
                }
            }

            return
                from Hardware hw in hardwareList
                from OperatingSystem os in operatingSystemList
                where os.IsCompatibleWith(hw)
                from Application app in applicationList
                where app.IsCompatibleWith(hw, os)
                from Violation violation in violationList
                select new Target()
                {
                    Hardware = hw,
                    OperatingSystem = os,
                    Application = app,
                    Violation = violation,
                    InitialAssumptions = assumptions == null ? new List<Assumption>() : new List<Assumption>(assumptions)
                };
        }

        public Target()
        {
            this.Hardware = new Hardware();
            this.OperatingSystem = new OperatingSystem();
            this.Application = new Application();
            this.Violation = new Violation();
            this.InitialAssumptions = new List<Assumption>();
        }

        public Hardware Hardware { get; set; }
        public OperatingSystem OperatingSystem { get; set; }
        public Application Application { get; set; }
        public Violation Violation { get; set; }
        public List<Assumption> InitialAssumptions { get; set; }

        public string Description
        {
            get
            {
                StringWriter writer = new StringWriter();

                Profile[] profiles =  {
                    this.Hardware,
                    this.OperatingSystem,
                    this.Application,
                    this.Violation
                };

                foreach (Profile profile in profiles)
                {
                    if (profile != null)
                    {
                        writer.WriteLine(profile.Description);
                    }
                }

                return writer.ToString();
            }
        }

        internal void AssumeTrue(AssumptionName name)
        {
            Assume(name, true);
        }

        internal void AssumeFalse(AssumptionName name)
        {
            Assume(name, false);
        }

        internal void AssumeTrue(Assumption assumption)
        {
            assumption.Probability = 1;
            Assume(assumption);
        }

        internal void AssumeFalse(Assumption assumption)
        {
            assumption.Probability = 0;
            Assume(assumption);
        }

        private void Assume(AssumptionName name, bool state)
        {
            Assumption assumption = new Assumption(name, (state) ? 1 : 0);
            Assume(assumption);
        }

        public void Assume(Assumption assumption)
        {
            //
            // If this assumption has already been added tot he list of initial assumptions, then
            // do not re-added if (if it was previously added by the user).  If it was not specified
            // by the user, then the new assumption shall supersede.
            //

            foreach (Assumption actualAssumption in this.InitialAssumptions)
            {
                if (actualAssumption == assumption)
                {
                    if (actualAssumption.Explicit)
                    {
                        return;
                    }
                    else
                    {
                        this.InitialAssumptions.Remove(actualAssumption);
                        break;
                    }
                }
            }

            this.InitialAssumptions.Add(assumption);
        }

        public bool IsAssumedTrue(AssumptionName name)
        {
            return IsAssumedTrue(new Assumption(name));
        }

        public bool IsAssumedTrue(Assumption assumption)
        {
            foreach (Assumption actualAssumption in this.InitialAssumptions)
            {
                if (actualAssumption == assumption)
                {
                    return (actualAssumption.Probability > 0);
                }
            }

            return false;
        }

        public void Recalibrate()
        {
            this.Hardware.Recalibrate(this);
            this.OperatingSystem.Recalibrate(this);
            this.Application.Recalibrate(this);
            this.Violation.Recalibrate(this);
        }

        public bool IsX86Application()
        {
            return 
                (
                 (this.Application.AddressBits == 32) &&
                 (
                  (this.Hardware.ArchitectureFamily == ArchitectureFamily.I386) ||
                  (this.Hardware.ArchitectureFamily == ArchitectureFamily.AMD64)
                 )
                ) ;
        }
    }
}
