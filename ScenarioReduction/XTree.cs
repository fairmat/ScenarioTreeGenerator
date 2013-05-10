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
using System.Text;

namespace ScenarioReduction
{
    public class XTree : ScenarioTree
    {
        /// <summary>
        /// Arrays of interval lengths.
        /// </summary>
        double[] length;
        double[] k;
        double[] sigma;

        double approximationType = 0;//0 eulero,1 ?

        public XTree(int approximationType, int periods, int periodDays)
        {
            int[] IntervalDays = new int[periods];
            for (int z = 0; z < periods; z++)
            {
                IntervalDays[z] = periodDays;
            }

            Setup(approximationType, IntervalDays);
        }

        public XTree(int p_approx_type)
        {
            int[] IntervalDays = { 7, 7, 15, 30, 30, 30, 45, 45, 45, 45, 45 };

            Setup(p_approx_type, IntervalDays);
        }

        void Setup(int approximationType, int[] intervalDays)
        {
            this.approximationType = approximationType;

            int TotalDays = 0;
            length = new double[intervalDays.Length];
            for (int s = 0; s < length.Length; s++)
            {
                length[s] = intervalDays[s] * 24;
                TotalDays += intervalDays[s];
            }

            k = new double[2];
            sigma = new double[2];

            // Model with all factors.
            k[0] = 0.2764;
            k[1] = 0.273;
            sigma[0] = 3.6616;
            sigma[1] = 5.6190;

            // model where the price function is estimated only for the
            // hydraulic generation

            sigma[0] = 5.32252182265899;
            sigma[1] = 8.16718323852644;
            k[0] = 0.16583;
            k[1] = 0.2315;
            deltaT = length;
        }

        /// <summary>
        /// Aproximation of the standard deviation in the interval
        /// </summary>
        /// <param name="j"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        double Sigma(int j, double dt)
        {
            return sigma[j] * Math.Sqrt((1 - Math.Exp(-2 * k[j] * dt)) / (2 * k[j]));
        }

        /// <summary>
        /// generated
        /// </summary>
        public override void Generate()
        {
            List<TreeNode> entry_nodes = null;

            for (int i = 0; i < length.Length; i++)
            {
                // The period.
                int t = i + 1;

                // Delta T in months.
                double delta_t = length[i] / (31 * 24);
                double r_delta_t = Math.Sqrt(delta_t);
                int children = 2;

                List<TreeNode> new_entry_nodes = new List<TreeNode>();

                if (t == 1)
                {
                    double[] inital_value = new double[2];

                    // Avg.
                    inital_value[0] = 3;

                    // Diff.
                    inital_value[1] = -5;

                    TreeNode Root = new TreeNode(i, null, 1.0, inital_value);
                    Add(Root);
                    new_entry_nodes.Add(Root);
                }
                else
                {
                    foreach (TreeNode entry_node in entry_nodes)
                    {
                        // For each current leaf generate the children.
                        for (int c = 0; c < children; c++)
                        {
                            // Underlying state variables values.
                            double[] Value = new double[2];
                            if (children == 1)
                            {
                                for (int j = 0; j < 2; j++)
                                {
                                    if (approximationType == 0)
                                    {
                                        Value[j] = (1 - k[j] * delta_t) * entry_node.Value[j];
                                    }
                                    else
                                    {
                                        Value[j] = Math.Exp(-k[j] * delta_t) * entry_node.Value[j];
                                    }
                                }
                            }
                            else
                            {
                                if (c == 0)
                                {
                                    for (int j = 0; j < 2; j++)
                                    {
                                        if (approximationType == 0)
                                        {
                                            Value[j] = (1 - k[j] * delta_t) * entry_node.Value[j] + r_delta_t * sigma[j];
                                        }
                                        else
                                        {
                                            Value[j] = Math.Exp(-k[j] * delta_t) * entry_node.Value[j] + Sigma(j, delta_t);
                                        }
                                    }
                                }
                                else
                                {
                                    for (int j = 0; j < 2; j++)
                                    {
                                        if (approximationType == 0)
                                        {
                                            Value[j] = (1 - k[j] * delta_t) * entry_node.Value[j] - r_delta_t * sigma[j];
                                        }
                                        else
                                        {
                                            Value[j] = Math.Exp(-k[j] * delta_t) * entry_node.Value[j] - Sigma(j, delta_t);
                                        }
                                    }
                                }
                            }

                            TreeNode NewNode = new TreeNode(i,
                                                           entry_node,
                                                           entry_node.Probability * 1.0 / (double)children,
                                                           Value);

                            Add(NewNode);
                            new_entry_nodes.Add(NewNode);
                        }
                    }
                }

                entry_nodes = new_entry_nodes;
            }
        }
    }
}
