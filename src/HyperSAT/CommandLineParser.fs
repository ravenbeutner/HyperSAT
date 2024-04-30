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

module HyperSAT.CommandLineParser

open System

open Configuration

type Encoding =
    | Function
    | Predicate
    | LIA

type Format =
    | SMTLIB
    | TPTP

type Input =
    | InputFile of string
    | InputContent of string

type CommandLineArguments =
    { Encoding: Encoding
      Format: Format
      FolSolver: option<FolSolver>
      Input: option<Input>

      LogPrintouts: bool // If set to true, we log intermediate steps to the console
      RaiseExceptions: bool } // If set to true, we raise exceptions

    static member Default =
        { Encoding = Function
          Format = TPTP
          FolSolver = None
          Input = None

          LogPrintouts = false
          RaiseExceptions = true }

let rec private splitByPredicate (f: 'T -> bool) (xs: list<'T>) =
    match xs with
    | [] -> [], []
    | x :: xs ->
        if f x then
            [], x :: xs
        else
            let r1, r2 = splitByPredicate f xs
            x :: r1, r2

let parseCommandLineArguments (args: list<String>) =
    let rec parseArgumentsRec (args: list<String>) (opt: CommandLineArguments) =

        match args with
        | [] -> Result.Ok opt
        | x :: xs ->
            match x with
            | "--predicate" -> parseArgumentsRec xs { opt with Encoding = Predicate }
            | "--function" -> parseArgumentsRec xs { opt with Encoding = Function }
            | "--lia" -> parseArgumentsRec xs { opt with Encoding = LIA }

            | "--smtlib" -> parseArgumentsRec xs { opt with Format = SMTLIB }
            | "--tptp" -> parseArgumentsRec xs { opt with Format = TPTP }

            | "--z3" -> parseArgumentsRec xs { opt with FolSolver = Some Z3 }
            | "--cvc5" -> parseArgumentsRec xs { opt with FolSolver = Some CVC5 }
            | "--vampire" -> parseArgumentsRec xs { opt with FolSolver = Some VAMPIRE }
            | "--paradox" -> parseArgumentsRec xs { opt with FolSolver = Some PARADOX }

            | "--log" -> parseArgumentsRec xs { opt with LogPrintouts = true }

            | "-f" ->
                if opt.Input.IsSome then
                    Result.Error "Input cannot be given more than once"
                else
                    match xs with
                    | [] -> Result.Error("Option '-f' must be followed by a file name")
                    | y :: ys -> parseArgumentsRec ys { opt with Input = Some(InputFile y) }
            | s when s <> "" && s.Trim().StartsWith "-" -> Result.Error("Option " + s + " is not supported")

            | x ->
                // When no option is given, we assume that this is the input
                if opt.Input.IsSome then
                    Result.Error "Input cannot be given more than once"
                else
                    parseArgumentsRec
                        xs
                        { opt with
                            Input = Some(InputContent x) }

    parseArgumentsRec args CommandLineArguments.Default
