// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace MSModel.Profiles
{
    /*
     * An exploit technique.
     * 
     * Classes of this type describe a method using one or more primitive to transition
     * from one type of violation to another type of violation (possibly with intermediate steps).
     * 
     */
    public abstract class ExploitationTechnique : Profile
    {
        public ExploitationTechnique()
        {
            this.Name = "unknown";
            this.Symbol = "unknown";
        }

        public override IEnumerable<Profile> Children
        {
            get { yield break; }
        }

        public override ModelType ModelType
        {
            get { return MSModel.ModelType.Technique; }
        }

        public abstract void AddTransitionsToSimulation(Simulation simulation);
    }

    /// <summary>
    /// A fundamental exploitation technique based on a primitive.
    /// </summary>
    public class SimpleTechnique : ExploitationTechnique
    {
        public SimpleTechnique(ExploitationPrimitive primitive)
        {
            this.Primitive = primitive;

            this.Name = primitive.Name;
            this.Symbol = primitive.Symbol;
        }

        public ExploitationPrimitive Primitive { get; private set; }

        public override void AddTransitionsToSimulation(Simulation simulation)
        {
            simulation.AddTransition(
                this, 
                this.Primitive
                );
        }
    }

    #region Stack-specific techniques

    public class CorruptStackReturnAddress : ExploitationTechnique
    {
        public CorruptStackReturnAddress()
        {
            this.Name = "stack return address overwrite";
            this.Symbol = "stack_return_address_overwrite";
        }

        public override void AddTransitionsToSimulation(Simulation simulation)
        {
            simulation.AddRootTransition(
                this,
                new WriteToReadPrimitive(
                    MemoryAccessParameter.Content,
                    name: "corrupt return address on stack",
                    writeAddress: MemoryAddress.AddressOfStackReturnAddress,
                    onSuccess: (SimulationContext context, ref Violation v) =>
                        {
                            v.Name = "read return address from stack";
                            v.Address = MemoryAddress.AddressOfStackReturnAddress;
                            v.ControlTransferMethod = ControlTransferMethod.FunctionReturn;
                            v.AddressingMode = MemoryAddressingMode.Absolute;

                            //context.AssumeIsStackProtectionCookieCorrupted();
                        }
                    )
                );

            simulation.AddTransition(
                this,
                new ReadToExecutePrimitive(
                    MemoryAddress.AddressOfStackReturnAddress,
                    controlTransferMethod: ControlTransferMethod.FunctionReturn,
                    name: "return from function with corrupted return address",
                    constraints: (context) =>
                        (
                            //
                            // Base verifies that this is an indirect control transfer via a return through a stack return address.
                            //

                            //
                            // The attacker must be able to trigger a return from the function.
                            //

                            (context.AttackerFavorsAssumeTrue(AssumptionName.CanTriggerFunctionReturn) == true)

                            &&

                            //
                            // If the violation does not corrupt the stack protection cookie or it is possible
                            // to bypass the stack protection check, then the transition is possible.
                            //

                            (
                                (context.AttackerFavorsEqual(context.CurrentViolation.FunctionStackProtectionEnabled, false) == true)

                                ||

                                (context.CanCorruptMemoryAtAddress(MemoryAddress.AddressOfStackProtectionCookie) == false)

                                ||

                                (context.CanDetermineStackProtectionCookie() == true)
                            )
                        ),
                    onSuccess: (SimulationContext context, ref Violation v) =>
                        {
                            v.Name = "execute from return address";
                        }
                    )
                );
        }
    }

    public class CorruptStackFramePointer : ExploitationTechnique
    {
        public CorruptStackFramePointer()
        {
            this.Name = "stack frame pointer overwrite";
            this.Symbol = "stack_frame_pointer_overwrite";
        }

        public override void AddTransitionsToSimulation(Simulation simulation)
        {
            simulation.AddRootTransition(
                this,
                new WriteToReadPrimitive(
                    MemoryAccessParameter.Content,
                    MemoryAddress.AddressOfStackFramePointer,
                    name: "corrupt stack frame pointer leading to function return",
                    constraints: (context) =>
                        (
                            (context.Target.IsX86Application() == true)
                        ),
                    onSuccess: (SimulationContext context, ref Violation v) =>
                    {
                        v.Address = MemoryAddress.AddressOfStackFramePointer;
                        v.Name = "read frame pointer from stack";
                    }
                    )
                );

            simulation.AddTransition(
                this,
                new ReadToReadPrimitive(
                    MemoryAccessParameter.Base,
                    MemoryAddress.AddressOfStackFramePointer,
                    name: "restore corrupted frame pointer and return from child function",
                    constraints: (context) =>
                        (
                            //
                            // The attacker must be able to trigger a return from the function.
                            //

                         (context.AttackerFavorsAssumeTrue(AssumptionName.CanTriggerFunctionReturn) == true)

                         &&

                         //
                            // If the violation does not corrupt the stack protection cookie or it is possible
                            // to bypass the stack protection check, then the transition is possible.
                            //

                         (
                          (context.CanCorruptMemoryAtAddress(MemoryAddress.AddressOfStackProtectionCookie) == false)

                          ||

                          (context.CanDetermineStackProtectionCookie() == true)
                         )
                        ),
                    onSuccess: (SimulationContext context, ref Violation v) =>
                    {
                        v.Name = "restore stack pointer";
                        v.Address = MemoryAddress.AddressOfStackReturnAddress;
                        v.ControlTransferMethod = ControlTransferMethod.FunctionReturn;
                        v.AddressingMode = MemoryAddressingMode.Absolute;
                    }
                    )
                );
        }
    }

    public class CorruptStackStructuredExceptionHandler : ExploitationTechnique
    {
        public CorruptStackStructuredExceptionHandler()
        {
            this.Name = "stack seh overwrite";
            this.Symbol = "stack_seh_overwrite";
        }

        public override void AddTransitionsToSimulation(Simulation simulation)
        {
            simulation.AddRootTransition(
                this,
                new WriteToReadPrimitive(
                    MemoryAccessParameter.Content,
                    MemoryAddress.AddressOfStackStructuredExceptionHandler,
                    name: "corrupt SEH handler",
                    constraints: (context) =>
                        (
                         (context.Target.OperatingSystem.IsWindows() == true)
                         &&
                         (context.Target.IsX86Application() == true)
                         &&
                         (context.CanTriggerException() == true)
                        ),
                    onSuccess: (SimulationContext context, ref Violation v) =>
                        {
                            v.Name = "read exception handler";
                            v.AddressingMode = MemoryAddressingMode.Absolute;
                            v.Address = MemoryAddress.AddressOfStackStructuredExceptionHandler;
                        }
                    )
                );

            simulation.AddTransition(
                this,
                new ReadToExecutePrimitive(
                    MemoryAddress.AddressOfStackStructuredExceptionHandler,
                    ControlTransferMethod.IndirectFunctionCall,
                    name: "call corrupted handler via exception",
                    constraints: (context) =>
                        (
                         (context.CanBypassSAFESEH() == true)
                         &&
                         (context.CanBypassSEHOP() == true)
                        ),
                    onSuccess: (SimulationContext context, ref Violation v) =>
                        {
                            v.Name = "transfer control to exception handler";

                            // Assume that we can find an image code segment if we can find the address of NTDLL
                            if (context.HasAssumption(new Assumption.CanFindAddress(MemoryAddress.AddressOfNtdllImageBase)))
                            {
                                context.Assume(new Assumption.CanFindAddress(new MemoryAddress(MemoryContentDataType.Code, MemoryRegionType.ImageCodeSegment)));
                            }
                        }
                    )
                );
        }
    }

    #endregion

    #region Region-independent techniques

    public class CorruptFunctionPointer : ExploitationTechnique
    {
        public CorruptFunctionPointer()
        {
            this.Name = "function pointer overwrite";
            this.Symbol = "function_pointer_overwrite";
        }

        public override void AddTransitionsToSimulation(Simulation simulation)
        {
            foreach (MemoryRegionType region in Enum.GetValues(typeof(MemoryRegionType)))
            {
                if (!MemoryAddress.WritableRegionTypes.Contains(region))
                {
                    continue;
                }

                MemoryAddress functionPointerAddress = new MemoryAddress(MemoryContentDataType.FunctionPointer, region);

                simulation.AddRootTransition(
                    this,
                    new WriteToReadPrimitive(
                        MemoryAccessParameter.Content,
                        functionPointerAddress,
                        name: String.Format("corrupt function pointer stored in {0}", region),
                        onSuccess: (SimulationContext context, ref Violation v) =>
                            {
                                v.Name = "call through function pointer";
                                v.ControlTransferMethod = ControlTransferMethod.IndirectFunctionCall;
                                v.ContentDataType = MemoryContentDataType.FunctionPointer;
                                v.AddressingMode = MemoryAddressingMode.Absolute;
                            }
                        )
                    );
            }

            MemoryAddress anyFunctionPointerAddress = new MemoryAddress(MemoryContentDataType.FunctionPointer);

            simulation.AddRootTransition(
                this,
                new ReadToExecutePrimitive(
                    anyFunctionPointerAddress,
                    ControlTransferMethod.IndirectFunctionCall,
                    name: String.Format("call through function pointer"),
                    constraints: (context) =>
                        (
                            (context.AttackerFavorsAssumeTrue(AssumptionName.CanTriggerFunctionPointerCall))
                        )
                    )
                );
        }
    }

    public class CorruptCppObjectVirtualTablePointer : ExploitationTechnique
    {
        public CorruptCppObjectVirtualTablePointer()
        {
            this.Name = "corrupt C++ object virtual table pointer";
            this.Symbol = "corrupt_cpp_object_vtable_ptr";
        }

        public override void AddTransitionsToSimulation(Simulation simulation)
        {
            foreach (MemoryRegionType region in Enum.GetValues(typeof(MemoryRegionType)))
            {
                if (!MemoryAddress.WritableRegionTypes.Contains(region))
                {
                    continue;
                }

                MemoryAddress virtualTablePointerAddress =
                    new MemoryAddress(MemoryContentDataType.CppVirtualTablePointer, region);

                simulation.AddRootTransition(
                    this,
                    new WriteToReadPrimitive(
                        MemoryAccessParameter.Content,
                        virtualTablePointerAddress,
                        name: String.Format("corrupt C++ object virtual table pointer stored in {0}", region),
                        constraints: (context) =>
                            (
                                (context.AttackerFavorsEqual(context.CurrentViolation.ContentContainerDataType, MemoryContentDataType.CppObject) == true)
                            ),
                        onSuccess: (SimulationContext context, ref Violation v) =>
                            {
                                v.Name = "read corrupted virtual table pointer";
                                v.ContentDataType = MemoryContentDataType.CppVirtualTablePointer;
                                v.AddressingMode = MemoryAddressingMode.Absolute;
                            }
                        )
                    );

                simulation.AddRootTransition(
                    this,
                    new ReadToReadPrimitive(
                        MemoryAccessParameter.Base,
                        virtualTablePointerAddress,
                        name: String.Format("read virtual table pointer from C++ object stored in {0}", region),
                        constraints: (context) =>
                            (
                                (context.AttackerFavorsEqual(context.CurrentViolation.ContentContainerDataType, MemoryContentDataType.CppObject) == true)

                                &&

                                (context.AttackerFavorsAssumeTrue(AssumptionName.CanTriggerVirtualMethodCall))
                            ),
                        onSuccess: (SimulationContext context, ref Violation v) =>
                            {
                                v.Name = "read virtual method address from virtual table";
                                v.ContentDataType = MemoryContentDataType.CppVirtualTable;
                                v.ControlTransferMethod = ControlTransferMethod.VirtualMethodCall;
                                v.AddressingMode = MemoryAddressingMode.Absolute;
                            }
                        )
                    );
            }

            MemoryAddress virtualTableAddress =
                new MemoryAddress(MemoryContentDataType.CppVirtualTable);

            simulation.AddTransition(
                this,
                new ReadToExecutePrimitive(
                    virtualTableAddress,
                    ControlTransferMethod.VirtualMethodCall,
                    name: "call virtual method via virtual table",
                    constraints: (context) =>
                        (
                            (context.AttackerFavorsAssumeTrue(AssumptionName.CanTriggerVirtualMethodCall) == true)
                        ),
                    onSuccess: (SimulationContext context, ref Violation v) =>
                        {
                            v.Name = "execute virtual method";
                        }
                    )
                );
        }
    }

    #endregion

    #region Code execution techniques

    /// <summary>
    /// Execute controlled data as code.
    /// </summary>
    public class ExecuteControlledDataAsCode : ExploitationTechnique
    {
        public ExecuteControlledDataAsCode()
        {
            this.Symbol = "execute_data_as_code";
        }

        public override void AddTransitionsToSimulation(Simulation simulation)
        {
            simulation.AddRootTransition(
                this,
                new CodeExecutionPrimitive(
                    name: "execute attacker controlled data as code",
                    codeExecutionAddress: new MemoryAddress(MemoryContentDataType.AttackerControlledData),
                    constraints: (context) =>
                        (
                            (context.IsAssumedTrue(AssumptionName.CanExecuteControlledCode) == false)

                            &&

                            (context.CanExecuteData() == true)

                            &&

                            (context.CanFindAddress(MemoryAddress.AddressOfAttackerControlledCode) == true)
                        ),
                    nextViolation: (context) =>
                        {
                            Violation v = context.CurrentViolation.NewTransitiveViolation(
                                MemoryAccessMethod.Execute,
                                name: "execute arbitrary code",
                                baseState: MemoryAccessParameterState.Controlled,
                                contentSrcState: MemoryAccessParameterState.Controlled
                                );

                            return v;
                        },
                    onSuccess: (SimulationContext context, ref Violation v) =>
                        {
                            context.Assume(AssumptionName.CanExecuteCode);
                            context.Assume(AssumptionName.CanExecuteControlledCode);
                            context.Assume(AssumptionName.CanExecuteDesiredCode);
                        }
                    )
                );

            // add another transition to represent hopping through a jmp esp to controlled data.
        }
    }

    public class ExecuteJITCode : ExploitationTechnique
    {
        public ExecuteJITCode()
        {
            this.Symbol = "execute_jit_payload";
        }

        public override void AddTransitionsToSimulation(Simulation simulation)
        {
            simulation.AddRootTransition(
                this,
                new CodeExecutionPrimitive(
                    "execute payload in JIT code region",
                    codeExecutionAddress: new MemoryAddress(MemoryContentDataType.Code, MemoryRegionType.JITCode),
                    constraints: (context) =>
                        (
                            (context.IsAssumedTrue(AssumptionName.CanExecuteControlledCode) == false)

                            &&

                            (context.IsAssumedTrue(AssumptionName.CanBypassNX) == false)

                            &&

                            (context.AttackerFavorsAssumeTrue(AssumptionName.IsJITEngineVersionKnown) == true)

                            &&

                            (context.CanInitializeCodeViaJIT() == true)
                        ),
                    nextViolation: (context) =>
                        {
                            Violation v = context.CurrentViolation.NewTransitiveViolation(
                                MemoryAccessMethod.Execute,
                                name: "execute arbitrary code",
                                baseState: MemoryAccessParameterState.Controlled,
                                contentSrcState: MemoryAccessParameterState.Controlled
                                );

                            return v;
                        },
                    onSuccess: (SimulationContext context, ref Violation v) =>
                        {
                            context.Assume(AssumptionName.CanInitializeCodeViaJIT);
                            context.Assume(new Assumption.CanFindAddress(MemoryAddress.AddressOfAttackerControlledCode));
                            context.Assume(AssumptionName.CanExecuteCode);
                            context.Assume(AssumptionName.CanExecuteControlledCode);
                            context.Assume(AssumptionName.CanBypassNX);
                            context.Assume(AssumptionName.CanExecuteDesiredCode);
                        }
                    )
                );
        }
    }

    public abstract class ExecuteROPPayload : ExploitationTechnique
    {
        public override void AddTransitionsToSimulation(Simulation simulation)
        {
            foreach (MemoryAddress codeAddress in MemoryAddress.ROPGadgetCodeAddresses)
            {
                AddTransitionsToSimulation(simulation, codeAddress);
            }
        }

        public virtual void AddTransitionsToSimulation(Simulation simulation, MemoryAddress codeAddress)
        {
        }

        public Func<SimulationContext, bool> GetBaseROPConstraints(MemoryAddress address)
        {
            switch (address.Region)
            {
                case MemoryRegionType.ImageCodeSegment:
                    return
                        (context) =>
                        (
                            (
                                (context.CanFindAddress(address) == true)

                                &&

                                (context.AttackerFavorsAssumeTrue(AssumptionName.IsROPGadgetImageVersionKnown) == true)

                                &&

                                (context.AttackerFavorsAssumeTrue(AssumptionName.CanFindRequiredROPGadgetsInImageCode) == true)

                                &&

                                (context.AttackerFavorsAssumeTrue(AssumptionName.CanPivotStackPointer) == true)

                                &&

                                (context.CanFindAddress(MemoryAddress.AddressOfAttackerControlledData) == true) // needed for pivoting ROP stack
                            )
                        );

                case MemoryRegionType.JITCode:
                    return 
                        (context) =>
                        (
                            (context.CanInitializeCodeViaJIT() == true)

                            &&

                            (context.CanFindAddress(address) == true)

                            &&

                            (context.AttackerFavorsAssumeTrue(AssumptionName.IsJITEngineVersionKnown) == true)

                            &&

                            (context.AttackerFavorsAssumeTrue(AssumptionName.CanFindRequiredROPGadgetsInJITCode) == true)

                            &&

                            (context.AttackerFavorsAssumeTrue(AssumptionName.CanPivotStackPointer) == true)
                        );

                default:
                    throw new NotSupportedException();
            }
        }

        public class StageViaVirtualAllocOrProtect : ExecuteROPPayload
        {
            public StageViaVirtualAllocOrProtect()
            {
                this.Symbol = "execute_rop_stage_to_virtual_alloc";
            }

            public override void AddTransitionsToSimulation(Simulation simulation, MemoryAddress codeAddress)
            {
                simulation.AddRootTransition(
                    this,
                    new CodeExecutionPrimitive(
                        "execute ROP (stage to VirtualProtect/VirtualAlloc)",
                        codeExecutionAddress: codeAddress,
                        constraints: (context) =>
                            (
                                (context.IsAssumedTrue(AssumptionName.CanExecuteControlledCode) == false)

                                &&

                                (context.CanExecuteData() == false)

                                &&

                                (context.IsAssumedTrue(AssumptionName.CanBypassNX) == false)

                                &&

                                (this.GetBaseROPConstraints(codeAddress)(context) == true)

                                &&

                                (context.AttackerFavorsAssumeTrue(AssumptionName.CanProtectDataAsCode) == true)
                            ),
                        nextViolation: (context) =>
                            {
                                Violation v = context.CurrentViolation.NewTransitiveViolation(
                                    MemoryAccessMethod.Execute,
                                    name: "execute arbitrary code",
                                    baseState: MemoryAccessParameterState.Controlled,
                                    contentSrcState: MemoryAccessParameterState.Controlled
                                    );

                                return v;
                            },
                        onSuccess: (SimulationContext context, ref Violation v) =>
                            {
                                context.Assume(AssumptionName.CanExecuteCode);
                                context.Assume(AssumptionName.CanExecuteControlledCode);
                                context.Assume(new Assumption.CanFindAddress(MemoryAddress.AddressOfAttackerControlledData));
                                context.Assume(new Assumption.CanFindAddress(MemoryAddress.AddressOfAttackerControlledCode));
                                context.Assume(AssumptionName.CanBypassNX);
                                context.Assume(AssumptionName.CanExecuteDesiredCode);
                            }
                        )
                    );
            }
        }

        public class StageViaExecutableHeap : StageViaVirtualAllocOrProtect
        {
            public override void AddTransitionsToSimulation(Simulation simulation, MemoryAddress codeAddress)
            {
                // TODO
            }
        }

        public class StageViaDisableNX : ExecuteROPPayload
        {
            public StageViaDisableNX()
            {
                this.Symbol = "execute_rop_disable_nx";
            }

            public override void AddTransitionsToSimulation(Simulation simulation, MemoryAddress codeAddress)
            {
                simulation.AddRootTransition(
                    this,
                    new CodeExecutionPrimitive(
                        "execute ROP (stage to disable NX for process)",
                        codeExecutionAddress: codeAddress,
                        constraints: (context) =>
                            (
                                (context.IsAssumedTrue(AssumptionName.CanExecuteControlledCode) == false)

                                &&

                                (context.CanExecuteData() == false)

                                &&

                                (context.IsAssumedTrue(AssumptionName.CanBypassNX) == false)

                                &&

                                (this.GetBaseROPConstraints(codeAddress)(context) == true)

                                &&

                                (context.Target.OperatingSystem.IsWindows() == true)

                                &&

                                (context.Target.IsX86Application() == true)

                                &&

                                (context.AttackerFavorsEqual(context.Target.Application.MemoryRegionNXPolicy[MemoryRegion.UserProcessHeap], MitigationPolicy.On) == true)

                                &&

                                (context.AttackerFavorsEqual(context.Target.Application.NXPermanent, false))
                            ),
                        nextViolation: (context) =>
                            {
                                Violation v = context.CurrentViolation.NewTransitiveViolation(
                                    MemoryAccessMethod.Execute,
                                    name: "execute arbitrary code",
                                    baseState: MemoryAccessParameterState.Controlled,
                                    contentSrcState: MemoryAccessParameterState.Unknown
                                    );

                                return v;
                            },
                        onSuccess: (SimulationContext context, ref Violation v) =>
                            {
                                context.Assume(AssumptionName.CanExecuteCode);

                                //
                                // Assume that it is now possible to execute data as code since we have disabled NX.
                                //

                                context.ForgetAssumption(AssumptionName.CanExecuteData);
                                context.AssumeIsTrue(AssumptionName.CanExecuteData);
                                context.Assume(new Assumption.CanFindAddress(MemoryAddress.AddressOfAttackerControlledData));
                                context.Assume(new Assumption.CanFindAddress(MemoryAddress.AddressOfAttackerControlledCode));
                                context.Assume(AssumptionName.CanBypassNX);
                            }
                        )
                    );
            }
        }

        public class PureROP : ExecuteROPPayload
        {
            public PureROP()
            {
                this.Symbol = "execute_rop_pure";
            }

            public override void AddTransitionsToSimulation(Simulation simulation, MemoryAddress codeAddress)
            {
                simulation.AddRootTransition(
                    this,
                    new CodeExecutionPrimitive(
                        "execute ROP (pure)",
                        codeExecutionAddress: codeAddress,
                        constraints: (context) =>
                            (
                                (context.IsAssumedTrue(AssumptionName.CanExecuteCode) == false)

                                &&

                                (context.IsAssumedTrue(AssumptionName.CanBypassNX) == false)

                                &&

                                (this.GetBaseROPConstraints(codeAddress)(context) == true)
                            ),
                        nextViolation: (context) =>
                            {
                                Violation v = context.CurrentViolation.NewTransitiveViolation(
                                    MemoryAccessMethod.Execute,
                                    name: "execute existing code",
                                    baseState: MemoryAccessParameterState.Controlled,
                                    contentSrcState: MemoryAccessParameterState.Fixed
                                    );

                                return v;
                            },
                        onSuccess: (SimulationContext context, ref Violation v) =>
                            {
                                context.Assume(AssumptionName.CanExecuteCode);
                                context.Assume(AssumptionName.CanBypassNX);
                                context.Assume(AssumptionName.CanExecuteDesiredCode);
                            }
                        )
                    );
            }
        }
    }

    #endregion

    #region Content initialization techniques

    // need techniques for initializing source content as well as destination content.

    public class HeapSpray : ExploitationTechnique
    {
        public HeapSpray()
        {
            this.Name = "heap spray";
            this.Symbol = "heap_spray";
        }

        public override void AddTransitionsToSimulation(Simulation simulation)
        {
            var dataTypes = new MemoryContentDataType[]
            {
                MemoryContentDataType.AttackerControlledData,
                MemoryContentDataType.WriteBasePointer,
                MemoryContentDataType.WriteDisplacement,
                MemoryContentDataType.WriteContent,
                MemoryContentDataType.WriteExtent,
                MemoryContentDataType.ReadBasePointer,
                MemoryContentDataType.ReadContent,
                MemoryContentDataType.ReadDisplacement,
                MemoryContentDataType.ReadExtent,
                MemoryContentDataType.FunctionPointer,
                MemoryContentDataType.CppVirtualTablePointer,
                MemoryContentDataType.CppVirtualTable,
            };

            var regionTypes = new MemoryRegionType[]
            {
                MemoryRegionType.Heap,
                MemoryRegionType.Any
            };

            foreach (MemoryRegionType regionType in regionTypes)
            {
                foreach (MemoryContentDataType dataType in dataTypes)
                {
                    MemoryAddress address = new MemoryAddress(dataType, regionType);

                    simulation.AddRootTransition(
                        this,
                        new InitializeDestinationContentPrimitive(
                            String.Format("heap spray content to init dest {0}", address),
                            destinationAddress: address,
                            constraints: (context) =>
                                (
                                    (context.CanInitializeContentViaHeapSpray() == true)
                                ),
                            onSuccess: (SimulationContext context, ref Violation v) =>
                                {
                                    context.Assume(AssumptionName.CanInitializeContentViaHeapSpray);
                                    context.Assume(new Assumption.CanFindAddress(MemoryAddress.AddressOfAttackerControlledData));
                                }
                            )
                        );

                    simulation.AddRootTransition(
                        this,
                        new InitializeSourceContentPrimitive(
                            String.Format("heap spray content to init src {0}", address),
                            sourceAddress: address,
                            constraints: (context) =>
                                (
                                    (context.CanInitializeContentViaHeapSpray() == true)
                                ),
                            onSuccess: (SimulationContext context, ref Violation v) =>
                                {
                                    context.Assume(AssumptionName.CanInitializeContentViaHeapSpray);
                                    context.Assume(new Assumption.CanFindAddress(MemoryAddress.AddressOfAttackerControlledData));
                                }
                            )
                        );
                }
            }

            //
            // Heap spraying can also be used to spray code (on systems that don't support or enable
            // NX).
            //

            MemoryAddress codeAddress = new MemoryAddress(MemoryContentDataType.AttackerControlledData);

            simulation.AddRootTransition(
                this,
                new InitializeExecutableContentPrimitive(
                    String.Format("initialize content with code via heap spray"),
                    codeAddress,
                    constraints: (context) =>
                        (
                            // no need for this technique if we can already accomplish this via spraying data.
                            (context.CanExecuteMemoryAtAddress(new MemoryAddress(MemoryContentDataType.AttackerControlledData, null)) == false)

                            &&

                            (context.CanInitializeContentViaHeapSpray() == true)
                        ),
                        onSuccess: (SimulationContext context, ref Violation v) =>
                        {
                            context.Assume(AssumptionName.CanInitializeContentViaHeapSpray);
                            context.Assume(new Assumption.CanFindAddress(MemoryAddress.AddressOfAttackerControlledData));
                            context.Assume(new Assumption.CanFindAddress(MemoryAddress.AddressOfAttackerControlledCode));
                        }
                    )
                );
        }
    }

    /// <summary>
    /// Initializes the content of an uninitialized local variable on the stack.
    /// </summary>
    public class StackLocalVariableInitialization : ExploitationTechnique
    {
        public StackLocalVariableInitialization()
        {
            this.Name = "initialize local variable via overlapping frame";
            this.Symbol = "stack_overlapping_variable_initialization";
        }

        public override void AddTransitionsToSimulation(Simulation simulation)
        {
            var dataTypes = new MemoryContentDataType[]
            {
                MemoryContentDataType.WriteBasePointer,
                MemoryContentDataType.WriteDisplacement,
                MemoryContentDataType.WriteContent,
                MemoryContentDataType.WriteExtent,
                MemoryContentDataType.ReadBasePointer,
                MemoryContentDataType.ReadContent,
                MemoryContentDataType.ReadDisplacement,
                MemoryContentDataType.ReadExtent,
                MemoryContentDataType.FunctionPointer,
                MemoryContentDataType.CppVirtualTablePointer
            };

            foreach (MemoryContentDataType dataType in dataTypes)
            {
                MemoryAddress address = new MemoryAddress(dataType, MemoryRegionType.Stack);

                simulation.AddRootTransition(
                    this,
                    new InitializeDestinationContentPrimitive(
                        String.Format("Initialize content at destination address ({0}) of write via stack local var initialization", address),
                        destinationAddress: address,
                        constraints: (context) =>
                            (
                                (context.AttackerFavorsAssumeTrue(AssumptionName.CanInitializeContentViaStackOverlappingLocal) == true)
                            )
                        )
                    );

                simulation.AddRootTransition(
                    this,
                    new InitializeSourceContentPrimitive(
                        String.Format("Initialize content at source address ({0}) of read via stack local var initialization", address),
                        sourceAddress: address,
                        constraints: (context) =>
                            (
                                (context.AttackerFavorsAssumeTrue(AssumptionName.CanInitializeContentViaStackOverlappingLocal) == true)
                            )
                        )
                    );
            }
        }
    }

    /// <summary>
    /// Load a non-ASLR image at a predictable address.
    /// </summary>
    public class LoadNonASLRImage : ExploitationTechnique
    {
        public LoadNonASLRImage()
        {
            this.Name = "load non-ASLR image";
            this.Symbol = "load_non_aslr_image";
        }

        public override void AddTransitionsToSimulation(Simulation simulation)
        {
            simulation.AddRootTransition(
                this,
                new InitializeExecutableContentPrimitive(
                    name: "load non-ASLR image",
                    codeAddress: new MemoryAddress(MemoryContentDataType.Code, MemoryRegionType.ImageCodeSegment),
                    newContentState: MemoryAccessParameterState.Fixed,
                    constraints: (context) =>
                        (
                            (context.CanLoadNonASLRImage() == true)
                        ),
                    onSuccess: (SimulationContext context, ref Violation v) =>
                        {
                            context.Assume(AssumptionName.CanLoadNonASLRImage);
                        }
                    )
                );
        }
    }

    /// <summary>
    /// Load a non-ASLR image at a predictable address.
    /// </summary>
    public class LoadNonASLRNonSafeSEHImage : ExploitationTechnique
    {
        public LoadNonASLRNonSafeSEHImage()
        {
            this.Name = "load non-safeseh image";
            this.Symbol = "load_non_safeseh_image";
        }

        public override void AddTransitionsToSimulation(Simulation simulation)
        {
            simulation.AddRootTransition(
                this,
                new InitializeExecutableContentPrimitive(
                    name: "load non-ASLR and non-safeSEH image",
                    codeAddress: new MemoryAddress(MemoryContentDataType.Code, MemoryRegionType.ImageCodeSegment),
                    newContentState: MemoryAccessParameterState.Fixed,
                    constraints: (context) =>
                        (
                            (context.CanLoadNonASLRImage() == true)

                            &&

                            (context.CanLoadNonSafeSEHImage() == true)
                        ),
                    onSuccess: (SimulationContext context, ref Violation v) =>
                        {
                            context.Assume(AssumptionName.CanLoadNonASLRImage);
                            context.Assume(AssumptionName.CanLoadNonASLRNonSafeSEHImage);
                        }
                    )
                );
        }
    }

    #endregion
}
