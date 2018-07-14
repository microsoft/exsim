// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSModel
{
    public enum MemoryAccessMethod
    {
        Read,
        Write,
        Execute
    }

    public enum MemoryAccessParameter
    {
        Base,
        Content,
        Displacement,
        Extent
    }

    public enum MemoryAccessParameterState
    {
        Controlled,
        Fixed,
        Uninitialized,
        Unknown,
        /// <summary>
        /// Not a parameter
        /// </summary>
        Nonexistant
    }

    public static class EnumExtensions
    {
        public static MemoryAddress GetMemoryAddress(this MemoryAccessParameter parameter, MemoryAccessMethod method, MemoryRegionType? region = null)
        {
            MemoryContentDataType dataType;

            switch (parameter)
            {
                case MemoryAccessParameter.Base:
                    if (method == MemoryAccessMethod.Read)
                    {
                        dataType = MemoryContentDataType.ReadBasePointer;
                    }
                    else if (method == MemoryAccessMethod.Write)
                    {
                        dataType = MemoryContentDataType.WriteBasePointer;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                    break;

                case MemoryAccessParameter.Content:
                    if (method == MemoryAccessMethod.Read)
                    {
                        dataType = MemoryContentDataType.ReadContent;
                    }
                    else if (method == MemoryAccessMethod.Write)
                    {
                        dataType = MemoryContentDataType.WriteContent;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                    break;

                case MemoryAccessParameter.Displacement:
                    if (method == MemoryAccessMethod.Read)
                    {
                        dataType = MemoryContentDataType.ReadDisplacement;
                    }
                    else if (method == MemoryAccessMethod.Write)
                    {
                        dataType = MemoryContentDataType.WriteDisplacement;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                    break;

                case MemoryAccessParameter.Extent:
                    if (method == MemoryAccessMethod.Read)
                    {
                        dataType = MemoryContentDataType.ReadExtent;
                    }
                    else if (method == MemoryAccessMethod.Write)
                    {
                        dataType = MemoryContentDataType.WriteExtent;
                    }
                    else
                    {
                        throw new NotSupportedException();
                    }
                    break;

                default:
                    throw new NotSupportedException();
            }

            return new MemoryAddress(dataType, region);
        }

        public static string GetName(this MemoryAccessParameterState state)
        {
            switch (state)
            {
                case MemoryAccessParameterState.Controlled: return "controlled";
                case MemoryAccessParameterState.Fixed: return "fixed";
                case MemoryAccessParameterState.Uninitialized: return "uninitialized";
                case MemoryAccessParameterState.Unknown: return "unknown";
                default: return "unknown";
            }
        }

        public static string GetName(this MemoryAccessParameterState? state)
        {
            return (state == null) ? "unknown" : state.Value.GetAbbreviation();
        }

        public static string GetAbbreviation(this MemoryAccessMethod method)
        {
            switch (method)
            {
                case MemoryAccessMethod.Read: return "r";
                case MemoryAccessMethod.Write: return "w";
                case MemoryAccessMethod.Execute: return "x";
                default: return "?";
            }
        }

        public static string GetAbbreviation(this MemoryAccessMethod? method)
        {
            return (method == null) ? "?" : method.Value.GetAbbreviation();
        }

        public static string GetAbbreviation(this MemoryAccessParameter parameter)
        {
            switch (parameter)
            {
                case MemoryAccessParameter.Base: return "b";
                case MemoryAccessParameter.Content: return "c";
                case MemoryAccessParameter.Displacement: return "d";
                case MemoryAccessParameter.Extent: return "e";
                default: return "?";
            }
        }

        public static string GetAbbreviation(this MemoryAccessParameter? parameter)
        {
            return (parameter == null) ? "?" : parameter.Value.GetAbbreviation();
        }

        public static string GetAbbreviation(this MemoryAccessParameterState state)
        {
            switch (state)
            {
                case MemoryAccessParameterState.Controlled: return "c";
                case MemoryAccessParameterState.Fixed: return "f";
                case MemoryAccessParameterState.Uninitialized: return "u";
                case MemoryAccessParameterState.Unknown: return "?";
                default: return "?";
            }
        }

        public static string GetAbbreviation(this MemoryAccessParameterState? state)
        {
            return (state == null) ? "?" : state.Value.GetAbbreviation();
        }
    }

    public enum MemoryAccessDirection
    {
        Forward,
        Reverse
    }

    public enum MemoryAddressingMode
    {
        Absolute,
        Relative
    }

    public enum MemoryAccessOffset
    {
        InsideObject,
        PostAdjacent,
        PostNonAdjacent,
        PreAdjacent,
        PreNonAdjacent
    }

    public enum MemoryRegionType
    {
        Any,
        Null,
        Stack,
        Heap,
        Other,
        ImageDataSegment,
        ImageCodeSegment,
        ImageCodeSegmentNtdll,
        JITCode
    }

    public enum ControlTransferMethod
    {
        IndirectJump,
        IndirectFunctionCall,
        VirtualMethodCall,
        FunctionReturn
    }

    public enum MemoryContentDataType
    {
        Other,

        Data,
        Code,
        WritableCode,

        AttackerControlledData,
        AttackerControlledCode,

        WriteBasePointer,
        WriteDisplacement,
        WriteContent,
        WriteExtent,

        ReadBasePointer,
        ReadContent,
        ReadDisplacement,
        ReadExtent,

        CppObject,
        CppVirtualTablePointer,
        CppVirtualTable,

        FunctionPointer,

        //
        // Stack-specific content data types.
        //

        StackStructuredExceptionHandlerFunctionPointer,
        StackReturnAddress,
        StackFramePointer,
        StackProtectionCookie
    }

    public enum WellKnownViolationChain
    {
        UninitializedUseLeadingToCallViaVirtualTablePointer,
        UninitializedUseLeadingToCallViaFunctionPointer,
        UninitializedUseLeadingToWrite,

        CorruptBaseOfWrite,
        CorruptContentOfWrite,
        CorruptDisplacementOfWrite,
        CorruptExtentOfWrite,

        CorruptVirtualTablePointerUsedByCall,
        CorruptFunctionPointerUsedByCall,
        CorruptReturnAddressUsedByReturn,

    }

    public enum Stance
    {
        Active,
        Passive
    }

    public enum FaultTolerance
    {
        /// <summary>
        /// Swallows relevant exceptions.
        /// </summary>
        Tolerant,
        Nontolerant
    }

    public enum Repeatability
    {
        Repeatable,
        Nonrepeatable
    }

    public enum ExecutionDomain
    {
        Unspecified,
        User,
        Kernel,
        Hypervisor
    }

    public enum AccessRequirement
    {
        Unspecified,
        Unauthenticated,
        Authenticated,
        Authorized
    }

    public static class ExecutionDomainExtension
    {
        public static bool IsUser(this ExecutionDomain? domain)
        {
            return (domain == null || domain == ExecutionDomain.User);
        }

        public static bool IsKernel(this ExecutionDomain? domain)
        {
            return (domain == null || domain == ExecutionDomain.Kernel);
        }
    }

    public enum Locality
    {
        Unspecified,
        Remote,
        Local,
        Both
    }

    public enum StackProtectionVersion
    {
        GS_VC7,
        GS_VC81,
        GS_VC10,
        GS_VC11,
        NotSupported
    }

    public enum HeapAllocator
    {
        NTVirtualMemory,
        NTHeapBackend,
        NTHeapFrontendLowFragmentationHeap,
        NTHeapFrontendLookasideLists
    }

    public enum ArchitectureFamily
    {
        I386,
        AMD64,
        IA64,
        ARM
    }

    public enum MitigationPolicy
    {
        On,
        OptIn,
        OptOut,

        //
        // All "off" policies must come after this point.
        //

        Off,
        NotSupported
    }

    public static class MitigationPolicyExtension
    {
        public static bool IsEnabled(this MitigationPolicy policy)
        {
            return policy < MitigationPolicy.Off;
        }

        public static bool IsDisabledOrNotSupported(this MitigationPolicy policy)
        {
            return policy >= MitigationPolicy.Off;
        }

        public static bool IsSupported(this MitigationPolicy policy)
        {
            return policy != MitigationPolicy.NotSupported;
        }

        public static bool IsSupported(this MitigationPolicy? policy)
        {
            return policy == null || policy.Value.IsSupported();
        }

        public static bool IsOn(this MitigationPolicy policy)
        {
            return policy == MitigationPolicy.On || policy == MitigationPolicy.OptOut;
        }

        public static bool? IsOn(this MitigationPolicy? policy)
        {
            if (policy == null)
            {
                return null;
            }
            else
            {
                return policy.Value.IsOn();
            }
        }

        public static bool IsOff(this MitigationPolicy policy)
        {
            return policy == MitigationPolicy.Off || policy == MitigationPolicy.NotSupported || policy == MitigationPolicy.OptIn;
        }

        public static MitigationPolicy EffectivePolicy(this MitigationPolicy policy, MitigationPolicy basePolicy)
        {
            switch (basePolicy)
            {
                case MitigationPolicy.On:
                    return MitigationPolicy.On;

                case MitigationPolicy.Off:
                    return MitigationPolicy.Off;

                case MitigationPolicy.NotSupported:
                    return MitigationPolicy.NotSupported;

                case MitigationPolicy.OptIn:
                    if (policy == MitigationPolicy.On)
                    {
                        return policy;
                    }
                    else
                    {
                        return MitigationPolicy.Off;
                    }

                case MitigationPolicy.OptOut:
                    if (policy == MitigationPolicy.Off)
                    {
                        return policy;
                    }
                    else
                    {
                        return MitigationPolicy.On;
                    }

                default:
                    throw new NotSupportedException();
            }
        }

        public static MitigationPolicy? EffectivePolicy(this MitigationPolicy? policy, MitigationPolicy? basePolicy)
        {
            if (policy == null)
            {
                return basePolicy;
            }
            else if (basePolicy == null)
            {
                return policy;
            }
            else
            {
                return policy.Value.EffectivePolicy(basePolicy.Value);
            }
        }
    }

    public class MemoryAddress
    {
        public readonly static MemoryAddress AddressOfStackFramePointer =
            new MemoryAddress(MemoryContentDataType.StackFramePointer, MemoryRegionType.Stack);
        public readonly static MemoryAddress AddressOfStackReturnAddress =
            new MemoryAddress(MemoryContentDataType.StackReturnAddress, MemoryRegionType.Stack);
        public readonly static MemoryAddress AddressOfStackStructuredExceptionHandler =
            new MemoryAddress(MemoryContentDataType.StackStructuredExceptionHandlerFunctionPointer, MemoryRegionType.Stack);
        public readonly static MemoryAddress AddressOfStackProtectionCookie =
            new MemoryAddress(MemoryContentDataType.StackProtectionCookie, MemoryRegionType.Stack);

        public readonly static MemoryAddress AddressOfWritableCode =
            new MemoryAddress(MemoryContentDataType.WritableCode);

        public readonly static MemoryAddress AddressOfAttackerControlledCode =
            new MemoryAddress(MemoryContentDataType.AttackerControlledCode);
        public readonly static MemoryAddress AddressOfAttackerControlledData =
            new MemoryAddress(MemoryContentDataType.AttackerControlledData);

        public readonly static MemoryAddress AddressOfNtdllImageBase =
            new MemoryAddress(MemoryContentDataType.Code, MemoryRegionType.ImageCodeSegmentNtdll);

        public readonly static MemoryAddress[] ROPGadgetCodeAddresses =
            new[]
            {
                new MemoryAddress(MemoryContentDataType.Code, MemoryRegionType.ImageCodeSegment),
                new MemoryAddress(MemoryContentDataType.Code, MemoryRegionType.JITCode),
            };

        public readonly static MemoryRegionType[] WritableRegionTypes =
            new[]
            {
                MemoryRegionType.Any,
                MemoryRegionType.Heap,
                MemoryRegionType.Stack
            };

        public MemoryAddress(MemoryContentDataType dataType, MemoryRegionType? region = null)
        {
            this.DataType = dataType;

            if (region != null)
            {
                this.Region = region.Value;
            }
            else
            {
                this.Region = MemoryRegionType.Any;
            }
        }

        public MemoryContentDataType DataType { get; private set; }
        public MemoryRegionType Region { get; private set; }

        public override int GetHashCode()
        {
            return String.Format("{0}.{1}", this.DataType, this.Region).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is MemoryAddress)
            {
                MemoryAddress objm = obj as MemoryAddress;

                return objm.DataType == this.DataType && objm.Region == this.Region;
            }

            return false;
        }

        public override string ToString()
        {
            return String.Format("&{0}.{1}", this.Region, this.DataType); 
        }

        public bool IsKernelAddress
        {
            get
            {
                switch (this.Region)
                {
                    case MemoryRegionType.Null:
                        return true;

                    default:
                        return false;
                }
            }
        }

        public bool IsImplicitlyInitialized
        {
            get
            {
                switch (this.Region)
                {
                    case MemoryRegionType.Stack:
                        switch (this.DataType)
                        {
                            case MemoryContentDataType.StackFramePointer:
                            case MemoryContentDataType.StackReturnAddress:
                            case MemoryContentDataType.StackStructuredExceptionHandlerFunctionPointer:
                            case MemoryContentDataType.StackProtectionCookie:
                                return true;

                            default:
                                break;
                        }
                        break;

                    case MemoryRegionType.ImageCodeSegment:
                        return true;

                    default:
                        break;
                }

                return false;
            }
        }
    }

    public static class MemoryAddressExtension
    {
        // MemoryAddress should be a class?
        public static MemoryAddress GetRegionSpecificAddress(this MemoryAddress address, MemoryRegionType? region)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Maps a memory address to regions that contain it.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static IEnumerable<MemoryRegion> MapToMemoryRegions(this MemoryAddress address, ExecutionDomain? domain)
        {
            throw new NotImplementedException();
#if false
            if (domain.IsUser())
            {
                switch (address.AddressType)
                {
                    //
                    // Stack return address.
                    //
                    case MemoryAddress.AddressOfStackReturnAddress:
                    case MemoryAddress.AddressOfStackStructuredExceptionHandler:
                    case MemoryAddress.AddressOfStackFramePointer:
                        yield return MemoryRegion.UserThreadStack;
                        break;

                    //
                    // Stack return address.
                    //
                    case MemoryAddress.Data:
                        foreach (MemoryRegion region in MemoryRegions.UserWritableRegions)
                        {
                            yield return region;
                        }
                        break;

                    default:
                        throw new NotSupportedException(String.Format("Memory address <-> region mapping not defined for {0}", address));
                }
            }
            else
            {
                throw new NotImplementedException();
            }
#endif
        }
    }

    public enum MemoryRegion
    {
        //
        // Kernel mode memory regions.
        //

        KernelInitialThreadStack,
        KernelThreadStack,
        KernelPagedPool,
        KernelNonPagedPool,
        KernelSessionPool,
        KernelPageTablePages,
        KernelDriverImage,
        KernelExeImage,
        KernelHyperspace,
        KernelPCR,
        KernelSharedUserData,
        KernelSystemPTE,
        KernelSystemCache,
        KernelPFNDatabase,
        KernelHALReserved,

        //
        // User mode memory regions.
        //

        UserProcessHeap,
        UserThreadStack,
        UserPEB,
        UserTEB,
        UserVirtualAllocBU,
        UserVirtualAllocBUHE,
        UserVirtualAllocTD,
        UserJITCode,

        UserExeImageBase,
        UserExeImageCode,
        UserExeImageData,

        UserDllImageBase,
        UserDllImageCode,
        UserDllImageData,

        UserForceRelocatedImageBase,
        UserForceRelocatedImageCode,
        UserForceRelocatedImageData,

        UserSharedUserData
    }

    public static class MemoryRegions
    {
        public static MemoryRegion[] UserMode
        {
            get
            {
                return new MemoryRegion[] {
                    MemoryRegion.UserSharedUserData,
                    MemoryRegion.UserProcessHeap,
                    MemoryRegion.UserThreadStack,
                    MemoryRegion.UserPEB,
                    MemoryRegion.UserTEB,
                    MemoryRegion.UserVirtualAllocBU,
                    MemoryRegion.UserVirtualAllocTD,
                    MemoryRegion.UserExeImageBase,
                    MemoryRegion.UserExeImageCode,
                    MemoryRegion.UserExeImageData,
                    MemoryRegion.UserDllImageBase,
                    MemoryRegion.UserDllImageCode,
                    MemoryRegion.UserDllImageData,
                    MemoryRegion.UserForceRelocatedImageBase,
                    MemoryRegion.UserForceRelocatedImageCode,
                    MemoryRegion.UserForceRelocatedImageData,
                };
            }
        }

        public static MemoryRegion[] BottomUp
        {
            get
            {
                return new MemoryRegion[] {
                    MemoryRegion.UserVirtualAllocBU,
                    MemoryRegion.UserProcessHeap,
                    MemoryRegion.UserForceRelocatedImageBase
                };
            }
        }

        public static IEnumerable<MemoryRegion> UserWritableRegions
        {
            get
            {
                return new MemoryRegion[] {
                    MemoryRegion.UserProcessHeap,
                    MemoryRegion.UserThreadStack,
                    MemoryRegion.UserPEB,
                    MemoryRegion.UserTEB,
                    MemoryRegion.UserVirtualAllocBU,
                    MemoryRegion.UserVirtualAllocTD,
                    MemoryRegion.UserExeImageData,
                    MemoryRegion.UserDllImageData
                };
            }
        }

        public static IEnumerable<MemoryRegion> UserCodeRegions
        {
            get
            {
                // JIT code?
                return new MemoryRegion[] {
                    MemoryRegion.UserExeImageCode,
                    MemoryRegion.UserDllImageCode,
                    MemoryRegion.UserJITCode
                };
            }
        }

        public static IEnumerable<MemoryRegion> UserImageBaseRegions
        {
            get
            {
                return new MemoryRegion[] {
                    MemoryRegion.UserExeImageBase,
                    MemoryRegion.UserDllImageBase
                };
            }
        }
    }

    public enum HeapFeature
    {
        HeapFreeSafeUnlinking,
        HeapTerminateOnCorruption,
        HeapAllocationOrderRandomization,

        HeapBlockHeaderCookies,
        HeapBlockHeaderEncryption,
        HeapPreventFreeHeapBase,
        HeapBusyBlockIntegrityCheck,
        HeapSegmentReserveGuardPage,
        HeapLargeAllocationAlignment,
        HeapEncodeCommitRoutineWithPointerKey,
        HeapEncodeCommitRoutineWithGlobalKey,


        //
        // Features specific to the kernel pool.
        //

        KernelPoolQuotaPointerEncoding,
        KernelPoolLookasideListCookie
    }
}
