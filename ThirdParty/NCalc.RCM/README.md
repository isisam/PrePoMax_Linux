# NCalc 1.3.8 - RC packaging build

This is the upstream NCalc v1.3.8 source snapshot already available in the
development inputs. It retains the 1.3.8.0 assembly version and all evaluation
logic. The generated lexer's and parser's GrammarFileName properties return
`NCalc.g`, removing a historical developer's absolute profile path.

RC1 builds this source as an unsigned NCalc assembly and rebuilds CaeGlobals
against it. This avoids binary modification of the signed NuGet NCalc assembly.
Antlr3.Runtime assembly version 3.1.3.22795 is supplied beside it, retaining its original identity.
No Linux implementation or host is part of this project.
