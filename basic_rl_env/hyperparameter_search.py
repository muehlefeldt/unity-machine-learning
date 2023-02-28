import yaml
from pathlib import Path
import os

# Paths: Config files and unity env.
path_to_working_dir = "basic"
path_to_config_file = "./config/rolleragent_ppo.yaml"
path_to_temp_config_file = "./config/current.yaml"
path_to_unity_env = "./build"

# Ensure correct working dir.
if (os.getcwd() != Path("./basic_rl_env").absolute()):
    os.chdir(Path("./basic_rl_env").absolute())

# Open the base config file.
with open(Path(path_to_config_file).absolute(), mode="r") as config_file:
    config = yaml.safe_load(config_file)

# Select parameter to modify in base config.
parameter_to_modify = "beta"
values = [
   1e-4,
   5e-4,
   1e-3,
   5e-3,
   1e-2
]

# Id number of the run. As shown in tensorboard. Needed to ensure traceability.
run_id_num = 44

for value in values:
    # Reload the base config. Modify selected parameter.
    tmp_config = config
    tmp_config['behaviors']['RollerAgent']['hyperparameters'][parameter_to_modify] = value

    # Save modified config as yaml file.
    with open(Path(path_to_temp_config_file).absolute(), mode="w") as new_file:
        yaml.dump(tmp_config, new_file)
    
    # Execute ml-agents using a compiled environment.
    return_code = os.system(
        f"mlagents-learn \
        {Path(path_to_temp_config_file).absolute()} \
        --env={Path(path_to_unity_env).absolute()} \
        --run-id={run_id_num}BasicEnvPpo_{parameter_to_modify}_{value} \
        --torch-device cpu \
        --force"
    )

    print(f"Run {run_id_num} with {parameter_to_modify} = {value}: Code {return_code}.")

    run_id_num += 1