# Installation and usage
This repository is used with the following software. Unity 2021.3.11f1, ML-Agents Release 20 and Python 3.9. Changes to software versions are difficult. Consult especially the documentation to ML-Agents. The python scripts were run using VS Code under Windows 10.

## Installation
* Install Python 3.9.13 (last 3.9.x version provided as windows installer). [Download](https://www.python.org/ftp/python/3.9.13/python-3.9.13-amd64.exe)
* (Windows) Powershell script execution policy needs to be set:
```ps
Set-ExecutionPolicy -scope CurrentUser -ExecutionPolicy RemoteSigned
```
* (Windows) Check execution policy:
```ps
Get-ExecutionPolicy -List
```
* Create a virtual environment based on the `requirements.txt` file in the root folder.
* Activate the virtual environment.
* Test the ML-Agents installation. Run in the venv:
```sh
mlagents-learn --help
```

## Basic usage
* Start the training process in the venv:
```sh
mlagents-learn config/rollerball_config.yaml --run-id=RunId --torch-device cpu
```
* Monitor the training process:
```sh
tensorboard --logdir results
```

## Use the provided script
This repository provides a main script in `python\basic_rl_env\hyperparameter_search.py`. This script has a number features but is basicly used to run any training. The config file `python\basic_rl_env\hyperparameter_search.yaml` is used to configure the training parameters. The config file is extensivly commented. The main section of the file:

* userconfig
  * Configures the script behaviour. During default operations all bool variables should be true.
  * The use of multiple environments (num_env) is discouraged.
  * The use of multiple processes is also problematic. The code to change this variable is still present but due to developed Unity config files problems arose. Do not change!
  * execution_order_not_random: This variable is a crute option to search large ranges of hyperparameter permutations. As the name suggests, this is only a random execution of possible configurations.

* env_config
  * Most useful achievment of this thesis.
  * Configure the unity enviroment via these variables.
  * A JSON file is created and the file is loaded by the Unity engine at startup.
  * Based on this code any number of functions and variables can be changed.
  * Future work in this field might use this approach to test diffrent reward functions, etc.
  * To run multiple configurations give values as a list. 
    * Example: sensorCount: [1, 2, 4, 8, 10, 16, 32, 64, 128]
    * This was used to conduct the sensor count studies.
    * Also useful for maxStep and stepPenalty variables.

* paths
  * Paths used by the script.
  * Update these to your systems specifications if needed.

* behaviors
  * Basic ML-Agents configuration.
  * ALL variables can be given as list to automatically run all possible permutations.