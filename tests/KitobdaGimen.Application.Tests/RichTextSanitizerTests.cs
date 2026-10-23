using KitobdaGimen.Application.Common;

namespace KitobdaGimen.Application.Tests;

public class RichTextSanitizerTests
{
    [Fact]
    public void Keeps_allowed_inline_tags()
    {
        var result = RichTextSanitizer.Sanitize("<b>qalin</b> <i>kursiv</i> <u>tag</u> <mark>marker</mark>");
        Assert.Equal("<b>qalin</b> <i>kursiv</i> <u>tag</u> <mark>marker</mark>", result);
    }

    [Fact]
    public void Strips_paragraphs_and_containers_without_leaving_p_tags()
    {
        var result = RichTextSanitizer.Sanitize("<p>Birinchi paragraf</p><p>Ikkinchi paragraf</p>");
        Assert.DoesNotContain("<p", result);
        Assert.DoesNotContain("&lt;p", result);
        Assert.Contains("Birinchi paragraf\n\nIkkinchi paragraf", result);
    }

    [Fact]
    public void Strips_p_with_attributes_and_keeps_clean_formatting()
    {
        var result = RichTextSanitizer.Sanitize("<p dir=\"ltr\"><strong>Baho: ⭐⭐⭐⭐⭐ (5/5)</strong></p><p>Ajoyib kitob!</p>");
        Assert.DoesNotContain("<p", result);
        Assert.DoesNotContain("&lt;p", result);
        Assert.Contains("<b>Baho: ⭐⭐⭐⭐⭐ (5/5)</b>\n\nAjoyib kitob!", result);
    }

    [Fact]
    public void Strips_script_and_style_blocks()
    {
        var result = RichTextSanitizer.Sanitize("<b>ok</b><script>alert(1)</script><a href=\"x\">link</a>");
        Assert.DoesNotContain("<script", result);
        Assert.DoesNotContain("alert(1)", result);
        Assert.DoesNotContain("<a", result);
        Assert.Equal("<b>ok</b>link", result);
    }

    [Fact]
    public void Drops_attributes_from_allowed_tags()
    {
        var result = RichTextSanitizer.Sanitize("<b onclick=\"alert('hack')\">hi</b>");
        Assert.DoesNotContain("onclick", result);
        Assert.Equal("<b>hi</b>", result);
    }

    [Fact]
    public void Normalizes_strong_and_em_aliases()
    {
        var result = RichTextSanitizer.Sanitize("<strong>a</strong><em>b</em>");
        Assert.Equal("<b>a</b><i>b</i>", result);
    }

    [Fact]
    public void Converts_br_and_block_ends_to_newlines()
    {
        var result = RichTextSanitizer.Sanitize("birinchi<br>ikkinchi<div>uchinchi</div>");
        Assert.Contains("birinchi\nikkinchi", result);
        Assert.Contains("uchinchi", result);
    }

    [Fact]
    public void Is_idempotent_safe_for_already_sanitized_text()
    {
        var once = RichTextSanitizer.Sanitize("<b>Tom & Jerry</b> 5 < 10");
        var twice = RichTextSanitizer.Sanitize(once);
        Assert.Equal(once, twice);
        Assert.Contains("&amp;", once);
        Assert.Contains("&lt;", once);
    }

    [Fact]
    public void Plain_old_text_with_angle_brackets_is_safe()
    {
        var result = RichTextSanitizer.Sanitize("oddiy matn <3 va & belgisi");
        Assert.Contains("&lt;3", result);
        Assert.Contains("&amp;", result);
        Assert.DoesNotContain("<3", result.Replace("&lt;3", ""));
    }

    [Fact]
    public void Empty_or_whitespace_returns_empty()
    {
        Assert.Equal(string.Empty, RichTextSanitizer.Sanitize(null));
        Assert.Equal(string.Empty, RichTextSanitizer.Sanitize("   "));
    }

    [Fact]
    public void StripHtml_removes_all_tags_and_preserves_multiline_paragraphs()
    {
        var html = "<p><strong>Baho: ⭐⭐⭐⭐⭐ (5/5)</strong></p><p>Kdkdd f f.<br>Yangi qator.</p>";
        var plain = RichTextSanitizer.StripHtml(html);
        Assert.DoesNotContain("<p", plain);
        Assert.DoesNotContain("<strong>", plain);
        Assert.Contains("Baho: ⭐⭐⭐⭐⭐ (5/5)\n\nKdkdd f f.\nYangi qator.", plain);
    }

    [Fact]
    public void StripHtml_cleans_already_encoded_p_entities()
    {
        var encoded = "&lt;p&gt;&lt;strong&gt;Baho: ⭐⭐⭐⭐⭐ (5/5)&lt;/strong&gt;&lt;/p&gt;&lt;p&gt;Matn&lt;/p&gt;";
        var plain = RichTextSanitizer.StripHtml(encoded);
        Assert.DoesNotContain("<p", plain);
        Assert.DoesNotContain("&lt;p", plain);
        Assert.Contains("Baho: ⭐⭐⭐⭐⭐ (5/5)\n\nMatn", plain);
    }

    [Fact]
    public void StripHtml_cleans_jackpot_review_text_with_html_tags_and_entities()
    {
        var raw = "<p>Oddiy maqsaddan boshlang. Cheklangan qobilyatingiz, aldashga moyilligingiz, noroziligingiz va ma&#39;suliyatdan qochishingiz bilan birdaniga og&#39;ir yukni ko&#39;taraolmaysiz. Masalan, shunday maqsad qo&#39;ying: kun oxirida hayotim tongdagidan ozgina yaxshiroq bo&#39;lsin. So&#39;ngra o&#39;zingizdan so&#39;rang: &quot;Bunga erishish uchun nima qilish qo&#39;limdan keladi, nima qilaman, evaziga qanday kichik mukofot bilan o&#39;zimni siylayman?&quot; Keyin tanlangan amalingizni bajaring, uni yomon bajarsangiz ham. So&#39;ng g&#39;olib sifatida o&#39;zingizni o&#39;sha la&#39;nati qavha bilan siylang. Biroz ahmoqona tuyulishi mumkin, lekin shunday davom etish kerak. Ertaga, indinga - har kuni shuni takrorlang. Shu taxlit har kuni marrangiz yuqorilaydi. Bu mo&#39;jiza. Bu omonatning murakkab foiziga o&#39;xshaydi. Uch yil shunday davom eting, hayotingiz butkul o&#39;zgaradi. Kattaroq oldidagi parda olinadi va barchasini ko&#39;ra boshlaysiz. Mo&#39;ljallanganlaringiz esa ko&#39;radiganlaringizni belgilaydi. Bu qaytarishga arziydi.</p>";
        var clean = RichTextSanitizer.StripHtml(raw);

        Assert.DoesNotContain("<p>", clean);
        Assert.DoesNotContain("</p>", clean);
        Assert.DoesNotContain("&#39;", clean);
        Assert.DoesNotContain("&quot;", clean);
        Assert.Contains("ma'suliyatdan", clean);
        Assert.Contains("og'ir", clean);
        Assert.Contains("\"Bunga erishish", clean);
        Assert.Contains("o'zingizni", clean);
        Assert.Contains("g'olib", clean);
    }
}
