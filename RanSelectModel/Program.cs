using System;

namespace RanSelectModel.cs
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (double overturnIfOffBy in new double[] { 0.0, 0.1, 0.2, 0.3, 0.4, 0.5 })
                foreach (int numAppellateJudges in new int[] { 1, 3, 5} )
                    foreach (double probabilityReview in new double[] { 1.0 })
                    {
                        Model m = new Model()
                        {
                            overturnIfOffBy = overturnIfOffBy,
                            numAppellateJudges = numAppellateJudges,
                            probabilityReview = probabilityReview
                        };
                        m.Initialize();
                        m.Calculate();
                    }
        }
    }
}
