// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MSModel;

using UR.Ui;

namespace MSSimulator
{
    public enum SimulatorMode
    {
        ShowViolations,
        RunSimulation,
        SimulateTechniques,
        Default
    }

    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            (new Program()).Run(new List<string>(args));
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.Error.WriteLine("UNHANDLED EXCEPTION: {0}", e.ExceptionObject);

            if (System.Diagnostics.Debugger.IsAttached)
            {
                throw e.ExceptionObject as Exception;
            }
            else
            {
                Environment.Exit(0);
            }
        }

        public Program()
        {
            this.Mode = SimulatorMode.Default;
            this.Model = new MemorySafetyModel();
        }

        public void Run(List<string> args)
        {
            this.Parser.Parse(args);

            switch (Mode)
            {
                case SimulatorMode.ShowViolations:
                    ShowViolations();
                    break;

                case SimulatorMode.RunSimulation:
                    RunSimulation();
                    break;

                case SimulatorMode.SimulateTechniques:
                    SimulateTechniques();
                    break;

                default:
                    break;
            }
        }

        private void ShowViolations()
        {
            Console.WriteLine("Violations:");
            foreach (Violation v in this.Model.ViolationModel.Violations.OrderBy(x => x.Alias))
            {
                Console.WriteLine("{0}", v.Alias);

                if (this.ShowProperties)
                {
                    foreach (ProfilePropertyInfo prop in v.Properties.OrderBy(x => x.Name))
                    {
                        Console.WriteLine("  {0}: {1}", prop.Name, prop.ValueString);
                    }
                }
            }
        }

        private void SimulateTechniques()
        {
            Target target = new Target()
            {
                Hardware = this.Model.HardwareModel.FullyQualifiedSymbolMap["x86/x86_pae"].Clone() as Hardware,
                OperatingSystem = this.Model.OperatingSystemModel.FullyQualifiedSymbolMap["windows/xp/rtm/32bit"].Clone() as MSModel.OperatingSystem,
                Application = this.Model.ApplicationModel.FullyQualifiedSymbolMap["windows/ie/ie6/32bit"].Clone() as MSModel.Application
            };

            Simulation simulation = Simulation.GetAllTechniquesSimulation(this.Model);

            Console.WriteLine("Simulation graph has {0} edges", simulation.CompleteGraph.Edges.Count);

            foreach (Transition transition in simulation.RootTransitions.Where(x => x.Primitive.IsIdentity == false))
            {
                Console.WriteLine("Exploring technique: {0} + {1}", transition.Technique.GetType(), transition.Technique.Name);

                target.Violation = new Violation(transition.Primitive.FromMethod.Value);
                target.Violation.ContentSrcState = MemoryAccessParameterState.Controlled;

                GlobalSimulationContext globalContext = new GlobalSimulationContext(target)
                {
                    TrackImpossible = false
                };

                SimulationContext initialContext = new SimulationContext(globalContext);

                Simulator simulator = Simulator.CreateDesiredCodeExecutionSimulator(simulation, initialContext);

                simulator.RestrictToRootTransitions(
                    simulation.Transitions.Where(x => x.Technique == transition.Technique)
                    );

                simulator.Run();

                Console.WriteLine("Simulation finished, press any key.");
                //Console.ReadLine();

                Console.WriteLine("{0}", globalContext.Description);

            }
        }















        private void PopulateSampleViolationList(List<Violation> violations)
        {
            //
            // Stack buffer overrun.
            //

            violations.Add(
                new Violation(
                    MemoryAccessMethod.Write,
                    baseState: MemoryAccessParameterState.Fixed,
                    contentSrcState: MemoryAccessParameterState.Controlled,
                    contentDstState: MemoryAccessParameterState.Unknown,
                    displacementState: MemoryAccessParameterState.Fixed,
                    extentState: MemoryAccessParameterState.Controlled
                    )
                {
                    AddressingMode = MemoryAddressingMode.Relative,
                    DisplacementInitialOffset = MemoryAccessOffset.PostAdjacent,
                    Direction = MemoryAccessDirection.Forward,
                    BaseRegionType = MemoryRegionType.Stack,
                    Name = "stack buffer overrun",
                    FunctionStackProtectionEnabled = true,
                    Assumptions = new HashSet<Assumption>(
                        new Assumption[] {
                            new Assumption.CanCorruptMemoryAtAddress(MemoryAddress.AddressOfStackProtectionCookie),
                            new Assumption.CanCorruptMemoryAtAddress(MemoryAddress.AddressOfStackReturnAddress),
                            new Assumption.CanCorruptMemoryAtAddress(MemoryAddress.AddressOfStackFramePointer),
                            new Assumption.CanCorruptMemoryAtAddress(MemoryAddress.AddressOfStackStructuredExceptionHandler),
                            new Assumption(AssumptionName.CanCorruptMemoryAtAddressListComplete)
                        }
                        )
                }
                );

            //
            // Use after free of C++ virtual table pointer.
            //

            violations.Add(
                new Violation(
                    MemoryAccessMethod.Read,
                    baseState: MemoryAccessParameterState.Fixed,
                    contentSrcState: MemoryAccessParameterState.Uninitialized,
                    contentDstState: MemoryAccessParameterState.Nonexistant,
                    displacementState: MemoryAccessParameterState.Fixed,
                    extentState: MemoryAccessParameterState.Fixed
                    )
                {
                    AddressingMode = MemoryAddressingMode.Absolute,
                    BaseRegionType = MemoryRegionType.Heap,
                    ContentContainerDataType = MemoryContentDataType.CppObject,
                    ContentDataType = MemoryContentDataType.CppVirtualTablePointer,
                    Name = "Use after free of C++ virtual table pointer"
                }
                );
        }

        public List<Target> GetTargets(Violation v)
        {
            List<Target> targets = new List<Target>();

            targets.Add(new Target()
            {
                Hardware = this.Model.HardwareModel.FullyQualifiedSymbolMap["x86/x86_pae"].Clone() as Hardware,
                OperatingSystem = this.Model.OperatingSystemModel.FullyQualifiedSymbolMap["windows/seven/rtm/32bit"].Clone() as MSModel.OperatingSystem,
                Application = this.Model.ApplicationModel.FullyQualifiedSymbolMap["windows/ie/ie8/32bit"].Clone() as MSModel.Application,
                Violation = v
            });

            targets.Add(new Target()
            {
                Hardware = this.Model.HardwareModel.FullyQualifiedSymbolMap["x86/x86_pae"].Clone() as Hardware,
                OperatingSystem = this.Model.OperatingSystemModel.FullyQualifiedSymbolMap["windows/eight/rtm/32bit"].Clone() as MSModel.OperatingSystem,
                Application = this.Model.ApplicationModel.FullyQualifiedSymbolMap["windows/ie/ie9/32bit"].Clone() as MSModel.Application,
                Violation = v
            });

            targets.Add(new Target()
            {
                Hardware = this.Model.HardwareModel.FullyQualifiedSymbolMap["x86/x86_pae"].Clone() as Hardware,
                OperatingSystem = this.Model.OperatingSystemModel.FullyQualifiedSymbolMap["windows/eight/rtm/64bit"].Clone() as MSModel.OperatingSystem,
                Application = this.Model.ApplicationModel.FullyQualifiedSymbolMap["windows/ie/ie10/64bit"].Clone() as MSModel.Application,
                Violation = v
            });

            return targets;
        }

        private void RunSimulation()
        {

            Simulation simulation = Simulation.GetAllTechniquesSimulation(this.Model);

            List<Violation> sampleViolations = new List<Violation>();

            PopulateSampleViolationList(sampleViolations);

            foreach (Violation v in sampleViolations)
            {
                List<Target> targets = GetTargets(v);

                foreach (Target target in targets)
                {
                    Console.WriteLine("=================================================");
                    Console.WriteLine("Simulating violation: {0} [{1}]", v, v.Name);

                    GlobalSimulationContext globalContext = new GlobalSimulationContext(target)
                    {
                        TrackImpossible = (Environment.GetEnvironmentVariable("MSMTRACKALL") == "1") ? true : false
                    };

                    SimulationContext initialContext = new SimulationContext(globalContext);

                    Console.WriteLine("Simulation graph has {0} edges", simulation.CompleteGraph.Edges.Count);

                    Simulator simulator = Simulator.CreateDesiredCodeExecutionSimulator(simulation, initialContext);

                    simulator.Run();

                    Console.WriteLine("{0}", globalContext.Description);

                    Console.WriteLine("Simulation finished, press any key.");
                }
            }
        }

        private Violation CreateViolationChain()
        {
            // load the definition here.
            Violation root = 
                new Violation(
                    MemoryAccessMethod.Write,
                    "stack buffer overrun",
                    baseState: MemoryAccessParameterState.Fixed,
                    contentSrcState: MemoryAccessParameterState.Controlled,
                    displacementState: MemoryAccessParameterState.Fixed,
                    extentState: MemoryAccessParameterState.Controlled
                    )
                {
                    AddressingMode = MemoryAddressingMode.Relative,
                    Direction = MemoryAccessDirection.Forward,
                    DisplacementInitialOffset = MemoryAccessOffset.PostAdjacent,
                    BaseRegionType = MemoryRegionType.Stack
                };

            Violation readReturn =
                new Violation(
                    MemoryAccessMethod.Read,
                    "read return address",
                    baseState: MemoryAccessParameterState.Fixed,
                    contentSrcState: MemoryAccessParameterState.Controlled,
                    displacementState: MemoryAccessParameterState.Fixed,
                    extentState: MemoryAccessParameterState.Fixed
                    )
                {
                    AddressingMode = MemoryAddressingMode.Absolute,
                    BaseRegionType = MemoryRegionType.Stack,
                    ContentDataType = MemoryContentDataType.StackReturnAddress
                };

            Violation executeReturn =
                new Violation(
                    MemoryAccessMethod.Execute,
                    "return from function",
                    baseState: MemoryAccessParameterState.Controlled,
                    contentSrcState: MemoryAccessParameterState.Unknown,
                    displacementState: MemoryAccessParameterState.Nonexistant,
                    extentState: MemoryAccessParameterState.Nonexistant
                    )
                {
                    AddressingMode = MemoryAddressingMode.Absolute
                };

            readReturn.TransitiveViolations.Add(
                new TransitiveViolation()
                {
                    Violation = executeReturn
                });
            readReturn.TransitiveExecuteListIsComplete = true;
            readReturn.TransitiveReadListIsComplete = true;
            readReturn.TransitiveWriteListIsComplete = true;

            root.TransitiveViolations.Add(
                new TransitiveViolation()
                {
                    Violation = readReturn
                });
            root.TransitiveReadListIsComplete = true;
            root.TransitiveExecuteListIsComplete = true;
            root.TransitiveWriteListIsComplete = true;

            return root;
        }

        private void Test()
        {
            //Primitives.GetSimulations<Primitives>(this.Model);

            Environment.Exit(0);
        }

        public SimulatorMode Mode { get; private set; }
        public bool ShowProperties { get; private set; }

        public MemorySafetyModel Model { get; private set; }

        private CommandLineParser Parser
        {
            get
            {
                return new CommandLineParser(
                    new CommandLineSwitch[]
                    {
                        new CommandLineSwitch("/violations", (context, value) => { this.Mode = SimulatorMode.ShowViolations; }),
                        new CommandLineSwitch("/props", (context, value) => { this.ShowProperties = true; }),
                        new CommandLineSwitch("/runsim", (context, value) => { this.Mode = SimulatorMode.RunSimulation; }),
                        new CommandLineSwitch("/simtech", (context, value) => { this.Mode = SimulatorMode.SimulateTechniques; }),
                        new CommandLineSwitch("/test", (context, value) => { this.Test(); })
                    });
            }
        }
    }
}
