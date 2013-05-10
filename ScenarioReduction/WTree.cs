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
    public class WTree : ScenarioTree
    {
        private int weatherTypes = 3; //0,1,2
        private double[,] mcProbability;

        private int lastPeriodIndex = 11;

        public WTree(int periods)
        {
            this.lastPeriodIndex = periods;
        }

        private enum WTypes
        {
            Dry = 0,
            Normal = 1,
            Rainy = 2
        }

        public override void Generate()
        {
            List<TreeNode> entry_nodes = new List<TreeNode>();
            List<TreeNode> new_entry_nodes = new List<TreeNode>();

            int[] eventStart = { 3, 6, 9 };

            if (this.lastPeriodIndex < 5)
            {
                eventStart = new int[1];
                eventStart[0] = 2;
            }

            int[] eventEnd = new int[eventStart.Length];

            this.mcProbability = new double[this.weatherTypes, this.weatherTypes];

            this.mcProbability[(int)WTypes.Dry, (int)WTypes.Dry] = 0.4;
            this.mcProbability[(int)WTypes.Dry, (int)WTypes.Normal] = 0.3;
            this.mcProbability[(int)WTypes.Dry, (int)WTypes.Rainy] = 0.3;

            this.mcProbability[(int)WTypes.Normal, (int)WTypes.Dry] = 0.4;
            this.mcProbability[(int)WTypes.Normal, (int)WTypes.Normal] = 0.2;
            this.mcProbability[(int)WTypes.Normal, (int)WTypes.Rainy] = 0.4;

            this.mcProbability[(int)WTypes.Rainy, (int)WTypes.Dry] = 0.3;
            this.mcProbability[(int)WTypes.Rainy, (int)WTypes.Normal] = 0.5;
            this.mcProbability[(int)WTypes.Rainy, (int)WTypes.Rainy] = 0.2;

            //generate start and end...
            for (int e = 0; e < eventStart.Length; e++)
            {
                if (e == eventStart.Length - 1)
                    eventEnd[e] = this.lastPeriodIndex;
                else
                    eventEnd[e] = eventStart[e + 1] - 1;
            }

            //first part, one node for each period
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
                {  //IS THE ROOT
                    tn.Value = new float[1];
                    tn.Value[0] = 0;           //START WEATHER STATE VARIABLE
                    tn.Probability = 1.0;
                }

                tn.Attributes = new string[1];
                tn.Attributes[0] = ((WTypes)tn.Value[0]).ToString();

                predecessor = tn;
                Add(tn);
            }

            entry_nodes.Add(this[Count - 1]); //the last inserted node

            //now start

            for (int subdivisions = 0; subdivisions < eventStart.Length; subdivisions++)
            {
                new_entry_nodes.Clear();
                foreach (TreeNode entry_node in entry_nodes)
                {
                    for (int t = eventStart[subdivisions]; t <= eventEnd[subdivisions]; t++)
                    {
                        if (t == eventStart[subdivisions])
                        {
                            for (int weather = 0; weather < this.weatherTypes; weather++)
                            {
                                TreeNode tn = new TreeNode(t - 1,
                                                           entry_node,
                                                           this.mcProbability[(int)entry_node.Value[0], weather] * entry_node.Probability);

                                tn.Value = new float[1];
                                tn.Value[0] = weather;

                                tn.Attributes = new string[1];
                                tn.Attributes[0] = ((WTypes)tn.Value[0]).ToString();

                                Add(tn);
                                if (t == eventEnd[subdivisions])
                                    new_entry_nodes.Add(tn);
                            }
                        }
                        else
                        {
                            for (int weather = 0; weather < this.weatherTypes; weather++)
                            {
                                predecessor = this[Count - this.weatherTypes];

                                if (predecessor.Period != t - 2)
                                    throw new Exception("!");

                                TreeNode tn = new TreeNode(t - 1,
                                                           predecessor,
                                                           predecessor.Probability);

                                tn.Value = new float[1];
                                tn.Value[0] = weather;

                                tn.Attributes = new string[1];
                                tn.Attributes[0] = ((WTypes)tn.Value[0]).ToString();

                                Add(tn);

                                if (t == eventEnd[subdivisions])
                                    new_entry_nodes.Add(tn);
                            }
                        }
                    }
                }

                entry_nodes.Clear();
                entry_nodes.AddRange(new_entry_nodes);
            }

            return;
        }
    }
}
