using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FixerBot.Dialogs
{
    public class CreatePostingDialog : CancelAndHelpDialog
    {
        public CreatePostingDialog()
            : base(nameof(CreatePostingDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                GetPostingTypeAsync,
                GetUserNameAsync,
                ConfirmStepAsync,
                ResolveConfirmAsync,
                CreatePostingAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> GetPostingTypeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var postinDetails = (PostingDetails)stepContext.Options;

            if (postinDetails.Posting == Posting.None)
            {
                var problemMessageTest = "TODO: Get User Input here. I assume you need to get materials";
                var problemMessage = MessageFactory.Text(problemMessageTest, problemMessageTest, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(problemMessage, cancellationToken);

                postinDetails.Posting = Posting.GetMaterials;
            }

            return await stepContext.NextAsync(postinDetails.Material, cancellationToken);
        }

        private async Task<DialogTurnResult> GetUserNameAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var postinDetails = (PostingDetails)stepContext.Options;

            if (string.IsNullOrEmpty(postinDetails.User))
            {
                var problemMessageTest = "TODO: Get User Input here. I assume your name is TestUser";
                var problemMessage = MessageFactory.Text(problemMessageTest, problemMessageTest, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(problemMessage, cancellationToken);

                postinDetails.User = "TestUser";
            }

            return await stepContext.NextAsync(postinDetails.User, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var postinDetails = (PostingDetails)stepContext.Options;

            var postType = postinDetails.Posting == Posting.GetMaterials ? "get materials for fixing" : "find someone to fix ";

            var messageText = $"Please confirm, I have you ({postinDetails.User}) wanting to {postType} {postinDetails.Problem} on/in your {postinDetails.Item}.";

            if (postinDetails.Posting == Posting.GetMaterials)
            {
                messageText += $" The material you require is: {postinDetails.Material}.";
            }

            messageText += " Is this correct?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }
        
        private async Task<DialogTurnResult> ResolveConfirmAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var confirm = (bool)stepContext.Result;

            if (confirm)
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                var notConfirmed = "Sorry, something is not right... Let's go again.";
                var notConfirmedMessage = MessageFactory.Text(notConfirmed, notConfirmed, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(notConfirmedMessage, cancellationToken);
                return await stepContext.ReplaceDialogAsync(InitialDialogId, new FixDetails(), cancellationToken);
            }
        }

        private async Task<DialogTurnResult> CreatePostingAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // to do link to new post, create post
            var url = "https://fixerupper.com/post/17";

            var createPost = $"Fantastic. We've created a post for you at: {url}.";
            var createPostMessage = MessageFactory.Text(createPost, createPost, InputHints.IgnoringInput);
            await stepContext.Context.SendActivityAsync(createPostMessage, cancellationToken);

            return await stepContext.NextAsync(null, cancellationToken);
        }
    }
}
