# Block Rift — FBX asset preview

> Arşiv (2026-10-28): Bu belge tamamlanmış bir geliştirme aşamasının tarihsel kaydıdır. Eski sahne/asset yolları ve test sonuçları güncel durum iddiası değildir. Güncel yönlendirme: [docs dizini](../../README.md).

Tarih: 26 Ekim 2026. Bu çalışma yalnız görsel karşılaştırmadır; gameplay, Core ve testler değiştirilmedi.

## Oluşturulan dosyalar

- `Assets/Scenes/AssetPreview.unity` ve `.meta`: dört orijinal FBX'e bağlı model örnekleri, isim etiketleri, orthographic kamera, nötr zemin ve tek yönlü ışık.
- `Assets/_BlockRift/Art/Preview/PreviewBlock.mat`: bütün model örneklerinde ortak, dokusuz URP Lit materyal.
- `Assets/_BlockRift/Art/Preview/PreviewGround.mat`: nötr zemin materyali. İlgili `.meta` dosyaları da eklendi.
- `docs/archive/asset-preview/AssetPreview.png`: kaydedilen sahnenin Unity render'ı, 1080×1920.
- `docs/archive/asset-preview/ASSET_PREVIEW_MEASUREMENTS.json`: Unity'de ölçülen bounds, mesh ve materyal bilgileri.
- `docs/archive/asset-preview/ASSET_PREVIEW_VALIDATION.json`: izole preview projesinin referans/GUID denetimi.

Unity'de `Assets/Scenes/AssetPreview.unity` dosyasını açıp Game görünümünü 9:16 ayarlamak yeterlidir; Play gerekli değildir. Kamera daha geniş ekranlarda da modelleri kapsar. Çok daha dar ekranlar ayrıca kadraj ayarı gerektirebilir.

## Karşılaştırma yöntemi

Modellerin her biri ayrı ayrı normalize edilmedi. Hepsine aynı `0.5256578` ölçek çarpanı uygulandı; 1x1'in X genişliği böylece 1 birimdir. Bu ortak çarpan boyut farklarını gizlemez. Örnekler yalnız merkezleme ve zemine oturtma amacıyla taşındı. Kaynak FBX, mesh, normal, UV veya importer ayarları bu araçla değiştirilmedi. Ortak materyal yalnız sahnedeki renderer override'ıdır.

Sahnenin bir örneği geçici Unity projesinde üretilip kaydedildi, yeniden açıldı ve NVIDIA GeForce MX450 üzerinde URP ile render edildi. Doğrulanan sahne ve iki materyal `.meta` dosyalarıyla birlikte byte-for-byte ana projeye aktarıldı. Geçici pipeline ve Editor aracı ana projeye aktarılmadı. Ana projenin mevcut URP kalite ayarları gölge/aydınlatmada küçük farklar yaratabilir.

## Model ölçümleri

Aşağıdaki boyutlar ortak ölçek uygulanmadan önce Unity Renderer.bounds ölçüleridir. X genişlik, Y yükseklik, Z derinliktir; dış bevel ve dekoratif detaylar da bounds'a dahildir.

| Model | X × Y × Z | X/Z oranı | Vertex | Üçgen |
|---|---|---:|---:|---:|
| 1x1 | 1.9024 × 0.5483 × 1.6733 | 1.137 | 45.355 | 90.706 |
| 2x2 | 1.9024 × 0.4194 × 1.9024 | 1.000 | 60.831 | 121.658 |
| 3x1 | 1.8996 × 0.3388 × 0.7243 | 2.623 | 158.229 | 99.418 |
| 4x1 | 1.8997 × 0.2583 × 0.6199 | 3.065 | 168.188 | 101.030 |

## Bulgular ve öneriler

### Oran ve ortak hücre ölçüsü

- **Dört model birlikte:** Dış genişlikleri yaklaşık 1.9 birim. Ortak hücre ölçeğinde 2x2'nin 1x1'in iki katı, 3x1'in üç katı, 4x1'in dört katı genişliğe yaklaşması beklenirken bu ilişki mevcut değil. Mevcut haliyle aynı transform ölçeğiyle ortak grid'e yerleştirmeye hazır değiller.
- **1x1:** Dış ayakizi tam kare değil; X, Z'den yaklaşık %13.7 büyük. Ortak hücrenin tanımı için önce bu referans düzeltilmeli.
- **2x2:** Kendi dış oranı kareye çok yakın; fakat dış boyutu 1x1 ile yaklaşık aynı. Alt hücrelerin fiziksel boyutu diğer modeldekilerle eşleşmiyor.
- **3x1:** Dış uzun/kısa oranı yaklaşık 2.62:1; tam 3:1 değil.
- **4x1:** Dış uzun/kısa oranı yaklaşık 3.06:1; tam 4:1 değil.

Bevel boşlukları nedeniyle dış bounds'ın tam tam sayılı oran vermesi zorunlu bir sanat kuralı değildir. Ancak hem bu sapmalar hem modellerin aynı toplam genişlikte olması birlikte değerlendirilince, ortak hücre ölçüsüyle yeniden export önerilir. Bu rapor hiçbir oyun kuralını veya collider ölçüsünü değiştirmez.

### Yükseklik, pivot ve dönüşümler

- Yükseklikler 1x1'den 4x1'e doğru 0.548 → 0.419 → 0.339 → 0.258 azalıyor. Görselde uzun bloklar daha ince; ortak kalınlık hedefleniyorsa kaynak modellerde eşitlenmeli.
- Pivotlar bounds merkezine yakın; alt yüzeye yerleştirilmiş değiller. Bu nedenle doğrudan y=0'a koymak zemine gömülmeye yol açabilir. Preview sahnesi alt bounds üzerinden zemine oturtur.
- Dört model de Unity'ye kök ölçek `(100,100,100)` ve X dönüşü 270 dereceyle geliyor. Birbirleri arasında tutarlı; tek başına bozuk geometri kanıtı değil. İleride kaynak birim/Apply Transform düzenini standartlaştırmak yerleştirmeyi kolaylaştırır.

### Materyal ve UV

- Kaynak materyaller Unity'de `Universal Render Pipeline/Lit` shader'ıyla bulundu; eksik materyal veya shader bağlantısı saptanmadı. Ortak preview materyali düzgün render edildi.
- **Dördünde de UV0 yok.** Dokusuz renkli materyalle karşılaştırmayı engellemiyor; UV tabanlı renk/normal/maske dokuları kullanılacaksa UV açılımı gerekiyor. Bu bulgu lightmap UV'sine ilişkin bir ölçüm değildir.
- Ortak materyal özellikle renk farklarını devreden çıkarır; mevcut kaynak materyallerin sanatsal renk değerlendirmesi bu görüntünün amacı değildir.

### Geometri

- Her modelde tek mesh ve renderer var. Normal dizileri mevcut; tam sıfır alanlı üçgen sayımı dört modelde de 0 çıktı.
- Toplam **412.812 üçgen** bulunuyor. Bu basit blok siluetleri için yoğun; mobil kullanımdan önce daha sade mesh/LOD değerlendirmesi önerilir. Bu bir cihaz performans testi veya FPS sonucu değildir.
- Render'da belirgin eksik yüzey görülmedi. Küçük üst yüzey desenlerinin kenarları bu uzaklıkta ince ve yer yer kırık çizgi gibi okunuyor; daha sade bir detay çözümü değerlendirilebilir. Bu gözlemden tek başına non-manifold veya bozuk normal sonucu çıkarılmadı. Manifold/intersection testi yapılmadı.

## Yürütülen doğrulamalar

Son başarılı Unity komutu:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -projectPath 'Temp/AssetPreviewProject' -executeMethod BuildAssetPreview.Run -logFile 'Temp/AssetPreviewResults/unity-verified.log'
```

Sonuç: `ASSET_PREVIEW_SUCCESS`; sahne kaydedildi, yeniden açıldı ve dört FBX render edildi. İlk geçici araç derlemesindeki iki enum ad çakışması tam isimleri kullanılarak düzeltildi; son çalışmada bu derleme hataları yoktur.

```powershell
& 'python' .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py Temp/AssetPreviewProject --json
```

Sonuç: **31 dosya, 0 hata, 0 uyarı**, çıkış kodu 0. Bu izole preview projesinin kontrolüdür; ana projenin bütün mevcut asset'leri için yeni bir tam kontrol sonucu değildir.

`Temp/AssetPreviewResults/promote.py` kontrolü: **22 korunan dosyanın hash'i aynı** (Core, testler ve dört kaynak FBX). Dört FBX'in ana proje ve preview GUID'leri eşleşti. Unity'nin otomatik oluşturduğu ilk FBX `.meta` dosyaları korundu; aktarım sırasında üzerine farklı importer ayarı yazılmadı.

Oyun mantığına müdahale edilmediği için gameplay/EditMode testleri yeniden çalıştırılmadı. Build Settings, ana proje paket manifesti ve Graphics/Quality ayarları değiştirilmedi. Commit, branch veya yayın işlemi yapılmadı.

> Depo hazırlığı: yerel kullanıcı yolları paylaşılmaması için proje göreli/taşınabilir gösterime çevrilmiştir; tarihsel test sonuçları değiştirilmemiştir.
