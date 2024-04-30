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

module HyperSAT.Program

open System.IO

open Util
open Configuration
open FOLUtil
open CommandLineParser

let mutable raiseExceptions = false

let run args =
    let sw = System.Diagnostics.Stopwatch()
    let swTotal = System.Diagnostics.Stopwatch()
    swTotal.Start()
    sw.Start()

    let solverConfig = Configuration.getConfig ()

    let cmdArgs =
        match CommandLineParser.parseCommandLineArguments (Array.toList args) with
        | Result.Ok x -> x
        | Result.Error e -> raise <| HyperSatException $"{e}"

    raiseExceptions <- cmdArgs.RaiseExceptions

    let logger =
        { Logger.Log =
            fun x ->
                if cmdArgs.LogPrintouts then
                    printf $"{x}" }

    let config =
        { Configuration.SolverConfig = solverConfig
          Logger = logger }

    let content =
        match cmdArgs.Input with
        | None -> raise <| HyperSatException "No input given"
        | Some(InputContent c) -> c
        | Some(InputFile path) ->
            try
                File.ReadAllText path
            with _ ->
                raise <| HyperSatException $"Could not open {path}"

    let hyperltl =
        HyperLTL.Parser.parseHyperLTL Util.ParserUtil.escapedStringParser content
        |> Result.defaultWith (fun e -> raise <| HyperSatException $"Could not parse HyperLTL formula: {e}")

    sw.Restart()

    match cmdArgs.Format with
    | SMTLIB ->

        let query =
            match cmdArgs.Encoding with
            | Predicate -> PredicateEncoding.computeEncodingSmtLib config hyperltl
            | Function -> FunctionEncoding.computeEncodingSmtLib config hyperltl
            | LIA -> LiaEncoding.computeEncodingSmtLib config hyperltl

        config.Logger.LogN
            $"Constructed FOL encoding in %i{sw.ElapsedMilliseconds} ms (%.4f{double (sw.ElapsedMilliseconds) / 1000.0} s)"

        sw.Restart()

        match cmdArgs.FolSolver with
        | Some solver ->
            let res = FOLUtil.isSatSmtLib config solver query

            config.Logger.LogN
                $"Solved encoding in %i{sw.ElapsedMilliseconds} ms (%.4f{double (sw.ElapsedMilliseconds) / 1000.0} s)"

            match res with
            | SAT -> printfn "SAT"
            | UNSAT -> printfn "UNSAT"
            | UNKNOWN -> printfn "UNKNOWN"

        | None ->
            let path = System.IO.Path.Combine(config.SolverConfig.MainPath, "query.smt2")
            let s = FOL.FirstOrderSmtLibInstance.print id query

            try
                File.WriteAllText(path, s)
            with _ ->
                raise <| HyperSatException $"Could not write to path {path}"

    | TPTP ->
        let query =
            match cmdArgs.Encoding with
            | Predicate -> PredicateEncoding.computeEncodingTPTP config hyperltl
            | Function -> FunctionEncoding.computeEncodingTPTP config hyperltl
            | LIA -> raise <| HyperSatException "(--lia, --tptp) is currently not supported"

        config.Logger.LogN
            $"Constructed FOL encoding in %i{sw.ElapsedMilliseconds} ms (%.4f{double (sw.ElapsedMilliseconds) / 1000.0} s)"

        sw.Restart()

        match cmdArgs.FolSolver with
        | Some solver ->
            let res = FOLUtil.isSatTPTP config solver query

            config.Logger.LogN
                $"Solved encoding in %i{sw.ElapsedMilliseconds} ms (%.4f{double (sw.ElapsedMilliseconds) / 1000.0} s)"

            match res with
            | SAT -> printfn "SAT"
            | UNSAT -> printfn "UNSAT"
            | UNKNOWN -> printfn "UNKNOWN"

        | None ->
            let path = System.IO.Path.Combine(config.SolverConfig.MainPath, "query.smt2")
            let s = FOL.FirstOrderTPTPInstance.print id query

            try
                File.WriteAllText(path, s)
            with _ ->
                raise <| HyperSatException $"Could not write to path {path}"

    config.Logger.LogN
        $"Total Time: %i{swTotal.ElapsedMilliseconds} ms (%.4f{double (swTotal.ElapsedMilliseconds) / 1000.0} s)"


[<EntryPoint>]
let main args =
    try
        run args

        0
    with
    | HyperSatException msg ->
        printfn "===== ERROR ====="
        printfn $"{msg}"

        if raiseExceptions then
            reraise ()

        1
    | e ->
        printfn "===== ERROR ====="
        printfn $"{e.Message}"

        if raiseExceptions then
            reraise ()

        1
