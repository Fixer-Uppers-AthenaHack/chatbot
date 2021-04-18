using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace FixerBot.Dialogs
{
    public class FixerDialog : CancelAndHelpDialog
    {
        public List<string> WhoFixes = new List<string>()
        {
            "Fix It Myself",
            "Find an Expert",
            "Further Diagnosis"
        };

        public FixerDialog(FixItYourselfDialog fixItYourselfDialog, ResourceDialog resourceDialog, CreatePostingDialog createPostingDialog)
            : base(nameof(FixerDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ItemStepAsync,
                ProblemStepAsync,
                WhoToFixStepAsync,
                ResolveWhoFixesAsync,
                RetryOrProgressAsync
            }));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(fixItYourselfDialog);
            AddDialog(resourceDialog);
            AddDialog(createPostingDialog);

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ResolveWhoFixesAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var fixer = ((FoundChoice)stepContext.Result).Value;

            var fixDetails = (FixDetails)stepContext.Options;
            fixDetails.Fixer = fixer;

            switch (fixer)
            {
                case "Fix It Myself":
                    // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(FixItYourselfDialog), fixDetails, cancellationToken);
                case "Find an Expert":
                    var postDeets = new PostingDetails()
                    {
                        Posting = Posting.GetPerson,
                        Item = fixDetails.Item,
                        Problem = fixDetails.Problem
                    };

                    return await stepContext.BeginDialogAsync(nameof(CreatePostingDialog), postDeets, cancellationToken);
                default:
                    var defaultText = $"TODO: another flow here for {fixer}";
                    var defaultMessage = MessageFactory.Text(defaultText, defaultText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(defaultMessage, cancellationToken);
                    break;
            }


            return await stepContext.NextAsync(fixDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> WhoToFixStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var fixDetails = (FixDetails)stepContext.Options;

            if (string.IsNullOrWhiteSpace(fixDetails.Fixer))
            {
                var whoFixesText = $"What do you want to do?";
                var promptMessage = MessageFactory.Text(whoFixesText, whoFixesText, InputHints.ExpectingInput);

                var choicePrompt = new List<Choice>();
                foreach (var val in WhoFixes)
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

            return await stepContext.NextAsync(fixDetails.Fixer, cancellationToken);
        }

        private async Task<DialogTurnResult> ProblemStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var fixDetails = (FixDetails)stepContext.Options;

            if (string.IsNullOrWhiteSpace(fixDetails.Problem))
            {
                var problemMessageTest = "TODO: Get User Input here. I assume you have a hole under your arm";
                var problemMessage = MessageFactory.Text(problemMessageTest, problemMessageTest, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(problemMessage, cancellationToken);

                fixDetails.Problem = "hole under arm";
            }

            return await stepContext.NextAsync(fixDetails.Problem, cancellationToken);
        }

        private async Task<DialogTurnResult> ItemStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var fixDetails = (FixDetails)stepContext.Options;

            if (string.IsNullOrWhiteSpace(fixDetails.Item))
            {
                var problemMessageTest = "TODO: Get User Input here. I assume you want to fix an orange jumper";
                var problemMessage = MessageFactory.Text(problemMessageTest, problemMessageTest, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(problemMessage, cancellationToken);

                fixDetails.Item = "orange jumper";
            }

            return await stepContext.NextAsync(fixDetails.Item, cancellationToken);
        }

        private async Task<DialogTurnResult> RetryOrProgressAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var fixDetails = (FixDetails)stepContext.Result;

            if(fixDetails == null)
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }

            var confirm = fixDetails.Correct;

            if (confirm)
            {
                var defaultText = $"Sweet! Lets get you some resources... :)";
                var defaultMessage = MessageFactory.Text(defaultText, defaultText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(defaultMessage, cancellationToken);

                return await stepContext.BeginDialogAsync(nameof(ResourceDialog), fixDetails, cancellationToken);
            }
            else
            {
                var notConfirmed = "Sorry, something seems to have gone wrong... Let's go again.";
                var notConfirmedMessage = MessageFactory.Text(notConfirmed, notConfirmed, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(notConfirmedMessage, cancellationToken);
                return await stepContext.ReplaceDialogAsync(InitialDialogId, new FixDetails(), cancellationToken);
            }
        }
    }
}
