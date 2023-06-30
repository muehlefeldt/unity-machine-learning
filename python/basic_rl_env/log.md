# Log
Notes to the ML-Agents runs.

## 00BasicEnvPpo
* Basic run to confirm tutorial.
* Base run used as verification for future runs.

## 01BasicEnvPpo
* Angular drag set 0. Sphere must not roll.
* RollerAgent now as cube and not as a sphere.
* Result was comparabale to the base run 00BasicEnvPpo.
* Added Ray Perception Sensor 3D.

## 02BasicEnvPpo
* Removed sensor input: No longer position of target and agent.
* Uses Ray Perception Sensor 3D. 
* Currently the agent can not rotate!
* Is the Ray Sensor working as expected? 

## 03BasicEnvPpo
* Replace the Ray Sensor Component with simple rays.
  * Background: The ray sensor adds to many information.
  * The drone receives only limited sensor data. 
* Rays introduced in 4 directions.

## 04BasicEnvPpo
* Normalised ray distance values.
* Code clean up needed.
* Still no change in PPO config.
* Compare against run 03: Check convergence speed. Better?
  * Limited comparison but speed seems better.

## 05BasicEnvPpo
* Introduce memory.
* Result was not very promissing.

## 06BasicEnvPpo
* More steps also with memory.
* Now running for 2e6 steps.
* Compare against run 04. Rerun 04 with more steps.
* Reward was very unstable.

## 07BasicEnvPpo
* Rerun of 04: No memory but more steps. 2e6 steps again used as in 05.
* How does the reward compare to 06 and 04?
* Unstable rewards.

## 08BasicEnvPpo
* Rerun of 04. No LSTM.
* Set step count to 1e6.
* Due to more steps behavior of beta, epsilon and/or
learning rate changes to reward signifcant?
* Step: 1000000. Time Elapsed: 7281.410 s.

## 09BasicEnvPpo
* Increase number of training areas. Now using 8 areas in total.
* Rerun a constant to config to verfiy result against 08BasicEnvPpo.
* Result: Rewards indicate a problem with code and the use of multiple areas.

## 10BasicEnvPpo
* Same as 10 but again run with 500k steps.
* Should be the same aus run 04 but with more training areas.

## 11BasicEnvPpo
* Only one training area. Area 0.

## 12BasicEnvPpo
* Only one training area. Area 7 to test local coordinate system.
* Shows same result as 10.
* Indicates code error.

## 13BasicEnvPpo
* Only one training area. Area 7 to test local coordinate system.
* Fixed: Ray measurements.
  * Rays were always cast based on the local coordinates. But in this cas global is correct.

## 14BasicEnvPpo
* Test again with multiple training areas.
* Expected: Improved result compared to 09 and 10.
* Result still not as during run 04.
* Work needed.

## 15BasicEnvPpo
* Increased distance between the training areas.
* Result: Again not the same rewards shown as during run 04.

## 16BasicEnvPpo
* Problem with ray length fixed.
* Increased distance between the training areas remains.
* Result: Reward plot as expected. Ray sensors fixed.

## 17BasicEnvPpo
* Rotation of the agent added.
* Control signal and sensor data 
* Forward indicator: If gizmos on, render only forward ray.

## 18BasicEnvPpo
* More hidden units.
* Previously not very much reward. Maybe longer training needed?

## 19BasicEnvPpo
* Sensor data change to rigidbody.
* Result: Good.

## 20BasicEnvPpo
* Increased training step count to 2e6 from 500k.
* Everything else remains steady.

## 21BasicEnvPpo
* Set beta: 1.0e-4. Was 5.0e-5.
* Look for: More stable learning since early in the run.
* Step count still set to 2e6.
* Reward drops during initial phase!
* Reward growth faster compared to 20. But still pretty unstable.

## 22BasicEnvPpo
* batch_size: 1000
* buffer_size: 10240

## 23BasicEnvPpo
* beta: 1.0e-2.
* Entropy should drop more slowly?

## 25BasicEnvPpo to 48BasicEnvPpo
* First use of a structured hyperparameter search.
* Still very simple.
* Uses hyperparameter_search.py to start search.

## 49
* Walls in area introduced.

## 50
* ?

## 51
* Test without build. Only in Unity editor.
* Observation: Runs using build executable are way faster.

## 52
* Fix: Training wareas wrong positions.
* Rerun of 51 with same config.
* Uses build env of unity.
* Result: Looks better comp. to 51.
* Error: Run was terminated due to waiting state of worker.

## 53
* Rerun of 52 due to earlier error.

## 54 till 73
* Parameter search using script.
* Best results from runs 60 and 66.

## 96
* More training areas.
* Uses best parameters from auto runs 76 to 95. Uses same config as run 78.
* Made error: Wrong beta value used.

## 97
* Slight change of reward handling in case of collision.
  * Add -1 and not set to -1.
* Still uses the wrong beta value!

## 98
* Rerun of 97 with correct beta value.
* Target: Compare to run 78.
* Comparison to run 78: Results differ. This run does not follow 78 after 800k steps in terms of reward.
* Speculation on the reason: Reward function changed?
* Need to compare configs between 78 and this run.
* Interesting observation: episode length between the the runs is comparable.

## 99
* Increased step number.
* Terminated with error.

## 100
* Rerun of 99 after rebuild.

## 103
* Discrete action test run.
* Uses parameter as 102 but changes to buffer size.
  * Docs recommend reduction in buffer size.
* Beta value to high? Or good? Is now 1e-2. Was way smaller before.
  * More tests needed with beta value.

## 108
* Test of script.

## 109
* Very first LSTM run.
* Aborted run. 

## 111
* Aborted.

## 115
* Not stable. memory_size = 32. sequence_length = 128.

## 119
* Not stable. memory_size = 64. sequence_length = 128.

## 122
* Looks good.

## 127
* Test random location of target.
* Uses build.
* Bad result clean run.

## 128
* Standard run w/o build.
* Manual run.
* Looks good at least.
* Result still no good but used same config as 127.

## 129 and 130
* Test of rewritten script.

## 131 and following
* Automated runs.
* Reward function may need tweaking.
* Use 154 as base config.

## 161
* Fix: Target position set to world.

## 162
* First try of run in editor using navmesh.
* Performance evaluation:
  * Performance severly compromised.

## 163
* Switched the wall navmesh components to navmesh obsticale.
* Walls were static navmesh set.
* Performance better but still restricted.

## 164, 165
* Same as 163 but using build.
* Question: Performance comparison with previous runs.
* Navmesh maybe to complex.

# 167
* Test of rewritten pathfinding.
* Again check in the editor first.
* Performance seems better -> Check again with compiled build.

# 168
* Same as 167 but compiled build.

# Next
* Second room.
* Target location set incorrectly.
* Test: Small reward for better state but not optimal state, +0.1.

# Longtrem plans
* Increase complexity of the environment.
