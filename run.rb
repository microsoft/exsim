require 'sim'

module ExSim

Target.init_profiles

case ARGV.join

when /-los/
  Target.os_profiles.keys.sort.each do |k|
     puts k
  end

when /-lapp/
  Target.app_profiles.keys.sort.each do |k|
     puts k
  end

when /-lflawcore/
  Target.flaw_core_profiles.keys.sort.each do |k|
     puts k
  end

when /-lflaw/
  Target.flaw_profiles.keys.sort.each do |k|
     puts k
  end


end

base_hw_32 = 
  [ 
    :x86_no_pae, 
    :x86_pae,
    :arm
  ]
base_os_32 = 
  [ 
    :windows_xp_rtm_32bit,
    :windows_xp_sp3_32bit,
    :windows_vista_rtm_32bit,
    :windows_vista_sp2_32bit,
    :windows_7_rtm_32bit,
    :windows_8_client_32bit,
  ]
base_app_32 = 
  [ 
    :ie6_32bit, 
    :ie8_32bit,
    :ie9_32bit, 
    :ie9_with_plugins_32bit,
    :ie10_32bit,
    :ie10_with_plugins_32bit,
    :office11_32bit, 
    :office12_sp3_32bit, 
    :office14_32bit, 
    :office15_32bit, 
    :windows_service_32bit,
    :windows_kernel_32bit
  ]

base_hw_64 = [ :x64 ]
base_os_64 =
  [
    :windows_server2003_rtm_64bit,
    :windows_server2003_sp1_64bit,
    :windows_server2003_sp2_64bit,
    :windows_xp_sp3_64bit,
    :windows_server2008_rtm_64bit,
    :windows_7_rtm_64bit,
    :windows_server2008r2_rtm_64bit,
    :windows_8_client_64bit,
    :windows_8_server_64bit
  ]
base_app_64 =
  [
    :ie6_64bit,
    :ie8_64bit,
    :ie9_64bit,
    :ie9_with_plugins_64bit,
    :ie10_64bit,
    :ie10_with_plugins_64bit,
    :office14_64bit,
    :office15_64bit,
    :windows_service_64bit,
    :windows_kernel_64bit
  ]

#base_flaw = [ Flaw::AbsoluteControlTransferViaNullDereference.new ]
#base_flaw = Target.flaw_core_profiles.keys
base_flaw = 
  [
    Flaw::AbsoluteControlTransfer.new,
    Flaw::AbsoluteWrite.new,
    Flaw::RelativeStackCorruptionForwardAdjacent.new,
    Flaw::RelativeStackCorruptionForwardNonAdjacent.new,
    Flaw::RelativeHeapCorruptionForwardAdjacent.new,
    Flaw::RelativeHeapCorruptionForwardNonAdjacent.new
  ]

scenarios = 
  [
    # One scenario covering all flaws on each hw/os/app configuration.
    [ 

      "all_32bit", 
      [ 
        :hw_base_profile, 
        :os_base_profile, 
        :app_base_profile,
        :flaw_base_profile,
        :flaw_local
      ],
      [
        base_hw_32,
        base_os_32,
        base_app_32,
        base_flaw,
        [ false, true ]
      ]

    ],

    [ 

      "all_64bit", 
      [ 
        :hw_base_profile, 
        :os_base_profile, 
        :app_base_profile,
        :flaw_base_profile,
        :flaw_local
      ],
      [
        base_hw_64,
        base_os_64,
        base_app_64,
        base_flaw,
        [ false, true ]
      ]

    ],

  ]

# Flush the scenarios output
`mkdir -p scenarios`
`rm -rf scenarios/*`

p = TargetPermutator.new
p.debug = true
p.modes = [ :attack_favor, :normal ]
p.track_equivalent_only = true
#p.track_impossible = false #true
p.track_impossible = true
p.output_directory = "scenarios"
p.permutate_scenarios(*scenarios)
p.save_csv

end
