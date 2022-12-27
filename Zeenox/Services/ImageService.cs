/*using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Zeenox.Extensions;
using SkiaSharp;
using Topten.RichTextKit;

namespace Zeenox.Services;

public class ImageService
{
    private static readonly SKBitmap WelcomeBitmap = SKBitmap.Decode(
        File.Open("Resources/welcome.png", FileMode.Open, FileAccess.Read)
    );
    private static readonly SKSurface WelcomeSurface = SKSurface.Create(new SKImageInfo(600, 300));
    private static readonly SKBitmap LevelBitmap = SKBitmap.Decode(
        File.Open("Resources/level.png", FileMode.Open, FileAccess.Read)
    );
    private static readonly SKSurface LevelSurface = SKSurface.Create(new SKImageInfo(600, 194));
    private readonly HttpClient _httpClient;

    public ImageService(HttpClient httpClient)
    {
        //_httpClient = httpClient;
        using var family = SKTypeface.FromFile("Resources/fonts/sfregular.ttf");
        var font = family.ToFont();
        font.Size = 72;
    }

    /*public async Task<Stream> CreateWelcomeImageAsync(SocketGuildUser user)
    {
        var avatar = await GetSkBitmapAvatar(user).ConfigureAwait(false);

        var canvas = WelcomeSurface.Canvas;
        canvas.Clear();
        canvas.DrawBitmap(WelcomeBitmap, 0, 0);
        canvas.DrawBitmap(avatar, 38, 38);

        var rs = new RichString()
            .Alignment(TextAlignment.Center)
            .TextColor(SKColors.White)
            .FontFamily("SF Pro Text")
            .HaloColor(SKColor.Parse("#858585"))
            .HaloBlur(10)
            .HaloWidth(3)
            .Add("Welcome", fontSize: 50, fontWeight: 700)
            .Paragraph()
            .Add($"{user.Username}#{user.Discriminator}", fontSize: 20, fontWeight: 200)
            .MarginBottom(10)
            .Paragraph()
            .Add(
                $"You are the {user.Guild.Users.Count:N0}th member!",
                fontSize: 20,
                fontWeight: 200
            );

        var op = new TextPaintOptions {Edging = SKFontEdging.SubpixelAntialias};

        rs.MaxWidth = 260;
        rs.MaxHeight = 300;
        rs.Paint(canvas, new SKPoint(280, 70), op);

        return WelcomeSurface.Snapshot().Encode(SKEncodedImageFormat.Png, 100).AsStream();
    }

    public async Task<Stream> CreateLevelImage(SocketUser user, int level, int xp, int required)
    {
        var avatar = await GetSkBitmapAvatar(user).ConfigureAwait(false);

        var canvas = LevelSurface.Canvas;
        canvas.Clear();
        canvas.DrawBitmap(LevelBitmap, 0, 0);
        canvas.DrawBitmap(avatar.Resize(new SKImageInfo(145, 145), SKFilterQuality.High), 27, 25);

        var progress = (float) xp / required;
        var progressBar = new SKRoundRect(new SKRect(0, 0, (int) (progress * 375), 31), 15);

        var progressBarPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            Color = SKColors.White,
            IsStroke = false
        };

        using var clipSurface = SKSurface.Create(
            new SKImageInfo((int) progressBar.Width, (int) progressBar.Height)
        );
        using var clipCanvas = clipSurface.Canvas;
        clipCanvas.Clear(SKColors.Transparent);
        clipCanvas.DrawRoundRect(progressBar, progressBarPaint);
        using var t = SKBitmap.FromImage(clipSurface.Snapshot());
        canvas.DrawBitmap(ClipProgressBar(t), 188, 128);

        var rankAndXp = new RichString()
            .Alignment(TextAlignment.Right)
            .TextColor(SKColors.White)
            .FontFamily("SF Pro Text")
            .Paragraph()
            .Add("RANK ", fontSize: 15, fontWeight: 200)
            .Add("#102", fontSize: 30, fontWeight: 700)
            .MarginBottom(26)
            .Paragraph()
            .Add($"{xp:N0}/{required:N0} XP", fontSize: 12, fontWeight: 200);
        var op = new TextPaintOptions {Edging = SKFontEdging.SubpixelAntialias};
        rankAndXp.MaxWidth = 364;
        rankAndXp.MaxHeight = 200;
        rankAndXp.Paint(canvas, new SKPoint(190, 13), op);

        var nameAndLevel = new RichString()
            .Alignment(TextAlignment.Left)
            .TextColor(SKColors.White)
            .FontFamily("SF Pro Text")
            .Paragraph()
            .Add("KMen", fontSize: 30, fontWeight: 700)
            .MarginBottom(20)
            .Paragraph()
            .Add("LEVEL ", fontSize: 15, fontWeight: 200)
            .Add($"{level}", fontSize: 20, fontWeight: 700);
        nameAndLevel.MaxWidth = 364;
        nameAndLevel.MaxHeight = 200;
        nameAndLevel.Paint(canvas, new SKPoint(195, 13), op);

        return LevelSurface.Snapshot().Encode(SKEncodedImageFormat.Png, 100).AsStream();
    }

    private async Task<SKBitmap> GetSkBitmapAvatar(IUser user)
    {
        var avatarData = await _httpClient
            .GetStreamAsync(user.GetAvatarUrl(size: 256))
            .ConfigureAwait(false);

        var avatarStream = new MemoryStream();
        await avatarData.CopyToAsync(avatarStream).ConfigureAwait(false);
        avatarStream.Position = 0;
        using var avatar = SKBitmap.Decode(avatarStream);
        return avatar.Resize(new SKSizeI(225, 225), SKFilterQuality.High).MakeImageRound();
    }

    private static SKBitmap ClipProgressBar(SKBitmap image)
    {
        var roundedImage = new SKBitmap(image.Width, image.Height);
        using var canvas = new SKCanvas(roundedImage);
        canvas.Clear(SKColors.Transparent);
        using var path = new SKPath();

        path.AddRoundRect(new SKRoundRect(new SKRect(0, 0, 375, 31), 15));
        canvas.ClipPath(path, SKClipOperation.Intersect, true);
        canvas.DrawBitmap(image, new SKPoint(0, 0));
        return roundedImage;
    }
}*/


