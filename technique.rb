# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
module ExSim

# compute desirability lattice by simulation and ordering based on average exploitability involving 
# a technique?
class Lattice

  def initialize(&block)
    @least_upper_bounds = {}
    @greatest_lower_bounds = {}
    @top = nil
    @bottom = nil

    instance_eval(&block) if block
  end

  def top=(n)
    @top = n
  end

  def bottom=(n)
    @bottom = n
  end

  def add_least_upper_bound(n, *upper_bounds)
    upper_bounds.each do |ub|
      @least_upper_bounds[n] = [] if @least_upper_bounds[n].nil?
      @least_upper_bounds[n] << ub
    end
  end

  def add_greatest_lower_bound(n, *lower_bounds)
    lower_bounds.each do |ub|
      @greatest_lower_bounds[n] = [] if @greatest_lower_bounds[n].nil?
      @greatest_lower_bounds[n] << ub
    end
  end

end

class Technique

  def initialize
    @citations = []
  end

  def cite(info)
    @citations = [] if @citations.nil?
    @citations << info
  end

  def desc
    nil
  end

private

end

class ReturnAddressOverwrite < Technique

  def initialize
    cite(
      :author => 'aleph1', 
      :title  => '', 
      :url    => '')
  end

end

class PartialReturnAddressOverwrite < Technique
end

class FramePointerOverwrite < Technique
end

class StructuredExceptionHandlerOverwrite < Technique
end

class StackVariableOverwrite < Technique
end

class FunctionPointerOverwrite < Technique
end

class WriteTargetPointerOverwrite < Technique
end

class CppObjectVtablePointerOverwrite < Technique
end

class HeapLFHLinkOffsetOverwrite < Technique
end

class HeapUnlinkOverwrite < Technique
end

class HeapApplicationDataOverwrite < Technique
end

class HeapHandleFreeAndReallocate < Technique
end

class HeapHandleCommitRoutineOverwrite < Technique
end

class BypassNxViaNtSetInformationProcess < Technique
end

class BypassNxViaStageToExecutableCrtHeap < Technique
end

class BypassNxViaStageToVirtualProtectOrAlloc < Technique
end

class BypassNxViaRopStageToVirtualProtectOrAlloc < Technique
end

class PivotStackPointer < Technique
end

class CodeExecutionViaSelfContainedRopPayload < Technique
end

class NullMap < Technique
end

class HeapPrematureFree < Technique
end

class LoadNonASLRDLL < Technique
end

class LoadNonASLRNonSafeSEHDLL < Technique
end

class SprayData < Technique
end

class SprayCode < Technique
end

end
