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
