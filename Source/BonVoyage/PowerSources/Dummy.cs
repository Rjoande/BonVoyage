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
using AbstractConverter = BonVoyage.PowerSources.Converter;
using AbstractPowerSupply = BonVoyage.PowerSources.PowerSupply;

namespace BonVoyage.PowerSources
{
	internal static class Dummy
	{
		internal class Converter : AbstractConverter
		{
			internal Converter(Vessel vessel) : base(vessel) { }
			internal override void Check(double throttle) { }
			internal override double GetAvailablePower() => 0.0d;
			internal override bool CheckResources(IResourceBroker broker) => false;
			internal override bool ProcessResources(IResourceBroker broker) => false;
			internal override void Update(ref double deltaT, ref double deltaTOver) { }
			internal override void Read(ConfigNode controllerNode) { }
			internal override void Write(ConfigNode controllerNode) { }
		}

		internal class PowerSupply:AbstractPowerSupply
		{
			internal override bool PowerIsAvailable => false;
			internal override bool PowerIsExhausted => false;
			internal override double GetAvailablePower() => 0.0d;
			internal override void Read(ConfigNode controllerNode) { }
			internal override void Write(ConfigNode controllerNode) { }
		}
	}
}
