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

module HyperSAT.Configuration 

open System.IO

open FsOmegaLib.JSON

open Util

type FolSolver = 
    | VAMPIRE
    | PARADOX
    | Z3 
    | CVC5

module FolSolver =
    let print s = 
        match s with  
        | VAMPIRE -> "vampire"
        | PARADOX -> "paradox"
        | Z3 -> "z3"
        | CVC5 -> "cvc5"



type SolverConfiguration = 
    {
        MainPath : string
        Ltl2tgbaPath: string
        SolverPaths : Map<FolSolver, string>
    }

type Logger = 
    {
        Log : string -> unit
    }

    member this.LogN s = this.Log (s + "\n")

type Configuration = 
    {
        SolverConfig : SolverConfiguration
        Logger : Logger
    }

let private parseConfigFile (s : string) =
    match FsOmegaLib.JSON.Parser.parseJsonString s with 
    | Result.Error err -> raise <| HyperSatException $"Could not parse config file: %s{err}"
    | Result.Ok x -> 
        {
            MainPath = "./"
            
            Ltl2tgbaPath = 
                (JSON.tryLookup "ltl2tgba" x)
                |> Option.defaultWith (fun _ -> raise <| HyperSatException "No field 'ltl2tgba' found")
                |> JSON.tryGetString
                |> Option.defaultWith (fun _ -> raise <| HyperSatException "Field 'ltl2tgba' must contain a string")
            
            SolverPaths = 
                [VAMPIRE; PARADOX; Z3; CVC5] 
                |> List.choose (fun solver -> 
                    JSON.tryLookup (FolSolver.print solver) x 
                    |> Option.map (fun x -> 
                        let path = 
                            x
                            |> JSON.tryGetString
                            |> Option.defaultWith (fun _ -> raise <| HyperSatException $"Field '{FolSolver.print solver}' must contain a string")

                        (solver, path)
                    )
                    )
                |> Map.ofList
        }

let getConfig() = 
    // By convention the paths.json file is located in the same directory as the executable
    let configPath = 
        System.IO.Path.Join [|System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location); "paths.json"|]
                     
    // Check if the path to the config file exists
    if System.IO.FileInfo(configPath).Exists |> not then 
        raise <| HyperSatException "The paths.json file does not exist in the same directory as the executable"            
    
    // Parse the config File
    let configContent = 
        try
            File.ReadAllText configPath
        with 
            | _ -> 
                raise <| HyperSatException "Could not open paths.json file"

    let solverConfig = parseConfigFile configContent

    solverConfig
