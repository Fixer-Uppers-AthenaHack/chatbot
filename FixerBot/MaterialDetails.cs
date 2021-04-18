using FixerBot.Dialogs;

namespace FixerBot
{
    public class MaterialDetails
    {
        public MaterialDetails() { }
        public MaterialDetails(FixDetails fixDetails)
        {
            Item = fixDetails.Item;
            Problem = fixDetails.Problem;
            Fixer = fixDetails.Fixer;
        }

        public string Item { get; set; }

        public string Problem { get; set; }

        public string Fixer { get; internal set; }

        public string Material { get; internal set; }
    }
}
