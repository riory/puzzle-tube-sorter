# puzzle-tube-sorter

This project allows to tinker with bead sorting on spindles.

It was inspired by this @TheVillageAvengers YouTube short: [Colour sorting challenge](https://youtube.com/shorts/LZfMWS_Q4gs?si=Ru9P5q_8EJdP7LT8)

![YT screenshot](/media/source.png "YT screenshot")

## Premise
- There is a total of 6 spindles
- Last spindle is initially empty
- Each spindle contains 9 beads of 5 colors (Red, Green, Blue, White, Yellow)
- Each bead can be moved on top of another spindle if capacity allows
- Once spindle contains single color it is considered solved
- To finish solving, last spindle should be empty in the end

## App
![Sample of RandomStrategySolver run](/media/sample_solve.png "Sample of RandomStrategySolver run")


Application is working in 2 **gameModes**: **Manual** and **Solver**

>puzzle-tube-sorter.exe gameMode=Manual

Will allow to manually enter step data - 2 spindle index (0-based). E.g. 05 means that bead should be moved from first to 6th spindle.

>puzzle-tube-sorter.exe gameMode=Solver solver=BogoSolver nogui

Will start app using provided solver (no step updates).

## Configuration

As of this version following keys are supported:

- **nogui** - flag, applies to Solver mode. Will run solver without UI updates.
- **gamemode** - string, Manual or Solver (see above)
- **speed** - int, number of milliseconds to wait between step updates. Solver only. 0 means no delay. 100 is default.
- **seed** - int, Random seed used for BeadSet initial generation - if provided, will be used for generation (this allows to run same set for different solvers)
- **maxstep** - int, number of steps that are the upper limit for solver to run. If exceeded and not solved, solver will halt.
- **solver** - string, solver class to be ran. See solvers section for more information

## Solvers

- BogoSolver - randomly chooses 2 spindles, inspired by BogoSort. Not really usefull
- RandomStrategySolver - employs 4-step strategy to solve
	- RandomUnload - empties spindle until it contains only same color beads
	- RandomLoad - picks beads of same color from other spindles, attempts to move mismatching beads to other spindles
	- RandomPurge - picks all beads from spindle (only runs after several spindles are solved)
	- FinalMove - moves all beds from last spindle if all spindles are solved and last one is not empty