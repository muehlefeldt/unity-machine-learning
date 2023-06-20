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

# Get the run id (number) based on past runs in the result folder.
def get_run_id() -> int:
    dir_content = os.listdir(Path("./results/").absolute())
    numbers = []
    for entry in dir_content:
        numbers.append(int(entry.split("_")[0]))
    return max(numbers) + 1

def get_parameter_combinations(parameters) -> tuple[list, list]:
    key_values = []
    for key in parameters:
        if isinstance(parameters[key], list):
            key_values.append({key: parameters[key]})

    input = [list(entry.values())[0] for entry in key_values]
    combinations = list(itertools.product(*input))
    logging.info(f"Found {len(input)} hyperparameter with dynamic values.")
    return key_values, combinations

def get_parameter(tmp, key_values: list, option, id: int):
    for index in range(len(key_values)):
        key = list(key_values[index].keys())[0]
        value = option[index]
        tmp[key] = value
        logging.info(f"[{id}] {key} = {value}.")
    return tmp

# Logging config.
logging.basicConfig(filename=Path(f"./logs/{get_run_id()}_search.log").absolute(), level=logging.INFO)

# Open the base config file.
with open(Path(path_to_config_file).absolute(), mode="r") as config_file:
    config = yaml.safe_load(config_file)

use_build = True
if "userconfig" in config:
    if config["userconfig"] is not None:
        if "mode" in config["userconfig"]:
            if any(s in config["userconfig"]["mode"] for s in ["no-build"]):
                use_build = False
    config.pop("userconfig")

hyperparamters = config['behaviors']['RollerAgent']['hyperparameters']
network = config['behaviors']['RollerAgent']['network_settings']

memory_key_value, memory_comb = [()], [()]
memory = None
# In case memory is configured in yaml file:
# Handle memory options seperate from network settings.
if "memory" in network:
    memory = config['behaviors']['RollerAgent']['network_settings']['memory']
    network.pop('memory')
    memory_key_value, memory_comb = get_parameter_combinations(memory)

hyper_key_value, hyper_comb = get_parameter_combinations(hyperparamters)
network_key_value, network_comb = get_parameter_combinations(network)

# Combine all possible options for possible runs.
for hyperparameter_option in hyper_comb:
    for network_option in network_comb:
        for memory_option in memory_comb:
            # Id number of the run. As shown in tensorboard. Needed to ensure traceability.
            run_id = get_run_id()
            path_to_temp_config_file = f"./configs/{run_id}.yaml"

            logging.info(f"[{run_id}] New run started with id {run_id}.")

            tmp_config = config

            tmp_hyper = get_parameter(hyperparamters, hyper_key_value, hyperparameter_option, run_id)
            tmp_config['behaviors']['RollerAgent']['hyperparameters'] = tmp_hyper

            tmp_network = get_parameter(network, network_key_value, network_option, run_id)
            tmp_config['behaviors']['RollerAgent']['network_settings'] = tmp_network
            
            # Memory might not be specified in the yaml file.
            if memory is not None:
                tmp_memory = get_parameter(memory, memory_key_value, memory_option, run_id)
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
                    --run-id={run_id}_basicenv_ppo_auto \
                    --torch-device cpu \
                    --force"
                )
            else:
                return_code = os.system(
                    f"mlagents-learn \
                    {Path(path_to_temp_config_file).absolute()} \
                    --run-id={run_id}_basicenv_ppo_manual \
                    --torch-device cpu \
                    --force"
                )

            if return_code != 0:
                logging.warning(f"[{run_id}] error code.")

            logging.info(f"[{run_id}] return code = {return_code}.")
