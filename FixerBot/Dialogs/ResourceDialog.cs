using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FixerBot.Dialogs
{

    public class ResourceDialog : CancelAndHelpDialog
    {
        public ResourceDialog(GetMaterialsDialog getMaterialsDialog)
            : base(nameof(ResourceDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                SupplyResourceAsync,
                PostResourcesChoiceAsync,
                ResolveMendingChoiceAsync,
                FeedbackStepAsync
            }));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(getMaterialsDialog);

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> FeedbackStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var fixDetails = (FixDetails)stepContext.Options;

            var messageText = $"How did you get on fixing your '{fixDetails.Item}'? Were the suggestions useful?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        public List<string> NextSteps = new List<string>()
        {
            "I Need Materials",
            "I Can't Do This",
            "Mending Complete"
        };

        private async Task<DialogTurnResult> ResolveMendingChoiceAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = ((FoundChoice)stepContext.Result).Value;
            var fixDetails = (FixDetails)stepContext.Options;

            switch (result)
            {
                case "I Can't Do This":
                    var postingDetails = new PostingDetails
                    {
                        Posting = Posting.GetPerson,
                        Item = fixDetails.Item,
                        Problem = fixDetails.Problem
                    };
                    return await stepContext.BeginDialogAsync(nameof(CreatePostingDialog), postingDetails, cancellationToken);
                case "I Need Materials":
                    return await stepContext.BeginDialogAsync(nameof(GetMaterialsDialog), fixDetails, cancellationToken);
                case "Mending Complete":
                    // TO DO: Give Feedback and level up!
                    return await stepContext.NextAsync(null, cancellationToken);
                default:
                    return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> PostResourcesChoiceAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var item = ((FixDetails)stepContext.Options).Item;
            var messageText = $"How do you feel about fixing your {item}?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            var choicePrompt = new List<Choice>();
            foreach (string val in NextSteps)
            {
                choicePrompt.Add(new Choice(val));
            }

            var prompOptions = new PromptOptions
            {
                Prompt = promptMessage,
                Choices = choicePrompt
            };

            return await stepContext.PromptAsync(nameof(ChoicePrompt), prompOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> SupplyResourceAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var fixDetails = (FixDetails)stepContext.Options;
            var resourceCard = CreateFixDetailsAttachment(fixDetails);

            var attachment = MessageFactory.Attachment(resourceCard, ssml: "Here are some resources");

            var resourceResponse = await stepContext.Context.SendActivityAsync(attachment, cancellationToken);

            return await stepContext.NextAsync(null, cancellationToken);
        }

        // Load attachment from embedded resource.
        private Attachment CreateFixDetailsAttachment(FixDetails fixDetails)
        {
            List<string> jumperList = new List<string>()
            {
                "jumper", "sweater", "hoodie", "oodie", "jersey"
            };

            //To Do: Add logic for different fixes... call IFitIt api?
            var cardResourcePath = "FixerBot.Cards.welcomeCard.json";
            if (jumperList.Contains(fixDetails.Item.Split(" ").Last().ToLower()))
            {
                cardResourcePath = "FixerBot.Cards.fixJumperCard.json";
            }


            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }
    }
}
