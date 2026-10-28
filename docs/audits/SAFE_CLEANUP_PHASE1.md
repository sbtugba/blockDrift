# Block Rift — Safe Cleanup Phase 1

Tamamlanma: 2026-10-28. Çalışma başlangıcı: 2026-10-27. Yalnız onaylanan A adayları temizlendi; Core, Solver, Level 1, kamera, ana sahne, Blender modelleri ve diğer prefab/materyaller değiştirilmedi. Commit/push yapılmadı.

## Uygulanan değişiklikler

| Dosya | İşlem | Güvenlik kanıtı |
|---|---|---|
| `C:/Users/sahba/BlockRift/Assets/_BlockRift/Art/Preview/PreviewBlock.mat` | Kaldırıldı | Unity AssetDatabase recursive bağımlı sayısı 0; proje GUID taramasında referans 0 |
| `C:/Users/sahba/BlockRift/Assets/_BlockRift/Art/Preview/PreviewBlock.mat.meta` | Materyalle birlikte kaldırıldı | GUID `7917bfc24c15a024e8f0436bea3dae42`; başka asset'e atanmadı, kalan GUID bağı yok |
| `C:/Users/sahba/BlockRift/Assets/_BlockRift/Art/Preview/PreviewGround.mat` | Kaldırıldı | Unity AssetDatabase recursive bağımlı sayısı 0; proje GUID taramasında referans 0 |
| `C:/Users/sahba/BlockRift/Assets/_BlockRift/Art/Preview/PreviewGround.mat.meta` | Materyalle birlikte kaldırıldı | GUID `0f44ddc3de9e95f41bcf2eda29bdea8a`; başka asset'e atanmadı, kalan GUID bağı yok |
| `C:/Users/sahba/BlockRift/Assets/_BlockRift/Editor/LevelOneQualityAudit.cs` | Yalnız kullanılmayan private `Copy` metodunun üç satırı çıkarıldı | Tek `Copy(` eşleşmesi metodun tanımıydı; çağrı/reflection/Unity callback bağlantısı yok. Diğer dosya içeriği ve satır sonları korundu |

Materyallerde kullanıcı değişikliği ayrıca incelendi: HEAD'e göre tek fark `_Color` bileşenlerinde en fazla yaklaşık **5×10⁻⁸** kayan nokta farkıydı. `_BaseColor` ve diğer içerik aynıydı. Anlamlı özgün materyal çalışması kanıtı bulunmadı; yine de değiştirilmiş dosyaların **tam çalışma-ağacı byte kopyaları**, metaları ve C# dosyasının ilk hali yedeklendi. HEAD'e reset yapılmadı.

Yedek/kanıt kökü: `C:/Users/sahba/BlockRift/TestResults/SafeCleanupPhase1_20261027_234230/`. Tam yedekler `Backup/` altında, başlangıç hash'leri `before-hashes.json`, ilk Git durumu `before-status.txt` içinde. Dosyalar ve kullanıcı değişiklikleri kaybolmadı.

## Gerçekte çalıştırılan testler

Unity **6000.3.23f1**, URP **17.3.0**. Açık ana editörlere müdahale edilmedi; aynı Assets/Packages/ProjectSettings çalışma ağacının `ValidationProject/` izole kopyasında Unity AssetDatabase ve gerçek Unity test runner kullanıldı. İki ek temizlik testi yalnız bu ignored kopyada; ana projeye yeni test/script eklenmedi.

| Kontrol | Önce | Son başarılı sonuç |
|---|---:|---:|
| Saf Core/Application, bellekte derleme + NUnit | 85 geçti | 85 geçti |
| Unity EditMode | 163 geçti | 163 geçti |
| Unity PlayMode | 6 geçti | 6 geçti |

EditMode sayısı **161 mevcut test + 2 geçici AssetDatabase kontrolü**dür; saf 85 test mevcut Unity testlerinin alt kümesidir, toplam sayıya ayrıca eklenmemeli. Son başarılı çalışmalarda başarısız/atlanan test yok.

Unity komutları şu kalıpla gerçekten çalıştırıldı:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'C:/Users/sahba/BlockRift/TestResults/SafeCleanupPhase1_20261027_234230/ValidationProject' -runTests -testPlatform EditMode -testResults '<kanıt-kökü>/after-editmode-corrected.xml' -logFile '<kanıt-kökü>/after-editmode-corrected.log'
# Aynı proje için -testPlatform PlayMode, after-playmode.xml ve after-playmode.log ile de çalıştırıldı.
```

Komut yukarıdaki `<kanıt-kökü>` yer tutucusu yerine tam mutlak yol kullanılarak yürütüldü. İlk sandbox denemesi lisans IPC erişiminde kaldı; yalnız bize ait doğrulama Unity süreci durdurulup erişim sınırı dışında aynı kopya yeniden çalıştırıldı. Diğer açık Unity süreçleri kapatılmadı.

Kanıtlar:

- `baseline-editmode.xml`: 163/163; `baseline-playmode.xml`: 6/6.
- `after-editmode-corrected.xml`: 163/163; `after-playmode.xml`: 6/6.
- `core-before.json`, `core-after.json`: 85/85.
- `static-reference-check.json`, `material-user-changes.json`, `final-guid-check.json`, `final-integrity.json`.

### Başarısız kontrol ve güvenli geri alma

İlk temizlik sonrası **161 mevcut test geçti**, fakat eklediğim iki temizlik testi `GUIDToAssetPath` sonucunun hemen boş olmasını beklediği için başarısız oldu (`after-editmode.xml`: 161 geçti, 2 başarısız). Beş aday dosya, eşzamanlı kullanıcı düzenlemesi olmadığı hash ile kontrol edilerek **birebir yedekten geri yüklendi**. Başlangıçtaki 351 dosyanın tamamının hash eşitliği doğrulandı (`rollback-executed.json`).

Hatanın nedeni kontrolün silinmiş GUID önbelleğini canlı asset gibi değerlendirmesiydi. Yalnız izole test, `AssetPathToGUID(..., OnlyExistingAssets)`, `LoadAssetAtPath == null`, GUID'in başka asset'e atanmaması ve recursive bağımlı bulunmaması kontrollerini kullanacak şekilde düzeltildi. Unity bu ayrımı [resmî AssetPathToGUID belgelerinde](https://docs.unity3d.com/ja/current/ScriptReference/AssetDatabase.AssetPathToGUID.html) açıklıyor; yerel UnityEditor.CoreModule XML belgeleri de kontrol edildi. Test zayıflatılarak canlı bağımlılık göz ardı edilmedi.

Düzeltilmiş testlerle temizlenmiş izole kopya EditMode **163/163**, PlayMode **6/6** geçtikten sonra, hash'i doğrulanmış **aynı değişiklik** ana projeye tekrar uygulandı. İlk başarısız sonuç saklandı; başarılı gibi raporlanmadı.

İzole Unity importu `Assets/Settings/Mobile_RPAsset.asset` dosyasını v12'den v13'e otomatik normalize etti. Bu fark tespit edildi ve **ana projeye aktarılmadı**. Packages lock dosyaları birebir aynı. Test sonuçları izole Unity ortamına aittir; ana editörde yeni Game View ekran görüntüsü alınmadı.

## Regresyon ve kapsam kontrolü

- Başlangıçtaki **351 dosyadan sadece onaylanan beşi** değişti: dört silme ve tek private-metot çıkarımı. Diğer **346 dosya byte/hash olarak aynı**. Sonuç raporu sonradan eklenen tek yeni dokümantasyondur.
- Ana sahne SHA256 aynı: `DAA4B3B233F9B829C887D24D78B9F7E72BDA735036B7BEC10388210AD5DAA4C9`.
- Ana sahne/kamera, BoardRoot/PiecesRoot, prefab/model/palet/katalog ve adaylar dışındaki materyal/metadata dosyaları değişmedi.
- Assets son taraması: eksik meta **0**, yetim meta **0**, duplicate GUID **0**, silinen iki GUID'e kalan metin referansı **0**.
- Prefab/gerçek mesh/pivot/kamera ve mouse/touch/Restart/kazanma regresyonlarını mevcut Unity testleri doğruladı. Yeni hareket kuralları uygulanmadı; geçen eski-kural testleri güncel GDD uyumunun kanıtı değildir.
- Önceki `docs/audits/CODEBASE_CLEANUP_AUDIT.md` dosyası bu çalışma başlangıcında diskte bulunamadı. Konuşmada raporlanan A listesi kullanıldı; her aday yeniden kanıtlandı. Eksik eski rapor kendiliğinden yeniden oluşturulmadı.

## GameplayBoardReplacement koruma seçenekleri — uygulanmadı

Dosya: `C:/Users/sahba/BlockRift/Assets/_BlockRift/Editor/GameplayBoardReplacement.cs`. Mevcut eski kamera yazımları risk olarak duruyor; bu görevde araç veya kamera değiştirilmedi.

1. **Öneri:** Açık historical-migration komut bayrağı **ve** beklenen eski sahne/katalog hash'lerini birlikte şart koşmak; uyuşmazlıkta yazmadan durmak. Küçük, açık ve test edilebilir koruma.
2. İleride board değiştirme ile kamera yapılandırmasını ayırıp board aracının kamera yazımlarını kaldırmak. Daha geniş B/D değişikliği; ayrı onay gerekir.
3. Yalnız uyarı veya Obsolete etiketi yanlış yeniden çalıştırmayı engellemez; tek başına yeterli değil.

## Kalan Git durumu

Başlangıç: **15 M + 17 D + 199 U = 231**, staged 0.

Bu rapor dahil sonuç: **13 M + 21 D + 200 U = 234**, staged 0. İki materyal M'den D'ye geçti; önceden temiz iki meta D oldu. LevelOneQualityAudit zaten untracked olduğundan U sayısını değiştirmedi; yeni rapor U sayısına bir ekledi. Önceden mevcut 17 silme ve diğer kullanıcı değişiklikleri korunuyor. Test kopyası/yedekler `.gitignore` kapsamındaki TestResults'te.

Reset/clean/force checkout/commit/push kullanılmadı. A dışı dosya silinmedi; B/D sistemleri yeniden düzenlenmedi.
