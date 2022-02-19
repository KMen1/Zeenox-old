using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using OsuSharp.Domain;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;
using IUser = OsuSharp.Interfaces.IUser;

namespace KBot.Modules.Utility;

public class Test : KBotModuleBase
{
    [RequireOwner]
    [SlashCommand("osutoptest", "Osu top test with pic")]
    public async Task TestCommand()
    {
        var sw = new Stopwatch();
        sw.Start();
        await DeferAsync().ConfigureAwait(false);
        var users = await Database.GetOsuIdsAsync(Context.Guild.Id, 10).ConfigureAwait(false);
        var userOsuPair = new Dictionary<SocketUser, IUser>();
        foreach (var (userId, osuId) in users)
        {
            userOsuPair.Add(Context.Client.GetUser(userId), await OsuClient.GetUserAsync((long)osuId, GameMode.Osu).ConfigureAwait(false));
        }
        var userOsuPairList = userOsuPair.ToList();
        userOsuPairList.Sort((x, y) => x.Value.Statistics.GlobalRank.CompareTo(y.Value.Statistics.GlobalRank));
        using var http = new HttpClient();
        var image = CropToSize(Image.FromFile("back.png"), 350, 548);
        
        using var g = Graphics.FromImage(image);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        var userFont = new Font("Segoe UI", 13, FontStyle.Bold);
        var rankFont = new Font("Segoe UI", 6, FontStyle.Bold);
        var x = (image.Width / 2) - 100; //42;
        var y = 28;

        var i = 0;
        foreach (var (user, osuUser) in userOsuPairList.Take(10))
        {
            i++;
            g.DrawString($"{i}.", new Font("Segoe UI", 20, FontStyle.Bold), Brushes.White, x - 35, y);
            g.DrawImage(CropToSize(ClipImageToCircle(Image.FromStream(await http.GetStreamAsync(user.GetAvatarUrl()).ConfigureAwait(false))), 40, 40), x, y);
            g.DrawString($"{user.Username}#{user.Discriminator}", userFont, Brushes.White, x + 47, y + 2);
            g.DrawString($"#{osuUser.Statistics.GlobalRank:n0} ({Math.Round(osuUser.Statistics.Pp)} PP)", rankFont, Brushes.LightGray, x + 58, y + 25);
            y += 50;
        }

        image.Save("reset.png");
        sw.Stop();
        await FollowupWithFileAsync(new FileAttachment("reset.png"), sw.Elapsed.ToString()).ConfigureAwait(false);
    }
    
    private static Image ClipImageToCircle(Image image)
    {
        Image destination = new Bitmap(image.Width, image.Height, image.PixelFormat);
        var radius = image.Width / 2;
        var x = image.Width / 2;
        var y = image.Height / 2;

        using var g = Graphics.FromImage(destination);
        var r = new Rectangle(x - radius, y - radius, radius * 2, radius * 2);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var brush = new SolidBrush(Color.Transparent);
        g.FillRectangle(brush, 0, 0, destination.Width, destination.Height);

        using var path = new GraphicsPath();
        path.AddEllipse(r);
        g.SetClip(path);
        g.DrawImage(image, 0, 0);
        return destination;
    }

    private static Bitmap CropToSize(Image image, int width, int height)
    {
        var originalWidth = image.Width;
        var originalHeight = image.Height;
        var destinationSize = new Size(width, height);

        var heightRatio = (float) originalHeight / destinationSize.Height;
        var widthRatio = (float) originalWidth / destinationSize.Width;

        var ratio = Math.Min(heightRatio, widthRatio);

        var heightScale = Convert.ToInt32(destinationSize.Height * ratio);
        var widthScale = Convert.ToInt32(destinationSize.Width * ratio);

        var startX = (originalWidth - widthScale) / 2;
        var startY = (originalHeight - heightScale) / 2;

        var sourceRectangle = new Rectangle(startX, startY, widthScale, heightScale);
        var bitmap = new Bitmap(destinationSize.Width, destinationSize.Height);
        var destinationRectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

        using var g = Graphics.FromImage(bitmap);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(image, destinationRectangle, sourceRectangle, GraphicsUnit.Pixel);

        return bitmap;
    }
}