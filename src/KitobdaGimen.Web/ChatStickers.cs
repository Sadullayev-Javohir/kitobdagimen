namespace KitobdaGimen.Web;

/// <summary>
/// Built-in chat stickers rendered as modern line-art SVGs (stroke-based, matching the app icon
/// style). The same map is used for server-side render of existing messages and serialised into
/// the client script so newly sent stickers render identically. Keys are stored on
/// <c>Message.StickerKey</c>.
/// </summary>
public static class ChatStickers
{
    /// <summary>Ordered sticker key → inline SVG markup.</summary>
    public static readonly IReadOnlyDictionary<string, string> All = new Dictionary<string, string>
    {
        ["book"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M4 19.5A2.5 2.5 0 0 1 6.5 17H20\"/><path d=\"M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z\"/></svg>",
        ["open"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M2 5.5C4 4 7 4 9 5.2c.6.4 1 1 1 1.8V20c0-.9-.4-1.5-1-1.9C8 17 5 17 3 18.3c-.6.4-1-.1-1-.8V6.3c0-.4.2-.7.5-.8z\"/><path d=\"M22 5.5C20 4 17 4 15 5.2c-.6.4-1 1-1 1.8V20c0-.9.4-1.5 1-1.9 2-1.1 5-1.1 7 .2.6.4 1-.1 1-.8V6.3c0-.4-.2-.7-.5-.8z\"/></svg>",
        ["bookmark"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z\"/></svg>",
        ["quill"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M20 4C10 6 6.5 11 4 20c3-3 6-4 9-4l7-12z\"/><path d=\"M4 20l5-5\"/><path d=\"M14.5 7.5c-2 1-3.5 2.5-4.5 4.5\"/></svg>",
        ["glasses"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><circle cx=\"5.5\" cy=\"15\" r=\"3.5\"/><circle cx=\"18.5\" cy=\"15\" r=\"3.5\"/><path d=\"M9 15c0-1.5 1.2-2.5 3-2.5s3 1 3 2.5\"/><path d=\"M2 12l2.5-4.5\"/><path d=\"M22 12l-2.5-4.5\"/></svg>",
        ["coffee"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M4 9h13v5a5 5 0 0 1-5 5H9a5 5 0 0 1-5-5z\"/><path d=\"M17 10h2.5a2.5 2.5 0 0 1 0 5H17\"/><path d=\"M7 2c-.6 1 .6 2 0 3M11 2c-.6 1 .6 2 0 3\"/></svg>",
        ["star"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M12 3l2.7 5.5 6 .9-4.35 4.24 1 6-5.35-2.8-5.35 2.8 1-6L3.3 9.4l6-.9z\"/></svg>",
        ["heart"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M20.8 5.6a5 5 0 0 0-7.1 0L12 7.3l-1.7-1.7a5 5 0 1 0-7.1 7.1L12 21l8.8-8.3a5 5 0 0 0 0-7.1z\"/></svg>",
        ["moon"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z\"/><path d=\"M18 4l.6 1.6L20 6l-1.4.4L18 8l-.6-1.6L16 6l1.4-.4z\"/></svg>",
        ["bulb"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M9 18h6\"/><path d=\"M10 22h4\"/><path d=\"M12 2a7 7 0 0 0-4 12.7c.6.5 1 1.3 1 2.1h6c0-.8.4-1.6 1-2.1A7 7 0 0 0 12 2z\"/></svg>"
    };
}
