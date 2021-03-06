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
    public class GetMaterialsDialog : CancelAndHelpDialog
    {
        public List<string> GetMaterials = new List<string>()
        {
            "From a Shop",
            "Community"
        };

        public GetMaterialsDialog(CreatePostingDialog createPostingDialog)
            : base(nameof(GetMaterialsDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                WhatMaterialsAsync,
                ResolveMaterialsAsync
            }));
            AddDialog(new WaterfallDialog("WaterFall2", new WaterfallStep[]
            {
                WhereGetMaterialsAsync,
                ResolveGetMaterialsAsync
            }));
            AddDialog(createPostingDialog);
            AddDialog(new ChoicePrompt("ChoicesChoices"));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ResolveGetMaterialsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var matDeets = (MaterialDetails)stepContext.Options;
            var getMatsFrom = ((FoundChoice)stepContext.Result).Value;

            switch (getMatsFrom)
            {
                case "From a Shop":
                    // adaptive card with where to get materials
                    var resourceCard = CreateMaterialsAttachment(matDeets);
                    var attachment = MessageFactory.Attachment(resourceCard, ssml: "Here are some resources");
                    var resourceResponse = await stepContext.Context.SendActivityAsync(attachment, cancellationToken);
                    break;
                case "Community":
                    var postingDetails = new PostingDetails
                    {
                        Posting = Posting.GetMaterials,
                        Item = matDeets.Item,
                        Problem = matDeets.Problem,
                        Material = matDeets.Material ?? "orange thread"
                    };
                    return await stepContext.BeginDialogAsync(nameof(CreatePostingDialog), postingDetails, cancellationToken);
                default:
                    var defaultText = $"TODO: another flow here for {getMatsFrom}";
                    var defaultMessage = MessageFactory.Text(defaultText, defaultText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(defaultMessage, cancellationToken);
                    break;
            }


            return await stepContext.NextAsync(true, cancellationToken);
        }

        private async Task<DialogTurnResult> WhereGetMaterialsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var matDeets = (MaterialDetails)stepContext.Options;

            var whereMaterials = $"Where do you want to get the {matDeets.Material} from?";
            var promptMessage = MessageFactory.Text(whereMaterials, whereMaterials, InputHints.ExpectingInput);

            var choicePrompt = new List<Choice>();
            foreach (var val in GetMaterials)
            {
                choicePrompt.Add(new Choice(val));
            }

            var prompOptions = new PromptOptions
            {
                Prompt = promptMessage,
                Choices = choicePrompt,
                
            };

            return await stepContext.PromptAsync("ChoicesChoices", prompOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> ResolveMaterialsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var matDeets = (MaterialDetails)stepContext.Options;
            matDeets.Material = (string)stepContext.Result;

            return await stepContext.ReplaceDialogAsync("WaterFall2", matDeets, cancellationToken);
        }

        private async Task<DialogTurnResult> WhatMaterialsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var matDeets = (MaterialDetails)stepContext.Options;

            if (string.IsNullOrWhiteSpace(matDeets.Material))
            {
                var materialsMessage = "What materials do you need?";
                var promptMessage = MessageFactory.Text(materialsMessage, materialsMessage, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(matDeets.Material, cancellationToken);
        }

        // Load attachment from embedded resource.
        private Attachment CreateMaterialsAttachment(MaterialDetails matDeets)
        {
            List<string> jumperList = new List<string>()
            {
                "jumper", "sweater", "hoodie", "oodie", "jersey"
            };

            //To Do: Add logic for different fixes... call IFitIt api?
            var cardResourcePath = "FixerBot.Cards.welcomeCard.json";
            if (jumperList.Contains(matDeets.Item.Split(" ").Last().ToLower()))
            {
                cardResourcePath = "FixerBot.Cards.buyThreadCard.json";
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
