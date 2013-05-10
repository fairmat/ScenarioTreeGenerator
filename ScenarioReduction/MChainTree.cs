/* Copyright (C) 2009-2012 Fairmat SRL (info@fairmat.com, http://www.fairmat.com/)
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

namespace ScenarioReduction
{
    public class MChainTree : ScenarioTree
    {
        private double[,] mcProb;
        private int lastPeriodIndex = -1;

        /// <summary>
        /// Index where the probability event takes places.
        /// </summary>
        private int[] eventStart;

        public MChainTree(int p_Periods, double[,] p_MC_prob, int[] p_event_start)
        {
            lastPeriodIndex = p_Periods;
            eventStart = p_event_start;
            mcProb = p_MC_prob;
        }

        public override void Generate()
        {
            List<TreeNode> entry_nodes = new List<TreeNode>();
            List<TreeNode> new_entry_nodes = new List<TreeNode>();

            int[] event_end = new int[eventStart.Length];

            // Generate start and end.
            for (int e = 0; e < eventStart.Length; e++)
            {
                if (e == eventStart.Length - 1)
                    event_end[e] = lastPeriodIndex;
                else
                    event_end[e] = eventStart[e + 1] - 1;
            }

            // First part.
            // Does one node for each period.
            TreeNode predecessor = null;
            for (int t = 1; t < eventStart[0]; t++)
            {
                TreeNode tn = new TreeNode();

                tn.Period = t - 1;
                tn.Predecessor = predecessor;

                if (predecessor != null)
                {
                    tn.Probability = predecessor.Probability;
                    tn.Value = (float[])predecessor.Value.Clone();
                }
                else
                {
                    // This is the root.
                    tn.Value = new float[1];

                    // Start weather state variable.
                    tn.Value[0] = 0;
                    tn.Probability = 1.0;
                }

                predecessor = tn;
                Add(tn);
            }

            // Inserts the last node.
            entry_nodes.Add(this[Count - 1]);

            // Start calculating.
            // First take the number of states.
            int N = mcProb.GetLength(0);

            for (int subdivisions = 0; subdivisions < eventStart.Length; subdivisions++)
            {
                new_entry_nodes.Clear();
                foreach (TreeNode entry_node in entry_nodes)
                {
                    for (int t = eventStart[subdivisions]; t <= event_end[subdivisions]; t++)
                    {
                        if (t == eventStart[subdivisions])
                        {
                            for (int state = 0; state < N; state++)
                            {
                                TreeNode tn = new TreeNode(t - 1,
                                                           entry_node,
                                                           mcProb[(int)entry_node.Value[0], state] * entry_node.Probability);

                                tn.Value = new float[1];
                                tn.Value[0] = state;

                                Add(tn);

                                // We add also here.
                                if (t == event_end[subdivisions])
                                    new_entry_nodes.Add(tn);
                            }
                        }
                        else
                        {
                            for (int state = 0; state < N; state++)
                            {
                                predecessor = this[Count - N];

                                if (predecessor.Period != t - 2)
                                    throw new Exception("!");

                                TreeNode tn = new TreeNode(t - 1,
                                                           predecessor,
                                                           predecessor.Probability);

                                tn.Value = new float[1];
                                tn.Value[0] = state;
                                Add(tn);
                                if (t == event_end[subdivisions])
                                {
                                    new_entry_nodes.Add(tn);
                                }
                            }
                        }
                    } //# end next t
                } //# next entry node

                entry_nodes.Clear();
                entry_nodes.AddRange(new_entry_nodes);
            } //# end for subdivisions

            return;
        }
    }
}
