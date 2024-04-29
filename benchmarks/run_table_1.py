from util import *

# =================================================================================================
# ======================================= QN Implications =========================================
# =================================================================================================

def construct_qn(traceVars):
    same_input = []

    for pi in traceVars:
        same_input.append('("i"_' + pi + ' <-> ' + '"i"_' + traceVars[0] + ' )')

    differnt_output = []

    for pi in traceVars:
        for pii in traceVars:
            if pi != pii:
                differnt_output.append('! ("o"_' + pi + ' <-> ' + '"o"_' + pii + ' )')

    if len(differnt_output) == 0:
        differnt_output.append('1')

    return '!(' + ' & '.join(same_input) + ' & ' + ' & '.join(differnt_output) + ')'


# Check if QN(n) -> QN(m), i.e., QN(n) ^ ! QN(m) is UNSAT
def implication_qn(n, m):
    pi_traces = []

    for i in range(n): 
        pi_traces.append('pi' + i * 'i')

    pii_traces = []
    
    for i in range(m): 
        pii_traces.append('qi' + i * 'i')

    exists_prefix = ' '.join(map(lambda x: 'exists ' + x + '.',pii_traces))
    forall_prefix = ' '.join(map(lambda x: 'forall ' + x + '.',pi_traces))

    return exists_prefix + forall_prefix + '(' + construct_qn(pi_traces) + ') & !(' + construct_qn(pii_traces) + ')' 


def run_qn():
    timeout = 60
    sizes = [1, 2, 3, 4, 5, 6, 7]
    solver_list = ['hypersat_vampire']

    for n in sizes:
        print ('{}  |'.format(n), end='', flush=True)

        for m in sizes:
            f = implication_qn(n, m)

            for s in solver_list:
                r = call_solver(s, f=f, timeout=timeout)

                if r == None:
                    print (' TO ', end='', flush=True)
                else:
                    print (' {:2.5f} '.format(r['time']), end='', flush=True)

            print (' |', end='', flush=True)

        print('')
     
run_qn()

