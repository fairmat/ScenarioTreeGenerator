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
using DVPLI;
using DVPLDOM;
namespace ScenarioReduction
{



    /// <summary>
    ///
    /// </summary>
    [Serializable]
    public class MultivariateTree : ScenarioTree
    {
        ProjectROV prj;

        int approx_type = 0;
        Matrix VarCov;
        //double[] kappa;
        //double[,] mean_series;
        bool[] isLog;
        static Random RRR;
        public int S = 10;//number of stages;
        double ProbTreshold = .5;// the higher it is the more scenario there will be

        public int RandomizedBranchingStartPeriod = 2;
        //additional parameters
        public double[] UpperBound = null;


        static MultivariateTree()
        {
            RRR = new Random();
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
        public MultivariateTree(ProjectROV p_prj)
        {
            //AlwaysUseMontecarlo = false;// p_AUM;
            //MonteCarloRealizations = 2;// p_MCR;
            prj = p_prj;



            //kappa = (double[])p_kappa.Clone();
            //deltaT = (double[])p_deltat.Clone();
            //mean_series = (double[,])p_mean_series.Clone();
            //log = (bool[])p_log.Clone();
            //ProbTreshold = p_ProbTreshold;
        }
        double[] simulationDates;
        List<RFunction> rfunctions;
        int[] rfunctions_exp_id;//update expressions....
        int[] rfunctions_r0_id;
        void Init()
        {
            prj.CreateSymbols();
            prj.Initialize();
            prj.CreateSymbols();


            //calculate simulation dates and delta t
            double T = prj.GetTotalTime();

            double dt = T / S;
            simulationDates = new double[S];
            deltaT = new double[S];
            for (int i = 0; i < S; i++)
            {
                simulationDates[i] = i * dt;
                deltaT[i] = dt;
            }

            isLog = new bool[prj.Processes.Count];
            for (int u = 0; u < prj.Processes.Count; u++)
            {
                StocasticProcess s = prj.Processes[u];

                isLog[u] = s.IsLog()[0];//todo: fix for multivariate
                if (s is StochasticProcessExtendible)
                    (s as StochasticProcessExtendible).process.Setup(simulationDates);
            }

            prj.Processes.GetCorrelationMatrix();
            SolverBase.MatrixTransformations.CalculateVolatilityMatrix(0, prj.Processes, out VarCov);
            //VarCov = DotNetMatrixHelper.FromMatrix(p_VarCov);

            D = VarCov.R; //Number of components

            switch (D)
            {
                case 1:
                    ProbTreshold = .5;
                    break;
                case 2:
                    ProbTreshold = .4;
                    break;
                case 3:
                    ProbTreshold = .3;
                    break;
                case 4:
                    ProbTreshold = .2;
                    break;

                default:
                    ProbTreshold = .1;
                    break;
            }

            rfunctions = prj.Symbols.GetRFunctions();
            F = rfunctions.Count;

            rfunctions_r0_id = new int[rfunctions.Count];
            rfunctions_exp_id = new int[rfunctions.Count];
            for (int f = 0; f < F; f++)
            {
                rfunctions_r0_id[f] = Engine.Parser.Parse(rfunctions[f].m_R0);
                rfunctions_exp_id[f] = Engine.Parser.Parse(rfunctions[f].m_UpdateExpression);
            }
            //Get Names
            componentNames = new List<string>();
            for (int d = 0; d < D; d++)
                componentNames.Add(prj.Processes[d].description);
            for (int f = 0; f < F; f++)
                componentNames.Add(rfunctions[f].VarName);

        }


        unsafe void SetVariableValues(TreeNode node)
        {
            prj.SetReservedSymbolsValue(node.Period, simulationDates[node.Period]);
            for (int d = 0; d < D; d++)
                Engine.Parser.SetVariableValue(prj.TotalProcesses.VariableV[d], node.Value[d]);
        }

        int D;//number of stochastic componenets
        int F;//number of derived components; //eg recurrence functions
        Matrix A;// cholesky decomposition...
        List<Matrix> Z;// realizations of the IID normal variables

        public override void Generate()
        {
            Init();

            //find the choleksy decomposition A of VarCov

            //This part can be done only once...

            A = VarCov.Cholesky();
            //A= cd.GetL();
            Z = IIDOutcomes();

            //Generates the root...
            TreeNode Root = new TreeNode();
            Root.Value = new float[D + F];
            for (int j = 0; j < D; j++)
                Root.Value[j] = (float)prj.Processes[j].InitialValueForSimulation();

            SetVariableValues(Root);
            for (int j = 0; j < F; j++)
                Root.Value[D + j] = (float)Engine.Parser.Evaluate(rfunctions_r0_id[j]);


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

        unsafe List<TreeNode> GenerateChildNodes(TreeNode entry_node)
        {
            List<TreeNode> Outcomes = new List<TreeNode>();

            double delta_t = deltaT[entry_node.Period];
            double r_delta_t = Math.Sqrt(delta_t);

            //otherwise Z is already calculated
            if (entry_node.Period >= RandomizedBranchingStartPeriod)
            {


                if (RRR.NextDouble() >= ProbTreshold)
                {
                    //return a zero idd outcome
                    Z = new List<Matrix>();
                    Matrix o = new Matrix(D, 1);//considers only  the stochastic components
                    Z.Add(o);
                }
                else
                    Z = IIDOutcomesBySimulation(D, MonteCarloRealizations);//considers only  the stochastic components
            }



            //iterates through the outcomes
            for (int s = 0; s < Z.Count; s++)
            {
                //Transform...
                Matrix epsilon = A * Z[s];

                TreeNode outcome = new TreeNode();
                outcome.Value = new float[D + F];
                outcome.Probability = entry_node.Probability / Z.Count;
                outcome.Period = entry_node.Period + 1;
                outcome.Predecessor = entry_node;


                //Double precision version
                Vector X = new Vector(D);
                for (int j = 0; j < D; j++)
                    X[j] = entry_node.Value[j];

                int i = entry_node.Period + 1;

                //iterates through the components of vector X
                for (int j = 0; j < D; j++)
                {
                    StocasticProcess sp = prj.Processes[j];


                    if (approx_type == 0)
                    {
                        if (!isLog[j])
                            outcome.Value[j] = entry_node.Value[j] + (float)(delta_t * sp.a(i, X.Buffer) + r_delta_t * epsilon[j, 0]);
                        else
                            outcome.Value[j] = entry_node.Value[j] * (float)Math.Exp(delta_t * sp.a(i, X.Buffer)
                                                                    + r_delta_t * epsilon[j, 0] - 0.5 * delta_t * Math.Pow(A[j, j], 2));

                        /*
                        if (UpperBound != null)
                            if (!double.IsNegativeInfinity(UpperBound[j]))
                                if (outcome.Value[j] > UpperBound[j])
                                    outcome.Value[j] = (float)UpperBound[j];
                        */
                    }
                }

                //calculates the remaining components
                SetVariableValues(outcome);
                for (int j = 0; j < F; j++)
                    outcome.Value[D + j] = (float)Engine.Parser.Evaluate(rfunctions_exp_id[j]);

                //double tmp = outcome.Value[0];
                //else //approx type =1
                //    Value[j] = Math.Exp(-kappa[j] * delta_t) * entry_node.Value[j] + Sigma(j, delta_t);
                Outcomes.Add(outcome);
            }//next s
            return Outcomes;
        }


        /// <summary>
        /// Get the outcomes for the tree fitting
        /// discretization (using 2^D values) of the normal multivariate distribution
        /// </summary>
        /// <returns></returns>
        List<Matrix> IIDOutcomes()
        {
            List<Matrix> Res = new List<Matrix>();


            double[] outcomes = { -1, 1 };
            //Now the low dimensionals case... then
            //it can be extended to a more general case with the multidimensional
            //iterator ()

            switch (D)
            {
                case 1:
                    {
                        for (int z1 = 0; z1 < 2; z1++)
                        {
                            Matrix GM = new Matrix(D, 1);
                            GM[0, 0] = outcomes[z1];
                            Res.Add(GM);
                        }
                    }
                    break;

                case 2:
                    for (int z1 = 0; z1 < 2; z1++)
                        for (int z2 = 0; z2 < 2; z2++)
                        {
                            Matrix GM = new Matrix(D, 1);
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
                                Matrix GM = new Matrix(D, 1);
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
                                    Matrix GM = new Matrix(D, 1);
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
                                        Matrix GM = new Matrix(D, 1);
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
                    break;
            }//end switch


            return Res;
        }

        NormalGenerator R;
        List<Matrix> IIDOutcomesBySimulation(int d, int n)
        {
            if (n % 2 != 0) throw new Exception("to use adjusted- random sampling use multiple of 2");
            if (R == null) R = new NormalGenerator(0);

            List<Matrix> Res = new List<Matrix>();

            double[] outcomes = new double[n];
            for (int i = 0; i < n / 2; i++)
            {
                outcomes[i] = R.Next();
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
