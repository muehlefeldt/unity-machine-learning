import os
from multiprocessing import Pool
from pathlib import Path

import subprocess

TEST = "String"


def f(x) -> dict:
    # Paths: Config files and unity env.
    #path_to_working_dir = "python/basic_rl_env"
    #path_to_config_file = "./hyperparameter_search.yaml"
    #path_to_unity_env = "./build"
    #path_to_log_dir = "./logs"

    # Ensure correct working dir.
    #if os.getcwd() != Path(path_to_working_dir).absolute():
    #    os.chdir(Path(path_to_working_dir).absolute())

    # Open the base config file.
    #with open(Path(path_to_config_file).absolute(), mode="r", encoding="utf8") as config_file:
    #    config = yaml.safe_load(config_file)

    #print(TEST)
    #return {"x": x}
    return subprocess.run()


if __name__ == "__main__":
    #e = []
    with Pool(5) as p:
        e = p.map(f, [1, 2, 3])
    
    print(e)
