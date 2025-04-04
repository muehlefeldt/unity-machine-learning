""" This script creates a pickle file from the ML-Agents result files for further use.
"""

import json
import os
import pickle
from pathlib import Path

import numpy as np
import yaml
from tensorboard.backend.event_processing.event_file_loader import EventFileLoader


def get_run_path_dict(paths_dict: dict) -> dict:
    """Generate a dictionary where the keys are run numbers and the values are
    dictionaries with paths to different files related to that run.
    """
    result = {}
    # dir_contents: list = []
    for key in paths_dict:
        if key != "working_dir":
            dir_contents = os.listdir(paths_dict[key])

            # numbers = []
            # rest_of_file_name: str = ""
            for entry in dir_contents:
                number = 0
                if "_" in entry:
                    number = int(entry.split("_")[0])
                    # rest_of_file_name = str.join("_", entry.split("_")[1:])

                else:
                    number = int(entry.split(".")[0])
                    # rest_of_file_name = str.join(".", entry.split(".")[1:])

                if number not in result:
                    result[number] = {}

                result[number][key] = paths_dict[key] / f"{entry}"

    return result


def get_file_contents(specific_run_dict: dict) -> dict:
    """Retrieve the contents of various configuration and statistics files for a specific run."""
    file_contents = {}
    if "stats_dir" in specific_run_dict:
        with open(specific_run_dict["stats_dir"]) as json_file:
            # results["stats"] = json.load(json_file)
            file_contents["stats"] = json.load(json_file)

    if "unity_configs_dir" in specific_run_dict:
        with open(specific_run_dict["unity_configs_dir"]) as json_file:
            # results["stats"] = json.load(json_file)
            file_contents["unity_config"] = json.load(json_file)

    if "configs_dir" in specific_run_dict:
        with open(specific_run_dict["configs_dir"]) as yaml_file:
            # results["stats"] = json.load(json_file)
            file_contents["training_config"] = yaml.safe_load(yaml_file)

    return file_contents


def get_result_data(run_dict: dict, id: int) -> dict:
    """Extract data from the result files for a specific run and compile it into a dictionary."""
    if "results_dir" in run_dict[id]:
        path = run_dict[id]["results_dir"]
    elif "results_archive_dir" in run_dict[id]:
        path = run_dict[id]["results_archive_dir"]
    else:
        raise KeyError

    # This dict structure is useful to reference when loading the pickle file at a later stage.
    data = {
        "env": {"cumulative_rewards": [], "ep_length": [], "target_reached": []},
        "door": {"bad_passage": [], "good_passage": [], "passage": []},
        "collision": {"initial": [], "stay": [], "total": []},
        "steps": [],
        "summary": {
            "mean_reward": 0,
            "std_mean_reward": 0,
            "episode_length": 0,
            "normalised_reward": 0,
        },
        "file_contents": get_file_contents(run_dict[id]),
    }

    # Get the tfevents file associated with the current run.
    path_to_result_folder = path / "RollerAgent/"
    try:
        path_to_result = sorted(Path(path_to_result_folder).glob("events.out.tfevents.*"))[0]
    except:
        return {}

    # Using tensorflow to access the tfevents data.
    # datarecord = EventFileLoader(str(path_to_result)).Load()
    for event in EventFileLoader(str(path_to_result)).Load():
        # event = event_pb2.Event.FromString(batch.numpy())
        for value in event.summary.value:
            if value.tag == "Environment/Cumulative Reward":
                data["env"]["cumulative_rewards"].append(value.tensor.float_val[0])
            elif value.tag == "Environment/Episode Length":
                data["env"]["ep_length"].append(value.tensor.float_val[0])
            elif value.tag == "Door/Bad passage":
                data["door"]["bad_passage"].append(value.tensor.float_val[0])
            elif value.tag == "Door/Good passage":
                data["door"]["good_passage"].append(value.tensor.float_val[0])
            elif value.tag == "Door/Passage":
                data["door"]["passage"].append(value.tensor.float_val[0])
            elif value.tag == "Collision/Initial":
                data["collision"]["initial"].append(value.tensor.float_val[0])
            elif value.tag == "Collision/Stay":
                data["collision"]["stay"].append(value.tensor.float_val[0])
            elif value.tag == "Collision/Total":
                data["collision"]["total"].append(value.tensor.float_val[0])
            elif value.tag == "Target Reached":
                data["env"]["target_reached"].append(value.tensor.float_val[0])

            if len(data["steps"]) == 0:
                data["steps"].append(event.step)
            elif data["steps"][-1] != event.step:
                data["steps"].append(event.step)

    # The last recorded rewards to be used as basis to gether some basic stats about the run.
    rewards_of_interest = data["env"]["cumulative_rewards"][-100:]
    passages_of_interest = data["door"]["passage"][-100:]

    data["final_mean_reward"] = np.mean(rewards_of_interest)
    data["final_std_reward"] = np.round(np.std(rewards_of_interest), 5)
    data["final_mean_episode_length"] = np.mean(data["env"]["ep_length"][-100:])
    data["final_mean_door_passage"] = np.mean(passages_of_interest)
    data["final_std_door_passage"] = np.round(np.std(passages_of_interest), 5)
    # data["normalised_reward"] = np.mean(rewards_of_interest) / unity["maxStep"]

    return data


if __name__ == "__main__":
    # Ensure correct working dir.
    if os.getcwd() != Path("./python/basic_rl_env").absolute():
        os.chdir(Path("./python/basic_rl_env").absolute())

    # Define paths to dirs.
    paths = {
        "working_dir": "C:/Users/max.muehlefeldt/Documents/GitHub/unity-machine-learning/python/basic_rl_env",
        "results_dir": "results/",
        "results_archive_dir": "results_archive/",
        "stats_dir": "stats/",
        "summaries_dir": "summaries/",
        "configs_dir": "configs/",
        "unity_configs_dir": "unity_configs/",
    }

    # Absolute path set for working dir.
    paths["working_dir"] = Path(paths["working_dir"]).absolute()

    # Update paths to be absolute.
    for key in paths:
        if key != "working_dir":
            paths[key] = paths["working_dir"] / paths[key]

    # Define run id range to be part of the created dict.
    from_run = 6000
    to_run = 7000

    run_path_dict: dict = get_run_path_dict(paths)

    # Get the selected ids.
    selected_ids = [x for x in run_path_dict.keys() if from_run <= x <= to_run]

    # Get the data from the requested runs.
    summary_dict = {}
    for id in selected_ids:
        print(f"Getting summary for {id}.")
        try:
            summary_dict[id] = get_result_data(run_path_dict, id)
        except KeyError:
            print(f"Error in ID {id}.")

    # Define the path for the summary pickle file. Remove existing file.
    summary_file_path: Path = Path("summary_dict.pickle").absolute()
    if summary_file_path.exists():
        print("Removed old summary file.")
        os.remove(summary_file_path)

    # Create the pickle file with summary dict.
    with open(summary_file_path, mode="wb") as file:
        print("Created new summary file.")
        pickle.dump(summary_dict, file)
