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

## 167
* Test of rewritten pathfinding.
* Again check in the editor first.
* Performance seems better -> Check again with compiled build.

## 168
* Same as 167 but compiled build.
* Performance: First 10k steps take 300 secs. Afterwards significant speedup.
* Terminated.

## 169
* Reward function introduced.
* Test run in the editor.

## 170
* Again run in editor.
* Learning progress not stable.
* Fix: One AddReward() changed to SetReward().
* Question remains: How does a good reward function look here?

## 171
* Run with revised reward function.
  * Reward 0f if distance to target increases.

## 172
* Not nice result.
* Target only found once.

## 173
* Reward recuced. beta = 0.5f.

## 174
* Editor was not started in time.
* Disregard.

## 175
* Check more tensorboard data charts.

## 176
* Further change to tensorboard recording.

## 177 and more
* Auto run.
* Check in the morning.

## 201
* Revised reward:
  * Closer to target: +0.1
  * Farther away from target: -0.1
* Run in editor.
* Not good results.

## 202 following
* Script test
* Disregard
 
## 210 and following
* Script test again

## 212 and following
* Test with reward function revised.
  * -0.2 is worse distance.
  * +0.1 if better

## 220
* Test python env.
* Repeat of run 215.

## 221 and following
* Large number of runs.
* Build used.
* Performance not sufficient.
* 221 shows ok result.

## 247
* Test of fixed Collider and step counter.

## before 340
* Test runs to evaluate the script. Summary was introduced.
* Config file restructure.

## 340 and following
* Large number of runs.
* 2 build env started during training.
* Check first run and evaluate.
* 500k steps in each run.
* 34992 runs ... too manny?
* Terminated: ETA was in 2025.

## 342 ff.
*  Terminated: Config not correct.

## 946
* 16 sensors.
* No wall.

## 1016
* Part of overfit test.
* 5e6 ep.
* Fixed inner wall and door.
* Bad result.

## 1016
* Part of overfit test.
* Very long run with 30e6 episodes.
* Fixed inner wall and door.
* Shocking result with -1 convergence.
* Problem with code?
* Maxstep to 15k. 

## 1017
* Overfit test.
* No inner wall. No door.
* 5e6 ep.
* Reduced sensor number back to default with 4 sensors.
* Fast convergence in comparison to 946 (16 sensors) and 8 ...
* Maxstep reduced back to default 5000.
* Ok result at 1.5e6 ep.

## 1018
* Overfit test.
* Inner wall and door. 
* Increased ep to 50e6.
* Rest comparable to 1017. 
* Terminated after 20e6 ep.

## 1019
* More steps per ep allowed. 25000 instead of 5000.
* Overfit test.
* Same as 1018 otherwise.

## 1020
* Same as 1019 but fewer steps.
* Overfit test.
* When does the run converge to 1?

## 1021
* Overfit test.
* Beta increased.
* Steps per ep. reduced back to 5k.

## 1022 and more
* Overfit tests.
* Learning rate and schedule
* beta and schedule 
* Only for 1e6 steps
* Env: Fixed door, agent and target.

## 1060
* Terminated. Error in Unity.

## 1061
* Collision handling changed. Collision between agent and other object does not end ep.
* Simple run with basic config.
* Overfit test.
* Converged towards -1. Hit always the ep. max step limit.

## 1062
* Change to reward function. Not every step punished.

## 1063
* Increased maxstep per ep to 25k.
* Every step punished.
* Problem with position check? Implausible position may be plausible ...

## 1064
* Position check of the agent removed. 
* In theory the agent should not be able to leave the training area. 
* Terminated.

## 1065
* Collision problems. The agent clips through contact points of the walls.
* Still part of overfit test.

## 1066
* Reward function: + for better distance, otherwise -.
* 5k max steps per ep.

## 1067
* Reward function: Punish more distance -0.15 and reward less dist to target with 0.1.
* Looks good.

## 1068
* Random door. Rest same as 1067.
* Reward based on distance.
* Looks good.

## 1069
* Door and wall random.
* Agent and target position fixed.

## 1070
* Build.
* Slight change in collision punishment. Stay collision more punished.

## 1071
* Slightly varied agent position.

## 1072
* Target pos not fixed.
* Code error with pos handling of agent.

## 1073
* Reset the agent on every ep begin.
* Previously: Agent reset only under given circumstances but due to change in collision handling problem.
* Uses build.

## 1074
* 10e6 ep.
* Same as 1073.
* Does it converge?
* Editor run, for visual observation by user.
* After every episode agent reset to first room.
* Ep. end after maxStep reached or target found.

## 1075
* 50e6 ep.
* Same as 1073 and 1074.

## 1076
* Shorter test with new decoy.
* Build used.
* Terminated. Very high cumulative rewards initially.
* Forgot training areas.

## 1077
* Rerun of 1076 with more areas.
* No build.
* Terminated. One agent too few maxsteps.

## 1078
* Rerun of 1077.

## 1079
* Test --initialize-from=<run-identifier> option of ml-agents.
* New option introduced to python code.
* Run ids now shortend.
* Terminated code error in python. Was not using the result of 1078.

## 1080
* Rerun 1079 with code fix.
* Terminal inidcated correct initial usage of previous results from run 1078.
* 20e6 steps.

## 1081
* Buffer size increased. Should help stability. Maybe even help speed if more training areas are used?
* Slight change to reward function. +0.1 in case of rotation to encourage rotation.
* Run uses the NN from 1079.
* Terminated. Wrong NN as basis.

## 1082
* Rerun of 1081 with NN from 1080 as basis.
* Slight change in reward (+ for rotation) did not improve result.
  * Question: Completly retraining from ground up for NN requiered to see changes?

## 1083
* Reward function test.
* Reward based on Matignon et al.
* Problem: No idea what the convergence target is.
* 40e6 steps to get first impression.
* How is the the reward function handling movements forwards and backwards?
* Reminder: Buffer size was increased for run 1081.

## 1084
* Rerun 1083 but with 60e6 steps.
* Result not good. Very unstable learning process.

## 1085
* Test simple reward function.
* Negative reward for collisions increased.
* First run with 25e6 steps to get feel for config.
* Using build.
* Result was strange. Reward for rotation may be too large.
* Developed strategy was to simply rotate until max limit was reached.
* Terminated.

## 1086
* Reward change. Now +0.05 for rotation.
* Otherwise rerun of 1085.
* Remark: Reward for rotation maybe stupid.

## 1087 - 1090
* Python code tests. Can be ignored.

## 1091
* Reward function does ignore rotation.
* Strange result. Was unstable until the end.
* Ep length did not decrease.
* Reward function to blame?

## 1092
* Reward structured very similar to 1073.
  * -0.5 and -0.3 for collisions.
  * But more punishment for step back. Dist to target increased result in more punischment. Too much?
* 10e6 again.
* Possible problem? C# code changes may cause issues with learning process. Ending of the ep was changed. Check this!

## 1093
* Switch of target (and decoy) to other room enabled.
* Switch happens if target was reached by the agent.
* Reward structure same as 1073.

## 1094
* Change agent pos on every episode begin. 
* Not really much information from this run.

## 1095
* Editor run for test.
* Target and decoy now take distance to eachother and distance to door into account.
  * Aim is to mitigate instance where the docy blocks the door or the target is too close to the door.
* Agent is randomly in one room positioned.
* Pos of agent is reset on every ep begin.
  * Maybe a change to this is not requiered?
* Terminated.

## 1096 and 1097
* Python tests.

## 1098
* Continue with 1095 training. But longer and complete restart.
* Build used.
* Fixes door isses when using multiple training areas.
* Python produces now summary even if terminated by keyboard interrupt.
* Ok result.

## 1099
* Uses 1098 NN as basis.
* Continued training.
* Result: NN was meh.

## 1100
* Revised reward function. Encourage rotation with +0.01.
* Result was ... interesting. The reward function with reward for rotation is questionable. 

## 1101
* Drone can take multiple actions per step.
* Reverted back to basic reward function.

## 1102
* Wrong reward function selected.
* Process very unstable.

## 1103
* Basic reward function selected.
* Build.
* Terminated: Unity clipping issue needs resolving.

## 1104
* Editor.
* Movement of the agent changed to rigidbody.AddForce.
* Test run.
* AddForce maybe problematic?

## 1105
* Build.
* Continues 1104.
* Again terminated. More work on the movement needed. Rotation makes problems. 

## 1106
* Rigidbody rotation introduced.
* ComplexDist reward function.
* Run in the editor for visual supervision.
* Terminated because run was based on previous nn.

## 1107
* Rerun of 1106.
* Shit.

## 1108
* One action per step. Validate new movement functions.
* Basic reward. Was correctly set?
* Make commit in the morning.
* Terminated: Reward structure not as expected.

## 1109
* Short run in the editor.

## 1110
* Ml-agents error.

## 1111
* Error. Nor clear.

## 1112
* Rerun of 1109.
* Öhm, reward was previously too high? Why is the difference so high between 1110 and 1112? What changed?
  * The penalties for the collisions?

## 1113
* Reward function set to only take the collisions in to account.
* No movement of the agent?

## ...
* No agent movement. Issue.

## 1115
* Test again with basic reward.

## 1116 until 1118
* Done sleepy.
* Fix problem with tag.
* Checkpoint in the door introduced with +1 on first trigger and -0.2 on subsequent triggers.
* Reward only based on collisions and contacts with objects.

## 1119
* Layer fixed of the checkpoint.
* Test run.

## 1120
* Based on the NN of 1119.
* 100e6 steps but basd on 1119.
* Idea: Lets see how we are doing with this reward function incl. checkpoint at the door.
* Result was disappointing. Is the switch between the rooms too difficult?

## 1121
* Curiosity test.
* Build. 
* Strength of curiosity?
* Interrupted. Wrong reward.

## 1122
* Reward set to sparse.
* Again curiosty test, same as 1121.
* Result: Shit. Not stable learning.

## 1123
* Reward without distance. Only collision punished. Punish each step. +1 For target reached. Sparse reward function.
* Staged process. 
  * First: Train only with the target and no decoy.
* No curiosty.
* First part in editor. Check for problems with changed decoy deployment.
* Heuristic was still set in area 1.
* Disappointing result.

## 1124
* Short run.
* Test 

## 1125 and 1126
* Test in editor. Only to check door and frame placement.
* Door frame introduced.
  * Allows granular reward giving through different tags on the objects.

## 1127
* Sparse reward function gives reward for door passage. 0.5 for correct direction and -0.5 for incorrect direction of passage.
  * After 10 passages -1 given to discourage multiple passages.
* Test with build.

## 1128 - 1129
* No clue.

## 1130
* Experiment reward function introduced to separate new tests from existing reward functions.
  * Uses sparse reward through collisions and being close enough to the target.
  * Also uses distance based function following Matignon et al. 
* Short run. In editor.
* Beta = 0.5.
  * Question regarding beta is the interaction with sparse rewards given through collisions.

## 1131
* Beta set to 0.2.
* Experiment reward function. Combine sparse collision rewards with feedback for every step.

## 1132
* Beta set to 0.4.
* Build run with 30e6 steps.

## 1133
* Omega = 0.4 and beta = 0.4 remains.
* Reduced number of steps. 10e6.
  * Should be sufficient to get an overview.
* Take a detailed look at the episode length.
* And maybe a further reduction in steps to 5e6 reasonable.

## 1134
* Rerun of 1133 but in editor to check more training areas.
* Will be terminated early if behaviour of the new areas is good.
* CPU usage low?

## 1135
* Rerun of 1133 in build. Compare runtime against 1134.
* Runtime was better. Comparable reward.
* Ep length reduction still problem but there was no change to improve this.

## 1136
* Build rerun of 1133 again. Now 40 areas with prefab.
* Check runtime and reward.
* 1000 sec less needed.
* Maybe mainly an issue of RAM usage? Who knows. No, tensorboard uses a lot RAM.

## 1137
* Now 60 areas. Rest unchanged.
* We now seem to hit the CPU limit.
* Check runtime and reward.

## 1138
* Rerun but without tensorboard running in the background.

## 1139 to 1156
* Number of configurations of hyperparameters.
* 1149 looked nice in the summary.

## 1157 and more.
* Compare runtime against 1149. Uses the same config but more areas. Baseline config has mem size 128 and seq len 64.
  * Now up to 100 areas. Was 60, I believe.
  * No tensorboard running.
* Some LSTM parameter search.

## 1169
* Based on best result from 1157 ff. 
* Similiar to 1149 but 100e6 steps.
* Lets take a look at sunday.
* Bad result. Ep. len not reduced.

## 1170
* Test without door. Short only with 5e6 steps.
* We should be seeing a somewhat useable result. Or not?
...

## 1171
* No door. Changed the reward function. But it is shit, isnt it?

## 1172
* Do not use distance to target as reward part.

## 1173
* We need to take one or more steps back.
* No door and target fixed.

## 1174
* Terminated.

## 1175
* Target no longer fixed. Otherwise same as 1173.

## 1176
* Longer run based on 1174. Terminated.
  * Wrong previous run selected.

## 1177
* Longer run. Based on 1175.

## 1178
* Test run. 32 sensors.
* Uses build. 

## 1179
* Again a lot sensors. Same as 1178.
* Experiment reward function: Fixed missing collision penalties.
* Short run with build.
* Add one single area to be used as heuristic test area.

## 1180
* Increased collision penalty.

## 1181
* End ep on collision.
* Why is the ep length not reducing as predicted?
* Problem with the observation size? Move the setting of the size to awake()?

## 1182
* Same as 1181 but with build and longer.

## 1183
* Same as 1182. With user monitoring. No build.
* Terminated with error. Awake() and sensor input size?

## 1184
* Awake() with sensor size setting introduced.
* Otherwise as 1183. User monitoring.

## 1185
... No clue.
* Very short run.

## 1186
* Reduced size of nn to 256.
* Compare against waht result?

## 1187
* Continue with the nn of 1186.
* Train for another 15e6 steps.
* Expected runtime is 2h.
* Again with build.
* Question: Do we converge against 1? Does the convergence stop?

## 1188
* Still continued learning based in 1187 and 1186.
* Long run overnight with 50e6.
* Converges against somewhat 1.0.

## 1189
* Shit show with wall.

## 1190
* Door 0.5 and -0.8.
* Short run with build.
* Had problem, see 1191.

## 1191
* Door passage was handled badly. Way too simple approach?
* Door rewards were not used. Fix.
* Rerun of 1190. 
* Next run: Without terminating episodes on collision. Just add -1 on collision?

## 1192
* Do not terminate ep. on collision.

## 1193
* Terminated before any running.

## 1194
* OnTriggerExit() now used to better reflect door passage of the agent.
* Reward function needs to be switched. Currently uses stupid combination of distance and step panelty. 
* Terminated to address issue with reward. 

## 1195
* Every step now punished by -1 / maxstep. As was previously the case.
* Result: From one room in the other ok ish. But from the the other room ... questionable.

## 1196
* Shows same behavior as above.
* Despite reward changes.

## 1197
* Make reward function dense as shown by chat. Use dist to target normalised.
* Now: penalty + last_dist - new_dist

## 1198
* Random rotation introduced.
* Short run for visual supervision in the editor.
* No massive change.
* Keep this change.

## 1199
* Reward change: Combine dist and step penalty.
* Run in editor.

## 1200
* The component of distance in the reward function increased.
* Again in editor.
* Not all Areas used.
* Result shows no significant change.

## 1201
* As 1200.
* Reward weight change. More weight given to distance.

## 1202
* Based on 1201. Using previous nn.
* Run with build and longer.

## 1203
* No hieght change possible.
* Terminated.

## 1204
* Same again. No height change.
* Error fixed with height inputs.

## 1205
* Short run.
* Terminated.

## 1206
* Rerun 1206.

## 1207
* Simpler reward.
* Terminated with error.

## 1208
* Rerun. Reward func fix.

## 1209
* Still no height change.
* Reward: penalty for every step and last dist - current dist * factor.
* Very unstable and slow learning. But atleast some convergence shown.
* Length of the episodes inconsistent.

## 1210
* Short run in editor.
* Reward: +0.01 if dist to target better. Else -0.02. Collision penalties also apply.
* Desperate run to be honest.
* Idea for future run: Training parameter adjust back to smaller values? But the parameters were choosen to promote more stable learning.
* Result: Very unstable learning.

## 1211
* Still no movement in y possible for drone.
* Dist to target normalised as reward.
* Shit show: Reward needs to negative when using the dist to target. Higher values indicate less distance to target. See 1212.

## 1212
* Still no movement in y possible for drone.
* reward = -1f * m_DistToTargetNormal;
* Consider to multiply the reward with a factor: For example -0.5 to lessen the rewards impact.
* Trend during learning: The learning process is slowly progressing but not over the top stable progress.
* Consider the factor stuff from above. -0.1 seems to be ok.

## 1213
* Still no movement in y possible for drone.
* Build.
* Reward changed as outlined above with facotr.
* reward = -0.1f * m_DistToTargetNormal;
* Longer run to get a feel. 10e6 steps.
* Result: Learning process is very unstable. But atleast promissing.
* Terminated. Error in action space size. Changes were not made to the prefab of the traingarea. 

## 1214
* Rerun of 1213. But 5e6 steps.
* Error fix in action space size. Does that even matter?
* Shows also very instable learning.

## 1215
* reward = -0.05f * m_DistToTargetNormal;
* Test different reward scaling factors.
* Still no movement in y possible for drone.

## 1216
* reward = -0.01f * m_DistToTargetNormal;
* Further scale factor search.

## 1217 - 1312
* Hyperparameter search.
* Only using 3e6 steps each run.
* Terminated in the end. Results were absolut shit.
  * Termination was pain. Every single needs to be terminated by STRG+C.
* Ok results / best results: 1225, 1228, 1241, ...
* Lesson from these runs? Its not working?

## 1313
* Some bla with reward scaling.
* Result again not high enough and not stable.

## 1314
* Change to punishment.
* Terminated. Again not good learning.

## 1315
* Editpr run. Target fixed pos to simplify the problem. 
  * Currently no height change possible. Target always at the same pos. Door fixed.

## 1316
* No wall. Fuck me.
* Target fixed.

## 1317
* No wall. Target random pos.

## 1318
* Change in reward scaling. No improvement.

## 1319
* LSTM changes. Where are we with the LSTM config.
* Mem size up to 128, as is suggested as default.
* Does this provide change in comparison to 1318?
  * First impression: Yes significant change to learning process.
  * Second impression: No shit.
* Not nice.

## 1320
* Further LSTM test.

## 1321
* Again LSTM test.
* Shit.

## 1322
* No LSTM.
* No, it is shit.

## 1323
* LSTM back on.

## 1324
* More memory size. Equal to sensor count / observation count.

## 1325
* Added position data to the observations.
* Test simple task.

## 1326
* Very long run in the editor. I need to sleep.
* What happens?
* Terminated after 25e6 steps. Results are unuseable. 
* Discussion:
  * The agent knows own and target position.
  * This should be a very easy problem to solve.
  * Currently using: LSTM and dense reward.

## 1327
* Attention: Reward may be very high due to dense, positive reward.
  * Issue: Unclear what a good reward can be.
* Short run ... 
* reward = 1 - dist_normal
* Compile problem. Run did not start.

## 1328
* Similar to 1327 but reward scaled down:
  *  currentReward = 0.1f * (1f - m_DistToTargetNormal)
* Again back to 5e6 steps.
* More observations still added. Does this even change anything?
* Was terminated by ml-agents. Env exception occured. No clue why.

## 1329
* One single training area.
* Question: Do we have a problem with multiple training areas? Esp. with the Areas20 prefab?
* Hyperparameters and reward are the same as 1328.
* Attention: Still more observations are used as input.

## 1330
* Reward change. Still only one training area.
  * currentReward = -0.01f * m_DistToTargetNormal;
* Rest is the same as 1329.
* Compare cumulative reward to 1215.

## 1331 New branch with less code
* Code was cleaned.
* Rerun 1330.
* Slight collision change.
* Terminated.

## 1332
* Reward scale changed.
  * currentReward = -0.1f * m_DistToTargetNormal
* Compare to 1331 after 1e6 steps.
* Still very unstable.

## 1333
* currentReward = -1f * m_DistToTargetNormal
* Shit.

## 1334
* No collision penalties.

## 1335
* currentReward = scalar * m_DistToTargetNormal;
* Did not run. Promblem with editor.

## 1336
* Terminated. Reward problem.

## 1337

## 1338
* Decision requester set to 5.
* Terminated after no good progress observed.


## 1339
* Slow process.
* Only one training area.

## 1340
* Rerun of 1339. More training areas. 

## 1341
* Back to known reward ... 

## 1342
* reward = -1f / maxStep

## 1343
* Same as 1342 but less sensors. No up and down.

## 1344 and 1345 and 1346
* Awake() fix.
* Rerun of 1343.
* 1344 and 1345 were incorrect runs. Heuristic was activated. 
* Do we see a change to 1343?
* Also uses more training areas. 
* 1346 also terminated.
  * Still seeing unstable learning behaviour.

## 1347 and 1348
* Again with Decision period set to 1.
  * Still no clue what this changes.
  * Really! What does this shit do?
* Attention: The one single training area needs to be checked.

## 1349
* DB = 5 and DB = True.
* Decision period: 5 and take decision between.
* This should converge against +1.
* What is the decision period?
* Result is not too bad ...

## 1350 - Interesting and good run
* DP = 5 and DB = True.
* Rerun of 1349 but with simple dense reward structure. 1349 was sparse reward with static punishment for each step.
  * currentReward = -0.01f * m_DistToTargetNormal;
* How does this behave with decision period 5 and decisions between?
* Converge against +1f?
* Shouldn't a dense reward help the training speed?
* Attention: Punishment per step is now higher! Result is lower initial reward.
* Question: Decision period higher?
* Visual observation in the editor:
  * Good movement speed.
  * How does DB = True impact this shit?

## 1351
* Continue based on 1350.
* Does this converge? Against which value?
* Simply a longer run of 1350. No meaningful improvements.

## 1352
* Decision period (dp) to 10 and DB true.
* Using build.
* Compare to 1350. Was using dp = 5.

## 1353
* Decision period (dp) to 10 but no decisions between.
* Otherwise same as 1353.
* Compare against 1352!
  * Much higher initial cumulative reward compared to 1352.

## 1354
* DP = 3 and decisions between.
* Compare against 1352.
* First impression: Unstable learning in comparison to 1352.

## 1355+
* Hyperparameter search.
* DP = 10 and DB (Decisions between).
* Summary file: 1356 best run with learning 1e-3 (pretty high) and less layers with num_layers = 1.
* 1356:
  * Visiual observations shows no good to the target.
  * Reward is converging but below 0f.

## 1361
* Rerun of 1356 config.
* max_step: Limited to 2.5e6.
* Change: DP = 15 and NO DB. What is the change?
* Cumulativ reward starts much higher. 
* Ep length is not really decreasing.
* NN in the editor:
  * Also very slow movement. Same as 1363.

## 1362
* Same as 1361.
* DP = 15 and DB = True.
* Ep. length decreasing faster compared to 1361.

## 1363
* Test with DP = 20 and DB = False.
* Same as 1361.
* Take a look at the NN in the editor.
  * Agent does pretty much not move. Very very slow movement.
* Why the cumulativ reward just above 0f?
* Ep. length is pretty much static. 

## 1364
* Based on 1350 config: DP = 5 and DB.
* Initial run with DB = True. Should we try a run with DB = False?
* Again relativly short run.
* Compare against 1350.
  * Initial progress is similar to 1350. Esp. cumulative reward.
  * Is there even any change to 1350 in the config? Other than probably the NN structure.

## 1365
* Same as 1364. Reward scale change.
  * currentReward = -0.1f * m_DistToTargetNormal;
  * Attention: Cumulativ reward changes significant but expected.
* Still shorter run.
* Ep. length shows similar behaviour at the beginning to 1350, 1349 and 1364. At least in the beginning ...

## 1366
* Again reward scale change.
  * currentReward = -0.005f * m_DistToTargetNormal;
* Comparable to 1349? Was 1349 with sparse reward?

## 1367
* currentReward = -0.01f * m_DistToTargetNormal;
* Collision penalty increased.
* Compare against?

## 1368
* Long run with max_steps = 10e6.
* Same as 1367 otherwise.
* Still no convergence against 10e6.

## 1369 - 1980
* LSTM hyperparamter search.
* Only 2.5e6 max_steps.
* "Best" run 1379.  memory_size = 128 and sequence_length = 64.
* Observation: Interestlingy relativley high values for LSTM config best. 

## 1981
* Terminated.

## 1982
* Run based on 1379 config but no longer.
* max_steps = 10e6.
* Previous run was only 2.5e6.
* I suspect there will be no significant improvement.
* Learning progress continued positivley. Surprise I guess.
* Recording created.

## 1983
* 2.5e6 steps with DP = 5 and no DB.




## 
* Well ... sparse reward again?

## ToDo for the night
* How do we ensure enough distance between agent and target at respawn?
