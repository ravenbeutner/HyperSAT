# Benchmarks For HyperSAT

This folder contains Python scripts to evaluate HyperSAT on a range of challenging benchmarks. 
As this repository contains only HyperSAT (and none of the other HyperLTL tools), we have adjusted the evaluation scripts such that only the HyperSAT results are reproduced. 
A full evaluation including the other tools, will be submitted to the artifact evaluation. 

## Setup
We assume that HyperSAT has been built and the executable `app/HyperSAT` exists.
Moreover, the _Vampire_ FOL solver should be installed and connected to HyperSAT. 
See the main `README.md` for instructions on how to build and set up HyperSAT.
To run the experiments, the current directory should be `benchmarks/`. 


## Run Experiments 

To reproduce the results in Table 1, Table 2, Table 3a, Table 3b, Table 4a, and Table 4b, run 

```
python3 run_table_1.py
```
```
python3 run_table_2.py
```
```
python3 run_table_3_a.py
```
```
python3 run_table_3_b.py
```
```
python3 run_table_4_a.py
```
```
python3 run_table_4_b.py
```

respectively. 
Each script generates the HyperLTL formulas, calls HyperSAT, and displays the result and runtime.

