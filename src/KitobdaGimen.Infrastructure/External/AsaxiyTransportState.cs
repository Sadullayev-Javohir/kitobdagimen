namespace KitobdaGimen.Infrastructure.External;

/// <summary>
/// Jarayon bo'yicha yagona (singleton), thread-xavfsiz transport tanlash holati.
/// asaxiy.uz ni bir nechta yo'l (transport) orqali o'qishga urinamiz: Cloudflare
/// Worker → SOCKS proksi → to'g'ridan-to'g'ri → Jina Reader. Qaysi transport oxirgi
/// marta ishlaganini eslab qolamiz ("sticky"), shunda har so'rovda ishlamaydigan
/// transportga vaqt sarflamaymiz.
///
/// MUHIM: hech qachon butunlay o'chirmaydi — istalgan so'rov (yoki "Kitoblarni
/// yangilash" tugmasi) transportlarni yana boshidan sinab ko'radi. Shu sabab servis
/// "umrbod" o'zini-o'zi tiklaydigan bo'ladi.
/// </summary>
public sealed class AsaxiyTransportState
{
    private volatile int _preferredIndex;

    /// <summary>Birinchi bo'lib sinaladigan transport indeksi (oxirgi ishlagan).</summary>
    public int PreferredIndex => _preferredIndex;

    /// <summary>Muvaffaqiyatli ishlagan transportni keyingi safar uchun eslab qoladi.</summary>
    public void Remember(int index) => _preferredIndex = index < 0 ? 0 : index;

    /// <summary>Holatni tiklaydi — keyingi so'rov transportlarni boshidan sinaydi.</summary>
    public void Reset() => _preferredIndex = 0;
}
