import itertools
import json
import logging
import math
import os
import subprocess

# import shutil
import time
from collections import OrderedDict
from multiprocessing import Pool
from pathlib import Path
from statistics import mean

# import tensorflow as tf
import yaml
from tensorboard.backend.event_processing.event_file_loader import EventFileLoader

# from tensorflow.core.util import event_pb2
# from tensorflow.data import TFRecordDataset


def get_run_id() -> int:
    """Get the run id (number) based on past runs in the result folder.
    If called multiple times, the result will be an increased number."""
    dir_contents = [
        *os.listdir(Path("./results/").absolute()),
        *os.listdir(Path("./logs/").absolute()),
        *os.listdir(Path("./summaries/").absolute()),
    ]
    numbers = []
    for entry in dir_contents:
        numbers.append(int(entry.split("_")[0]))
    if not numbers:
        return 0
    return max(numbers) + 1


def get_dynamic_parameters(base_config: list[dict]) -> list:
    """Get all dynamic parameters from the config dicts."""
    key_values = []
    for section in base_config:
        parameters = section[list(section.keys())[0]]
        for key in parameters:
            # Check if value for key is a list. If so, store key value pair.
            if isinstance(parameters[key], list):
                new = []
                for entry in parameters[key]:
                    new.append({"type": list(section.keys())[0], "content": {key: entry}})
                key_values.append(new)

    return key_values


def good_memory_settings(option: dict) -> bool:
    """Check run info / configuration for a valid memory configuration."""
    # Prepare variables.
    sequence_len = None
    batch_size = None

    # Is sequence length a dynamic parameter?
    for para in option["parameters"]:
        try:
            sequence_len = para["content"]["sequence_length"]
        except KeyError:
            continue

    # Is batch size a dynamic parameter?
    for para in option["parameters"]:
        try:
            batch_size = para["content"]["batch_size"]
        except KeyError:
            continue

    # If not found in dynamic parameters get the default value from the base configuration.
    # The value should be a single value and not a list.
    if sequence_len is None:
        sequence_len = option["base_config"]["behaviors"]["RollerAgent"]["network_settings"][
            "memory"
        ]["sequence_length"]

    if batch_size is None:
        batch_size = option["base_config"]["behaviors"]["RollerAgent"]["hyperparameters"][
            "batch_size"
        ]

    # When using memory, sequence length must be less than or equal to batch size.
    return sequence_len <= batch_size


def check_combinations(comb: list[dict]) -> list:
    good_combinations = []
    for run_info in comb:
        if good_memory_settings(run_info):
            good_combinations.append(run_info)
    return good_combinations


def get_parameter_combinations(parameters: list[list]) -> list[dict]:
    """Get the parameter combinations with associated run id."""

    # Generate the possible combinations of parameter values.
    possile_combinations = list(itertools.product(*parameters))

    # Create final list with format: Id: Parameter combination.
    first_id = get_run_id()

    id_possible_combinations = [
        {
            # "run_id": first_id + possile_combinations.index(x),
            # Dynamic parameter values for this run.
            "parameters": x,
            # User configuration.
            "userconfig": userconfig,
            # Base configuration.
            "base_config": config,
            # Path to the build environment.
            "path_env": Path(path_to_unity_env).absolute(),
            # Error state of the run. Default case is false.
            "error": False,
        }
        for x in possile_combinations
    ]

    # Check possible parameter combinations and discard implausibe combinations.
    id_possible_combinations = check_combinations(id_possible_combinations)

    # Add id and base port to run info.
    for comb in id_possible_combinations:
        tmp_id = id_possible_combinations.index(comb)
        # ID of this run config.
        comb["run_id"] = first_id + tmp_id
        # Decide on base port for ml-agents.
        comb["base_port"] = 5005 + tmp_id

    if production:
        logging.info("Found %i value combinations.", len(id_possible_combinations))
    return id_possible_combinations


def update_parameters_with_option(base: dict, run_info: dict):
    """
    Update key value pairs in temporary dict using the key values list and the current option.
    The run id is only used for logging purposes.
    """
    work_dict = base["behaviors"]["RollerAgent"]
    para_option = run_info["parameters"]
    id_num = run_info["run_id"]

    # Dynamic key value pairs are updated in the tmp dict.
    for entry in para_option:
        # Get the key of the section containing the parameter.
        type = entry["type"]
        entry_key = list(entry["content"].keys())[0]

        # Get the key of the parameter.
        # para_key = list(entry["content"][entry_key].keys())[0]

        # Get value of the parameter from the current option.
        value = entry["content"][entry_key]

        # If memory parameter change location to network settings.
        # Otherwise write value to selected section and parameter.
        # if entry_key == "memory":
        if type == "memory":
            work_dict["network_settings"][type][entry_key] = value
        else:
            work_dict[type][entry_key] = value

        if run_info["userconfig"]["production"]:
            logging.info("[%i] %s = %s.", id_num, entry_key, value)
    return


# def get_number_of_runs(list_of_key_values: list[dict]) -> int:
#    """ Get the number of runs to be performed. Calculation based on the """
#    num_runs = 1
#    for entry in list_of_key_values:
#        num_runs = entry.keys()[0]
#        print()
#
#    return num_runs


def get_mean_reward(name: str) -> float:
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
    return mean(cumulative_rewards[-5:])


def commence_mlagents_run(run_info: dict) -> dict:
    """Commence a ML-Agents run using the configuration provided in the dict."""
    # Id number of the run. As shown in tensorboard. Needed to ensure traceability.
    run_id = run_info["run_id"]
    production = run_info["userconfig"]["production"]

    if production:
        logging.info("[%i] New run started with id %i.", run_id, run_id)

    # Get copy of the base config as loaded.
    tmp_config = dict.copy(run_info["base_config"])

    update_parameters_with_option(tmp_config, run_info)

    path_to_temp_config_file = f"./configs/{run_id}.yaml"

    # Save modified config as yaml file.
    if production:
        with open(Path(path_to_temp_config_file).absolute(), mode="w", encoding="utf8") as new_file:
            yaml.dump(tmp_config, new_file)

        return_code = 0
        run_name = f"{run_id}_basicenv_ppo_auto"

        # Start ml-algents training using build version of unity.
        start_time = time.time()

        # Do we use a build environment?
        if run_info["userconfig"]["build"]:
            # Run ml-agents with pre build unity environemnts.
            try:
                subprocess.run(
                    [
                        "mlagents-learn",
                        f"{Path(path_to_temp_config_file).absolute()}",
                        f"--env={run_info['path_env']}",
                        f"--run-id={run_name}",
                        f"--num-envs={run_info['userconfig']['num_env']}",
                        f"--base-port={run_info['base_port']}",
                        "--torch-device",
                        "cpu",
                        "--force",
                    ],
                    shell=True,
                    check=True,
                )
            except subprocess.SubprocessError as err:
                run_info["error"] = True
                run_info["error_msg"] = str(err)

        # If we do not use a build environment, interaction with the unity editor is needed.
        else:
            # Run mlagents with the unity editor.
            try:
                subprocess.run(
                    [
                        "mlagents-learn",
                        f"{Path(path_to_temp_config_file).absolute()}",
                        f"--run-id={run_name}",
                        "--torch-device",
                        "cpu",
                        "--force",
                    ],
                    shell=True,
                    check=True,
                )
            except subprocess.SubprocessError as err:
                run_info["error"] = True
                run_info["error_msg"] = str(err)

        # Update the dict containing run infos.
        run_info["duration"] = time.time() - start_time
        run_info["return_code"] = return_code
        # ToDo: Can be removed.
        run_info["run_name"] = run_name

    return run_info


def check_userconfig():
    """Check userconfig in the configuration yaml file for content and datatypes."""
    if not "userconfig" in config:
        raise ValueError
    if not "build" in config["userconfig"] and not isinstance(config["userconfig"], bool):
        raise ValueError
    if not "production" in config["userconfig"] and not isinstance(
        config["userconfig"]["production"], bool
    ):
        raise ValueError
    if not "summary" in config["userconfig"] and not isinstance(
        config["userconfig"]["summary"], bool
    ):
        raise ValueError
    if not "num_env" in config["userconfig"] and not isinstance(
        config["userconfig"]["num_env"], bool
    ):
        raise ValueError
    if not "num_process" in config["userconfig"] and not isinstance(
        config["userconfig"]["num_process"], int
    ):
        raise ValueError
    if "message" in config["userconfig"] and not isinstance(config["userconfig"]["message"], str):
        raise ValueError
    return


def update_and_clean_summary(summary_list: list[dict]) -> dict:
    """Update and clean the summary information. Creates pure dict from list of dicts.
    Some information not neeeded or can not be stored as a json.
    Get mean reward of last entries for all runs and save."""
    summary_dict: dict = {}
    for entry in summary_list:
        if not entry["error"]:
            # Store mean reward over the last entries to the run.
            entry["last_cumulative_reward"] = get_mean_reward(entry["run_name"])
        else:
            entry["last_cumulative_reward"] = float("-inf")
        # Base config to much information in the summary.
        entry.pop("base_config")

        # Path not storeabe as json.
        entry.pop("path_env")

        summary_dict[entry["run_id"]] = entry
    return summary_dict


def create_summary_file(summary_list: list[dict]):
    """Save sorted summary file."""
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


if __name__ == "__main__":
    # Paths: Config files and unity env.
    path_to_working_dir = (
        "C:/Users/max.muehlefeldt/Documents/GitHub/unity-machine-learning/python/basic_rl_env"
    )
    path_to_config_file = "./hyperparameter_search.yaml"
    path_to_unity_env = "./build"
    path_to_log_dir = "./logs"

    # Ensure correct working dir.
    if os.getcwd() != Path(path_to_working_dir).absolute():
        os.chdir(Path(path_to_working_dir).absolute())

    # Open the base config file.
    with open(Path(path_to_config_file).absolute(), mode="r", encoding="utf8") as config_file:
        config = yaml.safe_load(config_file)

    check_userconfig()
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

    # Get the id of the first run. Used for logging and summary.
    ID_FIRST_RUN = get_run_id()

    # Logging config.
    if production:
        logging.basicConfig(
            filename=Path(f"./logs/{ID_FIRST_RUN}_search.log").absolute(),
            level=logging.INFO,
        )

    # Log the message from the config file.
    if production and message_for_log is not None:
        logging.info("Note: %s", message_for_log)

    hyperparamters = {"hyperparameters": config["behaviors"]["RollerAgent"]["hyperparameters"]}
    network = {"network_settings": config["behaviors"]["RollerAgent"]["network_settings"]}

    # In case memory is configured in yaml file:
    # Handle memory options seperate from network settings.
    memory_comb = [()]
    memory = {"memory": {}}
    if "memory" in network["network_settings"]:
        memory = {"memory": config["behaviors"]["RollerAgent"]["network_settings"]["memory"]}

    dynamic_parameters = get_dynamic_parameters([hyperparamters, network, memory])
    combinations = get_parameter_combinations(dynamic_parameters)

    # Get the number of runs the current config is goint to produce.
    num_count = len(combinations)
    if production:
        logging.info("%i runs are going to be started.", num_count)

    if not userconfig["build"]:
        summary = list(map(commence_mlagents_run, combinations))
    else:
        # Perform actual calculations using ml-agents distributed over a number of processes.
        with Pool(userconfig["num_process"]) as p:
            summary = p.map(commence_mlagents_run, combinations)

    if generate_summary:
        create_summary_file(summary)

    """
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
                logging.info(
                    f"[{run_id}] Expected end time of all runs: {time.strftime('%d %b %Y %H:%M:%S', time.localtime(time.time() + duration))}."
                )
        """
