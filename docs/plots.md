# Plots

* Use python script `python\basic_rl_env\results_summary_pickle.py` to prepare the training data. 
* A pickle file containing a dict is created to simplify access to the plots.
* You need to specify in the script the training run ids to be coverd by the created pickle file. Examples:
  * from_run = 6000
  * to_run = 6465
* The script may take quite some time. Some very long training runs may be covered by the script.
* Errors are usally without consequence. Usually aborted training runs without data cause these errors.
* The the Jupyter Notebook `python\notebooks\plot.ipynb` to create plots of the data using the created pickle file.
