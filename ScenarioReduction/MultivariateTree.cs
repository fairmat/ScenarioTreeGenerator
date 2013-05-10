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
using DVPLDOM;
using DVPLI;

namespace ScenarioReduction
{
    /// <summary>
    ///
    /// </summary>
    [Serializable]
    public class MultivariateTree : ScenarioTree
    {
        private ProjectROV prj;

        //not really used
        private int approximationType = 0;
        private Matrix VarCov;
        private bool[] isLog;
        private static Random rng;
        public int S = 10;//number of stages;
        private double ProbTreshold = .5;// the higher it is the more scenario there will be

        public int randomizedBranchingStartPeriod = 2;
        //additional parameters
        public double[] UpperBound = null;

        private NormalGenerator R;

        private int D;//number of stochastic componenets
        private int F;//number of derived components; //eg recurrence functions
        private Matrix A;// cholesky decomposition...
        private List<Matrix> Z;// realizations of the IID normal variables

        private double[] simulationDates;
        private List<RFunction> rfunctions;
        private int[] rfunctionsExpId;//update expressions....
        private int[] rfunctionR0Id;

        static MultivariateTree()
        {
            rng = new Random();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="p_VarCov">input Covariance Matrix</param>
        /// <param name="p_kappa"> input mean reverting coefficient</param>
        /// <param name="p_mean_series">Historical Mean vector</param>
        /// <param name="p_log">true if the component is a log mean reverting</param>
        /// <param name="p_deltat">vector of deltaT</param>
        /// <param name="p_ProbTreshold">p in the report</param>
        /// <param name="p_AUM">let to false</param>
        /// <param name="p_MCR">number of children when montecarlo (Z in the Report)</param>
        /// <param name="p_seed"></param>
        public MultivariateTree(ProjectROV project)
        {
            this.prj = project;
        }

        private void Init()
        {
            this.prj.CreateSymbols();
            this.prj.Initialize();
            this.prj.CreateSymbols();

            //calculate simulation dates and delta t
            double T = this.prj.GetTotalTime();

            double dt = T / this.S;
            this.simulationDates = new double[this.S];
            this.deltaT = new double[this.S];
            for (int i = 0; i < this.S; i++)
            {
                this.simulationDates[i] = i * dt;
                this.deltaT[i] = dt;
            }

            this.isLog = new bool[this.prj.Processes.Count];
            for (int u = 0; u < this.prj.Processes.Count; u++)
            {
                StocasticProcess s = this.prj.Processes[u];

                this.isLog[u] = s.IsLog()[0];//todo: fix for multivariate
                if (s is StochasticProcessExtendible)
                    (s as StochasticProcessExtendible).process.Setup(this.simulationDates);
            }

            this.prj.Processes.GetCorrelationMatrix();
            SolverBase.MatrixTransformations.CalculateVolatilityMatrix(0, this.prj.Processes, out this.VarCov);

            this.D = this.VarCov.R; //Number of components

            switch (this.D)
            {
                case 1:
                    this.ProbTreshold = .5;
                    break;
                case 2:
                    this.ProbTreshold = .4;
                    break;
                case 3:
                    this.ProbTreshold = .3;
                    break;
                case 4:
                    this.ProbTreshold = .2;
                    break;

                default:
                    this.ProbTreshold = .1;
                    break;
            }

            this.rfunctions = this.prj.Symbols.GetRFunctions();
            this.F = this.rfunctions.Count;

            this.rfunctionR0Id = new int[this.rfunctions.Count];
            this.rfunctionsExpId = new int[this.rfunctions.Count];
            for (int f = 0; f < this.F; f++)
            {
                this.rfunctionR0Id[f] = Engine.Parser.Parse(this.rfunctions[f].m_R0);
                this.rfunctionsExpId[f] = Engine.Parser.Parse(this.rfunctions[f].m_UpdateExpression);
            }
            //Get Names
            this.componentNames = new List<string>();
            for (int d = 0; d < this.D; d++)
                this.componentNames.Add(this.prj.Processes[d].description);
            for (int f = 0; f < this.F; f++)
                this.componentNames.Add(this.rfunctions[f].VarName);
        }

        private unsafe void SetVariableValues(TreeNode node)
        {
            this.prj.SetReservedSymbolsValue(node.Period, this.simulationDates[node.Period]);
            for (int d = 0; d < this.D; d++)
                Engine.Parser.SetVariableValue(this.prj.TotalProcesses.VariableV[d], node.Value[d]);
        }

        public override void Generate()
        {
            Init();

            //find the choleksy decomposition A of VarCov

            //This part can be done only once...

            this.A = this.VarCov.Cholesky();
            //A= cd.GetL();
            this.Z = IIDOutcomes();

            //Generates the root...
            TreeNode Root = new TreeNode();
            Root.Value = new float[this.D + this.F];
            for (int j = 0; j < this.D; j++)
                Root.Value[j] = (float)this.prj.Processes[j].InitialValueForSimulation();

            SetVariableValues(Root);
            for (int j = 0; j < this.F; j++)
                Root.Value[this.D + j] = (float)Engine.Parser.Evaluate(this.rfunctionR0Id[j]);

            Root.Period = 0;
            Root.Probability = 1;
            Add(Root);  //add the root to the tree

            List<TreeNode> EntryNodes = new List<TreeNode>();
            EntryNodes.Add(Root);

            //Ad ogni stage fino al penultimo genera tutti i nodi figli...
            //From Stage 2 to Stage I-1
            for (int i = 1; i < deltaT.Length; i++)
            {
                List<TreeNode> ChildNodes = new List<TreeNode>();
                foreach (TreeNode tn in EntryNodes)
                    ChildNodes.AddRange(GenerateChildNodes(tn));

                // add all those nodes to the tree...
                AddRange(ChildNodes);

                //now the nodes at stage i will became the new entry nodes.
                EntryNodes = ChildNodes;
            }
            AssignIds();

            return;
        }

        public int MonteCarloRealizations = 2; //Z in the report
        public bool AlwaysUseMontecarlo = false;

        private unsafe List<TreeNode> GenerateChildNodes(TreeNode entryNode)
        {
            List<TreeNode> Outcomes = new List<TreeNode>();

            double delta_t = deltaT[entryNode.Period];
            double r_delta_t = Math.Sqrt(delta_t);

            //otherwise Z is already calculated
            if (entryNode.Period >= this.randomizedBranchingStartPeriod)
            {
                if (rng.NextDouble() >= this.ProbTreshold)
                {
                    //return a zero idd outcome
                    this.Z = new List<Matrix>();
                    Matrix o = new Matrix(this.D, 1);//considers only  the stochastic components
                    this.Z.Add(o);
                }
                else
                    this.Z = IIDOutcomesBySimulation(this.D, MonteCarloRealizations);//considers only  the stochastic components
            }

            //iterates through the outcomes
            for (int s = 0; s < this.Z.Count; s++)
            {
                //Transform...
                Matrix epsilon = this.A * this.Z[s];

                TreeNode outcome = new TreeNode();
                outcome.Value = new float[this.D + this.F];
                outcome.Probability = entryNode.Probability / this.Z.Count;
                outcome.Period = entryNode.Period + 1;
                outcome.Predecessor = entryNode;

                //Double precision version
                Vector X = new Vector(this.D);
                for (int j = 0; j < this.D; j++)
                    X[j] = entryNode.Value[j];

                int i = entryNode.Period + 1;

                //iterates through the components of vector X
                for (int j = 0; j < this.D; j++)
                {
                    StocasticProcess sp = this.prj.Processes[j];

                    if (this.approximationType == 0)
                    {
                        if (!this.isLog[j])
                            outcome.Value[j] = entryNode.Value[j] + (float)(delta_t * sp.a(i, X.Buffer) + r_delta_t * epsilon[j, 0]);
                        else
                            outcome.Value[j] = entryNode.Value[j] * (float)Math.Exp(delta_t * sp.a(i, X.Buffer)
                                                                    + r_delta_t * epsilon[j, 0] - 0.5 * delta_t * Math.Pow(this.A[j, j], 2));
                    }
                }

                //calculates the remaining components
                SetVariableValues(outcome);
                for (int j = 0; j < this.F; j++)
                    outcome.Value[this.D + j] = (float)Engine.Parser.Evaluate(this.rfunctionsExpId[j]);

                Outcomes.Add(outcome);
            }

            return Outcomes;
        }

        /// <summary>
        /// Get the outcomes for the tree fitting
        /// discretization (using 2^D values) of the normal multivariate distribution
        /// </summary>
        /// <returns></returns>
        private List<Matrix> IIDOutcomes()
        {
            List<Matrix> Res = new List<Matrix>();
            double[] outcomes = { -1, 1 };
            //Now the low dimensionals case... then
            //it can be extended to a more general case with the multidimensional
            //iterator ()

            switch (this.D)
            {
                case 1:
                    {
                        for (int z1 = 0; z1 < 2; z1++)
                        {
                            Matrix GM = new Matrix(this.D, 1);
                            GM[0, 0] = outcomes[z1];
                            Res.Add(GM);
                        }
                    }
                    break;

                case 2:
                    for (int z1 = 0; z1 < 2; z1++)
                        for (int z2 = 0; z2 < 2; z2++)
                        {
                            Matrix GM = new Matrix(this.D, 1);
                            GM[0, 0] = outcomes[z1];
                            GM[1, 0] = outcomes[z2];
                            Res.Add(GM);
                        }
                    break;

                case 3:
                    for (int z1 = 0; z1 < 2; z1++)
                        for (int z2 = 0; z2 < 2; z2++)
                            for (int z3 = 0; z3 < 2; z3++)
                            {
                                Matrix GM = new Matrix(this.D, 1);
                                GM[0, 0] = outcomes[z1];
                                GM[1, 0] = outcomes[z2];
                                GM[2, 0] = outcomes[z3];
                                Res.Add(GM);
                            }
                    break;

                case 4:
                    for (int z1 = 0; z1 < 2; z1++)
                        for (int z2 = 0; z2 < 2; z2++)
                            for (int z3 = 0; z3 < 2; z3++)
                                for (int z4 = 0; z4 < 2; z4++)
                                {
                                    Matrix GM = new Matrix(this.D, 1);
                                    GM[0, 0] = outcomes[z1];
                                    GM[1, 0] = outcomes[z2];
                                    GM[2, 0] = outcomes[z3];
                                    GM[3, 0] = outcomes[z4];
                                    Res.Add(GM);
                                }
                    break;
                case 5:
                    for (int z1 = 0; z1 < 2; z1++)
                        for (int z2 = 0; z2 < 2; z2++)
                            for (int z3 = 0; z3 < 2; z3++)
                                for (int z4 = 0; z4 < 2; z4++)
                                    for (int z5 = 0; z5 < 2; z5++)
                                    {
                                        Matrix GM = new Matrix(this.D, 1);
                                        GM[0, 0] = outcomes[z1];
                                        GM[1, 0] = outcomes[z2];
                                        GM[2, 0] = outcomes[z3];
                                        GM[3, 0] = outcomes[z4];
                                        GM[4, 0] = outcomes[z5];
                                        Res.Add(GM);
                                    }
                    break;
                default:
                    throw new Exception("IIDOutcomes!");
            }

            return Res;
        }

        private List<Matrix> IIDOutcomesBySimulation(int d, int n)
        {
            if (n % 2 != 0) throw new Exception("to use adjusted- random sampling use multiple of 2");
            if (this.R == null) this.R = new NormalGenerator(0);

            List<Matrix> Res = new List<Matrix>();

            double[] outcomes = new double[n];
            for (int i = 0; i < n / 2; i++)
            {
                outcomes[i] = this.R.Next();
                outcomes[i + n / 2] = -outcomes[i];
            }

            for (int i = 0; i < n; i++)
            {
                Matrix o = new Matrix(d, 1);
                for (int c = 0; c < d; c++)
                    o[c, 0] = outcomes[i];
                Res.Add(o);
            }

            return Res;
        }
    }
}
