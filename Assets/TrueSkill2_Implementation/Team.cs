using System.Collections.Generic;
using System.Linq;

namespace TrueSkill2
{
    public class Team : List<Player>
    {
        public int Rank { get; set; }

        public Team(int rank, IEnumerable<Player> players) : base(players)
        {
            Rank = rank;
        }

        public double TotalMean => this.Sum(p => p.playerData.MyTrueSkillRating.Mean);
        public double TotalVariance => this.Sum(p => p.playerData.MyTrueSkillRating.Variance);
    }
}