
# Introduction

exsim is a research project developed in 2008 that simulates the exploitation and mitigation of memory safety vulnerabilities using an abstract state-based model. As this code is now many years old, the exploitation techniques, mitigations, and target environments do not include many contemporary developments. Nevertheless, the concepts explored are still relevant to modern exploitation.

## Installation

```
git clone https://github.com/Microsoft/exsim
sudo apt-get install ruby
sudo gem install rgl
sudo gem install statemachine
```

## Usage

This project is primarily intended to be a library for building tools that can be used to evaluate exploitability, but a standalone tool is included that uses a predefined combination of flaw profiles, hardware profiles, operating system profiles, application profiles, and local vs. remote vectors. This is implemented by `run.rb` and can be executed as follows:

```
$ ruby -I. run.rb
8192 simulations...

2018-07-12 18:28:36 -0700: saving all_32bit-1 [detail and csv]
2018-07-12 18:28:36 -0700: saving all_32bit-2 [detail and csv]
2018-07-12 18:28:36 -0700: saving all_32bit-5 [detail and csv]
...
2018-07-12 18:29:53 -0700: saving all_64bit-11671 [detail and csv]
2018-07-12 18:29:53 -0700: saving all_64bit-11672 [detail and csv]
2018-07-12 18:29:53 -0700: saving all_64bit-11673 [detail and csv]
```

This produces a collection of simulation summaries in the `scenarios` directory of the current working directory. For example, `all_64bit-8276.txt` contains a simulation involving a relative stack corruption in code running in the 64-bit version of Internet Explorer 8, on Windows 7 RTM 64-bit, on a 64-bit CPU.

```
hw_base_profile                                   : x64
os_base_profile                                   : windows_7_rtm_64bit
app_base_profile                                  : ie8_64bit
flaw_base_profile                                 : relative_stack_corruption_forward_adjacent
```

This file summarizes the exploitability of different simulations for this scenario. To help illustrate this, the canonical example of hijacking control flow by corrupting a return address and executing a ROP payload is represented as the following simulation:

```
============== simulation 8   [1 equivalent simulations]

Fitness       : 3.552713678800501e-15
Exploitability: 3.552713678800501e-15
Desirability  : 1.0
Likelihood    : 1.0
Homogeneity   : 3.552713678800501e-18
Population    : 0.001
Transitions   :
        target_defined                                     -> prepare_environment                 -> preparing_environment
        preparing_environment                              -> finish_preparing_environment        -> environment_prepared
        environment_prepared                               -> trigger_flaw                        -> flaw_triggered
        flaw_triggered                                     -> corrupt_return_address              -> control_of_return_address
        control_of_return_address                          -> return_from_function                -> control_of_instruction_pointer
        control_of_instruction_pointer                     -> pivot_stack_pointer                 -> control_of_stack_pointer
        control_of_stack_pointer                           -> execute_self_contained_rop_payload  -> control_of_code_execution
Assumptions   :
        can_corrupt_stack_memory()                         -> 1.0                                 [corrupt_return_address]
        can_find_stack_frame_address()                     -> 1.0                                 [corrupt_return_address]
        can_corrupt_return_address()                       -> 1.0                                 [corrupt_return_address]
        can_bypass_stack_protection()                      -> 3.552713678800501e-15               [return_from_function]
        can_control_stack_pointer()                        -> 1.0                                 [return_from_function] USED
        can_find_stack_pivot_gadget()                      -> 1.0                                 [pivot_stack_pointer]
        can_find_address(code)                             -> 1.0                                 [execute_self_contained_rop_payload]
        can_return_to_libc(image)                          -> 1.0                                 [execute_self_contained_rop_payload]
        can_find_all_necessary_gadgets()                   -> 1.0                                 [execute_self_contained_rop_payload]
Techniques    :
        ExSim::ReturnAddressOverwrite
        ExSim::PivotStackPointer
        ExSim::CodeExecutionViaSelfContainedRopPayload
```

This simulation states that the probability of this exploitation sequence succeeding is `3.552713678800501e-15` (very low). The reason for this is that the attacker would need to guess the value of the /GS security cookie which, in this case, has 48 bits of entropy. In this particular simulation, ASLR did not play a factor because an invariant of the simulation specified that it was assumed that an attacker could find the address of an image:

```
attacker_can_discover_image_address               : true
```

As it is difficult to analyze the individual simulations through free form text, the simulator also provides summary CSVs in the `scenarios` directory. The `simulations.csv` file provides a granular summary of all of the scenarios that were simulated. This file can be used to compare and contrast the exploitability of different scenarios including flaw type, hardware, operating system, application configuration.

# Project Overview

## Background

This project was developed in 2008 in conjunction with research on the theory and practice of exploiting memory safety vulnerabilities. The ideas explored by this project helped shape and inform a 2012 presentation on [Modeling the exploitation and mitigation of memory safety vulnerabilities](https://github.com/Microsoft/MSRC-Security-Research/blob/master/presentations/2012_10_Breakpoint/BreakPoint2012_Miller_Modeling_the_exploitation_and_mitigation_of_memory_safety_vulnerabilities.pdf).

## Objectives

The original objectives of this project were to:

1. Objectively evalute the relative difficulty of exploiting various classes and instances of memory safety vulnerabilities.

2. Define objective measures for the impact that mitigations have on exploiting memory safety vulnerabilities.

3. Assess the feasibility of automating and normalizing the exploitability assessment for a given vulnerability, e.g. to enable automated vulnerability triage.

## Design

A non-deterministic finite state machine is used to provide an abstract model for the sequence of events involved in exploiting a memory safety vulnerability. In this model, each state is a logical step of an exploit and each transition is an exploitation technique that can be used to reach the next stage. Each transition has zero or more predicates that place constraints on the probability of transitioning from one state to another. 

To evaluate the exploitability of a vulnerability, the simulator for this state machine takes as input the following:

* An invariant definition of the vulnerability class and properties (flaw profile).
* An invariant definition of the target environment (hardware profile, operating system profile, application profile).
* An invariant definition of the assumed initial capabilities of the attacker.

The simulator then executes the state machine until a desired end state is reached (such as control of code execution) or a fixed point is reached (e.g. no further states to explore). The simulator explores all possible paths from the initial state to the terminal state. Each path represents a possible strategy for exploiting the provided initial state.

A graph representation of the state machine as implemented can be seen [here](https://github.com/Microsoft/exsim/blob/master/doc/sm.png).

## Implementation

The following table summarizes the key files and their purpose:

|File|Purpose|
|----------|-----------------|
|env.rb|Classes that describe the invariants of memory safety flaws, hardware, operating systems, and applications. Used as input to the simulator.|
|sim.rb|Classes that implement the simulator including the state machine, simulation context, and so on|
|run.rb|A tool that provides an example of running the simulator with different types of profiles and invariants as input|

# Contributing

This project is not being actively developed at this time, but contributions
and suggestions are welcomed. Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and
actually do, grant us the rights to use your contribution. For details, visit
https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
