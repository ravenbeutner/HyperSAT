import subprocess
from subprocess import TimeoutExpired
import os
import sys
import time
import random

random.seed(0)

hypersat_path = '../app/HyperSAT'

# Executes a command and returns the exit-code, stdout, and stderr. 
# Return None if a timeout occurred
def system_call(cmd : str, timeout_sec=None):
    proc = subprocess.Popen(cmd, shell=True, stderr=subprocess.PIPE, stdout=subprocess.PIPE)

    try:
        stdout, stderr = proc.communicate(timeout=timeout_sec)
    except TimeoutExpired:
        proc.kill()
        return None, "", ""
   
    return proc.returncode, stdout.decode("utf-8").strip(), stderr.decode("utf-8").strip()

# Call HyperSAT on a system and formula with a specified timeout
def call_hypersat(formula : str, solver : str, timeout : int):
    # The fixed file we use to write the instance
    fileName = './formula.hltl'
    os.makedirs(os.path.dirname(fileName), exist_ok=True)

    # Write the instance_str to the fixed file
    with open(fileName, "w") as file:
        file.write(formula)

    args = [
        hypersat_path,
        '--' + solver, 
        '--tptp',
        fileName
    ]

    startTime = time.time()
    (code, out, err) = system_call(' '.join(args), timeout_sec=timeout)
    endTime = time.time()
    et = endTime - startTime 

    if code == None:
        return None
    
    if code != 0 or err != "": 
        print ("Error by HyperSAT: ", err, out, err, file=sys.stderr)
        return None
    
    if out == 'UNSAT':
        res = False 
    elif out == 'SAT':
        res = True
    else:
        print ("Unexpected output by HyperSAT: ", err, out, err, file=sys.stderr)
        return None

    return {'time': et, 'res': res}

def get_random_ltl(aps, trace_var_list, max_size : int):
    unary_operators = ['X', 'G']
    binary_operators = ['W', '&', '|', '->', '<->']

    def gen_rec (bound : int):
        if (bound >= max_size or random.random() <= 0.5):
            ap_index = random.randint(0, len(aps) - 1)
            trace_index = random.randint(0, len(trace_var_list) - 1)
            if random.random() <= 0.7 : 
                # Add negation
                return '! "' + aps[ap_index] + '"_' + trace_var_list[trace_index] + ' '
            else:
                return '"' + aps[ap_index] + '"_' + trace_var_list[trace_index] + ' '
        else:
            if random.random() <= 0.6:
                # Unary operator
                opp_index = random.randint(0, len(unary_operators) - 1)
                return '(' + unary_operators[opp_index] + ' ' + gen_rec(bound+1) + ')'
            else: 
                opp_index = random.randint(0, len(binary_operators) - 1)
                return '(' + gen_rec(bound+1) + ' ' + binary_operators[opp_index] + ' ' + gen_rec(bound+1) + ')'

    f = gen_rec(0)

    return f

def get_random_prop(aps, trace_var_list, max_size : int):
    unary_operators = ['!']
    binary_operators = ['&', '|', '->', '<->']

    def gen_rec (bound : int):
        if (bound >= max_size or random.random() <= 0.7):
            ap_index = random.randint(0, len(aps) - 1)
            trace_index = random.randint(0, len(trace_var_list) - 1)
            if random.random() <= 0.7 : 
                # Add negation
                return '! "' + aps[ap_index] + '"_' + trace_var_list[trace_index] + ' '
            else:
                return '"' + aps[ap_index] + '"_' + trace_var_list[trace_index] + ' '
        else:
            if random.random() <= 0.6:
                # Unary operator
                opp_index = random.randint(0, len(unary_operators) - 1)
                return '(' + unary_operators[opp_index] + ' ' + gen_rec(bound+1) + ')'
            else: 
                opp_index = random.randint(0, len(binary_operators) - 1)
                return '(' + gen_rec(bound+1) + ' ' + binary_operators[opp_index] + ' ' + gen_rec(bound+1) + ')'

    f = gen_rec(0)

    return f

def call_solver(s, f, timeout):
    if s == 'hypersat_vampire':
        return call_hypersat(formula=f, solver='vampire', timeout=timeout)
    if s == 'hypersat_paradox':
        return call_hypersat(formula=f, solver='paradox', timeout=timeout)
    else: 
        raise Exception('Unsupported solver: {}'.format(s))


def bounded_globally (f : str, n : int): 
    if n <= 0:
        return '1'
    elif n == 1:
        return f 
    else:
        return f + ' & ' + '(X ' + bounded_globally(f, n-1) + ')'

def bounded_eventually (f : str, n : int): 
    if n <= 0:
        return '0'
    elif n == 1:
        return f 
    else:
        return '(' + f + ' | ' + '(X ' + bounded_eventually(f, n-1) + ')' + ')'
    

def bounded_w (f1 : str, f2 : str, n : int): 
    if n <= 0:
        return '1'
    elif n == 1:
        return f2
    else:
        return f2 + ' | ' + '(' + f1 +  ' & X(' + bounded_w(f1, f2, n-1) + '))' 
    