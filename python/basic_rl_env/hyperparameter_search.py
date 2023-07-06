from pathlib import Path
from statistics import mean
from tensorflow.python.summary.summary_iterator import summary_iterator
from collections import OrderedDict
import yaml
import os
import logging
import itertools
import sys
import time
import json

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
            new = []
            for entry in parameters[key]:
                new.append({key: entry})
            key_values.append(new)

    
    combinations = list(itertools.product(*key_values))
    if production:
        logging.info(f"Found {len(key_values)} dynamic values.")
    return combinations

# Update key value pairs in temporary dict using the key values list and the current option.
# The run id is only used for logging purposes.
def update_parameter(base: dict, option: tuple, id: int) -> dict:
    # Work on copy of the base config values only.
    tmp = dict.copy(base)

    # Dynamic key value pairs are updated in the tmp dict.
    for entry in option:

        # Get the key for the entry in the option.
        key = list(entry.keys())[0]

        # Value for the given key.
        value = entry[key]

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

def get_mean_reward(name: str) -> float:
    cumulative_rewards = []
    path_to_result_folder = f"./results/{name}/RollerAgent/"
    path_to_result = sorted(Path(path_to_result_folder).glob("events.out.tfevents.*"))[0]

    for event in summary_iterator(str(path_to_result)):
        for v in event.summary.value:
            if v.tag == "Environment/Cumulative Reward":
                cumulative_rewards.append(v.simple_value)

    return mean(cumulative_rewards[-3:])

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
use_build_env = False
production = False
generate_summary = False

message_for_log = None
# Check the loaded config for user specified modes.
if "userconfig" in config:
    if config["userconfig"] is not None:
        # Build env requested?
        if "build" in config["userconfig"]:
            if config["userconfig"]["build"]:
                use_build_env = True

        # Production mode requested?
        if "production" in config["userconfig"]:
            if config["userconfig"]["production"]:
                production = True
        
        # Summary requested?
        if "summary" in config["userconfig"]:
            if config["userconfig"]["summary"]:
                generate_summary = True
        
        # Get the message from the config file to be logged.
        if "message" in config["userconfig"]:
            tmp_message = config["userconfig"]["message"]
            if tmp_message is not None and isinstance(tmp_message, str):
                message_for_log = tmp_message

    # User config information no longer needed.
    config.pop("userconfig")

# Logging config.
if production:
    logging.basicConfig(
        filename=Path(f"./logs/{get_run_id()}_search.log").absolute(),
        level=logging.INFO
        )

# Log the message from the config file.
if production and message_for_log is not None:
    logging.info(f"Note: {message_for_log}")

hyperparamters = config['behaviors']['RollerAgent']['hyperparameters']
network = config['behaviors']['RollerAgent']['network_settings']

# In case memory is configured in yaml file:
# Handle memory options seperate from network settings.
memory_comb = [()]
memory = None
if "memory" in network:
    memory = config['behaviors']['RollerAgent']['network_settings']['memory']
    network.pop('memory')
    memory_comb = get_parameter_combinations(memory)

hyper_comb = get_parameter_combinations(hyperparamters)
network_comb = get_parameter_combinations(network)

# Prepare summary.
if generate_summary:
    headings = []
    for section in [hyper_comb[0], network_comb[0], memory_comb[0]]:
        for entry in section:
            headings.append(list(entry.keys())[0])
    summary_dict = {}

# Get the number of runs the current config is goint to produce.
num_count = len(memory_comb) * len(network_comb) * len(hyper_comb)
if production:
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
            
            if production:
                logging.info(f"[{run_id}] New run started with id {run_id}.")

            # Get copy of the base config as loaded.
            tmp_config = dict.copy(config)

            tmp_hyper = update_parameter(hyperparamters, hyperparameter_option, run_id)
            tmp_config['behaviors']['RollerAgent']['hyperparameters'] = tmp_hyper

            tmp_network = update_parameter(network, network_option, run_id)
            tmp_config['behaviors']['RollerAgent']['network_settings'] = tmp_network
            
            # Memory might not be specified in the yaml file.
            if memory is not None:
                tmp_memory = update_parameter(memory, memory_option, run_id)
                tmp_config['behaviors']['RollerAgent']['network_settings']['memory'] = tmp_memory

            # Save modified config as yaml file.
            if production:
                with open(Path(path_to_temp_config_file).absolute(), mode="w") as new_file:
                    yaml.dump(tmp_config, new_file)
            
            # Execute ml-agents using a compiled environment.
            # Bypass if in test mode.
            return_code = 0
            start_time = time.time()
            run_name = f"{run_id}_basicenv_ppo_auto"
            if production:
                if use_build_env:
                    run_name = f"{run_id}_basicenv_ppo_auto"
                    # Start ml-algents using build version of unity.
                    return_code = os.system(
                        f"mlagents-learn \
                        {Path(path_to_temp_config_file).absolute()} \
                        --env={Path(path_to_unity_env).absolute()} \
                        --run-id={run_name}\
                        --torch-device cpu \
                        --force"
                    )
                else:
                    # Start ml-agents in the unity editor. Requires user interaction.
                    run_name = f"{run_id}_basicenv_ppo_manual"
                    return_code = os.system(
                        f"mlagents-learn \
                        {Path(path_to_temp_config_file).absolute()} \
                        --run-id={run_name} \
                        --torch-device cpu \
                        --force"
                    )

            end_time = time.time()
            if production:
                logging.info(f"[{run_id}] return code = {return_code}.")

            # Logging and error code handling.
            if return_code != 0:
                if production:
                    logging.warning(f"[{run_id}] error code.")
            else:
                run_durations.append(end_time - start_time)
                duration = (num_count - run_counter) * mean(run_durations)

                if production:
                    logging.info(f"[{run_id}] Duration: {int(run_durations[-1])} sec.")
                    logging.info(f"[{run_id}] Avg. duration: {int(mean(run_durations))} sec.")
                    logging.info(f"[{run_id}] Expected end time of all runs: {time.strftime('%d %b %Y %H:%M:%S', time.localtime(time.time() + duration))}.")
                
                if generate_summary:
                    summary_dict[run_id] = {}
                    for entry in [*hyperparameter_option, *network_option, *memory_option]:
                        summary_dict[run_id].update(entry)

                    summary_dict[run_id]['3_last_cumulative_reward'] = get_mean_reward(run_name)

if generate_summary:
    path_to_summary_file = f"./summaries/{get_run_id()}.json"
    with open(Path(path_to_summary_file).absolute(), mode="w", newline="") as summary_file:
        sorted_dict = OrderedDict(sorted(
            summary_dict.items(),
            key=lambda v: v[1]['3_last_cumulative_reward'],
            reverse=True))
        json.dump(sorted_dict, summary_file, indent=4)

