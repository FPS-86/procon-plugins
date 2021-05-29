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
    public class CHScounter : PRoConPluginAPI, IPRoConPluginInterface
    {

        #region Plugin VARS & INIT
        private string m_strHostName;
        private string m_strPort;
        private string m_strPRoConVersion;

        private int m_MaxHSKSinRound;
        private string m_MaxHSPlayerName;
        private int i_ChatSpamAt;
        private string str_ChatMSG;
        private string str_EORM;
        private Dictionary<string, CHScounter.PlayerData> D_PlayerDatas = new Dictionary<string, CHScounter.PlayerData>();

        private class PlayerData
        {
            public int i_PlKill;
            public int i_PlHsKs;
            public int i_PlHsCount;

            public PlayerData()
            {
                this.i_PlHsCount = 0;
                this.i_PlHsKs = 0;
                this.i_PlKill = 0;
            }
        }

        public CHScounter()
        {
            this.m_MaxHSKSinRound = 0;
            this.m_MaxHSPlayerName = "";
            this.i_ChatSpamAt = 3;
            this.str_ChatMSG = "%pn% has a %hsks% headshot kill streak and a total of %ths% over %kill% kills";
            this.str_EORM = "%pns% had the best HS kill streak with %mhsks%";
        }
        #endregion

        #region Plugin details
        public string GetPluginName()
        {
            return "Head Shot Counter";
        }

        public string GetPluginVersion()
        {
            return "1.0";
        }

        public string GetPluginAuthor()
        {
            return "Myriades";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net/forum/viewtopic.php?f=18&t=841";
        }

        // A note to plugin authors: DO NOT change how a tag works, instead make a whole new tag.
        public string GetPluginDescription()
        {
            return @"<h2>Description</h2>
This plugin displays head shots in global chat
<h2>Settings</h2>
	<h3>Chat spam</h3>
		<blockquote>
			<h4>Start @ :</h4>
			Minimum head shot kill streak before display<br>
		</blockquote>
		<blockquote>
			<h4>Message :</h4>
			The displayed message :)
		</blockquote>
		<blockquote>
			<h4>End of round message :</h4>
			The end of round displayed message :)
		</blockquote>
		<blockquote>
			<h4>Messages vars :</h4>
			<ul>
				<li>%pn% : the player name. The one that did a head shot kill streak.</li>
				<li>%hsks% : the current head shot kill streak of %pn%</li>
				<li>%ths% : the total head shot of %pn%</li>
				<li>%kill% : the number of kill done by %pn%</li>
				<li>%pns% : the player names that have the best head shot serie</li>
				<li>%mhsks% : the maximum head shot serie value</li>
			</ul>
		</blockquote>
";
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
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bHS counter ^2Enabled!");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bHS counter ^1Disabled =(");
        }
        #endregion

        #region plugin graphic controls
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            //	Chat
            lstReturn.Add(new CPluginVariable("Chat spam|Start @", typeof(int), this.i_ChatSpamAt));
            lstReturn.Add(new CPluginVariable("Chat spam|Message", typeof(string), this.str_ChatMSG));
            lstReturn.Add(new CPluginVariable("Chat spam|End of round message", typeof(string), this.str_EORM));

            return lstReturn;
        }

        // Lists all of the plugin variables.
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Start @", typeof(int), this.i_ChatSpamAt));
            lstReturn.Add(new CPluginVariable("Message", typeof(string), this.str_ChatMSG));
            lstReturn.Add(new CPluginVariable("End of round message", typeof(string), this.str_EORM));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            int TmpInt_1 = 0;
            switch (strVariable)
            {
                case "Start @":
                    if (int.TryParse(strValue, out TmpInt_1))
                        this.i_ChatSpamAt = TmpInt_1;
                    break;
                case "Message":
                    this.str_ChatMSG = strValue;
                    break;
                case "End of round message":
                    this.str_EORM = strValue;
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
            this.D_PlayerDatas.Add(strSoldierName, new PlayerData());
        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {
        }

        public void OnPlayerLeft(string strSoldierName)
        {
            this.D_PlayerDatas.Remove(strSoldierName);
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
            foreach (CPlayerInfo playerInfos in lstPlayers)
            {
                if (!this.D_PlayerDatas.ContainsKey(playerInfos.SoldierName))
                    this.D_PlayerDatas.Add(playerInfos.SoldierName, new PlayerData());
            }
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
            if (this.D_PlayerDatas.Count >= 4)
            {   //	Server started
                this.D_PlayerDatas[kKillerVictimDetails.Killer.SoldierName].i_PlKill++;
                if (kKillerVictimDetails.Headshot)
                {
                    this.D_PlayerDatas[kKillerVictimDetails.Killer.SoldierName].i_PlHsCount++;
                    this.D_PlayerDatas[kKillerVictimDetails.Killer.SoldierName].i_PlHsKs++;
                    //	Messages datas
                    if (this.D_PlayerDatas[kKillerVictimDetails.Killer.SoldierName].i_PlHsKs > this.m_MaxHSKSinRound)
                    {
                        this.m_MaxHSKSinRound = this.D_PlayerDatas[kKillerVictimDetails.Killer.SoldierName].i_PlHsKs;
                        this.m_MaxHSPlayerName = kKillerVictimDetails.Killer.SoldierName;
                    }
                    else if (this.D_PlayerDatas[kKillerVictimDetails.Killer.SoldierName].i_PlHsKs == this.m_MaxHSKSinRound && !this.m_MaxHSPlayerName.Contains(kKillerVictimDetails.Killer.SoldierName))
                    {
                        this.m_MaxHSPlayerName += " & " + kKillerVictimDetails.Killer.SoldierName;
                    }
                    if (this.D_PlayerDatas[kKillerVictimDetails.Killer.SoldierName].i_PlHsKs >= this.i_ChatSpamAt)
                    {
                        this.ExecuteCommand("procon.protected.send", "admin.say", this.MSGConverter(kKillerVictimDetails.Killer.SoldierName, this.str_ChatMSG), "all");
                    }
                }
                else
                {
                    this.D_PlayerDatas[kKillerVictimDetails.Killer.SoldierName].i_PlHsKs = 0;
                }
                this.D_PlayerDatas[kKillerVictimDetails.Victim.SoldierName].i_PlHsKs = 0;
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
            if (this.m_MaxHSPlayerName != "")
            {
                this.ExecuteCommand("procon.protected.send", "admin.say", this.MSGConverter("Server", this.str_EORM), "all");
            }
            this.m_MaxHSPlayerName = "";
            this.m_MaxHSKSinRound = 0;
            List<string> PlName = new List<string>();
            foreach (KeyValuePair<string, PlayerData> kvp_PlayerDatas in D_PlayerDatas)
            {
                PlName.Add(kvp_PlayerDatas.Key);
            }
            foreach (string str_PlayerDatas in PlName)
            {
                this.D_PlayerDatas[str_PlayerDatas] = new PlayerData();
            }
            PlName.Clear();
        }

        public void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
        {

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
        private string MSGConverter(string SoldierName, string TheMSG)
        {
            if (SoldierName != "Server")
                TheMSG = TheMSG.Replace("%pn%", SoldierName).Replace("%hsks%", this.D_PlayerDatas[SoldierName].i_PlHsKs.ToString()).Replace("%ths%", this.D_PlayerDatas[SoldierName].i_PlHsCount.ToString()).Replace("%kill%", this.D_PlayerDatas[SoldierName].i_PlKill.ToString());
            this.ExecuteCommand("procon.protected.pluginconsole.write", TheMSG.Replace("%pns%", this.m_MaxHSPlayerName).Replace("%mhsks%", this.m_MaxHSKSinRound.ToString()));
            return TheMSG.Replace("%pns%", this.m_MaxHSPlayerName).Replace("%mhsks%", this.m_MaxHSKSinRound.ToString());
        }
        #endregion

    }
}