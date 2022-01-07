using System;
using System.Collections.Generic;
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
        var filters = new List<IFilter>();
        var equalizerBands = Array.Empty<EqualizerBand>();
        var filtersName = new List<string>();

        await DeferAsync();
        
        foreach (var selection in selections)
        {
            switch (selection)
            {
                case "bassboost":
                {
                    equalizerBands = FilterHelper.BassBoost();
                    filtersName.Add("Basszus Erősítés");
                    break;
                }
                case "nightcore":
                {
                    filters.Add(FilterHelper.NightCore());
                    filtersName.Add("Nightcore");
                    break;
                }
                case "eightd":
                {
                    filters.Add(FilterHelper.EightD());
                    filtersName.Add("8D");
                    break;
                }
                case "vaporwave":
                {
                    filters.Add(FilterHelper.VaporWave());
                    filtersName.Add("Vaporwave");
                    break;
                }
                    
            }
        }
        
        await FollowupAsync(embed: await AudioService.SetFiltersAsync(Context.Guild, filters, equalizerBands, filtersName.ToArray()), ephemeral:true);
    }
    
    [ComponentInteraction("stop")]
    public async Task Stop()
    {
        await AudioService.StopAsync(Context.Guild);
        await AudioService.LeaveAsync(Context.Guild);
        await ((SocketMessageComponent) Context.Interaction).Message.DeleteAsync();
    }
    [ComponentInteraction("volumeup")]
    public async Task VolumeUp()
    {
        await AudioService.SetVolumeAsync(Context.Guild, VoiceButtonType.VolumeUp);
        await Context.Interaction.DeferAsync();
    }
    [ComponentInteraction("volumedown")]
    public async Task VolumeDown()
    {
        await AudioService.SetVolumeAsync(Context.Guild, VoiceButtonType.VolumeDown);
        await Context.Interaction.DeferAsync();
    }

    [ComponentInteraction("pause")]
    public async Task Pause()
    {
        await AudioService.PauseOrResumeAsync(Context.Guild);
        await Context.Interaction.DeferAsync();
    }
    [ComponentInteraction("next")]
    public async Task Next()
    {
        await AudioService.PlayNextTrack(Context.Guild, Context.User);
        await Context.Interaction.DeferAsync();
    }
    [ComponentInteraction("previous")]
    public async Task Previous()
    {
        await AudioService.PlayPreviousTrack(Context.Guild, Context.User);
        await Context.Interaction.DeferAsync();
    }
    [ComponentInteraction("repeat")]
    public async Task Repeat()
    {
        await AudioService.SetRepeatAsync(Context.Guild);
        await Context.Interaction.DeferAsync();
    }

    [ComponentInteraction("clearfilters")]
    public async Task ClearFilters()
    {
        await AudioService.ClearFiltersAsync(Context.Guild);
        await Context.Interaction.DeferAsync();
    }
}