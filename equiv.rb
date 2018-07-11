require 'set'

info = {}

File.open(ARGV.shift, "r") do |fd|
	while line = fd.gets
		ary = line.split(/,/)

		os = ary[8]

		# Get the information we're tracking for this OS	
		os_info = info[os]

		if os_info.nil?
			os_info = info[os] = {}

			os_info[:metrics] = {}
		end

		{
			2 => :effectiveness,
			3 => :exploitability
		}.each_pair do |idx, metric|
			metric_info = os_info[:metrics][metric]

			if metric_info.nil?
				metric_info = os_info[:metrics][metric] = {}

				metric_info[:classes] = {}
				metric_info[:groups]  = 
					{
						1.0 => 0,
						0.5 => 0,
						0.1 => 0,
						0.01 => 0,
						0.001 => 0,
						0.0001 => 0,
						0.0 => 0
					}
			end

			value = ary[idx].to_f

			metric_info[:classes][value] = 
				(metric_info[:classes][value] || 0) + 1

			metric_info[:groups].keys.sort { |x,y|
				y <=> x
			}.each do |key|
				if value >= key
					metric_info[:groups][key] += 1
					break
				end
			end
		end
	end
end

$stdout.puts "os,metric,value,count,norm,percent"
$stderr.puts "os,metric,value,count,norm,percent"
info.each_pair do |os, os_info|
	os_info[:metrics].each_pair do |metric, metric_info|
		min = nil
		max = nil
		tot = 0

		metric_info[:classes].each_pair do |klass, count|
			if min.nil?
				min = count
			elsif count < min
				min = count
			end

			if max.nil?
				max = count
			elsif count > max
				max = count
			end

			tot += count
		end

		metric_info[:classes].each_pair do |klass, count|
			if max == min
				norm = 0
			else
				norm = (count.to_f - min)/(max - min)
			end

			$stdout.puts "#{os},#{metric},#{klass},#{count},#{norm},#{count.to_f / tot}"
		end
		
		min = nil
		max = nil
		tot = 0

		metric_info[:groups].each_pair do |klass, count|
			if min.nil?
				min = count
			elsif count < min
				min = count
			end

			if max.nil?
				max = count
			elsif count > max
				max = count
			end

			tot += count
		end

		metric_info[:groups].each_pair do |klass, count|
			if max == min
				norm = 0
			else
				norm = (count.to_f - min)/(max - min)
			end

			$stderr.puts "#{os},#{metric},#{klass},#{count},#{norm},#{count.to_f / tot}"
		end

	end
end


