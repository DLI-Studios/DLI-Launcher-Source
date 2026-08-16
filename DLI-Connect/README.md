# DLI Connect

Discord benzeri görünüme sahip, WPF tabanlı bir masaüstü iletişim uygulaması. Kimlik doğrulama (Firebase Auth REST), kullanıcı profilleri, arkadaşlık sistemi (istek gönderme/kabul etme) ve birebir mesajlaşma (DM) içerir. Tüm veriler Firebase'de saklanır; uygulama Firebase'e **doğrudan REST API üzerinden** bağlanır (Firebase SDK'sı kullanılmaz).

---

## 1. Teknoloji Yığını

| Bileşen | Teknoloji |
|---|---|
| UI | WPF (.NET 9.0-windows), XAML, Segoe UI / Segoe MDL2 Assets ikonları |
| MVVM | CommunityToolkit.Mvvm (source-generated `[ObservableProperty]` / `[RelayCommand]`) |
| Backend | Firebase Authentication REST API + Cloud Firestore REST API |
| Çoklu oturum | `--profile` argümanı ile aynı makinede 2 bağımsız oturum |
| Dil | Türkçe UI |

**Firebase projesi:** `dlistudios` — API anahtarı `FirebaseConfig.cs` içinde.

---

## 2. Mimari ve Klasör Yapısı

```
DLI-Connect/
├── App.xaml / App.xaml.cs          → Tema kaynakları, DI kayıtları, MainWindow açılışı
├── MainWindow.xaml (+.cs)          → Çerçevesiz pencere, özel başlık çubuğu (küçült/büyüt/kapat), sayfa host
├── Firebase/
│   ├── FirebaseConfig.cs           → API key, base URL'ler
│   ├── FirebaseClient.cs           → HttpMessageHandler yönetimi
│   ├── FirebaseAuth.cs             → signUp/signIn/refresh/getUserInfo/verification/password reset
│   ├── FirebaseFirestore.cs        → Tüm Firestore REST katmanı (REST kısıtlarına uygun)
│   └── FirebaseRestError.cs        → Hata ayrıştırma + FirebaseApiException
├── Services/
│   ├── SessionManager.cs           → Oturum, profil, heartbeat (çevrimiçi durumu), çıkış
│   ├── NavigationService.cs        → ViewModel'lar arası gezinme + OnNavigatedTo/From
│   ├── FriendService.cs            → Arkadaş listesi, istekler, profil getirme
│   └── MessagingService.cs         → DM iş kuralları (mesaj/okundu/typing/konuşmalar)
├── ViewModels/                     → 11 ViewModel (Login, Register, Home, Friends, Messages, ...)
├── Views/                          → 9 View (Login, Register, Home, Messages, ...)
├── Models/                         → FirebaseUser, UserProfile, FriendInfo, FriendRequest,
│                                      ConversationInfo, Message, FirestoreDocument
├── Themes/
│   ├── Colors.xaml                 → Discord dark paleti (tüm fırçalar buradan beslenir)
│   └── Styles.xaml                 → Global stiller (TextBox, Button, Card, NavItem, ContextMenu, ...)
├── Helpers/SessionStorage.cs       → Refresh token'ı %AppData%\DLI Connect\ altında JSON olarak saklar
└── Utilities/
    ├── Converters.cs               → Bool↔Visibility, int count desteği, EnumEquals, tarih formatlayıcı
    └── Validators.cs               → Form doğrulama + Firestore hata kodlarını Türkçe mesaja çevirme
```

**Akış:** `MainWindowViewModel` → `CurrentViewModel` → `HomeViewModel` → `CurrentSection` (Friends/Messages vb.). Giriş sonrası `HomeViewModel` her bölüm için ilgili ViewModel'i üretir (transient); `MessagesViewModel` gezinme sırasında polling başlatır/durdurur.

---

## 3. Özellikler

### 3.1 Kimlik Doğrulama
- Kayıt (kullanıcı adı + görünen ad + e-posta + şifre), e-posta doğrulama sayfası (doğrula/tekrar gönder), şifremi unuttum
- "Beni hatırla" → refresh token `session.json`'a yazılır; açılışta `TryRestoreSessionAsync` ile otomatik giriş
- **Doğrulanmamış e-posta girişi engellenir**: girişte `GetUserInfoAsync` ile her zaman taze doğrulama durumu çekilir
- `SessionStorage.Clear()` üzerinden "hatırla" kapalıysa token dosyası silinir

### 3.2 Profil
- Firestore `users/{uid}` dokümanı: `username`, `displayName`, `email`, `status` (online/offline), `lastSeen`
- Profil yoksa otomatik oluşturulur (email'in `@` öncesi fallback kullanıcı adıyla)

### 3.3 Arkadaşlık
- Tüm kullanıcılar listelenebilir (Firestore'da indexsiz `array-contains` yerine `ListAllUsersAsync` — **uç: kullanıcı araması için composite index gerektiren filtrelerden kaçınılır**)
- İstek gönderme → `friendRequests/{fromUid}_{toUid}` (status: `pending`), alıcı onaylayınca `friendships/{uidA_uidB}` + istek `accepted`'a çekilir
- Reddedince istek `declined` → silinir/elenir; arkadaş listesi 5 sn'de bir polling ile yenilenir (FriendsViewModel)

### 3.4 Mesajlaşma (DM)
- Konuşma başına tek doküman: `conversations/{sıralı_uidA_uidB}` — katılımcılar alfabetik sırada birleştirilir (ör. `N07gFWX..._znie0EPj...`)
- Mesajlar alt koleksiyonda: `conversations/{id}/messages/{messageId}`
- Özellikler:
  - Yeni sohbet diyaloğu (arkadaş seç → `GetOrCreateConversationAsync`)
  - Mesaj gönderme (Enter; Shift+Enter yeni satır), otomatik kaydırma
  - Okunmamış sayaçları (konuşma listesinde kırmızı rozet, 99+)
  - Okundu işareti ✓ / ✓✓ (mesaj satırında, sadece kendi mesajlarında)
  - "yazıyor..." göstergesi (karşı taraf son 4 sn içinde yazdıysa)
  - Konuşma listesinde arama, sağ tık menüsü (Sohbeti Aç / Kullanıcı Adını Kopyala)
  - **Tüm sohbetler kalıcı**: gizleme özelliği tamamen kaldırıldı — konuşma listesi hiçbir sohbeti elemez
  - 3 sn'de bir polling (DispatcherTimer, `OnNavigatedTo`/`OnNavigatedFrom` ile yönetilir)

---

## 4. Firestore Veri Şeması

```
users/{uid}
├── username, displayName, email, status ("online"|"offline"), lastSeen (unix ms)

friendships/{sıralı_uidA_uidB}
├── uidA, uidB, createdAt

friendRequests/{fromUid}_{toUid}
├── fromUid, toUid, status ("pending"|"accepted"|"declined"), createdAt

conversations/{sıralı_uidA_uidB}
├── participantA, participantB, participants: [uidA, uidB]
├── unreadA, unreadB            → karşı tarafın okumadığı sayaç
├── hiddenA, hiddenB, hiddenUntilA, hiddenUntilB  → (şemada durur; artık kullanılmıyor)
├── lastMessage, lastMessageTime, lastSenderUid, createdAt

conversations/{id}/messages/{messageId}
├── messageId, senderUid, text, createdAt (unix ms)
├── read (bool), readAt (unix ms)

typing/{conversationId}
├── {uid}: son yazma zamanı (unix ms)   → "yazıyor..." durumu için
```

---

## 5. Kritik Firestore REST Kısıtları (keşfedilen ve uygulanan)

Firebase SDK kullanılmadığı için bu kısıtlar kodun her yerinde belirleyicidir:

1. **`updateTransforms` (increment/REQUEST_TIME) yalnızca `:commit` içinde çalışır.** PATCH ile transform yapmak 400 verir. → Sayaç artırımları (`unreadA/B`) `CommitAsync`'te `writes[].updateTransforms` ile yapılır.
2. **Filter + orderBy birleşimi composite index ister** (Firestore console'suz oluşturulamaz). → Konuşma listesi yalnızca `array-contains participants=uid` ile çekilir; sıralama (`LastMessageTime` desc) ve arama filtresi C# tarafında yapılır.
3. **Subcollection sorgusu parent-doc URL formu ister**: `documents/conversations/{id}:runQuery` + gövdede `collectionId`, `orderBy` ve `where` alanları. Normal `documents:runQuery` alt koleksiyon için 400 verir.
4. **`writes[].update.name` tam kaynak yolu olmalıdır**: `projects/dlistudios/databases/(default)/documents/conversations/{id}` — göreli yol (`conversations/{id}`) `INVALID_ARGUMENT` döndürür: "Document name ... lacks 'projects' at index 0". `MessagingService.DocName()` bu formatı üretir.
5. **Sıralı sorgularda tek alanlı `orderBy` serbesttir** (composite index gerekmez) → mesajlar `createdAt DESC` çekilir, istemcide ASC'ye çevrilir.
6. **`Url()` çift `?` tuzağı**: path'te zaten `?pageSize=...` varsa API anahtarı `&key=` ile eklenmelidir (`?key=` olarak eklenirse parametre kaybolur → "Arkadaşlar bölümünde hata").

---

## 6. Önemli İş Akışları (Uygulama İçi)

### 6.1 Mesaj gönderme — tek atomik `:commit`
```
1. conversations/{id}/messages/{yeniId}   → oluştur (text, createdAt, read=false)
2. conversations/{id}                     → lastMessage/lastMessageTime/lastSenderUid güncelle
3. aynı yazmaya transform ekle           → karşı tarafın unreadX = increment(1)
```
3 işlem tek istekte toplanır → ya hepsi olur ya hiçbiri.

### 6.2 Okundu işaretleme (`MarkReadAsync`)
Karşı taraftan okunmamış mesaj görülürse: kendi `unreadX` sıfırlanır + o mesajlara `read=true`/`readAt` yazılır (tek commit). UI'da ✓ → ✓✓ anında güncellenir.

### 6.3 "yazıyor..." (`SetTypingAsync` / `IsPeerTypingAsync`)
Kullanıcı yazmaya başlayınca (3 sn'de bir) `typing/{conversationId}/{uid}` güncellenir; karşı taraf `now - lastTypingAt < 4000 ms` ise "yazıyor..." görür.

### 6.4 Çevrimiçi durumu (heartbeat)
- Giriş/oturum geri yükleme/e-posta doğrulama sonrası: `status=online` yazılır + **30 sn'de bir heartbeat** (`StartHeartbeat`)
- Çıkış ve uygulama kapanışı: `status=offline` (kapanışta max 1,5 sn beklenir)
- **Takılı "online" sorunu çözüldü**: zorla kapatılan süreç offline yazamaz; bu yüzden çevrimiçi kabulü artık `status=="online" && now-lastSeen < 90sn` (2 heartbeat atlamışsa çevrimdışı sayılır) — `FriendInfo.IsOnline` ve `ConversationItemViewModel.IsOnline`

### 6.5 Konuşma listesi yenileme (3 sn polling)
`GetConversationsAsync` → tüm konuşmalar (`array-contains`) → istemcide `LastMessageTime` desc sıralanır; her öğe için karşı tarafın profili cache'ten/API'den çekilir (`_profiles` dictionary). Öğeler `_allItems` havuzunda tutulur, arama filtresi `ApplyFilter` ile uygulanır.

### 6.6 İki örnekle test etme
- Pencere 1: `DLI-Connect.exe` (normal) → `session.json`
- Pencere 2: `DLI-Connect.exe --profile=test` → `session-test.json`
- Aynı makinede iki farklı hesap açık kalabilir; mesajlaşma/presence gerçek zamanlı test edilir

---

## 7. UI / Tema (Discord Dark)

Tüm renkler tek noktadan (`Themes/Colors.xaml`) yönetilir; her View fırça anahtarlarını kullanır:

| Anahtar | Değer | Kullanım |
|---|---|---|
| `BackgroundBrush` | `#313338` | Sohbet alanı / içerik zemini |
| `SidebarBrush` | `#2B2D31` | Nav sidebar, konuşma listesi paneli |
| `ServerRailBrush` | `#1E1F22` | Sol sunucu rayı (Discord'un en koyu katmanı) |
| `CardBrush` | `#383A40` | Yükseltilmiş paneller / kartlar |
| `SidebarHoverBrush` | `#35373C` | Sidebar öğe hover'ı |
| `SidebarSelectedBrush` | `#404249` | Seçili nav/konuşma öğesi |
| `MessageHoverBrush` | `#2E3035` | Mesaj satırı hover'ı |
| `UserPanelBrush` | `#232428` | Alttaki kullanıcı paneli |
| `PrimaryBrush` / `AccentBrush` | `#5865F2` | Blurple (buton, rozet, seçili vurgu) |
| `SuccessBrush` | `#23A55A` | Çevrimiçi yeşili |
| `DangerBrush` | `#F23F43` | Okunmamış rozeti, tehlikeli butonlar |
| `TextPrimary/Secondary/Muted` | `#DBDEE1` / `#949BA4` / `#80848E` | Metin hiyerarşisi |

**Discord'a benzeyen düzen detayları:**
- HomeView: 72px sunucu rayı (yuvarlak logo butonu) + 240px kanal tarzı sidebar ("ANA MENÜ" başlığı, hover `#35373C`, seçili öğe `#404249` + beyaz metin) + içerik
- Alttaki kullanıcı paneli: çerçevesiz `#232428` bar, 32px avatar, durum noktası
- MessagesView: tam boy konuşma paneli (`#2B2D31`) + sohbet (`#313338`); mesajlar **balon değil, Discord satırı**: solda 40px avatar, üst satırda kalın isim + gri saat + ✓✓, hover'da `#2E3035`
- Yazı kutusu: `#383A40` 8px yuvarlatılmış çerçeve, içinde transparan TextBox, sağda dairesel blurple gönder butonu
- Context menu: `#111214` zemin, blurple hover; tüm köşe yarıçapları 8; kartlar gölgesiz/düşük gölgeli
- Pencere: çerçevesiz (`WindowStyle=None` + `AllowsTransparency`), 44px özel başlık çubuğu, `#1E1F22` kenarlık

**Converter'lar (`Utilities/Converters.cs`):**
- `BooleanToVisibilityConverter` / `InverseBooleanToVisibilityConverter`: `bool` **ve** `int`/`long` destekler (count > 0 → görünür). Listeler `BoolToVis`, boş-durum panelleri `InvBoolToVis` ile bağlanır
- `EnumEqualsConverter` (nav seçimi), `IsOnlineToColorConverter`, `FirstLetterConverter` (avatar harfi), `DateToDisplayConverter` (tr-TR tarih)

---

## 8. Hata Yönetimi ve Hata Ayıklama

- **Log dosyası:** `%TEMP%\dli-connect.log` — tüm ViewModel'lerin catch blokları `Log(ex)` yazar (zaman damgalı, tam stack trace)
- Firestore hata kodları `Validators.ToTurkishErrorMessage` ile Türkçeleştirilir: `PERMISSION_DENIED`, `UNAUTHENTICATED`, `INVALID_ARGUMENT`, `FAILED_PRECONDITION`, `RESOURCE_EXHAUSTED`, `NETWORK_ERROR` vb.
- Beklenen/zararsız hatalar sessizce yutulur (örn. heartbeat yazım hatası)

---

## 9. Derleme ve Çalıştırma

```powershell
# Derleme (Release)
dotnet build -c Release
# veya
dotnet run -c Release

# İki örnekle test
.\bin\Release\net9.0-windows\DLI-Connect.exe
.\bin\Release\net9.0-windows\DLI-Connect.exe --profile=test
```

**Gereksinim:** .NET 9 SDK, internet erişimi (Firebase API). Oturum dosyaları `%AppData%\DLI Connect\session*.json` içindedir.

---

## 10. Değişiklik Geçmişi (Önemli Düzeltmeler)

| Tarih | Değişiklik |
|---|---|
| 31.07.2026 | Phase 3 DM: teşhis → Firestore REST kısıtları (commit/query/URL formları) çözüldü; `CommitAsync`/`RunQueryAsync(parentPath)` eklendi |
| 31.07.2026 | `Url()` çift-`?` hatası düzeltildi (Arkadaşlar bölümü artık hatasız) |
| 31.07.2026 | `--profile` desteği → iki bağımsız oturum testi |
| 31.07.2026 | Presence: 30 sn heartbeat + 90 sn staleness kuralı (takılı "online" çözüldü) |
| 31.07.2026 | Mesaj gönderme 400 hatası: `:commit` tam kaynak yolu (`DocName`) ile çözüldü; Firestore'da iki yönlü mesajlaşma doğrulandı |
| 31.07.2026 | Görünüm hatası: Count bağlamalarının converter'ları ters çalışıyordu → liste/boş-durum bağlamaları düzeltildi |
| 31.07.2026 | Sohbetleri gizleme özelliği kaldırıldı → **tüm sohbetler kalıcı** |
| 31.07.2026 | Discord dark temaya geçiş (palet, sunucu rayı, sidebar, mesaj satırları, yazı kutusu) |
| 31.07.2026 | 22 adet UI/UX/frontend-design skill'i global kuruluma eklendi (`~/.config/opencode/skills/`; aktif olması için opencode restart gerekir) |

---

## 11. Bilinen Sınırlar

- **Gerçek zamanlı yok**: Firestore polling tabanlı (arkadaşlar 5 sn, mesajlar 3 sn) — Firestore'da gerçek Realtime isteğe bağlı (firestore emulator streaming gerekir)
- Mesaj geçmişi tek seferde son 60 mesaj (`QueryMessagesAsync limit=60`)
- E-posta doğrulaması olmadan DM kullanılamaz (giriş engellenir)
- Görüntü (avatar) yükleme altyapısı şemada hazırdır (`avatar` alanı) ama UI'da henüz desteklenmez — avatar yerine baş harf daireleri kullanılır
