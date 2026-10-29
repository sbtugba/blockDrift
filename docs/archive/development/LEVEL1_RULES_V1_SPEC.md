# Level 1 — uygulama ve kabul sözleşmesi

> Durum (2026-10-28): Bu spec 27 Ekim uygulama kapsamının kaydıdır; aşağıdaki başlangıç durumu ve dört yönlü uzun-blok test beklentileri tarihsel kabul edilmelidir. Güncel kurallar için `GAME_DESIGN.md` önceliklidir. Kural/içerik migration henüz uygulanmadı; spec silinmedi veya yeni kurallar adına yeniden yazılmadı.

GDD §§3–7, 10–11; 27 Ekim 2026 kullanıcı görevi. Mevcut spec bulunmadı.

Mevcut: immutable BoardState/LevelDefinition, BoardValidator, 69 Core test,
BoardVisualLayout/Grid, prefab kataloğu, URP sanat, kamera. Hareket, Solver,
Application oturumu, input, Undo/envanter ve save uygulaması yok.

En küçük kapsam: tetikleyicisiz sürümlü Level 1; saf Core tek eksen geçişi,
aynı geçişle BFS ve replay; Application oturumu; mevcut grid/prefab ile
mouse/touch sunumu ve paylaşılan altı URP renk. Gravity uygulanmayacak;
tetikleyicili level bu motor tarafından açıkça reddedilecek. Ücretsiz Undo,
ekonomi, save, generator eklenmeyecek. Mevcut perspective kamera korunacak.

Önceden tanımlı testler: dört yön/uzun blok, ara çarpışma, sınır, sabit hücre,
NO_OP, tek hamle maliyeti, kapıda erken durma, deterministic BFS ve minimum,
INCOMPLETE, restart, zorunlu engel hareketi; 69 Core ve tüm sunum regresyonları.
PlayMode: gerçek sahne yükleme, ID/mesh/materyal eşleme, çoklu aynı şekil/renk,
mouse/touch komutları, UI engelleme, eksen kilidi, anında kazanma/input kilidi,
restart ve Core↔world eşleme. Gerçek Game View görüntüsü alınacak.

Koordinatlar sol alt (0,0), +x sağ, +y tahta üstü/Unity +Z, hücre adımı 1.
## 27 Ekim — bulmaca tasarımını iyileştirme

Önceki alt-satır adayı reddedildi. Blok veya hamle sayısına baştan sınır yok.
Yeni kalite beklentileri: yıldızın alt/orta/üst bölgeleri kullanması; gerekli
hareketli engeller arasında gerçek sıra bağımlılığı; her hareketli engel
başlangıçta dondurulduğunda tam aramada kazanmanın mümkün olmaması; tüm
sabit hücrelerin manevra, geçit veya başlangıç kararlılığı işlevinin açıklanması.
Boşluk kullanımını yalnız doluluk yüzdesiyle değerlendirme.

Yeni testler önce eski düzende başarısız çalıştırılacak. Adaylar mevcut BFS ve
MovementEngine ile çözülecek; çözüm replay, alternatif optimum yollar,
satır/sütun/süpürülen hücreler, engel çıkarma/dondurma karşılaştırmaları raporlanacak.
Kesik analiz INCOMPLETE kalır. Tam generator, yeni hareket kuralı veya yeni
asset yok. Mevcut kamera, materyaller, prefablar ve arka plan hash ile korunur.

## Bağımsız tasarım challenge

Yeni block-rift-level-design skill'i uygulanır. Referansın koordinatları,
şekil kombinasyonu veya stratejisi kopyalanmaz. Elle tasarlanmış farklı fikirler
karşılaştırılır: alan açma zinciri, hedef cebini boşaltma, yıldızı geçici park.
Seçim teknik kanıt ve öznel öğreticilik değerlendirmesi olarak ayrı raporlanır.
Yeni davranış testi: hedef başlangıçta görünür kalır; yalnız komşu hücreyi
boşaltmak hedef cebine giriş açıldığı anlamına gelmez. Hareket motoru/Solver/input değişmez.
