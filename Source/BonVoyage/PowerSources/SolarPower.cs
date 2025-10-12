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
namespace BonVoyage.PowerSources
{
	internal abstract class SolarPower
	{
		internal bool Use;
		internal bool AutoDeploy;
		internal bool DriveWhenDeployed;

		protected int mainStarIndex; // Vessel's main star's index in the FlightGlobals.Bodies
		protected readonly Vessel vessel;

		internal SolarPower(Vessel vessel)
		{
			this.mainStarIndex = 0; // In the most cases The Sun
			this.vessel = vessel;
		}

		internal abstract void Read(ConfigNode subNode);
		internal abstract void Write(ConfigNode controllerNode);

		/// <summary>
		/// Calculate available power from solar panels
		/// </summary>
		/// <returns></returns>
		internal double GetAvailablePower() => this.calculateAvailablePower();

		protected abstract double calculateAvailablePower();
	}

	internal class NoSolarPower : SolarPower
	{
		internal NoSolarPower(Vessel vessel) : base(vessel)
		{
		}

		protected override double calculateAvailablePower()
		{
			return 0;
		}

		internal override void Read(ConfigNode controllerNode) { }
		internal override void Write(ConfigNode controllerNode) { }
	}

	internal class StockSolarPower : SolarPower
	{
		internal StockSolarPower(Vessel vessel) : base(vessel)
		{
			this.mainStarIndex = Tools.GetMainStar(vessel).flightGlobalsIndex;
		}

		internal override void Read(ConfigNode controllerNode)
		{
			ConfigNode subNode = controllerNode.GetNode("SOLAR_POWER");
			if (null == subNode) return;

			this.Use = Convert.ToBoolean(subNode.GetValue("useSolarPower") ?? "true");
			this.AutoDeploy = Convert.ToBoolean(subNode.GetValue("autoDeploy" ?? "false"));
			this.DriveWhenDeployed = Convert.ToBoolean(subNode.GetValue("driveWhenDeployed" ?? "false"));
		}

		internal override void Write(ConfigNode controllerNode)
		{
			ConfigNode subNode = new ConfigNode("SOLAR_POWER");
			subNode.AddValue("useSolarPower", this.Use);
			subNode.AddValue("autoDeploy", this.AutoDeploy);
			subNode.AddValue("driveWhenDeployed", this.DriveWhenDeployed);
			controllerNode.AddNode(subNode);
		}

		protected override double calculateAvailablePower()
		{
			if (!this.Use) return 0;

			// Kopernicus sets the right values for PhysicsGlobals.SolarLuminosity and PhysicsGlobals.SolarLuminosityAtHome so we can use them in all cases
			double solarPower = 0;
			double distanceToSun = Vector3d.Distance(this.vessel.GetWorldPos3D(), FlightGlobals.Bodies[mainStarIndex].position);
			double solarFlux = PhysicsGlobals.SolarLuminosity / (4 * Math.PI * distanceToSun * distanceToSun); // f = L / SA = L / 4π r2 (Wm-2)
			float multiplier = 1;

			for (int i = 0; i < this.vessel.parts.Count; ++i)
			{
				ModuleDeployableSolarPanel solarPanel = this.vessel.parts[i].FindModuleImplementing<ModuleDeployableSolarPanel>();
				if (solarPanel == null)
					continue;

				// this doesn't account for solar panel orientation, it will always assume full exposure for all panels.
				// this should be fixed, maybe by stealing some code from here
				// https://github.com/Kerbalism/Kerbalism/blob/94bf6e8dd016900404086a713fdf68c235c6a7c9/src/Kerbalism/Modules/SolarPanelFixer.cs#L886

				if ((solarPanel.deployState != ModuleDeployablePart.DeployState.BROKEN) && (solarPanel.deployState != ModuleDeployablePart.DeployState.RETRACTED) && (solarPanel.deployState != ModuleDeployablePart.DeployState.RETRACTING))
				{
					if (solarPanel.useCurve) // Power curve
						multiplier = solarPanel.powerCurve.Evaluate((float)distanceToSun);
					else // solar flux at current distance / solar flux at 1AU (Kerbin in stock, other value in Kopernicus)
						multiplier = (float)(solarFlux / PhysicsGlobals.SolarLuminosityAtHome);
					solarPower += solarPanel.chargeRate * multiplier;
				}
			}

			return solarPower;
		}
	}
}
