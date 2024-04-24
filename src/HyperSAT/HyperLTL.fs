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

module HyperSAT.HyperLTL 

open FsOmegaLib.LTL

type Quantifier = FORALL | EXISTS
type TraceVariable = string

type HyperLTL<'T when 'T: comparison> = 
    {
        QuantifierPrefix : list<Quantifier * TraceVariable>
        LTLMatrix : LTL<'T * TraceVariable>
    }

module HyperLTL = 
    let map f (formula : HyperLTL<'T>) = 
        {
            QuantifierPrefix = formula.QuantifierPrefix
            LTLMatrix = 
                formula.LTLMatrix
                |> LTL.map (fun (x, pi) -> (f x, pi))
        }


    exception private NotWellFormedException of string 

    let findError (formula : HyperLTL<'L>) =
        try 
            let traceVars = formula.QuantifierPrefix |> List.map snd

            if traceVars |> set |> Set.count <> List.length traceVars then 
                raise <| NotWellFormedException $"Some trace variable is used more than once."

            LTL.allAtoms formula.LTLMatrix
            |> Set.iter (fun (_, pi) -> 
                if List.contains pi traceVars |> not then 
                    raise <| NotWellFormedException $"Trace Variable %s{pi} is used but not defined in the prefix"
                )
            None
        with 
        | NotWellFormedException msg -> Some msg



module Parser =
    open FParsec
    
    let private keywords =
        [
            "X"
            "G"
            "F"
            "U"
            "W"
            "R"
        ]
        
    let traceVarParser : Parser<string, unit> =
        attempt (
            pipe2
                letter
                (manyChars (letter <|> digit))
                (fun x y -> string(x) + y)
            >>= fun s ->
                if List.contains s keywords then
                    fail ""
                else preturn s
            )
    
    let tracePrefixParser =
        let existsTraceParser = 
            skipString "exists " >>. spaces >>. traceVarParser .>> spaces .>> pchar '.'
            |>> fun pi -> (EXISTS, pi)

        let forallTraceParser = 
            skipString "forall " >>. spaces >>. traceVarParser .>> spaces .>> pchar '.'
            |>> fun pi -> (FORALL, pi)

        spaces >>.
        many1 (choice [existsTraceParser; forallTraceParser] .>> spaces)
        .>> spaces

    let private hyperLTLAtomParser atomParser =
        tuple2
            (atomParser)
            (spaces >>. pchar '_' >>. spaces >>. traceVarParser)

    let hyperLTLParser (atomParser : Parser<'T, unit>) : Parser<HyperLTL<'T>, unit> =     
        pipe2
            tracePrefixParser
            (FsOmegaLib.LTL.Parser.ltlParser (hyperLTLAtomParser atomParser))
            (fun x y -> {HyperLTL.QuantifierPrefix = x; LTLMatrix = y})
    
    let parseHyperLTL (atomParser : Parser<'T, unit>) s =    
        let full = hyperLTLParser atomParser .>> spaces .>> eof
        let res = run full s
        match res with
        | Success (res, _, _) -> Result.Ok res
        | Failure (err, _, _) -> Result.Error err
