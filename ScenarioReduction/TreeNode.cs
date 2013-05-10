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
    [Serializable]
    public class TreeNode
    {
        #region Serialized members

        /// <summary>
        /// Serializable version of <see cref="Predecessor"/>.
        /// </summary>
        public int PredecessorId;

        /// <summary>
        /// Node time-step.
        /// </summary>
        public int Period;

        /// <summary>
        /// Unconditional probability of the node.
        /// </summary>
        public double Probability;

        /// <summary>
        /// Value of the uncertainty in the node.
        /// </summary>
        public float[] Value;

        /// <summary>
        /// Vector of integer attributes.
        /// </summary>
        public string[] Attributes;

        /// <summary>
        /// Used when exporting. Id of this node, used to be referenced
        /// by <see cref="PredecessorId"/>.
        /// </summary>
        public int Id;

        #endregion Serialized members

        /// <summary>
        /// Reference to the predecessor.
        /// </summary>
        [NonSerialized]
        public TreeNode Predecessor; // reference to predecessor

        /// <summary>
        /// Used for merging operations.
        /// </summary>
        [NonSerialized]
        public List<TreeNode> ComesFromList;

        public int DecoratedPeriod(ScenarioTree tree)
        {
            return this.Period + tree.OneBasedPeriods;
        }

        public TreeNode Clone()
        {
            TreeNode tn = new TreeNode(this.Period, null, this.Probability);
            tn.Value = (float[])this.Value.Clone();
            if (this.Attributes != null)
                tn.Attributes = (string[])this.Attributes.Clone();
            tn.Id = this.Id;
            return tn;
        }

        public TreeNode()
        {
        }

        public TreeNode(int period, TreeNode predecessor, double probability)
        {
            this.Period = period;
            this.Predecessor = predecessor;
            this.Probability = probability;
        }

        public void ComesFrom(TreeNode n1, TreeNode n2)
        {
            this.ComesFromList = new List<TreeNode>();
            this.ComesFromList.Add(n1);
            this.ComesFromList.Add(n2);
        }

        public bool HaveEqualValues(TreeNode other)
        {
            // Check dimensions.
            if (this.Value.Length != other.Value.Length)
            {
                return false;
            }

            for (int d = 0; d < this.Value.Length; d++)
            {
                if (this.Value[d] != other.Value[d])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a new univariate node.
        /// </summary>
        /// <param name="period"></param>
        /// <param name="predecessor"></param>
        /// <param name="probability"></param>
        /// <param name="value"></param>
        public TreeNode(int period, TreeNode predecessor, double probability, double value)
            : this(period, predecessor, probability)
        {
            this.Value = new float[1];
            this.Value[0] = (float)value;
        }

        /// <summary>
        /// Creates a new multivariate node.
        /// </summary>
        /// <param name="period"></param>
        /// <param name="predecessor"></param>
        /// <param name="probability"></param>
        /// <param name="value"></param>
        public TreeNode(int period, TreeNode predecessor, double probability, double[] value)
            : this(period, predecessor, probability)
        {
            this.Value = new float[value.Length];
            for (int i = 0; i < value.Length; i++)
            {
                this.Value[i] = (float)value[i];
            }
        }
    }
}
