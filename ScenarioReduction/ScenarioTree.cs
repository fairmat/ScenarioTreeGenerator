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
using System.Text;

namespace ScenarioReduction
{
    [Serializable]
    public class ScenarioTree : List<TreeNode>
    {
        //not mandatory
        public List<string> componentNames;

        /// <summary>
        /// Whathever the periods start from zero or from one.
        /// </summary>
        public int OneBasedPeriods = 0;

        /// <summary>
        /// Information about interval length, must be calculated.
        /// </summary>
        public double[] deltaT; //information about... interval length must be calculated

        string ComponentName(int c)
        {
            if (componentNames != null)
                return componentNames[c];
            else
                return "C" + c.ToString();
        }

        public virtual void Generate()
        {
            throw new Exception("Implement it!");
        }

        /// <summary>
        /// Scenario,Period,Component
        /// </summary>
        public float[][][] Scenarios
        {
            get
            {
                List<TreeNode> leafs = Leafs;
                int T = leafs[0].Period + 1;

                float[][][] trajectories = new float[leafs.Count][][];
                for (int s = 0; s < leafs.Count; s++)
                {
                    trajectories[s] = new float[T][];

                    TreeNode tn = leafs[s];
                    do
                    {
                        trajectories[s][tn.Period] = tn.Value;
                        tn = tn.Predecessor;
                    }
                    while (tn != null);
                }

                return trajectories;
            }
        }

        /// <summary>
        /// Gets the list of nodes as a matrix the first 
        /// component is the scenario, the second is the stage.
        /// </summary>
        public TreeNode[][] ScenariosNodes
        {
            get
            {
                List<TreeNode> leafs = Leafs;

                int T = leafs[0].Period + 1;

                TreeNode[][] trajectories = new TreeNode[leafs.Count][];
                for (int s = 0; s < leafs.Count; s++)
                {
                    trajectories[s] = new TreeNode[T];

                    TreeNode tn = leafs[s];
                    do
                    {
                        trajectories[s][tn.Period] = tn;
                        tn = tn.Predecessor;
                    }
                    while (tn != null);
                }

                return trajectories;
            }
        }

        public double[] ScenariosProbabilities
        {
            get
            {
                List<TreeNode> leafs = Leafs;
                double[] probabilities = new double[leafs.Count];
                for (int s = 0; s < leafs.Count; s++)
                    probabilities[s] = leafs[s].Probability;

                return probabilities;
            }
        }

        /// <summary>
        /// Calculate succesors at a given period set for a ginve node
        /// </summary>
        /// <param name="n"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        public List<TreeNode> GetSuccessors(TreeNode n, int s)
        {
            List<TreeNode> S = new List<TreeNode>();
            for (int n1 = 0; n1 < Count; n1++)
            {
                if (this[n1].Period == s)
                {
                    TreeNode pred = this[n1].Predecessor;

                    // Go toward the root and see if finds n.
                    do
                    {
                        if (pred == n)
                        {
                            S.Add(this[n1]);
                            break;
                        }

                        if (pred == null) break;
                        pred = pred.Predecessor;
                    }
                    while (true);
                }
            }

            return S;
        }

        public int FindPredecessorIndex(int z)
        {
            return this.IndexOf(this[z].Predecessor);
        }

        public ScenarioTree Clone()
        {
            ScenarioTree cloned = new ScenarioTree();
            for (int z = 0; z < Count; z++)
            {
                cloned.Add(this[z]);
                if (this[z].Predecessor != null)
                {
                    cloned[z].Predecessor = cloned[this.FindPredecessorIndex(z)];
                }
            }

            cloned.deltaT = (double[])deltaT.Clone();
            return cloned;
        }

        public ScenarioDescriptiveStatistics DescriptiveStatistics(TextWriter tw)
        {
            //Calculate the mean at each interval...
            int TT = T + 1;
            int D = NodesAt(0)[0].Value.Length;
            double[,] M = new double[TT, D];
            double[,] S = new double[TT, D];

            for (int i = 0; i < TT; i++)
            {
                List<TreeNode> nodes = NodesAt(i);
                double[] mean = Mean(nodes);
                double[] std = Std(nodes);

                for (int d = 0; d < D; d++)
                {
                    M[i, d] = mean[d];
                    S[i, d] = std[d];
                }
            }

            tw.WriteLine("Mean:");
            for (int d = 0; d < D; d++)
            {
                for (int i = 0; i < TT; i++)
                    tw.Write(M[i, d].ToString() + "\t");

                tw.WriteLine();
            }

            tw.WriteLine("Std:");
            for (int d = 0; d < D; d++)
            {
                for (int i = 0; i < TT; i++)
                    tw.Write(S[i, d].ToString() + "\t");

                tw.WriteLine();
            }

            ScenarioDescriptiveStatistics sds = new ScenarioDescriptiveStatistics();
            sds.means = M;
            sds.variances = S;
            return sds;
        }

        /// <summary>
        /// Component wise mean of a set of nodes.
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        double[] Mean(List<TreeNode> nodes)
        {
            int D = nodes[0].Value.Length;
            double[] mean = new double[D];
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int d = 0; d < D; d++)
                {
                    mean[d] += nodes[i].Probability * nodes[i].Value[d];
                }
            }

            return mean;
        }

        /// <summary>
        /// Vector of standard deviations
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        double[] Std(List<TreeNode> nodes)
        {
            int D = nodes[0].Value.Length;
            double[] mean = new double[D];
            double[] sigma2 = new double[D];

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int d = 0; d < D; d++)
                    mean[d] += nodes[i].Probability * nodes[i].Value[d];
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int d = 0; d < D; d++)
                    sigma2[d] += nodes[i].Probability * Math.Pow(nodes[i].Value[d] - mean[d], 2);
            }

            for (int d = 0; d < D; d++)
                sigma2[d] = Math.Sqrt(sigma2[d]);

            return sigma2;
        }

        public List<TreeNode> Leafs
        {
            get
            {
                int max_Period = int.MinValue;

                //(1) get the max period
                for (int n = 0; n < Count; n++)
                    if (this[n].Period > max_Period) max_Period = this[n].Period;

                //(2) build the set of nodes with max period...
                List<TreeNode> leafs = new List<TreeNode>();
                for (int n = 0; n < Count; n++)
                    if (this[n].Period == max_Period) leafs.Add(this[n]);

                return leafs;
            }
        }

        public TreeNode Root
        {
            get
            {
                for (int n = 0; n < Count; n++)
                    if (this[n].Predecessor == null)
                        return this[n];

                return null;
            }
        }

        protected internal void AssignIds()
        {
            //find the root
            TreeNode root = Root;
            int id = 1;
            root.Id = id++;

            for (int i = 0; i < Count; i++)
            {
                if (this[i] != root)
                    this[i].Id = id++;
            }
        }

        public bool CheckProbabilities()
        {
            int Ts = this.T;
            double[] p = new double[Ts + 1];
            for (int n = 0; n < Count; n++)
            {
                p[this[n].Period] += this[n].Probability;
            }

            for (int i = 0; i <= Ts; i++)
            {
                if (p[i] > 1.0000001)
                {
                    return false;
                }
            }

            return true;
        }

        public string TXT_Export(int component)
        {
            StringBuilder sb = new StringBuilder();
            float[][][] sc = this.Scenarios;
            for (int t = 0; t < sc[0].Length; t++)
            {
                for (int s = 0; s < sc.Length; s++)
                {
                    sb.Append(sc[s][t][component].ToString() + "\t");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        public string AMPL_Export(string prefix0, string prefix, bool[] use_attribute)
        {
            int Dimensions = this[0].Value.Length;
            StringBuilder sb = new StringBuilder();
            AssignIds();
            sb.Append("param:" + prefix0 + "NODES:\t");
            sb.Append(prefix + "prob\t");
            sb.Append(prefix + "t\t");
            sb.Append(prefix + "prev\t");

            for (int z = 0; z < Dimensions; z++)
                sb.Append(prefix + ComponentName(z) + "\t");

            sb.AppendLine(":=");

            for (int n = 0; n < Count; n++)
            {
                if (this[n].Probability > 1)
                {
                    Console.WriteLine("!");
                }

                sb.Append(this[n].Id + "\t");
                sb.Append(this[n].Probability + "\t");
                sb.Append(this[n].DecoratedPeriod(this) + "\t");

                if (this[n].Predecessor != null)
                    sb.Append(this[n].Predecessor.Id + "\t");
                else
                    sb.Append(this[n].Id + "\t");

                if (use_attribute != null)
                {
                    int attrib_count = 0;
                    for (int z = 0; z < Dimensions; z++)
                        if (use_attribute[z] == false)
                            sb.Append(this[n].Value[z] + "\t");
                        else
                            sb.Append(this[n].Attributes[attrib_count++] + "\t");
                }
                else
                {
                    // Only real values are here.
                    for (int z = 0; z < Dimensions; z++)
                    {
                        sb.Append(this[n].Value[z] + "\t");
                    }
                }

                sb.AppendLine();
            }

            sb.AppendLine(";");
            return sb.ToString();
        }

        public static void AppendDefaulTrajectory(string onetrajectory_filename, string reduced_filename, int index_increment)
        {
            ScenarioTree onetrajectory_st = ScenarioTree.AMPL_Import(onetrajectory_filename);
            ScenarioTree reduced = ScenarioTree.AMPL_Import(onetrajectory_filename);
            AppendDefaulTrajectory(onetrajectory_st, reduced, reduced_filename, index_increment);
        }

        //index_increment when in memory indices start from 0 so must be incremented
        public static void AppendDefaulTrajectory(ScenarioTree onetrajectory_scenario_tree, ScenarioTree reduced, string reduced_filename, int index_increment)
        {
            // Now find the nodes belonging to the first in the second.
            int T = onetrajectory_scenario_tree.Count;

            int[] default_trajectory = new int[T];

            for (int i = 0; i < T; i++)
            {
                bool found = false;
                TreeNode N_i = onetrajectory_scenario_tree[i];
                for (int j = 0; j < reduced.Count; j++)
                {
                    TreeNode N_j = reduced[j];
                    if (N_i.Period == N_j.Period)
                    {
                        if (N_j.HaveEqualValues(N_j))
                        {
                            // The two nodes reference the same realization of the underlying.
                            default_trajectory[i] = N_j.Id;
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    Console.WriteLine("Cannot match node");
                    return;
                }
            }

            // Now write in the output the information about the default trajectory.
            StreamWriter sw = File.AppendText(reduced_filename);
            sw.WriteLine("param DefaultTrj:=");
            for (int i = 0; i < T; i++)
            {
                int period = onetrajectory_scenario_tree[i].Period + index_increment;
                sw.WriteLine(period.ToString() + "\t" + default_trajectory[i].ToString());
            }

            sw.WriteLine(";");
            sw.Close();
        }

        static string[] Reshape(string[] a, int start, int length)
        {
            string[] res = new string[length];
            for (int i = 0; i < length; i++)
                res[i] = a[start + i];
            return res;
        }

        public static ScenarioTree AMPL_Import(string file_name)
        {
            ScenarioTree St = new ScenarioTree();
            StreamReader freader = File.OpenText(file_name);

            string first_line = freader.ReadLine();

            do
            {
                string line = freader.ReadLine();
                string[] parts = line.Split('\t');

                if (parts[parts.Length - 1] == string.Empty)
                {
                    parts = Reshape(parts, 0, parts.Length - 1);
                }

                if (parts.Length > 6)
                {
                    TreeNode Tn = new TreeNode();

                    Tn.Id = int.Parse(parts[0]);
                    Tn.Probability = double.Parse(parts[1]);
                    Tn.Period = int.Parse(parts[2]);

                    if (parts[3] != string.Empty)
                        Tn.PredecessorId = int.Parse(parts[3]);
                    else
                        Tn.PredecessorId = -1;

                    Tn.Value = new float[parts.Length - 4];
                    for (int c = 0; c < parts.Length - 4; c++)
                        Tn.Value[c] = float.Parse(parts[4 + c]);

                    St.Add(Tn);
                }
            }
            while (!freader.EndOfStream);

            St.ConvertPredecessorIdsToReferences();

            // Create a deltaT vector.
            St.deltaT = new double[St.T];
            for (int i = 0; i < St.deltaT.Length; i++)
            {
                St.deltaT[i] = 1;
            }

            freader.Close();
            return St;
        }

        void ConvertPredecessorIdsToReferences()
        {
            // here we should set the predecessors
            foreach (TreeNode tn in this)
            {
                foreach (TreeNode pre in this)
                {
                    if (tn.PredecessorId != -1 && pre != tn)
                    {
                        if (pre.Id == tn.PredecessorId)
                        {
                            tn.Predecessor = pre;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the greater period id.
        /// </summary>
        public int T
        {
            get
            {
                int max_t = 0;
                for (int i = 0; i < Count; i++)
                    if (this[i].Period > max_t) max_t = this[i].Period;
                return max_t;
            }
        }

        /// <summary>
        /// Returns all the nodes of a give period
        /// </summary>
        /// <param name="period"></param>
        /// <returns></returns>
        public List<TreeNode> NodesAt(int period)
        {
            List<TreeNode> L = new List<TreeNode>();
            for (int i = 0; i < Count; i++)
            {
                if (this[i].Period == period)
                {
                    L.Add(this[i]);
                }
            }

            return L;
        }

        /// <summary>
        /// Return a scenario tree formed by only the component selected...
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public ScenarioTree GetComponent(int d)
        {
            ScenarioTree ST = new ScenarioTree();

            return null;
        }

        /// <summary>
        /// Merge the Value and Attributes fields of n1 and n2 on new_node
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <param name="new_node"></param>
        static void MergeValues(TreeNode n1, TreeNode n2, ref TreeNode new_node)
        {
            int D_S = 0;
            if (n1.Value != null) D_S += n1.Value.Length;
            if (n2.Value != null) D_S += n2.Value.Length;

            int A_S = 0;
            if (n1.Attributes != null) A_S += n1.Attributes.Length;
            if (n2.Attributes != null) A_S += n2.Attributes.Length;

            new_node.Value = new float[D_S];
            n1.Value.CopyTo(new_node.Value, 0);
            n2.Value.CopyTo(new_node.Value, n1.Value.Length);

            if (A_S > 0)
            {
                new_node.Attributes = new string[A_S];

                int start = 0;
                if (n1.Attributes != null)
                {
                    n1.Attributes.CopyTo(new_node.Attributes, 0);
                    start = n1.Attributes.Length;
                }

                if (n2.Attributes != null)
                    n2.Attributes.CopyTo(new_node.Attributes, start);
            }
        }

        /// <summary>
        /// Returns the predecessor node (The result of the two predecessors).
        /// </summary>
        /// <param name="sub_tree"></param>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <returns></returns>
        static TreeNode ComeFromListSearch(List<TreeNode> sub_tree, TreeNode n1, TreeNode n2)
        {
            for (int i = 0; i < sub_tree.Count; i++)
                if (sub_tree[i].ComesFromList.IndexOf(n1) >= 0 &&
                     sub_tree[i].ComesFromList.IndexOf(n2) >= 0)
                    return sub_tree[i];
            return null;
        }

        TreeNode ComeFromListSearch(TreeNode n1, TreeNode n2)
        {
            return ComeFromListSearch(this, n1, n2);
        }

        /// <summary>
        /// Builds a new scenario tree by merging two scenarios.
        /// </summary>
        /// <param name="St1"></param>
        /// <param name="St2"></param>
        /// <returns></returns>
        public static ScenarioTree Merge(ScenarioTree St1, ScenarioTree St2)
        {
            ScenarioTree St = new ScenarioTree();

            St.deltaT = St1.deltaT;

            //get the maximum period index...
            int T = St1.T;

            for (int t = 0; t <= T; t++)
            {
                if (t == 0)
                {
                    TreeNode root1 = St1.Root;
                    TreeNode root2 = St2.Root;
                    TreeNode root = new TreeNode(t, null, 1.0);
                    MergeValues(root1, root2, ref root);
                    root.ComesFrom(root1, root2);

                    St.Add(root);
                }
                else
                {
                    List<TreeNode> L1 = St1.NodesAt(t);
                    List<TreeNode> L2 = St2.NodesAt(t);

                    List<TreeNode> Previous = St.NodesAt(t - 1);
                    foreach (TreeNode n1 in L1)
                    {
                        foreach (TreeNode n2 in L2)
                        {
                            TreeNode n = new TreeNode();
                            n.Period = t;
                            n.Probability = n1.Probability * n2.Probability;
                            MergeValues(n1, n2, ref n);

                            n.Predecessor = ScenarioTree.ComeFromListSearch(Previous, n1.Predecessor, n2.Predecessor);

                            St.Add(n);
                            n.ComesFrom(n1, n2);
                        }
                    }
                }
            }

            return St;
        }

        public override string ToString()
        {
            string str = "Nodes=" + Count + System.Environment.NewLine +
                         "Scenarios=" + ScenariosProbabilities.Length + 
                         Environment.NewLine + Environment.NewLine;

            int TT = T + 1;
            float[][][] sc = Scenarios;
            int D = sc[0][0].Length;
            int S = sc.Length;

            for (int s = 0; s < S; s++)
            {
                for (int d = 0; d < D; d++)
                {
                    for (int t = 0; t < TT; t++)
                    {
                        str += sc[s][t][d] + "\t";
                    }
                }

                str += Environment.NewLine;
            }

            return str;
        }

        /// <summary>
        /// Remove all the characters before ':='.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        static string CutIdentifier(string line)
        {
            int i = line.IndexOf(":=");
            if (i != -1)
            {
                return line.Substring(i + 2);
            }
            else
                throw new Exception("Missing :=");
        }

        /// <summary>
        /// Read a text file with the following structure
        ///
        /// Node:
        /// Id:= x1
        /// p := x1
        /// t := x1
        /// v := x1 x2 xn
        /// ParentId := c1
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static ScenarioTree FromFile(string file)
        {
            StreamReader sr = File.OpenText(file);
            string line;

            ScenarioTree tree = new ScenarioTree();
            TreeNode tn = null;
            do
            {
                line = sr.ReadLine();

                if (line.Contains("Node:"))
                {
                    // Add the previous.
                    if (tn != null)
                        tree.Add(tn);

                    tn = new TreeNode();
                }
                else if (line.Contains("ParentId"))
                {
                    line = CutIdentifier(line);
                    int predessor_id = int.Parse(line);
                    if (predessor_id > 0)
                    {
                        tn.Predecessor = tree[predessor_id - 1];
                    }
                }
                else if (line.Contains("Id"))
                {
                    line = CutIdentifier(line);
                    tn.Id = int.Parse(line);
                }
                else if (line.Contains("p"))
                {
                    line = CutIdentifier(line);
                    tn.Probability = double.Parse(line);
                }
                else if (line.Contains("t"))
                {
                    line = CutIdentifier(line);
                    tn.Period = int.Parse(line);
                }
                else if (line.Contains("v"))
                {
                    line = CutIdentifier(line);
                    string[] values = line.Split('\t');
                    tn.Value = new float[values.Length - 1];
                    for (int i = 0; i < values.Length - 1; i++)
                    {
                        tn.Value[i] = float.Parse(values[i]);
                    }
                }
            }
            while (!sr.EndOfStream);

            // Aadd the last one.
            if (tn != null)
                tree.Add(tn);

            int min_period = 1;

            // Check for the format of periods.
            for (int z = 0; z < tree.Count; z++)
            {
                if (tree[z].Period < min_period)
                {
                    min_period = tree[z].Period;
                }
            }

            if (min_period == 1)
            {
                tree.OneBasedPeriods = 1;

                for (int z = 0; z < tree.Count; z++)
                {
                    tree[z].Period--;
                }
            }
            else
            {
                tree.OneBasedPeriods = 0;
            }

            int Periods = tree.T + 1;
            tree.deltaT = new double[Periods];
            for (int t = 0; t < Periods; t++)
            {
                tree.deltaT[t] = 1;
            }

            return tree;
        }
    }
}
