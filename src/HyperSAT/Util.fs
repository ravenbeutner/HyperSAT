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

module HyperSAT.Util

open System

exception HyperSatException of string

module ParserUtil =
    open FParsec

    let escapedStringParser: Parser<string, unit> =
        let escapedCharParser: Parser<string, unit> =
            anyOf "\"\\/bfnrt"
            |>> fun x ->
                match x with
                | 'b' -> "\b"
                | 'f' -> "\u000C"
                | 'n' -> "\n"
                | 'r' -> "\r"
                | 't' -> "\t"
                | c -> string c

        let doubleQuotes =
            between
                (pchar '"')
                (pchar '"')
                (stringsSepBy (manySatisfy (fun c -> c <> '"' && c <> '\\')) (pstring "\\" >>. escapedCharParser))

        let singleQuotes =
            between
                (pchar ''')
                (pchar ''')
                (stringsSepBy (manySatisfy (fun c -> c <> ''' && c <> '\\')) (pstring "\\" >>. escapedCharParser))

        doubleQuotes <|> singleQuotes

module SubprocessUtil =

    type SubprocessResult =
        { Stdout: String
          Stderr: String
          ExitCode: int }

    let executeSubprocess (cmd: string) (arg: string) =
        let psi = System.Diagnostics.ProcessStartInfo(cmd, arg)

        psi.UseShellExecute <- false
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        psi.CreateNoWindow <- true

        let p = System.Diagnostics.Process.Start(psi)
        let output = System.Text.StringBuilder()
        let error = System.Text.StringBuilder()

        p.OutputDataReceived.Add(fun args -> output.Append(args.Data) |> ignore)
        p.ErrorDataReceived.Add(fun args -> error.Append(args.Data) |> ignore)
        p.BeginErrorReadLine()
        p.BeginOutputReadLine()
        p.WaitForExit()

        { SubprocessResult.Stdout = output.ToString()
          Stderr = error.ToString()
          ExitCode = p.ExitCode }
