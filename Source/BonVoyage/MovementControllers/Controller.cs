/*
	This file is part of Bon Voyage /L
		© 2024-2025 LisiasT : http://lisias.net <support@lisias.net>
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


namespace BonVoyage.MovementControllers
{
	internal abstract class Controller
	{
		public double averageSpeed = 0;
		public string AverageSpeedAsText => (Double.IsNaN(this.averageSpeed) || Double.IsInfinity(this.averageSpeed) ? "---" : this.averageSpeed.ToString("F") + " m/s");
		public double vesselHeightFromTerrain = 0;
		public double maxSpeedBase;					// maximum speed without modifiers
		public string MaxSpeedBaseAsText => (Double.IsNaN(this.maxSpeedBase) || Double.IsInfinity(this.maxSpeedBase) ? "---" : this.maxSpeedBase.ToString("F") + " m/s");

		protected readonly Vessel vessel;

		protected Controller(Vessel vessel, ConfigNode moduleConfigNode)
		{
			this.vessel = vessel;

			// Load values from config if it isn't the first run of the mod (we are reseting vessel on the first run)
			if (!Configuration.FirstRun)
			{
				this.averageSpeed = double.Parse(moduleConfigNode.GetValue("averageSpeed") ?? "0");
				this.vesselHeightFromTerrain = double.Parse(moduleConfigNode.GetValue("vesselHeightFromTerrain") ?? "0");
			}

			this.maxSpeedBase = 0.5;
		}

		/// <summary>
		/// Save move of a rover. We need to prevent hitting an active vessel.
		/// </summary>
		/// <param name="latitude"></param>
		/// <param name="longitude"></param>
		/// <returns>true if rover was moved, false otherwise</returns>
		public abstract bool MoveSafely(double latitude, double longitude);

		internal virtual void Check(double throttle)
		{
			this.vesselHeightFromTerrain = vessel.radarAltitude;
		}
	}

}
