// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Microsoft.BotBuilderSamples
{
    // This bot will respond to the user's input with suggested actions.
    // Suggested actions enable your bot to present buttons that the user
    // can tap to provide input. 
    public class StoryPointPokerBot : ActivityHandler
    {
        public const string WelcomeText = "Please vote when prompted.";

      
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            // Send a welcome message to the user and tell them what actions they may perform to use this bot
            await SendWelcomeMessageAsync(turnContext, cancellationToken);
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {

            // Extract the text from the message activity the user sent.
            var text = turnContext.Activity.Text.ToLowerInvariant();

            // Take the input from the user and create the appropriate response.
            var responseText = ProcessSizeSelection(text);

            // Respond to the user.
            await turnContext.SendActivityAsync(responseText, cancellationToken: cancellationToken);

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

        private static string ProcessSizeSelection(string text)
        {
            const string colorText = "is the best color, I agree.";
            
            var selectedSize = -1;
            if (Int32.TryParse(text, out selectedSize)) {
                // Record the vote.
                return $"Vote recorded: {selectedSize}";
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
                    new CardAction() { Title = "One", Type = ActionTypes.ImBack, Value = "1", Image = "https://via.placeholder.com/20/2A2A2A?text=1", ImageAltText = "R" },
                    new CardAction() { Title = "Two", Type = ActionTypes.ImBack, Value = "2", Image = "https://via.placeholder.com/20/2A2A2A?text=2", ImageAltText = "Y" },
                    new CardAction() { Title = "Three", Type = ActionTypes.ImBack, Value = "3", Image = "https://via.placeholder.com/20/2A2A2A?text=3", ImageAltText = "B"   },
                    new CardAction() { Title = "Five", Type = ActionTypes.ImBack, Value = "5", Image = "https://via.placeholder.com/20/2A2A2A?text=5", ImageAltText = "B"   },
                    new CardAction() { Title = "Eight", Type = ActionTypes.ImBack, Value = "8", Image = "https://via.placeholder.com/20/2A2A2A?text=8", ImageAltText = "B"   },
                    new CardAction() { Title = "Thirteen", Type = ActionTypes.ImBack, Value = "13", Image = "https://via.placeholder.com/20/2A2A2A?text=13", ImageAltText = "B"   },
                },
            };
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}
