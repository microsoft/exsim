require 'sim'

module ExSim

# Initialize and permutate the different target configurations
p = TargetPermutator.new

p.output_directory = ARGV.shift || "results"

if ARGV.empty?
  p.permutate_fields(
    :hw_base_profile, 
    :os_base_profile,
    :app_base_profile,
    :flaw_base_profile,
    :flaw_local,
    :flaw_kernel)
else
  fields = []
  values = []

  ARGV.each do |desc| 
    field, value = desc.split(/=/)
    fields << field
    values << (value || '').split(/,/)
  end

  p.permutate_fields_custom_values(fields, values)
end

# Save CSV files for analysis
p.save_csv

end
