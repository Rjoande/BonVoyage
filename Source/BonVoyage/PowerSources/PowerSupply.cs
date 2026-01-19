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
namespace BonVoyage.PowerSources
{
	internal abstract class PowerSupply
	{
		internal bool Use; // Use batteries during a night

		internal abstract void Read(ConfigNode controllerNode);
		internal abstract void Write(ConfigNode controllerNode);

		internal abstract bool PowerIsAvailable {  get ; }
		internal abstract bool PowerIsExhausted {  get ; }
		internal abstract double GetAvailablePower();
	}
}