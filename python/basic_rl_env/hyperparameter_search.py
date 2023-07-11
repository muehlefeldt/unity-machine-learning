import itertools
import json
import logging
import os
import time
import shutil
from collections import OrderedDict
from multiprocessing import Pool
from pathlib import Path
from statistics import mean

import tensorflow as tf
import yaml
from tensorflow.core.util import event_pb2
from tensorflow.data import TFRecordDataset


def get_run_id() -> int:
    """Get the run id (number) based on past runs in the result folder.
    If called multiple times, the result will be an increased number."""
    dir_content = os.listdir(Path("./results/").absolute())
    numbers = []
    for entry in dir_content:
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
                    new.append({list(section.keys())[0]: {key: entry}})
                key_values.append(new)

    return key_values


def get_parameter_combinations(parameters: list[list]) -> list[dict]:
    """Get the parameter combinations with associated run id."""

    # Generate the possible combinations of parameter values.
    possile_combinations = list(itertools.product(*parameters))

    # Create final list with format: Id: Parameter combination.
    first_id = get_run_id()
    id_possible_combinations = [
        {
            "run_id": first_id + possile_combinations.index(x),
            "parameters": x,
            "userconfig": userconfig,
            "base_config": config,
            "path_env": Path(path_to_unity_env).absolute(),
            "path_copy_env": Path(f"./{first_id + possile_combinations.index(x)}_build").absolute()
        }
        for x in possile_combinations
    ]

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
    production = run_info["userconfig"]["production"]

    # Dynamic key value pairs are updated in the tmp dict.
    for entry in para_option:
        # Get the key of the section containing the parameter.
        entry_key = list(entry.keys())[0]

        # Get the key of the parameter.
        para_key = list(entry[entry_key].keys())[0]

        # Get value of the parameter from the current option.
        value = entry[entry_key][para_key]

        # If memory parameter change location to network settings.
        # Otherwise write value to selected section and parameter.
        if entry_key == "memory":
            work_dict["network_settings"][entry_key][para_key] = value
        else:
            work_dict[entry_key][para_key] = value

        if production:
            logging.info("[%i] %s = %s.", id_num, para_key, value)
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
    datarecord = TFRecordDataset(str(path_to_result))
    for batch in datarecord:
        event = event_pb2.Event.FromString(batch.numpy())
        for value in event.summary.value:
            if value.tag == "Environment/Cumulative Reward":
                cumulative_rewards.append(value.simple_value)

    # Return mean of the last 5 recorded cummulative rewards.
    return mean(cumulative_rewards[-5:])


def commence_mlagents_run(run_info: dict):
    # Id number of the run. As shown in tensorboard. Needed to ensure traceability.
    run_id = run_info["run_id"]
    path_to_temp_config_file = f"./configs/{run_id}.yaml"

    production = run_info["userconfig"]["production"]
    use_build_env = run_info["userconfig"]["build"]
    path = run_info["path_env"]
    path_to_copied_build = run_info["path_copy_env"]
    num_env = run_info["userconfig"]["num_env"]

    shutil.copytree(path, path_to_copied_build)

    if production:
        logging.info("[%i] New run started with id %i.", run_id, run_id)

    # Get copy of the base config as loaded.
    #tmp_config = dict.copy(config)
    tmp_config = dict.copy(run_info["base_config"])

    update_parameters_with_option(tmp_config, run_info)

    # Save modified config as yaml file.
    if production:
        with open(Path(path_to_temp_config_file).absolute(), mode="w", encoding="utf8") as new_file:
            yaml.dump(tmp_config, new_file)

    # Execute ml-agents using a compiled environment.
    # Bypass if in test mode.
    return_code = 0
    start_time = time.time()
    run_name = f"{run_id}_basicenv_ppo_auto"
    #if production:
    #    if use_build_env:
    run_name = f"{run_id}_basicenv_ppo_auto"
    # Start ml-algents using build version of unity.
    return_code = os.system(
        f"mlagents-learn \
        {Path(path_to_temp_config_file).absolute()} \
        --env={path} \
        --run-id={run_name}\
        --num-envs={num_env} \
        --base-port={5005 + run_id} \
        --torch-device cpu \
        --force"
    )
    """    else:
            # Start ml-agents in the unity editor. Requires user interaction.
            run_name = f"{run_id}_basicenv_ppo_manual"
            return_code = os.system(
                f"mlagents-learn \
                {Path(path_to_temp_config_file).absolute()} \
                --run-id={run_name} \
                --num-envs={num_env} \
                --torch-device cpu \
                --force"
            )"""
    return


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

    # Variables for control flow.
    use_build_env = False
    production = False
    generate_summary = False

    num_env = 1
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

            if "num_env" in config["userconfig"]:
                if isinstance(config["userconfig"]["num_env"], int):
                    num_env = config["userconfig"]["num_env"]

            # Get the message from the config file to be logged.
            if "message" in config["userconfig"]:
                tmp_message = config["userconfig"]["message"]
                if tmp_message is not None and isinstance(tmp_message, str):
                    message_for_log = tmp_message

        userconfig = config["userconfig"]
        # User config information no longer needed.
        config.pop("userconfig")

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

    # base_config = config["behaviors"]["RollerAgent"]

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

    # Store run durations.
    run_durations = []
    # run_counter = 0

    # Combine all possible options for possible runs.
    # for option in combinations:
    # run_counter += 1
    #    commence_mlagents_run(option)

    with Pool(2) as p:
        p.map(commence_mlagents_run, combinations)

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
        # if generate_summary:
        #    summary_dict[run_id] = {}
        #    for entry in [
        #        *hyperparameter_option,
        #        *network_option,
        #        *memory_option,
        #    ]:
        #        summary_dict[run_id].update(entry)

        #    summary_dict[run_id]["last_cumulative_reward"] = get_mean_reward(run_name)
    """
    # Create a summary file to provide an overview of used parameters and resulting rewards.
    if generate_summary:
        path_to_summary_file = f"./summaries/{ID_FIRST_RUN}.json"
        with open(
            Path(path_to_summary_file).absolute(), mode="w", newline="", encoding="utf8"
        ) as summary_file:
            # Sort the dict created during the runs.
            # The saved file shall be sorted by the highest cummulativ rewards.
            sorted_dict = OrderedDict(
                sorted(summary_dict.items(), key=lambda v: v[1]["last_cumulative_reward"], reverse=True)
            )
            json.dump(sorted_dict, summary_file, indent=4)
    """
