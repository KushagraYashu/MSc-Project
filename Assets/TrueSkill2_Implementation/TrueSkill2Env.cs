using UnityEngine;
using System;

namespace TrueSkill2
{
    public class TrueSkill2Env
    {
        public double Beta { get; } // Skill uncertainty
        public double Tau { get; }  // Dynamics factor
        public double DrawProbability { get; }
        public double Epsilon { get; } // Draw margin

        public TrueSkill2Env(double beta = 1.0,                     
                                  double tau = 10e-16,
                                  double drawProbability = 0.01) 
            //beta and tau are from Halo 5 data from trueskill2 whitepaper
        {
            Beta = beta;
            Tau = tau;
            DrawProbability = drawProbability;
            Epsilon = CalculateDrawMargin(drawProbability, beta);
        }

        private static double CalculateDrawMargin(double drawProbability, double beta)
        {
            return Math.Sqrt(2) * beta * PhiInv((drawProbability + 1) / 2);
        }

        // Inverse normal CDF (accurate approximation)
        private static double PhiInv(double p)
        {
            if (p <= 0 || p >= 1)
                throw new ArgumentOutOfRangeException(nameof(p));

            // Rational approximation coefficients
            double[] a = { -3.969683028665376e+01, 2.209460984245205e+02, -2.759285104469687e+02,
                          1.383577518672690e+02, -3.066479806614716e+01, 2.506628277459239e+00 };

            double[] b = { -5.447609879822406e+01, 1.615858368580409e+02, -1.556989798598866e+02,
                          6.680131188771972e+01, -1.328068155288572e+01 };

            // Piecewise approximation
            if (p < 0.02425)
            {
                double q = Math.Sqrt(-2 * Math.Log(p));
                return (((((a[0] * q + a[1]) * q + a[2]) * q + a[3]) * q + a[4]) * q + a[5]) /
                       ((((b[0] * q + b[1]) * q + b[2]) * q + b[3]) * q + 1);
            }
            else if (p > 0.97575)
            {
                double q = Math.Sqrt(-2 * Math.Log(1 - p));
                return -(((((a[0] * q + a[1]) * q + a[2]) * q + a[3]) * q + a[4]) * q + a[5]) /
                       ((((b[0] * q + b[1]) * q + b[2]) * q + b[3]) * q + 1);
            }
            else
            {
                double q = p - 0.5;
                double r = q * q;
                return (((((a[0] * r + a[1]) * r + a[2]) * r + a[3]) * r + a[4]) * r + a[5]) * q /
                       (((((b[0] * r + b[1]) * r + b[2]) * r + b[3]) * r + b[4]) * r + 1);
            }
        }
    }
}
