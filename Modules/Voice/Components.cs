using System;
using System.Linq;
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
        var number = selections.Select(int.Parse).ToList();
        var sum = number.Sum();
        switch (sum)
        {
            /*
             * bassboost = 1
             * nightcore = 2
             * 8d = 8
             * vaporwave = 4
             * 
             */
            
            case 1: //bassboost
            {
                await RespondAsync(embed: await AudioService.SetFiltersAsync(Context.Guild, Context.User, 
                    Array.Empty<IFilter>(), 
                    FilterHelper.BassBoost(),
                    new [] {"Basszus Erősítés"}), ephemeral: true);
                break;
            }
            case 2: //Nightcore
            {

                await RespondAsync(embed: await AudioService.SetFiltersAsync(Context.Guild, Context.User, 
                    new IFilter[] {FilterHelper.NightCore()}, 
                    Array.Empty<EqualizerBand>(),
                    new [] {"Nightcore"}), ephemeral: true);
                break;
            }
            case 8: //8d
            {
                await RespondAsync(embed: await AudioService.SetFiltersAsync(Context.Guild, Context.User, 
                    new IFilter[] {FilterHelper.EightD()}, 
                    Array.Empty<EqualizerBand>(),
                    new [] {"8D"}), ephemeral: true);
                break;
            }
            case 4: //vaporwave
            {
                await RespondAsync(embed: await AudioService.SetFiltersAsync(Context.Guild, Context.User, 
                    new IFilter[] {FilterHelper.VaporWave()}, 
                    Array.Empty<EqualizerBand>(),
                    new [] {"Vaporwave"}), ephemeral: true);
                break;
            }
            case 3: //bassboost + nightcore
            {
                await RespondAsync(embed: await AudioService.SetFiltersAsync(Context.Guild, Context.User, 
                    new IFilter[] {FilterHelper.NightCore()}, 
                    FilterHelper.BassBoost(),
                    new [] {"Basszus Erősítés", "Nightcore"}), ephemeral: true);
                break;
            }
            case 9: //bassboost + 8d
            {
                await RespondAsync(embed: await AudioService.SetFiltersAsync(Context.Guild, Context.User, 
                    new IFilter[] {FilterHelper.EightD()}, 
                    FilterHelper.BassBoost(),
                    new [] {"Basszus Erősítés", "8D"}), ephemeral: true);
                break;
            }
            case 5: //bassboost + vaporwave
            {
                await RespondAsync(embed: await AudioService.SetFiltersAsync(Context.Guild, Context.User, 
                    new IFilter[] {FilterHelper.VaporWave()}, 
                    FilterHelper.BassBoost(),
                    new [] {"Basszus Erősítés", "Vaporwave"}), ephemeral: true);
                break;
            }
            case 10: //nightcore + 8d
            {
                await RespondAsync(embed: await AudioService.SetFiltersAsync(Context.Guild, Context.User, 
                    new IFilter[] {FilterHelper.EightD(), FilterHelper.NightCore()},
                    Array.Empty<EqualizerBand>(),
                    new [] {"Nightcore", "8D"}), ephemeral: true);
                break;
            }
            case 6: //nightcore + vaporwave
            {
                await RespondAsync(embed: await AudioService.SetFiltersAsync(Context.Guild, Context.User, 
                    new IFilter[] {FilterHelper.VaporWave(), FilterHelper.NightCore()},
                    Array.Empty<EqualizerBand>(),
                    new [] {"Nightcore", "Vaporwave"}), ephemeral: true);
                break;
            }
            case 12: //8d + vaporwave
            {
                await RespondAsync(embed: await AudioService.SetFiltersAsync(Context.Guild, Context.User, 
                    new IFilter[] {FilterHelper.EightD(), FilterHelper.VaporWave()},
                    Array.Empty<EqualizerBand>(),
                    new [] {"8D", "Vaporwave"}), ephemeral: true);
                break;
            }
            case 7: //bassboost + nightcore + vaporwave
            {
                await RespondAsync(embed: await AudioService.SetFiltersAsync(Context.Guild, Context.User, 
                    new IFilter[] {FilterHelper.NightCore(), FilterHelper.VaporWave()},
                    FilterHelper.BassBoost(),
                    new [] {"Basszus Erősítés", "Nightcore", "Vaporwave"}), ephemeral: true);
                break;
            }
            case 14: //8d + nightcore + vaporwave
            {
                await RespondAsync(embed: await AudioService.SetFiltersAsync(Context.Guild, Context.User, 
                    new IFilter[] {FilterHelper.EightD(), FilterHelper.NightCore(), FilterHelper.VaporWave()},
                    Array.Empty<EqualizerBand>(),
                    new [] {"8D", "Nightcore", "Vaporwave"}), ephemeral: true);
                break;
            }
            case 13: //bassboost + 8d + vaporwave
            {
                await RespondAsync(embed: await AudioService.SetFiltersAsync(Context.Guild, Context.User, 
                    new IFilter[] {FilterHelper.EightD(), FilterHelper.VaporWave()},
                    FilterHelper.BassBoost(),
                    new [] {"Basszus Erősítés", "8D", "Vaporwave"}), ephemeral: true);
                break;
            }
            case 15: //bassboost + nightcore + 8d + vaporwave
            {
                await RespondAsync(embed: await AudioService.SetFiltersAsync(Context.Guild, Context.User, 
                    new IFilter[] {FilterHelper.EightD(), FilterHelper.NightCore(), FilterHelper.VaporWave()},
                    FilterHelper.BassBoost(),
                    new [] {"Basszus Erősítés", "8D", "Nightcore", "Vaporwave"}), ephemeral: true);
                break;
            }
        }
    }
    
    [ComponentInteraction("stop")]
    public async Task Stop()
    {
        await AudioService.StopAsync(Context.Guild);
        await AudioService.LeaveAsync(Context.Guild, Context.User);
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
        await AudioService.ClearFiltersAsync(Context.Guild, Context.User);
        await Context.Interaction.DeferAsync();
    }
}