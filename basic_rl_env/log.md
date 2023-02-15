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


## Next
* Introduce rotation (control signal and sensor data)?
* Increase number of training areas.
