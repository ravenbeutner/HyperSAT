from util import *

# ==========================================================================================
# ======================================= Examples =========================================
# ==========================================================================================

formulas = [
        {
            'name': 'gni -> ni_+', 
            'formula': 'exists ti. forall pi. forall pii. exists piii. exists qi. forall qii.  ("l"_pi <-> "l"_piii) & ("o"_pi <-> "o"_piii) & ("h"_pii <-> "h"_piii) & (! (("l"_qi <-> "l"_qii) & ("o"_qi <-> "o"_qii) & !"h"_qii)) & ! "h"_ti'
        }, 
        {
            'name': 'GniLeak', 
            'formula': 'forall pi. forall pii. exists piii. forall qi.  G ("l"_pi <-> "l"_piii) & G ("o"_pi <-> "o"_piii) & G ("h"_pii <-> "h"_piii) & G ("h"_qi <-> "o"_qi)'
        }, 
        {
            'name': 'GniLeak2', 
            'formula': 'forall pi. forall pii. exists piii. forall qi. exists ti. exists tii.   ("l"_pi <-> "l"_piii) & ("o"_pi <-> "o"_piii) & ("h"_pii <-> "h"_piii) & G ("h"_qi <-> "o"_qi) & ("h"_ti <-> ! "h"_tii)'
        }, 
        {
            'name': 'NiLeak2', 
            'formula': 'forall pi. exists pii. forall qi. exists ti. exists tii.  G ("l"_pi <-> "l"_pii) & G ("o"_pi <-> "o"_pii) & G (! "h"_pii) & G ("h"_qi <-> "o"_qi) & ("h"_ti <-> ! "h"_tii)'
        },
        {
            'name': 'k-anonymity-od', 
            'formula': 'forall pi. exists qi. exists qii. forall ti. forall tii. (G("l"_pi <-> "l"_qi) & G("l"_pi <-> "l"_qii) & ! ("o"_qi <-> "o"_qii)) & G ("o"_ti <-> "o"_tii)'
        },
        {
            'name': 'k-anonymity-func', 
            'formula': 'forall pi. exists qi. exists qii. forall ti. (G("l"_pi <-> "l"_qi) & G("l"_pi <-> "l"_qii) & ! ("o"_qi <-> "o"_qii)) & G ("l"_ti <-> "o"_ti)'
        },
    ]

def run_instance():
    timeout = 10
    solver_list = ['hypersat_vampire']

    for f in formulas: 

        print(f['name'], ':')

        for s in solver_list: 
            print('{}: '.format(s), end='')
            res = call_solver(s, f['formula'], timeout=timeout)

            if res != None:
                if res['res']:
                    print (' SAT, {:2.5f}'.format(res['time']))
                else:
                    print (' UNSAT, {:2.5f}'.format(res['time']))
            else:
                print (' {0: <7}'.format('TO'))

        print ('')


run_instance()
