# AssetPreview görünürlük incelemesi — 26 Ekim 2026

> Arşiv (2026-10-28): Bu belge tamamlanmış bir geliştirme aşamasının tarihsel kaydıdır. Eski sahne/asset yolları ve test sonuçları güncel durum iddiası değildir. Güncel yönlendirme: [docs dizini](../../README.md).

Blender'da Block_1x1 silinmemişti. Block_1x1, BR_Block_1x1_Review sahnesindeyken Block_2x1 yalnız BR_Block_2x1_Review sahnesine bağlanmıştı. Aktif sahnenin değişmesi 1x1'in görünmemesini açıklıyor. Her iki mesh'in render/viewport gizleme bayrakları kapalıydı; konumları kendi sahnelerinde (0,0,0), ölçekleri (1,1,1) idi. Yeni ArtProduction/AssetPreview/AssetPreview.blend dosyasında mevcut mesh'leri paylaşan iki ayrı obje oluşturuldu; merkezleri X=-1.3 ve X=0.7, aradaki boşluk 0.5 birim. Asıl mesh'ler ve master dosya değiştirilmedi.

Kaydedilmiş Unity Assets/Scenes/AssetPreview.unity sahnesinde mevcut 1x1 aslında bulunuyordu. Prefab kaynağı Assets/_BlockRift/Art/Models/1x1.fbx, mesh'i Mesh_0; activeSelf/activeInHierarchy ve Renderer.enabled true, SceneVisibility gizlemesi false idi. Konumu (-1.400316,0.1623237,-1.499704), ölçeği (52.56578,52.56578,52.56578) idi. Bu ölçek eski FBX içe aktarımının 100 ölçekli köküne ait; dünya genişliği 1 birim. Kaydedilmiş Unity sahnesinde 1x1'in silindiğine veya mesh referansının bozulduğuna dair bulgu yok. Canlı açık editördeki kaydedilmemiş durum bu incelemeye dahil değildir.

Unity'deki mevcut 1x1.fbx, ArtProduction/Block_1x1_Final içindeki son Blender masterından ayrı eski bir assettir. Kullanıcının mevcut modele dokunmama isteği doğrultusunda eski Unity 1x1 dosyası veya örneği değiştirilmedi. Son master Blender ortak önizlemesinde kullanıldı.

Üretilmiş Block_2x1.fbx henüz Unity Assets altında bulunmuyordu. Dosya yeniden modellenmeden Assets/_BlockRift/Art/Models/Block_2x1.fbx konumuna aktarıldı. Ayrı prefab örneği Block_2x1 | FBX Preview, (0,0.0200001,-4.3) konumuna yerleştirildi. Dünya ölçüsü (2,0.3,1). Mevcut bloklar korunarak kamera bütün örnekleri kapsayacak şekilde ayarlandı. Eski sahne yedeği Temp/AssetPreviewRepairResults/AssetPreview.before.unity.

## Gerçekleştirilen doğrulama

- Unity 6000.3.23f1, geçici AssetPreviewProject projesinde `-batchmode -executeMethod RepairAssetPreview.Run` ile mevcut sahneyi açtı, ekleme yaptı, kaydetti, yeniden açtı ve iki URP render aldı. İlk sandbox çalışması lisans IPC zaman aşımıyla durduruldu; dış sandbox çalışması PASS verdi.
- Yeniden açılan sahnede beş modelin tamamı aktif, Renderer'ları açık, mesh referansları dolu ve kamera frustum'u içindeydi. 1x1 ve 2x1 sınır kutuları kesişmiyor. 2x1 diğer mevcut bloklarla da kesişmiyor.
- Mevcut dört prefabın bağlantıları, konumları, rotasyonları ve ölçekleri önce/sonra aynı. YAML karşılaştırması yalnız kamera, kamera Transform'u ve scene roots belgelerinin değiştiğini, bir yeni prefab instance belgesinin eklendiğini doğruladı.
- `python Temp/AssetPreviewRepairResults/promote_repair.py` doğrulanmış sahneyi ve yeni 2x1 FBX/meta çiftini ana projeye aktardı; aktarımdan önce 58 mevcut assetin SHA-256 özeti korundu. Aktarımdan sonra önizleme sahnesi dışındaki mevcut assetler tekrar kontrol edildi.
- `python .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py . --json` ve aynı komutun Temp/AssetPreviewProject hedefli çalışması yürütüldü. Sonuçlar araç çıktısında raporlandı.

GDD v1.2 görsel tasarımı kapsam dışı bırakır; burada mekanik veya Core değişikliği yoktur. Core, test kaynakları ve diğer blok assetleri değiştirilmedi. Oyun mantığı testi gerektiren bir kural değişikliği yapılmadı.

Kanıt: docs/archive/asset-preview/ASSET_PREVIEW_REPAIR_AUDIT.json, docs/archive/asset-preview/AssetPreview_Repaired.png, Temp/AssetPreviewRepairResults/unity-retry.log. Ortak Blender önizlemesi: ArtProduction/AssetPreview/AssetPreview.png.
