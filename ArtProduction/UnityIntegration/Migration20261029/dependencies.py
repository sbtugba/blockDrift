"""Read-only GUID graph. Nothing is deleted, including legacy models and .meta files."""
from pathlib import Path
import re,json
ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).parent
guid_re=re.compile(r'guid:\s*([0-9a-f]{32})')
paths={};texts={};graph={};reverse={}
for meta in (ROOT/'Assets').rglob('*.meta'):
 match=re.search(r'^guid: ([0-9a-f]{32})',meta.read_text(encoding='utf-8-sig'),re.M)
 if match:paths[match[1]]=meta.with_suffix('').relative_to(ROOT).as_posix()
known=set(paths.values())
for file in (ROOT/'Assets').rglob('*'):
 if not file.is_file() or file.suffix.lower() in {'.fbx','.png','.jpg','.ttf','.dll'}:continue
 try:body=file.read_text(encoding='utf-8-sig')
 except (UnicodeError,OSError):continue
 rel=file.relative_to(ROOT).as_posix();texts[rel]=body
 refs={paths[g] for g in guid_re.findall(body) if g in paths}
 graph[rel]=refs
 for target in refs:
  if target!=rel.removesuffix('.meta'):reverse.setdefault(target,set()).add(rel)
roots=['Assets/Scenes/BlockRift_Gameplay.unity','Assets/_BlockRift/Content/GameplayVisualCatalog.asset']
roots += [p for p in known if '/Resources/' in p]
active=set();stack=roots.copy()
while stack:
 p=stack.pop()
 if p in active:continue
 active.add(p)
 stack.extend(graph.get(p,set()));stack.extend(graph.get(p+'.meta',set())-{p})
candidates=[]
names=['Block_2x1','Block_3x1','Block_4x1','Block_2x2','Board_6x6','Board_6x6_Frameless']
for name in names:
 for ext,folder in [('.fbx','Models'),('.prefab','Prefabs')]:
  p=f'Assets/_BlockRift/Art/{folder}/Gameplay/{name}{ext}'
  if p not in known:continue
  literal=[r for r,t in texts.items() if r.endswith('.cs') and (name+ext) in t]
  # The legacy build command concatenates Models + Ids[i] + '.fbx'; GUID-only scans miss it.
  if ext=='.fbx' and name in ['Block_2x1','Block_3x1','Block_4x1','Block_2x2','Board_6x6']:
   literal.append('Assets/_BlockRift/Editor/GameplayArtIntegration.cs (legacy Build: Models + Ids[i] + .fbx)')
  refs=sorted(reverse.get(p,set()))
  candidates.append({'path':p,'guid':next(g for g,v in paths.items() if v==p),'active_dependency':p in active,'guid_references':refs,'literal_test_or_editor_references':literal,'decision':'KEEP_ACTIVE' if p in active else ('KEEP_REVIEW_DEPENDENCIES' if refs or literal else 'UNREFERENCED_CANDIDATE_REQUIRES_APPROVAL')})
result={'deleted_files':[],'roots':roots,'active_assets':len(active),'candidates':candidates}
(OUT/'dependency_analysis.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
lines=['# Eski asset bağımlılık analizi','Hiçbir dosya veya .meta silinmedi. Aday listesi onay içindir.','', '| Dosya | Aktif bağımlılık | Karar |','|---|---|---|']
for c in candidates:lines.append(f"| `{c['path']}` | {c['active_dependency']} | {c['decision']} |")
for c in candidates:
 lines += ['',f"## {c['path']}",f"GUID: `{c['guid']}`",'GUID referansları: '+(', '.join(c['guid_references']) or 'Yok'),'Kod/test yol referansları: '+(', '.join(c['literal_test_or_editor_references']) or 'Yok')]
(OUT/'DEPENDENCIES.md').write_text('\n'.join(lines),encoding='utf-8')
print(json.dumps(result,indent=2))
