import copy
import itertools
import json
import logging
import os
import shutil
import signal
import subprocess

# import shutil
import time
from collections import OrderedDict
from multiprocessing import Pool
from pathlib import Path
from statistics import mean, stdev

import numpy as np

# import tensorflow as tf
import yaml
from tensorboard.backend.event_processing.event_file_loader import EventFileLoader

# from tensorflow.core.util import event_pb2
# from tensorflow.data import TFRecordDataset


def get_run_id() -> int:
    """Get the run id (number) based on past runs in the result folder.
    If called multiple times, the result will be an increased number."""
    dir_contents = [
        *os.listdir(Path(PATHS["results_dir"]).absolute()),
        *os.listdir(Path(PATHS["log_dir"]).absolute()),
        *os.listdir(Path(PATHS["summaries_dir"]).absolute()),
    ]
    numbers = []
    for entry in dir_contents:
        numbers.append(int(entry.split("_")[0]))
    if not numbers:
        return 0
    return max(numbers) + 1


def get_dynamic_parameters(base_config: dict, address: list) -> list:
    """Get all dynamic parameters from the config dict. Recursive function to parse nested dicts.
    Dynmic values are given as list.
    """
    new = []  # List to be returned.
    for key, value in base_config.items():
        # Dynamic values:
        if isinstance(value, list):
            # Every value of the found list as separate value stored.
            tmp_new = []
            for entry in value:
                tmp_new.append(
                    {
                        "address": address + [key],
                        "value": entry,
                    }
                )
            new.append(tmp_new)
        # Nested dict:
        elif isinstance(value, dict):
            # Recursive call:
            result = get_dynamic_parameters(value, address + [key])
            # Should the returned list be empty, no dynamic values found and result can be
            # discarded.
            if result:
                for entry in result:
                    new.append(entry)
    return new


def good_memory_settings(run_config: dict) -> bool:
    """Check run info / configuration for a valid memory configuration."""
    try:
        sequence_len = run_config["base_config"]["behaviors"]["RollerAgent"]["network_settings"][
            "memory"
        ]["sequence_length"]

        batch_size = run_config["base_config"]["behaviors"]["RollerAgent"]["hyperparameters"][
            "batch_size"
        ]
    except KeyError:
        # KeyError may occur if no memory is set. In this case the check is not requiered.
        return True

    # When using memory, sequence length must be less than or equal to batch size.
    return sequence_len <= batch_size


def set_dict_value(config_dict: dict, key_chain: list, value):
    """Recursive set function for a provided dict.
    Value is set at the position specified in the key chain.
    """
    # Recursion anchor. Last entry in key chain reached. Value can be set.
    if len(key_chain) == 1:
        config_dict[key_chain[-1]] = value
    else:
        set_dict_value(config_dict[key_chain[0]], key_chain[1:], value)


def get_parameter_combinations(parameters: list[dict]) -> list[dict]:
    """Get the parameter combinations with associated run id."""

    # Generate the possible combinations of parameter values.
    possile_combinations = list(itertools.product(*parameters))

    # Create final list with format: Id: Parameter combination.
    first_id = ID_FIRST_RUN
    tmp_id = 0

    id_possible_combinations = []
    for combination in possile_combinations:
        # Use deepcopy to ensure a copy without any references to the copied dict are created.
        tmp_config = copy.deepcopy(config)
        for entry in combination:
            print(entry)
            address = entry["address"]
            set_dict_value(tmp_config, address, entry["value"])

        # Check possible parameter combinations and discard implausibe combinations.
        # Otherwise add combination of parameters to the runs to be commensed.
        if good_memory_settings(tmp_config):
            id_possible_combinations.append(
                {
                    # Dynamic parameter values for this run.
                    "parameters": combination,
                    # User configuration.
                    "userconfig": userconfig,
                    # Run configuration to be used during the run.
                    "run_config": tmp_config,
                    # Path to the build environment.
                    "paths": PATHS,
                    # Error state of the run. Default case is false.
                    "error": False,
                    # ID of this run config.
                    "run_id": first_id + tmp_id,
                    # Decide on base port for ml-agents.
                    "base_port": 5005 + tmp_id,
                }
            )
        tmp_id += 1

    if production:
        logging.info("Found %i value combinations.", len(id_possible_combinations))

    return id_possible_combinations


def get_mean_reward(name: str) -> tuple[float, float]:
    """Get the mean reward over the last 5 cumulative rewards entries in the tfevents file."""
    cumulative_rewards = []

    # Get the tfevents file associated with the current run.
    path_to_result_folder = f"./results/{name}/RollerAgent/"
    path_to_result = sorted(Path(path_to_result_folder).glob("events.out.tfevents.*"))[0]

    # Using tensorflow to access the tfevents data.
    # datarecord = EventFileLoader(str(path_to_result)).Load()
    for event in EventFileLoader(str(path_to_result)).Load():
        # event = event_pb2.Event.FromString(batch.numpy())
        for value in event.summary.value:
            if value.tag == "Environment/Cumulative Reward":
                cumulative_rewards.append(value.tensor.float_val[0])

    # Return mean of the last 5 recorded cummulative rewards.
    rewards_of_interest = cumulative_rewards[-100:]
    return mean(rewards_of_interest), np.round(np.std(rewards_of_interest), 10)


def commence_mlagents_run(run_info: dict) -> dict:
    """Commence a ML-Agents run using the configuration provided in the dict."""
    # Id number of the run. As shown in tensorboard. Needed to ensure traceability.
    run_id = run_info["run_id"]

    # Location of the config file saved to run info.
    run_info["config_file"] = f"{run_info['paths']['configs_dir']}/{run_id}.yaml"

    # Save modified config as yaml file.
    if run_info["userconfig"]["production"]:
        with open(Path(run_info["config_file"]).absolute(), mode="w", encoding="utf8") as new_file:
            yaml.dump(run_info["run_config"], new_file)

        return_code = 0
        run_name = f"{run_id}"

        # Start ml-algents training using build version of unity.
        start_time = time.time()

        # Construct the ml-agents arguments to be called through subprocess.
        ml_agents_arguments = []
        if run_info["userconfig"]["build"]:  # Run ml-agents with pre build unity environemnts.
            ml_agents_arguments = [
                "mlagents-learn",
                f"{Path(run_info['config_file']).absolute()}",
                f"--env={run_info['paths']['unity_env']}",
                f"--run-id={run_name}",
                f"--num-envs={run_info['userconfig']['num_env']}",
                f"--base-port={run_info['base_port']}",
                "--no-graphics",
                "--torch-device",
                "cpu",
                "--force",
            ]
            # Is the run based on the result (nn) of a previous run?
            if run_info["userconfig"]["based_on_previous_nn"]:
                ml_agents_arguments.append(
                    f"--initialize-from={run_info['userconfig']['previous_run_id']}"
                )

        else:  # Run mlagents with the unity editor.
            # If we do not use a build environment, interaction with the unity editor is needed.
            ml_agents_arguments = [
                "mlagents-learn",
                f"{Path(run_info['config_file']).absolute()}",
                f"--run-id={run_name}",
                "--torch-device",
                "cpu",
                "--force",
            ]
            # Is the run based on the result (nn) of a previous run?
            if run_info["userconfig"]["based_on_previous_nn"]:
                ml_agents_arguments.append(
                    f"--initialize-from={run_info['userconfig']['previous_run_id']}"
                )

        # Execute ml-agents in a separate process.
        try:
            process_result: subprocess.CompletedProcess = subprocess.run(
                ml_agents_arguments,
                shell=True,
                check=True,
            )
        except subprocess.CalledProcessError as err:
            run_info["error"] = True
            run_info["error_msg"] = str(err)
        
        except KeyboardInterrupt:
            run_info["keyboard_interrupt"] = True    

        # Update the dict containing run infos.
        # Stores the directory path needed for possible delete of created files.
        run_info["duration"] = time.time() - start_time
        #run_info["return_code"] = process_result.returncode
        run_info["run_name"] = run_name
        run_info["result_dir"] = f"{run_info['paths']['results_dir']}/{run_name}"
    
    else: # Not production
        run_info["error"] = True
        run_info["error_msg"] = "Not production mode."

    return run_info


def check_userconfig():
    """Check userconfig in the configuration yaml file for content and datatypes."""

    # Userconfig contains all custom settings.
    if not "userconfig" in config:
        raise ValueError

    # Provide here key and type of the provided value in the yaml. 
    # Given as (key, type of value).
    keys_to_lookup = [
        ("build", bool),
        ("production", bool),
        ("summary", bool),
        ("num_env", int),
        ("num_process", int),
        ("message", str),
        ("keep_files", bool),
        ("based_on_previous_nn", bool),
        ("previous_run_id", int),
    ]

    # Are keys and types of the specified value present?
    for combo in keys_to_lookup:
        if not combo[0] in config["userconfig"]:
            raise ValueError
        if not isinstance(config["userconfig"][combo[0]], combo[1]):
            raise ValueError

    return


def update_and_clean_summary(summary_list: list[dict]) -> dict:
    """Update and clean the summary information. Creates pure dict from list of dicts.
    Some information not neeeded or can not be stored as a json.
    Get mean reward of last entries for all runs and save."""
    summary_dict: dict = {}
    for entry in summary_list:
        try:
            if not entry["error"]:
                # Store mean reward over the last entries to the run.
                (
                    entry["last_cumulative_reward"],
                    entry["last_cumulative_reward_std"],
                ) = get_mean_reward(entry["run_name"])
            else:
                # If error occured during mlagents run store default value.
                # Only requiered to ensure correct sort.
                entry["last_cumulative_reward"] = float("-inf")

            # Run config to much information in the summary.
            entry.pop("run_config")

            # Path not storeabe as json.
            entry.pop("paths")

            summary_dict[entry["run_id"]] = entry
        except KeyError:
            print("KeyError during summary generation.")
    return summary_dict


def create_summary_file(summary_list: list[dict]):
    """Save sorted summary file."""
    if summary_list == [{}]:
        return

    final_summary = update_and_clean_summary(summary_list)

    path_to_summary_file = f"./summaries/{ID_FIRST_RUN}_summary.json"
    with open(
        Path(path_to_summary_file).absolute(), mode="w", newline="", encoding="utf8"
    ) as summary_file:
        # Sort the dict created during the runs.
        # The saved file shall be sorted by the highest cummulativ rewards.
        sorted_dict = OrderedDict(
            sorted(
                final_summary.items(), key=lambda v: v[1]["last_cumulative_reward"], reverse=True
            )
        )
        json.dump(sorted_dict, summary_file, indent=4)

    # If requested delete summary file.
    if not userconfig["keep_files"]:
        try:
            os.remove(Path(path_to_summary_file).absolute())
        except OSError:
            print("Not able to remove summary file.")


def check_directories():
    """Ensure requiered directories exist."""
    for name in ["./logs", "./summaries", "./results", "./configs", "/build"]:
        # Succeds even if directory already exists.
        os.makedirs(Path(name).absolute(), exist_ok=True)


def remove_run_files_log(summary_list: list[dict]):
    """Delete files that have been created. Main purpose is to minimise number of irrelevant run id.
    Especially useful during debug runs. Set appropiate option in config file."""
    if summary_list == [{}]:
        return
    
    # Remove files created during mlagents-learn runs. Config file of each run and the directory
    # in the results directory.
    for run_info in summary_list:
        try:
            os.remove(Path(run_info["config_file"]).absolute())
        except (OSError, KeyError):
            print("Not able to delete run config file.")

        shutil.rmtree(Path(run_info["result_dir"]).absolute(), ignore_errors=True)

    # Delete log file.
    try:
        # Remove handlers from logging configuration. Enable the log file to be deleted.
        logging.getLogger().handlers.clear()
        os.remove(log_path.absolute())
    except OSError:
        print("Not able to delete log file.")

    return


if __name__ == "__main__":
    # Paths: Config files and unity env.
    PATHS = {
        "working_dir": "./python/basic_rl_env",
        "config_file": "./hyperparameter_search.yaml",
        "unity_env": str(Path("C:/build/windows").absolute()),
        "log_dir": "./logs",
        "summaries_dir": "./summaries",
        "results_dir": "./results",
        "configs_dir": "./configs",
    }

    # Ensure correct working dir.
    if os.getcwd() != Path(PATHS["working_dir"]).absolute():
        os.chdir(Path(PATHS["working_dir"]).absolute())

    check_directories()

    # Open the base config file.
    with open(Path(PATHS["config_file"]).absolute(), mode="r", encoding="utf8") as config_file:
        config = yaml.safe_load(config_file)

    try:
        check_userconfig()
    except ValueError:
        print("Error: Check user_config in configuration file.")
        raise

    userconfig = dict.copy(config["userconfig"])
    # User config information no longer needed.
    config.pop("userconfig")

    # Check the loaded config for user specified modes.
    # Build env requested?
    use_build_env = userconfig["build"]

    # Production mode requested?
    production = userconfig["production"]

    # Summary requested?
    generate_summary = userconfig["summary"]
    num_env = userconfig["num_env"]

    # Get the message from the config file to be logged.
    message_for_log = userconfig["message"]

    # Based on previous run?
    use_previous_nn: bool = userconfig["based_on_previous_nn"]
    previous_run_id: int = userconfig["previous_run_id"]

    # Get the id of the first run. Used for logging and summary.
    ID_FIRST_RUN = get_run_id()

    # Logging config.
    log_path = Path(f"./logs/{ID_FIRST_RUN}_search.log").absolute()
    logging.basicConfig(
        filename=log_path,
        level=logging.INFO,
    )

    # Log the message from the config file.
    if message_for_log is not None:
        logging.info("Note: %s", message_for_log)

    # Get dynamic parameters from the config file.
    # Create all possible and valid combinations of these parameters.
    dynamic_parameters = get_dynamic_parameters(config, [])
    combinations = get_parameter_combinations(dynamic_parameters)

    # Get the number of runs the current config is goint to produce.
    NUM_COUNT = len(combinations)
    if production:
        logging.info("%i runs are going to be started.", NUM_COUNT)

    summary = [{}]
    if not userconfig["build"]:
        summary = list(map(commence_mlagents_run, combinations))
    #elif userconfig["num_process"] == 1:
    #    # Todo
    else:
        # Perform actual calculations using ml-agents distributed over a number of processes.
        with Pool(
            userconfig["num_process"],
            initializer=signal.signal,
            initargs=(signal.SIGINT, signal.SIG_IGN),
        ) as p:
            try:
                summary = p.map(commence_mlagents_run, combinations)
            except KeyboardInterrupt:
                print("User interreupt.")

    if generate_summary:
        create_summary_file(summary)

    if not userconfig["keep_files"]:
        remove_run_files_log(summary)
