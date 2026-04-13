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


namespace BonVoyage.PowerSources
{
	internal class ElectricityPoweredConverter : Converter
	{
		internal ElectricityPoweredConverter(Vessel vessel) : base(vessel)
		{
		}

		internal override void Read(ConfigNode controllerNode)
		{
			ConfigNode subNode = controllerNode.GetNode("FUEL_CELLS");
			if (null == subNode) return;

			this.Use = Convert.ToBoolean(subNode.GetValue("useFuelCells"));
			this.OutputValue = Convert.ToDouble(subNode.GetValue("outputEC"));
			ConfigNode[] resources = subNode.GetNodes("RESOURCE");
			this.InputResources.Clear();
			this.knownInputResources.Clear();
			for (int r = 0; r < resources.Length; ++r)
			{
				Resource ir = new Resource(resources[r]);
				this.InputResources.Add(ir);
				this.knownInputResources.Add(ir.Name);
			}
		}

		internal override void Write(ConfigNode controllerNode)
		{
			ConfigNode subNode = new ConfigNode("FUEL_CELLS");

			subNode.AddValue("useFuelCells", this.Use);
			subNode.AddValue("outputEC", this.OutputValue);
			List<Resource> res = this.InputResources;
			ConfigNode resourceNode;
			for (int r = 0; r < res.Count; ++r)
			{
				resourceNode = new ConfigNode("RESOURCE");
				resourceNode.AddValue("name", res[r].Name);
				resourceNode.AddValue("ratio", res[r].Ratio);
				resourceNode.AddValue("maximumAmount", res[r].MaximumAmountAvailable);
				resourceNode.AddValue("currentAmount", res[r].CurrentAmountUsed);
				subNode.AddNode(resourceNode);
			}

			controllerNode.AddNode(subNode);
		}

		internal override void Check(double throttle)
		{
			// Get available EC from fuell cells
			this.OutputValue = 0;
			this.InputResources.Clear();
			this.knownInputResources.Clear();
			if (!this.Use) return;

			this.checkPowerCells(throttle);
		}

		private void checkPowerCells(double throttle)
		{
			List<ModuleResourceConverter> mrc = vessel.FindPartModulesImplementing<ModuleResourceConverter>();
			for (int i = 0; i < mrc.Count; ++i)
			{
				double ecRatio = 0;
				try
				{
					ResourceRatio ec = mrc[i].outputList.Find(x => x.ResourceName == "ElectricCharge"); // NullArgumentException when not found
					ecRatio = ec.Ratio;
				}
				catch (Exception e)
				{
					Log.dbg(e);
				}

				if (ecRatio > 0)
				{
					// Add input resources
					List<ResourceRatio> iList = mrc[i].inputList;
					for (int r = 0; r < iList.Count; ++r)
					{
						// Check if we have fuel for converter. If not, then continue without adding output ratio.
						if (!CheatOptions.InfinitePropellant && r == 0)
						{
							if (this.resourceBroker.AmountAvailable(vessel.rootPart, iList[r].ResourceName, 1, ResourceFlowMode.ALL_VESSEL) == 0)
								break;
						}

						Resource ir = this.InputResources.Find(x => x.Name == iList[r].ResourceName);
						if (ir == null)
						{
							ir = new Resource(iList[r]);
							this.InputResources.Add(ir);
							this.knownInputResources.Add(ir.Name);
						}
						ir.Ratio += iList[r].Ratio;

						// Add EC ration to output
						if (r == 0)
							this.OutputValue += ecRatio;
					}
				}
			}
		}

		internal override double GetAvailablePower()
		{
			double otherPower = 0;

			// Go through all parts and get power from generators and reactors
			for (int i = 0; i < this.vessel.parts.Count; ++i)
			{
				Part part = this.vessel.parts[i];

				// Standard RTG
				ModuleGenerator powerModule = part.FindModuleImplementing<ModuleGenerator>();
				if (powerModule != null)
				{
					if (powerModule.generatorIsActive || powerModule.isAlwaysActive)
					{
						// Go through resources and get EC power
						for (int j = 0; j < powerModule.resHandler.outputResources.Count; ++j)
						{
							ModuleResource resource = powerModule.resHandler.outputResources[j];
							if (resource.name == "ElectricCharge")
								otherPower += resource.rate * powerModule.efficiency;
						}
					}
				}

				// Other generators
				PartModuleList modules = part.Modules;
				for (int j = 0; j < modules.Count; ++j)
				{
					PartModule module = modules[j];

					// Near future fission reactors
					if (module.moduleName == "FissionGenerator")
						otherPower += double.Parse(module.Fields.GetValue("CurrentGeneration").ToString());
					// NFE + System Heat
					if (module.moduleName == "ModuleSystemHeatFissionReactor")
						otherPower += double.Parse(module.Fields.GetValue("CurrentElectricalGeneration").ToString());

					// KSP Interstellar generators
					if ((module.moduleName == "ThermalElectricEffectGenerator") || (module.moduleName == "IntegratedThermalElectricPowerGenerator") || (module.moduleName == "ThermalElectricPowerGenerator")
						|| (module.moduleName == "IntegratedChargedParticlesPowerGenerator") || (module.moduleName == "ChargedParticlesPowerGenerator") || (module.moduleName == "FNGenerator"))
					{
						if (bool.Parse(module.Fields.GetValue("IsEnabled").ToString()))
						{
							//otherPower += double.Parse(module.Fields.GetValue("maxElectricdtps").ToString()); // Doesn't work as expected

							string maxPowerStr = module.Fields.GetValue("MaxPowerStr").ToString();
							double maxPower = 0;

							if (maxPowerStr.Contains("GW"))
								maxPower = double.Parse(maxPowerStr.Replace(" GW", "")) * 1000000;
							else if (maxPowerStr.Contains("MW"))
								maxPower = double.Parse(maxPowerStr.Replace(" MW", "")) * 1000;
							else
								maxPower = double.Parse(maxPowerStr.Replace(" KW", ""));

							otherPower += maxPower;
						}
					}
				}

				// WBI reactors, USI reactors and MKS Power Pack
				ModuleResourceConverter converterModule = part.FindModuleImplementing<ModuleResourceConverter>();
				if (converterModule != null)
				{
					if (converterModule.ModuleIsActive()
						&& ((converterModule.ConverterName == "Nuclear Reactor") || (converterModule.ConverterName == "Reactor") || (converterModule.ConverterName == "Generator")))
					{
						for (int j = 0; j < converterModule.outputList.Count; ++j)
						{
							ResourceRatio resource = converterModule.outputList[j];
							if (resource.ResourceName == "ElectricCharge")
								otherPower += resource.Ratio * converterModule.GetEfficiencyMultiplier();
						}
					}
				}
			}

			return otherPower;
		}

		internal override void Update(ref double deltaT, ref double deltaTOver)
		{
			List<Resource> iList = this.InputResources;
			for (int i = 0; i < iList.Count; ++i)
			{
				iList[i].CurrentAmountUsed += iList[i].Ratio * deltaT;
				if (iList[i].CurrentAmountUsed > iList[i].MaximumAmountAvailable)
					deltaTOver = Math.Max(deltaTOver, (iList[i].CurrentAmountUsed - iList[i].MaximumAmountAvailable) / iList[i].Ratio);
			}

			if (deltaTOver > 0)
			{
				deltaT -= deltaTOver;
				// Reduce the amount of used resources
				for (int i = 0; i < iList.Count; ++i)
					iList[i].CurrentAmountUsed -= iList[i].Ratio * deltaTOver;
			}
		}

		internal override bool CheckResources(IResourceBroker broker)
		{
			if (!this.Use) return true;	// Cheat the caller on thinking there're resorces available.

			bool r = true;
			Log.dbg("CheckResources found {0} resources", this.InputResources.Count);
			for (int i = 0; i < this.InputResources.Count; ++i)
			{
				Resource p = this.InputResources[i];

				// Exactly why we need to do this here?
				p.MaximumAmountAvailable = broker.AmountAvailable(vessel.rootPart, p.Name, 1, ResourceFlowMode.ALL_VESSEL);

				Log.dbg("CheckResources {0} {1}", p.Name, p.MaximumAmountAvailable);
				r &= (p.MaximumAmountAvailable > 0);
			}

			return r;
		}

		internal override bool ProcessResources(IResourceBroker broker)
		{
			if (this.Use)
			{
				List<Resource> iList = this.InputResources;
				for (int i = 0; i < iList.Count; ++i)
				{
					iList[i].MaximumAmountAvailable -= broker.RequestResource(vessel.rootPart, iList[i].Name, iList[i].CurrentAmountUsed, 1, ResourceFlowMode.ALL_VESSEL);
					iList[i].CurrentAmountUsed = 0;
				}
			}

			return true;
		}
	}
}
