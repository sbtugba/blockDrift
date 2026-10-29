# Block Rift — Safe Cleanup Phase 1 — 29 Ekim 2026

Kapsam: eski entegrasyon girişlerini emekliye ayırma, dört eski blok FBX zincirini ve iki materyal varyantını kaldırma, Before yedeğini proje dışına arşivleme. Genel refactoring veya yeni özellik yapılmadı. Commit/reset/clean kullanılmadı.

## Geri dönüş ve arşivler

Arşiv hedefi: `C:/Users/sahba/BlockRift-Archives/CleanupPhase1_20261029/`. İlk arşiv, işlem öncesi çalışma dosyalarını; tracked, untracked ve ilgili ignore edilmiş sanat kaynaklarını içerir. Git deposu ve kullanıcıya ait diğer doğrulama kopyaları değiştirilmedi.

| Arşiv | Dosya | SHA-256 |
|---|---:|---|
| `Rollback.zip` | 1072 | `95d5bb0d309015e52152a922994ad77552600b0f5a482c812859a2b5cb3caa7b` |
| `Migration20261029-Before.zip` | 298 | `4ee3bce8e556293cb887e35922c4b3a485277e26d8c887f28660c156503b36ee` |
| `RetiredAssets.zip` | 12 | `253a90badbdc7f386c678b28bf9edcb1b2534bced7662856a7b6b25639d3232e` |

Her arşiv oluşturulduktan sonra bütün üyeler SHA-256 ile geri okunarak doğrulandı. Kaynak kaldırılmadan önce kaynak hashleri ve arşiv SHA-256 değeri yeniden karşılaştırıldı; kapanışta arşivlerin bütün üyeleri tekrar doğrulandı.

## Emekliye ayrılan komutlar

- `GameplayArtIntegration.Build` ve `FinishMaterials`: hiçbir assete erişmeden NotSupportedException verir. Ortak Models/Prefabs/Materials/CatalogPath sabitleri ve MeasureBounds aynen korunur.
- `GameplayBoardReplacement.Apply` ve `GameplayCameraCorrection.Apply`: eski model veya perspektif kamera yazma gövdeleri kaldırıldı; açıklayıcı yan etkisiz hata ve mevcut sınıf/.meta kimlikleri korunur.
- `ArtProduction/UnityIntegration/export_approved.py`, `export_frameless.py`, `deliver_validated.py`, `deliver_board_replacement.py`: eski yazma yolları importtan/yazmadan önce RuntimeError ile durur. Tarihsel üretim kaynakları dosyada korunur.
- `ApprovedAssetMigration.cs` değişmedi. Güncel Unity entegrasyon komutu `BlockRift.Editor.ApprovedAssetMigration.Build`; güncel Blender export kaynağı `ArtProduction/UnityIntegration/Migration20261029/export.py` korundu.

## Kaldırılan assetler

- `Assets/_BlockRift/Art/Materials/Gameplay/BR_Neutral_Candy.002.mat`
- `Assets/_BlockRift/Art/Materials/Gameplay/BR_Neutral_Candy.002.mat.meta`
- `Assets/_BlockRift/Art/Materials/Gameplay/BR_Neutral_Candy.003.mat`
- `Assets/_BlockRift/Art/Materials/Gameplay/BR_Neutral_Candy.003.mat.meta`
- `Assets/_BlockRift/Art/Models/Gameplay/Block_2x1.fbx`
- `Assets/_BlockRift/Art/Models/Gameplay/Block_2x1.fbx.meta`
- `Assets/_BlockRift/Art/Models/Gameplay/Block_2x2.fbx`
- `Assets/_BlockRift/Art/Models/Gameplay/Block_2x2.fbx.meta`
- `Assets/_BlockRift/Art/Models/Gameplay/Block_3x1.fbx`
- `Assets/_BlockRift/Art/Models/Gameplay/Block_3x1.fbx.meta`
- `Assets/_BlockRift/Art/Models/Gameplay/Block_4x1.fbx`
- `Assets/_BlockRift/Art/Models/Gameplay/Block_4x1.fbx.meta`

Toplam 12 asset/meta dosyası, 4,101,253 bayt. GUID ve dosya yolu referansları temizleme öncesinde sıfır; eski dinamik C# yol üretimi kaldırıldı. Eski Python yazma girişleri yan etkisiz kapatıldı. Son taramada kaldırılmış GUID referansı yok.

## Taşınan yedek

`ArtProduction/UnityIntegration/Migration20261029/Before/` → proje dışındaki `Migration20261029-Before.zip`. 298 dosya / 10.511.350 bayt; ignore edilmiş 17 dosya da dahil. Doğrulamadan önce hiçbir kaynak silinmedi. Diğer doğrulama kopyalarına dokunulmadı.

## Korunan bağımlılıklar ve kurallar

Core, Solver, Level Generator, Application, Presentation, Level 1 içerikleri, sahne, aktif prefablar/GUIDler ve güncel _Unified FBX modelleri değişmedi. 148 korunan dosyada başlangıca göre hash farkı yok. Eski board FBX/prefab dosyaları ve bunları kullanan karşılaştırma testi korundu. Aktif BR_Neutral_Candy ve güncel migration kaynakları korundu.

İlgili GDD: §3.3 şekiller ve manuel eksenler; §4 manuel hareket; §5–6 trigger/cascade; §7 anında Win; §11 Core/sunum sınırı. Bu temizlik oyun kuralı veya sahne değişikliği yapmaz.

## Gerçekte çalıştırılan doğrulamalar

| Çalıştırma | Başarılı | Hata | Atlanan |
|---|---:|---:|---:|
| Commands-EditMode | 11 | 0 | 0 |
| Models-EditMode | 18 | 0 | 0 |
| Materials-EditMode | 18 | 0 | 0 |
| Migration-EditMode | 11 | 0 | 0 |
| Final-EditMode | 375 | 0 | 0 |
| Final-PlayMode | 16 | 0 | 0 |

Dört eski Python girişi runpy ile ayrı ayrı çalıştırıldı: hepsi ilk hata ile durdu, Assets dosya hashleri değişmedi. Yeni dört NUnit vakası eski C# girişlerini reflection ile çağırıp NotSupportedException ve bütün Assets hashlerinin değişmezliğini doğrular.

Migration, yalnız bu görev için oluşturulan `Temp/CleanupPhase1Validation` kopyasında başarıyla çalıştı: exit 0 ve APPROVED_ASSET_MIGRATION_COMPLETE. Tüm 15 prefab metadata hashinin korunduğu doğrulandı; ardından 11 migration testi geçti. Son tam testlerden önce ana Assets/Packages/ProjectSettings yeniden kopyalandı ve 323 kaynak dosyanın ana proje hashleri giriş manifestiyle karşılaştırıldı. Ana sahneye migration çıktısı aktarılmadı.

GameplaySceneTests içindeki 13 sahne kontrolü tam EditMode paketinde geçti. PlayMode paketi input, Win, Restart ve ilerleme akışlarını geçti.

Komutlar (Unity 6000.3.23f1; Unity.exe yolu C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe):

```text
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase1Validation -runTests -testPlatform EditMode -testFilter BlockRift.Tests.ApprovedAssetMigrationTests -testResults .../Commands-EditMode.xml -logFile .../Commands-Cached.log
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase1Validation -runTests -testPlatform EditMode -testFilter BlockRift.Tests.GameplayArtIntegrationTests -testResults .../Models-EditMode.xml -logFile .../Models-EditMode.log
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase1Validation -runTests -testPlatform EditMode -testFilter BlockRift.Tests.GameplayArtIntegrationTests -testResults .../Materials-EditMode.xml -logFile .../Materials-EditMode.log
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase1Validation -executeMethod BlockRift.Editor.ApprovedAssetMigration.Build -quit -logFile .../Migration.log
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase1Validation -runTests -testPlatform EditMode -testFilter BlockRift.Tests.ApprovedAssetMigrationTests -testResults .../Migration-EditMode.xml -logFile .../Migration-EditMode.log
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase1Validation -runTests -testPlatform EditMode -testResults .../Final-EditMode.xml -logFile .../Final-EditMode.log
Unity.exe -batchmode -projectPath C:/Users/sahba/BlockRift/Temp/CleanupPhase1Validation -runTests -testPlatform PlayMode -testResults .../Final-PlayMode.xml -logFile .../Final-PlayMode.log
python -B .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py . --json
```

Yukarıdaki `.../` sonuç öneki: `C:/Users/sahba/BlockRift/TestResults/CleanupPhase1_20261029/`. İlk Unity denemeleri lisans/paket çözümlemesinde bekledi; yalnız görevin başlattığı Unity süreçleri yeniden başlatıldı. Ana Unity oturumları kapatılmadı. Ayrı kopyalanan mevcut paket cache ile başarılı çalıştırmalar tamamlandı.

## Bilinen hatalar ve sınırlar

GUID checker: 296 dosya, 4 hata, 0 uyarı. Dört hata başlangıçla birebir aynı DefaultVolumeProfile.asset eksik script referanslarıdır (satır 78, 119, 264, 832). Yeni GUID veya eksik script regresyonu yok. Mevcut hatalar bu kapsamda düzeltilmedi. Mobil performans/build boyutu iddiası yok.

İşlem sırasında görev dışından değişen/taşınan raporlar geri alınmadı:
- `ArtProduction/Environment_Sand_6x6/Alternatives/REPORT.md`: missing
- `ArtProduction/Environment_Sand_6x6/REPORT.md`: missing
- `ArtProduction/UnityIntegration/Migration20261029/REPORT.md`: missing

## Git kapanış görüntüsü

189 dosya: {' M': 51, ' D': 12, '??': 126}. Bu, çalışma ağacının mevcut görüntüsüdür; görev dışı rapor hareketlerini de içerir.

Bu görevin oluşturduğu Temp/CleanupPhase1Validation kopyası testler ve kanıt kayıtları tamamlandıktan sonra kaldırıldı. XML/log/hash kayıtları TestResults/CleanupPhase1_20261029 altında korundu.
