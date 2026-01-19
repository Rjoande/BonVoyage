/*
	This file is part of Bon Voyage /L
		© 2024-2026 LisiasT : http://lisias.net <support@lisias.net>
		© 2018-2024 Maja
		© 2016-2018 RealGecko

	Bon Voyage /L is licensed as follows:
		* GPL 3.0 : https://www.gnu.org/licenses/gpl-3.0.txt

	Bon Voyage /L is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

	You should have received a copy of the GNU General Public License 3.0
	along with Bon Voyage /L. If not, see <https://www.gnu.org/licenses/>.

*/
using System;
using System.Collections.Generic;

namespace BonVoyage.PowerSources
{
	/// <summary>
	/// Informations about resource for a converter
	/// </summary>
	internal class Resource
	{
		public readonly string Name;							// Name of the resource
		public double Ratio  = 0;								// Consumption per second
		public double MaximumAmountAvailable = 0;				// Maximum amout of the resource available for usage
		public double CurrentAmountUsed = 0;					// Current amout of the resource used by a converter

		internal Resource(ResourceRatio resourceRatio)
		{
			this.Name = resourceRatio.ResourceName;
			this.Ratio = resourceRatio.Ratio;
		}

		internal Resource(ConfigNode resourceNode)
		{
			this.Name = resourceNode.GetValue("name");
			this.Ratio = Convert.ToDouble(resourceNode.GetValue("ratio"));
			this.MaximumAmountAvailable = Convert.ToDouble(resourceNode.GetValue("maximumAmount"));
			this.CurrentAmountUsed = Convert.ToDouble(resourceNode.GetValue("currentAmount"));
		}

		public override int GetHashCode() => this.Name.GetHashCode();

		public override bool Equals(object obj)
		{
			if (null == obj) return false;
			if (obj == this) return true;
			Resource p = obj as Resource;
			if (null == p) return false;
			return this.Name.Equals(p.Name);
		}
	}


	/// <summary>
	/// Class for fuel cells and engines
	/// </summary>
	internal abstract class Converter
	{
		internal bool Use; // Use converter
		internal double OutputValue; // Output value for any output resource (e.g. EC for fuel cells)
		internal readonly List<Resource> InputResources = new List<Resource>();
		internal readonly HashSet<string> knownInputResources = new HashSet<string>();

		protected readonly Vessel vessel;

		internal Converter(Vessel vessel)
		{
			this.vessel = vessel;
		}

		internal abstract void Read(ConfigNode controllerNode);
		internal abstract void Write(ConfigNode controllerNode);

		internal abstract void Check(double throttle);

		/// <summary>
		/// Calculate available power from generators and reactors
		/// </summary>
		/// <returns></returns>
		internal abstract double GetAvailablePower();

		/// <summary>
		/// Process the available resources, consuming them as demanded.
		/// </summary>
		/// <param name="broker"></param>
		/// <returns>returns true if enough resources available, otherwise false (halting the processing)</returns>
		internal abstract bool ProcessResources(IResourceBroker broker);

		internal abstract void Update(ref double deltaT, ref double deltaTOver);
	}
}
