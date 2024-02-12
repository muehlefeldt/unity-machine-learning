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


def get_run_id(complete_config: dict) -> int:
    """Get the run id (number) based on past runs in the result folder.
    If called multiple times, the result will be an increased number."""
    dir_contents = [
        *os.listdir(complete_config["paths"]["results_dir"]),
        *os.listdir(complete_config["paths"]["results_archive_dir"]),
        *os.listdir(complete_config["paths"]["log_dir"]),
        *os.listdir(complete_config["paths"]["summaries_dir"]),
        *os.listdir(complete_config["paths"]["configs_dir"]),
        *os.listdir(complete_config["paths"]["unity_configs_dir"]),
    ]
    numbers = []
    for entry in dir_contents:
        if "_" in entry:
            numbers.append(int(entry.split("_")[0]))
        else:
            numbers.append(int(entry.split(".")[0]))
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
                    # "paths": PATHS,
                    # Error state of the run. Default case is false.
                    "error": False,
                    # ID of this run config.
                    "run_id": first_id + tmp_id,
                    # Decide on base port for ml-agents.
                    "base_port": 5005 + tmp_id,
                    # Add CLI arguments to be used.
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


def save_env_config_file(complete_config: dict):
    """Save the configuration for the Unity environment to a file.
    File is loaded by Unity to configure the enviroment as requested."""

    complete_config["run_config"]["paths"]["env_config_file"] = (
        complete_config["run_config"]["paths"]["unity_configs_dir"]
        / f"{complete_config['run_id']}_unity_config.json"
    )

    # Get the wanted configuration.
    local_config: dict = complete_config["run_config"]["env_config"]

    # Add further info to the config here as requiered.
    local_config["runId"] = complete_config["run_id"]

    # Export path as string to ensure Unity can handle the path.
    # Path is absolute and points to the final stats file for the specific run.
    local_config["statsExportPath"] = str(
        complete_config["run_config"]["paths"]["statsExportPath"]
        / f"{complete_config['run_id']}_stats.json"
    )

    # Save the dict as backup file.
    with open(
        complete_config["run_config"]["paths"]["env_config_file"], mode="w", encoding="utf8"
    ) as new_file:
        json.dump(local_config, new_file, indent=4)

    # Save the dict to the pre-build directory.
    # Also store the file in the assets folder of the editor. Allows for use with the editor.
    with open(
        complete_config["run_config"]["paths"]["unity_env_data"] / "env_config.json",
        mode="w",
        encoding="utf8",
    ) as new_file:
        json.dump(local_config, new_file, indent=4)

    with open(
        complete_config["run_config"]["paths"]["unity_assets"] / "env_config.json",
        mode="w",
        encoding="utf8",
    ) as new_file:
        json.dump(local_config, new_file, indent=4)

    return


def commence_mlagents_run(run_info: dict) -> dict:
    """Commence a ML-Agents run using the configuration provided in the dict."""
    # Id number of the run. As shown in tensorboard. Needed to ensure traceability.
    run_id = run_info["run_id"]

    # Location of the config files saved to run info.
    run_info["run_config"]["paths"]["config_file"] = (
        run_info["run_config"]["paths"]["configs_dir"] / f"{run_id}.yaml"
    )

    # Save modified config as yaml file. This is the configuration of ML-Agents.
    if run_info["userconfig"]["production"]:

        # Construct and save the ML-Agents specific config.
        with open(
            run_info["run_config"]["paths"]["config_file"], mode="w", encoding="utf8"
        ) as new_file:
            config_to_save: dict = {"behaviors": run_info["run_config"]["behaviors"]}
            yaml.dump(config_to_save, new_file)

        save_env_config_file(run_info)

        # Start ml-algents training using build version of unity.
        start_time = time.time()

        # Construct the ml-agents arguments to be called through subprocess.
        ml_agents_arguments = []
        if run_info["userconfig"]["build"]:
            # Run ml-agents with pre build unity environemnts.
            ml_agents_arguments = [
                "mlagents-learn",
                f"{run_info['run_config']['paths']['config_file']}",
                f"--env={run_info['run_config']['paths']['unity_env']}",
                f"--run-id={run_id}",
                f"--num-envs={run_info['userconfig']['num_env']}",
                f"--base-port={run_info['base_port']}",
                "--no-graphics",
                "--torch-device",
                "cpu",
                "--force",
            ]
            # Is the run based on the result (nn) of a previous run?
            if not run_info["userconfig"]["not_based_on_previous_nn"]:
                ml_agents_arguments.append(
                    f"--initialize-from={run_info['userconfig']['previous_run_id']}"
                )
        # elif run_info["userconfig"]["build"] and run_info["userconfig"]["cli_build"]:

        else:  # Run mlagents with the unity editor.
            # If we do not use a build environment, interaction with the unity editor is needed.
            ml_agents_arguments = [
                "mlagents-learn",
                f"{run_info['run_config']['paths']['config_file']}",
                f"--run-id={run_id}",
                "--torch-device",
                "cpu",
                "--force",
            ]
            # Is the run based on the result (nn) of a previous run?
            if not run_info["userconfig"]["not_based_on_previous_nn"]:
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
        # run_info["return_code"] = process_result.returncode
        run_info["run_name"] = run_id
        run_info["result_dir"] = f"{run_info['run_config']['paths']['results_dir']}/{run_id}"

    else:  # Not production.
        run_info["error"] = True
        run_info["error_msg"] = "Not production mode."

    return run_info


def check_env_config():
    """Check env_config in the configuration yaml file for content and datatypes.
    If problem is found (missing keys or value types wrong) Error is raised."""

    # Env_config contains all custom settings for the unity environment. If not present stop here.
    if not "env_config" in config:
        raise ValueError

    # Provide here key and type of the provided value in the yaml.
    # Given as {key, type of value, can_be_list}.
    keys_to_lookup: list[dict] = [
        {"key": "sensorCount", "value_type": int, "can_be_list": True},
        {"key": "useDecoy", "value_type": bool, "can_be_list": False},
        {"key": "createWall", "value_type": bool, "can_be_list": False},
        {"key": "doorWidth", "value_type": float, "can_be_list": False},
        {"key": "randomWallPosition", "value_type": bool, "can_be_list": False},
        {"key": "randomDoorPosition", "value_type": bool, "can_be_list": False},
        {"key": "targetAlwaysInOtherRoomFromAgent", "value_type": bool, "can_be_list": False},
        {"key": "targetFixedPosition", "value_type": bool, "can_be_list": False},
        {"key": "maxStep", "value_type": int, "can_be_list": False},
    ]

    # Are keys and types of the specified value present?
    for key_value in keys_to_lookup:
        key = key_value["key"]  # The current key.
        value_type = key_value["value_type"]  # The current type of the value.

        if not key in config["env_config"]:
            raise ValueError

        # If the config file contains a list, every entry of the list needs to be checked for type.
        if key_value["can_be_list"] and isinstance(config["env_config"][key], list):
            for entry in config["env_config"][key]:
                if not isinstance(entry, value_type):
                    raise ValueError

        # Just check type of the provided value in the config.
        # Default case, even if the value can be in a list.
        else:
            if not isinstance(config["env_config"][key], value_type):
                raise ValueError

    return


def check_userconfig():
    """Check userconfig in the configuration yaml file for content and datatypes.
    If problem is found (missing keys or value types wrong) Error is raised."""

    # Userconfig contains all custom settings. If not present stop here.
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
        ("not_based_on_previous_nn", bool),
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


def check_directories(complete_config):
    """Ensure requiered directories exist."""
    for key in [
        "log_dir",
        "summaries_dir",
        "results_dir",
        "results_archive_dir",
        "configs_dir",
        "unity_configs_dir",
    ]:
        # Succeds even if directory already exists.
        os.makedirs(Path(complete_config["paths"][key]).absolute(), exist_ok=True)


def remove_run_files_log(complete_config: dict, summary_list: list[dict]):
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
        os.remove(config["paths"]["log_file"])
    except OSError:
        print("Not able to delete log file.")

    return


def prepare_paths(complete_config):
    """Save the provided path as absolute paths using pathlib."""
    for key in complete_config["paths"]:
        complete_config["paths"][key] = Path(complete_config["paths"][key]).absolute()


if __name__ == "__main__":
    # Ensure correct working dir.
    if os.getcwd() != Path("./python/basic_rl_env").absolute():
        os.chdir(Path("./python/basic_rl_env").absolute())

    # Open the base config file for this script.
    with open(
        Path("./hyperparameter_search.yaml").absolute(), mode="r", encoding="utf8"
    ) as config_file:
        config = yaml.safe_load(config_file)

    # Make sure config provided in configuration file is useable.
    try:
        check_userconfig()
        check_env_config()
    except ValueError:
        print("Error: Check configuration file for value problem.")
        raise

    # Make paths in the configfile useable and ensure dirs are ok.
    prepare_paths(config)
    check_directories(config)

    userconfig = dict.copy(config["userconfig"])
    # env_config = dict.copy(config["env_config"])
    config.pop("userconfig")  # User config information no longer needed.
    # config.pop("env_config")

    # Check the loaded config for user specified modes.
    # Build env requested?
    # use_build_env: bool = userconfig["build"]

    # Production mode requested?
    production: bool = userconfig["production"]

    # Summary requested?
    generate_summary: bool = userconfig["summary"]
    num_env: int = userconfig["num_env"]

    # Get the message from the config file to be logged.
    message_for_log: str = userconfig["message"]

    # Get the id of the first run. Used for logging and summary.
    ID_FIRST_RUN: int = get_run_id(config)

    # Logging config.
    config["paths"]["log_file"] = config["paths"]["log_dir"] / f"{ID_FIRST_RUN}_search.log"
    # Path(f"./logs/{ID_FIRST_RUN}_search.log").absolute()
    logging.basicConfig(
        filename=config["paths"]["log_file"],
        level=logging.INFO,
    )

    # Log the message from the config file.
    if message_for_log is not None:
        logging.info("Note: %s", message_for_log)

    # Get dynamic parameters from the config file.
    # Create all possible and valid combinations of these parameters.
    dynamic_parameters_config = get_dynamic_parameters(config, [])
    # dynamic_parameters_env_config = get_dynamic_parameters(env_config, [])
    combinations = get_parameter_combinations(dynamic_parameters_config)

    # Get the number of runs the current config is goint to produce.
    NUM_COUNT = len(combinations)
    if production:
        logging.info("%i runs are going to be started.", NUM_COUNT)

    summary = [{}]
    if not userconfig["build"]:
        summary = list(map(commence_mlagents_run, combinations))
    # elif userconfig["num_process"] == 1:
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
        remove_run_files_log(config, summary)
