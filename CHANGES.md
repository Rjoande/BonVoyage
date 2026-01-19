# Bon Voyage :: CHANGES

* 2026-0118: 1.5.1.2 (LisiasT) for KSP >= 1.8
	+ Works around an **very** unfortunate intervention at my default by `BackgroudResourceProcessing`. Thanks to [Agartha-KSP](https://github.com/Agartha-KSP) for the report!
	+ Merges a fix on related to . Thanks to [LouisB3](https://github.com/LouisB3) for the fix!
	+ Closes Issues/PRs:
		+ [#34](https://github.com/net-lisias-ksp/BonVoyage/pull/34) Set PartUpgrades costs to zero to address VAB display issue.
		+ [#33](https://github.com/net-lisias-ksp/BonVoyage/issues/33) Exception & Bon Voyage & BackgroundResourceProcessing.
* 2025-1220: 1.5.1.1 (LisiasT) for KSP >= 1.8
	+ Promotes 1.5.1.0 codebase to production
		- Allows configuring the wheels's drive settings to do long journeys using less energy;
		- Automatically engages the Brakes when switching to a BV controlled rover;
		- Allows rovering using only Batteries, if you want to manually refill them when they are exhausted (just like The Martian).
	+ Works around an apparent idiosyncrasy on KSP where Vessels are instantiated before a Scene is properly loaded (like KSC at savegame loading).
		- Apparently it happens only when the Scenario is created (i.e., loaded by the fist time).
		- In a way or another, this hack will allows the thing to be used without Log Drama from this point. It worked on my test bed at least.
	+ Closes Issues:
		+ [#26](https://github.com/net-lisias-ksp/BonVoyage/issues/26) 1.5.1.0 Report Issue.
* 2025-1018: 1.5.1.0 (LisiasT) for KSP >= 1.8 PRE RELEASE
	+ The Martian edition.
	+ Allows configuring the wheels's drive settings to do long journeys using less energy;
	+ Automatically engages the Brakes when switching to a BV controlled rover;
	+ Allows rovering using only Batteries, if you want to manually refill them when they are exhausted (just like The Martian).
	+ Closes Issues:
		+ [#12](https://github.com/net-lisias-ksp/BonVoyage/issues/12) Cap the wheels power using the PAW limits instead of using the max values.
		+ [#7](https://github.com/net-lisias-ksp/BonVoyage/issues/7)Add something that would allow BV to behave like the rover on the movie The Martian
		+ [#3](https://github.com/net-lisias-ksp/BonVoyage/issues/3) Add an option to engage brakes when vessel is focused and BV is engaged
* 2024-1007: 1.5.0.0 (LisiasT) for KSP >= 1.8
	+ Initial version under LisiasT's stewardship. 
	+ KSP compatibility set (and guaranteed) down to 1.8.0
