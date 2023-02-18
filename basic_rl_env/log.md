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

# Next
* Merge both git repositories for easier handling.
* Reintroduce LSTM and test different step counts and options.
* Walls.

