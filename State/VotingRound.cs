using System.Collections.Generic;

namespace StoryPointPoker.State
{
    public class VotingRound
    {
        public string StoryId { get; set; }
        public List<int> StoryPoints { get; set; }
    }
}