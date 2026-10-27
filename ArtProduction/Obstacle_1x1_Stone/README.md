# 1x1 sabit taş engel — inceleme sürümü

Collection: Obstacle_1x1_Stone. Nesne: Obstacle_1x1_Stone_Export. Mevcut AssetPreview.blend güncellendi; çalışmadan önce AssetPreview_BeforeStone.blend yedeği alındı. Obstacle_1x1_Stone.blend ayrıca kayıt kopyasıdır. Diğer engeller üretilmedi, Unity'ye aktarım/FBX export yapılmadı.

## Geometri ve referans

Yerel sınırlar X/Y=-0.5..0.5, Z=0..0.30. Pivot taban merkezinde; ölçek (1,1,1), rotasyon sıfır. Önizleme konumu (3.5,4,0), mevcut modellerden ayrıdır.

Tok kare taş gövde, aşınmış dış kenarlar, düzensiz sığ üst yontuk ve üç sığ geometrik çatlak/groove bulunur. Yontuk tabanı Z=0.247, ölçülen rim Z=0.2972; yaklaşık 0.050 derinlik. Dört küçük kırık parça boolean union ile gövdeye kaynaştırıldı; iç içe kapalı kabuklar bırakılmadı. İnce kırıklı çatlak çizgileri, yan yüzlere devamları ve taş taneleri UV yüzey/normal haritalarındadır. Oyuncu bloklarındaki baklava işareti yoktur.

Referansa göre yontuğun sınırları daha yumuşak ve kırık parçalar daha sadedir. Referansın birebir kopyası olarak sunulmuyor; kullanıcı değerlendirmesi bekleniyor.

## Malzeme / URP veri eşlemesi

- BR_Stone_Neutral_URP: nötr gri, Metallic=0, Roughness=0.85. Renk vertex geometrisine yazılmaz.
- Stone_1x1_Surface.png: 512x512, lineer gri yüzey çarpanı; ince çatlak ve taneler içerir. Unity'de Base Map olarak sRGB kapalı kullanılmalı; Base Color ayrıca ayarlanabilir.
- Stone_1x1_Normal.png: 512x512, tangent-space +Y. Unity import aşamasında Texture Type=Normal map, sRGB kapalı; yaklaşık Normal Scale=0.75.
- URP Lit için Smoothness yaklaşık 0.15, Metallic=0. Blender node ağacı Unity materyaline otomatik aktarılmış sayılmaz; URP materyali ileride ayrıca kurulmalı.
- UV var; Mikk tangent üretimi mesh.calc_tangents ile başarılı. Haritalar .blend içine pack edildi ve ayrıca PNG olarak saklandı.
- Unity shader/runtime görünümü export yasağı nedeniyle çalıştırılmadı. Burada veri uyumluluğu doğrulandı; cihaz performansı ölçülmedi.

## Üretim ve yürütülen kontroller

Blender MCP execute_blender_code ile sırasıyla generate_stone_1x1.py, refine_stone_1x1.py ve finalize_stone_1x1.py çalıştırıldı. Yeniden üretim için taşın henüz bulunmadığı AssetPreview_BeforeStone.blend açılıp bu sıra izlenebilir. İlk script mevcut taş üzerine yazmayı reddeder. İnce yüzey ayrıntıları ikinci scriptte UV üçgenleri üzerinden rasterize edilir; son script testleri, renderları ve kaydı yapar.

Nihai mesh: 1.623 vertex / 3.242 üçgen; tek bağlı bileşen, sıfır manifold dışı kenar, sıfır dejenere yüzey, pozitif hacim. BVH ile vertex paylaşmayan üçgenler arasında sıfır kesişim. Normaller sonlu ve birim uzunlukta; tangentler sonlu. Ölçüler, taban ve pivot doğrulandı. UV mevcut; vertex renk katmanı yok.

İlk oluşturma ve sahneye ekleme sonunda mevcut sahne objelerinin mesh/topoloji/UV/normal/smooth/materyal bağlantıları, Transform, koleksiyon üyeliği, kamera ve ışık parametreleri önce/sonra karşılaştırıldı; aynı kaldı. Sonraki düzenlemeler yalnız yeni taşın mesh/materyal/dokularına yapıldı. Core, Unity Assets, oyun kuralları ve onaylı blokların üretim kaynakları değiştirilmedi.

Top.png, Perspective.png (40 derece), Side.png, Detail.png ve Compare_Block_1x1.png renderları alındı ve Blender MCP look ile incelendi. Kalite kontrol sayıları qa.json içinde bulunur.
