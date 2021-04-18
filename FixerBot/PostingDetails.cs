using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FixerBot
{
    public class PostingDetails
    {
        public Posting Posting { get; set; }

        public string LookingFor { get; set; }

        public string User { get; set; }
        public string Item { get; internal set; }
        public string Problem { get; internal set; }
        public string Material { get; internal set; }
    }

    public enum Posting
    {
        None,
        GetPerson,
        GetMaterials
    }
}
