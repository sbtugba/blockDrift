"""Doğrulanmış yeni board/sahneyi mevcut model ve script GUID'lerini koruyarak aktarır."""
from pathlib import Path
import hashlib, json, re, shutil

root=Path(__file__).resolve().parents[2]
stage=root/'Temp/GameplayFoundationValidation'
scene=Path('Assets/Scenes/BlockRift_Gameplay.unity')
before=root/'TestResults/BoardPerspective/scene-before.unity'
assert hashlib.sha256((root/scene).read_bytes()).digest()==hashlib.sha256(before.read_bytes()).digest(), 'Concurrent scene change; refusing overwrite'
catalog=Path('Assets/_BlockRift/Content/GameplayVisualCatalog.asset')
assert (root/catalog).read_bytes()==(root/'TestResults/BoardPerspective/catalog-before.asset').read_bytes(), 'Concurrent catalog change'
guid_re=re.compile(r'^guid: ([a-f0-9]{32})',re.M)
mapping={}
for main in (root/'Assets/_BlockRift').rglob('*.meta'):
    other=stage/main.relative_to(root)
    if other.exists():
        a=guid_re.search(main.read_text()).group(1);b=guid_re.search(other.read_text()).group(1)
        if a!=b:mapping[b]=a

files=[Path('Assets/_BlockRift/Art/Prefabs/Gameplay/Board_6x6_Frameless.prefab.meta'),
       Path('Assets/_BlockRift/Art/Prefabs/Gameplay/Board_6x6_Frameless.prefab'),
       Path('Assets/_BlockRift/Art/Models/Gameplay/Board_6x6_Frameless.fbx.meta'),catalog,scene]
for relative in files:
    text=(stage/relative).read_text(encoding='utf-8-sig')
    for old,new in mapping.items():text=text.replace(old,new)
    (root/relative).write_text(text,encoding='utf8',newline='\n')
shutil.copy2(stage/'docs/Gameplay_6x6_GameView.png',root/'docs/Gameplay_6x6_Perspective.png')
shutil.copy2(stage/'TestResults/ArtIntegration/gameview.json',root/'TestResults/BoardPerspective/gameview.json')
(root/'TestResults/BoardPerspective/guid-remapping.json').write_text(json.dumps(mapping,indent=2))
before_pieces=json.loads((root/'TestResults/BoardPerspective/piece-prefab-hashes-before.json').read_text(encoding='utf-8-sig'))
assert all(hashlib.sha256(Path(e['Path']).read_bytes()).hexdigest().upper()==e['Hash'] for e in before_pieces)
print(json.dumps({'delivered_files':len(files),'piece_prefabs_unchanged':True,'guid_remaps':len(mapping)}))
