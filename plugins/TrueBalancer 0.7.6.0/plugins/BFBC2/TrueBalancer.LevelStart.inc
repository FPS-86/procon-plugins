public override void OnLevelStarted() {

	this.DebugInfo("New Level started");
	this.DebugInfo(strcurrentGametype);
	this.DTLevelStart = DateTime.Now;
	this.boolRoundOver = false;
	
}

public override void OnPlayerSpawned(string soldierName, Inventory spawnedInventory) {

	//this.DebugInfo("Player " + soldierName + " spawned.");
	if (this.boolLevelStart == false)
	{
		//this.TSWait = DateTime.Now - this.DTLevelStart;
		if (this.boolRoundOver)
		{
			this.DebugInfo("RoundOver - Spawn not counting!");
		}
		else
		{
			this.boolLevelStart = true;
			this.DebugInfo("Round Start - First Spawner!");
			// if (this.boolFirstOP)
			// {
				
				// if (strcurrentGametype == "rush" || strcurrentGametype == "conquest")
				// {
					// CompareTeams();
				// }
				// else if (strcurrentGametype != "")
				// {
					// this.ExecuteCommand("procon.protected.pluginconsole.write", "TrueBalancer:  Still no data or GameMode not supported: " + "I" + strcurrentGametype + "I" );
				// }
			// }
			// else
			// {
				// this.DebugInfo("Firstspawner: Waiting for fist OP!");
			// }
		}
	}
}