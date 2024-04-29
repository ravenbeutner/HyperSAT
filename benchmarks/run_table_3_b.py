from util import *

# ==========================================================================================
# ========================================= Unsat ==========================================
# ==========================================================================================

def unsat_n (n : int): 
    f1 = '"a"_piii '

    f2 = '(G ("a"_pi -> X "a"_pii ))'

    f3 = '(' + ('X ' * n) + 'G ! "a"_pi ' + ')'

    return 'forall pi. exists pii. exists piii. ' + f1 + ' & ' + f2 + ' & ' + f3

def run_unsat():
    timeout = 10
    solvers = ['hypersat_vampire']

    n_list = range(0, 6)

    for n in n_list:
        print('n={}:'.format(n))
        
        f = unsat_n(n)

        for s in solvers: 
            print('{}: '.format(s), end='')
            res = call_solver(s, f, timeout)
            
            if res != None:
                if res['res']:
                    print (' SAT, {:2.5f}'.format(res['time']))
                else:
                    print (' UNSAT, {:2.5f}'.format(res['time']))
            else:
                print (' {0: <7}'.format('TO'))

        print ('')



run_unsat()
