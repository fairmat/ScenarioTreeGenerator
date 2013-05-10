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
    public class ScenarioDescriptiveStatistics
    {
        public double[,] means;
        public double[,] variances;

        public static double Distance1(ScenarioDescriptiveStatistics d1, ScenarioDescriptiveStatistics d2)
        {
            int I = d1.variances.GetLength(0);
            int D = d1.variances.GetLength(1);
            double d = 0;
            double norm = 0;
            for (int i = 0; i < I; i++)
            {
                double d0 = 0;
                double norm0 = 0;
                for (int c = 0; c < D; c++)
                {
                    d0 += Math.Pow(d1.variances[i, c] - d2.variances[i, c], 2);
                    norm0 += Math.Pow(d1.variances[i, c], 2);
                }

                d += Math.Sqrt(d0);
                norm += Math.Sqrt(norm0);
            }

            return d / norm;
        }

        public static double Distance3(ScenarioDescriptiveStatistics d1, ScenarioDescriptiveStatistics d2)
        {
            int I = d1.variances.GetLength(0);
            int D = d1.variances.GetLength(1);

            double maxd = 0;
            for (int c = 0; c < D; c++)
            {
                double d = 0;
                double norm = 0;
                for (int i = 0; i < I; i++)
                {
                    d += Math.Abs(d1.variances[i, c] - d2.variances[i, c]);
                    norm += Math.Abs(d1.variances[i, c]);
                }

                maxd = Math.Max(maxd, d / norm);
            }

            return maxd;
        }

        public static double Distance2(ScenarioDescriptiveStatistics d1, ScenarioDescriptiveStatistics d2)
        {
            int I = d1.variances.GetLength(0);
            int D = d1.variances.GetLength(1);
            double d = 0;
            double norm = 0;
            for (int i = 0; i < I; i++)
                for (int c = 0; c < D; c++)
                {
                    if (d1.variances[i, c] > 0)
                    {
                        d += Math.Pow((d1.variances[i, c] - d2.variances[i, c]) / d1.variances[i, c], 2);
                        norm += 1;
                    }
                }

            return Math.Sqrt(d) / Math.Sqrt(norm);
        }

        public static double Distance4(ScenarioDescriptiveStatistics d1, ScenarioDescriptiveStatistics d2)
        {
            int I = d1.variances.GetLength(0);
            int D = d1.variances.GetLength(1);
            double maxd = 0;
            for (int c = 0; c < D; c++)
            {
                double d = 0;
                double norm = 0;
                for (int i = 0; i < I; i++)
                {
                    if (d1.variances[i, c] > 0)
                    {
                        d += Math.Abs((d1.variances[i, c] - d2.variances[i, c]) / d1.variances[i, c]);
                        norm += 1;
                    }
                }

                maxd = Math.Max(d / norm, maxd);
            }

            return maxd;
        }
    }
}
