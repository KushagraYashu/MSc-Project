using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TrueSkill2
{
    public class TrueSkillCalculator
    {
        private readonly TrueSkill2Env environment;

        public TrueSkillCalculator(TrueSkill2Env environment)
        {
            this.environment = environment;
        }

        public void UpdateRatings(IEnumerable<Team> teams)
        {
            var teamList = teams.ToList();
            ValidateTeams(teamList);

            // Apply dynamics (add tau*tau to variance)
            foreach (var player in teamList.SelectMany(t => t))
            {
                player.playerData.MyTrueSkillRating.Update(
                    player.playerData.MyTrueSkillRating.Mean,
                    player.playerData.MyTrueSkillRating.Variance + environment.Tau * environment.Tau
                );
            }

            // Process team rankings
            var teamRatings = teamList.Select(t => new TeamRating(t)).ToList();
            UpdateTeamRatings(teamRatings);

            // Update individual player ratings
            foreach (var team in teamList)
            {
                foreach (var player in team)
                {
                    UpdatePlayerRating(player, teamRatings);
                }
            }
        }

        private void UpdateTeamRatings(List<TeamRating> teamRatings)
        {
            teamRatings.Sort((x, y) => x.Team.Rank.CompareTo(y.Team.Rank));

            for (int i = 0; i < teamRatings.Count; i++)
            {
                TeamRating currentTeam = teamRatings[i];

                for (int j = i + 1; j < teamRatings.Count; j++)
                {
                    TeamRating otherTeam = teamRatings[j];

                    double meanDiff = currentTeam.TotalMean - otherTeam.TotalMean;
                    double stdDev = Math.Sqrt(
                        currentTeam.TotalVariance +
                        otherTeam.TotalVariance +
                        2 * environment.Beta * environment.Beta
                    );

                    if (currentTeam.Team.Rank == otherTeam.Team.Rank) // Draw
                    {
                        double v = V(meanDiff / stdDev, environment.Epsilon / stdDev);
                        double w = W(meanDiff / stdDev, environment.Epsilon / stdDev, v);

                        currentTeam.AddResult(v, w, stdDev);
                        otherTeam.AddResult(-v, w, stdDev);
                    }
                    else // Not a draw
                    {
                        double v = V(meanDiff / stdDev, 0);
                        double w = W(meanDiff / stdDev, 0, v);

                        currentTeam.AddResult(v, w, stdDev);
                        otherTeam.AddResult(-v, w, stdDev);
                    }
                }
            }
        }

        private void UpdatePlayerRating(Player player, List<TeamRating> teamRatings)
        {
            var teamRating = teamRatings.First(tr => tr.Team.Contains(player));

            double multiplier = player.playerData.MyTrueSkillRating.Variance / teamRating.TotalVariance;

            double newMean = player.playerData.MyTrueSkillRating.Mean + multiplier * teamRating.V;
            double newVariance = player.playerData.MyTrueSkillRating.Variance * (1 - multiplier * teamRating.W);

            double oldMean = player.playerData.MyTrueSkillRating.Mean;

            player.playerData.MyTrueSkillRating.Update(newMean, newVariance);

            CustomTrueskillSystemManager.instance.PrintSomething($"Updated Player {player.playerData.Id}: " +
                      $"New Mean: {player.playerData.MyTrueSkillRating.Mean}, " +
                      $"Old Mean: {oldMean}");

            player.scaledMuHistory.Add(player.playerData.MyTrueSkillRating.ScaledMean);
            player.scaledSigmaHistory.Add(player.playerData.MyTrueSkillRating.ScaledStandardDeviation);
            player.scaledConservativeValHistory.Add(player.playerData.MyTrueSkillRating.ConservativeRating);
        }

        private static double V(double x, double epsilon)
        {
            double denom = CumulativeDistribution(x - epsilon);
            if (denom < 2.222758749e-162) return -x + epsilon;
            return GaussianDistribution(x - epsilon) / denom;
        }

        private static double W(double x, double epsilon, double v)
        {
            double denom = CumulativeDistribution(x - epsilon);
            if (denom < 2.222758749e-162)
                return x < 0 ? 1 : 0;
            return v * (v + x - epsilon);
        }

        private static double GaussianDistribution(double x)
        {
            return Math.Exp(-0.5 * x * x) / Math.Sqrt(2 * Math.PI);
        }

        private static double CumulativeDistribution(double x)
        {
            return 0.5 * (1 + Erf(x / Math.Sqrt(2)));
        }

        private static double Erf(double x)
        {
            // Abramowitz and Stegun approximation
            double sign = x < 0 ? -1 : 1;
            x = Math.Abs(x);

            double t = 1.0 / (1.0 + 0.3275911 * x);
            double y = 1.0 - ((((1.061405429 * t - 1.453152027) * t + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * Math.Exp(-x * x);

            return sign * y;
        }

        private void ValidateTeams(List<Team> teams)
        {
            if (teams == null) throw new ArgumentNullException(nameof(teams));
            if (teams.Count < 2) throw new ArgumentException("At least two teams required");
            if (teams.Any(t => t.Count == 0)) throw new ArgumentException("Teams cannot be empty");
            if (teams.SelectMany(t => t).GroupBy(p => p.playerData.Id).Any(g => g.Count() > 1))
                throw new ArgumentException("Duplicate player IDs detected");
        }

        private class TeamRating
        {
            public Team Team { get; }
            public double TotalMean { get; }
            public double TotalVariance { get; }
            public double V { get; private set; }
            public double W { get; private set; }

            public TeamRating(Team team)
            {
                Team = team;
                TotalMean = team.TotalMean;
                TotalVariance = team.TotalVariance;
            }

            public void AddResult(double v, double w, double c)
            {
                V += v / c;
                W += w / (c * c);
            }
        }
    }
}