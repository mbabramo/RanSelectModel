using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RanSelectModel.cs
{
    public class Model
    {
        public double numCases = 3_000_000;
        public int numDecisionmakers = 100_000;
        public int numAppellateJudges = 3;
        public double standardDeviationOfObfuscation = 0.2;
        public double preferenceStandardDeviationMean = 0.2;
        public double preferenceStandardDeviationStandardDeviation = 0.1;
        public double probabilityReview = 1.0;
        public double overturnIfOffBy = 0.1;
        public double[] preferenceStandardDeviation;

        public void Initialize()
        {

            // Some decisionmakers have more of a tendency to weight their own preferences.
            preferenceStandardDeviation = new double[numDecisionmakers];
            for (int i = 0; i < numDecisionmakers; i++)
                preferenceStandardDeviation[i] = ObfuscationCalc.NormalDistributionDraw(preferenceStandardDeviationStandardDeviation) + preferenceStandardDeviationMean;
        }

        public void Calculate()
        { 
            int[] numDecisionsReviewed = new int[numDecisionmakers];
            int[] numDecisionsOverturned = new int[numDecisionmakers];
            double[] proportionOverturned = new double[numDecisionmakers];
            int correctDecisionInitially = 0;
            int correctDecisionUltimately = 0;

            for (int c = 0; c < numCases; c++)
            {
                double actualQuality = ObfuscationCalc.UniformDistributionDraw(); // from 0 to 1.
                bool correctOutcome = actualQuality > 0.5;

                int decisionmaker = ObfuscationCalc.Ran.Next(0, numDecisionmakers);
                List<int> decisionmakers = new List<int>() { decisionmaker };
                double decision = GetDecision(decisionmaker, standardDeviationOfObfuscation, actualQuality);
                bool outcome = decision > 0.5;
                if (correctOutcome == outcome)
                    correctDecisionInitially++;

                if (ObfuscationCalc.Ran.NextDouble() < probabilityReview)
                {
                    int votesToAffirm = 0;
                    int votesToReverse = 0;
                    for (int j = 0; j < numAppellateJudges; j++)
                    {
                        int appellateDecisionmaker;
                        do
                        {
                            appellateDecisionmaker = ObfuscationCalc.Ran.Next(0, numDecisionmakers);
                        }
                        while (decisionmakers.Contains(appellateDecisionmaker));
                        decisionmakers.Add(appellateDecisionmaker);
                        double preferredDecision = GetDecision(decisionmaker, standardDeviationOfObfuscation, actualQuality);
                        bool preferredOutcome = preferredDecision > 0.5;
                        if (preferredOutcome == outcome || Math.Abs(preferredDecision - 0.5) < overturnIfOffBy)
                            votesToAffirm++;
                        else
                            votesToReverse++;
                    }

                    numDecisionsReviewed[decisionmaker]++;
                    bool reversed = votesToReverse > votesToAffirm;
                    if (reversed)
                        numDecisionsOverturned[decisionmaker]++;
                    bool ultimateOutcome = reversed ? !outcome : outcome;
                    if (correctOutcome == ultimateOutcome)
                        correctDecisionUltimately++;
                }
                else if (correctOutcome == outcome)
                    correctDecisionUltimately++;
            }
            for (int d = 0; d < numDecisionmakers; d++)
                proportionOverturned[d] = numDecisionsReviewed[d] == 0 ? 0 : (double)numDecisionsOverturned[d] / (double)numDecisionsReviewed[d];
            double corr = ComputeCoeff(preferenceStandardDeviation, proportionOverturned);
            double correctOutcomeInitiallyPercentage = (double) correctDecisionInitially / (double) numCases;
            double correctOutcomeUltimatelyPercentage = (double)correctDecisionUltimately / (double)numCases;
            Console.WriteLine($"Trial judges noise {standardDeviationOfObfuscation} Appellate judges {numAppellateJudges} probability review {probabilityReview} overturnIfOffBy {overturnIfOffBy} correct initial {correctOutcomeInitiallyPercentage} correct ultimately {correctOutcomeUltimatelyPercentage} correlation with judge quality {corr}");
        }



        public double GetDecision(int decisionmaker, double standardDeviationOfObfuscation, double actualQuality)
        {
            double estimate = GetEstimate(standardDeviationOfObfuscation, actualQuality);
            double bias = ObfuscationCalc.NormalDistributionDraw(preferenceStandardDeviation[decisionmaker]);
            return estimate + bias;
        }

        public double GetEstimate(double standardDeviationOfObfuscation, double actualQuality)
        {
            double signal = ObfuscationCalc.CalculateSignal(standardDeviationOfObfuscation, actualQuality);
            double estimate = ObfuscationCalc.CalculateEstimateGivenSignal(standardDeviationOfObfuscation, signal);
            return estimate;
        }

        public double ComputeCoeff(double[] values1, double[] values2)
        {
            if (values1.Length != values2.Length)
                throw new ArgumentException("values must be the same length");

            var avg1 = values1.Average();
            var avg2 = values2.Average();

            var sum1 = values1.Zip(values2, (x1, y1) => (x1 - avg1) * (y1 - avg2)).Sum();

            var sumSqr1 = values1.Sum(x => Math.Pow((x - avg1), 2.0));
            var sumSqr2 = values2.Sum(y => Math.Pow((y - avg2), 2.0));

            var result = sum1 / Math.Sqrt(sumSqr1 * sumSqr2);

            return result;
        }
    }
}
