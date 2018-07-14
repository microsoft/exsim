# Overview

This project implements a C# version of a memory safety model that builds upon and advances the model explored in the ruby implementation.

This implementation consists of three components:

1. `msmodel`: Defines the classes for the memory safety model and the exploitation simulator.
2. `mssim`: Implements an example tool for executing the simulator for a predefined set of scenarios.
3. `vexclass`: A graphical tool for defining the properties of a specific vulnerability, which can be used as input to the simulator.

# Usage

A simple example of using the simulator with a predefined target can be seen by running the following command:

```
mssim.exe /runsim
```

This will produces a textual description of the simulations attempted and the probability of different exploit chains being successful in exploiting the target.

# Design

This project, like the ruby version, also models exploitation as a non-deterministic finite state machine. In this model, each state is the current memory safety violation and each transition is an exploitation technique for transforming a violation into another violation (e.g. transform a read into a write).

The simulator is provided with a `Target` as input which defines the invariants of the target being exploited. These invariants are defined in terms of a `Profile` and include the `Hardware`, `OperatingSystem`, `Application`, initial `Violation`, and initial `Assumption` list. The transitions that are supported by the simulator abstractly define how exploitation techniques pivot from one memory safety violation to another. Using the target information as input, the simulator then traverses the state machine until it reaches a desired end-state violation (or a fixed point).

The list of known target profiles and exploitation techniques is defined by XML files that correspond to each profile type, such as `Flaws.xml`, `Hardware.xml`, `OperatingSystem.xml`, and `Techniques.xml`.

The primary driver for the memory safety model is defined by the `MemorySafetyModel` class which can be used to explore the target profiles that are supported
