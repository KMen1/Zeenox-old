using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Discord.Interactions;
using Discord.WebSocket;
using KBot.Enums;
using KBot.Helpers;
using KBot.Services;
using Victoria.Filters;

namespace KBot.Modules.Voice;

public class Components : InteractionModuleBase<SocketInteractionContext>
{
    public AudioService AudioService { get; set; }
    
    [ComponentInteraction("filterselectmenu")]
    public async Task HandleFilterSelectMenu(params string[] selections)
    {
        var selection = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(selections[0].ToLower());

        var result = Enum.TryParse(selection, out FilterType filterType);
        if (result)
        {
            await DeferAsync();
            await AudioService.SetFiltersAsync(Context.Guild, filterType);
        }
    }
    
    [ComponentInteraction("stop")]
    public async Task Stop()
    {
        await AudioService.StopAsync(Context.Guild);
        await AudioService.DisconnectAsync(Context.Guild);
        await ((SocketMessageComponent) Context.Interaction).Message.DeleteAsync();
    }
    [ComponentInteraction("volumeup")]
    public async Task VolumeUp()
    {
        await AudioService.SetVolumeAsync(Context.Guild, VoiceButtonType.VolumeUp);
        await DeferAsync();
    }
    [ComponentInteraction("volumedown")]
    public async Task VolumeDown()
    {
        await AudioService.SetVolumeAsync(Context.Guild, VoiceButtonType.VolumeDown);
        await DeferAsync();
    }

    [ComponentInteraction("pause")]
    public async Task Pause()
    {
        await AudioService.PauseOrResumeAsync(Context.Guild);
        await DeferAsync();
    }
    [ComponentInteraction("next")]
    public async Task Next()
    {
        await AudioService.PlayNextTrack(Context.Guild, Context.User);
        await DeferAsync();
    }
    [ComponentInteraction("previous")]
    public async Task Previous()
    {
        await AudioService.PlayPreviousTrack(Context.Guild, Context.User);
        await DeferAsync();
    }
    [ComponentInteraction("repeat")]
    public async Task Repeat()
    {
        await AudioService.SetRepeatAsync(Context.Guild);
        await DeferAsync();
    }

    [ComponentInteraction("clearfilters")]
    public async Task ClearFilters()
    {
        await AudioService.ClearFiltersAsync(Context.Guild);
        await DeferAsync();
    }
}