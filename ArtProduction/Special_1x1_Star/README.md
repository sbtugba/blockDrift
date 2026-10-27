# Special_1x1_Star — review version

AssetPreview.blend içinde Special_1x1_Star Collection ve Special_1x1_Star_Export mesh nesnesi oluşturuldu. Önizleme konumu (5.2,4,0), yerel pivot taban merkezi (0,0,0), ölçek (1,1,1), rotasyon sıfır; ölçü 1×1×0.30. İşlem öncesi Before_Star_*.blend yedeği bu klasördedir. Export yapılmadı.

Referans: geniş yuvarlak köşeli krem gövde, yumuşak dışbükey omuz, geniş düz tepe, gerçek yıldız biçimli kör oyuk ve oyuk içinde altın inlay. Yıldız genişliği 0.538835; gövde genişliğinin yaklaşık %54'ü. Referansa kıyasla renderdaki gölge ve parlaklık stüdyo ışıklarına bağlı değişir; görüntüdeki ışıklar geometri olarak üretilmedi. Yıldız üstü Z=0.290, gövde tepesi Z=0.300, oyuk tabanı Z=0.258. Yıldız tabanı Z=0.262; tabanda 0.004 açıklık vardır. Beş uç ve beş iç köşe kısa kuadratik yaylarla yuvarlatılır.

## Materyaller ve Unity

- MAT_Star_Ivory: sRGB #F0E8DB, Metallic 0, Roughness 0.31; URP Smoothness 0.69.
- MAT_Star_Gold: sRGB #FFC247, Metallic 0.04, Roughness 0.25; URP Smoothness 0.75.

Renkler Principled BSDF Base Color üzerinde doğru sRGB → lineer dönüşümüyle tanımlıdır. İki material slotu aynı export meshinde korunur. Kenarlardaki altın ton farkı geometri normalleri ve ışıkla sağlanır; siyah outline kullanılmaz. Unity URP Lit materyalleri bu slotlara yeniden eşlenmelidir. Blender shader dönüşümünün otomatik veya Unity'de doğrulanmış olduğu iddia edilmez. Unity import, mobil cihaz performans testi ve FBX export gerçekleştirilmedi.

## Geometri ve doğrulama

Tek nesne içinde iki kapalı bileşen: gerçek oyuğu içeren gövde ve altın yıldız. 4.644 vertex / 9.280 triangle. UV smart projection, özel split normals ve sonlu tangentler. Oyuk ve yıldız birbirini kesmez; materyal renkleri vertexlere gömülmez.

Blender MCP üzerinden generate_star.py, refine_tip_sampling.py ve render_star.py yürütüldü. İlk yıldız ucu örnekleme iyileştirmesinde aynı açılara çok yakın örneklerden kaynaklanan dejenere üçgenler QA ile yakalandı; açılar birleştirildi ve nihai QA tekrar geçti. Son renderlar Standard, Exposure -0.6 ile güncellendi.

Gerçek kontroller: ölçü/pivot/transform; BMesh kapalı manifold; dejenere yüzey; işaretli hacim; BVH ile ortak vertex paylaşmayan üçgenlerin kesişimi; finite/unit normals; finite UV; Mikk tangent hesabı. Sonuçlar qa.json içinde: non-manifold 0, degenerate 0, saptanan kesişim 0. BVH kontrolü genel amaçlı tüm olası öz-kesişimlerin matematiksel ispatı değildir.

Önce/sonra mesh, normal, UV, materyal değerleri, Transform ve görünürlük parmak izi karşılaştırmasıyla AssetPreview'daki bütün önceki nesnelerin değişmediği doğrulandı. Core, gameplay ve Unity Assets değiştirilmedi; GDD kuralları etkilenmedi.

Top.png, Perspective40.png, Side.png gerçek materyalli renderlardır; Star_ThreeViews.png bunların paftasıdır. Render stüdyosu ayrı Review_Special_1x1_Star sahnesinde ve Preview_Studio_Star Collection içinde tutulur. Mevcut Preview_Studio değiştirilmez. Üretim kodu ayrıca Blender Text olarak saklanır. Yeniden üretim için bu klasördeki Before_Star yedeğini açıp güncel generate_star.py, sonra render_star.py çalıştırılır; üretici mevcut yıldızın üzerine yazmayı reddeder.
