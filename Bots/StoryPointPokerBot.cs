// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

using StoryPointPoker.State;

namespace Microsoft.BotBuilderSamples
{
    // This bot will respond to the user's input with suggested actions.
    // Suggested actions enable your bot to present buttons that the user
    // can tap to provide input. 
    public class StoryPointPokerBot : ActivityHandler
    {
        private readonly ILogger<StoryPointPokerBot> logger;
        private BotState conversationState;
        public const string WelcomeText = "Please vote when prompted.";

        public StoryPointPokerBot(ConversationState conversationState, ILogger<StoryPointPokerBot> logger)
        {
            this.conversationState = conversationState;
            this.logger = logger;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            await this.conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            // Send a welcome message to the user and tell them what actions they may perform to use this bot
            await SendWelcomeMessageAsync(turnContext, cancellationToken);
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // 1. Setup/Access Bot State
            var conversationStateAccessors = this.conversationState.CreateProperty<VotingRound>(nameof(VotingRound));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new VotingRound());

            // 2. Process message
            await HandleMessageActivity(conversationData, turnContext, cancellationToken);

            // 3. Save Bot State
            await this.conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }
        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(
                        $"Welcome to the Story Point Poker bot {member.Name}. {WelcomeText}",
                        cancellationToken: cancellationToken);
                    //await SendStoryPointActionsAsync(turnContext, cancellationToken);
                }
            }
        }

        private static async Task HandleMessageActivity(VotingRound votingRound, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            string command = String.Empty;
            List<string> parameters = new List<string>();

            string activityCommand = turnContext.Activity.Text.ToLowerInvariant().Trim();
            if (!String.IsNullOrEmpty(activityCommand))
            {
                var commandParts = activityCommand.Split(' ');
                command = commandParts[0];
                parameters = commandParts.Skip(1).ToList();
            }

            switch (command)
            {
                case "vote":
                    await HandleStartVoteAsync(parameters, votingRound, turnContext, cancellationToken);
                    break;
                case "user-vote":
                    await HandleUserVoteReceivedAsync(parameters, votingRound, turnContext, cancellationToken);
                    break;
                case "end-vote":
                    await HandleEndVoteAsync(votingRound, turnContext, cancellationToken);
                    break;
                default:
                    await turnContext.SendActivityAsync($"Unknown command: {activityCommand}", cancellationToken: cancellationToken);
                    break;
            }
        }

        private static async Task HandleStartVoteAsync(List<string> parameters, VotingRound votingRound, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Set Story Id
            votingRound.StoryId = parameters.FirstOrDefault();

            // Clear received votes.
            if (votingRound.StoryPoints == null) {
                votingRound.StoryPoints = new List<UserVote>();
            }
            votingRound.StoryPoints.Clear();

            var message = "Starting vote";
            if (!String.IsNullOrEmpty(votingRound.StoryId))
            {
                message += $" for: {votingRound.StoryId}";
            }
            else
            {
                message += ":";
            }

            // Send message that voting has started.
            await turnContext.SendActivityAsync(
                        $"Starting vote for: {votingRound.StoryId}",
                        cancellationToken: cancellationToken);

            await SendStoryPointActionsAsync(turnContext, cancellationToken);
        }

        private static async Task HandleUserVoteReceivedAsync(List<string> parameters, VotingRound votingRound, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var userVote = RecordVote(parameters, votingRound, turnContext.Activity);
            if (userVote == null) {
                await SendStoryPointActionsAsync(turnContext, cancellationToken);
                return;
            }

            await turnContext.SendActivityAsync(
                        $"Vote recorded: {userVote.UserName}",
                        cancellationToken: cancellationToken);
        }

        private static UserVote RecordVote(List<string> parameters, VotingRound conversationData, IMessageActivity activity)
        {
            var selectedSize = -1;
            if (Int32.TryParse(parameters.FirstOrDefault(), out selectedSize))
            {
                // Record the vote.
                if (conversationData.StoryPoints == null)
                {
                    conversationData.StoryPoints = new List<UserVote>();
                }

                UserVote userVote = new UserVote();
                userVote.UserId = activity.From.Id;
                userVote.UserName = activity.From.Name;
                userVote.StoryPoints = selectedSize;

                conversationData.StoryPoints.Add(userVote);
                return userVote;
            }
            else
            {
                return null;
            }
        }

        private static async Task HandleEndVoteAsync(VotingRound conversationData, ITurnContext turnContext, CancellationToken cancellationToken) 
        {
            var messageBuilder = new StringBuilder();

            if (!String.IsNullOrEmpty(conversationData.StoryId)) {
                messageBuilder.AppendLine($"Voting results for {conversationData.StoryId}:");
            } else {
                messageBuilder.AppendLine("Voting results:");
            }

            foreach (var vote in conversationData.StoryPoints) {
                messageBuilder.AppendLine($"{vote.UserName} voted: {vote.StoryPoints}");
            }

            await turnContext.SendActivityAsync(messageBuilder.ToString(), cancellationToken: cancellationToken);
        }

        /// Creates and sends an activity with suggested actions to the user. When the user
        /// clicks one of the buttons the text value from the "CardAction" will be
        /// displayed in the channel just as if the user entered the text. There are multiple
        /// "ActionTypes" that may be used for different situations.
        private static async Task SendStoryPointActionsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = MessageFactory.SuggestedActions(
                new List<CardAction>()
                {
                    new CardAction() { Type = ActionTypes.MessageBack, Title = "1", Text = "user-vote 1" },
                    new CardAction() { Type = ActionTypes.MessageBack, Title = "2", Text = "user-vote 2" },
                    new CardAction() { Type = ActionTypes.MessageBack, Title = "3", Text = "user-vote 3" },
                    new CardAction() { Type = ActionTypes.MessageBack, Title = "5", Text = "user-vote 5" },
                    new CardAction() { Type = ActionTypes.MessageBack, Title = "8", Text = "user-vote 8" },
                    new CardAction() { Type = ActionTypes.MessageBack, Title = "13", Text = "user-vote 13" },
                }
            );

            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}
