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

namespace BonVoyage.PowerSources
{
	internal class PropellantPoweredConverter : ElectricityPoweredConverter
	{
		private readonly HashSet<string> knownPropellants = new HashSet<string>();
		internal readonly List<Fuel> propellants = new List<Fuel>(); // Information about propellants

		internal PropellantPoweredConverter(Vessel vessel) : base(vessel)
		{
		}

		internal override void Read(ConfigNode controllerNode)
		{
			base.Read(controllerNode);

			ConfigNode subNode = controllerNode.GetNode("PROPELLANTS");
			if (null == subNode) return;

			ConfigNode[] propellants = subNode.GetNodes("FUEL");
			this.propellants.Clear();
			this.knownPropellants.Clear();
			for (int r = 0; r < propellants.Length; ++r)
			{
				Fuel ir = new Fuel( propellants[r]);
				this.propellants.Add(ir);
				this.knownPropellants.Add(ir.Name);
			}
		}

		internal override void Write(ConfigNode controllerNode)
		{
			base.Write(controllerNode);

			ConfigNode subNode = new ConfigNode("PROPELLANTS");

			List<Fuel> props = this.propellants;
			ConfigNode propellantNode;
			for (int r = 0; r < props.Count; ++r)
			{
				propellantNode = new ConfigNode("FUEL");
				propellantNode.AddValue("name", props[r].Name);
				propellantNode.AddValue("fuelFlow", props[r].FuelFlow);
				propellantNode.AddValue("maximumAmount", props[r].MaximumAmountAvailable);
				propellantNode.AddValue("currentAmount", props[r].CurrentAmountUsed);
				subNode.AddNode(propellantNode);
			}

			controllerNode.AddNode(subNode);
		}

		internal override void Check(double throttle)
		{
			base.Check(throttle);
			this.checkPropellants(throttle);
		}

		private void checkPropellants(double throttle)
		{
			this.checkStockPropellants();
			this.checkSerenityPropellants();
			for (int i = 0; i < this.propellants.Count; ++i)
				this.propellants[i].FuelFlow *= (throttle / 100);
		}

		private void checkStockPropellants()
		{
			// Get jet engines' modules
			List<Part> jets = new List<Part>();
			for (int i = 0; i < vessel.parts.Count; ++i)
			{
				Part part = vessel.parts[i];
				if (part.Modules.Contains("ModuleEnginesFX"))
					jets.Add(part);
			}

			for (int i = 0; i < jets.Count; ++i)
			{
				List<ModuleEnginesFX> enginesFx = jets[i].FindModulesImplementing<ModuleEnginesFX>();
				if (enginesFx != null)
				{
					for (int k = 0; k < enginesFx.Count; ++k)
					{
						if (!enginesFx[k].engineShutdown && enginesFx[k].isOperational)
						{
							// Propellants used in ISP computation - what is not used is usually air
							for (int p = 0; p < enginesFx[k].propellants.Count; ++p)
							{
								if (!enginesFx[k].propellants[p].ignoreForIsp)
								{
									Fuel ir = propellants.Find(x => x.Name == enginesFx[k].propellants[p].name);
									if (ir == null)
									{
										ir = new Fuel(enginesFx[k].propellants[p]);
										this.propellants.Add(ir);
										this.knownPropellants.Add(ir.Name);
									}
									ir.FuelFlow += enginesFx[k].getMaxFuelFlow(enginesFx[k].propellants[p]) * enginesFx[k].thrustPercentage / 100;
								}
							}
						}
					}
				}
			}

			// Get rocket engines' modules
			List<Part> rockets = new List<Part>();
			for (int i = 0; i < vessel.parts.Count; ++i)
			{
				Part part = vessel.parts[i];
				if (part.Modules.Contains("ModuleEngines"))
					rockets.Add(part);
			}

			for (int i = 0; i < rockets.Count; ++i)
			{
				List<ModuleEngines> engines = rockets[i].FindModulesImplementing<ModuleEngines>();
				for (int k = 0; k < engines.Count; ++k)
				{
					if (!engines[k].engineShutdown && engines[k].isOperational)
					{
						// Propellants used in ISP computation - what is not used is usually air
						for (int p = 0; p < engines[k].propellants.Count; ++p)
						{
							if (!engines[k].propellants[p].ignoreForIsp)
							{
								Fuel ir = propellants.Find(x => x.Name == engines[k].propellants[p].name);
								if (ir == null)
								{
									ir = new Fuel(engines[k].propellants[p]);
									propellants.Add(ir);
								}
								ir.FuelFlow += engines[k].getMaxFuelFlow(engines[k].propellants[p]) * engines[k].thrustPercentage / 100;
							}
						}
					}
				}
			}
		}

		private void checkSerenityPropellants()
		{
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
						for (int r = 0; r < rotors[k].resHandler.inputResources.Count; ++r)
						{
							// Skip Air
							if (rotors[k].resHandler.inputResources[r].name == "IntakeAir")
								continue;

							Fuel ir = propellants.Find(x => x.Name == rotors[k].resHandler.inputResources[r].name);
							if (ir == null)
							{
								ir = new Fuel(rotors[k].resHandler.inputResources[r]);
								propellants.Add(ir);
								this.knownPropellants.Add(ir.Name);
							}
							ir.FuelFlow += rotors[k].maxMotorOutput * 4 / 3 * rotors[k].baseResourceConsumptionRate * rotors[k].resHandler.inputResources[r].rate * rotors[k].servoMotorSize / 100;
						}
					}
				}
			}
		}

		internal override void Update(ref double deltaT, ref double deltaTOver)
		{
			for (int i = 0; i < propellants.Count; ++i)
			{
				propellants[i].CurrentAmountUsed += propellants[i].FuelFlow * deltaT;
				if (propellants[i].CurrentAmountUsed > propellants[i].MaximumAmountAvailable)
					deltaTOver = Math.Max(deltaTOver, (propellants[i].CurrentAmountUsed - propellants[i].MaximumAmountAvailable) / (propellants[i].FuelFlow));
			}
			if (deltaTOver > 0)
			{
				deltaT -= deltaTOver;
				// Reduce the amount of used propellants
				for (int i = 0; i < propellants.Count; ++i)
					propellants[i].CurrentAmountUsed -= propellants[i].FuelFlow * deltaTOver;
			}
		}

		internal override bool ProcessResources(IResourceBroker broker)
		{
			base.ProcessResources(broker);

			for (int i = 0; i < this.propellants.Count; ++i)
			{
				Fuel p = this.propellants[i];
				p.MaximumAmountAvailable -= broker.RequestResource(vessel.rootPart, p.Name, p.CurrentAmountUsed, 1, ResourceFlowMode.ALL_VESSEL);
				p.CurrentAmountUsed = 0;
				if (0 == p.MaximumAmountAvailable) return false;
			}

			return true;
		}
	}
}
