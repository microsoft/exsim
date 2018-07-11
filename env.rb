module ExSim

class Profile
  attr_accessor :parent_target
  attr_accessor :sym

  def to_sym
    :undefined
  end

  def to_s
    to_sym.to_s
  end

  def to_sym
    @sym || self.class.to_s.downcase.to_sym
  end

  def recalibrate(target)
  end
end

class Hardware < Profile

  def initialize(name = nil, desc = nil, &block)
    @name = name
    @desc = desc
    @population = 1.0
    @address_bits = 32
    instance_eval(&block) if block
  end

  attr_accessor :name
  attr_accessor :desc
  attr_accessor :population

  attr_accessor :address_bits

  attr_accessor :nx_supported
  attr_accessor :nx_policy

  def nx_enabled
    if @nx_policy.nil?
      nil
    elsif @nx_supported and @nx_policy == :on
      true
    else
      false
    end
  end

  attr_accessor :smep_supported
  attr_accessor :smep_policy

  def smep_enabled
    if @smep_policy.nil?
      nil
    elsif @smep_supported and @smep_policy == :on
      true
    else
      false
    end
  end

  def is_arm?
    kind_of? ARM
  end

  class X86 < Hardware
    def initialize(name = 'x86', desc = '32-bit x86', &block)
      @address_bits = 32
      super(name, desc, &block)
    end
  end

  class X86_NO_PAE < X86
    def initialize(&block)
      @sym = :x86_no_pae
      @nx_supported = false
      @nx_policy = :off
      @smep_supported = false
      @smep_policy = :off
      super('x86_no_pae', '32-bit x86 (no PAE)', &block)
    end
  end

  class X86_PAE < X86
    def initialize(&block)
      @sym = :x86_pae
      @nx_supported = true
      @nx_policy = :on
      @smep_supported = false
      @smep_policy = :off
      super('x86_pae', '32-bit x86 (PAE)', &block)
    end
  end

  class X64 < Hardware
    def initialize(&block)
      @sym = :x64
      @address_bits = 64
      @nx_supported = true
      @nx_policy = :on
      @smep_supported = false
      @smep_policy = :off
      super('x64', '64-bit x64', &block)
    end
  end

  class ARM < Hardware
    def initialize(&block)
      @sym = :arm
      @address_bits = 32
      @nx_supported = true
      @nx_policy = :on
      @smep_supported = false
      @smep_policy = :off
      super('arm', 'ARM', &block)
    end
  end

end

class OS < Profile
  def initialize(family = nil, name = nil, sp = 0, address_bits = 32, &block)
    @family = family
    @name = name
    @sp = sp
    @address_bits = address_bits
    @population = 1.0
    instance_eval(&block) if block
  end

  def to_sym
    if @sym
      @sym
    elsif @name.nil?
      "any".to_sym
    else
      spstr = "sp#{@sp}"
      spstr = "rtm" if spstr == "sp0"

      "#{@family}_#{(@name || "any").downcase}_#{spstr}".to_sym
    end
  end

  def compatible_with?(hw)
    false
  end

  def recalibrate(target)
    super

    if @user_aslr_policy_exe_images == :off
      @user_aslr_entropy_exe_images = 0
    end
    
    if @user_aslr_policy_lib_images == :off
      @user_aslr_entropy_lib_images = 0
    end

    if @user_aslr_policy_force_relocation == :off
      @user_aslr_entropy_force_relocation = 0
    end
    
    if @user_aslr_policy_top_down == :off
      @user_aslr_entropy_top_down = 0
    end
    
    if @user_aslr_policy_bottom_up == :off
      @user_aslr_entropy_bottom_up = 0
    end
    
    if @user_aslr_policy_stacks == :off
      @user_aslr_entropy_stacks = 0
    end
    
    if @user_aslr_policy_heaps == :off
      @user_aslr_entropy_heaps = 0
    end
    
    if @user_aslr_policy_peb == :off
      @user_aslr_entropy_peb = 0
    end
  end

  attr_accessor :family
  attr_accessor :name
  attr_accessor :sp
  attr_accessor :address_bits
  attr_accessor :population

  attr_accessor :kernel_nx_supported
  attr_accessor :kernel_nx_policy_stacks
  attr_accessor :kernel_nx_policy_page_tables
  attr_accessor :kernel_nx_policy_image_data_sections

  attr_accessor :kernel_aslr_supported

  attr_accessor :kernel_aslr_policy_kernel_images
  attr_accessor :kernel_aslr_policy_driver_images
  attr_accessor :kernel_aslr_policy_stacks
  attr_accessor :kernel_aslr_policy_page_tables

  attr_accessor :kernel_aslr_entropy_kernel_images
  attr_accessor :kernel_aslr_entropy_driver_images
  attr_accessor :kernel_aslr_entropy_stacks
  attr_accessor :kernel_aslr_entropy_page_tables

  attr_accessor :kernel_smep_supported
  attr_accessor :kernel_smep_policy

  def kernel_smep_enabled
    if @kernel_smep_policy.nil?
      nil
    elsif @kernel_smep_supported and @kernel_smep_policy == :on
      true
    else
      false
    end
  end

  attr_accessor :kernel_null_deref_prevention_supported
  attr_accessor :kernel_null_deref_prevention_enabled
  attr_accessor :kernel_null_deref_prevention_version

  attr_accessor :user_nx_supported
  attr_accessor :user_nx_policy

  def user_nx_enabled
    if @user_nx_policy.nil?
      nil
    elsif @user_nx_supported and [:on, :optin, :optout].include?(@user_nx_policy)
      true
    else
      false
    end
  end

  attr_accessor :user_aslr_supported
  attr_accessor :user_aslr_force_relocation_supported
  attr_accessor :user_aslr_bottom_up_supported
  attr_accessor :user_aslr_bottom_up_he_supported
  attr_accessor :user_aslr_top_down_supported

  attr_accessor :user_aslr_policy_exe_images
  attr_accessor :user_aslr_policy_lib_images
  attr_accessor :user_aslr_policy_force_relocation
  attr_accessor :user_aslr_policy_top_down
  attr_accessor :user_aslr_policy_bottom_up
  attr_accessor :user_aslr_policy_bottom_up_he
  attr_accessor :user_aslr_policy_stacks
  attr_accessor :user_aslr_policy_heaps
  attr_accessor :user_aslr_policy_peb

  attr_accessor :user_aslr_entropy_exe_images
  attr_accessor :user_aslr_entropy_lib_images
  attr_accessor :user_aslr_entropy_force_relocation
  attr_accessor :user_aslr_entropy_top_down
  attr_accessor :user_aslr_entropy_bottom_up
  attr_accessor :user_aslr_entropy_stacks
  attr_accessor :user_aslr_entropy_heaps
  attr_accessor :user_aslr_entropy_peb

  attr_accessor :user_heap_safe_unlinking
  attr_accessor :user_heap_alloc_order_randomization
  attr_accessor :user_heap_alloc_order_entropy_bits
  attr_accessor :user_heap_termination_policy

  def user_heap_termination_enabled
    if @user_heap_termination_policy.nil?
      nil
    elsif @user_heap_termination_supported and [:on, :optin, :optout].include?(@user_heap_termination_policy)
      true
    else
      false
    end
  end

  attr_accessor :default_stack_protection_enabled
  attr_accessor :default_stack_protection_version

  module DefaultOS
    def inherit_defaults
      @kernel_nx_supported = false
      @kernel_nx_policy_stacks = :off
      @kernel_nx_policy_page_tables = :off
      @kernel_nx_policy_image_data_sections = :off
      @kernel_aslr_supported = false
      @kernel_aslr_policy_kernel_images = :off
      @kernel_aslr_policy_driver_images = :off
      @kernel_aslr_policy_stacks = :off
      @kernel_aslr_policy_page_tables = :off
      @kernel_aslr_entropy_kernel_images = 0
      @kernel_aslr_entropy_driver_images = 0
      @kernel_aslr_entropy_stacks = 0
      @kernel_aslr_entropy_page_tables = 0
      @kernel_smep_supported = false
      @kernel_smep_policy = :off
      @kernel_null_deref_prevention_supported = false
      @kernel_null_deref_prevention_enabled = false
      @kernel_null_deref_prevention_version = nil
      @user_nx_supported = false
      @user_nx_policy = :off
      @user_aslr_supported = false
      @user_aslr_force_relocation_supported = false
      @user_aslr_bottom_up_supported = false
      @user_aslr_bottom_up_he_supported = false
      @user_aslr_top_down_supported = false
      @user_aslr_policy_exe_images = :off
      @user_aslr_policy_lib_images = :off
      @user_aslr_policy_force_relocation = :off
      @user_aslr_policy_top_down = :off
      @user_aslr_policy_bottom_up = :off
      @user_aslr_policy_bottom_up_he = :off
      @user_aslr_policy_stacks = :off
      @user_aslr_policy_heaps = :off
      @user_aslr_policy_peb = :off
      @user_aslr_entropy_exe_images = 0
      @user_aslr_entropy_lib_images = 0
      @user_aslr_entropy_force_relocation = 0
      @user_aslr_entropy_top_down = 0
      @user_aslr_entropy_bottom_up = 0
      @user_aslr_entropy_stacks = 0
      @user_aslr_entropy_heaps = 0
      @user_aslr_entropy_peb = 0
      @user_heap_safe_unlinking = false 
      @user_heap_alloc_order_randomization = false
      @user_heap_alloc_order_entropy_bits = 0
      @user_heap_termination_policy = :off
      @default_stack_protection_enabled = false
      @default_stack_protection_version = nil
    end
  end

  #
  # Population metrics from http://en.wikipedia.org/wiki/Windows
  #
  class Windows < OS
    include DefaultOS

    def initialize(name, sp, address_bits = 32, &block)
      super(:windows, name, sp, address_bits, &block)

      inherit_defaults

      @kernel_nx_policy_paged_pool = :off
      @kernel_nx_policy_nonpaged_pool = :off
      @kernel_nx_policy_session_pool = :off
      @kernel_nx_policy_initial_stack = :off
      @kernel_nx_policy_hyperspace = :off
      @kernel_nx_policy_pcr = :off
      @kernel_nx_policy_shared_user_data = :off
      @kernel_nx_policy_system_ptes = :off
      @kernel_nx_policy_system_cache = :off
      @kernel_nx_policy_pfn_database = :off
      @kernel_nx_policy_hal_reserved = :off
      @kernel_aslr_policy_pools = :off
      @kernel_aslr_entropy_pools = :off
      @kernel_pool_safe_unlinking = false
      @kernel_pool_quota_pointer_encryption = false
      @kernel_pool_lookasidelist_cookie = false
      @user_sehop_supported = false
      @user_sehop_policy = :off
      @user_safeseh_supported = false
      @user_safeseh_policy = :off
      @user_heap_frontend = :nt
      @user_heap_block_header_cookies = false
      @user_heap_block_header_encryption = false
      @user_heap_unsafe_lookasidelists = true
      @user_heap_unsafe_freelists = true
      @user_heap_prevent_free_of_heap_base = false
      @user_heap_busy_block_integrity_check = false
      @user_heap_reserve_guard_pages = false
      @user_heap_reserve_guard_page_ratio = false
      @user_heap_large_allocation_padding = false
      @user_heap_large_allocation_padding_entropy = 0
      @user_pointer_encoding_uef = false
      @user_pointer_encoding_peb_fast_lock_routine = false
      @user_pointer_encoding_heap_commit_routine = false
    end

    def compatible_with?(hw)
      hw.kind_of?(Hardware::X86) or hw.kind_of?(Hardware::X64)
    end

    attr_accessor :kernel_nx_policy_paged_pool
    attr_accessor :kernel_nx_policy_nonpaged_pool
    attr_accessor :kernel_nx_policy_session_pool
    attr_accessor :kernel_nx_policy_initial_stack
    attr_accessor :kernel_nx_policy_hyperspace
    attr_accessor :kernel_nx_policy_pcr
    attr_accessor :kernel_nx_policy_shared_user_data
    attr_accessor :kernel_nx_policy_system_ptes
    attr_accessor :kernel_nx_policy_system_cache
    attr_accessor :kernel_nx_policy_pfn_database
    attr_accessor :kernel_nx_policy_hal_reserved

    attr_accessor :kernel_aslr_policy_pools
    attr_accessor :kernel_aslr_entropy_pools

    attr_accessor :kernel_pool_safe_unlinking
    attr_accessor :kernel_pool_quota_pointer_encryption
    attr_accessor :kernel_pool_lookasidelist_cookie

    attr_accessor :user_sehop_supported
    attr_accessor :user_sehop_policy

    def user_sehop_enabled
      if @user_sehop_policy.nil?
        nil
      elsif @user_sehop_supported and [:on, :optin, :optout].include?(@user_sehop_policy)
        true
      else
        false
      end
    end

    attr_accessor :user_safeseh_supported
    attr_accessor :user_safeseh_policy

    def user_safeseh_enabled
      if @user_safeseh_policy.nil?
        nil
      elsif @user_safeseh_supported and [:on, :optin, :optout].include?(@user_safeseh_policy)
        true
      else
        false
      end
    end

    attr_accessor :user_heap_frontend
    attr_accessor :user_heap_block_header_cookies
    attr_accessor :user_heap_block_header_encryption
    attr_accessor :user_heap_unsafe_lookasidelists
    attr_accessor :user_heap_unsafe_freelists
    attr_accessor :user_heap_prevent_free_of_heap_base
    attr_accessor :user_heap_busy_block_integrity_check
    attr_accessor :user_heap_reserve_guard_pages
    attr_accessor :user_heap_reserve_guard_page_ratio
    attr_accessor :user_heap_large_allocation_padding
    attr_accessor :user_heap_large_allocation_padding_entropy

    attr_accessor :user_pointer_encoding_uef
    attr_accessor :user_pointer_encoding_peb_fast_lock_routine
    attr_accessor :user_pointer_encoding_heap_commit_routine
  end

  class Windows2000 < Windows
    def initialize(sp, &block)
      super('2000', sp, &block)
      @population = 0.0147 # as of 12/2008
    end
  end

  ####
  ####
  #### Windows XP and Windows Server 2003
  ####
  ####

  #
  # Features or policies introduced with Windows XP
  #
  module FeatureSetWindowsXP
    include DefaultOS

    def inherit_xpsp2_features
      inherit_defaults

      @kernel_nx_supported = true
      @kernel_nx_policy_stacks = :on
      @kernel_nx_policy_hyperspace = :on

      if @address_bits == 64
        @kernel_nx_policy_pcr = :on
        @kernel_nx_policy_paged_pool = :on
        @kernel_nx_policy_session_pool = :on
        @kernel_nx_policy_image_data_sections = :on
      end

      @user_nx_supported = true
      @user_nx_policy = :optin
     
      # Windows XP SP2 supported randomization of PEBs
      @user_aslr_supported = true
      @user_aslr_peb_supported = true

      @user_safeseh_supported = true
      @user_safeseh_policy = :on

      @user_heap_safe_unlinking = true
      @user_heap_unsafe_lookasidelists = true
      @user_heap_unsafe_freelists = true

      @user_pointer_encoding_uef = true
      @user_pointer_encoding_peb_fast_lock_routine = true

      @default_stack_protection_enabled = true
      @default_stack_protection_version = :gs_vc7
    end

    def recalibrate_wow64_peb_entropy(target)
      #
      # PEB randomization is only effective for 32-bit and 64-bit native processes.
      # It is not effective for Wow64 processes on 64-bit.
      #
     
      if @user_aslr_supported and @user_aslr_peb_supported
        if target.os.address_bits == 32 or target.app.address_bits == 64
          @user_aslr_policy_peb = :on
          @user_aslr_entropy_peb = 4
        end
      end
    end

  end

  #
  # Features or policies common to Windows server SKUs.
  #
  module FeatureSetWindowsServer
    def inherit_server_policies

      if @user_nx_supported
        @user_nx_policy = :optout
      end

      if @user_sehop_supported
        @user_sehop_policy = :on
      end

    end
  end

  class WindowsXP < Windows
    include FeatureSetWindowsXP
    def initialize(sp, address_bits = 32, &block)
      super('XP', sp, address_bits, &block)

      if @sp >= 2
        inherit_xpsp2_features
      end

      if @sp >= 2
        @population = 0.7000 # as of 12/2008
      elsif @sp < 2
        @population = 0.0500 # guessed
      end
    end

    def recalibrate(target)
      super
      recalibrate_wow64_peb_entropy(target)
    end
  end

  class WindowsServer2003 < Windows
    include FeatureSetWindowsXP
    include FeatureSetWindowsServer

    def initialize(sp, address_bits = 32, &block)
      super('Server 2003', sp, address_bits, &block)

      if @sp >= 1
        inherit_xpsp2_features
      end

      inherit_server_policies

      @population = 0.0068 # as of 12/2008
    end

    def recalibrate(target)
      super
      recalibrate_wow64_peb_entropy(target)
    end
  end

  ####
  ####
  #### Windows Vista and Windows Server 2008
  ####
  ####

  #
  # Features or policies introduced with Windows Vista
  #
  module FeatureSetWindowsVista
    include FeatureSetWindowsXP

    def inherit_vista_rtm_features
      inherit_xpsp2_features

      @user_aslr_supported = true

      @user_aslr_policy_exe_images = :optin
      @user_aslr_policy_lib_images = :optin
      @user_aslr_policy_stacks = :optin
      @user_aslr_policy_heaps = :on

      @user_heap_frontend = :lfh
      @user_heap_frontend_version = :lfh_v1
      @user_heap_block_header_cookies = true
      @user_heap_unsafe_lookasidelists = false
      @user_heap_unsafe_freelists = false
      @user_heap_termination_policy = :optin

      @user_pointer_encoding_heap_commit_routine = true

      @default_stack_protection_enabled = true
      @default_stack_protection_version = :gs_vc81
    end

    def inherit_vista_sp1_features
      inherit_vista_rtm_features

      @kernel_aslr_supported = true

      @kernel_aslr_policy_kernel_images = :on
      @kernel_aslr_policy_driver_images = :on

      @kernel_aslr_entropy_kernel_images = 5
      @kernel_aslr_entropy_driver_images = 4

      @user_sehop_supported = true
      @user_sehop_policy = :optin
    end

    #
    # Recalibrate entropy of different regions based off the target
    # configuration (e.g. 32-bit vs. 64-bit OS/process).
    #
    def recalibrate_aslr_entropy(target)
      if target.os.address_bits == 32 or target.app.address_bits == 32
        if @user_aslr_policy_exe_images != :off
          @user_aslr_entropy_exe_images = 8
        else
        end
        if @user_aslr_policy_lib_images != :off
          @user_aslr_entropy_lib_images = 8
        end
        if @user_aslr_policy_stacks != :off
          @user_aslr_entropy_stacks = 14
        end
        if @user_aslr_policy_heaps != :off
          @user_aslr_entropy_heaps = 5
        end
      elsif target.os.address_bits == 64 and target.app.address_bits == 64
        if @user_aslr_policy_exe_images != :off
          @user_aslr_entropy_exe_images = 8
        else
        end
        if @user_aslr_policy_lib_images != :off
          @user_aslr_entropy_lib_images = 8
        end
        if @user_aslr_policy_stacks != :off
          @user_aslr_entropy_stacks = 14
        end
        if @user_aslr_policy_heaps != :off
          @user_aslr_entropy_heaps = 5
        end
      end
    end
  end

  class WindowsVista < Windows
    include FeatureSetWindowsVista

    def initialize(sp, address_bits = 32, &block)
      super('Vista', sp, address_bits, &block)

      if sp >= 1
        inherit_vista_sp1_features
      else
        inherit_vista_rtm_features
      end

      @population = 0.2000 # as of 12/2008
    end

    def recalibrate(target)
      super
      recalibrate_aslr_entropy(target)
      recalibrate_wow64_peb_entropy(target)
    end
  end

  class WindowsServer2008 < Windows
    include FeatureSetWindowsVista
    include FeatureSetWindowsServer

    def initialize(sp, address_bits = 32, &block)
      super('Server 2008', sp, address_bits, &block)

      inherit_vista_sp1_features
      inherit_server_policies

      @population = 0.0090 # unknown
    end
    
    def recalibrate(target)
      super
      recalibrate_aslr_entropy(target)
      recalibrate_wow64_peb_entropy(target)
    end
  end

  ####
  ####
  #### Windows 7 and Windows Server 2008 R2
  ####
  ####

  module FeatureSetWindows7
    include FeatureSetWindowsVista

    def inherit_win7_rtm_features
      inherit_vista_sp1_features

      @kernel_pool_safe_unlinking = true
    end

    def recalibrate_aslr_entropy(target)
      super

      @kernel_aslr_entropy_kernel_images = 5

      if @address_bits == 32
        @kernel_aslr_entropy_driver_images = 6
      elsif @address_bits == 64
        @kernel_aslr_entropy_driver_images = 8
      end
    end
  end

  class Windows7 < Windows
    include FeatureSetWindows7

    def initialize(sp, address_bits = 32, &block)
      super('7', sp, address_bits, &block)

      inherit_win7_rtm_features

      @population = 0.0010 # unknown
    end
    
    def recalibrate(target)
      super
      recalibrate_aslr_entropy(target)
      recalibrate_wow64_peb_entropy(target)
    end
  end

  class WindowsServer2008R2 < Windows
    include FeatureSetWindows7
    include FeatureSetWindowsServer

    def initialize(sp, &block)
      super('Server 2008 R2', sp, 64, &block)

      inherit_win7_rtm_features
      inherit_server_policies

      @population = 0.0010 # unknown
    end
    
    def recalibrate(target)
      super
      recalibrate_aslr_entropy(target)
      recalibrate_wow64_peb_entropy(target)
    end
  end

  ####
  ####
  #### Windows 8 client and server
  ####
  ####

  module FeatureSetWindows8
    include FeatureSetWindows7

    def inherit_win8_private_features
      inherit_win7_rtm_features

      @kernel_nx_policy_page_tables = :on
      @kernel_nx_policy_nonpaged_pool = :on
      @kernel_nx_policy_initial_stack = :on
      @kernel_nx_policy_pcr = :on
      @kernel_nx_policy_shared_user_data = :on
      @kernel_nx_policy_system_ptes = :on
      @kernel_nx_policy_system_cache = :on
      @kernel_nx_policy_pfn_database = :on
      @kernel_nx_policy_hal_reserved = :on

      @kernel_smep_supported = true
      @kernel_smep_policy = :on

      @kernel_null_deref_prevention_supported = true
      @kernel_null_deref_prevention_enabled = true

      @kernel_pool_quota_pointer_encryption = true
      @kernel_pool_lookasidelist_cookie = true

      @user_aslr_force_relocation_supported = true
      @user_aslr_bottom_up_supported = true
      @user_aslr_bottom_up_he_supported = true
      @user_aslr_top_down_supported = true

      @user_aslr_policy_force_relocation = :optin
      @user_aslr_policy_bottom_up = :optin
      @user_aslr_policy_bottom_up_he = :optin
      @user_aslr_policy_top_down = :on
      @user_aslr_policy_peb = :on

      # V2 does not have a linkoffset
      @user_heap_frontend_version = :lfh_v2
      @user_heap_alloc_order_randomization = true
      @user_heap_prevent_free_of_heap_base = true
      @user_heap_busy_block_integrity_check = true
      @user_heap_reserve_guard_pages = true
      @user_heap_reserve_guard_page_ratio = 0
      @user_heap_large_allocation_padding = true
      @user_heap_large_allocation_padding_entropy_bits = 4
      
      @default_stack_protection_enabled = true
      @default_stack_protection_version = :gs_vc10
    end

    def recalibrate_aslr_entropy(target)
      # All mitigations are fully enabled on ARM
      if target.hw.kind_of? Hardware::ARM
        @user_nx_policy = :on
        @user_sehop_supported = false
        @user_sehop_policy = :off
        @user_aslr_policy_exe_images = :on
        @user_aslr_policy_lib_images = :on
        @user_aslr_policy_force_relocation = :off
        @user_aslr_policy_bottom_up = :on
        @user_aslr_policy_bottom_up_he = :off
        @user_aslr_policy_stacks = :on
        @user_aslr_policy_heaps = :on
      end

      super

      if target.os.address_bits == 32 or target.app.address_bits == 32
        # This is a 32-bit process.
       
        if @user_aslr_policy_exe_images != :off
          @user_aslr_entropy_exe_images = 8
        end
        if @user_aslr_policy_lib_images != :off
          @user_aslr_entropy_lib_images = 8
        end
        if @user_aslr_policy_bottom_up != :off
          @user_aslr_entropy_bottom_up = 8
        end
        if @user_aslr_policy_top_down != :off
          @user_aslr_entropy_top_down = 8
        end
        if @user_aslr_policy_stacks != :off
          @user_aslr_entropy_stacks = 17
        end
        if @user_aslr_policy_heaps != :off
          @user_aslr_entropy_heaps = 8
        end
        if @user_aslr_policy_peb != :off
          @user_aslr_entropy_peb = 8
        end

        @user_heap_alloc_order_entropy_bits = 5
      elsif target.os.address_bits == 64 and target.app.address_bits == 64
        # This is a 64-bit process.
         
        if @user_aslr_policy_exe_images != :off
          @user_aslr_entropy_exe_images = 17
        end
        if @user_aslr_policy_lib_images != :off
          @user_aslr_entropy_lib_images = 19
        end
        if @user_aslr_policy_bottom_up != :off
          if target.app.user_aslr_policy_bottom_up_he == :on
            @user_aslr_entropy_bottom_up = 24
            @user_aslr_entropy_stacks = 33
            @user_aslr_entropy_heaps = 24
            @user_aslr_entropy_force_relocation = 24
          else
            @user_aslr_entropy_bottom_up = 8
            @user_aslr_entropy_stacks = 17
            @user_aslr_entropy_heaps = 8
            @user_aslr_entropy_force_relocation = 8
          end
        end
        if @user_aslr_policy_top_down != :off
          @user_aslr_entropy_top_down = 17
        end
        if @user_aslr_policy_peb != :off
          @user_aslr_entropy_peb = 17
        end

        @user_heap_alloc_order_entropy_bits = 6
      end
    end
  end

  class Windows8Client < Windows
    include FeatureSetWindows8

    def initialize(sp, address_bits = 32, &block)
      super('8 Client', sp, address_bits, &block)

      inherit_win8_private_features

      @population = 0.0010 # unknown
    end
    
    def compatible_with?(hw)
      super or hw.kind_of?(Hardware::ARM)
    end

    def recalibrate(target)
      super
      recalibrate_aslr_entropy(target)
    end
  end

  class Windows8Server < Windows
    include FeatureSetWindows8
    include FeatureSetWindowsServer

    def initialize(sp, &block)
      super('8 Server', sp, 64, &block)
     
      inherit_win8_private_features
      inherit_server_policies

      @population = 0.0010 # unknown
    end
    
    def recalibrate(target)
      super
      recalibrate_aslr_entropy(target)
    end
  end
end

#
# Population statistics from http://en.wikipedia.org/wiki/Internet_Explorer
#
class Application < Profile
  def initialize(name = nil, version = nil, address_bits = 32, &block)
    @name         = name
    @version      = version
    @address_bits = address_bits
    @population   = 1.0
    @kernel = false
    instance_eval(&block) if block
  end

  def to_sym
    if @sym
      @sym
    else
      ("#{(@name || "any")}" + (@version ? "_#{(@version || "")}" : "")).to_sym
    end
  end

  #
  # Returns true if this application is compatible with the provided hardware and
  # operating system.
  #
  def compatible_with?(hw, os)
    true
  end

  def recalibrate(target)
    super

    # If the application's NX policy is undefined and the OS
    # is configured to on or optout then set the application's
    # NX policy as on.
    if @user_nx_policy.nil? and [ :on, :optout ].include?(target.os.user_nx_policy)
      @user_nx_policy = :on
      @user_nx_permanent = true
    end

  end

  #
  # Some applications do not traditionally have local vulnerabilities (such as browsers).
  #
  def can_have_local_flaw?
    true
  end

  #
  # The name of the application.
  #
  attr_accessor :name

  #
  # The version of the application.
  #
  attr_accessor :version

  #
  # The size of the population that uses this application as a percentage 
  # of the total population.
  #
  attr_accessor :population

  #
  # The address bits of the process (default: 32).
  #
  attr_accessor :address_bits

  #
  # True if this application is a kernel mode application.
  #
  attr_accessor :kernel

  def is_user_app
    @kernel != true
  end

  def is_kernel_app
    @kernel == true
  end

  #
  #
  # Mitigation settings for this application.
  #
  # Note that application mitigation settings may not reflect the effective
  # mitigation settings of the target -- this is dependent on the configuration
  # and supported features of the operating system and the hardware.
  #
  #

  attr_accessor :user_nx_policy
  attr_accessor :user_nx_permanent

  def user_nx_enabled
    if @user_nx_policy.nil?
      nil
    elsif @user_nx_policy == :on
      true
    else
      false
    end
  end

  attr_accessor :user_sehop_policy

  def user_sehop_enabled
    if @user_sehop_policy.nil?
      nil
    elsif @user_sehop_policy == :on
      true
    else
      false
    end
  end

  attr_accessor :user_aslr_policy_force_relocation
  attr_accessor :user_aslr_policy_bottom_up
  attr_accessor :user_aslr_policy_bottom_up_he
  attr_accessor :user_aslr_policy_stacks

  attr_accessor :user_heap_frontend
  attr_accessor :user_heap_frontend_version
  attr_accessor :user_heap_termination_policy

  def user_heap_termination_enabled
    if @user_heap_termination_policy.nil?
      nil
    elsif @user_heap_termination_policy == :on
      true
    else
      false
    end
  end

  attr_accessor :exe_image_aslr_enabled
  attr_accessor :lib_images_aslr_enabled

  attr_accessor :restrict_automatic_restarts

  attr_accessor :default_stack_protection_enabled
  attr_accessor :default_stack_protection_version
  attr_accessor :default_vtguard_enabled
  attr_accessor :default_vtguard_level

  module UserModeApplication
  end
  
  module KernelModeApplication
  end

  class WindowsApplication < Application
    def initialize(name, version, address_bits, &block)
      super
    end

    def compatible_with?(hw, os)
      os.family == :windows and super
    end

    def recalibrate(target)
      super

      # NX is always enabled for 64-bit Windows processes.
      if @address_bits == 64 or target.hw.is_arm?
        @user_nx_policy = :on
        @user_nx_permanent = true
      end

      # ASLR is fully enabled on ARM.
      if target.hw.is_arm?
        @user_aslr_policy_force_relocation = true
        @exe_image_aslr_enabled = true
        @lib_images_aslr_enabled = true
      end

      # ASLR enabled for the EXE means stack/bottom-up randomization is
      # enabled (if supported by the OS).
      if @exe_image_aslr_enabled
        @user_aslr_policy_stacks = :on
        @user_aslr_policy_bottom_up = :on
      else
        @user_aslr_policy_stacks = :off
        @user_aslr_policy_bottom_up = :off
      end
    end
  end

  module InheritStackProtectionFromOSBuild
    def recalibrate(target)
      super

      # Inherit the default stack protection enable/version setting from the OS.  This
      # is assumed 
      if @default_stack_protection_enabled.nil?
        @default_stack_protection_enabled = target.os.default_stack_protection_enabled
        @default_stack_protection_version = target.os.default_stack_protection_version
      end
    end
  end
    

  class WindowsUserModeApplication < WindowsApplication
    include UserModeApplication

    def initialize(name, version, address_bits, &block)
      super

      @kernel = false
      @sym = :windows_user_app
    end
  end
  
  class WindowsKernelModeApplication < WindowsApplication
    include KernelModeApplication
    include InheritStackProtectionFromOSBuild

    def initialize(address_bits, &block)
      super("kernel", 0, address_bits, &block)
      
      @kernel = true
      @sym = :windows_kernel_app
    end
  end

  class WindowsInbox < WindowsUserModeApplication
    include InheritStackProtectionFromOSBuild

    def initialize(address_bits, name = 'inbox', &block)
      super(name, 0, address_bits, &block)
      @sym = :windows_inbox
    end

    def recalibrate(target)
      if target.os.user_aslr_policy_exe_images != :off
        @exe_image_aslr_enabled = true
        @lib_images_aslr_enabled = true
      end

      super

      if target.os.user_nx_policy != :off
        @user_nx_policy = :on
        @user_nx_permanent = true
      end

      if target.os.user_heap_termination_policy != :off
        @user_heap_termination_policy = :on
      end

      if target.os.user_aslr_policy_bottom_up != :off
        @user_aslr_policy_bottom_up = :on
      end

      if target.os.user_aslr_policy_bottom_up_he != :off
        @user_aslr_policy_bottom_up_he = :on
      end

      if target.os.user_sehop_policy != :off
        @user_sehop_policy = :on
      end

    end
  end

  class WindowsService < WindowsInbox
    def initialize(address_bits, &block)
      super(address_bits, 'svchost', &block)
      @sym = :windows_svchost
      @restrict_automatic_restarts = true
    end
  end

  ####
  ####
  #### Internet Explorer
  ####
  ####

  class IE < WindowsUserModeApplication
    include InheritStackProtectionFromOSBuild

    def initialize(name, version, address_bits, &block)
      super
      @sym = :ie
    end

    def can_have_local_flaw?
      false
    end

    def all_plugins
      [
        :none,
        :flash,
        :shockwave,
        :silverlight,
      ]
    end

    attr_accessor :plugins

    def plugin_capabilities
      {
        :none => lambda do |target|
        end,

        :flash => lambda do |target|
          target.cap.set_cap :can_spray_code_bottom_up, true
        end,

        :shockwave => lambda do |target|
          target.cap.set_cap :can_load_non_aslr_image, true
          target.cap.set_cap :can_load_non_aslr_non_safeseh_image, true
        end,

        :silverlight => lambda do |target|
          target.cap.set_cap :can_spray_code_bottom_up, true
        end,
      }
    end

    def recalibrate(target)
      # If the OS supports ASLR then IE has been built with ASLR
      # enabled.
      if target.os.user_aslr_policy_exe_images != :off
        @exe_image_aslr_enabled = true
      end
      
      if target.os.user_aslr_policy_lib_images != :off
        @lib_images_aslr_enabled = true
      end

      super

      # Assume the application inherits the default heap frontend
      @user_heap_frontend = target.os.user_heap_frontend

      # Assume that the application will opt-in to heap termination
      # if it is available.
      if target.os.user_heap_termination_enabled
        @user_heap_termination_policy = :on
      end
      
      # Default capabilities
      target.cap.set_cap :can_spray_data_bottom_up, true
      target.cap.set_cap :can_massage_heap, true

      # Capabilities introduced by the presence of plugins
      if plugins
        plugins.each do |plugin|
          plugin_cap = plugin_capabilities[plugin]

          if plugin_cap
            plugin_cap.call(target)
          end
        end
      end
    end
  end

  class IE6 < IE
    def initialize(address_bits, &block)
      super("IE6", 6.0, address_bits, &block)
      @sym = :ie6
    end

    def compatible_with?(hw, os)
      if os.kind_of?(OS::Windows2000) or 
         os.kind_of?(OS::WindowsXP) or 
         os.kind_of?(OS::WindowsServer2003)
         return super
      end

      return false
    end
  end

  class IE7 < IE
    def initialize(address_bits, &block)
      super("IE7", 7.0, address_bits, &block)
      @sym = :ie7
    end

    # 
    # IE7 runs on XP, Vista, Server 2003, and Server 2008
    #
    def compatible_with?(hw, os)
      if os.kind_of?(OS::WindowsXP) or 
         os.kind_of?(OS::WindowsVista) or
         os.kind_of?(OS::WindowsServer2003) or
         os.kind_of?(OS::WindowsServer2008)
         return super
      end

      return false
    end

    def recalibrate(target)
      super

      # .NET user controls can be loaded.
      target.cap.set_cap :can_load_non_aslr_image, true
    end
  end
  
  class IE8 < IE
    def initialize(address_bits, &block)
      super("IE8", 8.0, address_bits, &block)
      @sym = :ie8
      @restrict_automatic_restarts = true
    end

    # 
    # IE8 runs on XP, Vista, Win7, Server 2003, and Server 2008, and Server 2008 R2.
    #
    def compatible_with?(hw, os)
      if os.kind_of?(OS::WindowsXP) or 
         os.kind_of?(OS::WindowsVista) or
         os.kind_of?(OS::Windows7) or
         os.kind_of?(OS::WindowsServer2003) or
         os.kind_of?(OS::WindowsServer2008)
         os.kind_of?(OS::WindowsServer2008R2)
         return super
      end

      return false
    end

    def recalibrate(target)
      super

      if target.os.kind_of?(OS::WindowsXP) == false or target.os.sp >= 3
        @user_nx_policy = :on
        @user_nx_permanent = true
      end
      
      # mscorie.dll can be loaded currently
      target.cap.set_cap :can_load_non_aslr_image, true
    end
  end

  class IE9 < IE
    def initialize(address_bits, &block)
      super("IE9", 9.0, address_bits, &block)

      @sym = :ie9
      @user_nx_policy = :on
      @user_sehop_policy = :on
      @user_nx_permanent = true
      @restrict_automatic_restarts = true
    end

    # 
    # IE9 runs on Vista, Win7, Server 2008, and Server 2008 R2.
    #
    def compatible_with?(hw, os)
      if os.kind_of?(OS::WindowsVista) or
         os.kind_of?(OS::Windows7) or
         os.kind_of?(OS::WindowsServer2008)
         os.kind_of?(OS::WindowsServer2008R2)
         return super
      end

      return false
    end
  end

  class IE10 < IE
    def initialize(address_bits, &block)
      super("IE10", 10.0, address_bits, &block)

      @sym = :ie10
      @user_nx_policy = :on
      @user_nx_permanent = true
      @user_sehop_policy = :on
      @user_aslr_policy_force_relocation = :on
      @user_aslr_policy_bottom_up = :on
      @user_aslr_policy_bottom_up_he = :on
      @user_aslr_policy_stacks = :on

      @restrict_automatic_restarts = true
      @default_vtguard_enabled = true
      @default_vtguard_level = 1
    end

    # 
    # IE10 runs on Win8 Client/Server
    #
    def compatible_with?(hw, os)
      if os.kind_of?(OS::Windows8Client) or os.kind_of?(OS::Windows8Server)
         return super
      end

      return false
    end

    def recalibrate(target)
      super
    
      #
      # Spraying is currently considered impractical due to HEASLR.
      #
      if @address_bits == 64
        target.cap.set_cap :can_spray_data_bottom_up, false
        target.cap.set_cap :can_spray_code_bottom_up, false
      end
    end
  end

  ####
  ####
  #### Office
  ####
  ####

  class Office < WindowsUserModeApplication
    def initialize(name, version, address_bits, &block)
      super(name, version, address_bits, &block)

      @user_heap_frontend = :rockall
    end
    
    def can_have_local_flaw?
      false
    end
  end

  module FeatureSetOffice11
    def inherit_office11_features
      @user_safeseh_policy = :on

      @default_stack_protection_enabled = true
      @default_stack_protection_version = :gs_vc71
    end

    def recalibrate(target)
      target.cap.set_cap :can_spray_data_bottom_up, true, :private => true
    end
  end

  class Office11 < Office
    include FeatureSetOffice11
    def initialize(&block)
      super("Office11", "Office 2003 RTM", 32, &block)

      inherit_office11_features
    end
  end

  module FeatureSetOffice12
    include FeatureSetOffice11

    def inherit_office12_rtm_features
      inherit_office11_features

      @exe_image_aslr_enabled = false
      @lib_images_aslr_enabled = true

      @default_stack_protection_enabled = true
      @default_stack_protection_version = :gs_vc8
    end

    def inherit_office12_sp3_features
      inherit_office12_rtm_features

      @exe_image_aslr_enabled = true
    end
  end

  class Office12 < Office
    include FeatureSetOffice12

    def initialize(sp = 0, &block)
      super("Office12", "Office 2007 SP#{sp}", 32, &block)

      if sp >= 3
        inherit_office12_sp3_features
      else
        inherit_office12_rtm_features
      end
    end
  end

  module FeatureSetOffice14
    include FeatureSetOffice12

    def inherit_office14_features
      inherit_office12_sp3_features

      @user_nx_policy = :on
      @user_nx_permanent = true
      
      @user_sehop_policy = :on

      @default_stack_protection_enabled = true
      @default_stack_protection_version = :gs_vc81
    end
  end

  class Office14 < Office
    include FeatureSetOffice14

    def initialize(sp = 0, address_bits = 32, &block)
      super("Office14", "Office 2010 SP#{sp}", address_bits, &block)

      inherit_office14_features
    end
  end

  module FeatureSetOffice15
    include FeatureSetOffice14

    def inherit_office15_features
      inherit_office14_features

      @user_aslr_policy_force_relocation = :on
      @user_aslr_policy_bottom_up = :on
      @user_aslr_policy_bottom_up_he = :on

      @default_stack_protection_enabled = true
      @default_stack_protection_version = :gs_vc10
    end
  end

  class Office15 < Office
    include FeatureSetOffice15

    def initialize(sp = 0, address_bits = 32, &block)
      super("Office15", "Office15 SP#{sp}", address_bits, &block)

      inherit_office15_features
    end
  end

end

class Capabilities < Profile
  def initialize(&block)
    instance_eval(&block) if block
  end

  def to_sym
    :default
  end

  attr_accessor :can_load_non_aslr_image
  attr_accessor :can_load_non_aslr_non_safeseh_image
  attr_accessor :can_spray_data_bottom_up
  attr_accessor :can_spray_code_bottom_up
  attr_accessor :can_massage_heap
  attr_accessor :can_discover_desired_code_address
  attr_accessor :can_discover_desired_data_address
  attr_accessor :can_discover_attacker_controlled_code_address
  attr_accessor :can_discover_attacker_controlled_data_address
  attr_accessor :can_discover_stack_address
  attr_accessor :can_discover_heap_address
  attr_accessor :can_discover_heap_base_address
  attr_accessor :can_discover_peb_address
  attr_accessor :can_discover_image_address
  attr_accessor :can_discover_ntdll_image_address
  attr_accessor :can_discover_non_safeseh_image_address

  def set_cap_defaults_true
    set_cap_defaults(true)
  end

  def set_cap_defaults_false
    set_cap_defaults(false)
  end

  def set_cap_defaults(tf)
    @can_load_non_aslr_image = tf
    @can_load_non_aslr_non_safeseh_image = tf
    @can_spray_data_bottom_up = tf
    @can_spray_code_bottom_up = tf
    @can_massage_heap = tf
    @can_discover_desired_code_address = tf
    @can_discover_desired_data_address = tf
    @can_discover_attacker_controlled_code_address = tf
    @can_discover_attacker_controlled_data_address = tf
    @can_discover_stack_address = tf
    @can_discover_heap_address = tf
    @can_discover_heap_base_address = tf
    @can_discover_peb_address = tf
    @can_discover_image_address = tf
    @can_discover_ntdll_image_address = tf
    @can_discover_non_safeseh_image_address = tf
  end

  def set_cap(methodsym, value, opts = {})
    # TODO save opts
    #   :private
    #   :resources
    #   :people
    self.send(methodsym.to_s + "=", value)
  end

  def recalibrate(target)
    super

    if @can_load_non_aslr_non_safeseh_image
      @can_discover_non_safeseh_image_address = true
    end

    if target.app.is_user_app
      if @can_load_non_aslr_image or
         @can_discover_image_address or
         target.app.exe_image_aslr_enabled == false or
         target.app.lib_images_aslr_enabled == false or
         target.os.user_aslr_entropy_exe_images == 0 or
         target.os.user_aslr_entropy_lib_images == 0

        @can_discover_image_address = true

        if target.os.user_aslr_entropy_lib_images == 0
          @can_discover_ntdll_image_address = true
        end

      end

      if target.os.user_aslr_entropy_stacks == 0
        @can_discover_stack_address = true
      end
      
      if target.os.user_aslr_entropy_heaps == 0
        @can_discover_heap_address = true
        @can_discover_heap_base_address = true
      end
      
      if target.os.user_aslr_entropy_peb == 0
        @can_discover_peb_address = true
      end

    else

      if target.os.kernel_aslr_entropy_stacks == 0
        @can_discover_stack_address = true
      end
      
    end
  end
end

class Flaw < Profile
  def initialize
    @root_cause = nil
    @local = false
  end

  def desc
    'Undefined'
  end

  def to_sym
    classification.to_sym
  end

  #
  # The ultimate root cause of the flaw (if known).
  #
  attr_accessor :root_cause

  #
  # The general classification for this flaw (corruption, control transfer, double free, 
  # etc).
  #
  def classification
    :any
  end

  #
  # An array of flaws that can be enabled via this one.
  #
  def can_enable
    [ ]
  end

  #
  # A flaw is a 'root' flaw if it does not enable any other flaws.
  #
  def root?
    can_enable.empty?
  end

  #
  # Returns true if the flaw is compatible with the provided
  # environment.
  #
  def compatible_with?(hw, os, app)
    # Cannot write via %n on Windows.
    if @classification == :write and @root_cause == :format_string and os.kind_of?(OS::Windows) == true
      return false
    end

    return true
  end

  def recalibrate(target)
    super

    # Inherit stack protection settings from the OS if none have been explicitly defined.
    if @function_stack_protection_enabled.nil?
      @function_stack_protection_enabled = target.app.default_stack_protection_enabled
    end

    if @function_stack_protection_version.nil?
      @function_stack_protection_version = target.app.default_stack_protection_version
    end

    if @vtguard_enabled.nil?
      @vtguard_enabled = target.app.default_vtguard_enabled
    end
    
    if @vtguard_level.nil?
      @vtguard_level = target.app.default_vtguard_level
    end
  end

  #
  # True if this flaw can only be locally exploited.
  #
  attr_accessor :local

  #
  # Flaw can be triggered remotely
  #
  def remote
    @local.nil? || @local == false
  end

  #
  # The region of corruption.
  #
  def self.corruption_regions
    [
      :any,
      :stack,
      :heap,
      :dataseg,
      :none
    ]
  end

  attr_accessor :corruption_region

  #
  # The displacement of the corruption.
  #
  # Relative corruption happens with respect to some point in memory (such as
  # an array).
  #
  # Absolute corruption happens anywhere in the address space and is not
  # relative to a specific point in memory.
  #
  def self.corruption_displacements
    [
      :relative,
      :absolute
    ]
  end

  attr_accessor :corruption_displacement

  #
  # The direction in which corruption can occur.
  #
  # Forward corruption writes from lowest to highest address (most common).
  #
  # Reverse corruption writes from highest to lowest address.
  #
  def self.corruption_directions
    [
      :forward,
      :reverse
    ]
  end

  attr_accessor :corruption_direction

  #
  # The position at which corruption occurs.  
  #
  # Adjacent corruption starts immediately at the beginning or end of a buffer
  # that is being overflown.
  #
  # Non-adjacent corruption has no such restrictions.
  #
  def self.corruption_positions
    [
      :adjacent,
      :nonadjacent
    ]
  end

  attr_accessor :corruption_position

  #
  # True if the corruption length is controlled precisely by the attacker.
  #
  attr_accessor :corruption_length_controlled

  #
  # True if the function with the flaw has stack protection enabled.
  #
  attr_accessor :function_stack_protection_enabled

  #
  # The version of stack protection that the code was built with (default=gs_vc8).
  #
  # gs_vc7   = VS2002
  # gs_vc7.1 = VS2003
  # gs_vc8   = VS2005
  # gs_vc10  = VS2010
  #
  attr_accessor :function_stack_protection_version

  StackProtectionVersions = [
    #
    # Protection of functions with string-based buffers.  Initially
    # shipped with VC7 (VS2002)
    #
    :gs_vc7,

    #
    # Variable re-ordering added.  Initially shipped with VC7.1 (VS2003).
    #
    :gs_vc71,

    #
    # Shadow copy of parameters.  Initially shipped with VC8 (VS2005).
    #
    :gs_vc8,

    #
    # Support for strict_gs_check pragma.  Initially shipped with VC8.1 (VS2005
    # SP1).
    #
    :gs_vc81,

    #
    # Improved heuristics and optimization.  Initially shipped with VC10
    # (VS2010).
    #
    :gs_vc10
  ]

  #
  # If stack protection is enabled for the flawed function, then this boolean
  # indicates whether or not locals are reordered.
  #
  def function_stack_locals_reordered
    [ :gs_vc71, :gs_vc8, :gs_vc81, :gs_vc10 ].include?(@function_stack_protection_version)
  end

  #
  # If stack protection is enabled for the flawed function, then this boolean
  # indicates whether or not parameters are shadow copied.
  #
  def function_stack_parameters_shadow_copied
    [ :gs_vc8, :gs_vc81, :gs_vc10 ].include?(@function_stack_protection_version)
  end

  #
  # True if the flaw is triggered within a catch-all SEH scope.
  #
  attr_accessor :triggered_in_catch_all_seh_scope

  #
  # True if the module and class affected by the flaw have vtguard enabled.
  #
  attr_accessor :vtguard_enabled

  #
  # The level of instrumentation that has been inserted by vtguard.
  #
  attr_accessor :vtguard_level

  attr_accessor :can_transfer_control_anywhere
  attr_accessor :can_write_anywhere
  attr_accessor :can_read_anywhere

  attr_accessor :can_corrupt_return_address
  attr_accessor :can_corrupt_frame_pointer
  attr_accessor :can_corrupt_seh_frame
  attr_accessor :can_trigger_seh_exception

  attr_accessor :can_corrupt_object_vtable_pointer
  attr_accessor :can_corrupt_function_pointer
  attr_accessor :can_corrupt_write_target_pointer

  attr_accessor :can_corrupt_parent_frame_locals
  attr_accessor :can_corrupt_child_or_current_frame_locals

  attr_accessor :can_corrupt_heap_block_header
  attr_accessor :can_corrupt_heap_free_links

  attr_accessor :can_free_arbitrary_address

  attr_accessor :can_discover_stack_cookie
  attr_accessor :can_discover_vtguard_cookie

  ####
  #### Flaw primitives
  ####

  class ControlTransfer < Flaw
    def desc 
      'A flaw that permits a control transfer.'
    end

    def classification
      :control_transfer
    end

    def to_sym
      :control_transfer
    end
  end


  ####
  #### Absolute control transfer
  ####
  
  class AbsoluteControlTransfer < ControlTransfer
    def initialize
      super

      @can_transfer_control_anywhere = true
    end

    def desc
      "A flaw that permits a control transfer to an absolute address."
    end

    def to_sym
      :absolute_control_transfer
    end
  end

  class AbsoluteControlTransferViaTypeConfusion < AbsoluteControlTransfer
    def initialize
      super
      @root_cause = :type_confusion
    end

    def desc
      "A flaw that permits a control transfer to an absolute address as a result of a type confusion."
    end

    def to_sym
      :absolute_control_transfer_via_type_confusion
    end
  end

  class AbsoluteControlTransferViaUninitializedUse < AbsoluteControlTransfer
    def initialize
      super
      @root_cause = :uninitialized_use
    end

    def desc
      "A flaw that permits a control transfer to an absolute address as a result of using uninitialized memory."
    end

    def to_sym
      :absolute_control_transfer_via_uninitialized_use
    end
  end

  class AbsoluteControlTransferViaHeapUseAfterFree < AbsoluteControlTransferViaUninitializedUse
    def initialize
      super
    end

    def desc
      "A flaw that permits a control transfer to an absolute address as a result of using uninitialized memory that has been prematurely freed."
    end

    def to_sym
      :absolute_control_transfer_via_heap_use_after_free
    end
  end

  class AbsoluteControlTransferViaAbsoluteWrite < AbsoluteControlTransfer
    def initialize
      super
      @root_cause = :absolute_write
    end

    def desc
      "A flaw that permits a control transfer to an absolute address as a result of being able to write a value to an absolute address."
    end

    def to_sym
      :absolute_control_transfer_via_absolute_write
    end
  end

  class AbsoluteControlTransferViaRelativeWrite < AbsoluteControlTransfer
    def initialize
      super
      @root_cause = :relative_write
    end

    def desc
      "A flaw that permits a control transfer to an absolute address as a result of being able to write a value to a relative address."
    end

    def to_sym
      :absolute_control_transfer_via_relative_write
    end
  end
  class AbsoluteControlTransferViaAbsoluteRead < AbsoluteControlTransfer
    def initialize
      super
      @root_cause = :absolute_read
    end

    def desc
      "A flaw that permits a control transfer to an absolute address as a result of being able to read a value from an absolute address."
    end

    def to_sym
      :absolute_control_transfer_via_absolute_read
    end
  end

  class AbsoluteControlTransferViaRelativeRead < AbsoluteControlTransfer
    def initialize
      super
      @root_cause = :relative_read
    end

    def desc
      "A flaw that permits a control transfer to an absolute address as a result of being able to read a value from a relative address."
    end

    def to_sym
      :absolute_control_transfer_via_relative_read
    end
  end

  class AbsoluteControlTransferViaNewStackPointer < AbsoluteControlTransfer
    def initialize
      super
    end

    def desc
      "A flaw that permits a control transfer to an absolute address as a result of altering the stack pointer."
    end

    def to_sym
      :absolute_control_transfer_via_new_stack_pointer
    end
  end

  class AbsoluteControlTransferViaNullDereference < AbsoluteControlTransfer
    def initialize
      super
      @root_cause = :null_dereference
    end

    def desc
      "A flaw that permits a control transfer to an absolute address as a result of a NULL dereference."
    end

    def to_sym
      :absolute_control_transfer_via_null_dereference
    end
  end

  ####
  #### Absolute read
  ####

  class AbsoluteRead < Flaw
    def initialize
      super

      @can_read_anywhere = true
    end

    def classification
      :read
    end

    def desc
      "A flaw that permits reading from an absolute address."
    end

    def to_sym
      :absolute_read
    end

    def can_enable
      [
        AbsoluteControlTransferViaAbsoluteRead,
        AbsoluteWriteViaAbsoluteRead
      ]
    end
  end

  class AbsoluteReadViaTypeConfusion < AbsoluteRead
    def initialize
      super
      @root_cause = :type_confusion
    end

    def desc
      "A flaw that permits reading from an absolute address due to a type confusion."
    end

    def to_sym
      :absolute_read_via_type_confusion
    end
  end

  class AbsoluteReadViaUninitializedUse < AbsoluteRead
    def initialize
      super
      @root_cause = :uninitialized_use
    end

    def desc
      "A flaw that permits reading from an absolute address as a result of using uninitialized memory."
    end

    def to_sym
      :absolute_read_via_uninitialized_use
    end
  end

  class AbsoluteReadViaFormatString < AbsoluteRead
    def initialize
      super
      @root_cause = :format_string
    end

    def desc
      "A flaw that permits reading from an absolute address due to a format string vulnerability."
    end

    def to_sym
      :absolute_read_via_format_string
    end
  end

  class AbsoluteReadViaAbsoluteWrite < AbsoluteRead
    def initialize
      super
      @root_cause = :absolute_write
    end

    def desc
      "A flaw that permits reading from an absolute address due to an arbitrary write."
    end

    def to_sym
      :absolute_read_via_absolute_write
    end
  end

  class AbsoluteReadViaRelativeWrite < AbsoluteRead
    def initialize
      super
      @root_cause = :relative_write
    end

    def desc
      "A flaw that permits reading from an absolute address due to a relative write."
    end

    def to_sym
      :absolute_read_via_relative_write
    end
  end

  class AbsoluteReadViaNullDereference < AbsoluteRead
    def initialize
      super
      @root_cause = :null_dereference
    end

    def desc
      "A flaw that permits reading from an absolute address due to a NULL dereference."
    end

    def to_sym
      :absolute_read_via_null_dereference
    end
  end


  ####
  #### Relative read
  ####

  class RelativeRead < Flaw
    def initialize
      super
    end

    def classification
      :read
    end

    def desc
      "A flaw that permits reading from a relative address."
    end

    def to_sym
      :relative_read
    end

    def can_enable
      [
        AbsoluteControlTransferViaRelativeRead
      ]
    end
  end

  class RelativeReadViaTypeConfusion < RelativeRead
    def initialize
      super
      @root_cause = :type_confusion
    end

    def desc
      "A flaw that permits reading from an absolute address due to a type confusion."
    end

    def to_sym
      :relative_read_via_type_confusion
    end
  end

  class RelativeReadViaUninitializedUse < RelativeRead
    def initialize
      super
      @root_cause = :uninitialized_use
    end

    def desc
      "A flaw that permits reading from an relative address as a result of using uninitialized memory."
    end

    def to_sym
      :relative_read_via_uninitialized_use
    end
  end

  class RelativeReadViaFormatString < RelativeRead
    def initialize
      super
      @root_cause = :format_string
    end

    def desc
      "A flaw that permits reading from an absolute address due to a format string vulnerability."
    end

    def to_sym
      :relative_read_via_format_string
    end
  end

  class RelativeReadViaAbsoluteWrite < RelativeRead
    def initialize
      super
      @root_cause = :absolute_write
    end

    def desc
      "A flaw that permits reading from an absolute address due to an arbitrary write."
    end

    def to_sym
      :relative_read_via_absolute_write
    end
  end

  class RelativeReadViaRelativeWrite < RelativeRead
    def initialize
      super
      @root_cause = :relative_write
    end

    def desc
      "A flaw that permits reading from an absolute address due to a relative write."
    end

    def to_sym
      :relative_read_via_relative_write
    end
  end

  ####
  #### Absolute write
  ####

  class AbsoluteWrite < Flaw
    def initialize
      super

      @root_cause = :absolute_write

      @can_write_anywhere = true

      @corruption_region = :any
      @corruption_displacement = :absolute
      @corruption_direction = :forward
      @corruption_position = :nonadjacent
    end

    def classification
      :write
    end

    def desc
      "A flaw that permits writing a value to an absolute address."
    end

    def to_sym
      :absolute_write
    end

    def can_enable
      [
        AbsoluteControlTransferViaAbsoluteWrite,
        AbsoluteReadViaAbsoluteWrite,
        RelativeReadViaAbsoluteWrite
      ]
    end
  end

  class AbsoluteWriteViaTypeConfusion < AbsoluteWrite
    def initialize
      super
      @root_cause = :type_confusion
    end

    def desc
      "A flaw that permits writing a value to an absolute address as a result of a type confusion."
    end

    def to_sym
      :absolute_write_via_type_confusion
    end
  end

  class AbsoluteWriteViaUninitializedUse < AbsoluteWrite
    def initialize
      super
      @root_cause = :uninitialized_use
    end

    def desc
      "A flaw that permits writing a value to an absolute address as a result of using uninitialized memory."
    end

    def to_sym
      :absolute_write_via_uninitialized_use
    end
  end

  class AbsoluteWriteViaHeapUseAfterFree < AbsoluteWriteViaUninitializedUse
    def initialize
      super
    end

    def desc
      "A flaw that permits writing a value to an absolute address as a result of using heap memory that as freed prematurely."
    end

    def to_sym
      :absolute_write_via_heap_use_after_free
    end
  end

  class AbsoluteWriteViaDoubleFree < AbsoluteWrite
    def initialize
      super
      @root_cause = :double_free
    end

    def desc
      "A flaw that permits writing a value to an absolute address as a result of a double free."
    end

    def to_sym
      :absolute_write_via_double_free
    end
  end

  class AbsoluteWriteViaArbitraryFree < AbsoluteWrite
    def initialize
      super
      @root_cause = :arbitrary_free
    end

    def desc
      "A flaw that permits writing a value to an absolute address as a result of an arbitrary free."
    end

    def to_sym
      :absolute_write_via_arbitrary_free
    end
  end

  class AbsoluteWriteViaNullDereference < AbsoluteWrite
    def initialize
      super
      @root_cause = :null_dereference
    end

    def desc
      "A flaw that permits writing a value to an absolute address as a result of a NULL dereference."
    end

    def to_sym
      :absolute_write_via_null_dereference
    end
  end

  class AbsoluteWriteViaRelativeWrite < AbsoluteWrite
    def initialize
      super
      @root_cause = :relative_write
    end

    def desc
      "A flaw that permits writing a value to an absolute address as a result of a relative write."
    end

    def to_sym
      :absolute_write_via_relative_write
    end
  end

  class AbsoluteWriteViaAbsoluteRead < AbsoluteWrite
    def initialize
      super
      @root_cause = :relative_write
    end

    def desc
      "A flaw that permits writing a value to an absolute address as a result of an absolute read."
    end

    def to_sym
      :absolute_write_via_absolute_read
    end
  end

  class AbsoluteWriteViaFormatString < AbsoluteWrite
    def initialize
      super
      @root_cause = :format_string
    end

    def desc
      "A flaw that permits writing a value to an absolute address as a result of a format string vulnerability."
    end

    def to_sym
      :absolute_write_via_type_confusion
    end
  end

  ####
  #### Relative write
  ####

  class RelativeWrite < Flaw
    def initialize
      super

      @root_cause = :relative_write

      @corruption_region = :any
      @corruption_displacement = :relative
      @corruption_direction = :forward
      @corruption_position = :adjacent
    end

    def classification
      :write
    end

    def desc
      "A flaw that permits writing a value to a relative address."
    end

    def to_sym
      :relative_write
    end

    def can_enable
      [
        AbsoluteControlTransferViaRelativeWrite,
        AbsoluteReadViaRelativeWrite,
        RelativeReadViaRelativeWrite,
        AbsoluteWriteViaRelativeWrite
      ]
    end
  end

  class RelativeWriteViaTypeConfusion < RelativeWrite
    def initialize
      super
      @root_cause = :type_confusion
    end

    def desc
      "A flaw that permits writing a value to a relative address as a result of a type confusion."
    end

    def to_sym
      :relative_write_via_type_confusion
    end
  end

  class RelativeWriteViaUninitializedUse < RelativeWrite
    def initialize
      super
      @root_cause = :uninitialized_use
    end

    def desc
      "A flaw that permits writing a value to a relative address as a result of an uninitialized use."
    end

    def to_sym
      :relative_write_via_uninitialized_use
    end
  end

  ####
  #### Higher level flaw classes
  ####

  class RelativeStackCorruption < RelativeWrite
    def initialize
      super
      @root_cause = :stack_corruption
      @corruption_region = :stack
    end

    def to_sym
      :relative_stack_corruption
    end
  end

  class RelativeStackCorruptionForwardAdjacent < RelativeStackCorruption
    def initialize
      super
      @corruption_direction = :forward
      @corruption_position = :adjacent
    end

    def to_sym
      :relative_stack_corruption_forward_adjacent
    end
  end

  class RelativeStackCorruptionForwardNonAdjacent < RelativeStackCorruption
    def initialize
      super
      @corruption_direction = :forward
      @corruption_position = :nonadjacent
    end

    def to_sym
      :relative_stack_corruption_forward_nonadjacent
    end
  end

  class RelativeStackCorruptionReverseAdjacent < RelativeStackCorruption
    def initialize
      super
      @corruption_direction = :reverse
      @corruption_position = :adjacent
    end

    def to_sym
      :relative_stack_corruption_reverse_adjacent
    end
  end

  class RelativeStackCorruptionReverseNonAdjacent < RelativeStackCorruption
    def initialize
      super
      @corruption_direction = :reverse
      @corruption_position = :nonadjacent
    end

    def to_sym
      :relative_stack_corruption_reverse_nonadjacent
    end
  end

  class RelativeHeapCorruption < RelativeWrite
    def initialize
      super
      @root_cause = :heap_corruption
      @corruption_region = :heap
    end

    def to_sym
      :relative_heap_corruption
    end
  end

  class RelativeHeapCorruptionForwardAdjacent < RelativeHeapCorruption
    def initialize
      super
      @corruption_direction = :forward
      @corruption_position = :adjacent
    end

    def to_sym
      :relative_heap_corruption_forward_adjacent
    end
  end

  class RelativeHeapCorruptionForwardNonAdjacent < RelativeHeapCorruption
    def initialize
      super
      @corruption_direction = :forward
      @corruption_position = :nonadjacent
    end

    def to_sym
      :relative_heap_corruption_forward_nonadjacent
    end
  end

  class RelativeHeapCorruptionReverseAdjacent < RelativeHeapCorruption
    def initialize
      super
      @corruption_direction = :reverse
      @corruption_position = :adjacent
    end

    def to_sym
      :relative_heap_corruption_reverse_adjacent
    end
  end

  class RelativeHeapCorruptionReverseNonAdjacent < RelativeHeapCorruption
    def initialize
      super
      @corruption_direction = :reverse
      @corruption_position = :nonadjacent
    end

    def to_sym
      :relative_heap_corruption_reverse_nonadjacent
    end
  end

  class TypeConfusion < Flaw
    def initialize
      super
      @root_cause = :type_confusion
    end

    def to_sym
      :type_confusion
    end

    def can_enable
      [
        AbsoluteControlTransferViaTypeConfusion,
        AbsoluteWriteViaTypeConfusion,
        RelativeWriteViaTypeConfusion,
        AbsoluteReadViaTypeConfusion,
        RelativeReadViaTypeConfusion
      ]
    end
  end

  class UninitializedUse < Flaw
    def initialize
      super
      @root_cause = :uninitialized_use
    end

    def to_sym
      :uninitialized_use
    end

    def can_enable
      [
        AbsoluteControlTransferViaUninitializedUse,
        AbsoluteWriteViaUninitializedUse,
        RelativeWriteViaUninitializedUse,
        AbsoluteReadViaUninitializedUse
      ]
    end
  end

  class StackUninitializedUse < UninitializedUse
    def to_sym
      :stack_uninitialized_use
    end
  end

  class HeapUninitializedUse < UninitializedUse
    def to_sym
      :heap_uninitialized_use
    end
  end

  class DoubleFree < Flaw
    def initialize
      super
      @root_cause = :double_free
    end

    def to_sym
      :double_free
    end

    def can_enable
      [
        AbsoluteWriteViaDoubleFree
      ]
    end
  end

  class ArbitraryFree < Flaw
    def initialize
      super
      @root_cause = :arbitrary_free
      @can_free_arbitrary_address = true
    end

    def to_sym
      :arbitrary_free
    end

    def can_enable
      [
        AbsoluteWriteViaArbitraryFree
      ]
    end
  end

  class ArbitraryFreeViaUninitializedUse < Flaw
    def initialize
      super
      @root_cause = :arbitrary_free
      @can_free_arbitrary_address = true
    end

    def to_sym
      :arbitrary_free_via_uninitialized_use
    end

    def can_enable
      [
        AbsoluteWriteViaArbitraryFree
      ]
    end
  end

  class FormatString < Flaw
    def initialize
      super
      @root_cause = :format_string
    end

    def to_sym
      :format_string
    end

    def can_enable
      [
        AbsoluteWriteViaFormatString,
        AbsoluteReadViaFormatString,
        RelativeReadViaFormatString
      ]
    end
  end

  class NullDereference < Flaw
    def initialize
      super
      @root_cause = :null_dereference
    end

    def to_sym
      :null_dereference
    end

    def can_enable
      [
        AbsoluteControlTransferViaNullDereference,
        AbsoluteWriteViaNullDereference,
        AbsoluteReadViaNullDereference
      ]
    end
  end

end

class InvalidTargetBitValue < Exception
  def initialize(reason = nil)
    @reason = reason
  end

  attr_accessor :reason
end

#
# The target configuration in which an exploit will be simulated.  This
# is an aggregate of the hardware, operating system, application, and flaw
# configuration.
#
class Target

  def initialize(&block)
    self.class.init_profiles 

    @hw = nil
    @os = nil
    @app = nil
    @flaw = nil
    @cap = self.class.cap_profiles[:none].dup

    instance_eval(&block) if block
  end

  attr_accessor :hw
  attr_accessor :os
  attr_accessor :app
  attr_accessor :flaw
  attr_accessor :cap

  #
  # Ensures coherency of profiles once all changes have been made. 
  #
  def recalibrate
    @hw.recalibrate(self)
    @os.recalibrate(self)
    @app.recalibrate(self)
    @flaw.recalibrate(self)
    @cap.recalibrate(self)
  end

  #
  # Static initialization of built-in profiles.
  #

  class <<self
    attr_accessor :hw_profiles
    attr_accessor :os_profiles
    attr_accessor :app_profiles
    attr_accessor :flaw_profiles
    attr_accessor :flaw_core_profiles
    attr_accessor :cap_profiles
    
    attr_accessor :bit_descriptors
    attr_accessor :bit_map

    def init_profiles
      if @initialized != true
        init_hw_profiles
        init_os_profiles
        init_app_profiles
        init_flaw_profiles
        init_cap_profiles
        init_bit_map
        @initialized = true
      end
    end

    def hw_classes
      [
        Hardware,
        Hardware::X86,
        Hardware::X86_NO_PAE,
        Hardware::X86_PAE,
        Hardware::X64,
        Hardware::ARM
      ]
    end

    #
    # Initialize built-in hardware profiles.
    #
    def init_hw_profiles
      p = {}

      hw_classes.each do |hw_class|
        instance = hw_class.new
        p[instance.to_sym] = instance
      end

      @hw_profiles = p
    end

    #
    # Initializes built-in operating system profiles.
    #
    def init_os_profiles
      p = {}

      p[:windows_2000_sp4_32bit]         = OS::Windows2000.new(4)
      p[:windows_server2008r2_rtm_64bit] = OS::WindowsServer2008R2.new(0)
      p[:windows_8_server_64bit]         = OS::Windows8Server.new(0)

      address_bits = [ 32, 64 ]

      address_bits.each do |address_bit|
        ip = {}

        ip[:windows_server2003_rtm]   = OS::WindowsServer2003.new(0, address_bit)
        ip[:windows_server2003_sp1]   = OS::WindowsServer2003.new(1, address_bit)
        ip[:windows_server2003_sp2]   = OS::WindowsServer2003.new(2, address_bit)
        ip[:windows_xp_rtm]           = OS::WindowsXP.new(0, address_bit)
        ip[:windows_xp_sp2]           = OS::WindowsXP.new(2, address_bit)
        ip[:windows_xp_sp3]           = OS::WindowsXP.new(3, address_bit)
        ip[:windows_vista_rtm]        = OS::WindowsVista.new(0, address_bit)
        ip[:windows_vista_sp1]        = OS::WindowsVista.new(1, address_bit)
        ip[:windows_vista_sp2]        = OS::WindowsVista.new(2, address_bit)
        ip[:windows_server2008_rtm]   = OS::WindowsServer2008.new(0, address_bit)
        ip[:windows_server2008_sp1]   = OS::WindowsServer2008.new(1, address_bit)
        ip[:windows_7_rtm]            = OS::Windows7.new(0, address_bit)
        ip[:windows_8_client]         = OS::Windows8Client.new(0, address_bit)

        ip.each_pair do |namesym, os|
          p[(namesym.to_s + '_' + address_bit.to_s + 'bit').to_sym] = os
        end

      end

      @os_profiles = p
    end

    #
    # Initializes built-in application profiles.
    #
    def init_app_profiles
      p = {}

      p[:office11_32bit]     = Application::Office11.new
      p[:office12_rtm_32bit] = Application::Office12.new(0)
      p[:office12_sp3_32bit] = Application::Office12.new(3)

      address_bits = [ 32, 64]

      address_bits.each do |address_bit|
        ip = {}

        ip[:ie6]              = Application::IE6.new(address_bit)
        ip[:ie6_with_plugins] = Application::IE6.new(address_bit) do @plugins = all_plugins end
        ip[:ie7]              = Application::IE7.new(address_bit)
        ip[:ie7_with_plugins] = Application::IE7.new(address_bit) do @plugins = all_plugins end
        ip[:ie8]              = Application::IE8.new(address_bit)
        ip[:ie8_with_plugins] = Application::IE8.new(address_bit) do @plugins = all_plugins end
        ip[:ie9]              = Application::IE9.new(address_bit)
        ip[:ie9_with_plugins] = Application::IE9.new(address_bit) do @plugins = all_plugins end
        ip[:ie10]             = Application::IE10.new(address_bit)
        ip[:ie10_with_plugins]= Application::IE10.new(address_bit) do @plugins = all_plugins end
        ip[:office14]         = Application::Office14.new(address_bit)
        ip[:office15]         = Application::Office15.new(address_bit)
        ip[:windows_inbox]    = Application::WindowsInbox.new(address_bit)
        ip[:windows_service]  = Application::WindowsService.new(address_bit)
        ip[:windows_user_app] = Application::WindowsUserModeApplication.new("user mode app", 0, address_bit)
        ip[:windows_kernel]   = Application::WindowsKernelModeApplication.new(address_bit)

        ip.each_pair do |namesym, app|
          p[(namesym.to_s + '_' + address_bit.to_s + 'bit').to_sym] = app
        end

      end

      @app_profiles = p
    end

    #
    # Default flaw classes.
    #
    def flaw_classes 
      [
          Flaw::AbsoluteControlTransfer,
          Flaw::AbsoluteControlTransferViaTypeConfusion,
          Flaw::AbsoluteControlTransferViaUninitializedUse,
          Flaw::AbsoluteControlTransferViaHeapUseAfterFree,
          Flaw::AbsoluteControlTransferViaAbsoluteWrite,
          Flaw::AbsoluteControlTransferViaRelativeWrite,
          Flaw::AbsoluteControlTransferViaAbsoluteRead,
          Flaw::AbsoluteControlTransferViaRelativeRead,
          Flaw::AbsoluteControlTransferViaNewStackPointer,
          Flaw::AbsoluteControlTransferViaNullDereference,

          Flaw::AbsoluteRead,
          Flaw::AbsoluteReadViaTypeConfusion,
          Flaw::AbsoluteReadViaUninitializedUse,
          Flaw::AbsoluteReadViaFormatString,
          Flaw::AbsoluteReadViaAbsoluteWrite,
          Flaw::AbsoluteReadViaRelativeWrite,
          Flaw::AbsoluteReadViaNullDereference,

          Flaw::RelativeRead,
          Flaw::RelativeReadViaTypeConfusion,
          Flaw::RelativeReadViaUninitializedUse,
          Flaw::RelativeReadViaFormatString,
          Flaw::RelativeReadViaAbsoluteWrite,
          Flaw::RelativeReadViaRelativeWrite,

          Flaw::AbsoluteWrite,
          Flaw::AbsoluteWriteViaTypeConfusion,
          Flaw::AbsoluteWriteViaUninitializedUse,
          Flaw::AbsoluteWriteViaHeapUseAfterFree,
          Flaw::AbsoluteWriteViaDoubleFree,
          Flaw::AbsoluteWriteViaArbitraryFree,
          Flaw::AbsoluteWriteViaNullDereference,
          Flaw::AbsoluteWriteViaRelativeWrite,
          Flaw::AbsoluteWriteViaAbsoluteRead,
          Flaw::AbsoluteWriteViaFormatString,

          Flaw::RelativeWrite,
          Flaw::RelativeWriteViaTypeConfusion,

          Flaw::RelativeStackCorruption,
          Flaw::RelativeStackCorruptionForwardAdjacent,
          Flaw::RelativeStackCorruptionForwardNonAdjacent,
          Flaw::RelativeStackCorruptionReverseAdjacent,
          Flaw::RelativeStackCorruptionReverseNonAdjacent,
          Flaw::RelativeHeapCorruption,
          Flaw::RelativeHeapCorruptionForwardAdjacent,
          Flaw::RelativeHeapCorruptionForwardNonAdjacent,
          Flaw::RelativeHeapCorruptionReverseAdjacent,
          Flaw::RelativeHeapCorruptionReverseNonAdjacent,
          Flaw::TypeConfusion,
          Flaw::UninitializedUse,
          Flaw::StackUninitializedUse,
          Flaw::HeapUninitializedUse,
          Flaw::DoubleFree,
          Flaw::ArbitraryFree,
          Flaw::ArbitraryFreeViaUninitializedUse,
          Flaw::FormatString,
          Flaw::NullDereference
      ] 
    end

    #
    # Core flaw classes (excludes all 2nd order via classes).
    #
    def flaw_core_classes
      [
          Flaw::AbsoluteControlTransfer,
          Flaw::AbsoluteRead,
          Flaw::RelativeRead,
          Flaw::AbsoluteWrite,
          Flaw::RelativeWrite,
          Flaw::RelativeStackCorruption,
          Flaw::RelativeStackCorruptionForwardAdjacent,
          Flaw::RelativeStackCorruptionForwardNonAdjacent,
          Flaw::RelativeStackCorruptionReverseAdjacent,
          Flaw::RelativeStackCorruptionReverseNonAdjacent,
          Flaw::RelativeHeapCorruption,
          Flaw::RelativeHeapCorruptionForwardAdjacent,
          Flaw::RelativeHeapCorruptionForwardNonAdjacent,
          Flaw::RelativeHeapCorruptionReverseAdjacent,
          Flaw::RelativeHeapCorruptionReverseNonAdjacent,
          Flaw::TypeConfusion,
          Flaw::UninitializedUse,
          Flaw::StackUninitializedUse,
          Flaw::HeapUninitializedUse,
          Flaw::DoubleFree,
          Flaw::ArbitraryFree,
          Flaw::FormatString,
          Flaw::NullDereference
      ]
    end

    #
    # Initializes built-in flaw profiles.
    #
    def init_flaw_profiles
      p = {}
      cp = {}

      flaw_classes.each { |flaw_class|
        flaw = flaw_class.new
        p[flaw.to_sym] = flaw

        if flaw_core_classes.include?(flaw_class)
          cp[flaw.to_sym] = flaw_class.new
        end
      }

      @flaw_profiles = p
      @flaw_core_profiles = cp
    end

    #
    # Returns root flaw profiles.
    #
    def flaw_root_profiles
      @flaw_profiles.select do |flawsym, flaw|
        flaw.root?
      end
    end

    #
    # Builtin capability classes.
    #
    def cap_classes
      [
        Capabilities
      ]
    end

    #
    # Initializes builtin capability profiles
    #
    def init_cap_profiles 
      p = {}

      p[:default] = Capabilities.new
      
      none = Capabilities.new
      none.set_cap_defaults_false

      p[:none] = none
      
      all = Capabilities.new
      all.set_cap_defaults_true

      p[:all] = all

      @cap_profiles = p
    end

    # Initialize a context symbolically
    def init_context_symbolic(context, symbols)
      symbols.each_pair do |symbol, value| 
        @bit_descriptors.each do |bit_desc|
          next if symbol != bit_desc[:name]

          bit_desc[:set].call(context, value)
        end
      end
    end

    # Initialize a context using a bit string in bigint format
    def init_context_bitstring(context, bitstring)
      bit_map_idx = 0

      begin
        bit_desc = @bit_map[bit_map_idx]

        # Compute the value of the bits at the current index
        value = bitstring & ((1 << bit_desc[:bits]) - 1)

        # Set the value on the supplied context for this bit descriptor
        set_context_bitvalue(context, bit_desc, value)

        # Move to the next descriptor
        bit_map_idx  += bit_desc[:bits]
        bitstring   >>= bit_desc[:bits]

      end until bit_map_idx >= @bit_map.length or bitstring == 0
    end

    # Sets the field described by the supplied bit descriptor on the
    # supplied context by translating the bit value passed in value to
    # the actual value that should be set.
    def set_context_bitvalue(context, bit_desc, value)
      # Get the real value that we will set the field to
      case bit_desc[:type]
        when :enum
          if value >= bit_desc[:values].length
            raise InvalidTargetBitValue, "value exceeds enumerations [#{bit_desc[:name]}]"
          else
            value = bit_desc[:values][value]
          end
        when :bool
          if value == 0
            return nil
          elsif value == 1
            value = true
          elsif value == 2
            value = false
          else
            raise InvalidTargetBitValue, "invalid boolean for #{bit_desc[:name]}"
          end
      end

      # Set the appropriate field on the supplied context
      bit_desc[:set].call(context, value)
    end

    #
    # Verify that the context is sound with respect to any other
    # values that may have been set.  This is called after each
    # bit descriptor has had a chance to initialize the context.
    #
    def verify_context(context, bit_desc)
      bit_desc[:verify].call(context) if bit_desc[:verify]
    end

    def bit_descriptors_from_symbols(*symbols)
      symbols.map do |symbol|
        out_desc = nil

        bit_descriptors.each do |bit_desc|
          if symbol.to_sym == bit_desc[:name]
            out_desc = bit_desc 
            break
          end
        end

        out_desc
      end
    end

    #
    # Initializes the target bit map.
    #
    def init_bit_map
   
      #
      # Prepare the bit descriptors for each bit field.
      #
      @bit_descriptors =
        [
          #
          # base profile bits
          #

          {
            :name   => :hw_base_profile,
            :type   => :enum,
            :values => Target.hw_profiles.keys,
            :set    => lambda do |context, value|
              context.target.hw = Target.hw_profiles[value.to_sym].dup
              context.target.hw.sym = value
            end,
            :get    => lambda do |context|
              context.target.hw
            end
          },
          {
            :name   => :os_base_profile,
            :type   => :enum,
            :values => Target.os_profiles.keys,
            :set    => lambda do |context, value|
              context.target.os = Target.os_profiles[value.to_sym].dup
              context.target.os.sym = value
            end,
            :get    => lambda do |context|
              context.target.os
            end,
            :verify => lambda do |context|
              if context.target.os.compatible_with?(context.target.hw) == false
                raise InvalidTargetBitValue, "os #{context.target.os} not compatible with hw."
              end
            end
          },
          {
            :name   => :app_base_profile,
            :type   => :enum,
            :values => Target.app_profiles.keys,
            :set    => lambda do |context, value|
              context.target.app = Target.app_profiles[value.to_sym].dup
              context.target.app.sym = value
            end,
            :get    => lambda do |context|
              context.target.app
            end,
            :verify => lambda do |context|
              if context.target.app.compatible_with?(context.target.hw, context.target.os) == false
                raise InvalidTargetBitValue, "application #{context.target.app} not compatible with os/hw."
              end
            end

          },
          {
            :name   => :flaw_base_profile,
            :type   => :enum,
            :values => Target.flaw_profiles.keys,
            :set    => lambda do |context, value|
              context.target.flaw = Target.flaw_profiles[value.to_sym].dup
            end,
            :get    => lambda do |context|
              context.target.flaw
            end,
            :verify => lambda do |context|
              if context.target.flaw.compatible_with?(context.target.hw, context.target.os, context.target.app) == false
                raise InvalidTargetBitValue, "flaw #{context.target.flaw} not compatible with os/hw/app."
              end
            end
          },

          #
          # hw characteristic bits
          #

          {
            :name   => :hw_nx_policy,
            :type   => :enum,
            :values => [ :on, :off ],
            :set    => lambda do |context, value|
              context.target.hw.nx_policy = value
            end,
            :get    => lambda do |context|
              context.target.hw.nx_policy
            end
          },
          {
            :name   => :hw_smep_policy,
            :type   => :enum,
            :values => [ :on, :off ],
            :set    => lambda do |context, value|
              context.target.hw.smep_policy = value
            end,
            :get    => lambda do |context|
              context.target.hw.smep_policy
            end
          },

          #
          # os characteristic bits
          #

          {
            :name   => :os_address_bits,
            :type   => :enum,
            :values => [ 32, 64 ],
            :set    => lambda do |context, value|
              # A 64-bit OS cannot run on a 32-bit processor.
              if context.target.hw.address_bits == 32 and value == 64
                raise InvalidTargetBitValue, "cannot run 64-bit OS on 32-bit hardware."
              end

              context.target.os.address_bits = value
            end,
            :get    => lambda do |context|
              context.target.os.address_bits
            end
          },

          {
            :name   => :os_user_nx_policy,
            :type   => :enum,
            :values => [ :on, :off, :optin, :optout ],
            :set    => lambda do |context, value|
              context.target.os.user_nx_policy = value
            end,
            :get    => lambda do |context|
              context.target.os.user_nx_policy
            end
          },

          {
            :name   => :os_user_aslr_policy_exe_images,
            :type   => :enum,
            :values => [ :on, :off, :optin ],
            :set    => lambda do |context, value|
              if context.target.os.user_aslr_supported == false
                raise InvalidTargetBitValue, "ASLR is not supported by the target OS"
              end

              context.target.os.user_aslr_policy_exe_images = value
            end,
            :get    => lambda do |context|
              context.target.os.user_aslr_policy_exe_images
            end
          },
          {
            :name   => :os_user_aslr_policy_lib_images,
            :type   => :enum,
            :values => [ :on, :off, :optin ],
            :set    => lambda do |context, value|
              if context.target.os.user_aslr_supported == false
                raise InvalidTargetBitValue, "ASLR is not supported by the target OS"
              end

              context.target.os.user_aslr_policy_lib_images = value
            end,
            :get    => lambda do |context|
              context.target.os.user_aslr_policy_lib_images
            end
          },
          {
            :name   => :os_user_aslr_policy_force_relocation,
            :type   => :enum,
            :values => [ :on, :off, :optin ],
            :set    => lambda do |context, value|
              if context.target.os.user_aslr_force_relocation_supported == false
                raise InvalidTargetBitValue, "force relocation is not supported by the target OS"
              end

              context.target.os.user_aslr_policy_force_relocation = value
            end,
            :get    => lambda do |context|
              context.target.os.user_aslr_policy_force_relocation
            end
          },
          {
            :name   => :os_user_aslr_policy_top_down,
            :type   => :enum,
            :values => [ :on, :off, :optin ],
            :set    => lambda do |context, value|
              if context.target.os.user_aslr_top_down_supported == false
                raise InvalidTargetBitValue, "top down ASLR is not supported by the target OS"
              end

              context.target.os.user_aslr_policy_top_down = value
            end,
            :get    => lambda do |context|
              context.target.os.user_aslr_policy_top_down
            end
          },
          {
            :name   => :os_user_aslr_policy_bottom_up,
            :type   => :enum,
            :values => [ :on, :off, :optin ],
            :set    => lambda do |context, value|
              if context.target.os.user_aslr_bottom_up_supported == false
                raise InvalidTargetBitValue, "bottom up ASLR is not supported by the target OS"
              end

              context.target.os.user_aslr_policy_bottom_up = value
            end,
            :get    => lambda do |context|
              context.target.os.user_aslr_policy_bottom_up
            end
          },
          {
            :name   => :os_user_aslr_policy_bottom_up_he,
            :type   => :enum,
            :values => [ :on, :off, :optin ],
            :set    => lambda do |context, value|
              if context.target.os.user_aslr_bottom_up_he_supported == false
                raise InvalidTargetBitValue, "bottom up HEASLR is not supported by the target OS"
              end

              context.target.os.user_aslr_policy_bottom_up_he = value
            end,
            :get    => lambda do |context|
              context.target.os.user_aslr_policy_bottom_up_he
            end
          },
          {
            :name   => :os_user_aslr_policy_peb,
            :type   => :enum,
            :values => [ :on, :off ],
            :set    => lambda do |context, value|
              context.target.os.user_aslr_policy_peb = value
            end,
            :get    => lambda do |context|
              context.target.os.user_aslr_policy_peb
            end
          },
          {
            :name   => :os_user_aslr_policy_stacks,
            :type   => :enum,
            :values => [ :on, :off ],
            :set    => lambda do |context, value|
              context.target.os.user_aslr_policy_stacks = value
            end,
            :get    => lambda do |context|
              context.target.os.user_aslr_policy_stacks
            end
          },
          {
            :name   => :os_user_aslr_policy_heaps,
            :type   => :enum,
            :values => [ :on, :off ],
            :set    => lambda do |context, value|
              context.target.os.user_aslr_policy_heaps = value
            end,
            :get    => lambda do |context|
              context.target.os.user_aslr_policy_heaps
            end
          },

          {
            :name   => :os_user_sehop_policy,
            :type   => :enum,
            :values => [ :on, :off ],
            :set    => lambda do |context, value|
              if context.target.os.respond_to?(:user_sehop_supported) == false or context.target.os.user_sehop_supported == false
                raise InvalidTargetBitValue, "SEHOP is not supported by the target OS"
              end

              context.target.os.user_sehop_policy = value
            end,
            :get    => lambda do |context|
              context.target.os.user_sehop_policy
            end
          },

          {
            :name   => :os_kernel_smep_policy,
            :type   => :enum,
            :values => [ :on, :off ],
            :set    => lambda do |context, value|
              if context.target.os.kernel_smep_supported == false
                raise InvalidTargetBitValue, "SMEP is not supported by the target OS"
              end

              context.target.os.kernel_smep_policy = value
            end,
            :get    => lambda do |context|
              context.target.os.kernel_smep_policy
            end
          },
          {
            :name   => :os_kernel_null_deref_prevention_enabled,
            :type   => :bool,
            :set    => lambda do |context, value|
              if context.target.os.kernel_null_deref_prevention_supported == false
                raise InvalidTargetBitValue, "NULL dereference prevention is not supported by the target OS"
              end

              context.target.os.kernel_null_deref_prevention_enabled = value
            end,
            :get    => lambda do |context|
              context.target.os.kernel_null_deref_prevention_enabled
            end
          },

          #
          # app characteristic bits
          #
            
          {
            :name   => :app_is_kernel,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.app.kernel = value
            end,
            :get    => lambda do |context|
              context.target.app.kernel
            end
          },
          {
            :name   => :app_address_bits,
            :type   => :enum,
            :values => [ 32, 64 ],
            :set    => lambda do |context, value|
              if context.target.os.address_bits == 32 and value == 64
                raise InvalidTargetBitValue, "cannot run 64-bit application on a 32-bit OS"
              end

              context.target.app.address_bits = value
            end,
            :get    => lambda do |context|
              context.target.app.address_bits
            end,
            :verify => lambda do |context|
              if context.target.os.family == :windows and context.target.app.address_bits == 64
                context.target.app.user_nx_policy = :on
                context.target.app.user_nx_permanent = true
              end
            end
          },
          {
            :name   => :app_user_nx_policy,
            :type   => :enum,
            :values => [ :on, :off ],
            :set    => lambda do |context, value|
              if context.target.app.is_user_app
                raise InvalidTargetBitValue, "User settings do not matter for kernel applications."
              end

              context.target.app.user_nx_policy = value
            end,
            :get    => lambda do |context|
              context.target.app.user_nx_policy
            end
          },
          {
            :name   => :app_user_nx_permanent,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.app.user_nx_permanent = value
            end,
            :get    => lambda do |context|
              context.target.app.user_nx_permanent
            end
          },
          {
            :name   => :app_user_sehop_policy,
            :type   => :enum,
            :values => [ :on, :off ],
            :set    => lambda do |context, value|
              context.target.app.user_sehop_policy = value
            end,
            :get    => lambda do |context|
              context.target.app.user_sehop_policy
            end
          },

          #
          # flaw characteristic bits
          #
          {
            :name   => :flaw_local,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.local = value
            end,
            :get    => lambda do |context|
              context.target.flaw.local
            end,
            :verify => lambda do |context|
              if context.target.flaw.local and context.target.app.can_have_local_flaw? == false
                raise InvalidTargetBitValue, "application cannot have local flaws."
              end
            end
          },
          {
            :name   => :flaw_corruption_region,
            :type   => :enum,
            :values => Flaw.corruption_regions,
            :set    => lambda do |context, value|
              context.target.flaw.corruption_region = value
            end,
            :get    => lambda do |context|
              context.target.flaw.corruption_region
            end
          },
          {
            :name   => :flaw_corruption_direction,
            :type   => :enum,
            :values => Flaw.corruption_directions,
            :set    => lambda do |context, value|
              context.target.flaw.corruption_direction = value
            end,
            :get    => lambda do |context|
              context.target.flaw.corruption_direction
            end
          },
          {
            :name   => :flaw_corruption_displacement,
            :type   => :enum,
            :values => Flaw.corruption_displacements,
            :set    => lambda do |context, value|
              context.target.flaw.corruption_displacement = value
            end,
            :get    => lambda do |context|
              context.target.flaw.corruption_displacement
            end
          },
          {
            :name   => :flaw_corruption_position,
            :type   => :enum,
            :values => Flaw.corruption_positions,
            :set    => lambda do |context, value|
              context.target.flaw.corruption_position = value
            end,
            :get    => lambda do |context|
              context.target.flaw.corruption_position
            end
          },

          {
            :name   => :flaw_function_stack_protection_enabled,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.function_stack_protection_enabled = value
            end,
            :get    => lambda do |context|
              context.target.flaw.function_stack_protection_enabled
            end,
            :verify => lambda do |context|
              #
              # It does not matter if stack protection is enabled if this is not
              # a stack memory corruption vulnerability.
              #
              if context.target.flaw.function_stack_protection_enabled and context.target.flaw.corruption_region != :stack
                raise InvalidTargetBitValue, "stack protection does not matter for non-stack corruption."
              end
            end
          },
          {
            :name   => :flaw_function_stack_protection_version,
            :type   => :enum,
            :values => Flaw::StackProtectionVersions,
            :set    => lambda do |context, value|
              context.target.flaw.function_stack_protection_version = value
            end,
            :get    => lambda do |context|
              context.target.flaw.function_stack_protection_version
            end,
            :verify => lambda do |context|
              #
              # It does not matter if stack protection is enabled if this is not
              # a stack memory corruption vulnerability.
              #
              if context.target.flaw.function_stack_protection_enabled and context.target.flaw.corruption_region != :stack
                raise InvalidTargetBitValue, "stack protection does not matter for non-stack corruption."
              end
            end
          },

          {
            :name   => :flaw_triggered_in_catch_all_seh_scope,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.triggered_in_catch_all_seh_scope = value
            end,
            :get    => lambda do |context|
              context.target.flaw.triggered_in_catch_all_seh_scope
            end
          },

          {
            :name   => :flaw_vtguard_enabled,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.vtguard_enabled = value
            end,
            :get    => lambda do |context|
              context.target.flaw.vtguard_enabled
            end
          },

          {
            :name   => :flaw_vtguard_level,
            :type   => :enum,
            :values => [ 1, 2, 3 ],
            :set    => lambda do |context, value|
              context.target.flaw.vtguard_level = value
            end,
            :get    => lambda do |context|
              context.target.flaw.vtguard_level
            end
          },

          {
            :name   => :flaw_can_transfer_control_anywhere,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.can_transfer_control_anywhere = value
            end,
            :get    => lambda do |context|
              context.target.flaw.can_transfer_control_anywhere
            end
          },
          {
            :name   => :flaw_can_write_anywhere,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.can_write_anywhere = value
            end,
            :get    => lambda do |context|
              context.target.flaw.can_write_anywhere
            end
          },
          {
            :name   => :flaw_can_read_anywhere,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.can_read_anywhere = value
            end,
            :get    => lambda do |context|
              context.target.flaw.can_read_anywhere
            end
          },
          {
            :name   => :flaw_can_corrupt_return_address,
            :type   => :bool,
            :set    => lambda do |context, value|
              if context.is_flaw_stack_memory_corruption == false
                raise InvalidTargetBitValue, "must be able to corrupt stack memory"
              end

              context.target.flaw.can_corrupt_return_address = value
            end,
            :get    => lambda do |context|
              if context.target
                context.can_corrupt_return_address
              else
                context.target.flaw.can_corrupt_return_address
              end
            end
          },
          {
            :name   => :flaw_can_corrupt_frame_pointer,
            :type   => :bool,
            :set    => lambda do |context, value|
              if context.is_flaw_stack_memory_corruption == false
                raise InvalidTargetBitValue, "must be able to corrupt stack memory"
              end

              context.target.flaw.can_corrupt_frame_pointer = value
            end,
            :get    => lambda do |context|
              if context.target
                context.can_corrupt_frame_pointer
              else
                context.target.flaw.can_corrupt_frame_pointer
              end
            end
          },
          {
            :name   => :flaw_can_corrupt_seh_frame,
            :type   => :bool,
            :set    => lambda do |context, value|
              if context.is_flaw_stack_memory_corruption == false
                raise InvalidTargetBitValue, "must be able to corrupt stack memory"
              end

              context.target.flaw.can_corrupt_seh_frame = value
            end,
            :get    => lambda do |context|
              if context.target
                context.can_corrupt_seh_frame
              else
                context.target.flaw.can_corrupt_seh_frame
              end
            end
          },
          {
            :name   => :flaw_can_corrupt_object_vtable_pointer,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.can_corrupt_object_vtable_pointer = value
            end,
            :get    => lambda do |context|
              if context.target
                context.can_corrupt_object_vtable_pointer
              else
                context.target.flaw.can_corrupt_object_vtable_pointer
              end
            end
          },
          {
            :name   => :flaw_can_corrupt_function_pointer,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.can_corrupt_function_pointer = value
            end,
            :get    => lambda do |context|
              if context.target
                context.can_corrupt_function_pointer
              else
                context.target.flaw.can_corrupt_function_pointer
              end
            end
          },
          {
            :name   => :flaw_can_corrupt_write_target_pointer,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.can_corrupt_write_target_pointer = value
            end,
            :get    => lambda do |context|
              if context.target
                context.can_corrupt_write_target_pointer
              else
                context.target.flaw.can_corrupt_write_target_pointer
              end
            end
          },
           
          {
            :name   => :flaw_can_corrupt_parent_frame_locals,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.can_corrupt_parent_frame_locals = value
            end,
            :get    => lambda do |context|
              if context.target
                context.can_overwrite_parent_frame_local_variables
              else
                context.target.flaw.can_corrupt_parent_frame_locals
              end
            end
          }, 
          {
            :name   => :flaw_can_corrupt_child_or_current_frame_locals,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.can_corrupt_child_or_current_frame_locals = value
            end,
            :get    => lambda do |context|
              if context.target
                context.can_overwrite_child_or_current_frame_local_variables
              else
                context.target.flaw.can_corrupt_child_or_current_frame_locals
              end
            end
          }, 

          {
            :name   => :flaw_can_corrupt_heap_block_header,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.can_corrupt_heap_block_header = value
            end,
            :get    => lambda do |context|
              if context.target
                context.can_corrupt_heap_block_header
              else
                context.target.flaw.can_corrupt_heap_block_header
              end
            end
          },
          {
            :name   => :flaw_can_corrupt_heap_free_links,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.can_corrupt_heap_free_links = value
            end,
            :get    => lambda do |context|
              if context.target
                context.can_corrupt_heap_free_links
              else
                context.target.flaw.can_corrupt_heap_free_links
              end
            end
          },
          {
            :name   => :flaw_can_trigger_seh_exception,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.can_trigger_seh_exception = value
            end,
            :get    => lambda do |context|
              if context.target
                context.can_trigger_seh_exception
              else
                context.target.flaw.can_trigger_seh_exception
              end
            end
          },  
          {
            :name   => :flaw_can_discover_stack_cookie,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.can_discover_stack_cookie = value
            end,
            :get    => lambda do |context|
              if context.target
                context.can_discover_stack_cookie
              else
                context.target.flaw.can_discover_stack_cookie
              end
            end
          },
          {
            :name   => :flaw_can_discover_vtguard_cookie,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.flaw.can_discover_vtguard_cookie = value
            end,
            :get    => lambda do |context|
              if context.target
                context.can_discover_vtguard_cookie
              else
                context.target.flaw.can_discover_vtguard_cookie
              end
            end
          },

          #
          # Capability bits
          #
            
          {
            :name   => :attacker_can_load_non_aslr_image,
            :type   => :bool,
            :set    => lambda do |context, value|
              if value == true
                context.target.cap.can_discover_image_address = true
              end

              context.target.cap.can_load_non_aslr_image = value
            end,
            :get    => lambda do |context|
              context.target.cap.can_load_non_aslr_image
            end
          },
          {
            :name   => :attacker_can_load_non_aslr_non_safeseh_image,
            :type   => :bool,
            :set    => lambda do |context, value|
              if value == true
                context.target.cap.can_discover_image_address = true
                context.target.cap.can_discover_non_safeseh_image_address = true
              end

              context.target.cap.can_load_non_aslr_non_safeseh_image = value
            end,
            :get    => lambda do |context|
              context.target.cap.can_load_non_aslr_non_safeseh_image  
            end
          },
          {
            :name   => :attacker_can_spray_data_bottom_up,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.cap.can_spray_data_bottom_up = value
            end,
            :get    => lambda do |context|
              context.target.cap.can_spray_data_bottom_up
            end
          },
          {
            :name   => :attacker_can_spray_code_bottom_up,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.cap.can_spray_code_bottom_up = value
            end,
            :get    => lambda do |context|
              context.target.cap.can_spray_code_bottom_up
            end
          },
          {
            :name   => :attacker_can_massage_heap,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.cap.can_massage_heap = value
            end,
            :get    => lambda do |context|
              context.target.cap.can_massage_heap
            end
          },
          {
            :name   => :attacker_can_discover_desired_code_address,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.cap.can_discover_desired_code_address = value
            end,
            :get    => lambda do |context|
              context.target.cap.can_discover_desired_code_address
            end
          },
          {
            :name   => :attacker_can_discover_desired_data_address,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.cap.can_discover_desired_data_address = value
            end,
            :get    => lambda do |context|
              context.target.cap.can_discover_desired_data_address
            end
          },
          {
            :name   => :attacker_can_discover_stack_address,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.cap.can_discover_stack_address = value
            end,
            :get    => lambda do |context|
              context.target.cap.can_discover_stack_address
            end
          },
          {
            :name   => :attacker_can_discover_heap_address,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.cap.can_discover_heap_address = value
            end,
            :get    => lambda do |context|
              context.target.cap.can_discover_heap_address 
            end
          },
          {
            :name   => :attacker_can_discover_heap_base_address,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.cap.can_discover_heap_base_address = value
            end,
            :get    => lambda do |context|
              context.target.cap.can_discover_heap_base_address 
            end
          },
          {
            :name   => :attacker_can_discover_image_address,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.cap.can_discover_image_address = value
            end,
            :get    => lambda do |context|
              context.target.cap.can_discover_image_address 
            end
          },
          {
            :name   => :attacker_can_discover_ntdll_image_address,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.cap.can_discover_ntdll_image_address = value
            end,
            :get    => lambda do |context|
              context.target.cap.can_discover_ntdll_image_address 
            end
          },
          {
            :name   => :attacker_can_discover_non_safeseh_image_address,
            :type   => :bool,
            :set    => lambda do |context, value|
              context.target.cap.can_discover_non_safeseh_image_address = value
            end,
            :get    => lambda do |context|
              context.target.cap.can_discover_non_safeseh_image_address 
            end
          },

          # 
          # Aggregate bits that are non-settable
          #
            
          {
            :name   => :aggregate_user_nx_enabled,
            :type   => :bool,
            :get    => lambda do |context|
              context.user_nx_enabled
            end
          },
          {
            :name   => :aggregate_user_sehop_enabled,
            :type   => :bool,
            :get    => lambda do |context|
              context.user_sehop_enabled
            end
          },
          {
            :name   => :aggregate_user_aslr_images_enabled,
            :type   => :bool,
            :get    => lambda do |context|
              context.is_aslr_images_enabled
            end
          },
          {
            :name   => :aggregate_user_aslr_force_relocation_enabled,
            :type   => :bool,
            :get    => lambda do |context|
              context.is_aslr_force_relocation_enabled
            end
          },
          {
            :name   => :aggregate_user_aslr_bottom_up_enabled,
            :type   => :bool,
            :get    => lambda do |context|
              context.is_aslr_bottom_up_enabled
            end
          },
          {
            :name   => :aggregate_user_aslr_top_down_enabled,
            :type   => :bool,
            :get    => lambda do |context|
              context.is_aslr_top_down_enabled
            end
          },
          {
            :name   => :aggregate_user_aslr_stack_enabled,
            :type   => :bool,
            :get    => lambda do |context|
              context.is_aslr_stack_enabled
            end
          },
          {
            :name   => :aggregate_user_aslr_heap_enabled,
            :type   => :bool,
            :get    => lambda do |context|
              context.is_aslr_heap_enabled
            end
          },
          {
            :name   => :aggregate_user_aslr_peb_enabled,
            :type   => :bool,
            :get    => lambda do |context|
              context.is_aslr_peb_enabled
            end
          },
        ]


      # Prepare the bit map that we will use when certain bits 
      # are set.
      @bit_map = []

      @bit_descriptors.each do |desc| 
        bits_needed = 0

        if desc[:type] == :enum
          bits_needed =(Math.log(desc[:values].length) / Math.log(2)).ceil
        elsif desc[:type] == :bool
          bits_needed = 2
        end

        desc[:bits] = bits_needed

        bits_needed.times do |x|
          @bit_map << desc 
        end
      end

      # Return the constructed bit map to the caller
      @bit_map
    end

    def bit_map_bits
      @bit_map.length
    end

    def each_bit_desc(&block)
      bit = 0

      begin
        bit_desc = @bit_map[bit]

        block.call(bit, bit_desc)

        bit += bit_desc[:bits]
      end until bit >= @bit_map.length
    end

    def bit_map_to_s
      str = ''
      each_bit_desc do |bit, desc|
        str << "bit #{bit}: #{desc[:name]} [#{desc[:bits]} bits]\n"
      end
      str
    end

    def bit_map_to_csv
      str = ''
      each_bit_desc do |bit, desc|
        str << "," if str.length > 0
        str << "#{desc[:name]}"
      end
      str
    end

  end

end


end
