using UnityEngine;
using System;

namespace TrueSkill2
{
    public class Rating
    {
        // Default TrueSkill parameters
        private const double DefaultInitialMean = 25;
        private const double DefaultMin = 0.0;
        private const double DefaultMax = 50.0;

        // Player's actual skill parameters
        public double Mean { get; private set; }
        public double Variance { get; private set; }
        public double StandardDeviation => Math.Sqrt(Variance);

        // Custom range scaling
        private readonly double minRating;
        private readonly double maxRating;
        private readonly double scaleFactor;
        private readonly double offset;

        public Rating(double mean = DefaultInitialMean,
                    double variance = DefaultInitialMean/3,
                    double minRating = 1000.0,
                    double maxRating = 2600.0)
        {
            // Validate inputs
            if (maxRating <= minRating)
                throw new ArgumentException("maxRating must be greater than minRating");

            // Initialise true skill parameters
            Mean = mean;
            Variance = variance; //

            // Configure scaling
            this.minRating = minRating;
            this.maxRating = maxRating;
            scaleFactor = (maxRating - minRating) / (DefaultMax - DefaultMin);
            offset = minRating - (DefaultMin * scaleFactor);
        }

        public void Update(double newMean, double newVariance)
        {
            Mean = newMean;
            Variance = newVariance;
        }

        // Scaled properties
        public double ScaledMean => (Mean * scaleFactor) + offset;
        public double ScaledStandardDeviation => StandardDeviation * scaleFactor;
        public double ConservativeRating =>
            (Mean - 3 * StandardDeviation) * scaleFactor + offset;
    }
}
