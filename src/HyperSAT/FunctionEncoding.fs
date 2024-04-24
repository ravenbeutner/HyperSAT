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

module HyperSAT.FunctionEncoding 

open System
open FsOmegaLib.SAT
open FsOmegaLib.NSA
open FsOmegaLib.Operations

open Util
open Configuration
open HyperLTL
open FOL 

let private encodeNSA 
    (nsa : NSA<int,string * TraceVariable>) 
    (prefix : list<Quantifier * TraceVariable>)
    (traceSort : FirstOrderSort) 
    (timeSort : FirstOrderSort) 
    (initTimeConstant: FirstOrderFunctionSymbol) 
    (timeNextFunction: FirstOrderFunctionSymbol) 
    (predicatesForApsMap : Map<string,FirstOrderPredicateSymbol>) 
    (predicatesForStatesMap : Map<int,FirstOrderPredicateSymbol>) = 

    let traceVariables = prefix |> List.map snd

    // ========================================================= FO Variables =========================================================

    let timeVar : FirstOrderVariable = "I"

    let variablesForTraceVariablesMap : Map<TraceVariable,FirstOrderVariable> = 
        traceVariables
        |> List.mapi (fun i x -> x, "T_" + string i)
        |> Map.ofList

    // ========================================================= Constraints =========================================================
    
    let initFormula = 
        nsa.InitialStates
        |> Seq.toList
        |> List.map (fun q -> 
            PredicateApp(
                predicatesForStatesMap.[q], 
                FunctionApp(initTimeConstant,[]) :: 
                (traceVariables |> List.map (fun pi -> Variable variablesForTraceVariablesMap.[pi]))
            )
            )
        |> Or

    let transitionFormula = 
        nsa.States
        |> Seq.map (fun q ->
            let hasSuccessorFormula =  
                nsa.Edges.[q]
                |> List.map (fun (guard, qq) -> 
                    let simplifiedGuard = DNF.simplify guard

                    let guardFormula = 
                        simplifiedGuard 
                        |> List.map (fun clause -> 
                            clause
                            |> List.map (fun l -> 
                                let a, pi = nsa.APs.[Literal.getValue l]

                                let predForA = predicatesForApsMap.[a]
                                let varForPi = variablesForTraceVariablesMap.[pi]

                                let posPredicate = 
                                    PredicateApp(predForA, [Variable varForPi; Variable timeVar])

                                match l with 
                                | PL _ -> posPredicate
                                | NL _ -> Neg posPredicate
                                )
                            |> And
                            )
                        |> Or


                    And [
                        guardFormula; 
                        PredicateApp(
                            predicatesForStatesMap.[qq], 
                            (FunctionApp(timeNextFunction, [Variable timeVar])) :: 
                            (traceVariables |> List.map (fun pi -> Variable variablesForTraceVariablesMap.[pi]))
                        )
                    ]
                )
                |> Or

            Implies(
                PredicateApp(
                    predicatesForStatesMap.[q], 
                    (Variable timeVar) :: 
                    (traceVariables |> List.map (fun pi -> Variable variablesForTraceVariablesMap.[pi]))
                ), 
                hasSuccessorFormula)
        )
        |> Seq.toList
        |> And
        |> fun x -> 
            Forall ([(timeVar, timeSort)], x)

    let rec computeQuantifiedBody prefix = 
        match prefix with 
        | [] -> 
            // Base Case: Formula that asserts that the traces that have been chosen are accepted
            And [initFormula; transitionFormula]
        | (FORALL, pi) :: xs ->   
            Forall ([variablesForTraceVariablesMap.[pi], traceSort], computeQuantifiedBody xs)
        | (EXISTS, pi) :: xs ->   
            Exists ([variablesForTraceVariablesMap.[pi], traceSort], computeQuantifiedBody xs)


    computeQuantifiedBody prefix

let computeEncodingSmtLib (config : Configuration) (hyperltl : HyperLTL<string>) = 
    let traceVariables = hyperltl.QuantifierPrefix |> List.map snd

    let nsa =
        match FsOmegaLib.Operations.LTLConversion.convertLTLtoNSA false config.SolverConfig.MainPath config.SolverConfig.Ltl2tgbaPath hyperltl.LTLMatrix with 
        | Success x -> x
        | Fail err -> 
            config.Logger.LogN err.DebugInfo
            raise <| HyperSatException err.Info

    // ========================================================= Sorts =========================================================
    let traceSort = "Trace"
    let timeSort = "TimePoint"

    // ========================================================= Predicate Symbols =========================================================

    let predicatesForApsMap = 
        nsa.APs
        |> List.map fst 
        |> List.distinct
        |> List.mapi (fun i x -> x, "P_" + string i)
        |> Map.ofList

    let predicatesForStatesMap = 
        nsa.States
        |> Seq.map (fun i -> i, "at_" + string i)
        |> Map.ofSeq

    // ========================================================= Function Symbols =========================================================

    let initTimeConstant = "i0"
    let timeNextFunction = "ff"

    // ========================================================= Encode NSA =========================================================

    let finalFormula = encodeNSA nsa hyperltl.QuantifierPrefix traceSort timeSort initTimeConstant timeNextFunction predicatesForApsMap predicatesForStatesMap


    // ============================================================================================================================================
    // ========================================================= Final FOL Instance ===============================================================
    // ============================================================================================================================================

    {
        FirstOrderSmtLibInstance.Formulas = [finalFormula]
        Sorts = [timeSort; traceSort]
        FunctionSymbols = 
            [
                (initTimeConstant, [], timeSort);
                (timeNextFunction, [timeSort], timeSort)
            ]
        PredicateSymbols = 
            (
                predicatesForApsMap
                |> Map.values
                |> Seq.toList
                |> List.map (fun p -> (p, [traceSort; timeSort]))
            )
            @
            (
                predicatesForStatesMap
                |> Map.values
                |> Seq.toList
                |> List.map (fun p -> (p, timeSort :: List.init traceVariables.Length (fun _ -> traceSort) ))
            )
    }


let computeEncodingTPTP (config : Configuration) (hyperltl : HyperLTL<string>) = 
    let nsa =
        match FsOmegaLib.Operations.LTLConversion.convertLTLtoNSA false config.SolverConfig.MainPath config.SolverConfig.Ltl2tgbaPath hyperltl.LTLMatrix with 
        | Success x -> x
        | Fail err -> 
            config.Logger.LogN err.DebugInfo
            raise <| HyperSatException err.Info

    // ========================================================= Sorts =========================================================
    let traceSort = "trace"
    let timeSort = "timepoint"

    // ========================================================= Predicate Symbols =========================================================

    let predicatesForApsMap = 
        nsa.APs
        |> List.map fst 
        |> List.distinct
        |> List.mapi (fun i x -> x, "p_" + string i)
        |> Map.ofList

    let predicatesForStatesMap = 
        nsa.States
        |> Seq.map (fun i -> i, "at_" + string i)
        |> Map.ofSeq

    // ========================================================= Function Symbols =========================================================

    let initTimeConstant = "i0"
    let timeNextFunction = "ff"

    // ========================================================= Encode NSA =========================================================

    let finalFormula = encodeNSA nsa hyperltl.QuantifierPrefix traceSort timeSort initTimeConstant timeNextFunction predicatesForApsMap predicatesForStatesMap

    // ================================================================================================================================
    // ========================================================= Sort Constraints =====================================================
    // ================================================================================================================================

    // The initial time constant is a time Sort 
    let initTimeSort = 
        PredicateApp(timeSort, [Variable initTimeConstant])

    // Ensure that there exists at least one trace
    let existsTrace = 
        PredicateApp(traceSort, [Variable "t"])

    // The time function always maps to the next 
    let nextTimeSort = 
        Forall (["I", timeSort], PredicateApp(timeSort, [FunctionApp(timeNextFunction, [Variable "I"])]))

    // ============================================================================================================================================
    // ========================================================= Final FOL Instance ===============================================================
    // ============================================================================================================================================
    
    {
        FirstOrderTPTPInstance.Formulas = 
            [initTimeSort; nextTimeSort; existsTrace; finalFormula]
    }
