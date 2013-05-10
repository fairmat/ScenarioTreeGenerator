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
        private double[] length;
        private double[] k;
        private double[] sigma;

        private double approximationType = 0;//0 eulero,1 ?

        public XTree(int approximationType, int periods, int periodDays)
        {
            int[] intervalDays = new int[periods];
            for (int z = 0; z < periods; z++)
            {
                intervalDays[z] = periodDays;
            }

            Setup(approximationType, intervalDays);
        }

        public XTree(int p_approx_type)
        {
            int[] intervalDays = { 7, 7, 15, 30, 30, 30, 45, 45, 45, 45, 45 };

            Setup(p_approx_type, intervalDays);
        }

        private void Setup(int approximationType, int[] intervalDays)
        {
            this.approximationType = approximationType;

            length = new double[intervalDays.Length];
            for (int s = 0; s < length.Length; s++)
            {
                length[s] = intervalDays[s] * 24;
            }

            this.k = new double[2];
            this.sigma = new double[2];

            // Model with all factors.
            this.k[0] = 0.2764;
            this.k[1] = 0.273;
            this.sigma[0] = 3.6616;
            this.sigma[1] = 5.6190;

            // model where the price function is estimated only for the
            // hydraulic generation

            this.sigma[0] = 5.32252182265899;
            this.sigma[1] = 8.16718323852644;
            this.k[0] = 0.16583;
            this.k[1] = 0.2315;
            this.deltaT = length;
        }

        /// <summary>
        /// Aproximation of the standard deviation in the interval
        /// </summary>
        /// <param name="j"></param>
        /// <param name="dt"></param>
        /// <returns></returns>
        private double Sigma(int j, double dt)
        {
            return this.sigma[j] * Math.Sqrt((1 - Math.Exp(-2 * this.k[j] * dt)) / (2 * this.k[j]));
        }

        /// <summary>
        /// generated
        /// </summary>
        public override void Generate()
        {
            List<TreeNode> entryNodes = null;

            for (int i = 0; i < this.length.Length; i++)
            {
                // The period.
                int t = i + 1;

                // Delta T in months.
                double delta_t = this.length[i] / (31 * 24);
                double r_delta_t = Math.Sqrt(delta_t);
                int children = 2;

                List<TreeNode> newEntryNodes = new List<TreeNode>();

                if (t == 1)
                {
                    double[] inital_value = new double[2];

                    // Avg.
                    inital_value[0] = 3;

                    // Diff.
                    inital_value[1] = -5;

                    TreeNode root = new TreeNode(i, null, 1.0, inital_value);
                    Add(root);
                    newEntryNodes.Add(root);
                }
                else
                {
                    foreach (TreeNode entryNode in entryNodes)
                    {
                        // For each current leaf generate the children.
                        for (int c = 0; c < children; c++)
                        {
                            // Underlying state variables values.
                            double[] value = new double[2];
                            if (children == 1)
                            {
                                for (int j = 0; j < 2; j++)
                                {
                                    if (this.approximationType == 0)
                                    {
                                        value[j] = (1 - this.k[j] * delta_t) * entryNode.Value[j];
                                    }
                                    else
                                    {
                                        value[j] = Math.Exp(-this.k[j] * delta_t) * entryNode.Value[j];
                                    }
                                }
                            }
                            else
                            {
                                if (c == 0)
                                {
                                    for (int j = 0; j < 2; j++)
                                    {
                                        if (this.approximationType == 0)
                                        {
                                            value[j] = (1 - this.k[j] * delta_t) * entryNode.Value[j] + r_delta_t * this.sigma[j];
                                        }
                                        else
                                        {
                                            value[j] = Math.Exp(-this.k[j] * delta_t) * entryNode.Value[j] + Sigma(j, delta_t);
                                        }
                                    }
                                }
                                else
                                {
                                    for (int j = 0; j < 2; j++)
                                    {
                                        if (this.approximationType == 0)
                                        {
                                            value[j] = (1 - this.k[j] * delta_t) * entryNode.Value[j] - r_delta_t * this.sigma[j];
                                        }
                                        else
                                        {
                                            value[j] = Math.Exp(-this.k[j] * delta_t) * entryNode.Value[j] - Sigma(j, delta_t);
                                        }
                                    }
                                }
                            }

                            TreeNode newNode = new TreeNode(i,
                                                           entryNode,
                                                           entryNode.Probability * 1.0 / (double)children,
                                                           value);

                            Add(newNode);
                            newEntryNodes.Add(newNode);
                        }
                    }
                }

                entryNodes = newEntryNodes;
            }
        }
    }
}
