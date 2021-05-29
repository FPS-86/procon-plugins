using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;

namespace PRoConEvents
{
    public class player
    {
        public string soldierName;
        public int teamid;
        public int squadnum;
        private bool active_flag = false;
        private double current_score = 0;
        private double previous_score = 0;
        private DateTime time_of_most_recent_score_change = DateTime.Now.AddHours(-1.00);
        private DateTime time_of_last_activity = DateTime.Now.AddHours(-1.0);
        public void update_activity_time()
        {
            // player did something that qualifies as being active, update the time_of_last_activity
            time_of_last_activity = DateTime.Now;
        }
        public double getMinutesSinceLastActivity()
        {
            TimeSpan dt = DateTime.Now - time_of_last_activity;
            return dt.TotalMinutes;
        }
        public void updateScore(double newscore, enumBoolYesNo use4idle)
        {
            if (newscore != current_score)
            {
                time_of_most_recent_score_change = DateTime.Now;
                if (use4idle == enumBoolYesNo.Yes)
                {
                    update_activity_time();
                }
            }
            previous_score = current_score;
            current_score = newscore;
        }
        public DateTime getTimeOfLastScoreChange() { return time_of_most_recent_score_change; }
        public double getMinutesSinceLastScoreChange()
        {
            TimeSpan dt = DateTime.Now - time_of_most_recent_score_change;
            return dt.TotalMinutes;
        }
    }

    public class Flys_IdleManager : PRoConPluginAPI, IPRoConPluginInterface
    {
        private List<player> myPlayerList;
        private List<string> whiteList;
        private string kick_reason = "You were kicked because you were idle too long.";
        private bool pluginenabled = false;
        private int debugLevel = 1;
        private double time_since_activity = 5;

        private enumBoolYesNo idleCheck_chat = enumBoolYesNo.Yes;
        private enumBoolYesNo idleCheck_score = enumBoolYesNo.Yes;
        private enumBoolYesNo idleCheck_spawn = enumBoolYesNo.Yes;
        private enumBoolYesNo simulate_kick = enumBoolYesNo.Yes;

        private double kick_idleTimeMin = 10.0;
        private int kick_minPlayerCount = 30;




        public Flys_IdleManager()
        {
            this.myPlayerList = new List<player>();
            this.whiteList = new List<string>();
            this.whiteList.Add("Flyswamper");
            this.whiteList.Add("Snizzicks");
        }
        public string GetPluginName()
        {
            return "Flys_IdleManager";
        }
        public string GetPluginVersion()
        {
            return "1.0.0.0";
        }
        public string GetPluginAuthor()
        {
            return "Flyswamper";
        }
        public string GetPluginWebsite()
        {
            return "www.phogue.net/forumvb/showthread.php?3992-BF3-BalanceServerSeeders(1-0-1-5-21-Apr-2012)-Distribute-server-seeding-players>BalanceServerSeeders";
        }
        public string GetPluginDescription()
        {
            return @"
<h2>Description</h2>
<p>Manage when to kick idle players</p>


<h2>Settings</h2>
<blockquote><h4>Debug_Level  &lt;value (0-10)&gt;</h4>This will increase/decrease the amount of information that gets written to the plugin console window.  A value of 0 (zero) should eliminate all output from the plugin.  A value of 10 provides maximum output.</blockquote>

<blockquote><h4>Idle Detection - Use Score changes to detect activity</h4>Use changes in players score to reset time of last activity</blockquote>

<blockquote><h4>Idle Detection - Use Spawn events to detect activity</h4>Use spawn events to reset time of last activity</blockquote>

<blockquote><h4>Idle Detection - Use Chat events to detect activity</h4>Use chat events to reset time of last activity</blockquote>

<blockquote><h4>Kick Details - Message displayed as reason for the kick</h4>Use this to customise the message the user will see when kicked.  Note that this message will be pre-pended with the number of minutes that this plugin thinks the player was idle so that the player can see how long he/she was idle</blockquote>

<blockquote><h4>Kick Thresholds - Minutes that Player must be idle to be kicked</h4>Enter the number of minutes that a player must have been idle to be eligible for getting kicked</blockquote>

<blockquote><h4>Kick Thresholds - Number of players on server for kicking to begin</h4>Enter the number of players that must be on the server in order for anyone to get kicked</blockquote>

<blockquote><h4>Simulate Only</h4>Set this to YES if you want to watch the the plugin console log of this plugin without actually kicking anyone, set it to NO to actually kick idle players</blockquote>

<blockquote><h4>White list</h4>Enter a list of soldier names that will be exempt from being kicked for idle,  one soldier name per line</blockquote>
";

        }
        public void OnPluginEnable()
        {
            this.pluginenabled = true;
            this.ExecuteCommand("procon.protected.pluginsonsole.write", "^bFlys_IdleManager ^2Enabled!");
        }
        public void OnPluginDisable()
        {
            this.pluginenabled = false;
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bFlys_IdleManager ^1Disabled =(");
        }
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bFlys_idleManager ^2Loaded!");
            this.RegisterEvents(this.GetType().Name, "OnPluginLoaded",
                                                    "OnPlayerJoin",
                                                    "OnListPlayers",
                                                    "OnPlayerLeft",
                                                    "OnPlayerSpawned",
                                                    "OnGlobalChat",
                                                    "OnTeamChat",
                                                    "OnSquadChat"
                                                    );
        }
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Simulate kicks (yes= debug mode, no = actually kick idle players|Simulate Only", typeof(enumBoolYesNo), this.simulate_kick));
            lstReturn.Add(new CPluginVariable("White list (soldier names one per line)|Whitelist", typeof(string[]), this.whiteList.ToArray()));
            lstReturn.Add(new CPluginVariable("Debug Controls (0-10 higher is more info)|debug_level", debugLevel.GetType(), debugLevel));
            lstReturn.Add(new CPluginVariable("Idle/Active detection|Use score changes to detect activity", typeof(enumBoolYesNo), this.idleCheck_score));
            lstReturn.Add(new CPluginVariable("Idle/Active detection|Use spawn events to detect activity", typeof(enumBoolYesNo), this.idleCheck_spawn));
            lstReturn.Add(new CPluginVariable("Idle/Active detection|Use chat events to detect activity", typeof(enumBoolYesNo), this.idleCheck_chat));
            lstReturn.Add(new CPluginVariable("Kick Thresholds|Minutes that player must be idle to be kicked?", this.kick_idleTimeMin.GetType(), this.kick_idleTimeMin));
            lstReturn.Add(new CPluginVariable("Kick Thresholds|Server player count to begin kicking idle players?", this.kick_minPlayerCount.GetType(), this.kick_minPlayerCount));
            lstReturn.Add(new CPluginVariable("Kick Reason (players idle time will be prepended to this kick message)|Message displayed as reason for the kick?", this.kick_reason.GetType(), this.kick_reason));

            return lstReturn;
        }
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Simulate Only", typeof(enumBoolYesNo), this.simulate_kick));
            lstReturn.Add(new CPluginVariable("Whitelist", typeof(string[]), this.whiteList.ToArray()));
            lstReturn.Add(new CPluginVariable("debug_level", debugLevel.GetType(), debugLevel));
            lstReturn.Add(new CPluginVariable("Use score changes to detect activity", typeof(enumBoolYesNo), this.idleCheck_score));
            lstReturn.Add(new CPluginVariable("Use spawn events to detect activity", typeof(enumBoolYesNo), this.idleCheck_spawn));
            lstReturn.Add(new CPluginVariable("Use chat events to detect activity", typeof(enumBoolYesNo), this.idleCheck_chat));
            lstReturn.Add(new CPluginVariable("Minutes that player must be idle to be kicked?", this.kick_idleTimeMin.GetType(), this.kick_idleTimeMin));
            lstReturn.Add(new CPluginVariable("Server player count to begin kicking idle players?", this.kick_minPlayerCount.GetType(), this.kick_minPlayerCount));
            lstReturn.Add(new CPluginVariable("Message displayed as reason for the kick?", this.kick_reason.GetType(), this.kick_reason));

            return lstReturn;
        }
        public void SetPluginVariable(string strVariable, string strValue)
        {
            writeMsgToPluginConsole(1, string.Format("User setting change requested.  strVariable: {0}, strValue: {1}", strVariable, strValue));
            int result = 0;
            double resultTime = 5;

            if ((strVariable.CompareTo("Simulate Only") == 0) && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.simulate_kick = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }


            if (strVariable.CompareTo("Whitelist") == 0)
            {
                this.whiteList = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            }
            if ((strVariable.CompareTo("debug_level") == 0) && int.TryParse(strValue, out result) == true)
            {
                if ((result >= 0) && (result <= 10))
                {
                    writeMsgToPluginConsole(1, string.Format("Setting debug_level to {0}", result));
                    debugLevel = result;
                }
                else
                {
                    writeMsgToPluginConsole(1, string.Format("{0} is not a valid debug level.", result));

                }
            }

            if ((strVariable.CompareTo("Number of minutes since last activity for player to be considered idle") == 0) && double.TryParse(strValue, out resultTime) == true)
            {
                writeMsgToPluginConsole(1, string.Format("Setting time_since_activity to {0}", resultTime));
                this.time_since_activity = resultTime;
            }
            if ((strVariable.CompareTo("Use score changes to detect activity") == 0) && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.idleCheck_score = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            if ((strVariable.CompareTo("Use spawn events to detect activity") == 0) && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.idleCheck_spawn = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            if ((strVariable.CompareTo("Use chat events to detect activity") == 0) && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.idleCheck_chat = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            if ((strVariable.CompareTo("Minutes that player must be idle to be kicked?") == 0) && double.TryParse(strValue, out resultTime) == true)
            {
                writeMsgToPluginConsole(1, string.Format("Setting time since activity for player to initiate kick to {0}", resultTime));
                this.kick_idleTimeMin = resultTime;
            }
            if ((strVariable.CompareTo("Server player count to begin kicking idle players?") == 0) && int.TryParse(strValue, out result) == true)
            {

                writeMsgToPluginConsole(1, string.Format("Setting player count for  kicking to begin at {0}", result));
                this.kick_minPlayerCount = result;
            }
            if (strVariable.CompareTo("Message displayed as reason for the kick?") == 0)
            {
                this.kick_reason = strValue;
            }


        }

        public void writeMsgToPluginConsole(int msgLevel, string message)
        {
            if (msgLevel <= debugLevel)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", string.Format("Flys_IdleManager: {0}", message));
            }
        }
        public void OnPlayerJoin(string strSoldierName)
        {
            writeMsgToPluginConsole(7, string.Format("{0} joined", strSoldierName.ToString()));
            bool newPlayerCheck = true;      // assume this player will need to be added to internal player list

            // but check to be sure player isn't already there due to possibility of onjoin event reporting getting delayed after playerlist event
            for (int ndxPlayer = 0; ndxPlayer < myPlayerList.Count; ndxPlayer++)
            {
                if (string.Compare(strSoldierName, myPlayerList[ndxPlayer].soldierName) == 0)
                {
                    // player already exists in the list. no need to add them
                    newPlayerCheck = false;
                }
            }
            if (newPlayerCheck == true)
            {
                player newPlayer = new player();
                newPlayer.soldierName = strSoldierName;
                newPlayer.update_activity_time();
                myPlayerList.Add(newPlayer);
            }
        }
        public void OnPlayerLeft(CPlayerInfo playerInfo)
        {
            writeMsgToPluginConsole(7, string.Format("{0} left", playerInfo.SoldierName.ToString()));

            // need to remove player from player list
            for (int ndxPlayer = 0; ndxPlayer < myPlayerList.Count; ndxPlayer++)
            {
                if (string.Compare(playerInfo.SoldierName, myPlayerList[ndxPlayer].soldierName) == 0)
                {
                    myPlayerList.RemoveAt(ndxPlayer);
                }
            }


        }
        public void kick_One_IdlePlayer()
        {
            // find an idle player and kick them

            int ndx_to_kick = -1;
            double max_minutes_of_inactivity = 0.0;

            for (int ndxPlayer = 0; ndxPlayer < myPlayerList.Count; ndxPlayer++)
            {
                bool kick_eligible = true;  //  initially assume this player is eligible for a kick

                for (int ndxWhiteList = 0; ndxWhiteList < whiteList.Count; ndxWhiteList++)
                {
                    if (string.Compare(whiteList[ndxWhiteList], myPlayerList[ndxPlayer].soldierName) == 0)
                    {
                        writeMsgToPluginConsole(9, string.Format("Player {0} is in whitelist, not eligible for kick", whiteList[ndxWhiteList]));
                        kick_eligible = false;              // this player isn't eligible for being kicked
                    }
                }
                double minutes_since_activity = myPlayerList[ndxPlayer].getMinutesSinceLastActivity();
                if (minutes_since_activity <= kick_idleTimeMin)
                {
                    writeMsgToPluginConsole(10, string.Format("Player {0} time since activity = {1}, not eligible for kick", myPlayerList[ndxPlayer].soldierName, minutes_since_activity));
                    kick_eligible = false;
                }

                if (kick_eligible == true)
                {
                    writeMsgToPluginConsole(5, string.Format("Player {0} time since activity = {1}, IS eligible for kick", myPlayerList[ndxPlayer].soldierName, minutes_since_activity));

                    if (minutes_since_activity > max_minutes_of_inactivity)     // found a player that has been inactive longer than previous found player
                    {
                        writeMsgToPluginConsole(2, string.Format("Player {0} tentatively selected for kick, looking for players with longer inactivity time", myPlayerList[ndxPlayer].soldierName, minutes_since_activity));
                        max_minutes_of_inactivity = minutes_since_activity;
                        ndx_to_kick = ndxPlayer;
                    }
                }



            }

            // at this point, *IF* the ndx_to_kick is >= 0 that means an eligible player was found and the ndx_to_kick points at the eligible player that has been idle the longest time

            writeMsgToPluginConsole(1, string.Format("Player {0} has been idle for {1} minutes and will be kicked", myPlayerList[ndx_to_kick].soldierName, max_minutes_of_inactivity));

            if (simulate_kick == enumBoolYesNo.No)
            {
                double idle_minutes = myPlayerList[ndx_to_kick].getMinutesSinceLastActivity();
                string output_message = string.Format("Idle {0} minutes, {1}", idle_minutes, kick_reason);
                this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", myPlayerList[ndx_to_kick].soldierName, output_message);
            }
        }
        public virtual void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
        {
            // loop through seeders to see if the player that spawned is a seeder, update activity status if there is a match
            if (idleCheck_spawn == enumBoolYesNo.Yes)
            {
                for (int ndxPlayer = 0; ndxPlayer < myPlayerList.Count; ndxPlayer++)
                {
                    if (string.Compare(soldierName, myPlayerList[ndxPlayer].soldierName) == 0)
                    {
                        writeMsgToPluginConsole(10, string.Format("Player {0} spawned, setting time since last activity to zero", soldierName));
                        myPlayerList[ndxPlayer].update_activity_time();
                    }
                }
            }
        }
        public virtual void OnGlobalChat(string speaker, string message)
        {
            // loop through players to find the one that typed in chat, update activity status if there is a match
            if (idleCheck_chat == enumBoolYesNo.Yes)
            {
                for (int ndxPlayer = 0; ndxPlayer < myPlayerList.Count; ndxPlayer++)
                {
                    if (string.Compare(speaker, myPlayerList[ndxPlayer].soldierName) == 0)
                    {
                        writeMsgToPluginConsole(10, string.Format("Player {0} typed in global chat, setting time since last activity to zero", speaker));
                        myPlayerList[ndxPlayer].update_activity_time();
                    }
                }
            }

        }
        public virtual void OnTeamChat(string speaker, string message, int teamId)
        {
            // loop through players to find the one that typed in chat, update activity status if there is a match
            if (idleCheck_chat == enumBoolYesNo.Yes)
            {
                for (int ndxPlayer = 0; ndxPlayer < myPlayerList.Count; ndxPlayer++)
                {
                    if (string.Compare(speaker, myPlayerList[ndxPlayer].soldierName) == 0)
                    {
                        writeMsgToPluginConsole(10, string.Format("Player {0} typed in team chat, setting time since last activity to zero", speaker));
                        myPlayerList[ndxPlayer].update_activity_time();
                    }
                }
            }
        }
        public virtual void OnSquadChat(string speaker, string message, int teamId, int squadId)
        {
            // find which player typed in chat and update their status
            if (idleCheck_chat == enumBoolYesNo.Yes)
            {
                for (int ndxPlayer = 0; ndxPlayer < myPlayerList.Count; ndxPlayer++)
                {
                    if (string.Compare(speaker, myPlayerList[ndxPlayer].soldierName) == 0)
                    {
                        writeMsgToPluginConsole(10, string.Format("Player {0} typed in squad chat, setting time since last activity to zero", speaker));
                        myPlayerList[ndxPlayer].update_activity_time();
                    }
                }
            }
        }
        public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset cpsSubset)
        {
            writeMsgToPluginConsole(1, "--------");

            // no kicking done here, just update the player list with scores and make sure all players are in the local player list

            // if debug level is high enough, list all players from OnListPlayers event and from local player list
            if (debugLevel >= 10)
            {
                string output_message = "List of Players from OnListPlayers event: ";
                for (int ndxPlayer = 0; ndxPlayer < players.Count; ndxPlayer++)
                {
                    output_message = string.Format("{0}{1},", output_message, players[ndxPlayer].SoldierName);
                }
                writeMsgToPluginConsole(10, output_message);

                output_message = "List of Players from Local Plugin player list: ";
                for (int ndxLocalPlayer = 0; ndxLocalPlayer < myPlayerList.Count; ndxLocalPlayer++)
                {
                    output_message = string.Format("{0}{1},", output_message, myPlayerList[ndxLocalPlayer].soldierName);
                }
                writeMsgToPluginConsole(10, output_message);
            }

            // loop through each player in the list from the server, updating the players score and status

            for (int ndxPlayer = 0; ndxPlayer < players.Count; ndxPlayer++)
            {
                bool match_found = false;
                // loop through the local player list looking for a match
                for (int ndxLocalPlayer = 0; ndxLocalPlayer < myPlayerList.Count; ndxLocalPlayer++)
                {
                    if (string.Compare(myPlayerList[ndxLocalPlayer].soldierName, players[ndxPlayer].SoldierName) == 0)
                    {
                        writeMsgToPluginConsole(10, string.Format("Updating score for player: {0}", players[ndxPlayer].SoldierName));
                        myPlayerList[ndxLocalPlayer].updateScore(players[ndxPlayer].Score, idleCheck_score);
                        match_found = true;
                    }
                }

                if (match_found == false)
                {
                    // player reported from server wasn't found in local list, need to add them
                    writeMsgToPluginConsole(2, string.Format(" Player {0} showed up in OnListPlayers event but wasn't already in this plugins list of players.  Adding to local player list", players[ndxPlayer].SoldierName));
                    player newPlayer = new player();
                    newPlayer.soldierName = players[ndxPlayer].SoldierName;
                    newPlayer.update_activity_time();
                    newPlayer.updateScore(players[ndxPlayer].Score, idleCheck_score);
                    myPlayerList.Add(newPlayer);
                }
            }


            // lastly check that we don't have any "ghosted" players in the local player list
            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                // if all players were reported, then everyone in the local list should be present.  If they aren't, we need to remove them from the local list

                // loop through the local list and verify they exist in the player list received in the onListplayers event

                for (int ndxLocalPlayer = 0; ndxLocalPlayer < myPlayerList.Count; ndxLocalPlayer++)
                {
                    bool player_found = false;      // assume they aren't found initially
                    for (int ndxServerPlayer = 0; ndxServerPlayer < players.Count; ndxServerPlayer++)
                    {
                        if (string.Compare(players[ndxServerPlayer].SoldierName, myPlayerList[ndxLocalPlayer].soldierName) == 0)
                        {
                            // match found
                            player_found = true;
                            ndxServerPlayer = players.Count;        // stop looking for a match!
                        }
                    }
                    if (player_found == false)
                    {
                        // need to remove this ghosted player
                        writeMsgToPluginConsole(1, string.Format("Player {0} found in local player list but wasn't present in onlistplayers event from server, deleting this ghosted player from local player list", myPlayerList[ndxLocalPlayer].soldierName));
                        myPlayerList.RemoveAt(ndxLocalPlayer);
                    }

                }



            }


            // check player count against threshold set to begin kicking seeders
            if (players.Count >= this.kick_minPlayerCount)
            {
                writeMsgToPluginConsole(5, string.Format("Player count ({0}) exceeds threshold ({1}) to kick players, looking for an idle player to kick", players.Count, this.kick_minPlayerCount));
                // try and find a idle player that can be kicked 
                kick_One_IdlePlayer();
            }
            else
            {
                writeMsgToPluginConsole(6, string.Format("Player count({0}) is below threshold ({1}) for kicking players, do nothing...", players.Count, this.kick_minPlayerCount));
            }

        }
    }
}
