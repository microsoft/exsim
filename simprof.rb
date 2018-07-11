require 'sim'

module ExSim

profile_log_path = ARGV.shift

if profile_log_path.nil?
  puts "usage: simprof.rb <profile log>"
  exit 0
end

profile_scenarios = []

File.open(profile_log_path, "r") do |fd|

  scenario_id = 0
  option_ary = []
  value_ary = []

  while line = fd.gets

    line.chomp!
    
    case line

    when /^PROFILE: START/
      puts "start"
      option_ary = []
      value_ary = []

    when /^PROFILE: END/
      puts "end"
      profile_scenarios << [
        "scenario #{scenario_id += 1}",
        option_ary,
        value_ary
      ]

    when /^PROFILE: .+?=.+?$/
      pair = line.delete("PROFILE: ").split('=')

      if pair.length != 2
        puts "invalid profile option: #{line}"
        exit
      end

      option = pair[0].to_sym

      option_ary << option

      value = pair[1]

      if value == "true"
        value = true
      elsif value == "false"
        value = false
      else
        value = value.to_sym
      end

      value_ary << [ value ]

      puts "option #{option} value #{value}"
    end
  end

end

puts "#{profile_scenarios.length} total scenarios from profile."

p = TargetPermutator.new
p.debug = true
p.modes = [ :attack_favor, :normal ]
p.track_equivalent_only = true
p.track_impossible = true
p.output_directory = "scenarios"
p.permutate_scenarios(*profile_scenarios)
p.save_csv

end
