using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RanSelectModel.cs
{
    public static class ObfuscationCalc
    {
        public static Random Ran = new Random();

        static double[] c = { 2.515517, 0.802853, 0.010328 };
        static double[] d = { 1.432788, 0.189269, 0.001308 };

        static double RationalApproximation(double t)
        {
            // Abramowitz and Stegun formula 26.2.23.
            // The absolute value of the error should be less than 4.5 e-4.
            return t - ((c[2] * t + c[1]) * t + c[0]) /
                        (((d[2] * t + d[1]) * t + d[0]) * t + 1.0);
        }

        static double NormalCDFInverse(double p)
        {
            if (p <= 0.0 || p >= 1.0)
            {
                string msg = String.Format("Invalid input argument: {0}.", p);
                throw new ArgumentOutOfRangeException(msg);
            }

            // See article above for explanation of this section.
            if (p < 0.5)
            {
                // F^-1(p) = - G^-1(p)
                return -RationalApproximation(Math.Sqrt(-2.0 * Math.Log(p)));
            }
            else
            {
                // F^-1(p) = G^-1(1-p)
                return RationalApproximation(Math.Sqrt(-2.0 * Math.Log(1.0 - p)));
            }
        }

        static double erf(double x)
        {
            // constants
            double a1 = 0.254829592;
            double a2 = -0.284496736;
            double a3 = 1.421413741;
            double a4 = -1.453152027;
            double a5 = 1.061405429;
            double p = 0.3275911;

            // Save the sign of x
            int sign = 1;
            if (x < 0)
                sign = -1;
            x = Math.Abs(x);

            // A&S formula 7.1.26
            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return sign * y;
        }

        public static double norm_pdf(double x, double standard_deviation = 1.0)
        {
            double ssquaredtimes2 = 2 * standard_deviation * standard_deviation;
            double rt2p = Math.Sqrt(2 * Math.PI);
            return Math.Exp(-x * x / ssquaredtimes2) / (rt2p);
        }

        public static double Calculate_Alt(double sigma, double z)
        {
            double sigma_rt2 = Math.Sqrt(2) * sigma; // (square route of 2) times s
            double rt2p = Math.Sqrt(2 * Math.PI);
            double ssquaredtimes2 = 2 * sigma * sigma;
            double numeratorterm1 = -0.5 * z * erf((z - 1) / sigma_rt2);
            double numeratorterm2 = 0.5 * z * erf(z / sigma_rt2);
            double embeddedinterm3a = Math.Exp(-z * z / ssquaredtimes2);
            double embeddedinterm3b = Math.Exp(-(z - 1) * (z - 1) / ssquaredtimes2);
            double numeratorterm3 = sigma * (embeddedinterm3a - embeddedinterm3b) / rt2p;
            double testcalc = sigma * (norm_pdf(z, sigma) - norm_pdf(z - 1, sigma));
            if (Math.Abs(numeratorterm3 - testcalc) > 0.00001)
                throw new Exception();
            double numerator = numeratorterm1 + numeratorterm2 + numeratorterm3;
            double denominatorterm1 = erf(z / sigma_rt2);
            double denominatorterm2 = -erf((z - 1) / sigma_rt2);
            double denominator = 0.5 * (denominatorterm1 + denominatorterm2);
            return numerator / denominator;
        }

        public static double CalculateSignal(double standardDeviation, double originalNumber)
        {
            double normalDistDraw = NormalDistributionDraw(standardDeviation);
            return originalNumber + normalDistDraw;
        }

        public static double NormalDistributionDraw(double standardDeviation)
        {
            double r = UniformDistributionDraw();
            double normalDistDraw = NormalCDFInverse(r) * standardDeviation;
            return normalDistDraw;
        }

        public static double UniformDistributionDraw()
        {
            double r;
            do
            {
                r = Ran.NextDouble();
            }
            while (r == 0.0 || r == 1.0);
            return r;
        }

        public static double CalculateEstimateGivenSignal(double standardDeviation, double obfuscatedNumber)
        {
            if (standardDeviation == 0)
            {
                // avoid division by zero
                if (obfuscatedNumber > 0 && obfuscatedNumber < 1)
                    return obfuscatedNumber;
                return double.NaN;
            }
            double sigma_rt2 = Math.Sqrt(2) * standardDeviation; // (square route of 2) times s
                                                                 // [erf(z / rt2 * s)] z-1 to z
            double erfTerm = erf(obfuscatedNumber / sigma_rt2) - erf((obfuscatedNumber - 1) / sigma_rt2);
            double phiTerm = 2 * standardDeviation * (norm_pdf(obfuscatedNumber, standardDeviation) - norm_pdf((obfuscatedNumber - 1), standardDeviation));
            double result = obfuscatedNumber + phiTerm / erfTerm;
            return result;
        }

        public static double CalculateDerivativeAtPoint(double standardDeviation, double obfuscatedNumber, bool withRespectToStandardDeviation)
        {
            double originalCalculation = CalculateEstimateGivenSignal(standardDeviation, obfuscatedNumber);
            double derivDistance = 0.0001;
            if (withRespectToStandardDeviation)
                standardDeviation += derivDistance;
            else
                obfuscatedNumber += 0.0001;
            double nearbyCalculation = CalculateEstimateGivenSignal(standardDeviation, obfuscatedNumber);
            double derivative = (nearbyCalculation - originalCalculation) / derivDistance;
            return derivative;
        }

        public static string Calculate_Group(IEnumerable<double> sd, IEnumerable<double> obf)
        {
            string theString = "";
            foreach (var s in sd)
                theString += String.Join(",", obf.Select(x => CalculateEstimateGivenSignal(s, x).ToString())) + "\n";
            return theString;
        }

    }
}
