if(this.strcurrentGametype=="sqrush"){
this.ExecuteCommand("procon.protected.send", "admin.movePlayer", this.strMovedPlayer, this.intToTeam.ToString(), "1", "true");
}else{
this.ExecuteCommand("procon.protected.send", "admin.movePlayer", this.strMovedPlayer, this.intToTeam.ToString(), "0", "true");
}