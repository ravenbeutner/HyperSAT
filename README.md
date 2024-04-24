# HyperSAT: Satisfiability of Hyperproperties using First-Order Logic

This repository contains HyperSAT - a satisfiability checker for the temporal logic HyperLTL.
HyperLTL expresses temporal hyperproperties, i.e., properties that relate multiple execution traces of a system. 
HyperSAT attempts to determine if _some_ set of traces satisfies a given formula, which can be used to check for unrealizable specifications and test implications between (hyper)properties. 
At its core, HyperSAT takes a HyperLTL formula and computes an _equisatisfiable_ first-order logic (FOL) formula, which can then be solved using off-the-shelf FOL solvers. 

## Structure 

This repository is structured as follows:

- `src/` contains the source code of HyperSAT (written in F#). 
- `app/` is the target folder for the build. The final HyperSAT executable will be placed here.
- `examples/` contains some example HyperLTL formulas.
- `benchmarks/` contains various benchmark scripts.

## Build

This section contains instructions on how to build HyperSAT from sources. 

### Dependencies

To build and run HyperSAT, you need the following dependencies:

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download) (tested with version 8.0.204)
- [spot](https://spot.lrde.epita.fr/) (tested with version 2.11.6)

Install the .NET 8 SDK (see [here](https://dotnet.microsoft.com/en-us/download) for details).
Download and build _spot_ (details can be found [here](https://spot.lrde.epita.fr/)). 
You can place the _spot_ executables in any location of your choosing, and provide HyperSAT with the _absolute_ path (see details below).

Additionally, if you want to _solve_ the FOL queries computed by HyperSAT, you need a FOL solver. 
Currently, HyperSAT supports the following FOL/SMT solvers:

- [Vampire](https://vprover.github.io/) (tested with version 4.6.1)
- [Paradox](https://github.com/nick8325/equinox) (tested with version 4.0)
- [z3](https://github.com/Z3Prover/z3) (tested with version 4.12.3)
- [cvc5](https://cvc5.github.io/) (tested with version 1.0.8)

We recommend using the _Vampire__ solver. 


### Build HyperSAT

To build HyperSAT run the following (when in the main directory of this repository).

```shell
cd src/HyperSAT
dotnet build -c "release" -o ../../app
cd ../..
```

Afterward, the `HyperSAT` executable is located in the `app/` folder.

### Connect Spot and FOL Solvers

HyperSAT requires the _ltl2tgba_ executable from the spot library and, if required, a FOL solver. 
HyperSAT is designed such that it only needs the **absolute** path to these executables, so they can be installed and placed at whatever location fits best.
The absolute paths are specified in a `paths.json` configuration file. 
This file must be located in the *same* directory as the HyperSAT executables (this convention makes it easy to find the config file, independent of the relative path HyperSAT is called from). 
We provide a template file `app/paths.json` that **needs to be modified**. 
After having built _spot_, paste the absolute path to _spot_'s _ltl2tgba_ executable to the `paths.json` file. 
For example, if `/usr/bin/ltl2tgba` is the _ltl2tgba_ executables, the content of `app/paths.json` should be

```json
{
    "ltl2tgba": "/usr/bin/ltl2tgba"
}
```

The same file should also contain the paths to the FOL solvers. 
For example, if you installed the _Vampire_ FOL solver (say, located at `/usr/bin/vampire_rel`), the content of `app/paths.json` should be

```json
{
    "ltl2tgba": "/usr/bin/ltl2tgba",
    "vampire": "/usr/bin/vampire_rel"
}
```

Similarly, to add the _Paradox_, _Z3_, and _CVC5_ solver, use the keys `"paradox"`, `"z3"`, and `"cvc5"`, respectively. 


# Run HyperSAT

After you have built HyperSAT and modified the configuration file you can use HyperSAT by running the following
 
```shell
./app/HyperSAT <options> <instancePath>
```

where `<instancePath>` is the (path to the) input instance and `<options>` defines the command-line options. 

**Smoke Test:**
To test that the paths have been set up correctly, we can verify a small example instance from the paper. 
For this, run

```shell
./app/HyperSAT --vampire ./examples/simple_example.txt
```

HyperSAT should output `UNSAT`.


# Input to HyperSAT

In this section, we first discuss the command-line options of HyperSAT, followed by the structure of supported input. 
A call to HyperSAT has the form

```shell
./app/HyperSAT <options> <instancePath>
```

where `<instancePath>` is the (path to the) input instance and `<options>` defines the command-line options. 

## Command Line Options 

HyperSAT performs different FOL encodings, in different formats, and supports different FOL solvers. 

### Encodings

HyperSAT supports three basic encodings: 

- A predicate-based encoding where time steps are accessed via a _successor-predicate_ (`--predicate`), 
- A function-based encoding where time steps are accessed via a _successor-functions (`--function`), 
- A linear integer Arithmetic (LIA)-based encoding where time-steps are modeled as natural numbers (`--lia`). 

The first two encodings assume that the LTL body of the formula is a _safety_ formula; the LIA encoding applies to arbitrary HyperLTL formulas. 
By default, HyperSAT uses the function-based encoding. 

### Format

HyperSAT supports two file formats to represent FOL queries:

- [SMTLIB](https://smt-lib.org/) (`--smtlib`),
- [TPTP](https://www.tptp.org/) (`--tptp`)

By default, HyperSAT uses the TPTP format.
Note that `--tptp` is not compatible with the `--lia` encoding. 

### Solvers

Currently, HyperSAT supports the following FOL/SMT solvers

- _Vampire_ (`--vampire`)
- _Paradox_ (`--paradox`)
- _Z3_ (`--z3`)
- _CVC5_ (`--cvc5`)

To use one of the solvers, it needs to be installed and the path specified in the `paths.json` file (see [Connect Spot and FOL Solvers](#connect-spot-and-fol-solvers)).
E.g., when using `--vampire`, the _Vampire_ solver needs to be installed and connected to HyperSAT.
Note that not all solvers are compatible with all file formats.
For example, _Paradox_ does not support the SMTLIB format and _Z3_ and _CVC5_ do not support the TPTP format. 

If no solver is given, HyperSAT only computes the encoding but does not attempt to solve it. 
In this case, it generated a `query.smt2` (for the SMTLIB format) or `query.p` (for the TPTP format) file. 


## HyperLTL

HyperSAT supports HyperLTL formulas in an extension of [spot's](https://spot.lrde.epita.fr/) LTL format.
Concretely, a HyperLTL formula consists of an LTL body, preceded by a quantifier prefix of trace variables.
A trace variable (`<tvar>`) is any (non-reserved) sequence consisting of letters and digits (starting with a letter).

Formally, a HyperLTL formula has the form `<prefix> <body>`.

Here `<body>` can be one of the following:
- `1`: specifies the boolean constant true
- `0`: specifies the boolean constant false
- `"<ap>"_<tvar>`, where `<ap>` is an atomic proposition (AP), which can be any string not containing `"`. Note that APs always need to be escaped in `"`s.
- `(<body>)`
- `<body> <bopp> <body>`, where `<bopp>` can be `&` (conjunction), `|` (disjunction), `->` (implication), `<->` (equivalence), `U` (until operator), `W` (weak until operator), and `R` (release operator)
- `<uopp> <body>`, where `<uopp>` can be `!` (negation), `X` (next operator), `G` (globally), and `F` (eventually operator)

The operators follow the usual operator precedences. 
To avoid ambiguity, we recommend always placing parenthesis around each construct. 

The quantifier prefix `<prefix>` can be one of the following:

- The empty string
- Universal or existential trace quantification `forall <tvar>. <prefix>` and `exists <tvar>. <prefix>`. 

An example property is the following: 

```
forall pi. forall pii. G ("a"_pi <-> !"a"_pii)
```


## HyperSAT Output

If a solver is given, HyperSAT attempts to parse the solver's output and print `SAT`, `UNSAT`, or `UNKNOWN`.
If no solver is given, HyperSAT computes the encoding (in the SMTLIB or TPTP format), and terminates without any print. 

When using the command-line option `--log`, HyperSAT prints additional debug statements. 
