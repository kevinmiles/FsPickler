﻿namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FsPickler")>]
[<assembly: AssemblyProductAttribute("FsPickler")>]
[<assembly: AssemblyCopyrightAttribute("© Eirik Tsarpalis.")>]
[<assembly: AssemblyVersionAttribute("1.2.12")>]
[<assembly: AssemblyFileVersionAttribute("1.2.12")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.2.12"