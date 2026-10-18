"""Compose unaltered evidence images; assert Windows/Linux numerical parity."""
from pathlib import Path
import json,sys
from PIL import Image,ImageDraw
root=Path(sys.argv[1]);w=json.loads((root/'windows-view.json').read_text());l=json.loads((root/'Linux/results.json').read_text())
assert w['step']==l['step'] and w['increment']==l['increment'] and abs(w['time']-l['time'])<1e-9
for field,key in [('S (Mises)','S'),('U (magnitude)','U')]:
    assert w[key]==[l['fields'][field]['min'],l['fields'][field]['max']],field
for name in ('S','U','U-deformed'):
    images=[Image.open(root/('Windows-'+name+'.png')).convert('RGB'),Image.open(root/'Linux'/ (name+'.png')).convert('RGB')]
    canvas=Image.new('RGB',(2000,790),'#171e2b');draw=ImageDraw.Draw(canvas)
    draw.text((20,14),f'DCB | Front (+Z, up +Y) | {name} | Step {w["step"]}, increment {w["increment"]}, t={w["time"]:.9g}',fill='white')
    draw.text((20,42),'Windows PrePoMax (isolated hidden desktop)',fill='white');draw.text((1020,42),'Linux Avalonia IViewport3D shell (llvmpipe)',fill='white')
    for i,img in enumerate(images):
        img.thumbnail((980,680));canvas.paste(img,(10+i*1000+(980-img.width)//2,74+(680-img.height)//2))
    draw.text((20,760),'Viewer fixture only: original solve stopped before full end load; both views use the same common S/U increment. Raw PNGs retained.',fill='#ffcf7a')
    canvas.save(root/('comparison-'+name+'.png'))
(root/'comparison.json').write_text(json.dumps(dict(standardView='Front +Z/up +Y',step=w['step'],increment=w['increment'],time=w['time'],rangesEqual=True,sourcePmx='dcb.pmx',sourceFrd='dcb.frd',windowsCapture='never-switched hidden desktop',rawImagesRetained=True),indent=2))
print('PASS DCB Windows/Linux exact S/U ranges, common increment and side-by-side images')
