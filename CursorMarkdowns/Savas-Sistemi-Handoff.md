# Yeni agent handoff — dövüş sistemi tasarımı

Proje: `D:\ReactBattleArena`. Backend .NET 10 (DDD/CQRS), frontend Vite React TS (`web/`). Kullanıcı .NET API geliştiricisi; React’e yeni. Kod VS Code’da yazılır; Cursor yönlendirir, tüm frontend’i tek seferde implement etmez. WinForms örneği yok.

Önce oku: `CHECKPOINT.md`, `PROJE_MANTIGI.md`, `CursorMarkdowns/Sayfa-Modeli.md`. Sayfa modeli (karakter, takım, user, arena sayfaları, e-ticaret sırası) orada. Bu handoff yalnız yazım kuralları + savaşla ilgili alınan kararlardır.

Bu turda kod yazma. Önce konuş, karar al, sonra dosyaya işle. Kullanıcı “yaz / uygula” demeden implement etme.

---

## Yazım kuralları (chat ve md)

Türkçe yaz. Cümleler birbirine bağlansın. Neden ve sonuç aynı yerde dursun. İngilizce cümle kalıbı kullanma. Kopuk kısa cümle yığını yazma.

Terim kendi adıyla kalsın. Takma ad kullanma. “Cetvel”, “fiş”, “çanta”, “kâğıt” gibi uydurma ad yok. Benzetme ancak kodu gösterdikten sonra olur ve her cümlede asıl ad durur.

“Bunu zaten biliyorsun” diye kısaltma. Tablo kurup karşılaştırmayı orada bırakma. Düz yazı ile anlat.

React parçasında ASP.NET veya `HttpClient` karşılığını söyle. Backend parçasında bu kodu hangi çağrının tetiklediğini söyle.

Kullanıcı mekanizmayı zaten doğru söylediyse yeniden uzun anlatma. Doğru olduğunu söyle ve hangi koda denk geldiğini göster.

Bir paragraf bir fikir. Birkaç cümle yeter. Sonra kod veya yeni paragraf. Anlatımı kısa diye kesme. Roman da yazma.

Liste yalnız gerçek listedir: dosya adı, komut, bu parçanın yazılacağı sıralı adımlar.

Öğrenim notu yazılacaksa: önce gerçek kod bloğu (`başlangıç:bitiş:dosya`), sonra açıklama. Kod görünmeden soyut anlatıma girme. Detay: `.cursor/rules/ogrenim-yazim.mdc`.

Plan / karar metinleri `CHECKPOINT.md` ve `CursorMarkdowns/` altında güncellenir. Eski `REACT-OGRENIM.md` arşiv; satır silinmez.

---

## Savaş sistemi — alınan kararlar (14 Ekim 2026)

### Sayfa (kapı)

- Arena menüden açılır. Light Blue `AppLayout` kabuğu içinde.
- Sayfa giriş yapmış kullanıcının takımlarını okur. Okuma Takımlarım listesiyle aynıdır.
- Kullanıcı bir takımla dövüşe girer.
- Kapı sade kalacak. Rakibin kapıda mı yoksa dövüşün içinde mi kurulacağı henüz kilitli değil.
- Dövüşün iç hesabı (vuruş, rakip, sonuç) savaş sayfasının içindedir. Ayrı karmaşık oyun sayfası açılmaz.
- Savaş geçmişi: kullanıcının bitmiş dövüşleri grid’de; ilk dilim, kaydırınca sonraki dilim (`page` / `pageSize`).
- En güzel dövüşler herkesin gördüğü seçilmiş dövüşlerdir. Dashboard mı ayrı sayfa mı — kapı netleşince bağlanır.
- E-ticaret savaş modülünden **sonra** gelir. Bu turda ürün formu açılmaz.

### Takım ve yer sayısı

- Bir kullanıcının birden fazla takımı olur.
- Her takımda sabit sayıda yer. Sınırın sabit olması kilitlidir.
- Yer sayısı savaş sayfası / dövüş sistemi kurulurken seçilir. Beklenti 5. 6 veya 7 daha uygun çıkarsa bütün takımlar aynı sayıya uyar. Beş örnek sayıdır; sayı henüz kilitli değildir.

### Eğlence ve evren dengesi

- Eğlence kapının bir köşesine kilitlenmedi. Eğlenceli olan tüm dövüş sistemidir.
- Fikir: farklı evrenlerden karakterler aynı arenada kapışır (ör. Luffy ile Sandor Clegane).
- `Universe` yalnız eserin adıdır. Evrenlere ayrı katsayı vermekle denge kurulmaz.
- Güç, karakter satırındaki sayılardadır. Arena o sayıları okur.

### Karakter güç sayıları

Kodda bugün duranlar (`Character` entity):

- `BaseAttack`
- `BaseDefense`
- `BaseSpeed`
- `Rarity` (1–5)
- Üç base sayı validator’da 0–9999.

Konuşulup adı konulan, henüz kolonu yazılmayanlar:

- `BaseHealth` — karakter dövüşte ne kadar durur
- `BaseTechnique` — normal vuruştan ayrı ikinci vuruş
- `BaseAccuracy` — vuruşun karşıya denk gelmesi
- `BaseForesight` — vuruşu önceden görmek; kaçınma / daha az hasar / hasarı hiç yememe ucu. `Intelligence` bu işin adı olmadı. `BaseDefense` gelen hasarı azaltmaya devam eder.

`BaseForesight` adı kilitli. Kolonlar henüz migration / entity’ye eklenmedi. Dövüş formülü henüz yazılmadı — bu agent’ın asıl konuşma konusu.

### Açık kalanlar (14 Ekim — 19 Ekim’de kapandı)

19 Ekim’de kilitlendi. Güncel metin: `CHECKPOINT.md` bölüm 2 ve `CursorMarkdowns/Sayfa-Modeli.md`.

- Takım yer sayısı: **5**
- Rakip: `POST` içinde; yakın güçte kullanıcı takımı, yoksa sistem dizilimi
- Güç kolonları: adlar kilitli; entity/migration “yaz” denince
- Vuruş: iki vuruş; isabet `BaseAccuracy` vs `BaseForesight` (kura yok)
- Puan: kazanma 100, beraberlik 25; sistem dizilimine karşı yok
- Highlights: `/arena/highlights`; Dashboard ayrı modül
- Hesap `POST` + React oynatma; WebSocket / SignalR sonra

### Yapılmayacaklar (şimdilik)

- E-ticaret / ürün formu ayrıntısı
- Video alanı
- Kod implementasyonu kullanıcı istemeden
- Takma ad / uydurma terim
- WebSocket / SignalR / Redis / Queue (bu dilimde)
---

Kaynak: `CHECKPOINT.md` bölüm 2, `CursorMarkdowns/Sayfa-Modeli.md`, 14–19 Ekim plan konuşması.
