# Kitobdagimen — STITCH AI Prompts (UI / Frontend Generation)

Loyiha: **Kitobdagimen** — O'zbekiston bozori uchun ijtimoiy kitob o'qish platformasi.
Maqsad: STITCH AI orqali barcha frontend ekranlarini (responsive, mobile + web) zamonaviy minimalist uslubda generatsiya qilish.

---

## Qanday ishlatish

1. **Avval "MASTER PROMPT"ni** bir marta ishga tushiring — bu STITCHga umumiy dizayn tili, brend va palitrni o'rnatadi.
2. **Keyin har bir "EKRAN PROMPTI"ni alohida-alohida** yuguring. Har bir prompt oxirida `// Consistent with the established design system above.` iborasi bor — shuning uchun master-prompt avval yaratilgan dizayn bilan bog'lanadi.
3. STITCH bir vaqtda barcha ekranni emas, bitta yaxshi ekranni generatsiya qiladi — shuning uchun ekranlar alohida yuguriladi.
4. **UI matnlari o'zbek tilida** bo'lishi kerak (sarlavhalar, tugmalar, placeholderlar). O'zbek tilining lotin va kirill alifbosi uchun ham shrink/shrift mosligi zarur.

---

# MASTER PROMPT (birinchi ishga tushiring)

```
Design a complete mobile + web responsive UI design system for "Kitobdagimen", a modern social book-reading platform for the Uzbekistan market.

APP CONCEPT:
Kitobdagimen is a social reading app where users build a personal digital library, buy/rent books from a store, track reading goals, join reading challenges and book clubs, compete on leaderboards, physically exchange books with nearby people via a map, chat, earn achievements, and use AI features (recommendations, book summaries, quizzes, OCR scan, voice/audiobook, moderation). It also has Admin and Moderator panels.

DESIGN STYLE — Modern Minimalist:
- Clean, generous whitespace, calm and focused (reading-first mindset).
- Flat surfaces with very subtle shadows, thin 1px borders, soft rounded corners (radius 12–16px).
- No heavy gradients, no clutter. Restraint over decoration.

COLOR PALETTE (restrained):
- Background: #FFFFFF (app), #F6F7F9 (surfaces/cards)
- Text primary: #1A1D1F, Text secondary/muted: #6B7178
- Primary accent (brand): #2D6A4F (deep forest green — calm, literary, nature)
- Secondary accent (used sparingly for highlights/achievements): #E8A33D (warm sand/amber)
- Success: #2D6A4F, Error/Warning: #C0492F, Info: #3A7CA5
- Keep accent usage minimal — green for primary actions, amber only for rewards/badges.

TYPOGRAPHY:
- UI font: Inter (or system sans-serif). Clean, medium weight for labels.
- Reading/book font: a refined serif (Lora or Newsreader) for book content and quotes.
- Must render both Uzbek Latin ("Kitoblar", "Ma'lumot") and Cyrillic ("Китоблар") correctly.
- Font scale: clear hierarchy, large readable body (16px+), comfortable line-height.

COMPONENTS TO ESTABLISH (reusable):
- Top app bar (with back, title, optional action icon) and a bottom navigation bar (mobile) / left sidebar (web).
- Cards (book card with cover, title, author, rating), list rows, primary & secondary buttons, pill tags/chips (genre), search bar, tab bar, empty-state illustration, loading skeleton, toast/snackbar, modal/bottom-sheet, avatar, badge, progress bar (reading progress), rating stars.
- Responsive rule: mobile = single column, bottom nav; web = max-width centered container, left sidebar nav, multi-column grids for book shelves.

TONE: friendly, motivating, literary, trustworthy. UI copy in Uzbek language.

Produce a cohesive design-system overview: a style board showing the palette, typography, and 4–5 key reusable components (book card, bottom nav, button, search bar, progress bar) in both mobile and web layout. // This sets the visual language for all following screens.
```

---

# EKRAN PROMPTLARI (har birini alohida yuguring)

Har bir prompt STITCHga master-promptdagı dizayn tizimi bilan bir xil bo'lishini aytadi.

## 1. Home (Bosh sahifa)
```
Screen: Home dashboard for Kitobdagimen (mobile + web responsive). Modern minimalist design system (deep forest green #2D6A4F accent, white/#F6F7F9 surfaces, Inter + Lora serif, Uzbek UI copy).

Layout:
- Top bar: app logo "Kitobdagimen", greeting "Salom, [Ism]!", notification bell icon.
- "Davom ettirish" (Continue reading) hero card: current book cover, title, author, linear reading-progress bar (e.g. 64%), "Davom etish" button.
- Horizontal carousel "Siz uchun tavsiya" (AI recommendations): book cards (cover, title, author, rating stars).
- Section "Kundalik maqsad" (daily goal): small card with circular progress, pages/min read today, streak counter with amber flame badge.
- "Faol chaqiriqlar" (active challenges) row of pill cards.
- Bottom nav (mobile) / sidebar (web): Bosh, Kutubxona, Xarita, Profil.
Empty/loading states: skeleton cards. // Consistent with the established design system above.
```

## 2. Feed (Lenta)
```
Screen: Social Feed for Kitobdagimen (mobile + web responsive). Same modern minimalist design system, Uzbek copy.

Layout:
- Header "Lenta" with tabs: Hammasi / Kuzatilganlar / Do'stlar.
- Feed of user activity cards: avatar + name + action ("Kitob tugatdi", "Baholadi", "Shorava qo'shdi"), book mini-cover, timestamp, like & comment icons, optional review text.
- "Hikoya" style circles at top: friends' reading streaks.
- Floating "+" button to post a status / finished book.
- Pull-to-refresh, infinite scroll skeleton loaders.
- Bottom nav consistent with Home. // Consistent with the established design system above.
```

## 3. Reader (O'qish ekrani)
```
Screen: In-app Book Reader for Kitobdagimen (mobile + web responsive). Reading-first minimalist, same design system, Uzbek book text in Lora serif.

Layout:
- Distraction-free: white/sepia background, serif body text, comfortable line height.
- Minimal top bar: back arrow, book title (small), A− / A+ font-size control, theme toggle (light/sepia/dark).
- Bottom reader bar: chapter title, progress percentage, previous/next, table-of-contents (TOC) icon, bookmark, AI "Xulosa" button.
- Web: two-page spread option; mobile: single column.
- Highlight-on-tap with popover (highlight / note / share / AI explain). // Consistent with the established design system above.
```

## 4. Book Details (Kitob sahifasi)
```
Screen: Book Detail page for Kitobdagimen (mobile + web responsive). Same minimalist design system, Uzbek copy.

Layout:
- Book cover (large), title, author, average rating stars + review count.
- Genre pill tags; "Muallif haqida" short bio expandable.
- Action buttons: primary "O'qishni boshlash" / "Davom etish", secondary "Kutubxonaga" (saved), "Sotib olish" with price (if paid).
- Tabs: Tavsif | Sharhlar | Bo'limlar. Reviews tab: user reviews with avatar, rating, text, helpful count.
- "O'xshash kitoblar" horizontal carousel.
- AI panel: "AI xulosa", "AI viktorina" buttons. // Consistent with the established design system above.
```

## 5. Search (Qidiruv)
```
Screen: Search & Discover for Kitobdagimen (mobile + web responsive). Same design system, Uzbek copy.

Layout:
- Prominent search bar with placeholder "Kitob, muallif yoki janr izlang…", filter/sort icon.
- Recent searches chips + popular genres as filter pills (Detektiv, Roman, Tarix, Ilm-fan…).
- AI Search: "AI bilan top" natural-language box ("Mening kayfiyatimga mos kitob").
- Results grid: book cards (cover, title, author, rating). Web: multi-column grid; mobile: 2–3 column.
- Empty state: illustration + "Hech narsa topilmadi" with suggestion. // Consistent with the established design system above.
```

## 6. Profile (Profil)
```
Screen: User Profile for Kitobdagimen (mobile + web responsive). Same design system, Uzbek copy.

Layout:
- Header: avatar, display name, @username, bio, "Tahrirlash" button, follower/following counts.
- Stats row: o'qilgan kitoblar, sahifalar, streak, ballar (points).
- Tabs: Kutubxona | Sharhlar | Yutuqlar | Klublar.
- "Kutubxonam" grid of owned/reading books with progress.
- Achievement badges row (amber badges). // Consistent with the established design system above.
```

## 7. Settings (Sozlamalar)
```
Screen: Settings for Kitobdagimen (mobile + web responsive). Same design system, Uzbek copy.

Layout:
- Grouped list rows with icons: Hisob (Profil, Xavfsizlik, Til — O'zbek/Rus/English, Mamlakat), Bildirishnomalar (toggle switches), Ko'rinish (Tema: Yorug'/Sepiya/Qora, Shrift o'lchami), Maxfiylik (Profil ko'rinishi, Kuzatuv), To'lovlar (Obuna, Karta), Yordam (FAQ, Aloqa), Chiqish.
- Toggle switches, chevron rows, danger "Hisobni o'chirish" in muted red. // Consistent with the established design system above.
```

## 8. Chat (Chat)
```
Screen: Direct & Book Club Chat for Kitobdagimen (mobile + web responsive). Same design system, Uzbek copy.

Layout (two-pane on web, single on mobile):
- Left: conversation list (avatars, last message, unread badge, online dot).
- Right: chat thread — incoming/outgoing bubbles (green for outgoing), timestamps, typing indicator, book-share bubble (mini cover + title), attach/AI-summarize icon.
- Bottom: text input + send, emoji, attachment. // Consistent with the established design system above.
```

## 9. Challenge (Chaqiriqlar / Reading Challenge)
```
Screen: Reading Challenge for Kitobdagimen (mobile + web responsive). Same design system, Uzbek copy.

Layout:
- Challenge hero: cover/illustration, title (e.g. "100 kitob — 2026"), description, participants count, days left.
- Personal progress card: circular progress, books completed / target, leaderboard mini-rank.
- Milestones timeline with locked/unlocked badges (amber).
- "Qo'shilganlarim" list + "Yangi chaqiriq" button. // Consistent with the established design system above.
```

## 10. Leaderboard (Reyting)
```
Screen: Leaderboard for Kitobdagimen (mobile + web responsive). Same design system, Uzbek copy.

Layout:
- Tabs: Haftalik | Oylik | Umumiy; scopes: Do'stlar | Umumiy | Klub.
- Ranked list: top-3 podium cards (gold/silver/bronze badges), then numbered rows with avatar, name, points/streak, rank change arrow.
- Current user highlighted row ("Siz — #42").
- Web: table style; mobile: list style. // Consistent with the established design system above.
```

## 11. Store (Do'kon)
```
Screen: Book Store for Kitobdagimen (mobile + web responsive). Same design system, Uzbek copy.

Layout:
- Top promo banner (new releases / discounts), category filter chips.
- Sort: Mashhur | Yangi | Arzon | Baholi.
- Book grid with price tags; "Sotib olish" / "Ijaraga" buttons; subscribed users see "Obuna" badge (free).
- Coupon entry field. Cart icon with badge in top bar. // Consistent with the established design system above.
```

## 12. Map (Xarita — book exchange map)
```
Screen: Nearby Book Exchange Map for Kitobdagimen (mobile + web responsive). Same design system, Uzbek copy.

Layout:
- Full-bleed map with clustered pins (available books nearby), user location dot.
- Bottom sheet: selected pin → book cover, title, owner avatar, distance, "So'rov yuborish" button.
- Filter: janr, masofa slider, faqat almashish/ijaraga.
- List/Map toggle. // Consistent with the established design system above.
```

## 13. Exchange (Kitob almashish)
```
Screen: Book Exchange Requests for Kitobdagimen (mobile + web responsive). Same design system, Uzbek copy.

Layout:
- Tabs: Kelgan so'rovlar | Yuborilgan | Faol almashishlar.
- Request cards: counterpart avatar, book wanted vs offered, status pill (Kutilmoqda / Qabul qilindi / Rad etildi), chat-open button, meetup location/time.
- "Yangi e'lon" to list a book you offer. // Consistent with the established design system above.
```

## 14. Admin Panel (Admin)
```
Screen: Admin Panel dashboard for Kitobdagimen (web-first responsive). Same design system but denser/professional, Uzbek copy.

Layout:
- Left sidebar: Dashboard, Foydalanuvchilar, Kitoblar, Mualliflar, Sharhlar (moderatsiya), To'lovlar, Obunalar, AI nazorat, Analitika, Sozlamalar.
- Top KPI cards: active users, revenue, new books, reports.
- Data tables with sort/filter/pagination: users list, books catalog, flagged content.
- Right: recent activity / alerts.
- Moderation queue with approve/reject actions. // Consistent with the established design system above.
```

## 15. Authentication (Kirish / Ro'yxatdan o'tish) — qo'shimcha
```
Screen: Auth (Login & Sign-up) for Kitobdagimen (mobile + web responsive). Same minimalist design system, Uzbek copy.

Layout:
- Centered card: app logo, "Xush kelibsiz" heading.
- Login: phone/email input, password (show/hide), "Kirish", "Parolni unutdingizmi?".
- Social/Google + "Telegram orqali" OAuth buttons.
- Sign-up toggle: name, phone, email, password, terms checkbox.
- Clean, lots of whitespace, single primary green button. // Consistent with the established design system above.
```

## 16. Notifications (Bildirishnomalar) — qo'shimcha
```
Screen: Notifications for Kitobdagimen (mobile + web responsive). Same design system, Uzbek copy.

Layout:
- Tabs: Hammasi | Eslatmalar | Ijtimoiy | Tizim.
- List rows with icon (book, heart, trophy, system), title, body, timestamp, unread dot.
- Empty state illustration. // Consistent with the established design system above.
```

## 17. Book Clubs (Kitob klublari) — qo'shimcha
```
Screen: Book Clubs for Kitobdagimen (mobile + web responsive). Same design system, Uzbek copy.

Layout:
- "Mening klublarim" + "Top klublar" sections.
- Club cards: cover, name, members count, current book, schedule.
- Club detail: about, members grid, current discussion, "Qo'shilish" button, related chat entry. // Consistent with the established design system above.
```

## 18. Achievements (Yutuqlar) — qo'shimcha
```
Screen: Achievements / Badges for Kitobdagimen (mobile + web responsive). Same design system, Uzbek copy.

Layout:
- Progress to next level card; points total.
- Grid of badges: unlocked (colored, amber accents) vs locked (greyed) with tooltip/description ("10 kitob o'qidi", "30 kun streak").
- "Yutuqlarim" stats. // Consistent with the established design system above.
```

## 19. Moderator Panel (Moderator) — qo'shimcha
```
Screen: Moderator Panel for Kitobdagimen (web-first responsive). Same design system, denser, Uzbek copy.

Layout:
- Sidebar: Moderatsiya navbati, Sharhlar, Foydalanuvchi hisobotlari, AI flaglari, Qoidalar.
- Flagged content queue: preview, reporter reason, AI confidence score, Approve / Reject / Ban actions.
- Filters by content type and severity. // Consistent with the established design system above.
```

## 20. Subscriptions / Payments (Obuna / To'lov) — qo'shimcha
```
Screen: Subscription & Payment for Kitobdagimen (mobile + web responsive). Same design system, Uzbek copy.

Layout:
- Plan cards: Bepul | Premium (oylik/yillik toggle with discount badge). Feature comparison checklist.
- Payment sheet: card fields, coupon, "To'lash" primary button, saved methods.
- Order/receipt confirmation state. // Consistent with the established design system above.
```

---

## Maslahatlar (STITCH natijasini yaxshilash uchun)
- Har bir ekran promptini alohida yuguring, chunki STITCH bitta ekranga e'tibor qaratganda ancha detallı chiqaradi.
- "Uzbek UI copy" iborasini o'chirmang — aks holda STITCH inglizcha matn qo'yadi.
- Agar STITCH noto'g'ri palit qo'ysa, master-promptdagi aniq hex kodlarni (#2D6A4F) ekran promptiga ham qo'shib qo'ying.
- Web vs mobile farqini aniq aytdik ("web-first", "two-pane", "bottom nav") — agar faqat bitta kerak bo'lsa, tegishli qismni o'chiring.
```
