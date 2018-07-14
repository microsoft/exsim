// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Text;
using System.IO;
using System.ComponentModel;

using UR.Graphing;

namespace MSModel
{
    public class Flaw : Profile
    {
        public Flaw()
        {
            this.TransitiveViolations = new List<Violation>();
            this.TransitiveFlaws = new List<Flaw>();
        }

        public Flaw(XElement element, Profile parent)
            : base(element, parent)
        {
        }

        public override void FromXml(XElement element, Profile parent)
        {
            this.TransitiveViolations = new List<Violation>();
            this.TransitiveFlaws = new List<Flaw>();

            base.FromXml(element, parent);
            
            this.ChildFlaws =
                new List<Flaw>(
                    from XElement e in element.Elements("Flaw")
                    select new Flaw(e, this)
                    );
        }

        public override ModelType ModelType
        {
            get { return MSModel.ModelType.Flaw; }
        }

        public Flaw CloneFlaw()
        {
            Flaw clone = this.Clone() as Flaw;
            clone.Guid = Guid.NewGuid();
            return clone;
        }

        [Browsable(false), XmlIgnore]
        public override IEnumerable<Profile> Children
        {
            get { return this.ChildFlaws; }
        }

        [Browsable(false), XmlIgnore]
        public IEnumerable<Flaw> ChildFlaws { get; set; }

        [
         Browsable(false),
         XmlArray("TransitiveFlaws"),
         XmlArrayItem("Flaw")
        ]
        public List<Flaw> TransitiveFlaws { get; set; }

        [
         Browsable(false),
         XmlArray("TransitiveViolations"),
         XmlArrayItem("Violation")
        ]
        public List<Violation> TransitiveViolations { get; set; }

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

        [
         Browsable(false),
         XmlIgnore
        ]
        public IEnumerable<Violation> TransitiveExecuteViolations
        {
            get
            {
                return this.TransitiveViolations.Where(x => x.Method == MemoryAccessMethod.Execute);
            }
        }

        [
         Browsable(false),
         XmlIgnore
        ]
        public IEnumerable<Violation> TransitiveReadViolations
        {
            get
            {
                return this.TransitiveViolations.Where(x => x.Method == MemoryAccessMethod.Read);
            }
        }

        [
         Browsable(false),
         XmlIgnore
        ]
        public IEnumerable<Violation> TransitiveWriteViolations
        {
            get
            {
                return this.TransitiveViolations.Where(x => x.Method == MemoryAccessMethod.Write);
            }
        }
    }

    public class FlawModel : Model<Flaw>
    {
        public FlawModel()
        {
        }

        public FlawModel(Stream stream) : base(stream)
        {
        }

        protected override void BuildTransitiveMap()
        {
            this.TransitiveMap = new DirectedGraph();

#if false
            foreach (Flaw flaw in this.Profiles)
            {
                foreach (string transitiveFlawSymbol in flaw.TransitiveFlaws)
                {
                    if (transitiveFlawSymbol == "any")
                    {
                        var transitiveFlaws = from Flaw root in this.CompositionMap.Roots select root;

                        foreach (Flaw transitiveFlaw in transitiveFlaws)
                        {
                            if (transitiveFlaw == flaw)
                            {
                                continue;
                            }

                            this.TransitiveMap.AddEdge(flaw, transitiveFlaw);
                        }
                    }
                    else
                    {
                        this.TransitiveMap.AddEdge(flaw, this.SymbolMap[transitiveFlawSymbol]);
                    }
                }
            }
#endif
        }

        public IEnumerable<Flaw> Flaws
        {
            get
            {
                return this.Profiles;
            }
        }
    }
}
