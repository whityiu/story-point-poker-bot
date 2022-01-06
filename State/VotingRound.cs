using System.Collections.Generic;

namespace StoryPointPoker.State
{
    public class VotingRound
    {
        public string StoryId { get; set; }
        public int VoterCount { get; set; }
        public List<UserVote> StoryPoints { get; set; }
    }
}