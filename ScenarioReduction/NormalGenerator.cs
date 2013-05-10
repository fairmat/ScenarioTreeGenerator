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
    internal class NormalGenerator
    {
        private Random rnd;
        private double z1;
        private double z2;
        private int count = 0;

        public NormalGenerator()
        {
            this.rnd = new Random();
        }

        public NormalGenerator(int seed)
        {
            this.rnd = new Random(seed);
        }

        public double Next()
        {
            if (this.count == 0)
            {
                BoxMullerTransform();
                this.count++;
                return this.z1;
            }
            else
            {
                this.count = 0;
                return this.z2;
            }
        }

        /// <summary>
        /// Generates a pair of iid normal variates
        /// see http://en.wikipedia.org/wiki/Box-Muller_transform.
        /// </summary>
        private void BoxMullerTransform()
        {
            do
            {
                double x = 2 * this.rnd.NextDouble() - 1;
                double y = 2 * this.rnd.NextDouble() - 1;
                double s = x * x + y * y;
                if (s <= 1)
                {
                    double s2 = Math.Sqrt(-2 * Math.Log(s) / s);
                    this.z1 = x * s2;
                    this.z2 = y * s2;
                    break;
                }
            }
            while (true);
        }
    }
}
