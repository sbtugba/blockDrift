# Block Rift — Aşama 1 uygulama raporu

> Arşiv (2026-10-28): Bu belge tamamlanmış bir geliştirme aşamasının tarihsel kaydıdır. Eski sahne/asset yolları ve test sonuçları güncel durum iddiası değildir. Güncel yönlendirme: [docs dizini](../../README.md).

Tarih: 23 Ekim 2026. Kapsam: saf C# veri modelleri, başlangıç tanımı doğrulaması ve EditMode testleri.

## Onaylı doküman düzeltmeleri

- `docs/GAME_DESIGN.md` v1.2: §§2, 4, 6–7 kapıda bırakmayı beklemeden kazanmayı açıklar; §5.3 birlikte düşen blokların aynı adımda boşalan hücreyi kullanmasını kesinleştirir; §7.3 Undo'nun coin/yardım iadesi yapmadığını, §7.4 Hint'in ancak güncel doğrulanmış sonucun başarıyla sunulmasıyla tüketildiğini belirtir. §13.6 artık açık tasarım kararı değildir.
- `AGENTS.md`: aynı dört karar proje invariantlarına açıklama olarak eklendi.
- Üç proje skill'i: anında kazanma, eş zamanlılık, tahta/ekonomi ayrımı, Hint tüketimi ve Aşama 1 doğrulamasının sınırları eşitlendi. Gameplay skill'indeki eski bırakma ve tüm-state ifadeleri daraltıldı.
- `docs/CONSISTENCY_AUDIT.md`: kullanıcı onayı ve yeni durum kaydedildi; eski Undo/kazanma ifadeleri düzeltildi.
- Skill dosyaları çalışma başlangıcında standart `SKILL.md` adlarıyla zaten mevcuttu; yeniden adlandırılmadı.

## Oluşturulan kaynaklar

`Assets/_BlockRift/Core/`:

- `BlockRift.Core.asmdef`: referanssız Core assembly; `noEngineReferences: true`.
- `Board/Cell.cs`: tamsayı koordinat ve değer eşitliği; aşağı yön y-1.
- `Board/PieceDefinition.cs`: kararlı ID, parça türü, sabit boyut/yönelim, başlangıç hücresi; dokuz şeklin ortak kataloğu.
- `Board/LevelDefinition.cs`: bölüm/şema/kural sürümü, tahta, kapı, parçalar, sabit hücreler ve tetikleyiciler.
- `Board/BoardState.cs`: kimlikle eşlenen güncel konumlar ve tüketim bayrakları; doğrulanmış tanımdan başlangıç snapshot'ı.
- `Board/BoardValidator.cs`: yapısal hata kodları ve içerik inceleme uyarısı.

`Assets/_BlockRift/Tests/EditMode/`:

- `BlockRift.Core.EditModeTests.asmdef`: Editor test assembly, Core referansı.
- `BoardValidatorTests.cs`: 69 parametrik/bağımsız test vakası.

Unity'nin bu dosya ve klasörler için ürettiği `.meta` dosyaları korunmuştur.

## Tasarım tercihleri ve kurallar

- GDD §§3, 5.2, 10.2 ve geçici §13.1 koşulu uygulanır: yalnız 5×5/6×6, tam bir 1×1 hedef, dokuz dikdörtgen, sınır/çakışma denetimi, benzersiz kimlikler, geçerli kapı ve 0–2 başlangıçta boş tetikleyici.
- Girdi listeleri kopyalanır ve salt okunur sunulur; snapshot'lar dış listelerin değiştirilmesinden etkilenmez.
- Sabit engeller hücre kümesi, tetikleyici kimlikleri değişmez bölüm listesinin indeksidir. Aynı şekilli parçaların ID'leri korunur.
- Coin, yardım envanteri ve hamle geçmişi BoardState'e konulmadı. Gelecekte Application oturumu geçmiş/sayaç tutacak; profil ekonomisi Undo snapshot'ından geri yüklenmeyecek.
- Başlangıç kararlılığı yalnız statik alt-kenar desteğiyle denetlenir. Düşebilecek bir blok kümesi varsa en alttaki üye şu anda da desteksizdir; bu varlık kontrolü için gravity simülasyonu yazılmaz.
- `IsValid` çözülebilirlik, minimumluk, tetikleyicisiz yol veya yayın kabulü anlamına gelmez.
- Ana parçanın başlangıçta kapıda olması yeni bir yasak sayılmadı; `TargetStartsOnExit` uyarısıyla içerik incelemesine ayrıldı.
- Null koleksiyonlar kurucuda istisna üretir; tanımdaki kural ihlalleri validator hata kodlarıyla raporlanır. Runtime snapshot restore doğrulaması kayıt aşamasının işidir.
- Hareket, canlı kazanma, cascade, Undo/Hint servisleri veya solver uygulanmadı; bu davranışlar için çalışma testi iddiası yoktur.

## Gerçekte yürütülen doğrulama

Açık kullanıcı Unity oturumunu kapatmadan, kaynakların hash ile eşleştirildiği `Temp/Stage1Offline` projesinde yalnız mevcut yerel NUnit/Test Framework paketleri kullanıldı. Ana projenin paket manifesti değiştirilmedi ve yeni paket eklenmedi.

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe' -batchmode -nographics -projectPath 'Temp/Stage1Offline' -runTests -testPlatform EditMode -testResults 'Temp/Stage1Results/editmode.xml' -logFile 'Temp/Stage1Results/unity-offline.log'
```

Sonuç: Unity 6000.3.23f1, **69 geçti / 0 başarısız / 0 atlanan**, test runner çıkış kodu 0.

```powershell
dotnet build Temp/Stage1Results/Core.Standalone.csproj --ignore-failed-sources --verbosity minimal
dotnet build Temp/Stage1Results/Core.Standalone.csproj --no-restore --verbosity minimal
```

Sonuç: Core, Unity DLL referansı olmadan **netstandard2.1** hedefinde derlendi; **0 hata / 0 uyarı**. İkinci derleme son kaynak değişikliklerinden sonra yapıldı. Geçici proje yalnız Core kaynaklarını içerir, paket bağımlılığı yoktur.

```powershell
& 'python' .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py . --json
& 'python' .agents/skills/unity-csharp-scripting/scripts/check_unity_project.py Temp/Stage1Offline --json
```

Ana proje kontrolü: **4 hata / 0 uyarı**, çıkış kodu 1. Tamamı değiştirilmeyen `Assets/Settings/DefaultVolumeProfile.asset` içindeki mevcut script referanslarıdır (satırlar 78, 119, 264, 832). GUID'ler:

- `60f3b30c03e6ba64d9a27dc9dba8f28d`
- `0fd9ee276a1023e439cf7a9c393195fa`
- `5a00a63fdd6bd2a45ab1f2d869305ffd`
- `74955a4b0b4243bc87231e8b59ed9140`

Yeni Core/test kaynaklarını içeren izole proje kontrolü: **0 hata / 0 uyarı**, çıkış kodu 0. Mevcut görsel profil, Aşama 1 kapsamını aşmamak için değiştirilmedi; ana projenin bütünlük kontrolü tamamen yeşil değildir.

İlk Unity denemesi sandbox lisans erişiminde, tam paketli geçici proje ise paket çözümlemede bekledi. Yerel paketli izole proje başarıyla tamamlandı. Eski geçici test sürecini kapatma ilk isteği otomatik denetimce reddedildi; komut satırıyla yalnız bu görevin test süreci olduğu kanıtlandıktan sonra kapatma onaylandı. Kullanıcının mevcut Unity oturumları kapatılmadı.

Kalıcı kanıtlar: `TestResults/Stage1/editmode.xml`, `source-hashes.json`, `core-meta-check.json`. İlk denemelerin ve başarılı çalışmanın ayrıntılı günlükleri `Temp/Stage1Results/` altındadır.

## Açık sınırlar

Çözülebilirlik/tetikleyicisiz yol kanıtı henüz yoktur. Kazanma sonrası Undo, yıldız eşikleri, yardımın yıldız etkisi ve tekrar ödül yöntemi ileride karara bağlanmalıdır. Bu görevde sahne, görsel asset, sonraki aşama sistemi, branch veya commit oluşturulmadı.

> Depo hazırlığı: yerel kullanıcı yolları paylaşılmaması için proje göreli/taşınabilir gösterime çevrilmiştir; tarihsel test sonuçları değiştirilmemiştir.
