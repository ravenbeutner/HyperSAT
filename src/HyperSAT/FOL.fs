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

module HyperSAT.FOL


type FirstOrderVariable = string
type FirstOrderFunctionSymbol = string
type FirstOrderPredicateSymbol = string
type FirstOrderSort = string

type FirstOrderTerm<'T> =
    | Variable of 'T
    | FunctionApp of FirstOrderFunctionSymbol * list<FirstOrderTerm<'T>>

module FirstOrderTerm =
    let rec printSmtLib (varStringer: 'T -> string) (t: FirstOrderTerm<'T>) =
        match t with
        | Variable x -> varStringer x
        | FunctionApp(f, args) ->
            if List.isEmpty args then
                f
            else
                "("
                + f
                + " "
                + (args |> List.map (printSmtLib varStringer) |> String.concat " ")
                + ")"

    let rec printTPTP (varStringer: 'T -> string) (t: FirstOrderTerm<'T>) =
        match t with
        | Variable x -> varStringer x
        | FunctionApp(f, args) ->
            if List.isEmpty args then
                f
            else
                f + " (" + (args |> List.map (printTPTP varStringer) |> String.concat ",") + ")"


type FirstOrderFormula<'T> =
    | And of list<FirstOrderFormula<'T>>
    | Or of list<FirstOrderFormula<'T>>
    | Implies of FirstOrderFormula<'T> * FirstOrderFormula<'T>
    | Neg of FirstOrderFormula<'T>
    | PredicateApp of FirstOrderPredicateSymbol * list<FirstOrderTerm<'T>>
    | Forall of list<'T * FirstOrderSort> * FirstOrderFormula<'T>
    | Exists of list<'T * FirstOrderSort> * FirstOrderFormula<'T>


module FirstOrderFormula =
    let rec printSmtLib (varStringer: 'T -> string) (f: FirstOrderFormula<'T>) =
        match f with
        | And args ->
            match args with
            | [] -> "true"
            | [ x ] -> printSmtLib varStringer x
            | _ ->
                "(and "
                + (args |> List.map (printSmtLib varStringer) |> String.concat " ")
                + ")"
        | Or args ->
            match args with
            | [] -> "false"
            | [ x ] -> printSmtLib varStringer x
            | _ -> "(or " + (args |> List.map (printSmtLib varStringer) |> String.concat " ") + ")"
        | Implies(f1, f2) -> "(=> " + printSmtLib varStringer f1 + " " + printSmtLib varStringer f2 + ")"
        | Neg(f1) -> "(not " + printSmtLib varStringer f1 + ")"
        | PredicateApp(p, args) ->
            "("
            + p
            + " "
            + (args |> List.map (FirstOrderTerm.printSmtLib varStringer) |> String.concat " ")
            + ")"
        | Forall(prefix, f1) ->
            let prefixString =
                prefix
                |> List.map (fun (x, s) -> "(" + varStringer x + " " + s + ")")
                |> String.concat " "

            "(forall (" + prefixString + ") " + printSmtLib varStringer f1 + ")"
        | Exists(prefix, f1) ->
            let prefixString =
                prefix
                |> List.map (fun (x, s) -> "(" + varStringer x + " " + s + ")")
                |> String.concat " "

            "(exists (" + prefixString + ") " + printSmtLib varStringer f1 + ")"

    let rec printTPTP (varStringer: 'T -> string) (f: FirstOrderFormula<'T>) =
        match f with
        | And args ->
            match args with
            | [] -> "$true"
            | [ x ] -> printTPTP varStringer x
            | _ ->
                args
                |> List.map (printTPTP varStringer)
                |> List.reduce (fun a b -> "(" + a + ")" + " & " + "(" + b + ")")
        | Or args ->
            match args with
            | [] -> "$false"
            | [ x ] -> printTPTP varStringer x
            | _ ->
                args
                |> List.map (printTPTP varStringer)
                |> List.reduce (fun a b -> "(" + a + ")" + " | " + "(" + b + ")")
        | Implies(f1, f2) ->
            "("
            + printTPTP varStringer f1
            + ")"
            + " => "
            + "("
            + printTPTP varStringer f2
            + ")"
        | Neg(f1) -> "~ (" + printTPTP varStringer f1 + ")"
        | PredicateApp(p, args) ->
            p
            + "("
            + (args |> List.map (FirstOrderTerm.printTPTP varStringer) |> String.concat ",")
            + ")"
        | Forall(prefix, f1) ->
            let prefixString =
                prefix |> List.map (fun (x, _) -> varStringer x) |> String.concat ","

            let sortsString =
                prefix
                |> List.map (fun (x, s) -> s + "(" + varStringer x + ")")
                |> String.concat " & "
                |> fun x -> "(" + x + ")"

            "! ["
            + prefixString
            + "] : ("
            + sortsString
            + " => "
            + printTPTP varStringer f1
            + ")"
        | Exists(prefix, f1) ->
            let prefixString =
                prefix |> List.map (fun (x, _) -> varStringer x) |> String.concat ","

            let sortsString =
                prefix
                |> List.map (fun (x, s) -> s + "(" + varStringer x + ")")
                |> String.concat " & "
                |> fun x -> "(" + x + ")"

            "? ["
            + prefixString
            + "] : ("
            + sortsString
            + " & "
            + printTPTP varStringer f1
            + ")"



type FirstOrderSmtLibInstance<'T> =
    { Formulas: list<FirstOrderFormula<'T>>
      Sorts: list<FirstOrderSort>
      FunctionSymbols: list<FirstOrderFunctionSymbol * list<FirstOrderSort> * FirstOrderSort>
      PredicateSymbols: list<FirstOrderPredicateSymbol * list<FirstOrderSort>> }



module FirstOrderSmtLibInstance =
    let print (varStringer: 'T -> string) (p: FirstOrderSmtLibInstance<'T>) =
        let sortStrings =
            p.Sorts
            |> List.map (fun s -> "(declare-sort " + s + " 0)")
            |> String.concat "\n"

        let functionString =
            p.FunctionSymbols
            |> List.map (fun (f, args, r) -> "(declare-fun " + f + " (" + (args |> String.concat " ") + ") " + r + ")")
            |> String.concat "\n"

        let predicateString =
            p.PredicateSymbols
            |> List.map (fun (f, args) ->
                // In SMTLIB a predicate is just a bool-valued function
                "(declare-fun " + f + " (" + (args |> String.concat " ") + ") Bool)")
            |> String.concat "\n"

        let formulasString =
            p.Formulas
            |> List.map (fun x -> "(assert " + FirstOrderFormula.printSmtLib varStringer x + ")")
            |> String.concat "\n"


        sortStrings
        + "\n"
        + functionString
        + "\n"
        + predicateString
        + "\n"
        + formulasString
        + "\n"
        + "(check-sat)"

type FirstOrderTPTPInstance<'T> =
    { Formulas: list<FirstOrderFormula<'T>> }

module FirstOrderTPTPInstance =
    let print (varStringer: 'T -> string) (p: FirstOrderTPTPInstance<'T>) =
        let axiomsString =
            p.Formulas
            |> List.mapi (fun i x ->

                let s = FirstOrderFormula.printTPTP varStringer x
                $"fof(axiom_{i}, axiom, (\n {s})). \n"

            )
            |> String.concat "\n"

        axiomsString
