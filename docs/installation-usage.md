# Installation and usage
This repository is used with the following software. Unity 2021.3.11f1, ML-Agents Release 20 and Python 3.9. The versions of the software was determined to work as expected.

## Installation


## Usage


# Prepare ML-Agents Release 20

## Python 3.9
* Install Python 3.9.13 (last 3.9.x version provided as windows installer). [Download](https://www.python.org/ftp/python/3.9.13/python-3.9.13-amd64.exe)
* Create virtual environment (venv) VS Code: `CTRL+Shift+P` and `Python: Create environemnt`.
* Activate virtual environment.
* (Windows) Powershell script execution policy needs to be set:
```ps
Set-ExecutionPolicy -scope CurrentUser -ExecutionPolicy RemoteSigned
```
* Check execution policy:
```ps
Get-ExecutionPolicy -List
```
* Install PyTorch in the venv:
```sh
pip3 install torch~=1.7.1 -f https://download.pytorch.org/whl/torch_stable.html
```
* Install Python mlagents package in the venv:
```sh
pip install mlagents==0.30.0
```
* Test mlagents installation. Run in the venv:
```sh
mlagents-learn --help
```

## Learm with ML-Agents
* Start the training process in the venv:
```sh
mlagents-learn config/rollerball_config.yaml --run-id=RunId --torch-device cpu
```
* Monitor the training process:
```sh
tensorboard --logdir results
```

