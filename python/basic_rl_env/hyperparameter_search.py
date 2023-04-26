import yaml
from pathlib import Path
import os
import logging
import itertools


# Paths: Config files and unity env.
path_to_working_dir = "python/basic_rl_env"
path_to_config_file = "./config/search.yaml"
path_to_temp_config_file = "./config/current.yaml"
path_to_unity_env = "./build"
path_to_log_dir = "./logs"

# Ensure correct working dir.
if (os.getcwd() != Path(path_to_working_dir).absolute()):
    os.chdir(Path(path_to_working_dir).absolute())

def get_run_id() -> int:
    dir_content = os.listdir(Path("./results/").absolute())
    numbers = []
    for entry in dir_content:
        numbers.append(int(entry.split("_")[0]))
    return max(numbers) + 1

logging.basicConfig(filename=Path(f"./logs/{get_run_id()}_search.log").absolute(), level=logging.INFO)

# Open the base config file.
with open(Path(path_to_config_file).absolute(), mode="r") as config_file:
    config = yaml.safe_load(config_file)

hyperparamters = config['behaviors']['RollerAgent']['hyperparameters']

#
dynamic_key_values = []
for key in hyperparamters:
    if isinstance(hyperparamters[key], list):
        dynamic_key_values.append({key: hyperparamters[key]})

input = [list(entry.values())[0] for entry in dynamic_key_values]

parameter_combinations = list(itertools.product(*input))

for comb in parameter_combinations:
    # Id number of the run. As shown in tensorboard. Needed to ensure traceability.
    run_id_num = get_run_id()
    logging.info(f"[{run_id_num}] New run started with id {run_id_num}.")
    tmp_hyperparameters = hyperparamters

    for index in range(len(dynamic_key_values)):
        key = list(dynamic_key_values[index].keys())[0]
        value = comb[index]
        tmp_hyperparameters[key] = value
        logging.info(f"[{run_id_num}] {key} = {value}.")

    tmp_config = config
    tmp_config['behaviors']['RollerAgent']['hyperparameters'] = tmp_hyperparameters

    # Save modified config as yaml file.
    with open(Path(path_to_temp_config_file).absolute(), mode="w") as new_file:
        yaml.dump(tmp_config, new_file)
    
    # Execute ml-agents using a compiled environment.
    return_code = os.system(
        f"mlagents-learn \
        {Path(path_to_temp_config_file).absolute()} \
        --env={Path(path_to_unity_env).absolute()} \
        --run-id={run_id_num}_basicenv_ppo_auto \
        --torch-device cpu \
        --force"
    )

    if return_code != 0:
        logging.warning(f"[{run_id_num}] error code.")

    logging.info(f"[{run_id_num}] return code = {return_code}.")
