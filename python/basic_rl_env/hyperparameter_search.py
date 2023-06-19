import yaml
from pathlib import Path
import os
import logging
import itertools
import sys


# Paths: Config files and unity env.
path_to_working_dir = "python/basic_rl_env"
path_to_config_file = "./hyperparameter_search.yaml"
path_to_unity_env = "./build"
path_to_log_dir = "./logs"

# Ensure correct working dir.
if (os.getcwd() != Path(path_to_working_dir).absolute()):
    os.chdir(Path(path_to_working_dir).absolute())

use_build = False
if len(sys.argv) > 1:  # Optional parameter provided via cli.
        if any(s in sys.argv for s in ["--no-build"]):
            use_build = False

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
memory = config['behaviors']['RollerAgent']['network_settings']['memory']

#
hyperparameter_key_values = []
for key in hyperparamters:
    if isinstance(hyperparamters[key], list):
        hyperparameter_key_values.append({key: hyperparamters[key]})

hyperparameter_input = [list(entry.values())[0] for entry in hyperparameter_key_values]
hyperparameter_combinations = list(itertools.product(*hyperparameter_input))
logging.info(f"Found {len(hyperparameter_input)} hyperparameter with dynamic values.")

memory_key_values = []
if memory is not None:
    for key in memory:
        if isinstance(memory[key], list):
            memory_key_values.append({key: memory[key]})
memory_input = [list(entry.values())[0] for entry in memory_key_values]
memory_combinations = list(itertools.product(*memory_input))
logging.info(f"Found {len(memory_input)} memory parameters with dynamic values.")

for hyperparameter_option in hyperparameter_combinations:
    for memory_option in memory_combinations:
        # Id number of the run. As shown in tensorboard. Needed to ensure traceability.
        run_id_num = get_run_id()
        path_to_temp_config_file = f"./configs/{run_id_num}.yaml"

        logging.info(f"[{run_id_num}] New run started with id {run_id_num}.")

        tmp_hyperparameters = hyperparamters
        for index in range(len(hyperparameter_key_values)):
            key = list(hyperparameter_key_values[index].keys())[0]
            value = hyperparameter_option[index]
            tmp_hyperparameters[key] = value
            logging.info(f"[{run_id_num}] {key} = {value}.")

        tmp_memory = memory
        for index in range(len(memory_key_values)):
            key = list(memory_key_values[index].keys())[0]
            value = memory_option[index]
            tmp_memory[key] = value
            logging.info(f"[{run_id_num}] {key} = {value}.")

        tmp_config = config
        tmp_config['behaviors']['RollerAgent']['hyperparameters'] = tmp_hyperparameters
        tmp_config['behaviors']['RollerAgent']['network_settings']['memory'] = tmp_memory

        # Save modified config as yaml file.
        with open(Path(path_to_temp_config_file).absolute(), mode="w") as new_file:
            yaml.dump(tmp_config, new_file)
        
        return_code = 0
        # Execute ml-agents using a compiled environment.
        if use_build:
            return_code = os.system(
                f"mlagents-learn \
                {Path(path_to_temp_config_file).absolute()} \
                --env={Path(path_to_unity_env).absolute()} \
                --run-id={run_id_num}_basicenv_ppo_auto \
                --torch-device cpu \
                --force"
            )
        else:
            return_code = os.system(
                f"mlagents-learn \
                {Path(path_to_temp_config_file).absolute()} \
                --run-id={run_id_num}_basicenv_ppo_manual \
                --torch-device cpu \
                --force"
            )

        if return_code != 0:
            logging.warning(f"[{run_id_num}] error code.")

        logging.info(f"[{run_id_num}] return code = {return_code}.")
