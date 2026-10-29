"""2x2 unified topology system applied to approved 2x1, 3x1 and 4x1 profiles."""
from pathlib import Path
root=Path(__file__).parents[2]
template=(Path(__file__).parent/'create.py').read_text(encoding='utf-8')
for width in (2,3,4):
    label=f'{width}x1'
    out=root/f'Block_{label}'/'UnifiedRevision'
    out.mkdir(parents=True,exist_ok=True)
    code=template.replace('OUT=Path(__file__).parent',f'OUT=Path({str(out)!r})')
    code=code.replace('Block_2x2',f'Block_{label}')
    if width==3:
        code=code.replace("bpy.data.objects['Block_3x1_Export'].copy()","bpy.data.objects['Block_3x1'].copy()")
    code=code.replace('sx=(.5,-.5)',f'sx=({(width-1)/2},-{(width-1)/2})')
    code=code.replace('((-.5,.5) if abs(v.x)<1e-7 else (.5 if v.x>0 else -.5,))',f'((-{(width-1)/2},{(width-1)/2}) if abs(v.x)<1e-7 else ({(width-1)/2} if v.x>0 else -{(width-1)/2},))')
    start=code.index('  sy=');end=code.index('\n  for dx',start)
    code=code[:start]+'  sy=(0.,)'+code[end:]
    code=code.replace('centers=[(-.5,-.5),(.5,-.5),(-.5,.5),(.5,.5)]',f'centers={[(i-(width-1)/2,0.) for i in range(width)]!r}')
    code=code.replace('scale=2.65',f'scale={width+0.7}')
    code=code.replace('s.render.resolution_x=1000;s.render.resolution_y=1000','s.render.resolution_x=1200;s.render.resolution_y=800')
    code=code.replace(" o.hide_render=True;old.hide_render=False;render('Before_'+label,loc);o.hide_render=False;old.hide_render=True",' # The protected original remains hidden in this review scene.')
    code=code.replace('bpy.data.texts.load(str(Path(__file__)))','bpy.data.texts.load(str(OUT/"create.py"))')
    (out/'create.py').write_text(code,encoding='utf-8')
    exec(compile(code,str(out/'create.py'),'exec'),{'__file__':str(out/'create.py')})
