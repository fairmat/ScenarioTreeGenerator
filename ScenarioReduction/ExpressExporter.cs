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
using System.Linq;
using System.Text;

namespace ScenarioReduction
{
    /// <summary>
    /// Just write to Console....
    /// </summary>
    public interface IScenarioTreeExporter
    {
        void Export(ScenarioTree tree);
    }

    public class ExpressExporter : IScenarioTreeExporter
    {
        #region IScenarioTreeExporter Members

        public void Export(ScenarioTree tree)
        {
            // Number of scenarios.
            int L = tree.Leafs.Count;

            // Number of stages.
            int S = 1 + tree.T;

            // Prints a string like: stage: [1 2 2 3 3 3 3 4 4 4 4 4 4 4 4].
            Console.Write("stage: [");
            for (int n = 0; n < tree.Count; n++)
            {
                Console.Write(tree[n].Period + 1 + "\t");
            }

            Console.WriteLine("]");

            // Number of stages.
            Console.WriteLine("numTime: " + S);

            // Number of scenarios.
            Console.WriteLine("numScenario: " + tree.Leafs.Count);
            Console.WriteLine("numNode: " + tree.Count);

            int idx = -1;
            for (int z = 0; z < tree.Count; z++)
            {
                // Periods starts form zero.
                if (tree[z].Period == S - 2)
                {
                    idx = z;
                }
            }

            // Index of last node of the second last stage.
            Console.WriteLine("secondlaststagenode: " + (idx + 1));

            // Checks if it's correct!
            if (tree[idx + 1].Period != S - 1)
            {
                throw new Exception("Failed to calculate secondlaststagenode");
            }

            // Scenario node variable.
            TreeNode[][] sn = tree.ScenariosNodes;
            Console.Write("ScenarioNode: [");
            for (int l = 0; l < L; l++)
            {
                for (int s = 0; s < S; s++)
                    Console.Write(sn[l][s].Id + "\t");
                if (l < L - 1)
                    Console.WriteLine();
                else
                    Console.WriteLine("]");
            }

            // ***************************
            // Generate Probability: stages by row
            // Probability: [1 0.5 0.5].
            Console.Write("Probability: [");
            for (int s = 0; s < S; s++)
            {
                List<TreeNode> nodes = tree.NodesAt(s);
                foreach (TreeNode n in nodes)
                    Console.Write(n.Probability + "\t");

                if (s < S - 1)
                    Console.WriteLine();
                else
                    Console.WriteLine("]");
            }

            // Descendant...Matrix
            // for every node (rows) it calculates the start and the end of the
            // descendant nodes for every subsequent period.
            Console.WriteLine("successor: [");
            for (int n = 0; n < tree.Count; n++)
            {
                if (tree[n].Period < S - 1)
                {
                    for (int s = 1; s < S; s++)
                    {
                        List<TreeNode> successors = tree.GetSuccessors(tree[n], s);
                        if (successors.Count > 0)
                        {
                            Console.Write(successors[0].Id + "\t" +
                                          successors[successors.Count - 1].Id + "\t");
                        }
                        else
                        {
                            Console.Write("0\t0\t");
                        }
                    }

                    Console.WriteLine();
                }
            }

            Console.WriteLine("]");

            // Merge component names that contain _P _B in the name.
            List<int[]> indices = new List<int[]>();

            // Already taken.
            bool[] taken = new bool[tree.componentNames.Count];

            // Iterate for every component.
            for (int d = 0; d < tree.componentNames.Count; d++)
                if (!taken[d])
                {
                    List<int> sub = new List<int>();
                    sub.Add(d);
                    taken[d] = true;
                    if (tree.componentNames[d].Contains("_B"))
                    {
                        string name = tree.componentNames[d].Replace("_B", "_P");
                        for (int d1 = d + 1; d1 < tree.componentNames.Count; d1++)
                        {
                            if (name.Equals(tree.componentNames[d1],
                                            StringComparison.InvariantCultureIgnoreCase))
                            {
                                sub.Add(d1);
                                taken[d1] = true;
                                break;
                            }
                        }
                    }

                    indices.Add(sub.ToArray());
                }

            // Value Variables.
            for (int z = 0; z < indices.Count; z++)
            {
                int[] sub = indices[z];
                int d = sub[0];
                string name = "Component" + (z + 1);
                if (tree.componentNames != null)
                    if (d < tree.componentNames.Count)
                    {
                        name = tree.componentNames[d];
                        if (sub.Length > 1)
                            name = name.Replace("_B", string.Empty);
                    }

                Console.Write(name + ": [");
                for (int s = 0; s < S; s++)
                {
                    List<TreeNode> nodes = tree.NodesAt(s);
                    foreach (TreeNode n in nodes)
                    {
                        for (int z1 = 0; z1 < sub.Length; z1++)
                        {
                            Console.Write(n.Value[sub[z1]] + "\t");
                        }
                    }

                    if (s < S - 1)
                        Console.WriteLine();
                    else
                        Console.WriteLine("]");
                }
            }

            return;
        }

        #endregion
    }
}
