/* Copyright (C) 2009-2013 Fairmat SRL (info@fairmat.com, http://www.fairmat.com/)
 * Author(s): Matteo Tesser (matteo.tesser@fairmat.com)
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace ScenarioReduction
{
    public class ScenarioReduction
    {
        /// <summary>
        /// Used to select the type of distance to use:
        /// 0 = standard, 1 = Fortet-Mourier.
        /// </summary>
        public static int DistanceType = 0;

        /// <summary>
        /// Used to select the type of variance standardization:
        /// 0 = no standardization, 1 = puntal std, 2 = average std.
        /// </summary>
        public static int VarianceStandardizationType = 0;
        private double[] deltaT;

        /// <summary>
        /// Associates each element of the trajectory to the original node.
        /// </summary>
        protected TreeNode[][] nodesTrajectories;

        protected TreeNode[][] reducedNodesTrajectories;

        /// <summary>
        /// Array of uncertainties trajectories.
        /// </summary>
        /// <remarks>It's an array of N trajectories of length T and dimension K.</remarks>
        protected float[][][] trajectories;

        /// <summary>
        /// Standardized version of <see cref="trajectories"/>,
        /// used to make comparisons.
        /// </summary>
        /// <remarks>It's an array of N trajectories of length T and dimension K.</remarks>
        protected float[][][] standardizedTrajectories;

        /// <summary>
        /// Probability of each trajectory.
        /// </summary>
        protected double[] p;

        protected double[] Q;
        protected float[][][] newTrajectories;

        protected double[] distanceWithw0;

        /// <summary>
        /// Fixed element.
        /// </summary>
        private float[][] w0;

        private ScenarioTree inputScenarioTree;

        public float[][][] ReducedScenarios
        {
            get
            {
                return this.newTrajectories;
            }
        }

        public double[] ReducedProbabilities
        {
            get
            {
                return this.Q;
            }
        }

        public ScenarioReduction(ScenarioTree scenarioTree)
        {
            this.inputScenarioTree = scenarioTree;
            this.deltaT = scenarioTree.deltaT;

            this.trajectories = (float[][][])scenarioTree.Scenarios;
            this.p = (double[])scenarioTree.ScenariosProbabilities.Clone();

            // Standardize the scenario variables.
            Standardize(VarianceStandardizationType);

            this.nodesTrajectories = scenarioTree.ScenariosNodes;

            this.distanceWithw0 = new double[this.nodesTrajectories.Length];
            for (int z = 0; z < this.distanceWithw0.Length; z++)
            {
                this.distanceWithw0[z] = -1;
            }
        }

        /// <summary>
        /// Standardizes the trajectories.
        /// Type = 0 no standardizations.
        /// Type = 1 divides by the puntual standard deviation.
        /// Type = 2 divede by the standard deviation media.
        /// </summary>
        /// <param name="type"></param>
        private void Standardize(int type)
        {
            int S = this.trajectories.Length;
            int T = this.trajectories[0].Length;
            int D = this.trajectories[0][0].Length;

            this.standardizedTrajectories = new float[S][][];

            for (int s = 0; s < S; s++)
            {
                this.standardizedTrajectories[s] = new float[T][];
                for (int t = 0; t < T; t++)
                {
                    this.standardizedTrajectories[s][t] = new float[D];
                }
            }

            if (type == 0)
            {
                for (int t = 0; t < T; t++)
                {
                    for (int d = 0; d < D; d++)
                    {
                        for (int s = 0; s < S; s++)
                        {
                            this.standardizedTrajectories[s][t][d] = this.trajectories[s][t][d];
                        }
                    }
                }

                return;
            }

            double[,] means = new double[T, D];
            double[,] stds = new double[T, D];

            for (int t = 0; t < T; t++)
            {
                for (int d = 0; d < D; d++)
                {
                    for (int s = 0; s < S; s++)
                    {
                        means[t, d] += this.p[s] * this.trajectories[s][t][d];
                    }
                }
            }

            for (int t = 0; t < T; t++)
            {
                for (int d = 0; d < D; d++)
                {
                    for (int s = 0; s < S; s++)
                        stds[t, d] += this.p[s] * Math.Pow(this.trajectories[s][t][d] - means[t, d], 2);

                    stds[t, d] = Math.Sqrt(stds[t, d]);
                }
            }

            if (type == 1)
            {
                for (int t = 0; t < T; t++)
                {
                    for (int d = 0; d < D; d++)
                    {
                        for (int s = 0; s < S; s++)
                        {
                            if (stds[t, d] > 10e-12)
                            {
                                this.standardizedTrajectories[s][t][d] = (float)((this.trajectories[s][t][d] - means[t, d]) / stds[t, d]);
                            }
                        }
                    }
                }
            }
            else
            {
                // type == 2.
                double[] avg_std = new double[D];
                double[] avg_mean = new double[D];

                for (int t = 0; t < T; t++)
                {
                    for (int d = 0; d < D; d++)
                    {
                        avg_std[d] += (1.0 / T) * stds[t, d];
                        avg_mean[d] += (1.0 / T) * means[t, d];
                    }
                }

                for (int t = 0; t < T; t++)
                {
                    for (int d = 0; d < D; d++)
                    {
                        for (int s = 0; s < S; s++)
                        {
                            if (avg_std[d] > 10e-12)
                            {
                                this.standardizedTrajectories[s][t][d] = (float)((this.trajectories[s][t][d] - avg_mean[d]) / avg_std[d]);
                            }
                        }
                    }
                }
            }

            return;
        }

        protected double ScenarioDistance(int n1, int n2, float[][] t1, float[][] t2)
        {
            if (DistanceType == 0)
                return ScenarioDistanceStandard(n1, n2, t1, t2);
            else
                return ScenarioDistanceFortetMorier(n1, n2, t1, t2);
        }

        /// <summary>
        /// Simpler formula.
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        private double ScenarioDistanceStandard(int n1, int n2, float[][] t1, float[][] t2)
        {
            double d = 0;
            int T = t1.Length;
            for (int t = 0; t < T; t++)
                d += this.deltaT[t] * Distance(t1[t], t2[t]);
            return d;
        }

        /// <summary>
        /// Distance betweeen two trajectories
        /// (c in the article of  romish formula (3)).
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        private double ScenarioDistanceFortetMorier(int n1, int n2, float[][] t1, float[][] t2)
        {
            int T = t1.Length;

            // Generates the fixed element (w0).
            if (this.w0 == null)
            {
                // Be sure that the fixed element is present.
                this.w0 = (float[][])t1.Clone();
            }

            double d1_d2 = 0;
            double d1_d0 = 0;
            double d2_d0 = 0;

            if (this.distanceWithw0[n1] < 0)
            {
                for (int t = 0; t < T; t++)
                {
                    d1_d0 += this.deltaT[t] * Distance2(t1[t], this.w0[t]);
                }

                // Put the value into the cache.
                this.distanceWithw0[n1] = d1_d0;
            }
            else
            {
                // Get the value from the cache.
                d1_d0 = this.distanceWithw0[n1];
            }

            if (this.distanceWithw0[n2] < 0)
            {
                for (int t = 0; t < T; t++)
                {
                    d2_d0 += this.deltaT[t] * Distance2(t2[t], this.w0[t]);
                }

                // Put the value into the cache.
                this.distanceWithw0[n2] = d2_d0;
            }
            else
            {
                // Get the value from the cache.
                d2_d0 = this.distanceWithw0[n2];
            }

            for (int t = 0; t < T; t++)
            {
                d1_d2 += this.deltaT[t] * Distance(t1[t], t2[t]);
            }

            return Math.Max(1.0, Math.Max(d1_d0, d2_d0)) * d1_d2;
        }

        public virtual void Reduce(int n)
        {
            throw new Exception("Implement it!");
        }

        private double Distance(double[] t1, double[] t2)
        {
            return Math.Sqrt(Distance2(t1, t2));
        }

        private double Distance(float[] t1, float[] t2)
        {
            return Math.Sqrt(Distance2(t1, t2));
        }

        /// <summary>
        /// Does the norm^2 between two vectors.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        private double Distance2(double[] t1, double[] t2)
        {
            int T = t1.Length;
            double d = 0;
            for (int t = 0; t < T; t++)
            {
                double r = t1[t] - t2[t];
                d += r * r;
            }

            return d;
        }

        private double Distance2(float[] t1, float[] t2)
        {
            int D = t1.Length;
            double d = 0;
            for (int t = 0; t < D; t++)
            {
                double r = t1[t] - t2[t];
                d += r * r;
            }

            return d;
        }

        private unsafe double Distance2(float* t1, float* t2, int D)
        {
            double d = 0;
            for (int t = 0; t < D; t++)
            {
                double r = t1[t] - t2[t];
                d += r * r;
            }

            return d;
        }

        /// <summary>
        /// Gets a rebuilt a scenario trees from the reduced trajectories.
        /// </summary>
        public ScenarioTree ReducedScenarioTree
        {
            get
            {
                ScenarioTree reduced = new ScenarioTree();
                ScenarioTree reduced_cloned = new ScenarioTree();

                reduced_cloned.OneBasedPeriods = this.inputScenarioTree.OneBasedPeriods;

                for (int t = 0; t < this.reducedNodesTrajectories[0].Length; t++)
                {
                    for (int s = 0; s < this.reducedNodesTrajectories.Length; s++)
                    {
                        TreeNode tn = this.reducedNodesTrajectories[s][t];

                        int z = reduced.IndexOf(tn);
                        if (z == -1)
                        {
                            reduced.Add(tn);
                            TreeNode tnc = tn.Clone();
                            tnc.Probability = this.Q[s];
                            reduced_cloned.Add(tnc);
                        }
                        else
                        {
                            reduced_cloned[z].Probability += this.Q[s];
                            if (reduced_cloned[z].Probability > 1.00 + 1.0e-6)
                            {
                                throw new Exception("Wrong Reduced Probability! " +
                                                    "probabilities sum to " +
                                                    reduced_cloned[z].Probability.ToString());
                            }
                        }
                    }
                }

                reduced_cloned.deltaT = (double[])this.deltaT.Clone();

                // Now add all the preceding ones.
                for (int z = 0; z < reduced.Count; z++)
                    if (reduced[z].Predecessor != null)
                        reduced_cloned[z].Predecessor = reduced_cloned[reduced.FindPredecessorIndex(z)];

                reduced_cloned.AssignIds();
                reduced_cloned.componentNames = this.inputScenarioTree.componentNames;
                return reduced_cloned;
            }
        }
    }
}
