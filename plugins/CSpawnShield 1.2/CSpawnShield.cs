using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;

namespace PRoConEvents
{
    public class CSpawnShield : PRoConPluginAPI, IPRoConPluginInterface
    {

        #region Plugin VARS & INIT
        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private int I_SpawnShieldTime;
        private Dictionary<string, int> L_SpawnShield = new Dictionary<string, int>();
        private string S_KillText;
        private int I_TimeTillKill;


        public CSpawnShield()
        {
            this.I_SpawnShieldTime = 3;
            this.S_KillText = "You are not allowed to do SpawnKill. You will DIE!";
            this.I_TimeTillKill = 5;
        }
        #endregion

        #region Plugin details
        public string GetPluginName()
        {
            return "Spawn Shield";
        }

        public string GetPluginVersion()
        {
            return "1.2";
        }

        public string GetPluginAuthor()
        {
            return "Myriades";
        }

        public string GetPluginWebsite()
        {
            return "http://www.phogue.net/forum/viewtopic.php?f=18&t=1006";
        }

        // A note to plugin authors: DO NOT change how a tag works, instead make a whole new tag.
        public string GetPluginDescription()
        {
            return @"
<h2>What is it?</h2>
	<blockquote>
		As spawnshield doesn't exist and spawnkill exists, this plugin would answer to the spawnkiller!
	</blockquote>
<h2>How to</h2>
	<blockquote>
		<h4>SpawnShield time</h4>
			The time where no one is allowed to kill the player that just spawn<br>
			unit : second<br>
			limit : from 1 to 60 seconds
	</blockquote>
	<blockquote>
		<h4>Killing text</h4>
			The text that will be displayed to the killer
	</blockquote>
	<blockquote>
		<h4>Time till kill</h4>
			The time between warning and killing the spawn killer.<br>
			unit : second<br>
			limit : from 1 to 60 seconds
		</blockquote>
<h2>Credits</h2>
	<blockquote>
		Phogue and the developper staff<br>
		XORgEAfQ for the idea and tests ;D
	</blockquote>";
        }
        #endregion

        #region Plug load/enable/disable
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.m_strHostName = strHostName;
            this.m_strPort = strPort;
            this.m_strPRoConVersion = strPRoConVersion;
        }

        public void OnPluginEnable()
        {
            this.L_SpawnShield.Clear();
            this.ExecuteCommand("procon.protected.tasks.add", "CSpawnShield_CheckSpawnShield", "0", "1", "-1", "procon.protected.plugins.call", "CSpawnShield", "CheckSpawnShield");
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bSpawnShield ^2Enabled!");
        }

        public void OnPluginDisable()
        {
            this.L_SpawnShield.Clear();
            this.ExecuteCommand("procon.protected.tasks.remove", "CSpawnShield_CheckSpawnShield");
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bSpawnShield ^1Disabled =(");
        }
        #endregion

        #region plugin graphic controls
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("SpawnShield time", typeof(int), this.I_SpawnShieldTime.ToString()));
            lstReturn.Add(new CPluginVariable("Killing text", typeof(string), this.S_KillText));
            lstReturn.Add(new CPluginVariable("Time till kill", typeof(int), this.I_TimeTillKill.ToString()));

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            return this.GetDisplayPluginVariables();
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            int TmpInt_1 = 0;
            switch (strVariable)
            {
                case "Time till kill":
                    if (int.TryParse(strValue, out TmpInt_1))
                    {
                        this.I_TimeTillKill = IntLimiter(TmpInt_1, 1, 60);
                    }
                    break;
                case "SpawnShield time":
                    if (int.TryParse(strValue, out TmpInt_1))
                    {
                        this.I_SpawnShieldTime = IntLimiter(TmpInt_1, 1, 60);
                    }
                    break;
                case "Killing text":
                    this.S_KillText = strValue;
                    break;
            }
        }
        #endregion

        #region Account created
        public void OnAccountCreated(string strUsername)
        {
        }

        public void OnAccountDeleted(string strUsername)
        {
        }

        public void OnAccountPrivilegesUpdate(string strUsername, CPrivileges cpPrivs)
        {
        }

        public void OnReceiveProconVariable(string strVariableName, string strValue)
        {
        }

        // Connection
        public void OnConnectionClosed()
        {
        }
        #endregion

        #region Player events
        public void OnPlayerJoin(string strSoldierName)
        {
        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {
        }

        public void OnPlayerLeft(string strSoldierName)
        {
            this.L_SpawnShield.Remove(strSoldierName);
        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {
        }
        #endregion

        #region (un peu de tout)
        // Will receive ALL chat global/team/squad in R3.
        public void OnGlobalChat(string strSpeaker, string strMessage)
        {
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnTeamChat(string strSpeaker, string strMessage, int iTeamID)
        {
        }

        // Place holder, non-functioning in R3.  It recieves the same data as OnGlobalChat though so look out for now.
        public void OnSquadChat(string strSpeaker, string strMessage, int iTeamID, int iSquadID)
        {
        }

        public void OnLoadingLevel(string strMapFileName)
        {
        }

        public void OnLevelStarted()
        {
        }

        public void OnPunkbusterMessage(string strPunkbusterMessage)
        {
        }

        public void OnPunkbusterBanInfo(CBanInfo cbiPunkbusterBan)
        {
        }

        public void OnPunkbusterPlayerInfo(CPunkbusterInfo cpbiPlayer)
        {
        }

        // Global or misc..
        public void OnResponseError(List<string> lstRequestWords, string strError)
        {
        }

        // Login events
        public void OnLogin()
        {
        }

        public void OnLogout()
        {
        }

        public void OnQuit()
        {
        }

        public void OnVersion(string strServerType, string strVersion)
        {
        }

        public void OnHelp(List<string> lstCommands)
        {
        }

        public void OnRunScript(string strScriptFileName)
        {
        }

        public void OnRunScriptError(string strScriptFileName, int iLineError, string strErrorDescription)
        {
        }

        // Query Events
        public void OnServerInfo(CServerInfo csiServerInfo)
        {
        }
        #endregion

        #region Communication Events
        public void OnYelling(string strMessage, int iMessageDuration, CPlayerSubset cpsSubset)
        {
        }

        public void OnSaying(string strMessage, CPlayerSubset cpsSubset)
        {
        }
        #endregion

        #region Level Events
        public void OnRunNextLevel()
        {
        }

        public void OnCurrentLevel(string strCurrentLevel)
        {
        }

        public void OnSetNextLevel(string strNextLevel)
        {
        }

        public void OnRestartLevel()
        {
        }
        #endregion

        #region Does not work in R3, never called for now.
        public void OnSupportedMaps(string strPlayList, List<string> lstSupportedMaps)
        {
        }

        public void OnPlaylistSet(string strPlaylist)
        {

        }

        public void OnListPlaylists(List<string> lstPlaylists)
        {

        }
        #endregion

        #region Player Kick/List Events
        public void OnPlayerKicked(string strSoldierName, string strReason)
        {

        }

        public void OnPlayerTeamChange(string strSoldierName, int iTeamID, int iSquadID)
        {

        }

        public void OnPlayerSquadChange(string strSpeaker, int iTeamID, int iSquadID)
        {

        }

        public void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
        }
        #endregion

        #region Banning and Banlist Events
        public void OnBanList(List<CBanInfo> lstBans)
        {

        }

        public void OnBanAdded(CBanInfo cbiBan)
        {

        }

        public void OnBanRemoved(CBanInfo cbiUnban)
        {

        }

        public void OnBanListClear()
        {

        }

        public void OnBanListLoad()
        {

        }

        public void OnBanListSave()
        {

        }
        #endregion

        #region Reserved Slots Events
        public void OnReservedSlotsConfigFile(string strConfigFilename)
        {

        }

        public void OnReservedSlotsLoad()
        {

        }

        public void OnReservedSlotsSave()
        {

        }

        public void OnReservedSlotsPlayerAdded(string strSoldierName)
        {

        }

        public void OnReservedSlotsPlayerRemoved(string strSoldierName)
        {

        }

        public void OnReservedSlotsCleared()
        {

        }

        public void OnReservedSlotsList(List<string> lstSoldierNames)
        {

        }
        #endregion

        #region Maplist Events
        public void OnMaplistConfigFile(string strConfigFilename)
        {

        }

        public void OnMaplistLoad()
        {

        }

        public void OnMaplistSave()
        {

        }

        public void OnMaplistMapAppended(string strMapFileName)
        {

        }

        public void OnMaplistMapRemoved(int iMapIndex)
        {

        }

        public void OnMaplistCleared()
        {

        }

        public void OnMaplistList(List<string> lstMapFileNames)
        {

        }

        public void OnMaplistNextLevelIndex(int iMapIndex)
        {

        }

        public void OnMaplistMapInserted(int iMapIndex, string strMapFileName)
        {

        }
        #endregion

        #region Game Vars
        public void OnGamePassword(string strGamePassword)
        {

        }

        public void OnPunkbuster(bool blEnabled)
        {

        }

        public void OnHardcore(bool blEnabled)
        {

        }

        public void OnRanked(bool blEnabled)
        {

        }

        public void OnRankLimit(int iRankLimit)
        {

        }

        public void OnTeamBalance(bool blEnabled)
        {

        }

        public void OnFriendlyFire(bool blEnabled)
        {

        }

        public void OnMaxPlayerLimit(int iMaxPlayerLimit)
        {

        }

        public void OnCurrentPlayerLimit(int iCurrentPlayerLimit)
        {

        }

        public void OnPlayerLimit(int iPlayerLimit)
        {

        }

        public void OnBannerURL(string strURL)
        {

        }

        public void OnServerDescription(string strServerDescription)
        {

        }

        public void OnKillCam(bool blEnabled)
        {

        }

        public void OnMiniMap(bool blEnabled)
        {

        }

        public void OnCrossHair(bool blEnabled)
        {

        }

        public void On3dSpotting(bool blEnabled)
        {

        }

        public void OnMiniMapSpotting(bool blEnabled)
        {

        }

        public void OnThirdPersonVehicleCameras(bool blEnabled)
        {

        }
        #endregion

        #region IPRoConPluginInterface2

        //
        // IPRoConPluginInterface2
        //
        public void OnPlayerKilled(Kill kKillerVictimDetails)
        {
            string Message;
            if (this.L_SpawnShield.ContainsKey(kKillerVictimDetails.Victim.SoldierName))
            {
                if (!kKillerVictimDetails.IsSuicide && kKillerVictimDetails.Killer.TeamID != kKillerVictimDetails.Victim.TeamID)
                {
                    ExecuteCommand("procon.protected.send", "admin.yell", this.S_KillText, (this.I_TimeTillKill * 1000).ToString(), "player", kKillerVictimDetails.Killer.SoldierName);
                    Message = "Killing " + kKillerVictimDetails.Killer.SoldierName + " because of spawnkilling";
                    ExecuteCommand("procon.protected.send", "admin.say", Message, "all");
                    ExecuteCommand("procon.protected.tasks.add", "CSpawnShield_Protector_" + kKillerVictimDetails.Killer.SoldierName, this.I_TimeTillKill.ToString(), "1", "1", "procon.protected.send", "admin.killPlayer", kKillerVictimDetails.Killer.SoldierName);
                }
                this.L_SpawnShield.Remove(kKillerVictimDetails.Victim.SoldierName);
            }
        }

        public void OnPlayerLeft(CPlayerInfo cpiPlayer)
        {

        }

        public void OnServerName(string strServerName)
        {

        }

        public void OnTeamKillCountForKick(int iLimit)
        {

        }

        public void OnTeamKillValueIncrease(int iLimit)
        {

        }

        public void OnTeamKillValueDecreasePerSecond(int iLimit)
        {

        }

        public void OnTeamKillValueForKick(int iLimit)
        {

        }

        public void OnIdleTimeout(int iLimit)
        {

        }

        public void OnProfanityFilter(bool isEnabled)
        {

        }

        public void OnEndRound(int iWinningTeamID)
        {
        }

        public void OnRoundOverTeamScores(List<TeamScore> lstTeamScores)
        {

        }

        public void OnRoundOverPlayers(List<string> lstPlayers)
        {

        }

        public void OnRoundOver(int iWinningTeamID)
        {
            this.L_SpawnShield.Clear();
        }

        public void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
        {
            this.L_SpawnShield.Add(soldierName, this.I_SpawnShieldTime);
        }

        public void OnLevelVariablesList(LevelVariable lvRequestedContext, List<LevelVariable> lstReturnedValues)
        {

        }

        public void OnLevelVariablesEvaluate(LevelVariable lvRequestedContext, LevelVariable lvReturnedValue)
        {

        }

        public void OnLevelVariablesClear(LevelVariable lvRequestedContext)
        {

        }

        public void OnLevelVariablesSet(LevelVariable lvRequestedContext)
        {

        }

        public void OnLevelVariablesGet(LevelVariable lvRequestedContext, LevelVariable lvReturnedValue)
        {

        }

        #endregion

        #region IPRoConPluginInterface3

        //
        // IPRoConPluginInterface3
        //
        public void OnAnyMatchRegisteredCommand(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {

        }

        public void OnZoneTrespass(CPlayerInfo cpiSoldier, ZoneAction action, MapZone sender, Point3D pntTresspassLocation, float flTresspassPercentage)
        {

        }

        public void OnRegisteredCommand(MatchCommand mtcCommand)
        {

        }

        public void OnUnregisteredCommand(MatchCommand mtcCommand)
        {

        }

        public void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal)
        {

        }

        public void OnMaplistList(List<MaplistEntry> lstMaplist)
        {

        }

        #endregion

        #region IPRoConPluginInterface4

        public void OnZoneTrespass(CPlayerInfo cpiSoldier, ZoneAction action, MapZone sender, Point3D pntTresspassLocation, float flTresspassPercentage, object trespassState)
        {

        }
        #endregion

        #region MY functions
        public void CheckSpawnShield()
        {
            List<string> Soldat = new List<string>();
            List<int> SoldatSS = new List<int>();
            int i = 0;
            foreach (KeyValuePair<string, int> PlayerDatas in this.L_SpawnShield)
            {
                if (this.L_SpawnShield.ContainsKey(PlayerDatas.Key))
                {
                    SoldatSS.Add(PlayerDatas.Value - 1);
                    Soldat.Add(PlayerDatas.Key);
                }
            }
            foreach (string nom in Soldat)
            {
                if (this.L_SpawnShield.ContainsKey(nom))
                {
                    if (SoldatSS[i] > 0)
                        this.L_SpawnShield[nom] = SoldatSS[i];
                    else
                        this.L_SpawnShield.Remove(nom);
                }
                i++;
            }
            Soldat.Clear();
            SoldatSS.Clear();
            if (this.L_SpawnShield.Count == 0)
                this.L_SpawnShield.Clear();
        }

        private int IntLimiter(int ValueToCheck, int LowerLimit, int UpperLimit)
        {
            if (ValueToCheck < LowerLimit)
                ValueToCheck = LowerLimit;
            else if (ValueToCheck > UpperLimit)
                ValueToCheck = UpperLimit;
            return ValueToCheck;
        }


        #endregion

    }
}