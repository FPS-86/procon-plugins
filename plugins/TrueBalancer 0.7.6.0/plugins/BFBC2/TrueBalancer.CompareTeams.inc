if (strcurrentGametype == "rush" || strcurrentGametype == "conquest" || strcurrentGametype == "sqrush")
{
	CompareTeams();
}
else if (strcurrentGametype != "")
{
	if (this.boolgametype == false)
	{
		this.boolgametype = true;
		this.ExecuteCommand("procon.protected.pluginconsole.write", "TrueBalancer:  Still no data or GameMode not supported: " + "I" + strcurrentGametype + "I" );
		
	}
	
}