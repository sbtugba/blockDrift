# Block Rift — Dokümantasyon dizini

Son düzenleme: **2026-10-28**. Bu düzenleme yalnız dokümantasyon/görseller içindir; oyun kodu, Unity sahnesi/assetleri ve Blender kaynakları değiştirilmedi.

## Korunan ana belgeler

| Belge | Kullanım ve durum |
|---|---|
| [GAME_DESIGN.md](GAME_DESIGN.md) | Onaylı oyun kurallarının kaynağı. İçeriği değiştirilmedi. |
| [CONSISTENCY_AUDIT.md](CONSISTENCY_AUDIT.md) | AGENTS.md tarafından kullanılan teknik risk/tarihsel tutarlılık kaydı. İçeriği değiştirilmedi; eski şekil sayısı gibi tarihsel ifadeler güncel GDD'yi geçersiz kılmaz. |
| [LEVEL1_SPEC.md](LEVEL1_SPEC.md) | Uygulama/kabul kapsamı korunur; eski başlangıç durumu ve uzun blokların dört yön beklentisinin tarihsel olduğu işaretlendi. |
| [LEVEL1_REPORT.md](LEVEL1_REPORT.md) | Uygulanmış Level 1 yerleşimi, eski Solver/kalite kanıtları ve bakım komutları; yeni hareket kurallarına uygunluk kanıtı değildir. |
| [CAMERA_AIM_REPORT.md](CAMERA_AIM_REPORT.md) | Kullanılan kamera kadrajı, ölçümler ve doğrulama komutları. Eski kamera aşaması görselinin yeni arşiv yolu güncellendi. |
| [Safe Cleanup Phase 1](audits/SAFE_CLEANUP_PHASE1.md) | Son kaynak temizliği, geri alma ve test kanıtları; korunur. |

**Açık durum:** Şekle bağlı yeni hareket kuralları GDD'de onaylandı; Core/Solver ve mevcut Level 1 migration'ı henüz yapılmadı. Bu belge veya arşivleme işlemi migration'ın uygulandığı anlamına gelmez.

## Korunan görsel yolları

- [Gameplay_6x6_GameView.png](Gameplay_6x6_GameView.png): mevcut uygulanmış Level 1 görüntüsü; `GameplayGameViewCapture` ve `deliver_validated.py` için sabit çıktı yolu. Bu dosya ileride araç tarafından yenilenebilir.
- [Gameplay_6x6_Perspective.png](Gameplay_6x6_Perspective.png): eski 50° kamera/art düzeni görüntüsü; güncel kadraj referansı değildir. **`deliver_board_replacement.py` sabit çıktı yolu olduğu için yerinde korundu.**
- Birebir aynı SHA256'ya sahip `Level1_GameView.png` kaldırıldı; LEVEL1_REPORT bağlantısı ortak GameView dosyasına yönlendirildi. Görsel kaybı yok; silinen kopya ayrıca ignored yedekte tutuldu.

## Arşivlenen dosyalar

Tarihsel raporlar, ölçümler ve karşılaştırma görselleri silinmedi. Eski asset/sahne yollarını anlatmaları tarihsel kanıttır; o dosyaların bugün mevcut olduğu iddiası değildir. Arşivdeki batch komutlarını güncel onay ve önkoşul kontrolü olmadan yeniden çalıştırmayın.

| Arşiv | İçerik |
|---|---|
| [İlk asset önizlemesi](archive/asset-preview/ASSET_PREVIEW_REPORT.md) | Eski AssetPreview sahnesi/ölçek/FBX kurulum raporu |
| [Önizleme onarımı](archive/asset-preview/ASSET_PREVIEW_REPAIR.md) | Eski 1×1 görünürlük ve 2×1 ekleme incelemesi |
| [Ölçümler](archive/asset-preview/ASSET_PREVIEW_MEASUREMENTS.json) | Orijinal ölçüm verisi, değiştirilmeden taşındı |
| [Onarım denetimi](archive/asset-preview/ASSET_PREVIEW_REPAIR_AUDIT.json) | Orijinal önce/sonra kanıtı, değiştirilmeden taşındı |
| [Önizleme doğrulaması](archive/asset-preview/ASSET_PREVIEW_VALIDATION.json) | Orijinal referans denetimi, değiştirilmeden taşındı |
| [İlk görsel](archive/asset-preview/AssetPreview.png), [onarım görseli](archive/asset-preview/AssetPreview_Repaired.png) | Birbirinden farklı, eski sahneye ait karşılaştırma kanıtları |
| [Stage 1](archive/development/STAGE1_REPORT.md) | İlk Core veri modeli/validator uygulaması |
| [Gameplay iskeleti](archive/development/GAMEPLAY_SCENE_REPORT.md) | Başlangıçta boş sahne/kamera/UI kurulumu |
| [Görsel adaptör](archive/development/VISUAL_INTEGRATION_REPORT.md) | Asset importundan önceki koordinat/pivot altyapısı |
| [Gerçek asset entegrasyonu](archive/development/ART_INTEGRATION_REPORT.md) | Import/materyal/GUID ve açık normal kalitesi bulguları; teknik geçmiş korunur |
| [Tahta değişimi](archive/development/BOARD_REPLACEMENT_REPORT.md) | Frameless ölçüleri ve eski 50° kamera aşaması; güncel kamera için CAMERA_AIM_REPORT kullanılır |
| [TopDown kamera aşaması](archive/screenshots/Gameplay_6x6_TopDown.png) | 78° kamera denetimindeki önceki sanat dizilimi; mevcut Level 1 görüntüsü değildir |

## Temizlik kaydı ve doğrulama

- Başlangıçta docs altında **22 dosya** incelendi; altı PNG boyutları, görsel içerikleri ve SHA256 değerleri karşılaştırıldı.
- **13 dosya arşivlendi, 1 birebir kopya PNG kaldırıldı.** Diğer sekiz dosya yerinde korundu. Bu README yeni dizin/temizlik kaydıdır; toplam 22 dosya kalır.
- Kod, Editor, testler, AGENTS.md, skill'ler, ArtProduction teslim betikleri ve eski yerel araçların doküman/görsel adları tarandı. Aktif çıktı yolları taşınmadı; kod/test değişikliği gerekmedi.
- Taşınan yolları anan doküman referansları, LEVEL1_REPORT görsel bağlantısı ve CAMERA_AIM_REPORT TopDown yolu güncellendi. Başka aktif kod referansı güncellemesi gerekmiyor.
- GDD/CONSISTENCY dosyaları, kalan görseller, taşınan JSON/PNG içerikleri hash ile korundu. Docs dışındaki mevcut proje dosyaları başlangıçla karşılaştırıldı; fark yok.
- Unity testleri bu yalnız-dokümantasyon görevinde tekrar çalıştırılmadı. Bağlantı/dosya varlığı, JSON geçerliliği, taşıma hash'leri ve korunan çıktı yolları doğrulandı.
- Tam önceki docs kopyası ve hash/taşıma kayıtları: `TestResults/DocsCleanup_20261028_000819/` (ignored). Önceki kullanıcı değişiklikleri korundu; reset/clean/commit/push yapılmadı.
- Son Git durumu: **13 değiştirilmiş + 27 silinmiş + 208 untracked = 248**, staged **0**. Başlangıç **13 M + 21 D + 200 U = 234** idi. Altı tracked dosyanın taşınması, stage edilmediği için eski yolda silinmiş/yeni yolda untracked görünür. Önceden root ignore desenine uyan iki tarihsel JSON arşiv konumunda artık untracked görünür; ikisi de korunmuştur. GDD'nin M durumu önceden vardı, bu temizlikte içeriği değiştirilmedi.
- Daha önce zaten bulunmayan `docs/audits/CODEBASE_CLEANUP_AUDIT.md` yeniden oluşturulmadı; SAFE_CLEANUP_PHASE1 içindeki yokluk kaydı tarihsel açıklama olarak korundu. Taşıma nedeniyle güncellenmesi gereken aktif kod/test referansı kalmadı.
