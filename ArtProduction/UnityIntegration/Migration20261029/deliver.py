"""Apply only validated assets, preserve existing GUIDs and concurrent user edits."""
from pathlib import Path
import json,re,hashlib,shutil
ROOT=Path(__file__).resolve().parents[3]
STAGE=ROOT/'Temp/AssetMigrationValidation';OUT=Path(__file__).parent
before=json.loads((OUT/'before_hashes.json').read_text())
mapping={}
guid_re=re.compile(r'^guid: ([0-9a-f]{32})',re.M)
for meta in (STAGE/'Assets').rglob('*.meta'):
 target=ROOT/meta.relative_to(STAGE)
 if target.exists():
  a=guid_re.search(meta.read_text(encoding='utf-8-sig'));b=guid_re.search(target.read_text(encoding='utf-8-sig'))
  if a and b and a[1]!=b[1]:mapping[a[1]]=b[1]
files=['Assets/Scenes/BlockRift_Gameplay.unity','Assets/_BlockRift/Content/GameplayVisualCatalog.asset']
art='Assets/_BlockRift/Art/'
files += [art+'Prefabs/Gameplay/'+n+'.prefab' for n in ['Block_2x1','Block_3x1','Block_4x1','Block_2x2','Board_6x6_Frameless','Board_5x5_Mobile']]
files += [art+'Materials/Gameplay/BR_Plastic_'+n+'.mat' for n in ['Blue','Green','Red','Yellow','Purple','Orange']]
files += [art+f'Materials/Gameplay/MAT_Mobile_Sand_{n}.mat' for n in [5,6]]
# Every old destination must still match the pre-task snapshot.
for rel in files:
 if rel in before:assert hashlib.sha256((ROOT/rel).read_bytes()).hexdigest()==before[rel],f'Concurrent edit: {rel}'
files += [str(p.relative_to(STAGE)).replace('\\','/') for p in (STAGE/(art+'Models/Gameplay')).glob('*.fbx.meta') if any(s in p.name for s in ['_Unified','_Cells','Sand_BoardTransition'])]
files += [str(p.relative_to(STAGE)).replace('\\','/') for p in (STAGE/(art+'Textures/Sand')).rglob('*.meta')]
files += [r+'.meta' for r in list(files) if not r.endswith('.meta') and (STAGE/(r+'.meta')).exists() and not (ROOT/(r+'.meta')).exists()]
for rel in files:
 source=STAGE/rel;dest=ROOT/rel;dest.parent.mkdir(parents=True,exist_ok=True)
 body=source.read_text(encoding='utf-8-sig')
 for old,new in mapping.items():body=body.replace(old,new)
 dest.write_text(body,encoding='utf-8',newline='\n')
# New script meta GUIDs are adopted only if the main Editor has not generated them yet.
for rel in ['Assets/_BlockRift/Editor/ApprovedAssetMigration.cs.meta','Assets/_BlockRift/Tests/Presentation/ApprovedAssetMigrationTests.cs.meta']:
 if not (ROOT/rel).exists():shutil.copy2(STAGE/rel,ROOT/rel);files.append(rel)
(OUT/'delivered.json').write_text(json.dumps({'files':files,'guid_remaps':mapping,'deleted':[]},indent=2))
print(json.dumps({'delivered':len(files),'preserved_guid_remaps':len(mapping),'deleted':0}))
