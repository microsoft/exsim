// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

using System.Linq.Expressions;

using UR.Graphing;

using MSModel.Profiles;

namespace MSModel
{
    public delegate object TransitionSuccessDelegate(SimulationContext context);

    public class Transition
    {
        public class TransitionConstraint
        {
            public Expression<Func<SimulationContext, bool>> ConstraintExpression { get; set; }
            public Func<SimulationContext, bool> CompiledConstraint { get; set; }
        }

        public Transition()
        {
            // Make XML serialization happy.
        }

        public Transition(ExploitationPrimitive primitive, ExploitationTechnique technique, bool isRootTransition)
        {
            this.Primitive = primitive;
            this.Technique = technique;
            this.IsRootTransition = isRootTransition;

            List<TransitionConstraint> compiledConstraints = new List<TransitionConstraint>();

            foreach (Expression<Func<SimulationContext, bool>> constraintExpr in primitive.ConstraintList)
            {
                compiledConstraints.Add(new TransitionConstraint()
                {
                    ConstraintExpression = constraintExpr,
                    CompiledConstraint = constraintExpr.Compile()
                });
            }

            this.Constraints = compiledConstraints;

            this.OnSuccess = (context) =>
                {
                    Violation v = this.Primitive.GetNextViolation(context);

                    this.Primitive.NotifyOnSuccess(context, ref v);

                    return v;
                };
        }

        public void Evaluate(SimulationContext context)
        {
            if (this.Constraints != null)
            {
                foreach (var constraint in this.Constraints)
                {
                    if (constraint.CompiledConstraint.Invoke(context) == false)
                    {
                        throw new ConstraintNotSatisfied(constraint.ConstraintExpression.ToString());
                    }
                }
            }
        }

        [XmlIgnore]
        public ExploitationPrimitive Primitive { get; private set; }

        [XmlIgnore]
        public ExploitationTechnique Technique { get; private set; }

        public string Label
        {
            get
            {
                return this.Primitive.Name;
            }
        }

        [XmlIgnore]
        public IEnumerable<TransitionConstraint> Constraints { get; private set; }

        [XmlIgnore]
        public TransitionSuccessDelegate OnSuccess { get; set; }

        [XmlIgnore]
        public int Ordinal { get; set; }

        [XmlIgnore]
        public bool IsRootTransition { get; private set; }

        public override string ToString()
        {
            return this.Label;
        }
    }

    public class TransitionDescriptor
    {
        public TransitionDescriptor()
        {
        }

        public TransitionDescriptor(Transition transition)
        {
            this.TransitionObject = transition;

            if (transition != null)
            {
                this.Name = transition.Technique.Symbol;
            }
        }

        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlIgnore]
        public object TransitionObject { get; set; }

        public Transition Transition
        {
            get
            {
                return this.TransitionObject as Transition;
            }
        }
    }


    public class TransitionInformation
    {
        public Transition Transition { get; set; }
        public Violation PreViolation { get; set; }
        public Violation PostViolation { get; set; }
    }

    public class TransitionChain
    {
        public TransitionChain()
        {
            this.Transitions = new List<TransitionInformation>();
        }

        public MemoryAccessMethod FromMethod
        {
            get
            {
                TransitionInformation first = this.Transitions.FirstOrDefault();

                if (first == null)
                {
                    throw new InvalidOperationException();
                }

                return first.PreViolation.Method;
            }
        }

        public ExploitationTechnique Technique
        {
            get
            {
                TransitionInformation first = this.Transitions.FirstOrDefault();

                if (first == null)
                {
                    return null;
                }

                return first.Transition.Technique;
            }
        }

        public ExploitationPrimitive Primitive
        {
            get
            {
                TransitionInformation first = this.Transitions.FirstOrDefault();

                if (first == null)
                {
                    return null;
                }

                return first.Transition.Primitive;
            }
        }

        public string ChainDescriptor
        {
            get
            {
                TransitionInformation first = this.Transitions.FirstOrDefault();

                if (first == null)
                {
                    return "unknown";
                }

                StringBuilder builder = new StringBuilder();

                builder.Append(first.PreViolation.Method.GetAbbreviation());

                foreach (TransitionInformation ti in this.Transitions)
                {
                    builder.AppendFormat("->{0}", ti.PostViolation.Method.GetAbbreviation());
                }

                return builder.ToString();
            }
        }

        public IEnumerable<Violation> Violations
        {
            get
            {
                TransitionInformation first = this.Transitions.FirstOrDefault();
                List<Violation> violations = new List<Violation>();

                Violation previousViolation = first.PreViolation.CloneViolation();

                violations.Add(previousViolation);

                foreach (TransitionInformation ti in this.Transitions)
                {
                    Violation nextViolation = ti.PostViolation.CloneViolation();

                    previousViolation.TransitiveViolations.Add(
                        new TransitiveViolation()
                        {
                            Violation = nextViolation,
                            TransitionDescriptor = new TransitionDescriptor(ti.Transition)
                        });

                    violations.Add(nextViolation);

                    previousViolation = nextViolation;
                }

                return violations;
            }
        }

        public override string ToString()
        {
            return String.Format("{0} [{1}]", this.Primitive.Name, this.ChainDescriptor);
        }

        public List<TransitionInformation> Transitions { get; set; }
    }
}
