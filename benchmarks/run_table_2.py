from util import *


# =============================================================================================================
# ======================================= RANDOM \forall^1\exists^* ===========================================
# =============================================================================================================

def gen_random_hyperltl (number_of_formulas, max_depth, number_exists):
    aps = ['a', 'b', 'c', 'd', 'e']

    exists_trace_vars = []
    forall_trace_vars = ['pi']

    for i in range(number_exists):
        exists_trace_vars.append('qi' + 'i' * i)

    formulas = []

    for _ in range(number_of_formulas):
        formulas.append(get_random_ltl(aps, exists_trace_vars + forall_trace_vars, max_depth))
   
    prefix = ' '.join(map(lambda x: 'forall ' + x + '. ', forall_trace_vars)) + ' '.join(map(lambda x: 'exists ' + x + '. ', exists_trace_vars))

    remapped_formulas = list(map(lambda x: prefix + x, formulas))

    return remapped_formulas


def run_random():
    timeout = 10
    exists_counts = [1, 3, 5, 7, 9]

    solver = ['hypersat_vampire']

    for m in exists_counts:

        print ('m={}: '.format(m))

        formulas = gen_random_hyperltl(number_of_formulas=20,max_depth=4, number_exists=m)

        results = dict()

        for s in solver:
            results[s] = []

        for f in formulas:
            for s in solver:
                r = call_solver(s, f, timeout=timeout)
                
                results[s].append(r)

                if r == None:
                    print('x', end='', flush=True)
                else:
                    print('|', end='', flush=True)

            print(' # ', end='', flush=True)

        print('')

        for s in solver:
            res = results[s]
            solved = list(filter(lambda x: x != None, res))
            percentage = len(solved) * 100 / len(res)
            # Compute the average and maximum time used by HyperSAT on all the formulas in the given template
            time_sum = sum(map(lambda x: x['time'], solved))

            avg_time = time_sum / len(solved) if len(solved) != 0.0 else 0.0

            print ('{}: {:2.1f}%, {:2.5f}s'.format(s, percentage, avg_time)) 

        print ('')

    print('\n')


print('Running the experiments in Table 2. Each \'|\' denotes a successful run of HyperSAT and \'x\' denotes a timeout. After each run of experiments, we display the success rate and average runtime of HyperSAT \n')

run_random()


