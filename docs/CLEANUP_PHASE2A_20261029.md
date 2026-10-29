# Block Rift — Cleanup Phase 2A — 29 Ekim 2026

Yalnız onaylı ölü bakım kodu temizlendi. Core, Solver, Level Generator, Application, Level 1, gameplay sahnesi, FBX, materyal ve aktif prefab dosyaları değiştirilmedi. Commit/reset/clean kullanılmadı.

## Yedek ve Python kaynak kanıtı

İşlem öncesi 16 dosya ayrı arşivde korundu: `C:\Users\sahba\BlockRift-Archives\CleanupPhase2A_20261029\Before-Phase2A.zip`. SHA-256: `7501222ec904045fa889a3c80f801f4d3175a4a40555849774d87b3aabd4e3f4`. Her üye geri okunarak hash doğrulandı.

Dört Python dosyasının özgün tam kaynakları Phase 1 Rollback.zip üyeleriyle SHA-256 düzeyinde doğrulandı. Arşiv üyelerinin raw hashleri Phase 1 manifestiyle eşleşti. Güncel kapatma satırları çıkarılıp yalnız CRLF/LF normalize edilerek bütün kaynak içeriğinin de aynı olduğu doğrulandı; sonuçlar phase1-python-verification.json içinde.

## Yeniden adlandırma ve çağrılar

`Assets/_BlockRift/Editor/GameplayArtIntegration.cs` → `GameplayArtAssets.cs`; `.meta` dosyası birlikte taşındı ve byte içeriği aynı kaldı. Korunan GUID: `450bd984216ad7d43915272e11cd3249`. Models, Prefabs, Materials, CatalogPath ve MeasureBounds korunur. MeasureBounds gövdesi ve dört sabit önceki metinle aynıdır. Build/FinishMaterials kaldırıldı.

Altı dosyada toplam 27 üye referansı güncellendi:
- `Assets/_BlockRift/Editor/ApprovedAssetMigration.cs`: 6 referans.
- `Assets/_BlockRift/Editor/GameplayGameViewCapture.cs`: 4 referans.
- `Assets/_BlockRift/Editor/LevelOneSetup.cs`: 7 referans.
- `Assets/_BlockRift/Tests/Presentation/GameplayArtIntegrationTests.cs`: 5 referans.
- `Assets/_BlockRift/Tests/Presentation/BoardPerspectiveTests.cs`: 2 referans.
- `Assets/_BlockRift/Tests/Presentation/ApprovedAssetMigrationTests.cs`: 3 referans.

Beş çağıran dosyanın byte karşılaştırması yalnız GameplayArtIntegration. → GameplayArtAssets. değişikliğini doğrular. Altıncı dosya ApprovedAssetMigrationTests ayrıca yalnız dört emeklilik vakasını, onların AssetHashes yardımcısını ve artık kullanılmayan importlarını kaybetti.

## Kaldırılan dosyalar

- `Assets/_BlockRift/Editor/GameplayBoardReplacement.cs`
- `Assets/_BlockRift/Editor/GameplayBoardReplacement.cs.meta`
- `Assets/_BlockRift/Editor/GameplayCameraCorrection.cs`
- `Assets/_BlockRift/Editor/GameplayCameraCorrection.cs.meta`
- `ArtProduction/UnityIntegration/export_approved.py`
- `ArtProduction/UnityIntegration/export_frameless.py`
- `ArtProduction/UnityIntegration/deliver_validated.py`
- `ArtProduction/UnityIntegration/deliver_board_replacement.py`

Toplam 8 dosya kaldırıldı; 2 dosya (helper ve meta) yeniden adlandırıldı. Eski iki C# sınıfına ve eski helper adına Assets içindeki C# kaynaklarından aktif referans kalmadı.

## Testler ve migration

| Çalıştırma | Keşfedilen | Geçen | Hata | Atlanan |
|---|---:|---:|---:|---:|
| CSharp-Presentation | 144 | 144 | 0 | 0 |
| Migration-EditMode | 7 | 7 | 0 | 0 |
| Final-EditMode | 371 | 371 | 0 | 0 |
| Final-PlayMode | 16 | 16 | 0 | 0 |

Phase 1 XMLindeki 375 EditMode vakasıyla son XML karşılaştırıldı: aşağıdaki dört vaka dışında kayıp vaka yok, ek vaka yok. Sayı hedefe uydurulmadı; gerçek keşif sonucu raporlandı.

- `BlockRift.Tests.ApprovedAssetMigrationTests.RetiredMaintenanceCommandsRejectWithoutChangingAssets("GameplayArtIntegration","Build")`
- `BlockRift.Tests.ApprovedAssetMigrationTests.RetiredMaintenanceCommandsRejectWithoutChangingAssets("GameplayArtIntegration","FinishMaterials")`
- `BlockRift.Tests.ApprovedAssetMigrationTests.RetiredMaintenanceCommandsRejectWithoutChangingAssets("GameplayBoardReplacement","Apply")`
- `BlockRift.Tests.ApprovedAssetMigrationTests.RetiredMaintenanceCommandsRejectWithoutChangingAssets("GameplayCameraCorrection","Apply")`

Güncel ApprovedAssetMigration.Build yalnız izole Temp/CleanupPhase2AValidation kopyasında çalıştı, exit 0 ve tamamlanma işareti doğrulandı. Prefab metadata hashleri başlangıçla aynı kaldı. Ardından 7 asset kabul vakası geçti. Son tam testlerden önce ana Assets/Packages/ProjectSettings tekrar kopyalandı; 319 giriş dosyasının hashleri ana projeyle doğrulandı. Ana sahneye veya prefablarına test çıktısı aktarılmadı.

Unity 6000.3.23f1 ile gerçekte yürütülen komutlar (tam log/XML dizini: C:/Users/sahba/BlockRift/TestResults/CleanupPhase2A_20261029):

```text
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase2AValidation -runTests -testPlatform EditMode -assemblyNames BlockRift.Presentation.EditModeTests -testResults .../CSharp-Presentation.xml -logFile .../CSharp-Presentation.log
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase2AValidation -executeMethod BlockRift.Editor.ApprovedAssetMigration.Build -quit -logFile .../Migration.log
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase2AValidation -runTests -testPlatform EditMode -testFilter BlockRift.Tests.ApprovedAssetMigrationTests -testResults .../Migration-EditMode.xml -logFile .../Migration-EditMode.log
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase2AValidation -runTests -testPlatform EditMode -testResults .../Final-EditMode.xml -logFile .../Final-EditMode.log
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase2AValidation -runTests -testPlatform PlayMode -testResults .../Final-PlayMode.xml -logFile .../Final-PlayMode.log
python -B .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py . --json
python -B TestResults/CleanupPhase2A_20261029/verify.py
```

GUID kontrolü: 292 dosya; başlangıçla aynı dört DefaultVolumeProfile.asset eksik script hatası (78, 119, 264, 832), sıfır yeni hata ve sıfır uyarı. Bu eski hatalara dokunulmadı.

## Kullanıcı geliştirmelerinin korunması

760 başlangıç kaydının 744 dosyası hash olarak değişmedi; kalan 16 yol yalnız önceden yedeklenen onaylı kapsamdır. Kullanıcının mevcut değişiklikleri HEAD ile değiştirilmedi; bu iş başlangıcındaki dosyalarla karşılaştırıldı. Core/Application/Presentation, level içerikleri, bütün Art assetleri, ProjectSettings, Packages, diğer Blender/migration kaynakları ve Phase 1 teslim raporu aynı kaldı. Phase 1 TestResults kayıtlarına yazılmadı. İlgili GDD §11 katman sınırı ve ortak deterministik kural motoru korunur; gameplay değişmedi.

## Bu aşamada ele alınmayan legacy içerik

- Migration20261029/dependencies.py, report.py, deliver.py ve tarihsel DEPENDENCIES.md korundu; eski varsayımları Phase 2 analizinde açıklandı.
- LevelOneSetup.Build içindeki materyal yazma davranışı ve LevelOnePalette korundu; yalnız helper referansları değişti.
- Eski board dosyaları ve karşılaştırma testleri korundu.
- Diğer Blender, board ve texture üretim araçlarına dokunulmadı.
- GameplayArtIntegrationTests test sınıfı adı korunur; yeniden adlandırma kapsamda değildi. Bu sınıf eski bakım girişini çağırmaz, gerçek assetleri doğrular.

## Git kapanış görüntüsü

195 dosya; {' M': 44, ' D': 22, '??': 129}. Başlangıç: 189 dosya (51 M, 12 D, 126 untracked). Unstaged yeniden adlandırma Git status içinde eski yol D ve yeni yol untracked olarak ayrı görünür.

Bu görev için oluşturulan geçici CleanupPhase2AValidation kopyası doğrulamadan sonra kaldırıldı. XML, log, yedek manifesti ve hash kanıtları ayrı TestResults/CleanupPhase2A_20261029 klasöründe korundu. Diğer doğrulama kopyalarına dokunulmadı.
