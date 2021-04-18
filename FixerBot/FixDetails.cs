using FixerBot.Dialogs;

namespace FixerBot
{
    public class FixDetails
    {
        public string ConfidenceLevel { get; set; }

        public string Item { get; set; }

        public string Problem { get; set; }

        public string Fixer { get; internal set; }

        public bool Correct { get; set; }
    }
}
