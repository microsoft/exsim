require 'sim'

module ExSim

base_hw = [ :p3, :p4]
base_os = 
  [ 
    :win_2000_sp4,
    :win_xp_rtm,
    :win_xp_sp2,
    :win_srv03_rtm,
    :win_srv03_sp1,
    :win_vista_rtm,
    :win_srv08_rtm,
    :win_7_rtm 
  ]
base_user_app = [ :ie7, :ie8, :svchost  ]
base_kernel_app = [ :kernel ]


base_scenarios = 
  [
    # Base simulations
    [ 
      "base",
      [ 
        :hw_base_profile, 
        :os_base_profile, 
        :app_base_profile,
        :flaw_base_profile,
        :flaw_local,
        :flaw_function_gs_enabled,
        :flaw_can_partial_overwrite_return_address,
        :app_can_leak_address_info,
        :app_can_control_heap_layout,
      ],
      [
        [ :p3, :p4 ],
        [ :win_xp_rtm, :win_xp_sp2, :win_vista_rtm, :win_vista_sp1, :win_7_rtm, :win_srv03_sp1, :win_srv08_rtm ],
        [ :ie7, :ie8, :svchost, :kernel, :thirdparty ],
        [ :any, :stack_memory_corruption, :non_traditional_stack_memory_corruption, :heap_memory_corruption, :non_traditional_heap_memory_corruption, :null_deref ],
        [ true, false ],
        [ true, false ],
        [ true, false ],
        [ true, false ],
        [ true, false ],
      ]
    ],
  ]

theoretical_scenarios =
  [

    # Theoretical simulations
    [ 
      "theoretical",
      [
        :hw_nx_supported, 
        :hw_nx_enabled,
        :os_nx_supported, 
        :os_nx_enabled,
        :os_aslr_supported, 
        :os_aslr_enabled,
        :os_sehop_supported, 
        :os_sehop_enabled,
        :app_nx_enabled,
        :app_nx_permanent,
        :app_can_spray_data,
        :app_can_spray_code,
        :app_can_host_dotnet_controls,
        :app_can_leak_address_info,
        :app_can_control_heap_layout,
        :flaw_base_profile,
        :flaw_local,
        :flaw_function_gs_enabled,
      ],
      [
        # hw
        [ true ],
        [ true, false ],
        # os
        [ true ],
        [ true, false ],
        [ true ],
        [ true, false ],
        [ true ],
        [ true, false ],
        # app
        [ true, false ],
        [ true, false ],
        [ true, false ],
        [ true, false ],
        [ true, false ],
        [ true, false ],
        [ true, false ],
        # flaw
        [ :any, :stack_memory_corruption, :non_traditional_stack_memory_corruption, :heap_memory_corruption, :non_traditional_heap_memory_corruption ],
        [ true, false ],
        [ true, false ],
      ]
    ],


  ]

# Flush the scenarios output
`rm -rf scenarios/base/*`
`rm -rf scenarios/theoretical/*`

p = TargetPermutator.new
p.output_directory = "scenarios/base"
#p.track_impossible = true
p.permutate_scenarios(*base_scenarios)
p.save_csv

#p = TargetPermutator.new
#p.output_directory = "scenarios/theoretical"
#p.permutate_scenarios(*theoretical_scenarios)
#p.save_csv

end
