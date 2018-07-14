# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
#
# The exploitability simulator
#
# This simulator can be used to model the characteristics of exploitation
# techniques under a variety circumstances.
#
# The following conditions can be customized to broadly define the properties
# of the target being exploited:
#
#   Environmental properties:
#     - Hardware         (ex: p3, p4, x64, ...)
#     - Operating system (ex: win_vista_rtm, win_2000_sp4, ...)
#     - Application      (ex: ie6, ie8, svchost, any, ...)
#
#   Flaw properties:
#     - Flaw             (ex: stack memory corruption, gs enabled, ...)
#
# The characteristics that are measured for each exploitation simulation include:
#  
#   - Exploitability
#     The chance of successfully exploiting the flaw.
#
#   - Desirability
#     The attractiveness of using a technique based on subjective perceptions
#     and chance of success.
#
#   - Likelihood
#     The subjective likelihood that a required assumption or predicate will be
#     true for a given technique.
#
#   - Homogeneity
#     The population that is likely to be successfully exploited.  A function
#     of population size and exploitability.
#
#   - Fitness
#     The fitness (or fitness) of an exploitation strategy.  A function
#     of exploitability, desirability, and likelihood.
#
# The term optimistic is used to define a view-point where things are decided
# in the attacker's favor, thus increasing the chance of successful
# exploitation.  The term pesimistic is dually defined.
#
# mamill
# 12/2008
#
require 'set'
require 'statemachine'
require 'env'
require 'technique'

Undesirable = 0.10
Desirable   = 1.0

Unlikely    = 0.10
Likely      = 1.0

#
# Modifications to the Statemachine interface to facilitate automatic
# simulation.
#

module Statemachine
class Statemachine
  attr_accessor :autosimulate
  attr_accessor :simulation_stack
  def state
    @state
  end
  def states
    @states
  end
  def transitions
    @transitions
  end
  def simulate
    @autosimulate = true
    @simulation_stack = []
    @state.enter if @state
  end
end
class State
  def enter(args=[])
    @statemachine.trace("\tentering #{self}")
    @statemachine.invoke_action(@entry_action, args, "entry action for #{self}") if @entry_action

    # If we should auto simulate all of the events in the state machine,
    # then do so now
    if @statemachine.autosimulate
      # Duplicate the state machine context
      orig_context = @statemachine.context
      orig_state   = @statemachine.state

      transitions.each_pair do |event, transition|

        next if @statemachine.simulation_stack.member?(transition)

        @statemachine.simulation_stack.push(transition)
        @statemachine.context = orig_context.dup
        @statemachine.context.current_transition = transition
        @statemachine.context.current_event = event

        # Process the event
        begin
          @statemachine.process_event(event, args)
        rescue StatemachineException => ex
          if @statemachine.context.track_impossible
            @statemachine.context.transitions = @statemachine.simulation_stack.dup
            @statemachine.context.aborted = true
            @statemachine.context.abort_reason = ex.abort_reason
            @statemachine.context.global.add_simulation @statemachine.context
          end
        end

        # Restore the original context and state for the next iteration
        @statemachine.simulation_stack.pop

        @statemachine.context = orig_context
        @statemachine.state  = orig_state
      end       
    end
  end
end
end

module ExSim

# A predicate was not satisfied, abort the statemachine transition
class PredicateNotSatisfied < Statemachine::StatemachineException
  def initialize(predicate)
    @predicate = predicate
  end
  def abort_reason
    (@predicate || '').to_s
  end
  attr_reader :predicate
end

# Global context shared across all branches of a given simulation
class GlobalSimulationContext

  def initialize
    @track_equivalent_only = false
    @track_minimal_only = false
    @simulations = {}
    @simulation_count = 0
  end

  def simulations_sorted
    sorted_values = @simulations.values.sort do |x, y|
      [
        y[:fitness],
        x[:simulation].transitions.length
      ] <=> [
        x[:fitness],
        y[:simulation].transitions.length
      ]
    end

    if @track_equivalent_only
      sorted_values.map do |val|
        val[:simulation].equivalence_count = val[:member_count]
        val[:simulation]
      end
    else
      sims = []
      sorted_values.each do |val|
        sims = sims + val[:simulations]
      end
      sims
    end
  end

  def min_fitness
    min { |s| s.fitness}
  end

  def min_exploitability
    min { |s| s.exploitability }
  end

  def min_desirability
    min { |s| s.desirability }
  end

  def min_likelihood
    min { |s| s.likelihood }
  end

  def min_homogeneity
    min { |s| s.homogeneity }
  end

  def max_fitness
    max { |s| s.fitness }
  end

  def max_exploitability
    max { |s| s.exploitability }
  end

  def max_desirability
    max { |s| s.desirability }
  end

  def max_likelihood
    max { |s| s.likelihood }
  end

  def max_homogeneity
    max { |s| s.homogeneity }
  end

  def avg_fitness
    avg { |s| s.fitness}
  end

  def avg_exploitability
    avg { |s| s.exploitability }
  end

  def avg_desirability
    avg { |s| s.desirability }
  end

  def avg_likelihood
    avg { |s| s.likelihood }
  end

  def avg_homogeneity
    avg { |s| s.homogeneity }
  end

  #
  # Add the simulation
  #
  def add_simulation(sim)
    return if @track_minimal_only and sim.is_minimal? == false

    eqid = sim.equivalence_id

    if @simulations.has_key? eqid
      @simulation_count += 1
      @simulations[eqid][:member_count] += 1

      if @track_equivalent_only == false
        @simulations[eqid][:simulations] << sim
      end
    else
      @simulation_count = 1
      @simulations[eqid] = {
        :fitness      => sim.fitness,
        :simulation   => sim,
        :member_count => 1
      }

      if @track_equivalent_only == false
        @simulations[eqid][:simulations] = [ sim ]
      end
    end

  end

  attr_accessor :track_equivalent_only
  attr_accessor :track_minimal_only

  def each_simulation(&block)
    @simulations.values.each do |simgroup|
      if @track_equivalent_only
        yield simgroup[:simulation]
      else
        simgroup[:simulations].each do |sim|
          yield sim
        end
      end
    end
  end

  def exploitability_range_counts
    counts = {}
    ranges = [ 0, 0.0001, 0.001, 0.01, 0.10, 0.25, 0.50, 0.75, 0.99, 1.0 ]

    each_simulation do |simulation|
      match_range = nil
      prev_range = 0
      ranges.each do |range|

        if simulation.exploitability <= range
          match_range = range
          break
        end
        prev_range = range
      end

      if match_range.nil?
        match_range = 0
      end

      if counts[match_range].nil?
        counts[match_range] = {}
        counts[match_range][:total_count] = 0
        counts[match_range][:equiv_count] = 0
        counts[match_range][:prev_range] = prev_range
      end

      counts[match_range][:total_count] += @simulations[simulation.equivalence_id][:member_count]
      counts[match_range][:equiv_count] += 1

    end

    counts.to_a.sort do |x, y|
      y[0] <=> x[0]
    end

  end

  attr_reader :simulation_count

private

  attr_accessor :simulations

  def avg(&block)
    return 0.0 if @simulations.length == 0

    average = 0.0
    cnt = 0
    each_simulation do |simulation|
      average += block.call(simulation)
      cnt += 1
    end
    average /= cnt
  end

  def min(&block)
    return 0.0 if @simulations.length == 0

    min = nil
    each_simulation do |simulation|
      value = block.call(simulation)
      if min.nil? or value < min
        min = value
      end
    end
    min
  end

  def max(&block)
    return 0.0 if @simulations.length == 0

    max = nil
    each_simulation do |simulation|
      value = block.call(simulation)
      if max.nil? or value > max
        max = value
      end
    end
    max
  end


end

#
# A context that is unique to a specific path taken by an 
# exploitation strategy during simulation.
#
class SimulationContext

  def dup
    instance = super
    instance.assumptions = @assumptions.dup
    instance.techniques = @techniques.dup
    instance
  end

  def initialize(global = GlobalSimulationContext.new)
    @global = global

    @target = Target.new

    @allow_impossible = false
    @track_impossible = false 
    @debug = false
    @modes = []

    @exploitability = 1.0
    @desirability = 1.0
    @likelihood = 1.0

    @assumptions = {}
    @techniques = Set.new
    @assumption_id = 0
    @predicate_id = 0

    #
    # By default we assume that things will go in the attacker's favor.
    #

    @pesimistic = false
    @optimistic = true
  end

  SimulationModes = 
    [
      :attack_favor,
      :defense_favor,
      :public_only,
      :normal
    ]

  @@evtcounts = {}
  @@debugcount = 0

  def target_detail_to_s
    str = ''
    Target.each_bit_desc do |bit, desc|
      str << "#{desc[:name].to_s.ljust(50)}: #{desc[:get].call(self)}\n"
    end
    str
  end

  def target_detail_to_csv
    str = ''
    Target.each_bit_desc do |bit, desc|
      val = desc[:get].call(self)
      val = 1 if val == true
      val = 0 if val == false

      str << "," if str.length > 0
      str << "#{val}"
    end
    str
  end

  def genkey(sym, *args)
    return "#{sym}(#{args.map {|x| x.to_s }.join(",")})"
  end

  def predicate(sym, *args, &block)
    if @debug 
      evt = "#{self.current_event}"
      @@evtcounts[evt] = 0 if @@evtcounts[evt].nil?
      @@evtcounts[evt] += 1
      @@debugcount += 1 
      
      if ((@@debugcount + 1) % 200000) == 0
        cache = @@evtcounts.keys.sort do |x, y|
          @@evtcounts[x] <=> @@evtcounts[y]
        end
        cache.each do |cacheevt|
          puts "#{cacheevt}: #{@@evtcounts[cacheevt]}"
        end
      end
    end

    key = genkey(sym, *args)

    # If we've already evaluated this predicate, then don't do so again
    # as we do not want to factor it into exploitability multiple times
    return get_assumption(key)[:rv] if has_assumption(key)

    # Evaluate the predicate
    if block.nil?
      rv = send(sym, *args)
    else
      rv = block.call(*args)
    end

    # Translate values
    if rv == true
      rv = 1.0
    elsif rv == false
      rv = 0.0
    end

    # If the predicate returns zero, then it was not satisfied.  We can now
    # abort the simulation because we have reached an impossible condition.
    if rv == 0 and @allow_impossible == false
      @exploitability = 0.0
      raise PredicateNotSatisfied.new(sym) 
    end

    # Create a new assumption based on the answer to the predicate
    new_assumption(
      key,
      :id         => @assumption_id += 1,
      :rv         => rv,
      :predicated => true,
      :transition => self.current_transition,
      :event      => self.current_event)

    # Adjust the effective exploitability based on the degree
    # to which the predicate was satisfied.
    @exploitability *= rv

    rv
  end

  def predicate_nocache(sym, *args, &block)
    key = genkey(sym, *args)

    rv = predicate(sym, *args, &block)

    @assumptions.delete(key)

    rv
  end

  #
  # Associates the current event with a technique.
  #
  def technique(technique_class)
    @techniques << technique_class if @techniques.member?(technique_class) == false
  end

  #
  # A citation
  #
  def cite(name, opts = {})
  end

  #
  # An opaque note associated with a transition.
  #
  def note(note)
    # no-op currently
  end

  # Checks to see if a condition is assumed
  def assumes?(sym, *args)
    key = genkey(sym, *args)
    not get_assumption(key).nil?
  end

  def assumed_true?(sym, *args)
    key = genkey(sym, *args)
    get_assumption(key).nil? == false and get_assumption(key)[:rv] == 1.0
  end

  def assumed_false?(sym, *args)
    key = genkey(sym, *args)
    get_assumption(key).nil? == false and get_assumption(key)[:rv] == 0.0
  end

  def assumed_false_or_nil?(sym, *args)
    key = genkey(sym, *args)
    get_assumption(key).nil? or assumed_false?(sym, *args)
  end

  # Get the assumption that corresponds to the provided key and flag it as
  # having been used.
  def get_assumption(key)
    a = @assumptions[key]
    
    if a
      a[:used] = true
    end

    a
  end

  def has_assumption(key)
    @assumptions[key]
  end

  # Assume the degree to which a condition is satisfied
  def assume(sym, *args, &block)
    key = genkey(sym, *args)

    return if @assumptions[key]

    # Evaluate the assumption
    if block.nil? and respond_to?(sym)
      rv = send(sym, *args)
    elsif block
      rv = block.call(*args)
    else
      rv = args.length == 1 ? args[0] : args
    end
  
    # Translate values
    if rv == true
      rv = 1.0
    elsif rv == false
      rv = 0.0
    end

    new_assumption(
      key,
      :id         => @assumption_id += 1,
      :rv         => rv,
      :predicated => false,
      :transition => self.current_transition,
      :event      => self.current_event)

    # Adjust the effective exploitability based on the degree
    # to which the assumption is satisfied.
    @exploitability *= rv

    rv
  end

  def new_assumption(key, hash = {})
    @assumptions[key] = hash
  end

  def explicitly_assume(sym, *args)
    assume(sym, *args) do
      1.0
    end
  end

  def assume_true(sym, *args)
    assume(sym, *args) do
      1.0
    end
  end

  def assume_zero(sym, *args)
    assume(sym, *args) do
      0.0
    end
  end

  def inorder_assumptions
    @assumptions.sort do |x, y|
      x[1][:id] <=> y[1][:id]
    end
  end

  # A simulation is minimal if all transitions contribute at least
  # one assumption that is later predicated upon.
  def is_minimal?

    # Determine if there were any pre-exploitation transitions that had assumptions
    # which were never used to enable exploitation (meaning they are superfluous).
    transition_used = {}

    @assumptions.values.each do |assumption|
      transition = assumption[:transition]

      next if transition.origin_id != :preparing_environment

      transition_used[transition] = false if transition_used[transition].nil?

      if assumption[:used]
        transition_used[transition] = true
      end
    end

    # If this sequence does not include any pre-exploitation transitions that
    # contribute no value, then we consider this simulation to be 'minimal'.
    not transition_used.values.include?(false)
  end

  # The global simulation context
  attr_reader   :global

  attr_accessor :abort_reason
  attr_accessor :aborted

  attr_accessor :context_id
  attr_accessor :allow_impossible
  attr_accessor :track_impossible
  attr_accessor :track_equivalent_only
  attr_accessor :track_minimal_only
  attr_accessor :debug
  attr_accessor :modes
  attr_accessor :statemachine
  attr_accessor :transitions
  attr_accessor :assumptions
  attr_accessor :techniques

  attr_accessor :equivalence_count

  attr_accessor :current_transition
  attr_accessor :current_event

  # The target configuration.
  attr_accessor :target

  def fitness
    exploitability * desirability * likelihood
  end

  # Likelihood describes the likelihood of certain conditions occurring in practice
  def likelihood(n = nil)
    key = genkey(:likelihood)

    return @assumptions[key][:rv] if @assumptions[key]

    if n
      @likelihood *= n
    end
    @likelihood
  end

  # Desirabile gives a measure of ease-of-attack where easy attacks
  # are more desirable than those that are not
  def desirability(n = nil)
    key = genkey(:desirability)

    return @assumptions[key][:rv] if @assumptions[key]

    if n
      @desirability *= n
    end
    @desirability
  end

  # Exploitability describes the expected chance of successful exploitation
  def exploitability(n = nil)
    if n
      @exploitability *= n
    end
    @exploitability
  end

  def population
    if @population
      return @population
    else
      target.hw.population * target.os.population * target.app.population
    end
  end

  def population=(x)
    @population = x
  end

  #
  # Two simulations are considered equivalent if their fitness values are the
  # same and they both employ the same exploitation techniques (or a subset
  # thereof).
  #
  def equivalent(sim)
    sim.equivalence_id == self.equivalence_id
  end

  def equivalence_id
    [ self.fitness, self.techniques ]
  end

  def mode_is_attack_favor?
    @modes.include? :attack_favor
  end

  def mode_is_defense_favor?
    @modes.include? :defense_favor
  end

  def mode_is_public_only?
    @modes.include? :public_only
  end

  def mode_is_normal?
    @modes.include? :normal
  end

  def attacker_favors_true(v = nil)
    if v.nil?
      if mode_is_attack_favor?
        true
      elsif mode_is_defense_favor?
        false
      else
        v
      end
    else
      v
    end
  end

  def attacker_favors_false(v = nil)
    if v.nil?
      if mode_is_attack_favor?
        false
      elsif mode_is_defense_favor?
        true
      else
        v
      end
    else
      v
    end
  end

  def normally_true(v = nil)
    if mode_is_normal? and v.nil?
      true
    else
      v
    end
  end

  def normally_false(v = nil)
    if mode_is_normal? and v.nil?
      false
    else
      v
    end
  end

  #
  # The current measure of homogeneity (e.g. the population density affected)
  #
  def homogeneity
    population * @exploitability    
  end

  ###
  ###
  ### Predicate helper routines
  ###
  ###

  #
  # True if the attacker is able to leak an address within the provided virtual address region.
  #
  def can_discover_address(va)
    case va
    when :stack   
      attacker_favors_true target.cap.can_discover_stack_address
    when :heap    
      attacker_favors_true target.cap.can_discover_heap_address
    when :heapbase
      attacker_favors_true target.cap.can_discover_heap_base_address
    when :peb    
      attacker_favors_true target.cap.can_discover_peb_address
    when :image   
      attacker_favors_true target.cap.can_discover_image_address
    when :ntdll
      attacker_favors_true target.cap.can_discover_ntdll_image_address
    when :nonsafeseh
      attacker_favors_true target.cap.can_discover_non_safeseh_image_address
    else false
    end
  end

  #
  # Determines if the attacker can discover the stack cookie value.
  #
  def can_discover_stack_cookie
    attacker_favors_true(normally_false(target.flaw.can_discover_stack_cookie))
  end

  #
  # Determines if the attacker can discover the vtguard cookie value.
  #
  def can_discover_vtguard_cookie
    attacker_favors_true(normally_false(target.flaw.can_discover_vtguard_cookie))
  end

  #
  # The effective number of bits of entropy for the provided virtual address region (e.g. stack
  # heap, or image).  
  #
  def aslr_entropy_bits(va)
    (
     if is_kernel_app
       case va
       when :stack
         target.os.kernel_aslr_entropy_stacks
       when :image
         bits = target.os.kernel_aslr_entropy_kernel_images

         if bits.nil? or (target.os.kernel_aslr_entropy_driver_images and bits > target.os.kernel_aslr_entropy_driver_images)
           bits = target.os.kernel_aslr_entropy_driver_images
         end

         bits
       else
         0.0
       end
     else
       case va
       when :stack
         target.os.user_aslr_entropy_stacks
       when :heap, :heapbase
         target.os.user_aslr_entropy_heaps
       when :peb
         target.os.user_aslr_entropy_peb
       when :bottom_up
         target.os.user_aslr_entropy_bottom_up
       when :image, :vtimage
         bits = target.os.user_aslr_entropy_exe_images

         if bits.nil? or (target.os.user_aslr_entropy_lib_images and bits > target.os.user_aslr_entropy_lib_images)
           bits = target.os.user_aslr_entropy_lib_images
         end

         bits
       when :force_relocate_image
         target.os.user_aslr_entropy_force_relocation
       else
         0.0
       end
     end
    ) || 0.0
  end

  #
  # The minimum number of entropy bits given a list of virtual address regions.
  #
  def min_aslr_entropy_bits(*va_list)
    min_bits = nil

    va_list.each { |va|
      va_bits = aslr_entropy_bits(va)
      min_bits = va_bits if min_bits.nil? or va_bits < min_bits
    }

    min_bits || 0.0
  end

  #
  # Determines the degree to which ASLR inhibits discovering the an address
  # within the provided virtual address region (such as an executable image)
  # and returns the probability of ASLR inhibiting the guessing of an address
  # where 0.0 means ASLR will never inhibit and 1.0 means ASLR will always inhibit.
  #
  def aslr_inhibition_degree(va)

    case va

      #
      # ASLR does not inhibit the finding of a stack, heap, or PEB/TEB if their
      # address can be leaked.
      #
      when :stack, :heap, :heapbase, :peb
        if can_discover_address(va)
          0.0
        elsif aslr_entropy_bits(va) > 0
          (1.0 - (1.0 / (2 ** aslr_entropy_bits(va))))
        else
          0.0
        end

      when :force_relocate_image
        if aslr_entropy_bits(va) > 0
          (1.0 - (1.0 / (2 ** aslr_entropy_bits(va))))
        else
          0.0
        end

      #
      # ASLR does not inhibit the finding of writable data if the attacker can
      # spray data bottom up or if they can leak the address of a writable
      # memory region
      #
      when :data
        if (assumed_true? :can_find_desired_data or
            attacker_favors_true(target.cap.can_spray_data_bottom_up) or 
            can_discover_address :stack or 
            can_discover_address :heap or 
            can_discover_address :image)
          0.0
        elsif min_aslr_entropy_bits(:stack, :heap, :image) > 0
          (1.0 - (1.0 / (2 ** min_aslr_entropy_bits(:stack, :heap, :image))))
        else
          0.0
        end

      #
      # ASLR does not inhibit the finding of code if this is a local flaw, the attacker
      # can spray code bottom up, or the attacker is able to leak the address of an image.
      #
      when :code, :image
        if (is_local_flaw or 
            assumed_true? :can_find_desired_code or 
            assumed_true? :can_find_attacker_controlled_code or 
            attacker_favors_true(normally_false(target.cap.can_spray_code_bottom_up)) or 
            can_discover_address(:image))
          0.0
        elsif aslr_entropy_bits(:image) > 0
          (1.0 - (1.0 / (2 ** aslr_entropy_bits(:image))))
        else
          0.0
        end

      when :vtimage
        if aslr_entropy_bits(:image) > 0
          (1.0 - (1.0 / (2 ** aslr_entropy_bits(:image))))
        else
          0.0
        end

      #
      # The inhibition degree for a specific image.
      #
      when /^image/
        if va == 'image:ntdll' and can_discover_address :ntdll
          0.0
        elsif is_local_flaw or can_discover_address :image
          0.0
        elsif aslr_entropy_bits(:image) > 0
          (1.0 - (1.0 / (2 ** aslr_entropy_bits(:image))))
        else
          0.0
        end
      
      else 0.0
    end

  end

  #
  # Determines if the attacker can trigger an exception during the course of
  # exploiting this flaw.  This is is needed in order to be able to exploit
  # an SEH overwrite.
  #
  def can_trigger_seh_exception
    attacker_favors_true(normally_true(target.flaw.can_trigger_seh_exception))
  end

  #
  # User mode NX is enabled if the hardware, operating system, and application
  # have all enabled NX.
  #
  def user_nx_enabled
    enabled = target.app.user_nx_enabled

    if enabled and target.os.user_nx_supported == false
      enabled = false
    elsif enabled.nil?
      enabled = false
    end

    if enabled and target.hw.nx_enabled == false
      enabled = false
    elsif enabled.nil?
      enabled = target.hw.nx_enabled
    end

    #
    # If NX has not been explicitly enabled or disabled, then we consider it to
    # be enabled if and only if the hardware and os support NX.  
    #

    if enabled.nil?
      attacker_favors_false and target.hw.nx_supported and target.os.nx_supported
    else
      enabled
    end
  end

  #
  # True if SEHOP is effectively enabled in the current context.
  #
  def user_sehop_enabled
    (attacker_favors_false(target.app.user_sehop_enabled) and target.os.user_sehop_supported) or 
    (attacker_favors_false(target.os.user_sehop_enabled))
  end

  #
  # Boolean helper routines
  #

  #
  # True if the target flaw represents a traditional form of the flaw.  An example
  # of a forward linear corruption flaw is a linear buffer overrun.  An example of a non-traditional
  # flaw is a buffer overrun that is able to write at an desired offset.
  #
  def is_forward_linear_corruption_flaw
    target.flaw.classification == :corruption and target.flaw.corruption_direction == :forward and target.flaw.corruption_pattern = :linear
  end

  #
  # True if the target flaw is triggered locally
  #
  def is_local_flaw
    if target.flaw.local.nil?
      attacker_favors_true
    else
      target.flaw.local
    end
  end

  #
  # True if the target flaw is triggered remotely
  #
  def is_remote_flaw
    if target.flaw.remote.nil?
      attacker_favors_true
    else
      target.flaw.remote
    end
  end

  #
  # True if the target application is a kernel application (e.g. driver)
  #
  def is_kernel_app
    if target.app.kernel.nil?
      attacker_favors_true
    else
      target.app.kernel
    end
  end

  #
  # True if this is a kernel mode flaw.
  #
  def is_kernel_flaw
    is_kernel_app
  end

  #
  # True if the target application is a user application
  #
  def is_user_app
    if target.app.kernel.nil?
      attacker_favors_true
    else
      target.app.kernel == false
    end
  end

  #
  # True if this is a user mode flaw.
  #
  def is_user_flaw
    is_user_app
  end

  #
  # True if this is a remote kernel flaw.
  #
  def is_remote_kernel_flaw
    is_remote_flaw and is_kernel_flaw
  end

  #
  # True if this a local kernel flaw.
  #
  def is_local_kernel_flaw
    is_local_flaw and is_kernel_flaw
  end

  #
  # True if this is a remote user flaw.
  #
  def is_remote_user_flaw
    is_remote_flaw and is_user_flaw
  end

  #
  # True if this is a local user flaw, such as a flaw in a privileged service.
  #
  def is_local_user_flaw
    is_local_flaw and is_user_flaw
  end

  #
  # True if this application is executing 
  #
  def is_x86_app
    target.app.address_bits == 32 and target.hw.kind_of? Hardware::X86
  end

  #
  # True if the target operating system's family is Windows.
  #
  def is_windows_os
    target.os.kind_of? OS::Windows
  end

  #
  # True if this is a memory corruption flaw
  #
  def is_flaw_memory_corruption
    if target.flaw.classification.nil?
      attacker_favors_true
    else
      target.flaw.classification == :write
    end
  end

  #
  # True if the flaw is stack memory corruption
  #
  def is_flaw_stack_memory_corruption
    if target.flaw.corruption_region.nil? and target.flaw.classification == :write
      attacker_favors_true
    else
      target.flaw.corruption_region == :stack or 
      (can_write_anywhere and predicate(:can_find_address, :stack) > 0)
    end
  end

  #
  # True if the flaw is a heap memory corruption.
  #
  def is_flaw_heap_memory_corruption
    if target.flaw.corruption_region.nil? and target.flaw.classification == :write
      attacker_favors_true
    else
      target.flaw.corruption_region == :heap or
      (can_write_anywhere and predicate(:can_find_address, :heap) > 0)
    end
  end

  #
  # True if the flaw is an arbitrary memory corruption.
  #
  def is_flaw_arbitrary_memory_corruption
    if target.flaw.corruption_region.nil? and target.flaw.classification == :write
      attacker_favors_true
    else
      target.flaw.corruption_region == :any
    end
  end

  #
  # True if this is considered a control transfer flaw.
  #
  def is_control_transfer_flaw
    target.flaw.classification == :control_transfer
  end

  #
  # True if this is a null dereference flaw.
  #
  def is_null_dereference_flaw
    target.flaw.root_cause == :null_dereference
  end

  def all_policies_enabled?(*policies)
    policies.each do |policy|
      if policy == :off
        return false
      end
    end

    return true
  end

  ###
  ###
  ### Predicates
  ###
  ###

  #
  # Determines if the attacker is able to map the NULL page (e.g. the OS
  # doesn't prevent this from happening).
  #
  def can_map_null_page
    if target.os.kernel_null_deref_prevention_enabled.nil?
      attacker_favors_true
    else
      target.os.kernel_null_deref_prevention_enabled == false
    end
  end

  def is_aslr_images_enabled
    attacker_favors_false(target.os.user_aslr_supported) and all_policies_enabled?(target.os.user_aslr_policy_exe_images, target.os.user_aslr_policy_lib_images)
  end

  def is_aslr_force_relocation_enabled
    attacker_favors_false(target.os.user_aslr_force_relocation_supported) and all_policies_enabled?(target.app.user_aslr_policy_force_relocation, target.os.user_aslr_policy_force_relocation)
  end

  def is_aslr_bottom_up_enabled
    attacker_favors_false(target.os.user_aslr_bottom_up_supported) and all_policies_enabled?(target.app.user_aslr_policy_bottom_up, target.os.user_aslr_policy_bottom_up)
  end

  def is_aslr_top_down_enabled
    attacker_favors_false(target.os.user_aslr_top_down_supported) and all_policies_enabled?(target.os.user_aslr_policy_top_down)
  end

  def is_aslr_stack_enabled
    all_policies_enabled?(target.app.user_aslr_policy_stacks, target.os.user_aslr_policy_stacks)
  end

  def is_aslr_heap_enabled
    all_policies_enabled?(target.os.user_aslr_policy_heaps)
  end

  def is_aslr_peb_enabled
    all_policies_enabled?(target.os.user_aslr_policy_peb)
  end

  #
  # True if the flaw makes it possible to transfer control anywhere.
  #
  def can_transfer_control_anywhere
    target.flaw.can_transfer_control_anywhere
  end

  #
  # True if the flaw makes it possible to corrupt stack memory.
  #
  def can_corrupt_stack_memory
    is_flaw_stack_memory_corruption
  end

  #
  # True if the flaw makes it possible to corrupt heap memory.
  #
  def can_corrupt_heap_memory
    is_flaw_heap_memory_corruption
  end

  #
  # True if the flaw can be used to write anywhere.
  #
  def can_write_anywhere
    if is_flaw_arbitrary_memory_corruption or target.flaw.can_write_anywhere
      true
    else
      false
    end
  end

  #
  # Assuming this is a stack memory corruption, we can only overwrite local variables
  # if stack locals and parameters have not been safely re-ordered.
  #
  def can_overwrite_local_variables
    can_overwrite_current_frame_local_variables or
    can_overwrite_parent_frame_local_variables
  end

  #
  # True if the attacker is able to corrupt non-buffer local variables 
  # (including parameters) in the current frame.
  #
  def can_overwrite_current_frame_local_variables
    assumes? :can_corrupt_stack_memory and 
    (target.flaw.function_stack_locals_reordered != true or 
     target.flaw.function_stack_parameters_shadow_copied != true)
  end

  #
  # True if the attacker is able to corrupt parent frame local variables
  # that are passed to the current frame and used prior to the current
  # frame returning.
  #
  def can_overwrite_parent_frame_local_variables
    attacker_favors_true(normally_true(target.flaw.can_corrupt_parent_frame_locals))
  end
  
  def can_overwrite_child_or_current_frame_local_variables
    # FIXME: untested
    attacker_favors_true(normally_true(target.flaw.can_corrupt_child_or_current_frame_locals))
  end

  def can_corrupt_return_address
    can_corrupt_stack_memory and attacker_favors_true(target.flaw.can_corrupt_return_address)
  end

  def can_corrupt_frame_pointer
    can_corrupt_stack_memory and attacker_favors_true(target.flaw.can_corrupt_frame_pointer)
  end

  def can_corrupt_seh_frame
    can_corrupt_stack_memory and attacker_favors_true(target.flaw.can_corrupt_seh_frame)
  end

  def can_corrupt_object_vtable_pointer
    is_flaw_memory_corruption and attacker_favors_true(target.flaw.can_corrupt_object_vtable_pointer)
  end

  def can_corrupt_function_pointer
    is_flaw_memory_corruption and attacker_favors_true(target.flaw.can_corrupt_function_pointer)
  end

  def can_corrupt_write_target_pointer
    is_flaw_memory_corruption and attacker_favors_true(target.flaw.can_corrupt_write_target_pointer)
  end

  def can_corrupt_heap_block_header
    can_corrupt_heap_memory and attacker_favors_true(target.flaw.can_corrupt_heap_block_header)
  end

  def can_corrupt_heap_free_links
    can_corrupt_heap_memory and attacker_favors_true(target.flaw.can_corrupt_heap_free_links)
  end

  def can_free_arbitrary_address
    attacker_favors_true(normally_false(target.flaw.can_free_arbitrary_address))
  end

  #
  # True if stack protection is enabled in the context of this flaw.
  #
  def stack_protection_enabled
    attacker_favors_false target.flaw.function_stack_protection_enabled
  end

  #
  # The number of bits of entropy in the stack protection cookie
  #
  def stack_protection_entropy_bits
    if stack_protection_enabled

      case target.flaw.function_stack_protection_version
      when :gs_vc7, :gs_vc71, :gs_vc8, :gs_vc81, :gs_vc10

        if is_kernel_app
          4 # FIXME
        else 
          #
          # Only 17 bits of entropy exist if the flaw is local (based off of
          # techniques that can be used to reduce entropy).
          #
          if is_local_flaw
            return 17
          else
            if target.app.address_bits == 64
              return 48
            else
              if target.os.kind_of? OS::WindowsXP or target.os.kind_of? OS::WindowsServer2003
                return 16
              else
                return 32
              end
            end
          end
        end
      else 
        return 0
      end
    else
      return 0
    end
  end

  #
  # The probability of the attacker being able to guess the stack protection
  # cookie.  If stack protection is not enabled, or corruption occurs in a
  # non-linear pattern, then the probability is 1.
  #
  def can_bypass_stack_protection
    if stack_protection_enabled == false
      1.0 # stack protection is not enabled
    elsif target.flaw.corruption_position == :nonadjacent or attacker_favors_true(normally_false(target.flaw.can_discover_stack_cookie))
      1.0 # non-adjacent corruption or the cookie can be discovered
    else
      bits = stack_protection_entropy_bits

      if bits > 0
        (1.0 / (2 ** bits))
      else
        1.0 # no effective entropy
      end
    end
  end

  #
  # The attacker can bypass SafeSEH if:
  #
  #   1) it is not supported.
  #   2) a non-safeSEH image can be loaded.
  #   3) desired data can be found.
  #
  def can_bypass_safeseh
    if (attacker_favors_false(target.os.user_safeseh_supported) and
        attacker_favors_true(can_discover_address(:nonsafeseh)) and
        assumed_true? :can_find_desired_data)
      false
    else
      true
    end
  end

  #
  # The attacker can bypass SEHOP if:
  #
  #   1) it is not enabled.
  #   2) the address of ntdll can be discovered.
  #   3) the address of a stack object can be discovered.
  #
  def can_bypass_sehop
    if (attacker_favors_false(user_sehop_enabled) and 
        can_find_address 'image:ntdll' == false and
        can_find_address :stack == false)
      false
    else
      true
    end
  end

  #
  # VTguard can be bypassed if:
  #
  #   1) the flaw is not affected by it.
  #   2) an address inside the vtguard protected image can be discovered.
  #
  def can_bypass_vtguard
    if attacker_favors_false(target.flaw.vtguard_enabled)
      if can_discover_vtguard_cookie
        true
      else
        can_find_address :vtimage
      end
    else
      true
    end
  end

  #
  # An attacker can brute force an address if:
  #
  #   1) The flaw is triggered in an EH scope
  #
  # OR
  #
  #   2) The amount of entropy is less than 8 bits and the process will
  #   restart a sufficient number of times.
  #
  def can_brute_force_address(va)
    attacker_favors_true(normally_false(target.flaw.triggered_in_catch_all_seh_scope)) or (
      aslr_entropy_bits(va) <= 8 and
      (attacker_favors_false(target.app.restrict_automatic_restarts) == false)
    )
  end

  #
  # True if the attacker is able to massage the heap (e.g. place allocations at a desired
  # relative location).
  #
  def can_massage_heap
    attacker_favors_true(target.cap.can_massage_heap)
  end

  #
  # Assume that we can pivot the stack pointer if we can find an image address
  # and we're able to find desired data (in this case meaning the fake stack
  # to pivot into).  Like other predicates, this can be overidden by an
  # assumption.
  #
  def can_control_stack_pointer
    (predicate :can_find_address, :image) > 0 and (predicate :can_find_desired_data) > 0
  end

  #
  # An aggregate that controls whether or not return to libc is possible based on
  # the ability to find a given region and control the stack pointer
  #
  def can_return_to_libc(image = nil)
    # code regions in general
    if is_aslr_images_enabled == false or is_aslr_bottom_up_enabled == false
      v = predicate :can_find_address, :code
    # Otherwise, we need to worry about finding the supplied region specifically
    else
      v = predicate :can_find_address, image
    end
   
    # If there are any cases where we can find the address, then return to libc
    # is possible, assuming we can control the stack pointer
    ((v > 0) ? 1.0 : 0.0) * (predicate :can_control_stack_pointer)
  end

  #
  # Can an address be found for a given virtual address region type (e.g. code)?
  #
  def can_find_address(va = nil)

    # Some regions can be found if other regions are already able to be found.  
    # Account for these conditions.
    noop = 
      [
        (va == :code and assumes?(:can_find_address, :image)),
        (va == :image and assumes?(:can_find_address, :vtimage)),
        (va == :image and assumes?(:can_find_address, 'image:ntdll')), 
        (va == :data and assumes?(:can_find_address, :heap)),
        (va == :data and assumes?(:can_find_address, :stack)),
        (va == :data and assumes?(:can_find_address, :peb))
      ]
    
    noop.each do |n|
      return true if n
    end

    # Return the probability of the attacker being able to find the address.
    if can_brute_force_address(va)
      assume_true :can_brute_force_address, va
      1.0
    else
      1.0 - aslr_inhibition_degree(va)
    end
  end

  def can_find_stack_frame_address
    if ((target.flaw.corruption_displacement == :relative and target.flaw.corruption_region == :stack) or
        (can_write_anywhere and predicate(:can_find_address, :stack) > 0))
      true
    else
      false
    end
  end

  def can_find_attacker_controlled_code
    attacker_favors_true(normally_false(target.cap.can_discover_attacker_controlled_code_address))
  end

  def can_find_attacker_controlled_data
    attacker_favors_true(normally_false(target.cap.can_discover_attacker_controlled_data_address))
  end

  #
  # True to the degree to which code can be found.
  #
  def can_find_desired_code
    attacker_favors_true(target.cap.can_discover_desired_code_address) or predicate(:can_find_address, :code) > 0
  end
 
  #
  # True to the degree to which data can be found.
  #
  def can_find_desired_data
    attacker_favors_true(target.cap.can_discover_desired_data_address) or predicate(:can_find_address, :data) > 0
  end

  #
  # Can we execute at a given address
  #
  def can_execute_at_address(va = nil)
    if user_nx_enabled and [:data, :heap, :heapbase, :stack, :peb].include?(va)
      false
    else
      true
    end
  end

  #
  # True if safe unlinking is not present in the context of the 
  # flawed application.
  #
  def safe_unlinking_not_present
    if is_user_flaw
      if target.os.user_heap_safe_unlinking.nil?
        attacker_favors_true
      else
        target.os.user_heap_safe_unlinking == false
      end
    else
      if target.os.kernel_pool_safe_unlinking.nil?
        attacker_favors_true
      else
        target.os.kernel_pool_safe_unlinking == false
      end
    end
  end

  #
  # True if the low fragmentation heap is enabled for the target application.
  #
  def user_lfh_enabled
    target.app.user_heap_frontend == :lfh
  end

  #
  # Checks if NX has not already been bypassed
  #
  def nx_not_already_evaded
    (assumes? :can_execute_at_address, :data) ? 0.0 : 1.0
  end

end

# Simulates a given context
class Simulator

  def initialize(context)
    @context = context
  end

  # Runs the simulation from a given initial state
  def run(state = :target_defined)
    # Build the state machine
    sm = build

    @context.statemachine = sm

    # Set our initial state
    sm.state = state

    # Run the simulation
    sm.simulate
  end

  # Build the state machine that we will use to perform our exploitability 
  # analysis
  def build
    sm = Statemachine.build do

      context @context

      ###
      ###
      ##
      #   Pre-exploitation stage
      #
      #   Spans from one or more environmental preparations to triggering the flaw.
      ##
      ###
      ###

      state :target_defined do
        event :prepare_environment, :preparing_environment
      end

      #
      # Prepare the target environment for exploitation such as by loading
      # non-ASLR/non-safeSEH DLLs, spraying data/code, and so on.
      #
      state :preparing_environment do

        #
        # After all pre-exploitation steps have finished we can stop
        # preparing the environment.
        #
        event :finish_preparing_environment, :environment_prepared

        #
        # Load a non-ASLR image to enable assumptions about the location of
        # code and data.
        #
        event :load_non_aslr_image, :preparing_environment, lambda {

          technique ExSim::LoadNonASLRDLL

          #
          # This only applies to windows.
          #
          predicate :is_windows_os

          #
          # No need to load a non-ASLR image if desired code and data can
          # already be located.
          #
          predicate :load_non_aslr_image_necessary do
            if assumed_true? :can_find_desired_code and assumed_true? :can_find_desired_data
              false
            else
              true
            end
          end

          #
          # The attacker must be able to load a non-ASLR image in the first place.  They cannot
          # do this if:
          #
          #   1. The OS has ASLR turned on (vs. OptIn) for all images regardless of image flags.
          #
          #   Or
          #
          #   2. The attacker is not actually able to load a non-ASLR image.
          #
          predicate :can_load_non_aslr_image do
            target.os.user_aslr_policy_lib_images != :on and attacker_favors_true target.cap.can_load_non_aslr_image
          end

          #
          # The non-ASLR image must load at its preferred base address.
          #
          predicate :non_aslr_image_loads_at_preferred_address do
            if is_aslr_force_relocation_enabled
              can_find_address :force_relocate_image
            else
              true
            end
          end

          #
          # If all predicates are satisfied, then we assume that desired code
          # and data can be found.
          #

          assume_true :can_find_address, :image
          assume_true :can_find_address, :code
          assume_true :can_find_address, :data
          assume_true :can_find_desired_code
          assume_true :can_find_desired_data
          assume_true :non_aslr_image_loaded

        }

        #
        # Load a non-SafeSEH image into the context of the target process.
        #
        event :load_non_aslr_non_safeseh_image, :preparing_environment, lambda {

          technique ExSim::LoadNonASLRDLL
          technique ExSim::LoadNonASLRNonSafeSEHDLL

          #
          # It must be possible to load a non-ASLR DLL.
          #
          predicate :can_be_loaded_as_non_aslr_image do
            assumed_true? :non_aslr_image_loaded
          end

          #
          # This only applies to windows.
          #
          predicate :is_windows_os

          #
          # The attacker must be able to load a non-ASLR and non-SafeSEH image.
          #
          predicate :can_load_non_aslr_non_safeseh_image do
            attacker_favors_true target.cap.can_load_non_aslr_non_safeseh_image
          end
             
          #
          # The ability to load a non-ASLR/non-SafeSEH DLL means we can bypass
          # SafeSEH.
          #
          assume_true :can_bypass_safeseh

        }

        #
        # Spray the heap with desired data (or shellcode).  This
        # requires the attacker to have the ability to spray heap in the
        # context of the target application.  If this can be done then we can
        # assume that the attacker will be able to assume the address of
        # controlled data.
        #
        event :spray_data, :preparing_environment, lambda {

          technique ExSim::SprayData

          #
          # It is not necessary to spray data if we can already find desired
          # data.
          #
          predicate :spray_data_necessary do
            assumed_false_or_nil? :can_find_desired_data
          end

          #
          # The attacker must be able to spray data bottom up.
          #
          predicate :can_spray_data_bottom_up do
            attacker_favors_true target.cap.can_spray_data_bottom_up
          end

          #
          # If all predicates are satisfied then we can now assume that we are
          # able to find desired data at a desired address.
          #
          assume_true :can_find_address, :data
          assume_true :can_find_address, :heap
          assume_true :can_find_attacker_controlled_data
          assume_true :can_find_desired_data

        }

        #
        # Spray the address space with desired code.  This requires
        # the attacker to have the ability to use a technique like JIT spraying.
        #
        event :spray_code, :preparing_environment, lambda {

          technique ExSim::SprayCode

          #
          # It is not necessary to spray code if we can already find desired
          # code.
          #
          predicate :spray_code_necessary do
            assumed_false_or_nil? :can_find_desired_code
          end

          #
          # The attacker must be able to spray code bottom up (e.g. via JIT spraying).
          #
          predicate :can_spray_code_bottom_up do
            attacker_favors_true target.cap.can_spray_code_bottom_up
          end

          #
          # If all predicates are satisfied we can now assume that we are able to 
          # find desired data and code at a desired address.
          #
          assume_true :can_find_address, :code
          assume_true :can_find_address, :data
          assume_true :can_find_attacker_controlled_code
          assume_true :can_find_desired_code
          assume_true :can_find_desired_data

        }

        event :map_null_page, :preparing_environment, lambda {

          technique ExSim::NullMap

          #
          # Only applicable to NULL dereference flaws at the moment.
          #
          predicate :is_null_dereference_flaw

          #
          # This must be a local kernel flaw.
          #
          predicate :is_local_kernel_flaw

          #
          # The attacker must be able to map the NULL page.
          #
          predicate :can_map_null_page

          assume_true :null_page_mapped
          assume_true :can_find_address, :data
          assume_true :can_find_attacker_controlled_data
          assume_true :can_find_desired_data

        }

        #
        # Put heap allocations into a preferred configuration relative to 
        # one another prior to triggering the flaw.
        #
        event :massage_heap, :preparing_environment, lambda {

          #
          # This must be a heap memory corruption flaw for this to matter.
          #
          predicate :can_corrupt_heap_memory

          #
          # The attacker must be able to massage the heap.
          #
          predicate :can_massage_heap

        }

        #
        # Discover a stack address as a result of an address space information
        # disclosure.
        #
        event :discover_stack_address, :preparing_environment, lambda {

          # 
          # ASLR for the stack must be enabled for this to matter.
          #
          predicate :is_aslr_stack_enabled 

          #
          # The attacker must be able to discover a stack address.
          #
          predicate :can_discover_stack_address do
            can_discover_address :stack
          end

          assume_true :can_find_address, :stack
          assume_true :can_find_address, :data

        }

        #
        # Leak a heap address back to the attacker.  If possible, this
        # allows the attacker to assume knowledge of where a heap has
        # been mapped and where writable data can be found.
        #
        event :discover_heap_address, :preparing_environment, lambda {

          # 
          # ASLR must be enabled for the information leak to matter.
          #
          predicate :is_aslr_heap_enabled 

          #
          # The attacker must be able to discover a heap address.
          #
          predicate :can_discover_heap_address do
            can_discover_address :heap
          end

          assume_true :can_find_address, :heap
          assume_true :can_find_address, :data

        }

        #
        # Leak the PEB address back to the attacker.  If possible, this
        # allows the attacker to assume knowledge of where a heap has
        # been mapped and where writable data can be found.
        #
        event :discover_peb_address, :preparing_environment, lambda {

          # 
          # ASLR must be enabled for the information leak to matter.
          #
          predicate :is_aslr_peb_enabled 

          #
          # The attacker must be able to discover a peb address.
          #
          predicate :can_discover_peb_address do
            can_discover_address :peb
          end

          assume_true :can_find_address, :peb
          assume_true :can_find_address, :data

        }

        #
        # Leak an image address back to the attacker.  If possible, this
        # allows the attacker to assume knowledge of where code and data
        # are mapped in the target address space.
        #
        event :discover_image_address, :preparing_environment, lambda {

          # 
          # ASLR must be enabled for the information leak to matter.
          #
          predicate :is_aslr_images_enabled 

          #
          # The attacker must be able to discover an image address.
          #
          predicate :can_discover_image_address do
            can_discover_address :image
          end

          assume_true :can_find_address, :image
          assume_true :can_find_address, :code
          assume_true :can_find_address, :data

        }

        #
        # Leak the address of the image with the flaw which has been protected
        # by vtugard.
        #
        event :discover_vtguard_cookie, :preparing_environment, lambda {

          # 
          # ASLR must be enabled for the information leak to matter.
          #
          predicate :is_aslr_images_enabled

          #
          # Leaking the vtguard image address is only necessary if vtguard is enabled.
          #
          predicate :is_vtguard_enabled do
            attacker_favors_false target.flaw.vtguard_enabled
          end

          #
          # Can the attacker actually discover the vtguard cookie?
          #
          predicate :can_discover_vtguard_cookie do
            attacker_favors_true(normally_false(target.flaw.can_discover_vtguard_cookie)) or predicate(:can_find_address, :vtimage) > 0
          end

          assume_true :can_find_address, :image
          assume_true :can_find_address, :code
          assume_true :can_find_address, :data

        }

      end

      #
      # The environment has been prepared and the flaw can now be triggered.
      #
      state :environment_prepared do

        #
        # Trigger the flaw.
        #
        event :trigger_flaw, :flaw_triggered

      end

      ###
      ###
      ##
      #   Exploitation stage
      #
      #   Spans from triggering the flaw to executing arbitrary code.
      ##
      ###
      ###

      #
      # A flaw has been triggered.
      #
      state :flaw_triggered do

        #
        # If the flaw provides us with the ability the transfer control to an arbitrary 
        # address, then do so and get control of the instruction pointer.
        #
        event :transfer_control, :control_of_instruction_pointer, lambda {

          #
          # This must not be a NULL dereference flaw (handled via a separate event).
          #
          predicate_nocache :is_not_null_dereference_flaw do
            not is_null_dereference_flaw
          end

          #
          # This must be a control transfer flaw (such as a control transfer via a
          # type mismatch).
          #
          predicate :is_control_transfer_flaw

          #
          # The flaw must provide us with the ability to transfer control anywhere.
          #
          predicate :can_transfer_control_anywhere

        }

        #
        # Get control of the instruction pointer by transferring to a target
        # derived from a NULL read.
        #
        event :transfer_control_via_null_read, :control_of_instruction_pointer, lambda {

          technique ExSim::NullMap

          #
          # This must be a null dereference flaw.
          #
          predicate :is_null_dereference_flaw

          #
          # The NULL page must have been mapped by the attacker in
          # pre-exploitation.
          #
          predicate :null_page_is_mapped do
            assumed_true? :null_page_mapped
          end

          #
          # This flaw must allow us to transfer control anywhere.
          #
          predicate :can_transfer_control_anywhere

        }

        #
        # Get control of a write target pointer by transferring control to a
        # target derived from a NULL read.
        #
        event :write_via_null_read, :control_of_write_target_pointer, lambda {

          technique ExSim::NullMap

          #
          # This must be a null dereference flaw.
          #
          predicate :is_null_dereference_flaw

          #
          # The NULL page must have been mapped by the attacker in
          # pre-exploitation.
          #
          predicate :null_page_is_mapped do
            assumed_true? :null_page_mapped
          end

        }

        #
        # Corrupt the saved return address on the stack.
        #
        event :corrupt_return_address, :control_of_return_address, lambda {

          technique ExSim::ReturnAddressOverwrite

          #
          # Must be able to corrupt stack memory.
          #
          predicate :can_corrupt_stack_memory

          #
          # Must be able to find the address of the stack (which is implicit for
          # stack-based corruption vulnerabilities, but not for absolute writes).
          #
          predicate :can_find_stack_frame_address

          #
          # Must be able to use this flaw to corrupt the return address.
          #
          predicate :can_corrupt_return_address

        }

        #
        # Partially corrupt the return address on the stack.
        #
        event :corrupt_return_address_partially, :control_of_return_address, lambda {

          technique ExSim::PartialReturnAddressOverwrite

          #
          # Must be able to corrupt stack memory.
          #
          predicate :can_corrupt_stack_memory

          #
          # Must be able to find the address of the stack (which is implicit for
          # stack-based corruption vulnerabilities, but not for absolute writes).
          #
          predicate :can_find_stack_frame_address

          #
          # Must be able to use this flaw to partially corrupt the return address.  This is
          # possible if the attacker can write anywhere or can precisely control the length
          # of the corruption.
          #
          predicate :can_corrupt_return_address_partially do
            can_write_anywhere or attacker_favors_true(target.flaw.corruption_length_controlled)
          end

        }

        #
        # Corrupt the saved frame pointer on the stack.
        #
        event :corrupt_frame_pointer, :control_of_frame_pointer, lambda {

          technique ExSim::FramePointerOverwrite

          #
          # Must be able to corrupt stack memory.
          #
          predicate :can_corrupt_stack_memory

          #
          # Must be able to find the address of the stack (which is implicit for
          # stack-based corruption vulnerabilities, but not for absolute writes).
          #
          predicate :can_find_stack_frame_address

          #
          # Must be able to use this flaw to corrupt the frame pointer.
          #
          predicate :can_corrupt_frame_pointer

        }

        #
        # Corrupt an exception registration record on the stack.
        #
        event :corrupt_seh_frame, :control_of_seh_frame, lambda {

          technique ExSim::StructuredExceptionHandlerOverwrite

          #
          # The target operating system family must be windows.
          #
          predicate :is_windows_os 
         
          #
          # The target application must be 32-bit x86.
          #
          predicate :is_x86_app

          #
          # Must be able to corrupt stack memory.
          #
          predicate :can_corrupt_stack_memory

          #
          # Must be able to find the address of the stack (which is implicit for
          # stack-based corruption vulnerabilities, but not for write anywhere).
          #
          predicate :can_find_stack_frame_address

          #
          # Must be able to use this flaw to corrupt an SEH frame.
          #
          predicate :can_corrupt_seh_frame

        }

        #
        # Corrupt a local C++ object on the stack to gain control of the
        # object's state.
        #
        event :corrupt_stack_cpp_object_vtable, :control_of_cpp_object_vtable, lambda {

          technique ExSim::StackVariableOverwrite
          technique ExSim::CppObjectVtablePointerOverwrite

          #
          # Must be able to corrupt stack memory.
          #
          predicate :can_corrupt_stack_memory

          #
          # Must be able to find the address of the stack (which is implicit for
          # stack-based corruption vulnerabilities, but not for write anywhere).
          #
          predicate :can_find_stack_frame_address

          #
          # We must be able to overwrite local variables in the current frame
          # or in the parent frame.
          #
          predicate :can_overwrite_local_variables

          #
          # The flaw must permit the attacker to overwrite a C++ object's vtable
          # pointer.
          #
          predicate :can_corrupt_object_vtable_pointer

        }

        #
        # Corrupt a local function pointer that is used prior to the function
        # returning.
        #
        event :corrupt_stack_function_pointer, :control_of_function_pointer, lambda {

          technique ExSim::StackVariableOverwrite
          technique ExSim::FunctionPointerOverwrite

          #
          # Must be able to corrupt stack memory.
          #
          predicate :can_corrupt_stack_memory

          #
          # Must be able to find the address of the stack (which is implicit for
          # stack-based corruption vulnerabilities, but not for write anywhere).
          #
          predicate :can_find_stack_frame_address

          #
          # We must be able to overwrite local variables in the current frame or in
          # a parent frame.
          #
          predicate :can_overwrite_local_variables

          #
          # The flaw must permit us to corrupt a local function pointer.
          #
          predicate :can_corrupt_function_pointer

        }

        #
        # Corrupt a local write target pointer that is written to prior to function
        # return.
        #
        event :corrupt_stack_write_target_pointer, :control_of_write_target_pointer, lambda {

          technique ExSim::StackVariableOverwrite
          technique ExSim::WriteTargetPointerOverwrite

          #
          # Must be able to corrupt stack memory.
          #
          predicate :can_corrupt_stack_memory

          #
          # Must be able to find the address of the stack (which is implicit for
          # stack-based corruption vulnerabilities, but not for write anywhere).
          #
          predicate :can_find_stack_frame_address

          #
          # We must be able to overwrite local variables in the current frame or in 
          # a parent frame.
          #
          predicate :can_overwrite_local_variables

          #
          # The flaw must permit us to corrupt a local write target pointer.
          #
          predicate :can_corrupt_write_target_pointer

        }

        #
        # Trigger a double free, arbitrary free, or uninitialized free in a manner that
        # provides us with control over the state of an in-use object.
        #
        event :free_and_realloc_in_use_heap_object, :control_of_in_use_object_state, lambda {

          technique ExSim::HeapPrematureFree

          #
          # The flaw must be a pre-mature free flaw.
          #
          predicate :is_premature_free do
            case target.flaw.to_sym
            when :double_free, :arbitrary_free
              true
            else
              false
            end
          end

          #
          # The ability to corrupt an in use heap object depends on the attacker's
          # ability to massage the heap.  Massaging the heap implies normalization
          # and placement of a desired object in a desired location.
          #
          predicate :can_massage_heap

        }

        #
        # Corrupt a typically adjacent in use application object that is stored
        # on the heap to get control of the object's state.
        #
        event :corrupt_in_use_heap_object, :control_of_in_use_object_state, lambda {

          technique ExSim::HeapApplicationDataOverwrite

          #
          # Must be able to corrupt heap memory.
          #
          predicate :can_corrupt_heap_memory

          #
          # The ability to corrupt an in use heap object depends on the attacker's
          # ability to massage the heap.  Massaging the heap implies normalization
          # and placement of a desired object in a desired location.
          #
          predicate :can_massage_heap

        }

        #
        # Corrupt (typically adjacent) heap memory such that a freed chunk's free 
        # links are controlled by the attacker.
        #
        event :corrupt_heap_free_links, :control_of_heap_entry_free_links, lambda {

          technique ExSim::HeapUnlinkOverwrite

          #
          # Must be able to corrupt heap memory.
          #
          predicate :can_corrupt_heap_memory

          #
          # Must be able to corrupt heap entry free links using this flaw.
          #
          predicate :can_corrupt_heap_free_links

        }

        #
        # Corrupt the LinkOffset field of a freed LFH user block.
        #
        event :corrupt_lfh_linkoffset, :control_of_lfh_linkoffset, lambda {

          technique ExSim::HeapLFHLinkOffsetOverwrite

          #
          # This only applies to Windows.
          #
          predicate :is_windows_os

          #
          # This must be a user mode flaw.
          #
          predicate :is_user_flaw

          #
          # It is only possible to corrupt the link offset of a freed LFH user block
          # if the region of corruption is the heap or the flaw enables corruption of
          # arbitrary memory.
          #
          predicate :can_corrupt_heap_memory

          #
          # LFH must be enabled.
          #
          predicate :user_lfh_enabled do
            target.app.user_heap_frontend == :lfh
          end

          #
          # LFH must be a version that has the link offset metadata
          #
          predicate :lfh_has_link_offset_metadata do
            target.app.user_heap_frontend_version == :lfh_v1
          end

        }

        #
        # Corrupt a heap handle by freeing and then reallocating it.
        #
        event :free_and_alloc_heap_handle, :control_of_heap_handle, lambda {
      
          technique ExSim::HeapHandleFreeAndReallocate
          technique ExSim::HeapHandleCommitRoutineOverwrite

          #
          # This only applies to Windows.
          #
          predicate :is_windows_os

          #
          # This must be a user mode flaw.
          #
          predicate :is_user_flaw

          #
          # The flaw must permit an arbitrary free.
          #
          predicate :can_free_arbitrary_address

          #
          # Must be able to find a heap address from which we can attempt to infer
          # the heap handle base.
          #
          predicate :can_find_address, :heapbase

          #
          # The OS must not prevent freeing of the _HEAP base.
          #
          predicate :can_free_heap_handle do
            attacker_favors_false(target.os.user_heap_prevent_free_of_heap_base) == false
          end

        }

        #
        # Corrupt the _HEAP handle structured associated with a heap.  This can
        # enable us to transfer control to an arbitrary address.
        #
        event :corrupt_heap_handle, :control_of_heap_handle, lambda {

          technique ExSim::HeapHandleCommitRoutineOverwrite

          #
          # This only applies to Windows.
          #
          predicate :is_windows_os

          #
          # This must be a user mode flaw.
          #
          predicate :is_user_flaw

          #
          # Must be able to corrupt heap memory.
          #
          predicate :can_corrupt_heap_memory

          #
          # Must be able to find a heap address from which we can attept to infer
          # the heap handle base.
          #
          predicate :can_find_address, :heapbase

       }

      end

      #
      # An additional flaw has been triggered (such as gaining the ability to
      # write anywhere via corrupting application state).  This results in
      # re-entering the flaw triggered state and may open up additional 
      # avenues of exploitation.
      #
      state :next_flaw_triggered do
        event :trigger_flaw, :flaw_triggered, lambda {

          #
          # Do not simulate multiple write anywhere flaws
          #
          predicate_nocache :have_not_triggered_write_anywhere do
            if assumed_true? :triggered_write_anywhere
              false
            else
              assume_true :triggered_write_anywhere 
              true
            end
          end

        }
      end

      #
      # Once we have control of the return address, the next step is to return
      # from the function and gain control of the instruction pointer.
      #
      state :control_of_return_address do

        event :return_from_function, :control_of_instruction_pointer, lambda {

          technique ExSim::ReturnAddressOverwrite

          #
          # Must be able to guess (or ignore) the stack protection cookie if
          # one is present.
          #
          predicate :can_bypass_stack_protection

          #
          # We can now safely assume that we have control of memory at the 
          # stack pointer.
          #
          assume_true :can_control_stack_pointer

        }

      end

      #
      # Once we have control of the frame pointer we can return from the
      # function and gain control of different parts of the parent stack frame.
      #
      state :control_of_frame_pointer do

        event :return_from_function_ra, :control_of_return_address, lambda {

          technique ExSim::FramePointerOverwrite
         
          #
          # Must be able to guess (or ignore) the stack protection cookie if
          # one is present.
          #
          predicate :can_bypass_stack_protection

        }

        event :return_from_function_os, :control_of_in_use_object_state, lambda {

          technique ExSim::FramePointerOverwrite
         
          #
          # Must be able to guess (or ignore) the stack protection cookie if
          # one is present.
          #
          predicate :can_bypass_stack_protection

        }

        event :return_from_function_fp, :control_of_function_pointer, lambda {

          technique ExSim::FramePointerOverwrite
         
          #
          # Must be able to guess (or ignore) the stack protection cookie if
          # one is present.
          #
          predicate :can_bypass_stack_protection

        }

        event :return_from_function_wtp, :control_of_write_target_pointer, lambda {

          technique ExSim::FramePointerOverwrite
         
          #
          # Must be able to guess (or ignore) the stack protection cookie if
          # one is present.
          #
          predicate :can_bypass_stack_protection

        }

      end

      #
      # Control of a function pointer can allow us to gain control of the
      # instruction pointer, but only after we've triggered a call via the
      # function pointer.
      #
      state :control_of_function_pointer do

        event :call_function_pointer, :control_of_instruction_pointer, lambda {

          #
          # Assume that the attacker will have a way to trigger a call via
          # the function pointer.
          #
          assume_true :can_trigger_function_pointer_call

        }

      end

      #
      # Control of an SEH frame can allow us to gain control of the instruction
      # pointer if we can trigger an exception.
      #
      state :control_of_seh_frame do

        event :trigger_exception, :control_of_instruction_pointer, lambda {

          technique ExSim::StructuredExceptionHandlerOverwrite

          #
          # Must be able to trigger the exception during the course of
          # exploiting the flaw, otherwise control of the SEH frame
          # does not matter.
          #
          predicate :can_trigger_seh_exception

          #
          # Must be able to bypass SafeSEH (if it is relevant in the context
          # of the target).
          #
          predicate :can_bypass_safeseh

          #
          # Must be able to bypass SEHOP (if it is relevant/enabled).
          #
          predicate :can_bypass_sehop

        }

      end

      #
      # We now have control of the LinkOffset of a freed LFH user block.
      #
      state :control_of_lfh_linkoffset do

        #
        # Control of the LinkOffset of a freed LFH user block can permit us to cause
        # the heap to allocate an already in-use heap object and then gain
        # control of the object's state.
        #
        event :allocate_in_use_heap_object, :control_of_in_use_object_state, lambda {

          technique ExSim::HeapLFHLinkOffsetOverwrite

          #
          # We can only do this if we are able to massage the heap in such a way as to
          # make sure that the heap allocates a specific heap object that is already
          # in-use.
          #
          predicate :can_massage_heap

        }

      end

      #
      # We now control the Flink and Blink pointers associated with a heap entry.  This
      # enables us to unlink the heap entry and gain control of a write target pointer.
      #
      state :control_of_heap_entry_free_links do

        #
        # Unlink the heap entry, leading to a write anywhere.
        #
        event :unlink_heap_entry, :control_of_write_target_pointer, lambda {

          technique ExSim::HeapUnlinkOverwrite

          #
          # Safe unlinking must not be present.
          #
          predicate :safe_unlinking_not_present

        }

      end

      #
      # Once we have control of a _HEAP handle, we can trigger a call through
      # the CommitRoutine function pointer.
      #
      state :control_of_heap_handle do

        #
        # Trigger a code path that invokes the commit routine.
        #
        event :call_commit_routine, :control_of_instruction_pointer, lambda {

          technique ExSim::HeapHandleCommitRoutineOverwrite

          #
          # Explicitly assumes that this action will not destabalize
          # the heap in any way.
          #
          explicitly_assume :does_not_destabalize_heap

        }

      end

      #
      # We now have control of the vtable pointer of a C++ object.
      #
      state :control_of_cpp_object_vtable do

        #
        # Trigger a virtual method call on the object whose state we control.  This
        # can give us control of the instruction pointer.
        #
        event :call_virtual_method, :control_of_instruction_pointer, lambda {

          technique ExSim::CppObjectVtablePointerOverwrite

          #
          # We must be able to find desired data in order to be able
          # to locate our fake vtable.
          #
          # FIXME: We could also find one in a mapped image or other region
          # that the attacker does not directly control.  Model this.
          #
          predicate :can_locate_fake_vtable do
            can_find_desired_data
          end

          #
          # If vtguard is enabled in the context of the flawed code, then
          # the attacker must be able to specify a valid vtable which
          # is possible if the attacker can leak an address inside of
          # the vtguard protected image.
          #
          predicate :can_bypass_vtguard

          #
          # We assume that the attacker can actually trigger the virtual method
          # call.
          #
          explicitly_assume :can_trigger_virtual_method_call

        }

      end

      #
      # We now have control of the state associated with an object that is currently
      # in-use by the application.
      #
      state :control_of_in_use_object_state do

        #
        # Trigger a virtual method call on the object whose state we control.  This
        # can give us control of the instruction pointer.
        #
        event :call_virtual_method, :control_of_instruction_pointer, lambda {

          technique ExSim::CppObjectVtablePointerOverwrite

          #
          # We must be able to find desired data in order to be able
          # to locate our fake vtable.
          #
          # FIXME: We could also find one in a mapped image or other region
          # that the attacker does not directly control.  Model this.
          #
          predicate :can_locate_fake_vtable do
            can_find_desired_data
          end

          #
          # If vtguard is enabled in the context of the flawed code, then
          # the attacker must be able to specify a valid vtable which
          # is possible if the attacker can leak an address inside of
          # the vtguard protected image.
          #
          predicate :can_bypass_vtguard

          #
          # We assume that the attacker can actually trigger the virtual method
          # call.
          #
          explicitly_assume :can_trigger_virtual_method_call

        }

        event :call_via_member_field_function_pointer, :control_of_instruction_pointer, lambda {
          
          technique ExSim::FunctionPointerOverwrite

          #
          # We assume that the attacker can actually trigger a call via the
          # function pointer.
          #
          explicitly_assume :can_trigger_call_via_member_function_pointer

        }

        #
        # Trigger a write to a member field pointer using the object that
        # we control.  This provides us with a write anywhere primitive.
        #
        event :write_via_member_field_pointer, :next_flaw_triggered, lambda {

          technique ExSim::WriteTargetPointerOverwrite

          assume_true :can_trigger_write_via_member_field_pointer

          assume_true :can_write_anywhere
          assume_true :can_corrupt_stack_memory
          assume_true :can_corrupt_heap_memory

        }

      end

      #
      # We have control of a pointer that is used when writing memory.
      #
      state :control_of_write_target_pointer do

        #
        # Write to memory using the pointer that is controlled by the attacker.  
        # This triggers a subsequent flaw by granting the ability to write anywhere
        # in memory.
        #
        event :write_memory, :next_flaw_triggered, lambda {
          assume_true :can_write_anywhere
          assume_true :can_corrupt_stack_memory
          assume_true :can_corrupt_heap_memory
        }

      end

      #
      # Once we have control of the instruction pointer we are able
      # to take the next step toward executing arbitrary code.  This
      # may require us to bypass NX, use ROP, or find some alternative
      # strategy to run code.
      #
      state :control_of_instruction_pointer do

        #
        # Transfer control to the address of desired code.
        #
        event :transfer_to_attacker_controlled_code, :control_of_code_execution, lambda {

          #
          # We must be able to find the address of attacker controlled code.
          #
          predicate :can_find_attacker_controlled_code

        }

        #
        # Transfer control to a data address.
        #
        event :transfer_to_data_address, :control_of_code_execution, lambda {

          #
          # Must be able to find the address of attacker controlled data.
          #
          predicate :can_find_attacker_controlled_data

          #
          # Must be able to execute data as code.
          #
          predicate :can_execute_at_address, :data

        }

        #
        # Pivot to control of the stack pointer as an alternative means of getting
        # control of code execution.
        #
        event :pivot_stack_pointer, :control_of_stack_pointer, lambda {

          technique ExSim::PivotStackPointer

          #
          # To do this we must be able to control the stack pointer (and the content
          # pointed to by the stack pointer).
          #
          predicate :can_control_stack_pointer
         
          #
          # We assume that the attacker is able to find a stack pivot gadget of
          # some sort.
          #
          explicitly_assume :can_find_stack_pivot_gadget

        }

        #
        # Bypass NX if it is necessary.
        #
        event :bypass_nx, :bypassing_nx, lambda {

          #
          # We don't need to bypass NX if we're already able to execute data
          # as code.
          #
          predicate_nocache :nx_bypass_necessary, :data do

            if can_execute_at_address :data == 1.0
              0.0
            else
              1.0
            end

          end

        }

      end

      #
      # Once we have control of the stack pointer, we can execute complete ROP
      # payload and gain control of code execution.
      #
      state :control_of_stack_pointer do

        #
        # Execute a payload composed entirely of ROP gadgets and thus gain
        # control of code execution.
        #
        event :execute_self_contained_rop_payload, :control_of_code_execution, lambda {

          technique ExSim::CodeExecutionViaSelfContainedRopPayload

          #
          # ROP payloads are only really needed if we cannot find the address of
          # attacker controlled code.
          #
          predicate_nocache :rop_payload_is_warranted do
            can_find_attacker_controlled_code == false
          end

          #
          # The attacker must be able to control the stack pointer.  While not
          # entirely true given the form of certain gadgets, the state of the
          # art implementations all require stack pointer control for chaining.
          # 
          # We must be able to find an image that will contain our gadgets.  In
          # some cases we may also need to know the address of kernel32.dll,
          # but for now we assume this to not be the case.
          #
          predicate :can_return_to_libc, :image

          #
          # We explicitly assume that the attacker can find all of the gadgets
          # that are needed to write their self-contained payload.
          #
          explicitly_assume :can_find_all_necessary_gadgets

        }

      end

      state :bypassing_nx do

        #
        # Disable DEP for the process by returning into code that invokes
        # NtSetInformationProcess with MEM_EXECUTE_ENABLE.
        #
        event :disable_dep_for_process, :control_of_instruction_pointer, lambda {

          technique ExSim::BypassNxViaNtSetInformationProcess

          #
          # This only applies to Windows.
          #
          predicate :is_windows_os

          #
          # This must be a user flaw.
          #
          predicate :is_user_flaw

          #
          # This must be a 32-bit x86 application.
          #
          predicate :is_x86_app

          #
          # DEP must not be enabled permanently.
          #
          predicate :is_dep_not_permanent do
            attacker_favors_false(target.app.user_nx_permanent) == false
          end

          #
          # We must be able to find ntdll.dll or acgeneral.dll.
          #
          # The ability to return to libc implies that we must be able to control
          # the stack pointer.
          #
          predicate :can_return_to_libc, "image:ntdll.dll"

          #
          # We must know the address of writable data given the requirement that
          # ebp be writable.
          #
          predicate :can_find_address, :data
          
          #
          # If all predicates are satisfied, assume that we can now execute data
          # as code.
          #
          assume_true :can_execute_at_address, :data
          assume_true :can_execute_at_address, :heap
          assume_true :can_execute_at_address, :stack

          #
          # Assume that we can now find desired code if we
          # know where desired data is.
          #
          assume :can_find_desired_code do
            can_find_desired_data or can_find_desired_code
          end

        }

        #
        # Bypass NX by staging the payload to an executable heap.
        #
        event :stage_to_executable_heap, :control_of_instruction_pointer, lambda {

          technique ExSim::BypassNxViaStageToExecutableCrtHeap

          #
          # This only applies to Windows.
          #
          predicate :is_windows_os

          #
          # This is only applicable in user mode.
          #
          predicate :is_user_flaw

          #
          # We must be able to find msvcrt.dll or an image that has a
          # HeapCreate IAT entry.
          #
          # The ability to return to libc implies that we must be able to
          # control the stack pointer.
          #
          predicate :can_return_to_libc

          #
          # If the predicates are satisfied, assume that we can now execute data as code
          # and that are also able to find the address of our code (implicitly).
          #
          assume_true :can_execute_at_address, :data
          assume_true :can_find_address, :code
          assume_true :can_find_attacker_controlled_code
          assume_true :can_find_desired_code

        }

        #
        # Bypass NX by staging the payload to memory that is re-protected or allocated
        # as executable.
        #
        event :stage_via_virtual_protect, :control_of_instruction_pointer, lambda {

          technique ExSim::BypassNxViaStageToVirtualProtectOrAlloc

          #
          # This only applies to Windows.
          #
          predicate :is_windows_os

          #
          # This is only applicable in user mode.
          #
          predicate :is_user_flaw

          #
          # We must be able to find kernel32.dll in order to return into VirtualProtect
          # or VirtualAlloc.  Alternatively we could return into ntdll.dll, but the 
          # effect is the same.
          #
          predicate :can_return_to_libc, "image:kernel32.dll"

          #
          # If the predicates are satisfied, assume that we can now execute data as
          # code.
          #
          assume_true :can_execute_at_address, :data
          assume_true :can_find_address, :code
          assume_true :can_find_attacker_controlled_code
          assume_true :can_find_desired_code

        }

        event :stage_via_virtual_protect_rop, :control_of_instruction_pointer, lambda {

          technique ExSim::BypassNxViaRopStageToVirtualProtectOrAlloc

          #
          # This only applies to Windows.
          #
          predicate :is_windows_os

          #
          # Staging to VirtualProtect is only applicable in user mode.
          #
          predicate :is_user_flaw

          #
          # The attacker must be able to control the stack pointer.  While not
          # entirely true given the form of certain gadgets, the state of the
          # art implementations all require stack pointer control for chaining.
          #
          predicate :can_control_stack_pointer

          #
          # We must be able to find an image that will contain our gadgets.  In some cases
          # we may also need to know the address of kernel32.dll, but for now we assume
          # this to not be the case.
          #
          predicate :can_find_address, :image

          #
          # Assume that we can now execute data as code and that we're now able to find
          # desired code implicitly.
          #
          assume_true :can_execute_at_address, :data
          assume_true :can_find_address, :code
          assume_true :can_find_attacker_controlled_code
          assume_true :can_find_desired_code

          explicitly_assume :can_find_stack_pivot_gadget

        }

      end

      ###
      ###
      ##
      #   Post-exploitation stage
      #
      #   Spans from code execution to ...
      ##
      ###
      ###

      #
      # We now have control of arbitrary code execution.  Exploitation is now
      # successful and we can transition into the post-exploitation stage.
      #
      state :control_of_code_execution do

        #
        # When we enter this state, copy the transitions that we traversed
        # and then add our current context to the list of global simulation
        # contexts so that we can do global analysis on it.
        #
        on_entry lambda {
            @transitions = self.statemachine.simulation_stack.dup
            self.global.add_simulation self
        }

      end

    end

    #
    # Set the statemachine's context and return the statemachine to the control
    #
    sm.context = @context

    sm
  end

end

# Permutates various target configurations
class TargetPermutator
 
  def initialize
    Target.init_profiles

    @debug = false
    @track_impossible = false
    @track_equivalent_only = true
    @track_minimal_only = true
    @modes = []

    @log_simulation_details = true

    reset
  end

  attr_accessor :output_directory

  def reset
    @context_id_pool = 0
    @scenarios = []
    @max = {}
    @min = {}
    @avg = {}
  end

  def permutate_scenarios(*scenarios)
    scenarios.each do |obj|
      scenario, fields, field_values = obj

      @current_scenario = scenario

      if field_values.nil?
        do_permutate_fields(*fields)
      else
        do_permutate_fields_custom_values(fields, field_values)
      end
    end

    normalize
  end

  def permutate_fields(*fields)
    do_permutate_fields(*fields)
    normalize
  end
  
  def permutate_fields_custom_values(fields, field_values)
    do_permutate_fields_custom_values(fields, field_values)
    normalize
  end

  def save_csv
    # Dump CSVs with the results of our simulation analysis
    File.open(File.join(@output_directory||'', "tab_scenario.csv"), "w") do |tab_scenario_fd|
      tab_scenario_fd.puts "name,scenario,#{Target.bit_map_to_csv}"

      @scenarios.sort { |x,y|
        x[:name] <=> y[:name]
      }.each do |scenario|
        tab_scenario_fd.puts "#{scenario[:name]},#{scenario[:scenario]},#{scenario[:target_detail]}"
      end
    end

    # Dump the individual metric tables
    [ 
      :fitness, 
      :exploitability, 
      :desirability, 
      :likelihood,
      :homogeneity
    ].each do |metric|

      File.open(File.join(@output_directory||'', "tab_metric_#{metric}.csv"), "w") do |tab_fd|
        tab_fd.puts "name,scenario,hw,os,app,flaw,norm,min,max,avg"

        @scenarios.sort { |x,y|
          if y[metric][:norm].nan?
            -1
          elsif x[metric][:norm].nan?
            1
          else
            y[metric][:norm] <=> x[metric][:norm] 
          end
        }.each do |scenario|
          row =
            [
              scenario[:name],
              scenario[:scenario],
              scenario[:target].hw,
              scenario[:target].os,
              scenario[:target].app,
              scenario[:target].flaw,
              scenario[metric][:norm],
              scenario[metric][:min],
              scenario[metric][:max],
              scenario[metric][:avg]
            ]
        
          tab_fd.puts row.map { |x| x.to_s }.join(",")
        end
      end
    end
  end

  def max_fitness
    do_max :fitness
  end

  def max_exploitability
    do_max :exploitability
  end

  def max_desirability
    do_max :desirability
  end

  def max_likelihood
    do_max :likelihood
  end

  def max_homogeneity
    do_max :homogeneity
  end

  def min_fitness
    do_min :fitness
  end

  def min_exploitability
    do_min :exploitability
  end

  def min_desirability
    do_min :desirability
  end

  def min_likelihood
    do_min :likelihood
  end

  def min_homogeneity
    do_min :homogeneity
  end

  def avg_fitness
    do_avg :fitness
  end

  def avg_exploitability
    do_avg :exploitability
  end

  def avg_desirability
    do_avg :desirability
  end

  def avg_likelihood
    do_avg :likelihood
  end

  def avg_homogeneity
    do_avg :homogeneity
  end

  def sorted_scenarios
    @scenarios.sort { |x, y|
      [
        y[:fitness][:norm], 
        y[:exploitability][:norm]
      ] <=> [
        x[:fitness][:norm],
        x[:exploitability][:norm]
      ]
    }
  end

  attr_accessor :debug
  attr_accessor :track_impossible
  attr_accessor :track_equivalent_only
  attr_accessor :modes
 
  attr_accessor :log_simulation_details

  attr_reader :scenarios

private

  def do_permutate_fields(*fields)
    do_permutate_fields_with_descriptors(
      fields,
      Target.bit_descriptors_from_symbols(*fields))
  end

  def do_permutate_fields_custom_values(fields, field_values)
    # Get the bit descriptors associated with the provided fields
    orig_bit_descriptors = Target.bit_descriptors_from_symbols(*fields)

    # Duplicate each bit descriptor, setting its values to what we
    # wish to permutate
    new_bit_descriptors = []

    orig_bit_descriptors.each_with_index do |bit_desc, index|
      bit_values = field_values[index]

      if bit_values.length > 0 and bit_values[0] != '*' 
        new_bit_desc = bit_desc.dup

        # Translate the boolean field bit values
        if bit_desc[:type] == :bool
          new_bit_desc[:type] = :enum
          bit_values.map! do |value|
            if value == "true" or value == true
              true
            else
              false
            end
          end
        end

        new_bit_desc[:values] = bit_values
        new_bit_desc[:bits]   =(Math.log(bit_values.length) / Math.log(2)).ceil
      else
        new_bit_desc = bit_desc
      end
        
      new_bit_descriptors << new_bit_desc
    end

    # Now permutate these fields with our updated bit descriptors
    do_permutate_fields_with_descriptors(
      fields,
      new_bit_descriptors)
  end

  def do_permutate_fields_with_descriptors(fields, bit_descriptors)
    begin

      if @output_directory
        begin
          Dir.mkdir(@output_directory)
        rescue Errno::EEXIST
        end
      end

      open_simulation_csv

      # Calculate the maximum accumulator size
      max_accum = 1

      bit_descriptors.each do |bit_desc|
        max_accum <<= bit_desc[:bits]
      end

      puts "#{max_accum} simulations..."
      puts

      # Enumerate each bit value for these fields
      0.upto(max_accum - 1) do |accum|
        context = SimulationContext.new

        context.context_id = (@context_id_pool += 1)
        context.debug = @debug
        context.modes = @modes || []
        context.track_impossible = @track_impossible
        context.global.track_equivalent_only = @track_equivalent_only
        context.global.track_minimal_only = @track_minimal_only

        begin
          fields.each_with_index do |field, index|
            bit_desc = bit_descriptors[index]

            # Mask off the specific value for this bit position
            value = accum & ((1 << bit_desc[:bits]) - 1)

            # Set the corresponding members of our context for
            # this bit descriptor
            Target.set_context_bitvalue(context, bit_desc, value)

            accum >>= bit_desc[:bits]        
          end
          
          fields.each_with_index do |field, index|
            bit_desc = bit_descriptors[index]
            Target.verify_context(context, bit_desc)
          end

          # Recalibrate the target to ensure that all settings
          # are coherent.
          context.target.recalibrate

        rescue InvalidTargetBitValue
          next
        end
        
        # Run the simulation
        simulator = Simulator.new(context)
        simulator.run

        # Save the results of our simulation
        save(context)
      end
    ensure
      @simulation_fd.close if @simulation_fd
    end
  end

  def do_max(scenario)
    @max[scenario] || @max[scenario] = max { |s| s[scenario][:avg] }
  end

  def do_min(scenario)
    @min[scenario] || @min[scenario] = min { |s| s[scenario][:avg] }
  end

  def do_avg(scenario)
    @avg[scenario] || @avg[scenario] = avg { |s| s[scenario][:avg] }
  end

  def avg(&block)
    average = 0.0
    @scenarios.each { |scenario|
      average += block.call(scenario)
    }
    average /= @scenarios.length
  end

  def min(&block)
    min = nil
    @scenarios.each { |scenario|
      value = block.call(scenario)
      if min.nil? or value < min
        min = value
      end
    }
    min
  end

  def max(&block)
    max = nil
    @scenarios.each { |scenario|
      value = block.call(scenario)
      if max.nil? or value > max
        max = value
      end
    }
    max
  end

  def normalize
    @scenarios.each { |scenario|
      if max_fitness == min_fitness
        scenario[:fitness][:norm] = 1.0
      else
        scenario[:fitness][:norm] = 
          (scenario[:fitness][:avg] - min_fitness) / (max_fitness - min_fitness)
      end

      if max_exploitability == min_exploitability
        scenario[:exploitability][:norm] = 1.0
      else
        scenario[:exploitability][:norm] =
          (scenario[:exploitability][:avg] - min_exploitability) / (max_exploitability - min_exploitability)
      end
       
      if max_desirability == min_desirability
        scenario[:desirability][:norm] = 1.0
      else
        scenario[:desirability][:norm] =
          (scenario[:desirability][:avg] - min_desirability) / (max_desirability - min_desirability)
      end
     
      if max_likelihood == min_likelihood
        scenario[:likelihood][:norm] = 1.0
      else
        scenario[:likelihood][:norm] =
          (scenario[:likelihood][:avg] - min_likelihood) / (max_likelihood - min_likelihood)
      end
     
      if max_homogeneity == min_homogeneity
        scenario[:homogeneity][:norm] = 1.0
      else
        scenario[:homogeneity][:norm] =
          (scenario[:homogeneity][:avg] - min_homogeneity) / (max_homogeneity - min_homogeneity)
      end
    }
  end

  # Save the results of this context 
  def save(context)
    global = context.global


    # Record the high-level data points for this simulation
    if @current_scenario
      name = "#{@current_scenario}-#{context.context_id}"
      scenario = @current_scenario
    else
      name = "#{context.target.hw.to_sym}-#{context.target.os.to_sym}-#{context.target.app.to_sym}-#{context.target.flaw}-#{context.context_id}"
      scenario = name
    end

=begin
    @scenarios <<
      {
        :name           => name,
        :scenario       => scenario,
        :target         => context.target,
        :target_detail  => context.target_detail_to_csv,
        :fitness  => { :avg => global.avg_fitness, :min => global.min_fitness, :max => global.max_fitness },
        :exploitability => { :avg => global.avg_exploitability, :min => global.min_exploitability, :max => global.max_exploitability },
        :desirability   => { :avg => global.avg_desirability, :min => global.min_desirability, :max => global.max_desirability },
        :likelihood     => { :avg => global.avg_likelihood, :min => global.min_likelihood, :max => global.max_likelihood },
        :homogeneity    => { :avg => global.avg_homogeneity, :min => global.min_homogeneity, :max => global.max_homogeneity },
      }
=end

    if @log_simulation_details
    
      puts "#{Time.now}: saving #{name} [detail and csv]"

      File.open("#{File.join(@output_directory||'',name)}.txt", "w") do |fd|
        fd.puts "#{global.simulation_count} simulations recorded"
        fd.puts 
        fd.puts "Characteristics"
        fd.puts
        fd.puts "Fitness       : avg=#{global.avg_fitness} min=#{global.min_fitness} max=#{global.max_fitness}"
        fd.puts "Exploitability: avg=#{global.avg_exploitability} min=#{global.min_exploitability} max=#{global.max_exploitability}"
        fd.puts "Desirability  : avg=#{global.avg_desirability} min=#{global.min_desirability} max=#{global.max_desirability}"
        fd.puts "Likelihood    : avg=#{global.avg_likelihood} min=#{global.min_likelihood} max=#{global.max_likelihood}"
        fd.puts "Homogeneity   : avg=#{global.avg_homogeneity} min=#{global.min_homogeneity} max=#{global.max_homogeneity}"
        fd.puts

        fd.puts "Exploitability counts"
        fd.puts

        global.exploitability_range_counts.each do |range_count|
          region, opts = range_count
          fd.puts "Exploitability (#{opts[:prev_range]},#{region.to_s}]: #{opts[:equiv_count]} (total=#{opts[:total_count]})"
        end

        fd.puts
        fd.puts "Configuration"
        fd.puts
        fd.puts "Scenario: #{@current_scenario}" if @current_scenario
        fd.puts context.target_detail_to_s
        fd.puts

        global.simulations_sorted.each_with_index do |simulation, cnt|

          member_cnt = nil

          if simulation.equivalence_count > 0
            member_cnt = "  [#{simulation.equivalence_count} equivalent simulations]"
          end

          fd.puts "============== simulation #{cnt+1} #{member_cnt}"
          fd.puts
          fd.puts "Fitness       : #{simulation.fitness}"
          fd.puts "Exploitability: #{simulation.exploitability}"
          fd.puts "Desirability  : #{simulation.desirability}"
          fd.puts "Likelihood    : #{simulation.likelihood}"
          fd.puts "Homogeneity   : #{simulation.homogeneity}"
          fd.puts "Population    : #{simulation.population}"

          if simulation.aborted
            fd.puts "Aborted       : Yes/#{simulation.abort_reason}"
          end

          fd.puts "Transitions   :"

          simulation.transitions.each do |transition|
            fd.puts "\t#{transition.origin_id.to_s.ljust(50)} -> #{transition.event.to_s.ljust(35)} -> #{transition.destination_id}"
          end 

          fd.puts "Assumptions   :"

          simulation.inorder_assumptions.each do |row|
            assumption, value = row
            fd.puts "\t#{assumption.to_s.ljust(50)} -> #{value[:rv].to_s.ljust(35)} [#{value[:event]}] #{value[:used] ? "USED" : ""}"
          end

          fd.puts "Techniques    :"

          simulation.techniques.each do |technique|
            fd.puts "\t#{technique.to_s}"
          end

          fd.puts

          # Write to the simulation CSV
          log_simulation_csv(
            context,
            scenario,
            simulation,
            cnt)


        end
      end

    else

      puts "saving #{name} [csv only]"

      global.simulations_sorted.each_with_index do |simulation, cnt|

        # Write to the simulation CSV
        log_simulation_csv(
          context,
          scenario,
          simulation,
          cnt)

      end
    end
  end

  # Open the simulations file descriptor so that we can log detail
  # about each simulation
  def open_simulation_csv
    simulation_csv_file = File.join(@output_directory || '', "simulations.csv")
    write_header = File.exists?(simulation_csv_file) == false || File.stat(simulation_csv_file).size == 0

    @simulation_fd = File.new(simulation_csv_file, "a")

    log_simulation_csv_header(write_header)
  end

  # Log the simulation CSV header
  def log_simulation_csv_header(write_header = false)
    main_fields = 
      [
        'simulation',
        'scenario',
        'fitness',
        'exploitability',
        'desirability',
        'likelihood',
        'homogeneity',
        'aborted',
        'aborted_predicate',
      ]

    # Determine the set of possible transitions
    s  = Simulator.new(SimulationContext.new)
    sm = s.build

    @all_transitions = {}
    @all_events = {}  

    sm.states.values.each do |state|
      state.transitions.each_pair do |event, transition|
        @all_transitions[transition.origin_id.to_s] = true  
        @all_transitions[transition.destination_id.to_s] = true 
        @all_events[event.to_s] = true
      end
    end

    # Build an ordered list of events and transitions
    @all_transitions_ordered = []
    @all_events_ordered = []

    @all_transitions.keys.sort.each_with_index do |transition, index|
      @all_transitions[transition] = index
      @all_transitions_ordered << transition
    end 
    
    @all_events.keys.sort.each_with_index do |event, index|
      @all_events[event] = index
      @all_events_ordered << event
    end 
    
    # A template that is used duplicated when figuring out
    # which transitions and events a simulation went through
    # when converting to csv
    @sim_transitions_template = Array.new(@all_transitions_ordered.length) { 0 }
    @sim_events_template = Array.new(@all_events_ordered.length) { 0 }

    # Build columns string
    columns = ''
    columns << main_fields.join(",") + ","
    columns << Target.bit_map_to_csv + ","
    columns << @all_transitions_ordered.join(",") + ","
    columns << @all_events_ordered.join(",")

    @simulation_fd.puts columns if write_header
  end

  # Log a simulation CSV record
  def log_simulation_csv(context, scenario, simulation, simulation_id)
    row_fields = 
      [
        "#{scenario}-#{context.context_id}-#{simulation_id}",
        scenario,
        simulation.fitness,
        simulation.exploitability,
        simulation.desirability,
        simulation.likelihood,
        simulation.homogeneity,
        simulation.aborted ? 1 : 0,
        simulation.abort_reason
      ]

    transition_fields = @sim_transitions_template.dup
    event_fields = @sim_events_template.dup

    simulation.transitions.each do |transition|
      transition_fields[@all_transitions[transition.origin_id.to_s]] = 1
      transition_fields[@all_transitions[transition.destination_id.to_s]] = 1
      event_fields[@all_events[transition.event.to_s]] = 1
    end
    
    row = ''
    row << row_fields.join(",") + ","
    row << context.target_detail_to_csv + ","
    row << transition_fields.join(",") + ","
    row << event_fields.join(",")

    # Add target detail information
    @simulation_fd.puts row
  end

end

end
