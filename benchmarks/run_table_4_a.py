from util import *

# =======================================================================================================
# ======================================= GNI - NI Implications =========================================
# =======================================================================================================

def gni (p1, p2, p3, bound : int): 
    c1 = bounded_globally('("l"_' + p1 + ' <-> "l"_' + p3 + ')', bound)
    c2 = bounded_globally('("o"_' + p1 + ' <-> "o"_' + p3 + ')', bound)
    c3 = bounded_globally('("h"_' + p2 + ' <-> "h"_' + p3 + ')', bound)

    return '(' + c1 + ') & (' + c2 + ') & (' + c3 + ')'

def ni (p1, p2, bound : int): 
    c1 = bounded_globally('("l"_' + p1 + ' <-> "l"_' + p2 + ')', bound)
    c2 = bounded_globally('("o"_' + p1 + ' <-> "o"_' + p2 + ')', bound)
    c3 = bounded_globally('(! "h"_' + p2 + ')', bound)

    return '(' + c1 + ') & (' + c2 + ') & (' + c3 + ')'

def gni_implies_ni(bound : int):
    f1 = gni('pi', 'pii', 'piii', bound)
    f2 = ni('qi', 'qii', bound)

    return 'forall pi. forall pii. exists piii. exists qi. forall qii.  ' + f1 + ' & !( ' + f2 + ')'

def ni_implies_gni(bound : int):
    f1 = ni('qi', 'qii', bound)
    f2 = gni('pi', 'pii', 'piii', bound)

    return 'exists pi. exists pii. forall piii. forall qi. exists qii.  ' + f1 + ' & !( ' + f2 + ')'

def gni_ni_exp():
    timeout = 60
    bounds = [1, 2, 3, 4, 5, 6]

    solver_list = ['hypersat_vampire']

    print('========= GNI -> NI =========')

    for bound in bounds:
        print ('b={}:'.format(bound))

        f = gni_implies_ni(bound)

        for s in solver_list:
            print('{}: '.format(s), end='')

            res = call_solver(s, f, timeout=timeout)

            if res != None:
                if res['res']:
                    print (' SAT, {:2.5f}'.format(res['time']))
                else:
                    print (' UNSAT, {:2.5f}'.format(res['time']))
            else:
                print ('{0: <7}'.format('TO'))

        print('')

    print('\n')

    print('========= NI -> GNI =========')

    for bound in bounds:
        print ('b={}:'.format(bound))

        f = ni_implies_gni(bound)

        for s in solver_list:
            print('{}: '.format(s), end='')

            res = call_solver(s, f, timeout=timeout)

            if res != None:
                if res['res']:
                    print (' SAT, {:2.5f}'.format(res['time']))
                else:
                    print (' UNSAT, {:2.5f}'.format(res['time']))
            else:
                print ('{0: <7}'.format('TO'))

        print('')


gni_ni_exp()
