using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace FixerBot.Dialogs
{
    public class FixItYourselfDialog : CancelAndHelpDialog
    {
        public List<string> ConfidenceLevel = new List<string>()
        {
            "I can't even thread a needle",
            "Sew-sew",
            "Pretty darn good!",
            "I'm a sewing machine!",
            "I could be on the Great British Sewing Bee."
        };

        public FixItYourselfDialog()
            : base(nameof(FixItYourselfDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ConfidenceStepAsync,
                ConfirmStepAsync,
                RetryOrProgressAsync
            }));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ConfidenceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var messageText = $"How would you describe your experience level with sewing?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            var choicePrompt = new List<Choice>();
            foreach(var val in ConfidenceLevel)
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

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var fixDetails = (FixDetails)stepContext.Options;

            fixDetails.ConfidenceLevel = ((FoundChoice)stepContext.Result).Value;

            var messageText = $"Please confirm, I have you wanting to fix '{fixDetails.Item}' with '{fixDetails.Problem}' yourself. Your confidence level is: {fixDetails.ConfidenceLevel}. Is this correct?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> RetryOrProgressAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var confirm = (bool)stepContext.Result;

            var fixDetails = (FixDetails)stepContext.Options;
            fixDetails.Correct = confirm;
            fixDetails.Fixer = "Fix It Myself";

            return await stepContext.NextAsync(fixDetails, cancellationToken);
        }
    }
}
