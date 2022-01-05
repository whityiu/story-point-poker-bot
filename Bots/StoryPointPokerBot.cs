// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

using StoryPointPoker.State;

namespace Microsoft.BotBuilderSamples
{
    // This bot will respond to the user's input with suggested actions.
    // Suggested actions enable your bot to present buttons that the user
    // can tap to provide input. 
    public class StoryPointPokerBot : ActivityHandler
    {
        private BotState conversationState;
        public const string WelcomeText = "Please vote when prompted.";

        public StoryPointPokerBot(ConversationState conversationState) {
            this.conversationState = conversationState;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken)) {
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
            var conversationStateAccessors = this.conversationState.CreateProperty<VotingRound>(nameof(VotingRound));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new VotingRound());

            // Extract the text from the message activity the user sent.
            var text = turnContext.Activity.Text.ToLowerInvariant();

            // Take the input from the user and create the appropriate response.
            var responseText = ProcessSizeSelection(conversationData, text);

            // Respond to the user.
            await turnContext.SendActivityAsync(responseText, cancellationToken: cancellationToken);

            string voteSummary = "Vote summary: " + string.Join(",", conversationData.StoryPoints);
            await turnContext.SendActivityAsync(voteSummary, cancellationToken: cancellationToken);

            await SendSuggestedActionsAsync(turnContext, cancellationToken);
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
                    await SendSuggestedActionsAsync(turnContext, cancellationToken);
                }
            }
        }

        private static async Task ProcessInput(ITurnContext turnContext) 
        {
            string command = String.Empty;
            List<string> parameters = new List<string>();

            string activityCommand = turnContext.Activity.Text.ToLowerInvariant().Trim();
            if (!String.IsNullOrEmpty(activityCommand)) {
                var commandParts = activityCommand.Split(' ');
                command = commandParts[0];
                parameters = commandParts.Skip(1).ToList();
            }

            switch (activityCommand) {
                case "vote":
                    HandleStartVote();
                    break;
                default:
                    break;
            }
        }

        private void HandleStartVote() {

        }

        private string ProcessSizeSelection(VotingRound conversationData, string text)
        {
            var selectedSize = -1;
            if (Int32.TryParse(text, out selectedSize)) {
                // Record the vote.
                if (conversationData.StoryPoints == null) {
                    conversationData.StoryPoints = new List<int>();
                }
                
                conversationData.StoryPoints.Add(selectedSize);

                return $"Vote recorded: {selectedSize}.";
            } else {
                return "Please select a story size.";
            }
        }

        /// Creates and sends an activity with suggested actions to the user. When the user
        /// clicks one of the buttons the text value from the "CardAction" will be
        /// displayed in the channel just as if the user entered the text. There are multiple
        /// "ActionTypes" that may be used for different situations.
        private static async Task SendSuggestedActionsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = MessageFactory.Text("Please select on a story size:");

            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction() { Type = ActionTypes.ImBack, Value = "1" },
                    new CardAction() { Type = ActionTypes.ImBack, Value = "2" },
                    new CardAction() { Type = ActionTypes.ImBack, Value = "3" },
                    new CardAction() { Type = ActionTypes.ImBack, Value = "5" },
                    new CardAction() { Type = ActionTypes.ImBack, Value = "8" },
                    new CardAction() { Type = ActionTypes.ImBack, Value = "13" },
                },
            };
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}
