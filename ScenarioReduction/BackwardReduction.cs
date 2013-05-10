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
using System.Collections.Generic;
using System.IO;

namespace ScenarioReduction
{
    /// <summary>
    /// Implements the backward reduction of a single scenario...
    /// </summary>
    public class BackwardReduction : ScenarioReduction
    {
        // List of already removed indices.
        private bool[] removed_indices;
        public BackwardReduction(ScenarioTree p_st)
            : base(p_st)
        {
            LogReset();
        }

        float[][] scenario_distance_matrix;

        void CalculateScenarioDistanceMatrix()
        {
            Log("Calculate Scenario Distance Matrix (Start)");
            int N = trajectories.Length;

            scenario_distance_matrix = new float[N][];

            for (int n1 = 0; n1 < N; n1++)
            {
                scenario_distance_matrix[n1] = new float[N - n1 - 1];
            }

            for (int n1 = 0; n1 < N; n1++)
            {
                if (n1 % 100 == 0)
                    Log(n1.ToString());

                for (int n2 = n1 + 1; n2 < N; n2++)
                {
                    scenario_distance_matrix[n1][n2 - n1 - 1] = (float)ScenarioDistance(n1, n2, standardizedTrajectories[n1],
                                                                         standardizedTrajectories[n2]);

                    //scenario_distance_matrix[n2, n1] = scenario_distance_matrix[n1, n2];
                }
            }

            Log("Calculate Scenario Distance Matrix (End)");
        }

        string log_name = @"BackwardReduction_log.txt";
        void LogReset()
        {
            FileInfo fi = new FileInfo(log_name);
            if (fi.Exists) fi.Delete();
        }

        void Log(string s)
        {
            /*
            FileInfo fi = new FileInfo(log_name);
            StreamWriter sw = null;
            if(!fi.Exists)
                sw= fi.CreateText();
            else
                sw = fi.AppendText();

            if (sw != null)
            {

                sw.WriteLine(DateTime.Now.ToString()+"\t"+ s);
                sw.Close();
                sw.Dispose();
            }
            */
        }

        /// <summary>
        /// One step of the backward reduction algorithm
        /// if the index is not in the removed_indices array.
        /// </summary>
        /// <returns></returns>
        int RemoveOneScenario()
        {
            int N = trajectories.Length;
            double _min = double.MaxValue;

            int arg_min_lj_l = -1;//  index of scenario to remove
            int arg_min_lj_j = -1;//  its pair...

            for (int l = 0; l < N; l++)
                if (removed_indices[l] == false)
                {
                    double min_j = double.MaxValue;
                    int arg_min_j = -1;
                    ///////////////////////  Inner minimization...
                    for (int j = 0; j < N; j++)
                        if (removed_indices[j] == false)
                            if (j != l)
                            {
                                double d = 0;
                                if (l < j)
                                    d = scenario_distance_matrix[l][j - l - 1];
                                else
                                    d = scenario_distance_matrix[j][l - j - 1];
                                if (min_j > d)
                                {
                                    min_j = d;
                                    arg_min_j = j;
                                }
                            }

                    /////////////////////////////

                    double d2 = p[l] * min_j;
                    if (_min > d2)
                    {
                        _min = d2;
                        arg_min_lj_l = l;
                        arg_min_lj_j = arg_min_j;
                    }
                }
            // now arg_min_lj_l have is the scenario were to redistribute
            // and arg_min_lj_j is the scenario to remove

            //In order not to give precedente to the scenario order we
            //can exchange the indices some times
            p[arg_min_lj_j] += p[arg_min_lj_l];
            p[arg_min_lj_l] = 0;
            removed_indices[arg_min_lj_l] = true;
            return arg_min_lj_l;
        }

        List<int> keeped_indices;
        int RemoveOneScenarioV2()
        {
            double _min = double.MaxValue;

            if (keeped_indices == null)
            {
                int N = trajectories.Length;
                keeped_indices = new List<int>();
                for (int z = 0; z < N; z++) keeped_indices.Add(z);
            }

            int arg_min_lj_l = -1;//  index of scenario to remove
            int arg_min_lj_j = -1;//  its pair...
            int K = keeped_indices.Count;
            for (int kl = 0; kl < K; kl++)
            {
                int l = keeped_indices[kl];
                double min_j = double.MaxValue;
                int arg_min_j = -1;
                ///////////////////////  Inner minimization...
                for (int kj = 0; kj < K; kj++)
                    if (kj != kl)
                    {
                        int j = keeped_indices[kj];
                        double d = 0;
                        if (l < j)
                            d = scenario_distance_matrix[l][j - l - 1];
                        else
                            d = scenario_distance_matrix[j][l - j - 1];

                        if (min_j > d)
                        {
                            min_j = d;
                            arg_min_j = j;
                        }
                    }
                /////////////////////////////
                double d2 = p[l] * min_j;
                if (_min > d2)
                {
                    _min = d2;
                    arg_min_lj_l = l;
                    arg_min_lj_j = arg_min_j;
                }
            }
            // now arg_min_lj_l have is the scenario were to redistribute
            // and arg_min_lj_j is the scenario to remove

            p[arg_min_lj_j] += p[arg_min_lj_l];
            p[arg_min_lj_l] = 0;
            removed_indices[arg_min_lj_l] = true;
            keeped_indices.Remove(arg_min_lj_l);
            Log(_min.ToString());
            return arg_min_lj_l;
        }

        //mantains a list of pairs with the lowest distance.
        //This way if the scenario arg_min_lj_l is removed
        //the pair which comes after doesn't contain it and is already the
        //optimal solution

        double[] d2;
        int[] d_arg;

        int RemoveOneScenarioV3()
        {
            double _min = double.MaxValue;

            if (keeped_indices == null)
            {
                int N = trajectories.Length;
                keeped_indices = new List<int>();
                d2 = new double[N];
                d_arg = new int[N];
                for (int z = 0; z < N; z++)
                {
                    keeped_indices.Add(z);
                    d_arg[z] = -1;
                }
            }

            int arg_min_lj_l = -1;//  index of scenario to remove
            int arg_min_lj_j = -1;//  its pair...
            int K = keeped_indices.Count;
            for (int kl = 0; kl < K; kl++)
            {
                int l = keeped_indices[kl];
                double min_j = double.MaxValue;
                int arg_min_j = -1;
                ///////////////////////  Inner minimization...

                //The following speedup computation a lot...
                //If one of the not removed scenario had paired
                //a removed scenario, the minimum must be recalculated

                if (d_arg[l] == -1)//recompute distance...
                {
                    for (int kj = 0; kj < K; kj++)
                        if (kj != kl)
                        {
                            int j = keeped_indices[kj];
                            double d = 0;
                            if (l < j)
                                d = scenario_distance_matrix[l][j - l - 1];
                            else
                                d = scenario_distance_matrix[j][l - j - 1];

                            if (min_j > d)
                            {
                                min_j = d;
                                arg_min_j = j;
                            }
                        }
                    ///////////////////////////Keep the minimum values...
                    d2[l] = p[l] * min_j;
                    d_arg[l] = arg_min_j;
                }

                if (_min > d2[l])
                {
                    _min = d2[l];
                    arg_min_lj_l = l;
                    arg_min_lj_j = d_arg[l];// arg_min_j;
                }
            }
            // now arg_min_lj_l have is the scenario were to redistribute
            // and arg_min_lj_j is the scenario to remove

            p[arg_min_lj_j] += p[arg_min_lj_l];
            p[arg_min_lj_l] = 0;
            removed_indices[arg_min_lj_l] = true;
            keeped_indices.Remove(arg_min_lj_l);
            Log(_min.ToString());

            // Update all distances (1 is the minimum).
            for (int ki = 0; ki < keeped_indices.Count; ki++)
            {
                int i = keeped_indices[ki];
                if (d_arg[i] == arg_min_lj_l)
                {
                    // Recompute...
                    d_arg[i] = -1;
                }
            }

            return arg_min_lj_l;
        }

        /// <summary>
        /// Implements the reduction using the iterated backward reduction algorithm.
        /// </summary>
        /// <param name="K">The new number of scenarios.</param>
        public override void Reduce(int K)
        {
            int N = trajectories.Length;

            if (N == K)
            {
                newTrajectories = (float[][][])trajectories.Clone();
                reducedNodesTrajectories = (TreeNode[][])nodesTrajectories.Clone();
                Q = (double[])p.Clone();
                return;
            }

            CalculateScenarioDistanceMatrix();
            removed_indices = new bool[N];
            for (int n = 0; n < N; n++) removed_indices[n] = false;

            for (int k = 0; k < N - K; k++)
            {
                DateTime T0 = DateTime.Now;
                //int s = RemoveOneScenario();
                //int s = RemoveOneScenarioV2();
                int s = RemoveOneScenarioV3();
                DateTime T1 = DateTime.Now;

                Log("Removed scenario #" + s + "\t t=" + (T1 - T0).ToString() + "\t(" + k.ToString() + " of " + (N - K).ToString() + ")");

                //
                int actual_scenarios = N - k - 1;
                //if we have to
                /*
                if (actual_scenarios==)
                {
                    PackKeepedScenarios(actual_scenarios, N);
                    //ReducedScenarioTree
                }
                 */

            }//next k

            PackKeepedScenarios(K, N);

        }

        void PackKeepedScenarios(int K, int N)
        {
            //build the keeped scenarios
            int T = trajectories[0].Length;
            int D = trajectories[0][0].Length;
            newTrajectories = new float[K][][];
            reducedNodesTrajectories = new TreeNode[K][];
            Q = new double[K];
            int j1 = 0;

            for (int j = 0; j < N; j++)
            {
                //if has not being removed...
                if (removed_indices[j] == false)
                {
                    newTrajectories[j1] = (float[][])trajectories[j].Clone();
                    reducedNodesTrajectories[j1] = (TreeNode[])nodesTrajectories[j].Clone();
                    Q[j1] = p[j];
                    j1++;
                }
            }
        }
    }
}
