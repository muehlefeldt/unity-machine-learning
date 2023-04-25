import yaml
from pathlib import Path
import os
import logging



# Paths: Config files and unity env.
path_to_working_dir = "python/basic_rl_env"
path_to_config_file = "./config/rolleragent_ppo.yaml"
path_to_temp_config_file = "./config/current.yaml"
path_to_unity_env = "./build"

# Ensure correct working dir.
if (os.getcwd() != Path(path_to_working_dir).absolute()):
    os.chdir(Path(path_to_working_dir).absolute())

def get_run_id() -> int:
    dir_content = os.listdir(Path("./results/").absolute())
    numbers = []
    for entry in dir_content:
        numbers.append(int(entry.split("_")[0]))
    return max(numbers) + 1

logging.basicConfig(filename=f"{get_run_id()}_search.log", level=logging.INFO)

# Open the base config file.
with open(Path(path_to_config_file).absolute(), mode="r") as config_file:
    config = yaml.safe_load(config_file)

# Select parameter to modify in base config.
para = {
    "beta": [
        1e-4,
        #5e-4,
        #1e-3,
        #5e-3,
        #1e-2
    ],
    "lambd": [
        #0.85,
        #0.9,
        0.925,
        #0.95
    ]
}

for beta_value in para["beta"]:
    # Reload the base config. Modify selected parameter.
    tmp_config = config
    tmp_config['behaviors']['RollerAgent']['hyperparameters']["beta"] = beta_value

    for lambd_value in para["lambd"]:
        tmp_config['behaviors']['RollerAgent']['hyperparameters']["lambd"] = lambd_value

        # Id number of the run. As shown in tensorboard. Needed to ensure traceability.
        run_id_num = get_run_id()

        logging.info(f"[{get_run_id()}] New run started with id {get_run_id()}.")
        logging.info(f"[{get_run_id()}] beta = {beta_value}.")
        logging.info(f"[{get_run_id()}] lambd = {lambd_value}.")

        # Reload the base config. Modify selected parameter.
        #tmp_config = config
        #tmp_config['behaviors']['RollerAgent']['hyperparameters'][parameter_to_modify] = value

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
            logging.warning(f"[{get_run_id()}] error code.")

        logging.info(f"[{get_run_id()}] return code = {return_code}.")
