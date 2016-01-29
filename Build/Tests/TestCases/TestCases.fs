﻿open Docopt
open Docopt.Arguments
open System
open System.Diagnostics

let mutable count = 0 in
let stopwatch = Stopwatch() in

// HELPER FUNCTIONS FOR ASSERTIONS
type Assert =
  static member Seq(test':string, usage':string, [<ParamArray>]statements':(Docopt -> string * bool)[]) =
    try
      let mutable doc = Docopt(usage')
      count <- count + 1
      printf "%s\n{" test'
      Console.WriteLine(usage')
      printfn "  Asts: %A" doc.UsageParser.Asts
      Array.iter (fun assertion' -> let doc = Docopt(usage')
                                    count <- count + 1
                                    let msg, res = assertion' doc
                                    printfn "    %s . . . %A" msg res
                                    Debug.Assert(res, msg)) statements'
      Console.WriteLine("}\n")
    with e -> printfn ">>> ERROR: %A" e;
              reraise ()
let ( ->= ) (argv':string) val' (doc':Docopt) =
  let argv = argv'.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
  let args = doc'.Parse(argv).AsList()
  let res = List.sort args = List.sort val'
  if not res then printfn "Got args = %A" args
  (sprintf "%A ->= %A" argv' val'), res
let ( ->! ) (argv':string) val' (doc':Docopt) =
  let argv = argv'.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
  let msg, res =
    try let _ = doc'.Parse(argv).AsList() in (box "<NO EXN>", false)
    with e -> (box e, e.GetType() = val')
  (sprintf "%A ->! %A" argv' msg), res
// END HELPER FUNCTIONS FOR ASSERTIONS

stopwatch.Start()

Assert.Seq("Empty usage", """
Usage: prog

""",
  ""      ->= [],
  "--xxx" ->! typeof<ArgvException>
)

Assert.Seq("Basic short option", """
Usage: prog [options]

Options: -a  All.

""",
  ""   ->= [],
  "-a" ->= [("-a", Flag)],
  "-x" ->! typeof<ArgvException>
)
Assert.Seq("Basic long option", """
Usage: prog [options]

Options: --all  All.

""",
  ""      ->= [],
  "--all" ->= [("--all", Flag)],
  "--xxx" ->! typeof<ArgvException>
)

Assert.Seq("Synonymous short and long option, with truncation", """
Usage: prog [options]

Options: -v, --verbose  Verbose.

""",
  "--verbose" ->= [("-v", Flag);("--verbose", Flag)],
  "--ver"     ->= [("-v", Flag);("--verbose", Flag)],
  "-v"        ->= [("-v", Flag);("--verbose", Flag)]
)

Assert.Seq("Short option with argument", """
Usage: prog [options]

Options: -p PATH

""",
  "-p home/" ->= [("-p", Argument("home/"))],
  "-phome/"  ->= [("-p", Argument("home/"))],
  "-p"       ->! typeof<ArgvException>
)

Assert.Seq("Long option with argument", """
Usage: prog [options]

Options: --path <path>

""",
  "--path home/" ->= [("--path", Argument("home/"))],
  "--path=home/" ->= [("--path", Argument("home/"))],
  "--pa home/"   ->= [("--path", Argument("home/"))],
  "--pa=home/"   ->= [("--path", Argument("home/"))],
  "--path"       ->! typeof<ArgvException>
)

Assert.Seq("Synonymous short and long option with both arguments declared", """
Usage: prog [options]

Options: -p PATH, --path=<path>  Path to files.

""",
  "-proot" ->= [("-p", Argument("root"));("--path", Argument("root"))]
)

Assert.Seq("Synonymous short and long option with one argument declared", """
Usage: prog [options]

Options:    -p --path PATH  Path to files.

""",
  "-p root"     ->= [("-p", Argument("root"));("--path", Argument("root"))],
  "--path root" ->= [("-p", Argument("root"));("--path", Argument("root"))]
)

Assert.Seq("Short option with default", """
Usage: prog [options]

Options:
 -p PATH  Path to files [default: ./]

""",
  ""       ->= [("-p", Argument("./"))],
  "-phome" ->= [("-p", Argument("home"))]
)

Assert.Seq("Unusual formatting", """
UsAgE: prog [options]

OpTiOnS: --path=<files>  Path to files
                [dEfAuLt: /root]

""",
  ""            ->= [("--path", Argument("/root"))],
  "--path=home" ->= [("--path", Argument("home"))]
)

Assert.Seq("Multiple short options", """
usage: prog [options]

options:
    -a        Add
    -r        Remote
    -m <msg>  Message

""",
  "-a -r -m Hello" ->= [("-a", Flag);("-r", Flag);("-m", Argument("Hello"))],
  "-armyourass"    ->= [("-a", Flag);("-r", Flag);("-m", Argument("yourass"))],
  "-a -r"          ->= [("-a", Flag);("-r", Flag)]
)

Assert.Seq("Truncated long option disambiguation", """
Usage: prog [options]

Options: --version
         --verbose

""",
  "--version" ->= [("--version", Flag)],
  "--verbose" ->= [("--verbose", Flag)],
  "--ver"     ->! typeof<ArgvException>,
  "--verb"    ->= [("--verbose", Flag)]
)

Assert.Seq("Short options in square brackets", """
usage: prog [-a -r -m <msg>]

options:
 -a        Add
 -r        Remote
 -m <msg>  Message

""",
  "-armyourass" ->= [("-a", Flag);("-r", Flag);("-m", Argument("yourass"))]
)

Assert.Seq("Short option pack in square brackets", """
usage: prog [-armmsg]

options: -a        Add
         -r        Remote
         -m <msg>  Message

""",
  "-a -r -m Hello" ->= [("-a", Flag);("-r", Flag);("-m", Argument("Hello"))]
)

Assert.Seq("Required short options", """
usage: prog -a -b

options:
 -a
 -b

""",
  "-a -b" ->= [("-a", Flag);("-b", Flag)],
  "-b -a" ->= [("-a", Flag);("-b", Flag)],
  "-a"    ->! typeof<ArgvException>,
  ""      ->! typeof<ArgvException>
)

Assert.Seq("Required short options in brackets", """
usage: prog (-a -b)

options: -a
         -b

""",
  "-a -b" ->= [("-a", Flag);("-b", Flag)],
  "-b -a" ->= [("-a", Flag);("-b", Flag)],
  "-a"    ->! typeof<ArgvException>,
  ""      ->! typeof<ArgvException>
)

Assert.Seq("Two options, one is optional","""
usage: prog [-a] -b

options: -a
 -b

""",
  "-a -b" ->= [("-a", Flag);("-b", Flag)],
  "-b -a" ->= [("-a", Flag);("-b", Flag)],
  "-a"    ->! typeof<ArgvException>,
  "-b"    ->= [("-b", Flag)],
  ""      ->! typeof<ArgvException>
)

Assert.Seq("Required in optional", """
usage: prog [(-a -b)]

options: -a
         -b

""",
  "-a -b" ->= [("-a", Flag);("-b", Flag)],
  "-b -a" ->= [("-a", Flag);("-b", Flag)],
  "-a"    ->! typeof<ArgvException>,
  "-b"    ->! typeof<ArgvException>,
  ""      ->= []
)

Assert.Seq("Exclusive or", """
usage: prog (-a|-b)

options: -a
         -b

""",
  "-a -b" ->! typeof<ArgvException>,
  ""      ->! typeof<ArgvException>,
  "-a"    ->= [("-a", Flag)],
  "-b"    ->= [("-b", Flag)]
)

Assert.Seq("Optional exclusive or", """
usage: prog [ -a | -b ]

options: -a
         -b

""",
  "-a -b" ->! typeof<ArgvException>,
  ""      ->= [],
  "-a"    ->= [("-a", Flag)],
  "-b"    ->= [("-b", Flag)]
)

Assert.Seq("Argument", """
usage: prog <arg>""",
  "10"    ->= [("<arg>", Argument("10"))],
  "10 20" ->! typeof<ArgvException>,
  ""      ->! typeof<ArgvException>
)

Assert.Seq("Optional argument", """
usage: prog [<arg>]""",
  "10"    ->= [("<arg>", Argument("10"))],
  "10 20" ->! typeof<ArgvException>,
  ""      ->= []
)

Assert.Seq("Multiple arguments", """
usage: prog <kind> <name> <type>""",
  "10 20 40" ->= [("<kind>", Argument("10"));("<name>", Argument("20"));("<type>", Argument("40"))],
  "10 20"    ->! typeof<ArgvException>,
  ""         ->! typeof<ArgvException>
)

Assert.Seq("Multiple arguments, two optional", """
usage: prog <kind> [<name> <type>]""",
  "10 20 40" ->= [("<kind>", Argument("10"));("<name>", Argument("20"));("<type>", Argument("40"))],
  "10 20"    ->= [("<kind>", Argument("10"));("<name>", Argument("20"))],
  ""         ->! typeof<ArgvException>
)

Assert.Seq("Multiple arguments xor'd in optional", """
usage: prog [<kind> | <name> <type>]""",
  "10 20 40" ->! typeof<ArgvException>,
  "20 40"    ->= [("<name>", Argument("20"));("<type>", Argument("40"))],
  ""         ->= []
)

Assert.Seq("Mixed xor, arguments and options", """
usage: prog (<kind> --all | <name>)

options:
 --all

""",
  "10 --all" ->= [("--all", Flag);("<kind>", Argument("10"))],
  "10"       ->= [("<name>", Argument("10"))],
  ""         ->! typeof<ArgvException>
)

Assert.Seq("Stacked argument", """
usage: prog [<name> <name>]""",
  "10 20" ->= [("<name>", Arguments(["20";"10"]))],
  "10"    ->= [("<name>", Argument("10"))],
  ""      ->= []
)

Assert.Seq("Same, but both arguments must be present", """
usage: prog [(<name> <name>)]""",
  "10 20" ->= [("<name>", Arguments(["20";"10"]))],
  "10"    ->! typeof<ArgvException>,
  ""      ->= []
)

Assert.Seq("Ellipsis (one or more (also, ALL-CAPS argument name))", """
usage: prog NAME...""",
  "10 20" ->= [("NAME", Arguments(["20";"10"]))],
  "10"    ->= [("NAME", Argument("10"))],
  ""      ->! typeof<ArgvException>
)

Assert.Seq("Optional in ellipsis", """
usage: prog [NAME]...""",
  "10 20" ->= [("NAME", Arguments(["20";"10"]))],
  "10"    ->= [("NAME", Argument("10"))],
  ""      ->= []
)

Assert.Seq("Ellipsis in optional", """
usage: prog [NAME...]""",
  "10 20" ->= [("NAME", Arguments(["20";"10"]))],
  "10"    ->= [("NAME", Argument("10"))],
  ""      ->= []
)

Assert.Seq("", """
usage: prog [NAME [NAME ...]]""",
  "10 20" ->= [("NAME", Arguments(["20";"10"]))],
  "10"    ->= [("NAME", Argument("10"))],
  ""      ->= []
)

Assert.Seq("Argument mismatch with option", """
usage: prog (NAME | --foo NAME)

options: --foo

""",
  "10"       ->= [("NAME", Argument("10"))],
  "--foo 10" ->= [("NAME", Argument("10"));("--foo", Flag)],
  "--foo=10" ->! typeof<ArgvException>
)

Assert.Seq("Multiple “options:” statements", """
usage: prog (NAME | --foo) [--bar | NAME]

options: --foo
options: --bar

""",
  "10"          ->= [("NAME", Argument("10"))],
  "10 20"       ->= [("NAME", Arguments(["20";"10"]))],
  "--foo --bar" ->= [("--foo", Flag);("--bar", Flag)]
)

Assert.Seq("Big Bertha: command, multiple usage and --long=arg", """
Naval Fate.

Usage:
  prog ship new <name>...
  prog ship [<name>] move <x> <y> [--speed=<kn>]
  prog ship shoot <x> <y>
  prog mine (set|remove) <x> <y> [--moored|--drifting]
  prog -h | --help
  prog --version

Options:
  -h --help     Show this screen.
  --version     Show version.
  --speed=<kn>  Speed in knots [default: 10].
  --moored      Mored (anchored) mine.
  --drifting    Drifting mine.

""",
  "ship Guardian move 150 300 --speed=20" ->= [
    "--speed", Argument("20");
    "<name>", Argument("Guardian");
    "<x>", Argument("150");
    "<y>", Argument("300");
    "move", Flag;
    "ship", Flag;
  ]
)

Assert.Seq("No `options:` part", """
usage: prog --hello""",
  "--hello" ->= [("--hello", Flag)]
)

Assert.Seq("No `options:` part", """
usage: prog [--hello=<world>]""",
  ""             ->= [],
  "--hello wrld" ->= [("--hello", Argument("wrld"))]
)

Assert.Seq("No `options:` part", """
usage: prog [-o]""",
  "" ->= [],
  "-o" ->= [("-o", Flag)]
)

Assert.Seq("No `options:` part", """
usage: prog [-opr]""",
  "-op" ->= [("-o", Flag);("-p", Flag)]
)

Assert.Seq("1 flag", """
Usage: prog -v""",
  "-v" ->= [("-v", Flag)]
)

Assert.Seq("2 flags", """
Usage: prog [-v -v]""",
  ""    ->= [],
  "-v"  ->= [("-v", Flag)],
  "-vv" ->= [("-v", Flags(2))]
)

Assert.Seq("Many flags", """
Usage: prog -v ...""",
  ""        ->! typeof<ArgvException>,
  "-v"      ->= [("-v", Flag)],
  "-vv"     ->= [("-v", Flags(2))],
  "-vvvvvv" ->= [("-v", Flags(6))]
)

Assert.Seq("Many flags xor'd", """
Usage: prog [-v | -vv | -vvv]

This one is probably most readable user-friendly variant.

""",
  ""      ->= [],
  "-v"    ->= [("-v", Flag)],
  "-vv"   ->= [("-v", Flags(2))],
  "-vvvv" ->! typeof<ArgvException>
)

Assert.Seq("Counting long options", """
usage: prog [--ver --ver]""",
  "--ver --ver" ->= [("--ver", Flags(2))]
)

Assert.Seq("1 command", """
usage: prog [go]""",
  "go" ->= [("go", Flag)]
)

Assert.Seq("2 commands", """
usage: prog [go go]""",
  ""         ->= [],
  "go"       ->= [("go", Flag)],
  "go go"    ->= [("go", Flags(2))],
  "go go go" ->! typeof<ArgvException>
)

Assert.Seq("Many commands", """
usage: prog go...""",
  "go go go go go" ->= [("go", Flags(5))]
)

Assert.Seq("[options] does not include options from usage-pattern", """
usage: prog [options] [-a]

options: -a
         -b
""",
  "-a"  ->= [("-a", Flag)],
  "-aa" ->! typeof<ArgvException>
)

Assert.Seq("[options] shortcut", """
Usage: prog [options] A
Options:
    -q  Be quiet
    -v  Be verbose.

""",
  "arg" ->= [("A", Argument("arg"))],
  "-v arg" ->= [("A", Argument("arg"));("-v", Flag)],
  "-q arg" ->= [("A", Argument("arg"));("-q", Flag)]
)

Assert.Seq("Single dash", """
usage: prog [-]""",
  "-" ->= [("-",  Flag)],
  ""  ->= []
)
(*
//
// If argument is repeated, its value should always be a list
//

let doc = Docopt("""usage: prog [NAME [NAME ...]]""")

$ prog a b
{"NAME": ["a", "b"]}

$ prog
{"NAME": []}

//
// Option's argument defaults to null/None
//

let doc = Docopt("""usage: prog [options]
options:
 -a        Add
 -m <msg>  Message

""")
$ prog -a
{"-m": null, "-a": true}

//
// Test options without description
//

let doc = Docopt("""usage: prog --hello""")
$ prog --hello
{"--hello": true}

let doc = Docopt("""usage: prog [--hello=<world>]""")
$ prog
{"--hello": null}

$ prog --hello wrld
{"--hello": "wrld"}

let doc = Docopt("""usage: prog [-o]""")
$ prog
{"-o": false}

$ prog -o
{"-o": true}

let doc = Docopt("""usage: prog [-opr]""")
$ prog -op
{"-o": true, "-p": true, "-r": false}

let doc = Docopt("""usage: git [-v | --verbose]""")
$ prog -v
{"-v": true, "--verbose": false}

let doc = Docopt("""usage: git remote [-v | --verbose]""")
$ prog remote -v
{"remote": true, "-v": true, "--verbose": false}

//
// Test empty usage pattern
//

let doc = Docopt("""usage: prog""")
$ prog
{}

let doc = Docopt("""usage: prog
           prog <a> <b>
""")
$ prog 1 2
{"<a>": "1", "<b>": "2"}

$ prog
{"<a>": null, "<b>": null}

let doc = Docopt("""usage: prog <a> <b>
           prog
""")
$ prog
{"<a>": null, "<b>": null}

//
// Option's argument should not capture default value from usage pattern
//

let doc = Docopt("""usage: prog [--file=<f>]""")
$ prog
{"--file": null}

let doc = Docopt("""usage: prog [--file=<f>]

options: --file <a>

""")
$ prog
{"--file": null}

let doc = Docopt("""Usage: prog [-a <host:port>]

Options: -a, --address <host:port>  TCP address [default: localhost:6283].

""")
$ prog
{"--address": "localhost:6283"}

//
// If option with argument could be repeated,
// its arguments should be accumulated into a list
//

let doc = Docopt("""usage: prog --long=<arg> ...""")

$ prog --long one
{"--long": ["one"]}

$ prog --long one --long two
{"--long": ["one", "two"]}

//
// Test multiple elements repeated at once
//

let doc = Docopt("""usage: prog (go <direction> --speed=<km/h>)...""")
$ prog  go left --speed=5  go right --speed=9
{"go": 2, "<direction>": ["left", "right"], "--speed": ["5", "9"]}

//
// Required options should work with option shortcut
//

let doc = Docopt("""usage: prog [options] -a

options: -a

""")
$ prog -a
{"-a": true}

//
// If option could be repeated its defaults should be split into a list
//

let doc = Docopt("""usage: prog [-o <o>]...

options: -o <o>  [default: x]

""")
$ prog -o this -o that
{"-o": ["this", "that"]}

$ prog
{"-o": ["x"]}

let doc = Docopt("""usage: prog [-o <o>]...

options: -o <o>  [default: x y]

""")
$ prog -o this
{"-o": ["this"]}

$ prog
{"-o": ["x", "y"]}

//
// Test stacked option's argument
//

let doc = Docopt("""usage: prog -pPATH

options: -p PATH

""")
$ prog -pHOME
{"-p": "HOME"}

//
// Issue 56: Repeated mutually exclusive args give nested lists sometimes
//

let doc = Docopt("""Usage: foo (--xx=x|--yy=y)...""")
$ prog --xx=1 --yy=2
{"--xx": ["1"], "--yy": ["2"]}

//
// POSIXly correct tokenization
//

let doc = Docopt("""usage: prog [<input file>]""")
$ prog f.txt
{"<input file>": "f.txt"}

let doc = Docopt("""usage: prog [--input=<file name>]...""")
$ prog --input a.txt --input=b.txt
{"--input": ["a.txt", "b.txt"]}

//
// Issue 85: `[options]` shourtcut with multiple subcommands
//

let doc = Docopt("""usage: prog good [options]
           prog fail [options]

options: --loglevel=N

""")
$ prog fail --loglevel 5
{"--loglevel": "5", "fail": true, "good": false}

//
// Usage-section syntax
//

let doc = Docopt("""usage:prog --foo""")
$ prog --foo
{"--foo": true}

let doc = Docopt("""PROGRAM USAGE: prog --foo""")
$ prog --foo
{"--foo": true}

let doc = Docopt("""Usage: prog --foo
           prog --bar
NOT PART OF SECTION""")
$ prog --foo
{"--foo": true, "--bar": false}

let doc = Docopt("""Usage:
 prog --foo
 prog --bar

NOT PART OF SECTION""")
$ prog --foo
{"--foo": true, "--bar": false}

let doc = Docopt("""Usage:
 prog --foo
 prog --bar
NOT PART OF SECTION""")
$ prog --foo
{"--foo": true, "--bar": false}

//
// Options-section syntax
//

let doc = Docopt("""Usage: prog [options]

global options: --foo
local options: --baz
               --bar
other options:
 --egg
 --spam
-not-an-option-

""")
$ prog --baz --egg
{"--foo": false, "--baz": true, "--bar": false, "--egg": true, "--spam": false}

*)

stopwatch.Stop()
printfn "\n>>> %i Docopt calls in %A\nless than %fms per call" count stopwatch.Elapsed (float stopwatch.ElapsedMilliseconds / float count)
