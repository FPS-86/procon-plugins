/* SevLeague.cs

by Sev

Free to use as is in any way you want with no warranty.

*/

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections;
using System.Net;
using System.Web;
using System.Data;
using System.Threading;
using System.Timers;
using System.Diagnostics;
using System.Reflection;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;


namespace PRoConEvents
{

    //Aliases
    using EventType = PRoCon.Core.Events.EventType;
    using CapturableEvent = PRoCon.Core.Events.CapturableEvents;

    public class SevLeague : PRoConPluginAPI, IPRoConPluginInterface
    {

        /* Inherited:
            this.PunkbusterPlayerInfoList = new Dictionary<string, CPunkbusterInfo>();
            this.FrostbitePlayerInfoList = new Dictionary<string, CPlayerInfo>();
        */

        private bool fIsEnabled;
        private enumBoolYesNo mIsKick;
        private int fDebugLevel;
        private string fLeader = "Leader";
        private string fLeaderClan = "LeaderClan";
        private string fChallengerClan = "ChallengerClan";
        private string fSolToMove = "";
        private string fSolMoved = "";
        private int fIsLeaderOn;            // 0 = offline   1 = online-passive		2 - online-active
        private int fLeaderTeamId;
        private int fChallengerTeamId;


        private string fclanTAG;


        public SevLeague()
        {
            fIsEnabled = false;
            fDebugLevel = 1;
            fIsLeaderOn = 0;
            this.mIsKick = enumBoolYesNo.Yes;
        }

        public enum MessageType { Warning, Error, Exception, Normal };

        public String FormatMessage(String msg, MessageType type)
        {
            String prefix = "[^bSev League^n] ";

            if (type.Equals(MessageType.Warning))
                prefix += "^1^bWARNING^0^n: ";
            else if (type.Equals(MessageType.Error))
                prefix += "^1^bERROR^0^n: ";
            else if (type.Equals(MessageType.Exception))
                prefix += "^1^bEXCEPTION^0^n: ";

            return prefix + msg;
        }


        public void LogWrite(String msg)
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", msg);
        }

        public void ConsoleWrite(string msg, MessageType type)
        {
            LogWrite(FormatMessage(msg, type));
        }

        public void ConsoleWrite(string msg)
        {
            ConsoleWrite(msg, MessageType.Normal);
        }

        public void ConsoleWarn(String msg)
        {
            ConsoleWrite(msg, MessageType.Warning);
        }

        public void ConsoleError(String msg)
        {
            ConsoleWrite(msg, MessageType.Error);
        }

        public void ConsoleException(String msg)
        {
            ConsoleWrite(msg, MessageType.Exception);
        }

        public void DebugWrite(string msg, int level)
        {
            if (fDebugLevel >= level) ConsoleWrite(msg, MessageType.Normal);
        }


        public void ServerCommand(params String[] args)
        {
            List<string> list = new List<string>();
            list.Add("procon.protected.send");
            list.AddRange(args);
            this.ExecuteCommand(list.ToArray());
        }


        public string GetPluginName()
        {
            return "Sev League";
        }

        public string GetPluginVersion()
        {
            return "0.0.0.2";
        }

        public string GetPluginAuthor()
        {
            return "Sev";
        }

        public string GetPluginWebsite()
        {
            return "";
        }

        public string GetPluginDescription()
        {
            return @"
<h1>Sev League Clan Balancer</h1>
<p>Autobalance players</p>

<h2>Description</h2>
<p>This plugin search for a leader and move all other players with the leader clan tag in the leader team</p>
<p>This plugin also move all players with challenger clan tag to the opposite team of the leader</p>
<p>All players with clanTAG differs from leader or challenger can be automatically kicked</p>

<h2>Commands</h2>
<p>In order to Enable balancing, the leader must send the keyword '@slbal' in the global ingame chat</p>
<p>Another keyword can be use to enable/disable the kick for unknown clan Tag : '@slkick'</p>

<h2>Settings</h2>
<p>You have to set de leader soldier name, the leade clan TAG and the challenger clan TAG</p>
<p>You can activate the kicking option</p>

<h2>Development</h2>
<p>Sev</p>
<h3>Changelog</h3>
<blockquote><h4>0.0.0.2 (15-FEV-2016)</h4>
	- You can disable he kick function<br/>
</blockquote>
<blockquote><h4>0.0.0.1 (14-FEV-2016)</h4>
	- initial version<br/>
</blockquote>
";
        }




        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Sev League|Leader Soldier", fLeader.GetType(), fLeader));
            lstReturn.Add(new CPluginVariable("Sev League|Leader clan Tag", fLeaderClan.GetType(), fLeaderClan));
            lstReturn.Add(new CPluginVariable("Sev League|Challenger clan Tag", fChallengerClan.GetType(), fChallengerClan));
            lstReturn.Add(new CPluginVariable("Sev League|Kick other clan Tag", typeof(enumBoolYesNo), this.mIsKick));
            lstReturn.Add(new CPluginVariable("Sev League|Debug level", fDebugLevel.GetType(), fDebugLevel));

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Leader Soldier", fLeader.GetType(), fLeader));
            lstReturn.Add(new CPluginVariable("Leader clan Tag", fLeaderClan.GetType(), fLeaderClan));
            lstReturn.Add(new CPluginVariable("Challenger clan Tag", fChallengerClan.GetType(), fChallengerClan));
            lstReturn.Add(new CPluginVariable("Kick other clan Tag", typeof(enumBoolYesNo), this.mIsKick));
            lstReturn.Add(new CPluginVariable("Debug level", fDebugLevel.GetType(), fDebugLevel));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {

            if (strVariable.CompareTo("Leader Soldier") == 0) fLeader = strValue;

            else if (strVariable.CompareTo("Leader clan Tag") == 0) fLeaderClan = strValue;

            else if (strVariable.CompareTo("Challenger clan Tag") == 0) fChallengerClan = strValue;

            else if (strVariable.CompareTo("Kick other clan Tag") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.mIsKick = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }

            else if (strVariable.CompareTo("Debug level") == 0 && int.TryParse(strValue, out fDebugLevel) == true)
            {
                int.TryParse(strValue, out fDebugLevel);
            }

        }


        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name, "OnListPlayers", "OnPlayerLeft", "OnPlayerTeamChange", "OnGlobalChat");
        }

        public void OnPluginEnable()
        {
            fIsEnabled = true;
            ConsoleWrite("Enabled!");
        }

        public void OnPluginDisable()
        {
            fIsEnabled = false;
            ConsoleWrite("Disabled!");
        }


        public override void OnVersion(string serverType, string version) { }

        public override void OnServerInfo(CServerInfo serverInfo)
        {
            // ConsoleWrite("Debug level = " + fDebugLevel);
        }

        public override void OnResponseError(List<string> requestWords, string error) { }

        public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset)
        {

            if (fSolToMove != "")
            {                   // if there is a specific soldier to balance...
                foreach (CPlayerInfo cpiPlayer in players)
                {   // ...for each player...
                    if (cpiPlayer.SoldierName == fSolToMove)
                    {   // ...but in fact only the Soldier To Move...		
                        fclanTAG = cpiPlayer.ClanTag;           // ...read his clan TAG...
                        if (fclanTAG == fLeaderClan)
                        {           // ..if this is the leader clan tag...
                            if (cpiPlayer.TeamID != fLeaderTeamId)
                            {   // ..and he is not in the leader team...
                                fSolMoved = cpiPlayer.SoldierName;
                                this.ExecuteCommand("procon.protected.send", "admin.movePlayer", cpiPlayer.SoldierName, fLeaderTeamId.ToString(), "0", "true");
                                this.ExecuteCommand("procon.protected.send", "admin.say", "AUTOBALANCE : \"" + cpiPlayer.SoldierName + "\" moved to Leader Team :" + fLeaderTeamId, "all");
                                DebugWrite("OnListPlayers SPEC: \"" + cpiPlayer.SoldierName + "\" moved to team " + fLeaderTeamId, 1);  // ..move him
                            }
                            else DebugWrite("OnListPlayers SPEC: \"" + cpiPlayer.SoldierName + "\" is allready in the leader team", 2);

                        }
                        else if (fclanTAG == fChallengerClan)
                        {   // ..if this is the challenger clan tag...
                            if (cpiPlayer.TeamID != fChallengerTeamId)
                            {   // ..and he is not in the challenger team...
                                fSolMoved = cpiPlayer.SoldierName;
                                this.ExecuteCommand("procon.protected.send", "admin.movePlayer", cpiPlayer.SoldierName, fChallengerTeamId.ToString(), "0", "true");
                                this.ExecuteCommand("procon.protected.send", "admin.say", "AUTOBALANCE : \"" + cpiPlayer.SoldierName + "\" moved to Challenger Team : " + fChallengerTeamId, "all");
                                DebugWrite("OnListPlayers SPEC: \"" + cpiPlayer.SoldierName + "\" moved to team " + fChallengerTeamId, 1);  // ..move him
                            }
                            else DebugWrite("OnListPlayers SPEC: \"" + cpiPlayer.SoldierName + "\" is allready in the challenger team", 2);
                        }
                        else
                        {                                   // ..if his clan TAG is neither Leader or Challgner..
                            DebugWrite("OnListPlayers SPEC: \"" + cpiPlayer.SoldierName + "\" is kicked for invalid clan TAG \"" + fclanTAG + "\"", 1);
                            this.ExecuteCommand("procon.protected.send", "admin.say", "KICK : \"" + cpiPlayer.SoldierName + "\" / plaque de clan invalide : \"" + fclanTAG + "\"", "all"); // ..just kick him
                            this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", cpiPlayer.SoldierName, "Plaque de clan non valide : \"" + fclanTAG + "\"");
                        }
                        fSolToMove = "";            // blank this specific field to let the next OnListPlayers run normally
                    }
                }
            }
            else if (fIsLeaderOn == 2)
            {               // if the leader is Online AND Active
                foreach (CPlayerInfo cpiPlayer in players)
                {   // ...for each player...
                    if (cpiPlayer.SoldierName != fLeader)
                    {       // ...except the leader...
                        fclanTAG = cpiPlayer.ClanTag;           // ...read his clan TAG...
                        if (fclanTAG == fLeaderClan)
                        {           // ..if this is the leader clan tag...
                            if (cpiPlayer.TeamID != fLeaderTeamId)
                            {   // ..and he is not in the leader team...
                                fSolMoved = cpiPlayer.SoldierName;
                                this.ExecuteCommand("procon.protected.send", "admin.movePlayer", cpiPlayer.SoldierName, fLeaderTeamId.ToString(), "0", "true");
                                this.ExecuteCommand("procon.protected.send", "admin.say", "AUTOBALANCE : \"" + cpiPlayer.SoldierName + "\" moved to Leader Team :" + fLeaderTeamId, "all");
                                DebugWrite("OnListPlayers GLOB: \"" + cpiPlayer.SoldierName + "\" moved to team " + fLeaderTeamId, 1);  // ..move him
                            }
                            else DebugWrite("OnListPlayers GLOB: \"" + cpiPlayer.SoldierName + "\" is allready in the leader team", 2);

                        }
                        else if (fclanTAG == fChallengerClan)
                        {   // ..if this is the challenger clan tag...
                            if (cpiPlayer.TeamID != fChallengerTeamId)
                            {   // ..and he is not in the challenger team...
                                fSolMoved = cpiPlayer.SoldierName;
                                this.ExecuteCommand("procon.protected.send", "admin.movePlayer", cpiPlayer.SoldierName, fChallengerTeamId.ToString(), "0", "true");
                                this.ExecuteCommand("procon.protected.send", "admin.say", "AUTOBALANCE : \"" + cpiPlayer.SoldierName + "\" moved to Challenger Team : " + fChallengerTeamId, "all");
                                DebugWrite("OnListPlayers GLOB: \"" + cpiPlayer.SoldierName + "\" moved to team " + fChallengerTeamId, 1);  // ..move him
                            }
                            else DebugWrite("OnListPlayers GLOB: \"" + cpiPlayer.SoldierName + "\" is allready in the challenger team", 2);
                        }
                        else if (this.mIsKick == enumBoolYesNo.Yes)
                        {                                   // ..if his clan TAG is neither Leader or Challgner..
                            DebugWrite("OnListPlayers GLOB: \"" + cpiPlayer.SoldierName + "\" is kicked for invalid clan TAG \"" + fclanTAG + "\"", 1);
                            this.ExecuteCommand("procon.protected.send", "admin.say", "KICK : \"" + cpiPlayer.SoldierName + "\" / plaque de clan invalide : \"" + fclanTAG + "\"", "all"); // ..just kick him
                            this.ExecuteCommand("procon.protected.send", "admin.kickPlayer", cpiPlayer.SoldierName, "Plaque de clan non valide : \"" + fclanTAG + "\"");
                            //this.ExecuteCommand("procon.protected.tasks.add", "CPingKicker", this.m_iDelayBetweenMessageAndKick.ToString(), "1", "1", "procon.protected.send", "admin.kickPlayer", cpiPlayer.SoldierName, "Plaque de clan non valide : \"" + fclanTAG + "\"");
                        }
                    }
                }
            }
            else if (fIsLeaderOn == 0)
            {               // if the leader seems offline...
                foreach (CPlayerInfo cpiPlayer in players)
                {   // ...for each player...
                    if (cpiPlayer.SoldierName == fLeader)
                    {   // ...but in fact only the Soldier To Move...		
                        DebugWrite("OnListPlayers: Leader \"" + fLeader + "\" found in Team : " + cpiPlayer.TeamID + " ...", 1);
                        fLeaderTeamId = cpiPlayer.TeamID;                           // ...Set the right team ID for each clan...
                        if (fLeaderTeamId == 1) fChallengerTeamId = 2;
                        else if (fLeaderTeamId == 2) fChallengerTeamId = 1;
                        else ConsoleError("LeaderTeam not equal to 0 or 1");
                        fIsLeaderOn = 1;
                    }
                }
            }
        }

        public override void OnPlayerJoin(string soldierName)
        {

        }

        public override void OnPlayerLeft(CPlayerInfo playerInfo)
        {

            if (playerInfo.SoldierName == fLeader)
            {
                DebugWrite("OnPlayerLeft: Leader \"" + fLeader + "\" has left", 1);
                fIsLeaderOn = 0;
            }

        }

        public override void OnPlayerKilled(Kill kKillerVictimDetails) { }

        public override void OnPlayerSpawned(string soldierName, Inventory spawnedInventory) { }

        public override void OnPlayerTeamChange(string soldierName, int teamId, int squadId)
        {

            if (soldierName == fLeader)
            {                           // If the changing player is the leader...
                DebugWrite("OnPlayerTeamChange: Leader \"" + fLeader + "\" arrived in Team : " + teamId + " ...", 1);
                fLeaderTeamId = teamId;                             // ...Set the right team ID for each clan...
                if (fLeaderTeamId == 1) fChallengerTeamId = 2;
                else if (fLeaderTeamId == 2) fChallengerTeamId = 1;
                else ConsoleError("LeaderTeam not equal to 0 or 1");
                if (fIsLeaderOn == 0) fIsLeaderOn = 1;              // ...If leader was offline, set it online-passive...
                if (fIsLeaderOn == 2)
                {                               // ...If leader is active, retrigger PlayerList to balance every soldier...
                    DebugWrite("	...Retrigger PlayerList", 1);
                    this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
                }
            }
            else
            {
                if (fIsLeaderOn == 2)
                {                               // If the changing player is not the leader, but the leader is active...
                    if (fSolMoved != "") fSolMoved = "";
                    else
                    {
                        fSolToMove = soldierName;                       // ...fill the specific soldier to move...
                        DebugWrite("OnPlayerTeamChange: triggering specific playerList cause \"" + soldierName + "\" arrived in Team : " + teamId, 1);
                        this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");       // ...retrigger list player to balance THIS soldier.		
                    }
                }
            }
        }

        public override void OnGlobalChat(string speaker, string message)
        {
            if (speaker == fLeader)
            {   // If the leader is telling talking...
                if (message == "@slbal")
                {       // ...with the balance keyword...
                    if (fIsLeaderOn == 1)
                    {                               // ...and the current statut is passive...
                        fIsLeaderOn = 2;                                    // ...set it Online-Active...
                        DebugWrite("OnGlobalChat: ENABLE balance", 1);
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Activate Sev League balancing...", "all");
                        this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");       // ...and retrigger list player to balance all soldiers.
                    }
                    else if (fIsLeaderOn == 2)
                    {                               // ...but if the current statut is active...
                        fIsLeaderOn = 1;                                    // ...set it Online-Passive...
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Sev League Balancing Disabled", "all");
                        DebugWrite("OnGlobalChat: DISABLE balance", 1);
                    }
                }

                if (message == "@slkick")
                {       // ...with the balance kick...
                    if (this.mIsKick == enumBoolYesNo.No)
                    {                               // ...and the current statut is disabled...
                        this.mIsKick = enumBoolYesNo.Yes;                                   // ...set it Active...
                        DebugWrite("OnGlobalChat: ENABLE kick", 1);
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Activate Sev League kicking for unknown clan Tag...", "all");
                    }
                    else if (this.mIsKick == enumBoolYesNo.Yes)
                    {                               // ...but if the current statut is active...
                        this.mIsKick = enumBoolYesNo.No;                                    // ...disable it...
                        this.ExecuteCommand("procon.protected.send", "admin.say", "Sev League kicking Disabled", "all");
                        DebugWrite("OnGlobalChat: DISABLE kick", 1);
                    }

                }


            }

        }

        public override void OnTeamChat(string speaker, string message, int teamId) { }

        public override void OnSquadChat(string speaker, string message, int teamId, int squadId) { }

        public override void OnRoundOverPlayers(List<CPlayerInfo> players) { }

        public override void OnRoundOverTeamScores(List<TeamScore> teamScores) { }

        public override void OnRoundOver(int winningTeamId) { }

        public override void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal) { }

        public override void OnLevelStarted() { }

        public override void OnLevelLoaded(string mapFileName, string Gamemode, int roundsPlayed, int roundsTotal) { } // BF3


    } // end SevLeague

} // end namespace PRoConEvents



