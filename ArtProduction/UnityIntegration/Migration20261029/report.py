from pathlib import Path
import json,hashlib,xml.etree.ElementTree as ET
ROOT=Path(__file__).resolve().parents[3];OUT=Path(__file__).parent
before=json.loads((OUT/'before_hashes.json').read_text())
export=json.loads((OUT/'export.json').read_text())
delivery=json.loads((OUT/'delivered.json').read_text())
dependencies=json.loads((OUT/'dependency_analysis.json').read_text())
tests={}
for mode in ['EditMode','PlayMode']:
 p=ROOT/f'TestResults/AssetMigration-Final-{mode}.xml';r=ET.parse(p).getroot()
 tests[mode]={k:r.attrib[k] for k in ['total','passed','failed','skipped','result']}
 assert r.attrib['failed']=='0' and r.attrib['result']=='Passed'
protected=[p for p in before if '/Core/' in p or '/Application/' in p or 'Star' in p or 'Stone' in p or p=='Assets/Settings/DefaultVolumeProfile.asset']
assert all(hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==before[p] for p in protected)
changed=[p for p,h in before.items() if (ROOT/p).is_file() and hashlib.sha256((ROOT/p).read_bytes()).hexdigest()!=h]
assert all((ROOT/p).exists() for p in before),'Unexpected removed file'
summary={'tests':tests,'deleted_files':[],'protected_unchanged':protected,'changed_existing_files':changed,'delivered':delivery['files'],'exported':export['assets'],'known_preexisting_guid_errors':4}
(OUT/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
lines=['# Block Rift — Unity asset migration, 29 Ekim 2026','',
'Entegrasyon tamamlandı. Temizlik yapılmadı; eski assetler ve .meta dosyaları korunuyor.',
'', '## Aktarım ve bağlantılar',
'- Kaynak: `ArtProduction/AssetPreview/AssetPreview.blend`. Onaylı Blender geometrileri değişmedi.',
'- FBX eksen dönüşümü: Blender Z-up → Unity Y-up, birim ölçek. Unity import globalScale=1, imported normals, Mikk tangents, collider yok.',
'- Mevcut dört blok prefabının GUID ve root kimlikleri korundu; yalnız model çocukları yeni `_Unified.fbx` dosyalarına bağlandı.',
'- `Block_1x2`, `Block_1x3`, `Block_1x4` aynı yatay prefabları sabit 90° yönelimle kullanmayı sürdürüyor; oyuncu rotasyonu eklenmedi.',
'- `Board_6x6_Frameless.prefab` mevcut GUID ile hücreler + ayrı kum meshini kullanıyor. `Board_5x5_Mobile.prefab` gerçek 25 hücreli varyanttır.',
'- BoardRoot, BoardVisualsRoot, PiecesRoot ve authored Level 1 korunuyor. BoardVisualLayout yalnız uygun gerçek tahta prefabını seçiyor.',
'- Hücre üstleri Y=-0.001, altları Y=-0.061; aralıklardaki aynı kum yüzeyi Y=-0.062. Blok tabanı Y=0: hücre üstüne açıklık 0.001; destek paneli yok.',
'- Kumun en yüksek noktası yaklaşık Y=-0.0145; blok yerleşim hacminin altında. Gameplay collider yok.',
'- Hücrelerin eski alt bevel özel normallerinde bulunan yön hataları yalnız FBX export kopyasında düzeltildi (5×5: 2100, 6×6: 3024 köşe normali); kaynak meshler değişmedi.',
'','| Yeni FBX | Üçgen |','|---|---|']
for a in export['assets']:lines.append(f"| `Assets/_BlockRift/Art/Models/Gameplay/{a['asset']}.fbx` | {a['triangles']} |")
lines += ['', '## Materyal ve kamera',
'- Mevcut URP Lit altı renk: Blue #087FFF, Green #12DA45, Red #FF3838, Yellow #FFD12A, Purple #9735F5, Orange #FF9B21. Metallic=0, Smoothness=0.8. Aynı prefab farklı JSON renk anahtarıyla kullanılabilir.',
'- Krem hücre, yıldız ve taş materyalleri hash karşılaştırmasıyla değişmeden korundu. Kumun iki varyantı 1024px renk/normal haritaları, Clamp, Lit, Smoothness=0.17 kullanıyor.',
'- Kamera: orthographic, X=90°, Y/Z=0°, 1080×1920, mevcut safe-area/HUD/kontrol anchorları. Kaydedilmiş 6×6 orthographicSize=6.013605, padding=1.02. 5×5 kadrajı mevcut viewport koduyla hesaplanıyor.',
'- `GameView_1080x1920.png`: Unity ScreenCapture, gerçek Play Mode Game View. İzole doğrulama kopyasındaki aynı entegre Assets ile üretildi; Blender renderı veya mockup değildir.',
'', '## Testler ve doğrulama',
'Ana Unity projesi açık ve kullanıcı değişiklikleri içerdiğinden testler `Temp/AssetMigrationValidation` kopyasında çalıştırıldı. Aktarım/GUID eşlemesinden sonra ana Assets birebir yeniden kopyalandı ve tüm testler tekrar geçti.',
'', '| Son çalışma | Geçen | Hata | Atlanan |','|---|---|---|---|']
for mode,t in tests.items():lines.append(f"| {mode} | {t['passed']} / {t['total']} | {t['failed']} | {t['skipped']} |")
lines += ['', 'Çalıştırılan Unity komutları (Unity 6000.3.23f1):', '```text',
'Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/AssetMigrationValidation -executeMethod BlockRift.Editor.ApprovedAssetMigration.Build -quit -logFile TestResults/AssetMigration-Build5.log',
'Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/AssetMigrationValidation -runTests -testPlatform EditMode -testResults C:/Users/sahba/BlockRift/TestResults/AssetMigration-Final-EditMode.xml',
'Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/AssetMigrationValidation -runTests -testPlatform PlayMode -testResults C:/Users/sahba/BlockRift/TestResults/AssetMigration-Final-PlayMode.xml',
'Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/AssetMigrationValidation -executeMethod BlockRift.Editor.GameplayGameViewCapture.CaptureBatch',
'python .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py . --json', '```',
'İlgili GDD: §3–6 birim grid ve manuel hareket/trigger ayrımı, §7 anında Win, Undo ve Restart. Core ve Application dosyaları görev öncesi hashleriyle aynı; kural değişikliği yapılmadı. PlayMode testleri input/sürükleme, Undo, Restart, Win ve ilerleme akışlarını doğruluyor.',
'', '## Açık bulgular ve kapsam sınırı',
'- GUID checker: 4 hata, 0 uyarı. Dördü de görev öncesinde aynı içerikle bulunan `Assets/Settings/DefaultVolumeProfile.asset` içindeki eksik OutlineVolumeComponent, TestAnimationCurveVolumeComponent, OasisFogVolumeComponent ve TestVolume scriptleridir. Yeni mesh/prefab/scriptlerde checker hatası yok. Temizlik onayı alınmadan bu eski alt assetler düzenlenmedi.',
'- Unity ScreenCapture başarılı oldu; yakalama editörünün SearchDatabase indeksleyicisinde bir kez dahili IndexOutOfRange hatası kaydedildi. Oynanış testlerinde hata yok.',
'- Canlı Unity penceresinden screenshot okumak iki denemede zaman aşımına uğradı; kullanıcı sahnesi UI üzerinden kapatılmadı veya kaydedilmedi. Teslim edilen görüntü doğrulama editörünün gerçek Game View çıktısıdır.',
'- Mobil cihaz/GPU performans profili alınmadı. Triangle bütçesi raporlandı; cihaz FPS iddiası yok.',
'', '## Temizlik için onay bekleyen kapsam',
'Dört eski blok FBX çalışma sahnesi/katalog/prefab graphında kullanılmıyor ve GUID referansları sıfır. Ancak eski `GameplayArtIntegration.Build` bakım komutu adlardan eski FBX yolları üretiyor. Bu yardımcı güncellenmeden bu dosyalar bağımlılıksız kabul edilemez.',
'- `Block_2x1.fbx` + `.meta`', '- `Block_3x1.fbx` + `.meta`', '- `Block_4x1.fbx` + `.meta`', '- `Block_2x2.fbx` + `.meta`',
'Eski `Board_6x6.fbx` / `Board_6x6.prefab` referans karşılaştırma testinde, eski `Board_6x6_Frameless.fbx` bakım komutunda kullanılıyor; korunuyor. Güncel isimlerini koruyan aktif blok prefabları ve Board_6x6_Frameless.prefab kesinlikle silinmeyecek.',
'Detay: `DEPENDENCIES.md`, `dependency_analysis.json`. Henüz dosya taşıma veya silme yapılmadı; onaydan sonra proje dışı yedek hedefiyle ayrıca uygulanmalı.',
'', '## Yedek ve yeniden üretim',
'- `ArtProduction/UnityIntegration/Migration20261029/Before/`: göreve başlamadan önce Assets, Packages ve ProjectSettings dosyalarının 298 dosyalık kopyası. Aktif proje dışında son temizlik yedeği henüz oluşturulmadı; dosya kaldırma aşamasına geçilmedi.',
'- `before_hashes.json`, `export.json`, `delivered.json`, `summary.json`: kaynak, GUID ve değişiklik kayıtları.',
'- `export.py`, `deliver.py`, `dependencies.py`: aktarım ve salt okunur analiz. Silme komutu yok.',
'', '## Bu görevde değişen mevcut dosyalar']
lines += ['- `'+p+'`' for p in changed]
lines += ['', 'Yeni bakım ve test kodları:', '- `Assets/_BlockRift/Editor/ApprovedAssetMigration.cs`', '- `Assets/_BlockRift/Tests/Presentation/ApprovedAssetMigrationTests.cs`',
'Yeni materyal, FBX, doku, prefab ve .meta listesi: `delivered.json` + `export.json`.']
(OUT/'REPORT.md').write_text('\n'.join(lines),encoding='utf-8')
print(json.dumps({'tests':tests,'deleted':0,'changed_existing':len(changed),'known_preexisting_errors':4}))
