"""Deterministic C# UI literal inventory; no application/solver launch.

Concatenated constant strings are joined; interpolated expressions become {n}.
Every candidate retains source file, line and reason. Nonliteral fragments are
retained for review rather than pretending to infer arbitrary runtime messages.
"""
from pathlib import Path
import re, json, argparse
ROOT=Path(__file__).resolve().parents[2]
PATTERN=r'(?:\$@|@\$|@)"(?:""|[^"])*"|\$?"(?:[^"\\\r\n]|\\.)*"'
STRING=re.compile(PATTERN)
COMMENTS=re.compile(r'//[^\n]*|/\*[\s\S]*?\*/')

def decode(m):
    raw=m[0];pos=raw.index('"');prefix=raw[:pos];body=raw[pos+1:-1]
    if '@' in prefix: body=body.replace('""','"')
    else:
        escapes={'n':'\n','r':'\r','t':'\t','"':'"',"'":"'",'\\':'\\','0':'\0'}
        body=re.sub(r'\\(u[0-9a-fA-F]{4}|.)',lambda x:chr(int(x[1][1:],16)) if x[1].startswith('u') else escapes.get(x[1],x[0]),body)
    if '$' in prefix:
        index=[0]
        def token(m):
            n=index[0];index[0]+=1;return '{'+str(n)+'}'
        body=re.sub(r'(?<!\{)\{[^{}]+\}(?!\})',token,body)
    return body

def arguments(text,start):
    """Split a C# invocation, treating literals and nested calls as opaque."""
    parts=[];begin=start;pos=start;depth=0
    while pos<len(text):
        literal=STRING.match(text,pos)
        if literal:pos=literal.end();continue
        char=text[pos]
        if char in '([{':depth+=1
        elif char in ')]}':
            if depth==0:parts.append(text[begin:pos]);return parts
            depth-=1
        elif char==',' and depth==0:parts.append(text[begin:pos]);begin=pos+1
        pos+=1
    return []

def message_template(expression):
    parts=[];begin=0;pos=0;depth=0
    while pos<len(expression):
        literal=STRING.match(expression,pos)
        if literal:pos=literal.end();continue
        char=expression[pos]
        if char in '([{':depth+=1
        elif char in ')]}':depth-=1
        elif char=='+' and depth==0:parts.append(expression[begin:pos].strip());begin=pos+1
        pos+=1
    parts.append(expression[begin:].strip())
    if not any(STRING.fullmatch(p) for p in parts):return None
    result='';index=0
    for part in parts:
        match=STRING.fullmatch(part)
        if match:result+=decode(match)
        elif part=='Environment.NewLine':result+='\r\n'
        elif not part:return None
        else:result+='{'+str(index)+'}';index+=1
    return result if index else None

def extract():
    records={}
    for folder in ['PrePoMax','UserControls','CaeGlobals','CaeModel','CaeMesh','CaeResults','vtkControl']:
        for p in sorted((ROOT/folder).rglob('*.cs')):
            rel=p.relative_to(ROOT).as_posix()
            if any(x in p.parts for x in ('bin','obj','Properties','Mcp','Rpc','Python','Commands')):continue
            if '/FileInOut/Output/' in rel:continue # solver input is never a translation surface
            s=p.read_text(encoding='utf-8-sig',errors='replace')
            # Mask comments without corrupting URLs or comment characters inside literals.
            mask=re.sub(r'(?P<string>'+PATTERN+r')|(?P<comment>//[^\n]*|/\*[\s\S]*?\*/)',
                lambda m:m[0] if m['string'] is not None else re.sub(r'[^\n]',' ',m[0]),s)
            matches=list(STRING.finditer(mask)); i=0
            for call in re.finditer(r'(?:MessageBoxes\.Show\w*|throw\s+new\s+\w*Exception)\s*\(',mask):
                for argument in arguments(mask,call.end()):
                    value=message_template(argument)
                    if value and re.search('[A-Za-z]{2}',value):
                        records.setdefault(value,[]).append({'file':rel,'line':mask.count('\n',0,call.start())+1,'kind':'message-template'})
            while i<len(matches):
                m=matches[i]; value=decode(m); end=m.end(); i+=1
                while i<len(matches) and re.fullmatch(r'\s*\+\s*',mask[end:matches[i].start()]):
                    value+=decode(matches[i]);end=matches[i].end();i+=1
                if not re.search('[A-Za-z]{2}',value):continue
                before=mask[max(0,m.start()-240):m.start()]
                line=mask[mask.rfind('\n',0,m.start())+1:mask.find('\n',end) if '\n' in mask[end:] else len(mask)]
                reason=None
                if p.name.endswith('Designer.cs') and re.search(r'\.(Text|HeaderText|ToolTipText)\s*=\s*$',before):reason='designer'
                elif re.search(r'(Category|Description|DisplayName|OrderedDisplayName|StandardValue)\s*\([^\[;{}]*$',before):reason='attribute'
                elif re.search(r'(MessageBoxes\.|MessageBox\.|throw new (?:Cae|Argument|InvalidOperation|NotSupported)?Exception)',line):reason='message'
                elif '/Forms/' in rel or folder=='UserControls':
                    if re.search(r'\b(Text|HeaderText|ToolTipText)\s*=|new (?:Label|Button|GroupBox|TabPage|TreeNode|ToolStripMenuItem)|\.SetToolTip\(|\.Columns.Add\(',line):reason='runtime-ui'
                    elif re.search(r'\breturn\s*$',before) and '/Forms/' in rel:reason='display-return'
                    elif re.search(r'new ListViewItem\s*\(|\bName\s*\{.*\breturn\b',line):reason='list-type'
                    elif 'DrawText(' in before or 'DrawString(' in before:reason='custom-draw'
                elif p.name=='Globals.cs' and ('Text' in line or 'Name' in line):reason='status/category'
                # Descriptions/validation in the FE model also reach GUI helpers.
                if not reason and re.search(r'throw new (?:Cae|Argument|InvalidOperation)Exception',line):reason='validation'
                if reason:
                    item={'file':rel,'line':mask.count('\n',0,m.start())+1,'kind':reason}
                    records.setdefault(value,[]).append(item)
    return dict(sorted(records.items()))

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--out',type=Path,default=ROOT/'lang/strings.inventory.json');args=parser.parse_args()
    values=extract();args.out.parent.mkdir(parents=True,exist_ok=True)
    args.out.write_text(json.dumps(values,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    print(len(values),'unique source strings;',sum(map(len,values.values())),'locations ->',args.out)
if __name__=='__main__':main()
