# Block Rift — Cleanup Phase 2B: Material Ownership — 29 Ekim 2026

## Analiz ve kapsam

LevelOneSetup.Build altı renk materyalini eski hex değerleri ve .78 smoothness ile yazıyordu. Material helper eksik materyali new Material/CreateAsset ile oluşturuyor; shader, BaseColor, Metallic ve Smoothness alanlarını değiştiriyordu. Exit aynı helper üzerinden .75 smoothness ve eski renk alıyor, ardından _EMISSION keyword ve _EmissionColor yeniden yazılıyordu.

Renderer.sharedMaterial/sharedMaterials atamaları materyal assetinin içeriğini değiştirmez; görsel bağlama sorumluluğu olarak korundu. Palette ScriptableObject oluşturma ve EditorUtility.SetDirty(palette) materyal oluşturma/değiştirme değildir; korundu.

LevelOnePalette, Level 1 pieceId → assetId/material bağlarını ve altı renk dizisini taşır. GameplayVisualCatalog aynı materyal GUIDlerini genel renk anahtarlarıyla (blue, green, red, yellow, purple, orange) eşler. Exit prefab referansları aynıdır. Runtime JSON görsel eşlemesi katalogdan gelir; palette Editor kurulumu içindir. Bu veri modeli yeniden tasarlanmadı.

Mevcut testler geometri, URP shader, prefab ve runtime renk eşlemelerini doğruluyordu; LevelOneSetup.Build/RefreshPuzzle materyal değişmezliğini doğrudan test etmiyordu. Üç yeni vaka bu boşluğu kapatır.

## Sınırlı değişiklik

- Assets/_BlockRift/Editor/LevelOneSetup.cs: Material(name,hex,smoothness) yerine salt okunur RequireMaterial(name). Eksikte açık InvalidOperationException; otomatik materyal üretimi, shader/color/float/keyword yazma yok.
- Altı renk ve exit mevcut materyallerden yüklenir. Eski hex dizisi ve exit emission yazmaları kaldırıldı.
- Assets/_BlockRift/Tests/Presentation/ApprovedAssetMigrationTests.cs: gerçek Build/RefreshPuzzle çağrılarında bütün materyal byte/hashleri, bellekteki serialized materyal değerleri, prefab metadata ve palette–katalog referansları denetlenir. Eksik materyalin oluşturulmadığı da denetlenir.

RefreshPuzzle, BindApprovedPalette, EnsureVerticalPrefabs ve Label gövdeleri başlangıç metniyle aynı kaldı. Build içindeki diğer kurulum/prefab işlemleri yeniden tasarlanmadı. ApprovedAssetMigration, diğer migration/Blender/board scriptleri değişmedi.

## Yedek ve mevcut materyal değerleri

İlgili Assets/Packages/ProjectSettings dosyalarının 319 kayıtlık yedeği: `C:\Users\sahba\BlockRift-Archives\CleanupPhase2B_20261029\Before-Phase2B.zip`. Arşiv SHA-256: `3c106fd218eac27f9936be1323ad00c139c4787b63c62570da15b473f9efd70e`. Üyeler kaynak hashleriyle geri okunarak doğrulandı.

16 mevcut materyalin tüm başlangıç değerleri ve SHA-256 hashleri TestResults/CleanupPhase2B_20261029/materials-before.json içinde kaydedildi. Son hashler materials-after.json içinde. Tüm 16 hash aynı; yeni veya kaldırılmış materyal yok. Altı renk smoothness=.8; exit=.75 olarak korundu. Exit keyword listesi boşken _EmissionColor=(0,.35,.3,1) olarak kaydedildi ve değiştirilmedi. Diskte kaydedilmiş değerler esas alındı; ana Unity oturumuna müdahale edilmedi.

## Gerçekte yürütülen doğrulamalar

| Çalıştırma | Keşfedilen | Geçen | Hata | Atlanan |
|---|---:|---:|---:|---:|
| MaterialOwnership | 10 | 10 | 0 | 0 |
| Final-EditMode | 374 | 374 | 0 | 0 |
| Final-PlayMode | 16 | 16 | 0 | 0 |
| Migration-EditMode | 10 | 10 | 0 | 0 |

İlk deneme 9/10 geçti; Build testinin finally bloğu batch başlangıcındaki boş SceneManagerSetup durumunu geri yükleyemedi. Üretim materyal/hash assertionları bu hatadan önce geçmişti. Yalnız test temizliği, boş başlangıçta boş sahne açacak şekilde düzeltildi. İlk başarısız XML/log MaterialOwnership-Initial.* olarak korundu. Son tam pakette dosya kontrollerine ek olarak bellekteki serialized materyal değerleri de denetlendi.

Phase 2A XMLiyle karşılaştırma: üç yeni vaka eklendi, önceki vakalardan hiçbiri silinmedi. Yeni vakalar:
- `BlockRift.Tests.ApprovedAssetMigrationTests.LevelOneSetupPreservesExistingMaterials("Build")`
- `BlockRift.Tests.ApprovedAssetMigrationTests.LevelOneSetupPreservesExistingMaterials("RefreshPuzzle")`
- `BlockRift.Tests.ApprovedAssetMigrationTests.MissingSetupMaterialFailsWithoutCreatingAnAsset`

Güncel ApprovedAssetMigration.Build yalnız Temp/CleanupPhase2BValidation izole kopyasında başarıyla çalıştı. Sonrasında migration/ownership testleri tekrar geçti. Migration çıktısı ana projeye aktarılmadı; ana materyal değerleri korunur. Ana kaynakların 319 giriş hashinin değişmediği doğrulandı.

Unity 6000.3.23f1 ile gerçek komutlar; .../ öneki C:/Users/sahba/BlockRift/TestResults/CleanupPhase2B_20261029/:

```text
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase2BValidation -runTests -testPlatform EditMode -testFilter BlockRift.Tests.ApprovedAssetMigrationTests -testResults .../MaterialOwnership.xml -logFile .../MaterialOwnership.log
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase2BValidation -runTests -testPlatform EditMode -testResults .../Final-EditMode.xml -logFile .../Final-EditMode.log
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase2BValidation -runTests -testPlatform PlayMode -testResults .../Final-PlayMode.xml -logFile .../Final-PlayMode.log
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase2BValidation -executeMethod BlockRift.Editor.ApprovedAssetMigration.Build -quit -logFile .../Migration.log
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase2BValidation -runTests -testPlatform EditMode -testFilter BlockRift.Tests.ApprovedAssetMigrationTests -testResults .../Migration-EditMode.xml -logFile .../Migration-EditMode.log
python -B .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py . --json
```

GUID kontrolü başlangıçla aynı dört DefaultVolumeProfile eksik script hatasını bildirdi; yeni hata ve uyarı yok. Bu bilinen hatalar kapsam dışı korundu.

## Güvenlik ve kalan sınırlar

748 başlangıç kaynak kaydından yalnız iki C# dosyası değişti; diğer 746 dosya hash olarak aynı. Core, Solver, Application, runtime Presentation, Level 1, gameplay sahnesi, prefablar/GUIDleri, FBX ve materyaller değişmedi. Kullanıcı geliştirmeleri HEAD'e döndürülmedi. İlgili GDD §11 katman sınırı korunur; gameplay kuralı değişmedi.

ApprovedAssetMigration tek mevcut sanat materyali düzenleme yolu olarak korundu. Bu komutun ana projede yeniden çalıştırılması kendi onaylı renk ayarlarını uygular; bu görev komutu yalnız izole kopyada doğruladı. Diğer legacy konular ele alınmadı. Commit/reset/clean kullanılmadı.

Git kapanış görüntüsü: 196 kayıt; {' M': 44, ' D': 22, '??': 130}. Başlangıç 195 kayıttı; yeni rapor untracked olarak eklenir.

Bu görevin geçici doğrulama projesi kaldırıldı. XML/log ve SHA-256 kanıtları TestResults/CleanupPhase2B_20261029 altında korundu; diğer doğrulama kopyalarına dokunulmadı.
