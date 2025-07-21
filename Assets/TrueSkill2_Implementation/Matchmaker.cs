using System;
using System.Collections.Generic;
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

        public (List<Player> team1, List<Player> team2)? FindBestMatch(
            List<Player> playerPool,
            int teamSize = 5,
            int maxAttempts = 1000)
        {
            // 1. Filter players by availability, etc. (expand as needed)
            var availablePlayers = playerPool.Where(p => p.playerState == Player.PlayerState.Idle).ToList();

            if (availablePlayers.Count < teamSize * 2)
                return null;

            // 2. Sort by conservative rating for efficient searching
            var sortedPlayers = availablePlayers
                .OrderBy(p => p.playerData.MyTrueSkillRating.ConservativeRating)
                .ToList();

            // 3. Find best match using TrueSkill's quality metric
            double bestQuality = minMatchQuality;
            (List<Player>, List<Player>)? bestMatch = null;

            for (int i = 0; i < maxAttempts; i++)
            {
                // Random sampling works better than brute force for large pools
                var candidates = RandomSample(sortedPlayers, teamSize * 2);
                var possibleTeams = GenerateTeamCombinations(candidates, teamSize);

                foreach (var (team1, team2) in possibleTeams)
                {
                    double quality = CalculateMatchQuality(team1, team2);
                    if (quality > bestQuality)
                    {
                        bestQuality = quality;
                        bestMatch = (team1, team2);

                        // Early exit if we find an excellent match
                        if (quality > 0.9) return bestMatch;
                    }
                }
            }

            return bestMatch;
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