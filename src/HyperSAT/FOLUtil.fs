(*    
    Copyright (C) 2024 Raven Beutner

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*)

module HyperSAT.FOLUtil 

open System.IO

open Util
open Configuration
open FOL


type SatResult = 
    | SAT 
    | UNSAT 
    | UNKNOWN

let isSatSmtLib (config : Configuration) (solver : FolSolver) (query : FirstOrderSmtLibInstance<string>) = 
    let instancePath = System.IO.Path.Combine(config.SolverConfig.MainPath, "query.smt2")
    let s = FirstOrderSmtLibInstance.print id query

    try File.WriteAllText(instancePath, s) with 
    | _ -> raise <| HyperSatException $"Could not write to path {instancePath}"

    let solverPath = 
        match solver with 
        | Z3 | CVC5 | VAMPIRE -> 
            if Map.containsKey solver config.SolverConfig.SolverPaths |> not then 
                raise <| HyperSatException $"No path to '{FolSolver.print solver}' given"

            config.SolverConfig.SolverPaths[solver]
        | _ -> 
            raise <| HyperSatException $"`Solver '{FolSolver.print solver}' does not support the SMTLIB format"

    let res = Util.SubprocessUtil.executeSubprocess solverPath instancePath

    if res.ExitCode <> 0 then 
        raise <| HyperSatException $"Unexpected (!= 0) exit code {res.ExitCode} by '{FolSolver.print solver}'"

    let output = res.Stdout

    let res = 
        if output.Contains("unsat") then 
            UNSAT 
        elif output.Contains("sat") then 
            SAT 
        elif output.Contains("unknown") then 
            UNKNOWN 
        else 
            raise <| HyperSatException $"Unexpected output by '{FolSolver.print solver}': {output}"
    
    res

let isSatTPTP (config : Configuration) (solver : FolSolver) (query : FirstOrderTPTPInstance<string>) = 

    let path = System.IO.Path.Combine(config.SolverConfig.MainPath, "query.p")
    let s = FirstOrderTPTPInstance.print id query

    try File.WriteAllText(path, s) with 
    | _ -> raise <| HyperSatException $"Could not write to path {path}"


    let solverPath = 
        match solver with 
        | VAMPIRE | PARADOX -> 
            if Map.containsKey solver config.SolverConfig.SolverPaths |> not then 
                raise <| HyperSatException $"No path to '{FolSolver.print solver}' given"

            config.SolverConfig.SolverPaths[solver]
        | _ -> 
            raise <| HyperSatException $"`Solver '{FolSolver.print solver}' does not support the TPTP format"

    let res = Util.SubprocessUtil.executeSubprocess solverPath path

    if res.ExitCode <> 0 then 
        raise <| HyperSatException $"Unexpected exit code {res.ExitCode} by '{FolSolver.print solver}'"

    let output = res.Stdout

    let res = 
        match solver with 
        | VAMPIRE -> 
            if output.Contains("Termination reason: Refutation") then 
                UNSAT 
            elif output.Contains("Termination reason: Satisfiable") then 
                SAT 
            else 
                raise <| HyperSatException $"Unexpected output by '{FolSolver.print solver}': {output}"
        | PARADOX -> 
            if output.Contains("RESULT: Satisfiable") then 
                SAT 
            elif output.Contains("RESULT: Unsatisfiable") then 
                UNSAT 
            else 
                raise <| HyperSatException $"Unexpected output by '{FolSolver.print solver}': {output}"
        | _ -> 
            raise <| HyperSatException $"Unexpected output by '{FolSolver.print solver}': {output}"
    
    res
