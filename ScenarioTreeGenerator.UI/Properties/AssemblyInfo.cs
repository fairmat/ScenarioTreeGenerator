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

using System.Reflection;
using System.Runtime.InteropServices;
using Mono.Addins;

// The following lines tell that the assembly is an addin
[assembly: Mono.Addins.Addin("Scenario Tree Generator", "1.0.3", Category = "Export")]
[assembly: Mono.Addins.AddinDependency("Fairmat", "1.0")]
[assembly: Mono.Addins.AddinAuthor("Fairmat SRL")]
[assembly: AddinDescription("Scenario Tree Generator")]

// This plug-in is also an addin root.
[assembly: Mono.Addins.AddinRoot("ScenarionTreeGenerator", "1.0")]
[assembly: AssemblyTrademark("Fairmat")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("706dc359-e8fe-486d-8491-c4aefcb46940")]
