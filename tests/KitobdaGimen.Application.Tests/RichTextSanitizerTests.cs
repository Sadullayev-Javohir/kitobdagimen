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
    public void Strips_script_and_other_tags()
    {
        var result = RichTextSanitizer.Sanitize("<b>ok</b><script>alert(1)</script><a href=\"x\">link</a>");
        Assert.DoesNotContain("<script", result);
        Assert.DoesNotContain("<a", result);
        Assert.Contains("<b>ok</b>", result);
        // Tegishsiz teglar matn sifatida ko'rsatiladi (encode qilingan).
        Assert.Contains("&lt;script&gt;", result);
    }

    [Fact]
    public void Drops_attributes_from_allowed_tags()
    {
        // Atributli ruxsat etilgan teg REAL teg sifatida tiklanmaydi — u encode
        // bo'lib matn sifatida ko'rsatiladi, shuning uchun onclick ishlamaydi.
        var result = RichTextSanitizer.Sanitize("<b onclick=\"x\">hi</b>");
        Assert.DoesNotContain("<b onclick", result);
        Assert.Contains("&lt;b onclick", result);
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
        // Render paytida qayta chaqirilganda ikki marta kodlanmasligi kerak.
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
}
