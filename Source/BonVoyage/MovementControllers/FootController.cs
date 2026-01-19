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


namespace BonVoyage.MovementControllers
{
	internal class FootController:Controller
	{
		// TODO: Speed based on Tiredness ?
		// TODO: Speed based on how much wheight being carried?
		// TODO: Speed based on terrain slope?

		internal FootController(Vessel vessel, ConfigNode moduleConfigNode) : base(vessel, moduleConfigNode)
		{
		}

		internal override void Check(double throttle)
		{
			base.Check(throttle);

			this.averageSpeed = this.maxSpeedBase * Math.Pow(9.81 * (this.vessel.mainBody.Radius * this.vessel.mainBody.Radius / this.vessel.mainBody.gravParameter), 1.0 / 3.0);
			this.vesselHeightFromTerrain = this.vessel.radarAltitude;
		}

		public override bool MoveSafely(double latitude, double longitude)
		{
			double altitude = GeoUtils.TerrainHeightAt(latitude, longitude, this.vessel.mainBody);
			if (FlightGlobals.ActiveVessel != null)
			{
				Vector3d newPos = this.vessel.mainBody.GetWorldSurfacePosition(latitude, longitude, altitude);
				Vector3d actPos = FlightGlobals.ActiveVessel.GetWorldPos3D();
				double distance = Vector3d.Distance(newPos, actPos);
				if (distance <= 2400)
					return false;
			}

			this.vessel.latitude = latitude;
			this.vessel.longitude = longitude;
			if (this.vessel.situation == Vessel.Situations.SPLASHED)
				this.vessel.altitude = vesselHeightFromTerrain;
			else
				this.vessel.altitude = altitude + vesselHeightFromTerrain;

			return true;
		}
	}

}
