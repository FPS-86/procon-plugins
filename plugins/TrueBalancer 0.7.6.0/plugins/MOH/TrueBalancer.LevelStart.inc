public override void OnLevelStarted() {

	this.DebugInfo("New Level started");
	this.DebugInfo(strcurrentGametype);
	this.DTLevelStart = DateTime.Now;
	this.boolRoundOver = false;
	
	this.ExecuteCommand("procon.protected.tasks.add", "TrueBalancerLoadStartWait", "15", "1", "1",  "procon.protected.plugins.call", "TrueBalancer", "OnLevelStartedWaitCallback");
	
}

public void OnLevelStartedWaitCallback() {
    this.boolLevelStart = true;
	this.DebugInfo("Round Start - By Timer (MoH)!");
	// if (this.boolFirstOP)
	// {
		// CompareTeams();
	// }
	// else
	// {
		// this.DebugInfo("LevelStartTimer: Waiting for fist OP!");
	// }
}

public override void OnPlayerKilled(Kill kKillerVictimDetails) 
{	

	if (this.boolLevelStart == false)
	{
		this.DebugInfo("First Player Killed");
		this.ExecuteCommand("procon.protected.tasks.add", "TrueBalancerLoadStartWait", "1", "1", "1",  "procon.protected.plugins.call", "TrueBalancer", "OnLevelStartedWaitCallback");

	}
	
}