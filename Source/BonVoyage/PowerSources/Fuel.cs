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

namespace BonVoyage.PowerSources
{
	/// <summary>
	/// Informations about propellant for engines
	/// </summary>
	internal class Fuel
	{
		internal readonly string Name;					// Name of the propellant
		internal double FuelFlow;						// Consumption per second
		internal double MaximumAmountAvailable = 0;		// Maximum amout of the propellant available for usage
		internal double CurrentAmountUsed = 0;          // Current amout of the propellant used by engines

		public Fuel(Propellant propellant)
		{
			this.Name = propellant.name;
		}

		public Fuel(ModuleResource moduleResource)
		{
			this.Name = moduleResource.name;
		}

		internal Fuel(ConfigNode propellantNode)
		{
			this.Name = propellantNode.GetValue("name");
			this.FuelFlow = Convert.ToDouble(propellantNode.GetValue("fuelFlow"));
			this.MaximumAmountAvailable = Convert.ToDouble(propellantNode.GetValue("maximumAmount"));
			this.CurrentAmountUsed = Convert.ToDouble(propellantNode.GetValue("currentAmount"));
		}

		public override int GetHashCode() => this.Name.GetHashCode();

		public override bool Equals(object obj)
		{
			if (null == obj) return false;
			if (obj == this) return true;
			Fuel p = obj as Fuel;
			if (null == p) return false;
			return this.Name.Equals(p.Name);
		}

	}
}
