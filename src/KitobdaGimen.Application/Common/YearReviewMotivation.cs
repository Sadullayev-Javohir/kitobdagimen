namespace KitobdaGimen.Application.Common;

/// <summary>
/// Har bir foydalanuvchi uchun <b>noyob</b> (bir-biriga o'xshamaydigan) motivatsion xabar
/// va yangi-yilona dizayn variantini generatsiya qiladi. Xabar to'rt mustaqil qismdan
/// (kirish, maqtov, mulohaza, tilak) yig'iladi; foydalanuvchi id'si kombinatsiya fazosiga
/// <b>bijektiv</b> (teskari-yagona) tarzda joylashtiriladi — shu sababli turli
/// foydalanuvchilar (kombinatsiyalar soni chegarasida) turlicha xabar oladi. Natija
/// deterministik: bir foydalanuvchi har safar aynan bir xabarni ko'radi.
/// </summary>
public static class YearReviewMotivation
{
    // ── Xabar qismlari (har biri mustaqil tanlanadi) ──────────────────────────────

    private static readonly string[] Openers =
    {
        "Bu yil sahifalar orasida sayohat qilding.",
        "Yil davomida kitoblar seni yangi olamlarga olib bordi.",
        "Bu yilgi o'qishlaring seni oldingidan-da kuchli qildi.",
        "Har bir ochilgan kitob — o'zingga qo'ygan yangi qadam bo'ldi.",
        "So'zlar seni ilhomlantirdi, hikoyalar seni o'stirdi.",
        "Bu yil sen fikrlaringni kitoblar bilan boyitding.",
        "Sahifadan sahifaga — sen bilimga oshno bo'lding.",
        "Bu yil kitob seniki, sen esa kitobniki bo'lding.",
        "O'qigan har bir satr yuragingda iz qoldirdi.",
        "Bu yilgi sarguzashting muqovalar ortida yashirin edi.",
        "Kitoblar bilan o'tgan daqiqalaring behuda ketmadi.",
        "Bu yil sen tinimsiz izlanding va o'qib o'sding.",
        "Yil bo'yi harflar seni yetaklab, ufqlaringni kengaytirdi.",
        "Bu yil o'qish sening eng sodiq hamrohing bo'ldi.",
    };

    private static readonly string[] Praises =
    {
        "Sabring va qiziqishing havas qilsa arziydigan darajada.",
        "Bunday izchillik har kimga ham nasib etmaydi.",
        "Sen o'qishni odatga aylantirding — bu katta yutuq.",
        "Har kuni ozgina o'qish — kelajakka qilingan katta sarmoya.",
        "Bu intilishing atrofdagilar uchun ham namuna bo'ladi.",
        "Sen faqat o'qimading — o'ylab, his qilib o'qiding.",
        "Iroda va matonating sahifalarda aks etdi.",
        "Bunday kitobxonlik ruhi chinakam boylik.",
        "Sen bilimga chanqoqligingni isbotlading.",
        "O'z ustingda ishlashing hurmatga loyiq.",
        "Sen vaqtingni eng qadrli narsaga — o'sishga sarflading.",
        "Bu yilgi mehnating albatta samara beradi.",
        "Sening qat'iyating sokin, ammo kuchli edi.",
        "Sen kamtarona, lekin ishonchli qadamlar bilan yurding.",
    };

    private static readonly string[] Reflections =
    {
        "Kitob — bu yolg'iz emasligingni eslatuvchi do'st.",
        "Har bir hikoya senga yangi bir nigoh sovg'a qildi.",
        "O'qigan narsang seni sen qilib shakllantiradi.",
        "Bilim — hech kim tortib ololmaydigan xazina.",
        "Bugungi o'qishing ertangi sen uchun tayyorgarlik.",
        "Sahifalar ortida butun bir dunyo yashiringan edi.",
        "Har bir kitob — boshqa bir hayotni yashab ko'rish.",
        "O'qish seni sabrli va mehribon insonga aylantiradi.",
        "Fikrlaring kengaydi, dilingga nur qo'shildi.",
        "So'zlar ko'ngil bog'ini sug'organ yomg'ir kabidir.",
        "Kitob o'qigan inson hech qachon adashib qolmaydi.",
        "Sen bilimni to'plading, bilim esa seni ko'tardi.",
    };

    private static readonly string[] Wishes =
    {
        "Kelasi yil bundan-da ko'p sahifalar seni kutmoqda!",
        "Yangi yilda yangi kitoblar, yangi orzular bo'lsin!",
        "Kelgusi yil ham shu ruh bilan davom et!",
        "Qo'lingdan kitob tushmasin, ko'nglingdan orzu!",
        "Yangi yil senga tinch, unumli o'qishlar keltirsin!",
        "Kelasi yakun bundan-da yorqinroq bo'lishiga ishonaman!",
        "Har bir yangi kitob senga baxt olib kelsin!",
        "Yangi yilda ming sahifa, ming taassurot senga yor bo'lsin!",
        "O'qishdan to'xtama — eng yaxshi boblar hali oldinda!",
        "Kelasi yil ham kitob javoning to'lib-toshsin!",
        "Yangi yil muborak, aziz kitobxon — o'qishda davom!",
        "Orzularing kitoblaringdek chuqur va go'zal bo'lsin!",
    };

    /// <summary>Dizayn variantlari soni (year-review.css dagi .yr-theme-N bilan mos).</summary>
    public const int ThemeCount = 8;

    /// <summary>Har bir dizayn variantiga mos bayram emoji to'plami (kartochkada aksent uchun).</summary>
    private static readonly string[][] EmojiSets =
    {
        new[] { "🎄", "✨", "📚" },
        new[] { "❄️", "📖", "🌟" },
        new[] { "🎁", "📕", "💫" },
        new[] { "🕯️", "📗", "⭐" },
        new[] { "🎉", "📘", "🌙" },
        new[] { "🔥", "📙", "✨" },
        new[] { "🌲", "📔", "💛" },
        new[] { "☃️", "📚", "🌟" },
    };

    /// <summary>Generatsiya natijasi.</summary>
    public sealed record Result(
        string Message,
        int ThemeVariant,
        IReadOnlyList<string> Emojis,
        string PrimaryEmoji);

    /// <summary>
    /// Berilgan foydalanuvchi (va yil) uchun noyob motivatsion xabar va dizayn variantini
    /// qaytaradi. Statistika (kitob/bet soni) xabarga shaxsiy tus berish uchun ishlatiladi.
    /// </summary>
    public static Result For(int userId, int year, int booksRead, int totalPages, int activeDays)
    {
        // Kombinatsiya fazosi hajmi va foydalanuvchini unga bijektiv joylashtirish.
        long total = (long)Openers.Length * Praises.Length * Reflections.Length * Wishes.Length;
        long key = ((long)userId * 2 + year) % total; // yil ham ta'sir qiladi
        if (key < 0) key += total;

        long k = NextCoprime(total);                  // gcd(k, total) = 1 → bijeksiya
        long index = (key * k) % total;               // teskari-yagona aralashtirish

        int wi = (int)(index % Wishes.Length); index /= Wishes.Length;
        int ri = (int)(index % Reflections.Length); index /= Reflections.Length;
        int pi = (int)(index % Praises.Length); index /= Praises.Length;
        int oi = (int)(index % Openers.Length);

        // Statistikaga asoslangan shaxsiy jumla (raqamlar bilan) — xabar boshiga qo'shiladi.
        var statLine = BuildStatLine(booksRead, totalPages, activeDays);

        var message = $"{statLine} {Openers[oi]} {Praises[pi]} {Reflections[ri]} {Wishes[wi]}";

        // Dizayn varianti — mustaqil aralashtirish (xabardan farqli o'zgarsin).
        uint themeSeed = unchecked((uint)userId * 2654435761u ^ (uint)(year * 40503));
        int theme = (int)(themeSeed % ThemeCount);
        var emojis = EmojiSets[theme];

        return new Result(message.Trim(), theme, emojis, emojis[0]);
    }

    private static string BuildStatLine(int booksRead, int totalPages, int activeDays)
    {
        if (booksRead <= 0 && totalPages <= 0)
        {
            return "Bu yil o'qish sari ilk qadamlaringni qo'yding.";
        }

        if (booksRead <= 0)
        {
            return $"Bu yil jami {totalPages} bet o'qib, o'zingga ishonch qo'shding.";
        }

        return $"Bu yil {booksRead} ta kitobda {totalPages} bet o'qiding" +
               (activeDays > 0 ? $" — {activeDays} kun kitob bilan birga bo'lding." : ".");
    }

    /// <summary>total bilan o'zaro tub bo'lgan kichik konstanta (bijeksiya ko'paytiruvchisi).</summary>
    private static long NextCoprime(long total)
    {
        // total dan katta bo'lmagan, ammo u bilan o'zaro tub bo'lgan toq son.
        long candidate = total / 2 + 1;
        if (candidate % 2 == 0) candidate++;
        while (Gcd(candidate, total) != 1)
        {
            candidate += 2;
            if (candidate >= total) candidate = 3; // ehtiyot chorasi
        }
        return candidate;
    }

    private static long Gcd(long a, long b)
    {
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }
        return a < 0 ? -a : a;
    }
}
