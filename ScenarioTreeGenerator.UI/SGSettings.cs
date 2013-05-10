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
using System.IO;
using System.Windows.Forms;
using DVPLDOM;
using ScenarioReduction;

namespace ScenarioTreeGenerator.UI
{
    public partial class SGSettings : Form
    {
        private Document doc;

        public SGSettings(Document doc)
        {
            InitializeComponent();
            this.doc = doc;
        }

        private string GenerateOutputFilename()
        {
            int L = int.Parse(textBoxScenarios.Text);
            int S = int.Parse(textBoxStages.Text);

            return Path.Combine(textBoxOutputFolder.Text, "Scenarios_" + L + "_" + S + ".dat");
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            int L = int.Parse(textBoxScenarios.Text);
            int S = int.Parse(textBoxStages.Text);

            MultivariateTree mv = new MultivariateTree(this.doc.DefaultProject as ProjectROV);

            mv.AlwaysUseMontecarlo = false;
            mv.MonteCarloRealizations = 4;
            mv.S = S;

            mv.Generate();
            ScenarioTree st = mv;

            if (st.Leafs.Count > L)
            {
                BackwardReduction br = new BackwardReduction(st);
                br.Reduce(L);
                st = br.ReducedScenarioTree;
            }

            // Export the data.
            ExpressExporter export = new ExpressExporter();

            System.IO.TextWriter tw = System.IO.File.CreateText(GenerateOutputFilename());
            System.IO.TextWriter tmp = Console.Out;
            Console.SetOut(tw);

            // Save temporarily the current culture to restore it later.
            System.Globalization.CultureInfo current = System.Threading.Thread.CurrentThread.CurrentCulture;

            // Sets EN-US culture to have a correct number formatting (decimal sperator).
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("En-Us");
            export.Export(st);
            tw.Close();

            // Restore the previous culture settings.
            System.Threading.Thread.CurrentThread.CurrentCulture = current;

            Console.SetOut(tmp);
            MessageBox.Show(this, "Scenario tree generated succesfully");
        }

        private void buttonChoseFolder_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                textBoxOutputFolder.Text = folderBrowserDialog1.SelectedPath;
                textBoxFileName.Text = GenerateOutputFilename();
            }
        }

        private void LoadUI()
        {
            textBoxFileName.Text = GenerateOutputFilename();
        }

        private void SGSettings_Load(object sender, EventArgs e)
        {
            LoadUI();
        }
    }
}
