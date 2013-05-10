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
    class Program
    {
        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;
            DateTime t0 = DateTime.Now;

            string mode = "A";// only AMPL

            if (args.Length < 2)
            {

                Console.WriteLine("ScenarioReduction V1.2.2\n");
                Console.WriteLine("Usage: ScenarioReduction infilename  action [out_scenarios]  [outputmode default=A] [Options]");
                //Console.WriteLine("mode = b (default) : extended  outputs");

                Console.WriteLine("action can be: rnd:reduce  or N: no operation (displays in output the original scenario) ");
                Console.WriteLine("output mode: C complete (descriptive statistics), A only AMPL output");
                Console.WriteLine("options can be:");
                Console.WriteLine("[Sn] standardize n=0,1 (use time specific std),2 (use avg std)");
                Console.WriteLine("[Mn] metric n=0: L2 ,n=1: fortet mourer (L2)");

                Console.WriteLine("Args count=", args.Length.ToString());
                return;
            }

            string infilename = args[0];

            string action = args[1].ToUpper();

            int out_scenenarios = -1;
            int optional_param_count = 0;

            if (action == "rnd")
            {
                out_scenenarios = int.Parse(args[2]);
                optional_param_count++;
            }
            if (args.Length > 2 + optional_param_count)
                mode = args[2 + optional_param_count].ToUpper();

            if (2 + optional_param_count < args.Length - 1)
            {
                Console.WriteLine("Options:");
                do
                {
                    optional_param_count++;
                    string op = args[2 + optional_param_count];
                    int val = int.Parse(op.Substring(1));
                    switch (op[0])
                    {
                        case 'S'://standardize
                            ScenarioReduction.VarianceStandardizationType = val;
                            if (mode == "C")
                                Console.WriteLine("Standardize=" + val);
                            break;
                        case 'M':// metric
                            ScenarioReduction.DistanceType = val;
                            if (mode == "C")
                                Console.WriteLine("Metric =" + val);
                            break;
                    }
                }
                while (2 + optional_param_count < args.Length - 1);
            }
            ScenarioReduction SRA = null;


            //ScenarioTree ST =GenerateOld();
            //ScenarioTree ST = NewModel();
            ScenarioTree ST = ScenarioTree.FromFile(infilename);


            if (mode == "C")
            {
                Console.WriteLine("Original Tree Descriptive Statistics:");
                ST.DescriptiveStatistics(Console.Out);

            }
            //Console.WriteLine("Merged Tree:");
            //Console.WriteLine(ST.ToString());


            //if(mode=="C")
            //    WriteTrajectories(ST.Scenarios, ST.ScenariosProbabilities,0);

            ScenarioTree reduced_tree = null;
            if (action == "rnd")
            {
                SRA = new BackwardReduction(ST);
                SRA.Reduce(out_scenenarios);
                reduced_tree = SRA.ReducedScenarioTree;
            }
            else
                reduced_tree = ST;





            if (mode == "C")
            {
                Console.WriteLine("Reduced Tree descriptive Statistics:");
                reduced_tree.DescriptiveStatistics(Console.Out);

                Console.WriteLine("Reduced Tree:");
                Console.WriteLine(reduced_tree.ToString());
                //Console.WriteLine("Reduced Scenarios");
                //Console.WriteLine("Component 1");
                //WriteTrajectories(SRA.ReducedScenarios, SRA.ReducedProbabilities, 0);
                //Console.WriteLine("Component 2");
                //WriteTrajectories(SRA.ReducedScenarios, SRA.ReducedProbabilities, 1);

                // From the reduced tree
                //for (int z = 0; z < reduced_tree.Scenarios[0][0].Length; z++)
                //{
                //   Console.WriteLine("RT Component " + z);
                //  WriteTrajectories(reduced_tree.Scenarios, reduced_tree.ScenariosProbabilities, z);
                //}
            }
            //string[] value_names={"X_avg", "X_dif", "weather"};


            //bool[] use_attribute ={false,false,true};
            Console.Write(reduced_tree.AMPL_Export("", "N_", null));// use_attribute));


            DateTime t1 = DateTime.Now;
            //Console.WriteLine("Running time=" + (t1 - t0).ToString());

        }


        static void WriteTrajectories(double[][][] trajectories, double[] probabilities, int component)
        {
            for (int s = 0; s < trajectories.Length; s++)
            {
                Console.Write(probabilities[s] + "\t");
                for (int t = 0; t < trajectories[0].Length; t++)
                {
                    Console.Write(trajectories[s][t][component] + "\t");
                }
                Console.WriteLine("");
            }
        }


        static List<string> component_names;
        static ScenarioTree NewModel()
        {
            component_names = new List<string>();

            //Components to be generated
            bool GeneratePriceLatentVariable = false;
            bool GenerateLatentDiffVariable = false;
            bool GenerateHydro = true;
            bool GenerateDemand = true;
            bool GenerateRenewable = true;

            //montly avg
            double[] avg_inflows ={3130107.4, 3089472.9,  2978591.8, 2877180.8,
                                2757422.4,  1432365.7,  578381.8,  336431.6,
                                563184.5,   1486838.8,  2177877.2,  2942903.9};

            int periods = 6;
            int N = 0;
            int pra = -1;          //latent price avg
            int prd = -1;          //latent price diff
            int inf = -1;// 0;    //inflows index
            int dem = -1;//1;    //demand index
            int ren = -1;//2;



            if (GeneratePriceLatentVariable)
            {
                pra = N; N++;

                component_names.Add("X_avg");

            }
            if (GenerateLatentDiffVariable)
            {
                prd = N; N++;
                component_names.Add("X_dif");
            }

            if (GenerateHydro)
            { inf = N; N++; component_names.Add("inflows"); }
            if (GenerateDemand)
            { dem = N; N++; component_names.Add("demand"); }
            if (GenerateRenewable)
            { ren = N; N++; component_names.Add("ren"); }

            //0 inflows
            //1 demand
            //2 renovable

            double[,] V = new double[N, N];
            double[] kappa = new double[N];
            bool[] log = new bool[N];
            double[,] mean_series = new double[N, periods];
            double[] UpperBound = NewUpperBound(N);


            for (int i = 0; i < periods; i++)
            {
                double p = (double)i / (double)(periods - 1);
                double phase = 2 * 3.14 * p;

                //if(inf!=-1)
                //    mean_series[inf, i] = 1+0.7*Math.Cos( phase );

                if (inf != -1)
                {
                    mean_series[inf, i] = avg_inflows[i];
                }

                if (dem != -1)
                    mean_series[dem, i] = 1 + 0.8 * Math.Sin(phase);

                if (ren != -1)
                    mean_series[ren, i] = 2000000 + 1000000 * Math.Abs(Math.Cos(phase));
            }


            if (pra != -1)
            {
                log[pra] = false;
                if (prd != -1)
                    log[prd] = false;

                //All Factors Quadratic

                //V[pra, pra] = 10.37500155;
                V[pra, pra] = 5.37500155;

                UpperBound[pra] = 7937700;

                if (prd != -1)
                {
                    V[pra, prd] = 7.856198138;
                    V[prd, pra] = 7.856198138; V[prd, prd] = 18.8784327;
                }

                kappa[pra] = 0.33661;

                if (prd != -1)
                    kappa[prd] = 0.170762;
            }

            //inflows
            if (inf != -1)
            {
                log[inf] = true;
                V[inf, inf] = 1.1;// double.Parse(textBoxInflowsV.Text);// 1.1;
                kappa[inf] = 3;// double.Parse(textBoxInflowsK.Text);   //3 ;
            }
            if (dem != -1)
            {
                log[dem] = true;
                //demand
                V[dem, dem] = .2;
                kappa[dem] = 2;
            }

            if (ren != -1)
            {
                log[ren] = true;
                //renovable
                V[ren, ren] = .2;
                //V[ren, ren] = 0;//kill renewable

                kappa[ren] = 3;
            }

            double[] deltat = new double[periods];
            for (int z = 0; z < deltat.Length; z++)
                deltat[z] = 1.0 / 12.0; //monthly...  1.0 / periods;


            //Generation parameters...
            double prob_treshold = .2;
            bool use_MC = true;
            int MC_realizations = 2; // realizations per node
            int seed = 1;

            ScenarioTree tree = new MultivariateAR1Tree(V, kappa, mean_series, log, deltat,
                                prob_treshold,
                                  use_MC,
                                    MC_realizations,
                                    seed);

            ((MultivariateAR1Tree)tree).UpperBound = UpperBound;

            tree.Generate();
            //last = tree;
            //Info();
            return tree;
        }

        static double[] NewUpperBound(int N)
        {
            double[] u = new double[N];
            for (int i = 0; i < N; i++)
                u[i] = double.NegativeInfinity;
            return u;
        }


        //old generation routine
        static ScenarioTree GenerateOld()
        {
            int p_Periods = 11;

            ScenarioTree XST = new XTree(1);
            XST.Generate();


            ScenarioTree WST = new WTree(p_Periods);
            WST.Generate();

            ScenarioTree ST = ScenarioTree.Merge(XST, WST);
            return ST;
        }
    }
}
