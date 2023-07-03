from pathlib import Path
from statistics import mean
import yaml
import os
import logging
import itertools
import sys
import time
import csv

# Get the run id (number) based on past runs in the result folder.
def get_run_id() -> int:
    dir_content = os.listdir(Path("./results/").absolute())
    numbers = []
    for entry in dir_content:
        numbers.append(int(entry.split("_")[0]))
    return max(numbers) + 1

# Get the parameter combinations.
def get_parameter_combinations(parameters) -> tuple[list, list]:
    key_values = []
    for key in parameters:
        # Check if value for key is a list. If so, store key value pair.
        if isinstance(parameters[key], list):
            key_values.append({key: parameters[key]})

    input = [list(entry.values())[0] for entry in key_values]
    combinations = list(itertools.product(*input))
    logging.info(f"Found {len(input)} dynamic values.")
    return key_values, combinations

# Update key value pairs in temporary dict using the key values list and the current option.
# The run id is only used for logging purposes.
def update_parameter(base: dict, key_values: list, option, id: int) -> dict:
    # Work on copy of the base config values only.
    tmp = dict.copy(base)

    # Dynamic key value pairs are only of interest.
    for index in range(len(key_values)):

        # Get the key for the given index.
        key = list(key_values[index].keys())[0]

        # Value from the option for the given key.
        value = option[index]

        # Update the temporary dict.
        tmp[key] = value
        
        logging.info(f"[{id}] {key} = {value}.")
    return tmp

def get_number_of_runs(list_of_key_values: list[dict]) -> int:
    num_runs = 1
    for entry in list_of_key_values:
        num_runs = entry.keys()[0]
        print()

    return num_runs

# Paths: Config files and unity env.
path_to_working_dir = "python/basic_rl_env"
path_to_config_file = "./hyperparameter_search.yaml"
path_to_unity_env = "./build"
path_to_log_dir = "./logs"

# Ensure correct working dir.
if (os.getcwd() != Path(path_to_working_dir).absolute()):
    os.chdir(Path(path_to_working_dir).absolute())

# Open the base config file.
with open(Path(path_to_config_file).absolute(), mode="r") as config_file:
    config = yaml.safe_load(config_file)

# Variables for control flow.
use_build = True
no_test = True

# Check the loaded config for user specified modes.
if "userconfig" in config:
    if config["userconfig"] is not None:
        if "mode" in config["userconfig"]:
            if any(s in config["userconfig"]["mode"] for s in ["no-build", "editor"]):
                use_build = False
            if any(s in config["userconfig"]["mode"] for s in ["test"]):
                test = False
    
    # Mode information no longer needed.
    config.pop("userconfig")

# Logging config.
if no_test:
    logging.basicConfig(
        filename=Path(f"./logs/{get_run_id()}_search.log").absolute(),
        level=logging.INFO
        )

hyperparamters = config['behaviors']['RollerAgent']['hyperparameters']
network = config['behaviors']['RollerAgent']['network_settings']

# In case memory is configured in yaml file:
# Handle memory options seperate from network settings.
memory_key_value, memory_comb = [()], [()]
memory = None
if "memory" in network:
    memory = config['behaviors']['RollerAgent']['network_settings']['memory']
    network.pop('memory')
    memory_key_value, memory_comb = get_parameter_combinations(memory)

hyper_key_value, hyper_comb = get_parameter_combinations(hyperparamters)
network_key_value, network_comb = get_parameter_combinations(network)

use_summary = False

if use_summary:
    path_to_summary_file = f"./summaries/{get_run_id()}.csv"
    with open(Path(path_to_summary_file).absolute(), mode="w", newline="") as summary_file:
        summary_writer = csv.writer(summary_file)
        headings = []
        for section in [memory_key_value, hyper_key_value, network_key_value]:
            for entry in section:
                headings.append(list(entry.keys())[0])
        headings.append('5_last_cumulative_reward')
        summary_writer.writerow(headings)

# Get the number of runs the current config is goint to produce.
num_count = len(memory_comb) * len(network_comb) * len(hyper_comb)
logging.info(f"{num_count} runs are going to be started.")

# Store run durations.
run_durations = []
run_counter = 0

# Combine all possible options for possible runs.
for hyperparameter_option in hyper_comb:
    for network_option in network_comb:
        for memory_option in memory_comb:
            run_counter += 1

            # Id number of the run. As shown in tensorboard. Needed to ensure traceability.
            run_id = get_run_id()
            path_to_temp_config_file = f"./configs/{run_id}.yaml"

            logging.info(f"[{run_id}] New run started with id {run_id}.")

            # Get copy of the base config as loaded.
            tmp_config = dict.copy(config)

            tmp_hyper = update_parameter(hyperparamters, hyper_key_value, hyperparameter_option, run_id)
            tmp_config['behaviors']['RollerAgent']['hyperparameters'] = tmp_hyper

            tmp_network = update_parameter(network, network_key_value, network_option, run_id)
            tmp_config['behaviors']['RollerAgent']['network_settings'] = tmp_network
            
            # Memory might not be specified in the yaml file.
            if memory is not None:
                tmp_memory = update_parameter(memory, memory_key_value, memory_option, run_id)
                tmp_config['behaviors']['RollerAgent']['network_settings']['memory'] = tmp_memory

            # Save modified config as yaml file.
            if no_test:
                with open(Path(path_to_temp_config_file).absolute(), mode="w") as new_file:
                    yaml.dump(tmp_config, new_file)
            
            # Execute ml-agents using a compiled environment.
            # Bypass if in test mode.
            return_code = 0
            start_time = time.time()
            if no_test:
                if use_build:
                    # Start ml-algents using build version of unity.
                    return_code = os.system(
                        f"mlagents-learn \
                        {Path(path_to_temp_config_file).absolute()} \
                        --env={Path(path_to_unity_env).absolute()} \
                        --run-id={run_id}_basicenv_ppo_auto \
                        --torch-device cpu \
                        --force"
                    )
                else:
                    # Start ml-agents in the unity editor. Requires user interaction.
                    return_code = os.system(
                        f"mlagents-learn \
                        {Path(path_to_temp_config_file).absolute()} \
                        --run-id={run_id}_basicenv_ppo_manual \
                        --torch-device cpu \
                        --force"
                    )
            end_time = time.time()
            logging.info(f"[{run_id}] return code = {return_code}.")

            # Logging and error code handling.
            if return_code != 0:
                logging.warning(f"[{run_id}] error code.")
            else:
                run_durations.append(end_time - start_time)
                logging.info(f"[{run_id}] Duration: {int(run_durations[-1])} sec.")
                logging.info(f"[{run_id}] Avg. duration: {int(mean(run_durations))} sec.")
                duration = (num_count - run_counter) * mean(run_durations)
                logging.info(f"[{run_id}] Expected end time of all runs: {time.strftime('%d %b %Y %H:%M:%S', time.localtime(time.time() + duration))}.")
                
                if use_summary:
                    with open(Path(path_to_summary_file).absolute(), mode="a", newline="") as summary_file:
                        summary_writer = csv.writer(summary_file, delimiter="")
                        #summary_writer.writerow


            
