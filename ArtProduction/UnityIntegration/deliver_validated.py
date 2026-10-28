"""İzole Unity çıktısını mevcut GUID'leri koruyarak ana projeye aktarır."""
from pathlib import Path
import hashlib, json, re, shutil

root = Path(__file__).resolve().parents[2]
stage = root / 'Temp/GameplayFoundationValidation'
scene = Path('Assets/Scenes/BlockRift_Gameplay.unity')
before = root/'TestResults/ArtIntegration/scene-before.unity'
assert hashlib.sha256((root/scene).read_bytes()).digest() == hashlib.sha256(before.read_bytes()).digest(), 'Scene changed during task'
guid_re = re.compile(r'^guid: ([a-f0-9]{32})', re.M)
mapping = {}
for main_meta in (root/'Assets/_BlockRift').rglob('*.meta'):
    other = stage/main_meta.relative_to(root)
    if other.exists():
        a = guid_re.search(main_meta.read_text()).group(1)
        b = guid_re.search(other.read_text()).group(1)
        if a != b: mapping[b] = a

def write(relative):
    source, target = stage/relative, root/relative
    target.parent.mkdir(parents=True, exist_ok=True)
    if source.suffix in {'.meta','.asset','.mat','.prefab','.unity'}:
        value = source.read_text(encoding='utf-8-sig')
        for old, new in mapping.items(): value = value.replace(old,new)
        target.write_text(value,encoding='utf8',newline='\n')
    else: shutil.copy2(source,target)

for folder in ['Assets/_BlockRift/Art/Prefabs','Assets/_BlockRift/Art/Materials','Assets/_BlockRift/Content']:
    for file in (stage/folder).rglob('*'):
        if file.is_file(): write(file.relative_to(stage))
    meta = Path(folder+'.meta')
    if (stage/meta).exists() and not (root/meta).exists(): write(meta)
for file in (stage/'Assets/_BlockRift/Art/Models/Gameplay').glob('*.fbx.meta'):
    write(file.relative_to(stage))
for name in ['Stone_1x1_Surface.png.meta','Stone_1x1_Normal.png.meta']:
    write(Path('Assets/_BlockRift/Art/Textures/GameplayPreview')/name)
write(scene)
shutil.copy2(stage/'docs/Gameplay_6x6_GameView.png',root/'docs/Gameplay_6x6_GameView.png')
shutil.copy2(stage/'TestResults/ArtIntegration/gameview.json',root/'TestResults/ArtIntegration/gameview.json')
(root/'TestResults/ArtIntegration/guid-remapping.json').write_text(json.dumps(mapping,indent=2))
print(json.dumps({'copied':'validated prefabs/materials/catalog/scene/texture settings/Game View','preserved_guid_remaps':len(mapping)}))
