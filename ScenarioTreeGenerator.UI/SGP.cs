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

using DVPLDOM;
using DVPLI;
using Mono.Addins;

namespace ScenarioTreeGenerator.UI
{
    [Extension("/Fairmat/DocumentToolUI")]
    public class SGP : IDocumentToolUI
    {
        private Document doc;
        #region IDocumentToolUI Members

        public IMyDocument Document
        {
            set
            {
                this.doc = value as Document;
            }
        }

        #endregion

        #region ICommand Members

        public void Execute()
        {
            SGSettings s = new SGSettings(this.doc);
            s.ShowDialog();
            return;
        }

        #endregion

        #region IToolUIInfo Members

        public string Category
        {
            get
            {
                return "Export";
            }
        }

        #endregion

        #region IMenuItemDescription Members

        public string ToolTipText
        {
            get
            {
                return "Generare a Scenario (Event) Tree for " +
                       "the stocastic components of the model";
            }
        }

        #endregion

        #region IDescription Members

        public string Description
        {
            get
            {
                return "Scenario/Event Tree Generator";
            }
        }

        #endregion
    }
}
