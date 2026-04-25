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

using Expansions.Serenity;

namespace BonVoyage.MovementControllers
{
	internal class EngineController:Controller
	{
		/// <summary>
		/// Result of the test of wheels
		/// </summary>
		private struct EngineTestResult
		{
			internal double maxThrustSum; // Sum of max thrusts of all enabled engines
			internal double powerRequired; // Total EC power required

			internal EngineTestResult(double maxThrustSum, double powerRequired)
			{
				this.maxThrustSum = maxThrustSum;
				this.powerRequired = powerRequired;
			}

			public override string ToString() => string.Format("EngineTestResult maxThrustSum={0}; powerRequired={1}", maxThrustSum, powerRequired);
		}

		private EngineTestResult engineTestResult = new EngineTestResult();		// Result of a test of engines
		internal double MaxThrust => this.engineTestResult.maxThrustSum;
		internal double PowerRequired => this.engineTestResult.powerRequired;

		internal EngineController(Vessel vessel, ConfigNode moduleConfigNode) : base(vessel, moduleConfigNode)
		{
		}

		internal override void Check(double throttle)
		{
			base.Check(throttle);

			// Test engines and rotors
			EngineTestResult testResultStockEngines = CheckStockEngines();
			Log.dbg("testResultStockEngines : {0}", testResultStockEngines.ToString());
			EngineTestResult testResultBGRotors = CheckBGRotors();
			Log.dbg("testResultBGRotors : {0}", testResultBGRotors.ToString());

			// Sum it
			this.engineTestResult.maxThrustSum = testResultStockEngines.maxThrustSum + testResultBGRotors.maxThrustSum;
			this.engineTestResult.powerRequired = /*testResultStockEngines.powerRequired +*/ testResultBGRotors.powerRequired; // NOTE: Fuel Engines **DO NOT** require EC!!
			this.engineTestResult.maxThrustSum *= (Convert.ToDouble(throttle) / 100);
		}

		public override bool MoveSafely(double latitude, double longitude)
		{
			if (FlightGlobals.ActiveVessel != null)
			{
				Vector3d newPos = vessel.mainBody.GetWorldSurfacePosition(latitude, longitude, 0);
				Vector3d actPos = FlightGlobals.ActiveVessel.GetWorldPos3D();
				double distance = Vector3d.Distance(newPos, actPos);
				if (distance <= 2400)
					return false;
			}

			this.vessel.latitude = latitude;
			this.vessel.longitude = longitude;
			this.vessel.altitude = vesselHeightFromTerrain;

			return true;
		}

		/// <summary>
		/// Test stock engines implementing standard modules ModuleEnginesFX and ModuleEngines
		/// </summary>
		/// <returns></returns>
		private EngineTestResult CheckStockEngines()
		{
			// Get jet engines' modules (and also rocket, saving a loop)
			List<Part> jets = new List<Part>();
			List<Part> rockets = new List<Part>();
			for (int i = 0; i < vessel.parts.Count; ++i)
			{
				Part part = vessel.parts[i];
				if (part.Modules.Contains("ModuleEnginesFX"))
					jets.Add(part);
				if (part.Modules.Contains("ModuleEngines"))
					rockets.Add(part);
			}

			double maxThrustSum = 0;
			double powerRequired = 0;
			for (int i = 0; i < jets.Count; ++i)
			{
				List<ModuleEnginesFX> enginesFx = jets[i].FindModulesImplementing<ModuleEnginesFX>();
				if (enginesFx != null)
				{
					for (int k = 0; k < enginesFx.Count; ++k)
					{
						if (!enginesFx[k].engineShutdown && enginesFx[k].isOperational)
						{
							// Max thrust
							maxThrustSum += enginesFx[k].maxThrust * enginesFx[k].thrustPercentage;

							// Propellants used in ISP computation - what is not used is usually air
							for (int p = 0; p < enginesFx[k].propellants.Count; p++)
							{
								if (!enginesFx[k].propellants[p].ignoreForIsp)
								{
									// For electric engines - save required power and don't add it to propellants
									if (enginesFx[k].propellants[p].name == "ElectricCharge")
									{
										powerRequired += enginesFx[k].getMaxFuelFlow(enginesFx[k].propellants[p]) * enginesFx[k].thrustPercentage;
										continue;
									}
								}
							}
						}
					}
				}
			}

			maxThrustSum /= 100;
			powerRequired /= 100;

			for (int i = 0; i < rockets.Count; ++i)
			{
				List<ModuleEngines> engines = rockets[i].FindModulesImplementing<ModuleEngines>();
				for (int k = 0; k < engines.Count; ++k)
				{
					if (!engines[k].engineShutdown && engines[k].isOperational)
					{
						// Max thrust
						maxThrustSum += engines[k].MaxThrustOutputAtm(false, true, Convert.ToSingle(vessel.mainBody.atmPressureASL), vessel.atmosphericTemperature, vessel.mainBody.atmDensityASL);
					}
				}
			}

			return new EngineTestResult(maxThrustSum, powerRequired);
		}


		/// <summary>
		/// Test Breaking Ground DLC rotors implementing module ModuleRoboticServoRotor
		/// It must be moved to its own class. Eventually...
		/// </summary>
		/// <returns></returns>
		private EngineTestResult CheckBGRotors()
		{
			double powerRequired = 0;
			double maxThrustSum = 0;

			// Get rotors
			List<Part> servoRotor = new List<Part>();
			for (int i = 0; i < vessel.parts.Count; ++i)
			{
				Part part = vessel.parts[i];
				if (part.Modules.Contains("ModuleRoboticServoRotor"))
					servoRotor.Add(part);
			}

			for (int i = 0; i < servoRotor.Count; ++i)
			{
				List<ModuleRoboticServoRotor> rotors = servoRotor[i].FindModulesImplementing<ModuleRoboticServoRotor>();
				for (int k = 0; k < rotors.Count; ++k)
				{
					if (rotors[k].servoIsMotorized && rotors[k].servoMotorIsEngaged)
					{
						// Max thrust
						maxThrustSum += rotors[k].maxMotorOutput * rotors[k].servoMotorSize / 100 / 9; // We need to change max thrust to be in line with the base values for jets

						for (int r = 0; r < rotors[k].resHandler.inputResources.Count; ++r)
						{
							// Skip Air
							if (rotors[k].resHandler.inputResources[r].name == "IntakeAir")
								continue;

							// For EC - save required power and don't add it to propellants
							if (rotors[k].resHandler.inputResources[r].name == "ElectricCharge")
							{
								powerRequired += rotors[k].maxMotorOutput * 4 / 3 * rotors[k].baseResourceConsumptionRate * rotors[k].resHandler.inputResources[r].rate * rotors[k].servoMotorSize / 100;
								continue;
							}
						}
					}
				}
			}

			return new EngineTestResult(maxThrustSum, powerRequired);
		}
	}
}