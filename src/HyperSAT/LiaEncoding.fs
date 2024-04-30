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

module HyperSAT.LiaEncoding

open System

open FsOmegaLib.SAT
open FsOmegaLib.NBA
open FsOmegaLib.Operations

open Util
open Configuration
open HyperLTL
open FOL

let computeEncodingSmtLib (config: Configuration) (hyperltl: HyperLTL<string>) =
    let traceVariables = hyperltl.QuantifierPrefix |> List.map snd

    let nba =
        FsOmegaLib.Operations.LTLConversion.convertLTLtoNBA
            false
            config.SolverConfig.MainPath
            config.SolverConfig.Ltl2tgbaPath
            hyperltl.LTLMatrix
        |> AutomataOperationResult.defaultWith (fun err ->
            config.Logger.LogN err.DebugInfo
            raise <| HyperSatException err.Info)

    // ========================================================= Sorts =========================================================
    let traceSort = "Trace"

    // ========================================================= Predicate Symbols =========================================================

    let predicatesForApsMap =
        nba.APs
        |> List.map fst
        |> List.distinct
        |> List.mapi (fun i x -> x, "P_" + string i)
        |> Map.ofList

    let predicatesForStatesMap =
        nba.States |> Seq.map (fun i -> i, "at_" + string i) |> Map.ofSeq

    // ========================================================= FO Variables =========================================================
    let timeVar = "i"
    let timeVarNext = "ii"

    let variablesForTraceVariablesMap =
        traceVariables |> List.mapi (fun i x -> x, "t_" + string i) |> Map.ofList

    // ================================================================================================================================
    // ========================================================= Formula Construction =================================================
    // ================================================================================================================================

    let initFormula =
        nba.InitialStates
        |> Seq.toList
        |> List.map (fun q ->
            PredicateApp(
                predicatesForStatesMap.[q],
                FunctionApp("0", [])
                :: (traceVariables
                    |> List.map (fun pi -> Variable variablesForTraceVariablesMap.[pi]))
            ))
        |> Or

    // Construct the FO formula that models the transitions of the automaton
    let transitionFormula =
        nba.States
        |> Seq.map (fun q ->
            let hasSuccessorFormula =
                nba.Edges.[q]
                |> List.map (fun (guard, qq) ->
                    let simplifiedGuard = DNF.simplify guard

                    let guardFormula =
                        simplifiedGuard
                        |> List.map (fun clause ->
                            clause
                            |> List.map (fun l ->
                                let a, pi = nba.APs.[Literal.getValue l]

                                let predForA = predicatesForApsMap.[a]
                                let varForPi = variablesForTraceVariablesMap.[pi]

                                let posPredicate = PredicateApp(predForA, [ Variable varForPi; Variable timeVar ])

                                match l with
                                | PL _ -> posPredicate
                                | NL _ -> Neg posPredicate)
                            |> And)
                        |> Or


                    And
                        [ guardFormula
                          PredicateApp(
                              predicatesForStatesMap.[qq],
                              (FunctionApp("+", [ Variable timeVar; Variable "1" ]))
                              :: (traceVariables
                                  |> List.map (fun pi -> Variable variablesForTraceVariablesMap.[pi]))
                          ) ])
                |> Or

            Implies(
                PredicateApp(
                    predicatesForStatesMap.[q],
                    (Variable timeVar)
                    :: (traceVariables
                        |> List.map (fun pi -> Variable variablesForTraceVariablesMap.[pi]))
                ),
                hasSuccessorFormula
            )

        )
        |> Seq.toList
        |> And
        |> fun x -> Forall([ (timeVar, "Int") ], x)

    let acceptingFormula =
        Forall(
            [ timeVar, "Int" ],
            Exists(
                [ timeVarNext, "Int" ],
                [ PredicateApp("<=", [ Variable timeVar; Variable timeVarNext ])

                  // At this time, no non-accepting state is visited
                  (Set.difference nba.States nba.AcceptingStates)
                  |> Seq.map (fun q ->
                      Neg(
                          PredicateApp(
                              predicatesForStatesMap.[q],
                              (Variable timeVarNext)
                              :: (traceVariables
                                  |> List.map (fun pi -> Variable variablesForTraceVariablesMap.[pi]))
                          )
                      ))
                  |> Seq.toList
                  |> And ]
                |> And

            )
        )

    let rec computeQuantifiedBody prefix =
        match prefix with
        | [] ->
            // Base Case: Formula that asserts that the traces that have been chosen are accepted
            And [ initFormula; transitionFormula; acceptingFormula ]
        | (FORALL, pi) :: xs -> Forall([ variablesForTraceVariablesMap.[pi], traceSort ], computeQuantifiedBody xs)
        | (EXISTS, pi) :: xs -> Exists([ variablesForTraceVariablesMap.[pi], traceSort ], computeQuantifiedBody xs)

    let finalFormula = computeQuantifiedBody hyperltl.QuantifierPrefix

    // ============================================================================================================================================
    // ========================================================= Final FOL Instance ===============================================================
    // ============================================================================================================================================

    { FirstOrderSmtLibInstance.Formulas = [ finalFormula ]
      Sorts = [ traceSort ]
      FunctionSymbols = []
      PredicateSymbols =
        (predicatesForApsMap
         |> Map.values
         |> Seq.toList
         |> List.map (fun p -> (p, [ traceSort; "Int" ])))
        @ (predicatesForStatesMap
           |> Map.values
           |> Seq.toList
           |> List.map (fun p -> (p, "Int" :: List.init traceVariables.Length (fun _ -> traceSort))))

    }
