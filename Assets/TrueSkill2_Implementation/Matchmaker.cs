using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TrueSkill2
{
    public class Matchmaker
    {
        private readonly TrueSkillCalculator calculator;
        private readonly TrueSkill2Env env;
        private readonly double minMatchQuality;

        public Matchmaker(TrueSkill2Env environment, double minMatchQuality = 0.5)
        {
            this.env = environment;
            this.calculator = new TrueSkillCalculator(environment);
            this.minMatchQuality = minMatchQuality;
        }

        public void FindBestMatch(
            List<Player> playerPool,
            ref List<Player> team1,
            ref List<Player> team2,
            int teamSize = 5,
            int maxAttempts = 1000)
        {
            //Filter players by availability, etc. (expand as needed)
            var availablePlayers = playerPool.Where(p => p.playerState == Player.PlayerState.Idle).ToList();

            if (availablePlayers.Count < teamSize * 2)
                return;

            for (int i = 0; i < maxAttempts; i++)
            {
                // Random sampling works better than brute force for large pools
                var candidates = RandomSample(availablePlayers, teamSize * 2);
                var possibleTeams = GenerateTeamCombinations(candidates, teamSize);

                foreach (var (possibleTeam1, possibleTeam2) in possibleTeams)
                {
                    double quality = CalculateMatchQuality(possibleTeam1, possibleTeam2);
                    CustomTrueskillSystemManager.instance.PrintSomething(quality.ToString());
                    if (quality > minMatchQuality)
                    {
                        team1 = possibleTeam1;
                        team2 = possibleTeam2;
                    }
                }

                return;
            }

            return;
        }

        // TrueSkill match quality calculation (0 = terrible, 1 = perfect)
        public double CalculateMatchQuality(List<Player> team1, List<Player> team2)
        {
            // Sum team statistics (with partial play)
            double mu1 = team1.Sum(p => p.playerData.MyTrueSkillRating.Mean);
            double mu2 = team2.Sum(p => p.playerData.MyTrueSkillRating.Mean);

            double sigma1Sq = team1.Sum(p => p.playerData.MyTrueSkillRating.Variance);
            double sigma2Sq = team2.Sum(p => p.playerData.MyTrueSkillRating.Variance);

            double twoBetaSq = 2 * env.Beta * env.Beta;
            double denominator = twoBetaSq + sigma1Sq + sigma2Sq;

            // First term: Skill uncertainty scaling
            double term1 = Math.Sqrt(twoBetaSq / denominator);

            // Second term: Skill difference penalty
            double muDiff = mu1 - mu2;
            double term2 = Math.Exp(-(muDiff * muDiff) / (2 * denominator));

            return term1 * term2;
        }

        private List<Player> RandomSample(List<Player> players, int count)
        {
            var rnd = new Random();
            return players.OrderBy(x => rnd.Next()).Take(count).ToList();
        }

        private IEnumerable<(List<Player>, List<Player>)> GenerateTeamCombinations(
            List<Player> players,
            int teamSize)
        {
            // Shuffle before splitting
            var rnd = new Random();
            var shuffled = players.OrderBy(_ => rnd.Next()).ToList();

            var team1 = shuffled.Take(teamSize).ToList();
            var team2 = shuffled.Skip(teamSize).Take(teamSize).ToList();

            yield return (team1, team2);
        }
    }
}