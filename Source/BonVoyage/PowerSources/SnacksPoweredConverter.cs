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
	internal class SnacksPoweredConverter : Converter
	{
		// TODO: Consume Oxygen, Water and Food when available

		internal SnacksPoweredConverter(Vessel vessel) : base(vessel)
		{
		}

		internal override void Check(double throttle) { }

		internal override double GetAvailablePower() => 0;

		internal override bool ProcessResources(IResourceBroker broker) => true;
		internal override void Update(ref double deltaT, ref double deltaTOver) { }
		internal override void Read(ConfigNode subNode) { }
		internal override void Write(ConfigNode subNode) { }
	}
}
