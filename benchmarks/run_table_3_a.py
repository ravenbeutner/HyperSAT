from util import *


# ==========================================================================================
# ============================================== Enforce ===================================
# ==========================================================================================

def enforce_n (n : int, bound : int): 
    traces = []

    for i in range(n):
        traces.append('pi' + 'i' * i)

    f = []

    for i in range(n):
        for j in range(n): 
            if i != j:
                body = '! ("a"_' + traces[i] + ' <-> ' + '"a"_' + traces[j] + ' )'
                f.append(bounded_eventually(body, bound))

    if n == 1:
        f = ['1']

    prefix = ' '.join(map(lambda x: 'exists ' + x + '.', traces))
    body = ' & '.join(f)

    return 'forall t. ' + prefix + body


def run_enforce():
    timeout = 60
    solvers = ['hypersat_vampire']

    n_list = range(1, 6)

    bound_list = range(1, 3)

    for bound in bound_list:
        for n in n_list:
            print('n={}, b={}: '.format(n, bound))

            f = enforce_n(n, bound)

            for s in solvers: 
                print('{}: '.format(s), end='')
                res = call_solver(s, f, timeout=timeout)

                if res != None:
                    if res['res']:
                        print (' SAT, {:2.5f}'.format(res['time']))
                    else:
                        print (' UNSAT, {:2.5f}'.format(res['time']))
                else:
                    print (' {0: <7}'.format('TO'))

            print('')
        print ('')

run_enforce()

