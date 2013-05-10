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

    enum WTypes
    { Dry = 0, Normal = 1, Rainy = 2 };
    /*

    class WTreeNode : TreeNode
    {
        public
    }
    */

    class WTree : ScenarioTree
    {
        int WeatherTypes = 3; //0,1,2
        double[,] MC_prob;

        int LAST_PERIOD_INDEX = 11;


        public WTree(int p_Periods)
        {
            LAST_PERIOD_INDEX = p_Periods;
        }

        public override void Generate()
        {
            List<TreeNode> entry_nodes = new List<TreeNode>();
            List<TreeNode> new_entry_nodes = new List<TreeNode>();

            // int[] event_start={5,9};

            int[] event_start = { 3, 6, 9 };

            if (LAST_PERIOD_INDEX < 5)
            {
                event_start = new int[1];
                event_start[0] = 2;
            }
            //if(LAST_PERIOD_INDEX!=11)
            //    event_start[0]=2;


            int[] event_end = new int[event_start.Length];

            MC_prob = new double[WeatherTypes, WeatherTypes];


            MC_prob[(int)WTypes.Dry, (int)WTypes.Dry] = 0.4;
            MC_prob[(int)WTypes.Dry, (int)WTypes.Normal] = 0.3;
            MC_prob[(int)WTypes.Dry, (int)WTypes.Rainy] = 0.3;

            MC_prob[(int)WTypes.Normal, (int)WTypes.Dry] = 0.4;
            MC_prob[(int)WTypes.Normal, (int)WTypes.Normal] = 0.2;
            MC_prob[(int)WTypes.Normal, (int)WTypes.Rainy] = 0.4;

            MC_prob[(int)WTypes.Rainy, (int)WTypes.Dry] = 0.3;
            MC_prob[(int)WTypes.Rainy, (int)WTypes.Normal] = 0.5;
            MC_prob[(int)WTypes.Rainy, (int)WTypes.Rainy] = 0.2;






            //generate start and end...
            for (int e = 0; e < event_start.Length; e++)
            {
                if (e == event_start.Length - 1)
                    event_end[e] = LAST_PERIOD_INDEX;
                else
                    event_end[e] = event_start[e + 1] - 1;
            }
            ////////////////////

            //first part, one node for each period
            TreeNode predecessor = null;
            for (int t = 1; t < event_start[0]; t++)
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


                //     let W_NODES:= W_NODES union {current_i};



                predecessor = tn;
                Add(tn);
            }
            entry_nodes.Add(this[Count - 1]); //the last inserted node

            //now start

            for (int subdivisions = 0; subdivisions < event_start.Length; subdivisions++)
            {

                new_entry_nodes.Clear();
                foreach (TreeNode entry_node in entry_nodes)
                {
                    for (int t = event_start[subdivisions]; t <= event_end[subdivisions]; t++)
                    {
                        if (t == event_start[subdivisions])
                        {
                            for (int weather = 0; weather < WeatherTypes; weather++)
                            {
                                TreeNode tn = new TreeNode(t - 1,
                                                           entry_node,
                                                           MC_prob[(int)entry_node.Value[0], weather] * entry_node.Probability);

                                tn.Value = new float[1];
                                tn.Value[0] = weather;

                                tn.Attributes = new string[1];
                                tn.Attributes[0] = ((WTypes)tn.Value[0]).ToString();

                                Add(tn);
                                if (t == event_end[subdivisions])
                                    new_entry_nodes.Add(tn);
                            }

                        }
                        else
                        {
                            for (int weather = 0; weather < WeatherTypes; weather++)
                            {
                                predecessor = this[Count - WeatherTypes];

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

                                if (t == event_end[subdivisions])
                                    new_entry_nodes.Add(tn);
                            }
                        }
                    } //# end next t

                    //for(int k=0;k<WeatherTypes;k++)
                    //   new_entry_nodes.Add(this[Count-1-k]);

                } //# next entry node

                entry_nodes.Clear();
                entry_nodes.AddRange(new_entry_nodes);
            } //# end for subdivisions

            return;
        }
    }
}
