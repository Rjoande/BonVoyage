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

namespace BonVoyage.MovementControllers
{
	internal class WheelController:Controller
	{
		/// <summary>
		/// Result of the test of wheels
		/// </summary>
		private struct WheelTestResult
		{
			internal double powerRequired; // Total power required (0 to 1.0)
			internal double maxSpeedSum; // Sum of max speeds of all online wheels (0 to 1.0)
			internal int inTheAir; // Count of wheels in the air
			internal int operable; // Count of operable wheels
			internal int damaged; // Count of damaged wheels
			internal int online; // Count of online wheels
			internal float maxWheelRadius; // Maximum of radii of all aplicable wheels

			internal WheelTestResult(double powerRequired, double maxSpeedSum, int inTheAir, int operable, int damaged, int online, float maxWheelRadius)
			{
				this.powerRequired = powerRequired;
				this.maxSpeedSum = maxSpeedSum;
				this.inTheAir = inTheAir;
				this.operable = operable;
				this.damaged = damaged;
				this.online = online;
				this.maxWheelRadius = maxWheelRadius;
			}

			public override string ToString() => string.Format("WheelTestResult powerRequired:{0}; maxSpeedSum:{1}; operable:{2}; damaged:{3}; online:{4}; maxWheelRadius:{5}.", this.powerRequired, this.maxSpeedSum, this.operable, this.damaged, this.online, this.maxWheelRadius);
		}

		internal int wheelsPercentualModifier;							// Speed modifier based on wheels
		internal string WheelsPercentualModifierAsText => (Double.IsNaN(this.maxSpeedBase) || Double.IsInfinity(this.maxSpeedBase) ? "---" : this.maxSpeedBase.ToString("F") + "%");

		internal WheelController(Vessel vessel, ConfigNode moduleConfigNode) : base(vessel, moduleConfigNode)
		{
		}

		private WheelTestResult wheelTestResult = new WheelTestResult(); // Result of a test of wheels
		internal int OnLine => this.wheelTestResult.online;
		internal int InTheAir => this.wheelTestResult.inTheAir;
		internal int Operable => this.wheelTestResult.operable;
		internal double PowerRequired => this.wheelTestResult.powerRequired;

		internal override void Check(double throttle)
		{
			base.Check(throttle);

			// Test stock wheels
			WheelTestResult testResultStockWheels = CheckStockWheels();
			Log.dbg("testResultStockWheels : {0}", testResultStockWheels.ToString() );

			// Test KSPWheels
			WheelTestResult testResultKSPkWheels = CheckKSPWheels();
			Log.dbg("testResultKSPkWheels : {0}", testResultKSPkWheels.ToString() );

			// Sum it
			this.wheelTestResult.powerRequired = testResultStockWheels.powerRequired + testResultKSPkWheels.powerRequired;
			this.wheelTestResult.maxSpeedSum = testResultStockWheels.maxSpeedSum + testResultKSPkWheels.maxSpeedSum;
			this.wheelTestResult.inTheAir = testResultStockWheels.inTheAir + testResultKSPkWheels.inTheAir;
			this.wheelTestResult.operable = testResultStockWheels.operable + testResultKSPkWheels.operable;
			this.wheelTestResult.damaged = testResultStockWheels.damaged + testResultKSPkWheels.damaged;
			this.wheelTestResult.online = testResultStockWheels.online + testResultKSPkWheels.online;
			this.wheelTestResult.maxWheelRadius = testResultStockWheels.maxWheelRadius + testResultKSPkWheels.maxWheelRadius;

			if (0 != this.wheelTestResult.online)
			{
				this.maxSpeedBase = this.wheelTestResult.maxSpeedSum / this.wheelTestResult.online;
				this.wheelsPercentualModifier = Math.Min(70, (40 + 5 * this.wheelTestResult.online));
				this.averageSpeed = this.maxSpeedBase * Convert.ToDouble(this.wheelsPercentualModifier) / 100 * (Convert.ToDouble(throttle) / 100);
			}
			else
				this.maxSpeedBase = this.averageSpeed = 0;
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
			this.vessel.altitude = altitude + this.vesselHeightFromTerrain + Configuration.HeightOffset;

			return true;
		}
		/// <summary>
		/// Test stock wheels implementing standard module ModuleWheelBase
		/// </summary>
		/// <returns></returns>
		private WheelTestResult CheckStockWheels()
		{
			int inTheAir = 0;
			int operable = 0;
			int damaged = 0;
			int online = 0;
			float maxWheelRadius = 0;

			// Get wheel modules
			List<Part> wheels = new List<Part>();
			for (int i = 0; i < vessel.parts.Count; ++i)
			{
				Part part = vessel.parts[i];
				if (part.Modules.Contains("ModuleWheelBase"))
					wheels.Add(part);
			}

			double powerRequired = 0;
			double maxSpeedSum = 0;
			for (int i = 0; i < wheels.Count; ++i)
			{
				ModuleWheelBase wheelBase = wheels[i].FindModuleImplementing<ModuleWheelBase>();

				// Skip legs
				if (WheelType.LEG == wheelBase.wheelType)
					continue;

				// Save max wheel radius for height compensations
				if (wheelBase.radius < maxWheelRadius)
					maxWheelRadius = wheelBase.radius;

				// Check damaged wheels
				ModuleWheels.ModuleWheelDamage wheelDamage = wheels[i].FindModuleImplementing<ModuleWheels.ModuleWheelDamage>();
				if (wheelDamage != null)
				{
					if (wheelDamage.isDamaged)
					{
						++damaged;
						continue;
					}
				}

				// Whether or not wheel is touching the ground
				if (!wheelBase.isGrounded)
				{
					++inTheAir;
					continue;
				}
				else
					++operable;

				// Check motorized wheels
				ModuleWheels.ModuleWheelMotor wheelMotor = wheels[i].FindModuleImplementing<ModuleWheels.ModuleWheelMotor>();
				if (null != wheelMotor)
				{
					// Wheel is on
					if (wheelMotor.motorEnabled)
					{
						powerRequired += wheelMotor.avgResRate * wheelMotor.driveLimiter;
						maxSpeedSum += wheelMotor.GetMaxSpeed() * wheelMotor.driveLimiter;
						++online;
					}
				}
			}

			powerRequired /= 100;
			maxSpeedSum /= 100;
			return new WheelTestResult(powerRequired, maxSpeedSum, inTheAir, operable, damaged, online, maxWheelRadius);
		}

		/// <summary>
		/// Test KSPWheels implementing module KSPWheelBase
		/// Ideally it should be on its own dedicated class but since it works on a Stock only installment, why bother?
		/// </summary>
		/// <returns></returns>
		private WheelTestResult CheckKSPWheels()
		{
			double powerRequired = 0;
			double maxSpeedSum = 0;
			int inTheAir = 0;
			int operable = 0;
			int damaged = 0;
			int online = 0;
			float maxWheelRadius = 0;

			// Get wheel modules
			List<Part> wheels = new List<Part>();
			for (int i = 0; i < vessel.parts.Count; ++i)
			{
				Part part = vessel.parts[i];
				if (part.Modules.Contains("KSPWheelBase"))
					wheels.Add(part);
			}

			for (int i = 0; i < wheels.Count; ++i)
			{
				List<PartModule> partModules = wheels[i].Modules.GetModules<PartModule>();
				PartModule wheelBase = partModules.Find(t => t.moduleName == "KSPWheelBase");

				// Save max wheel radius for height compensations
				float radius = (float)wheelBase.Fields.GetValue("wheelRadius");
				if (radius < maxWheelRadius)
					maxWheelRadius = radius;

				// Check damaged wheels
				if (wheelBase.Fields.GetValue("persistentState").ToString() == "BROKEN")
				{
					++damaged;
					continue;
				}

				// Whether or not wheel is touching the ground
				PartModule wheelDamage = partModules.Find(t => t.moduleName == "KSPWheelDamage");
				float maxSafeSpeed = 0f; // We compare this value later with maxDrivenSpeed to eliminate unreal gear ratios
				if (wheelDamage != null)
				{
					++operable;
					maxSafeSpeed = (float)wheelDamage.Fields.GetValue("maxSafeSpeed");
				}

				// Check motorized wheels
				List<PartModule> wheelMotors = partModules.FindAll(t => t.moduleName == "KSPWheelMotor");
				for (int m = 0; m < wheelMotors.Count; ++m)
				{
					// Wheel is on
					if (!(bool)wheelMotors[m].Fields.GetValue("motorLocked"))
					{
						++online;
						powerRequired += (float)wheelMotors[m].Fields.GetValue("maxECDraw") * (float)wheelMotors[m].Fields.GetValue("motorOutput"); // Motor output can be limited by a slider
						if (maxSafeSpeed > 0)
							maxSpeedSum += Math.Min((float)wheelMotors[m].Fields.GetValue("maxDrivenSpeed"), maxSafeSpeed);
						else
							maxSpeedSum += (float)wheelMotors[m].Fields.GetValue("maxDrivenSpeed");
					}
				}

				// Check tracks
				PartModule wheelTracks = partModules.Find(t => t.moduleName == "KSPWheelTracks");
				if (wheelTracks != null)
				{
					++operable; // We will count them as two wheels, so add another operable wheel
					if (!(bool)wheelTracks.Fields.GetValue("motorLocked"))
					{
						online += 2;
						powerRequired += (float)wheelTracks.Fields.GetValue("maxECDraw") * (float)wheelTracks.Fields.GetValue("motorOutput"); // Motor output can be limited by a slider
						if (maxSafeSpeed > 0)
							maxSpeedSum += 2 * Math.Min((float)wheelTracks.Fields.GetValue("maxDrivenSpeed"), maxSafeSpeed);
						else
							maxSpeedSum += 2 * (float)wheelTracks.Fields.GetValue("maxDrivenSpeed");
					}
				}
				else // Special cases
				{
					if (wheels[i].name.StartsWith("critterCrawler")) // Critter crawler has six legs with motors
					{
						damaged *= 6;
						inTheAir *= 6;
						operable *= 6;
					}
				}
			}

			powerRequired /= 100;
			return new WheelTestResult(powerRequired, maxSpeedSum, inTheAir, operable, damaged, online, maxWheelRadius);
		}

	}
}
