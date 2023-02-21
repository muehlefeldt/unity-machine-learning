import yaml
from pathlib import Path
import os

path_to_config_file = "basic_rl_env/config/rolleragent_ppo.yaml"
path_to_temp_config_file = "basic_rl_env/config/current.yaml"
path_to_unity_env = "basic_rl_env/build"

# Open the base config file.
with open(Path(path_to_config_file).absolute(), mode="r") as config_file:
    config = yaml.safe_load(config_file)

# Select parameter to modify in base config.
parameter_to_modify = "buffer_size"
values = [
    10,
    50,
    100,
    1000,
    2000
]

# Id number of the run. As shown in tensorboard. Needed to ensure traceability.
run_id_num = 25

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