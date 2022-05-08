using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace KBot.Modules.Reviews;

[DefaultMemberPermissions(GuildPermission.SendMessages)]
public class ReviewCommands : SlashModuleBase
{
    [SlashCommand("review", "Review something")]
    public async Task ReviewAsync()
    {
        var config = await Mongo.GetGuildConfigAsync(Context.Guild).ConfigureAwait(false);
        if (config.ReviewChannelId == 0)
        {
            var eb = new EmbedBuilder()
                .WithDescription(
                    "**Reviews are disabled on this server! Please ask an admin to enable them!**"
                )
                .WithColor(Color.Red)
                .Build();
            await RespondAsync(embed: eb, ephemeral: true).ConfigureAwait(false);
        }

        var m = new ModalBuilder()
            .WithTitle("Review")
            .WithCustomId($"review:{config.ReviewChannelId}")
            .AddTextInput(
                "What are you reviewing?",
                "review-title",
                required: true,
                placeholder: "Avengers: Endgame"
            )
            .AddTextInput(
                "You can add a url if you want!",
                "review-url",
                required: false,
                placeholder: "https://www.imdb.com/title/tt4154796/"
            )
            .AddTextInput(
                "What is the review?",
                "review-body",
                TextInputStyle.Paragraph,
                "The perfect movie for a sunday night...",
                required: true
            )
            .AddTextInput(
                "How many stars would you give it out of 5?",
                "review-stars",
                TextInputStyle.Short,
                "5",
                minLength: 1,
                maxLength: 1,
                required: true)
            .Build();

        await RespondWithModalAsync(m).ConfigureAwait(false);
    }

    [ModalInteraction("review:*")]
    public async Task HandleReviewModal(ulong channelId, ReviewModal modal)
    {
        var result = int.TryParse(modal.ReviewStars, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stars);
        if (!result)
        {
            await RespondAsync("You only use numbers between 1 and 5 for a star rating!").ConfigureAwait(false);
            return;
        }
        var channel = Context.Guild.GetTextChannel(channelId);

        var eb = new EmbedBuilder()
            .WithAuthor(
                $"{Context.User.Username}#{Context.User.Discriminator}",
                Context.User.GetAvatarUrl()
            )
            .WithTitle(modal.ReviewTitle)
            .WithUrl(modal.ReviewUrl)
            .WithDescription(modal.ReviewBody)
            .AddField($"Star Rating ({stars}/5)", string.Concat(Enumerable.Repeat(":star:", stars)))
            .Build();
        await channel.SendMessageAsync(embed: eb).ConfigureAwait(false);
        var eb2 = new EmbedBuilder()
            .WithDescription("**Your review has been submitted!**")
            .WithColor(Color.Green)
            .Build();
        await RespondAsync(embed: eb2, ephemeral: true).ConfigureAwait(false);
    }

    public class ReviewModal : IModal
    {
        public string Title => "Review";

        [ModalTextInput("review-title")]
        public string ReviewTitle { get; set; }

        [ModalTextInput("review-url")]
        public string ReviewUrl { get; set; }

        [ModalTextInput("review-body")]
        public string ReviewBody { get; set; }

        [ModalTextInput("review-stars")]
        public string ReviewStars { get; set; }
    }
}
