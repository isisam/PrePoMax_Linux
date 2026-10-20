# NCalc 1.3.8 - RC packaging build

This is the upstream NCalc v1.3.8 source snapshot already available in the
development inputs. It retains the 1.3.8.0 assembly version and all evaluation
logic. The generated lexer's and parser's GrammarFileName properties return
`NCalc.g`, removing a historical developer's absolute profile path.

RC1 builds this source as an unsigned NCalc assembly and rebuilds CaeGlobals
against it. This avoids binary modification of the signed NuGet NCalc assembly.
The public teaching build restores the unmodified Antlr 3.1.3.42154 NuGet
package instead of the excluded historical DLL. This keeps the 3.1.3 runtime
API and preserves the original BSD notice. Linux expression checks cover the
restored dependency. No private runtime copy is required.
