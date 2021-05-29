/*  Copyright 2010 Geoffrey 'Phogue' Green

    This file is part of BFBC2 PRoCon.

    BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    BFBC2 PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;

namespace PRoConEvents
{
    public class CMixedGamemodes : PRoConPluginAPI, IPRoConPluginInterface
    {

        private int m_iCurrentIndex = 0;
        private List<CMap> m_lstMapDefines;

        // Variables
        private List<CMapNameRounds> m_lstMaplist;

        private class CMapNameRounds
        {
            private string m_strMapFileName;
            private int m_iRoundsPlayed;

            public CMapNameRounds(string strMapFileName, int iRoundsPlayed)
            {
                this.m_strMapFileName = strMapFileName;
                this.m_iRoundsPlayed = iRoundsPlayed;
            }

            public string MapFileName
            {
                get
                {
                    return this.m_strMapFileName;
                }
            }

            public int Rounds
            {
                get
                {
                    return this.m_iRoundsPlayed;
                }
            }
        }

        public CMixedGamemodes()
        {
            this.m_lstMaplist = new List<CMapNameRounds>();
        }

        public string GetPluginName()
        {
            return "Mixed Gamemodes";
        }

        public string GetPluginVersion()
        {
            return "2.1.0.0";
        }

        public string GetPluginAuthor()
        {
            return "Phogue";
        }

        public string GetPluginWebsite()
        {
            return "www.phogue.net";
        }

        public string GetPluginDescription()
        {

            List<string> mapList = this.GetMapList("<tr><td><b>{FileName}</b></td><td>{GameMode}</td><td>{PublicLevelName}</td></tr>", "CONQUEST", "RUSH", "SQRUSH", "SQDM");

            return @"
<h2>Description</h2>
<p>Nullifies any maplist control by the client and cycles through a set map list, changing gamemodes if need be.</p>

<h2>Settings</h2>
    <h3>Maplist</h3>
        <blockquote><h4>Maplist</h4>You can specify the number of rounds after the map e.g:
            <ul>
	            <li>levels/mp_002 1</li>
	            <li>levels/mp_002 5</li>
	            <li>levels/mp_002 3</li>
            </ul>
            .. or leave blank to play the default rounds for that game type (usually 2)
            <ul>
	            <li>levels/mp_002</li>
            </ul>
        </blockquote>

<h2>Additional Information</h2>
    <ul>
        <li>CAUTION, enabling will immediately alter your maplist.  You cannot have the same map played twice in a row =(</li>
    </ul>

<h2>Available Maps</h2>
    <table style=""padding-left: 30px;"">
        " + String.Join("", mapList.ToArray()) + @"
    </table>
";

            /*
            List<string> mapList = this.GetMapList("<b>{FileName}</b> - {GameMode}: {PublicLevelName}", "CONQUEST", "RUSH", "SQRUSH", "SQDM");

            return @"
<h2>Description</h2>
<p>Nullifies any maplist control by the client and cycles through a set map list, changing gamemodes if need be.</p>

<h2>Settings</h2>
    <h3>Maplist</h3>
        <blockquote><h4>Maplist</h4>You can specify the number of rounds after the map e.g:
            <ul>
	            <li>levels/mp_002 1</li>
	            <li>levels/mp_002 5</li>
	            <li>levels/mp_002 3</li>
            </ul>
            .. or leave blank to play the default rounds for that game type (usually 2)
            <ul>
	            <li>levels/mp_002</li>
            </ul>
        </blockquote>

<h2>Additional Information</h2>
    <ul>
        <li>CAUTION, enabling will immediately alter your maplist.  You cannot have the same map played twice in a row =(</li>
    </ul>

<h2>Available Maps</h2>
    <ul>
        <li>
            " + String.Join("</li><li>", mapList.ToArray()) + @"
        </li>
    </ul>";*/
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            // Reload debugging.
            this.m_lstMaplist = new List<CMapNameRounds>();
            //this.m_lstForwardOnRoundChange = new List<string>();
            this.m_lstMapDefines = this.GetMapDefines();
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMultiple Gamemodes ^2Enabled!");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bMultiple Gamemodes ^1Disabled =(");
        }

        private string[] StringifyMaplist()
        {
            string[] a_strReturn = new string[this.m_lstMaplist.Count];

            for (int i = 0; i < this.m_lstMaplist.Count; i++)
            {
                //foreach (CMapNameRounds cMapNameRound in this.m_lstMaplist) {
                a_strReturn[i] = String.Format("{0} {1}", this.m_lstMaplist[i].MapFileName, this.m_lstMaplist[i].Rounds);
            }

            return a_strReturn;
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Maplist (Press '...', one map filename per line)|Maplist", typeof(string[]), this.StringifyMaplist()));

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Maplist", typeof(string[]), this.StringifyMaplist()));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            int iTimeSeconds = 15;

            if (strVariable.CompareTo("Maplist") == 0)
            {

                this.m_lstMapDefines = this.GetMapDefines();
                List<string> lstValidateMaplist = new List<string>(CPluginVariable.DecodeStringArray(strValue));

                // Gets rid of the error message when no array is set.
                lstValidateMaplist.RemoveAll(String.IsNullOrEmpty);

                if (this.m_lstMaplist.Count > 0)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^3Clearing Maplist.."));
                    this.m_lstMaplist.Clear();
                }

                string strPreviouslyAddedMapFilename = "";
                bool blShowMapAdditionError = true;
                foreach (string strMapFileNameRound in lstValidateMaplist)
                {

                    string[] a_strMapFileNameRound = strMapFileNameRound.Split(' ');

                    string strMapFileName = "";
                    int iRounds = 0;

                    if (a_strMapFileNameRound.Length >= 1)
                    {
                        strMapFileName = a_strMapFileNameRound[0];

                        if (a_strMapFileNameRound.Length >= 2)
                        {
                            int.TryParse(a_strMapFileNameRound[1], out iRounds);
                        }
                    }

                    if (String.IsNullOrEmpty(strMapFileName) == false)
                    {

                        blShowMapAdditionError = true;

                        foreach (CMap cMapDefine in this.m_lstMapDefines)
                        {
                            if (String.Compare(strMapFileName, cMapDefine.FileName, true) == 0)
                            {
                                if (String.Compare(strPreviouslyAddedMapFilename, cMapDefine.FileName, true) != 0)
                                {
                                    this.m_lstMaplist.Add(new CMapNameRounds(cMapDefine.FileName.ToLower(), iRounds));
                                    strPreviouslyAddedMapFilename = cMapDefine.FileName;

                                    if (iRounds == 0)
                                    {
                                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^2Adding ^4{0} ^5{1}^2 to the rotation with default number of rounds..", cMapDefine.GameMode, cMapDefine.PublicLevelName));
                                    }
                                    else
                                    {
                                        this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^2Adding ^4{0} ^5{1}^2 to the rotation with {2} round(s)..", cMapDefine.GameMode, cMapDefine.PublicLevelName, iRounds));
                                    }

                                }
                                else
                                {
                                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1Removing consecutive map \"{0}\" from the list.  Can't run the same map twice in a row =(", strMapFileName));
                                }

                                blShowMapAdditionError = false;
                                break;
                            }
                        }

                        if (blShowMapAdditionError == true)
                        {
                            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1The map \"{0}\" is not a valid map (or it's unknown to procon at the moment)", strMapFileName));
                        }
                    }
                }

                if (this.m_iCurrentIndex > this.m_lstMaplist.Count)
                {
                    this.m_iCurrentIndex = 0;
                }
            }
        }

        // Account created
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

        // Player events
        public void OnPlayerJoin(string strSoldierName)
        {

        }

        public void OnPlayerAuthenticated(string strSoldierName, string strGuid)
        {

        }

        public void OnPlayerLeft(string strSoldierName)
        {

        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {

        }

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

        private string m_strCurrentMapFilename = "";
        public void OnLoadingLevel(string strMapFileName)
        {

            this.m_strCurrentMapFilename = strMapFileName;

            /*
            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("OnLoadingLevel: {0} {1} {2}", strMapFileName, m_strCurrentMapFilename, m_blLoadNextLevel));

            try {
                if (String.Compare(strMapFileName, this.m_strCurrentMapFilename, true) != 0) {
                    this.m_blLoadNextLevel = true;
                }
                else {
                    this.m_blLoadNextLevel = false;
                }

                this.m_strCurrentMapFilename = strMapFileName;
            }
            catch (Exception e) {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "CMixedGamemodeException: " + e.Message);
            }
            */
        }

        public void OnLevelStarted()
        {

            this.m_blFinalRoundSet = false;
            this.ExecuteCommand("procon.protected.send", "serverInfo");

            /*
            this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("OnLevelStarted: {0} {1}", m_strCurrentMapFilename, m_blLoadNextLevel));

            if (this.m_blLoadNextLevel == true) {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^7Setting next map now..");
                this.SetNextMap();

                this.m_blLoadNextLevel = false;
            }
            */
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

        private int m_iSkippedErrorMaps = 0;

        // Global or misc..
        public void OnResponseError(List<string> lstRequestWords, string strError)
        {

            if (lstRequestWords.Count >= 2 && String.Compare(lstRequestWords[0], "mapList.append") == 0 && String.Compare(strError, "InvalidMapName") == 0)
            {

                CMap cmErrorMap = this.GetMap(lstRequestWords[1]);

                if (cmErrorMap != null)
                {

                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1Error appending map \"{0}\":\"{1} - {2}\".  The server does not recognize it as a valid map.  Removing from mixed gamemode maplist.", cmErrorMap.FileName, cmErrorMap.GameMode, cmErrorMap.PublicLevelName));

                    //this.m_lstMaplist.RemoveAll(strMap => String.Compare(strMap, cmErrorMap.FileName, true) == 0);
                    for (int i = 0; i < this.m_lstMaplist.Count; i++)
                    {
                        if (String.Compare(this.m_lstMaplist[i].MapFileName, cmErrorMap.FileName) == 0)
                        {
                            this.m_lstMaplist.RemoveAt(i);
                        }
                    }

                    //while (this.m_lstMaplist.Contains(cmErrorMap.FileName) == true) {
                    //    this.m_lstMaplist.Remove(cmErrorMap.FileName);
                    //}

                    this.m_iSkippedErrorMaps++;
                    //this.m_iCurrentIndex = this.NextMapIndex();
                    this.SetNextMap();
                }
            }

            //this.ExecuteCommand("procon.protected.pluginconsole.write", "Error: " + String.Join(" ", lstRequestWords.ToArray()) + " Code: " + strError);
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

        private CMap GetMap(string strMapFilename)
        {

            CMap cmReturn = null;

            foreach (CMap cMapDefine in this.m_lstMapDefines)
            {
                if (String.Compare(strMapFilename, cMapDefine.FileName) == 0)
                {
                    cmReturn = cMapDefine;
                    break;
                }
            }

            return cmReturn;
        }

        private int NextMapIndex()
        {
            return this.m_lstMaplist.Count > 0 ? (this.m_iCurrentIndex + 1) % this.m_lstMaplist.Count : 0;
        }

        private void SetNextMap()
        {

            // If the entire maplist is has been removed due to errors..
            // (They've only added the two new maps and the server does not know them yet..)
            if (this.m_iSkippedErrorMaps > 0 && this.m_lstMaplist.Count == 0)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1Mixed Gamemode Maplist Panic!"));
                this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^b^1The server does not know any of the maps in the Mixed Gamemode Maplist.  Setting to default RUSH list."));
                this.SetPluginVariable("Maplist", "levels/mp_002|levels/mp_004|levels/mp_006|levels/mp_008|levels/mp_009gr|levels/mp_012gr");

                this.m_iSkippedErrorMaps = 0;
            }

            if (String.Compare(this.m_lstMaplist[this.NextMapIndex()].MapFileName, this.m_strCurrentMapFilename) != 0)
            {

                // See if we have this map defined and set it to that.. otherwise restart the map list.
                bool blLoopedOnce = false;
                bool blRestartMapList = true;
                for (int i = this.NextMapIndex(); i < this.m_lstMaplist.Count; i++)
                {

                    if (String.Compare(this.m_lstMaplist[i].MapFileName, this.m_strCurrentMapFilename, true) == 0)
                    {
                        this.m_iCurrentIndex = i;
                        blRestartMapList = false;
                        break;
                    }

                    if (blLoopedOnce == false && i + 1 >= this.m_lstMaplist.Count)
                    {
                        // Start from the start..
                        i = -1; // Cancels the ++ to bring to 0.
                        blLoopedOnce = true;
                    }
                }

                if (blRestartMapList == true)
                {
                    this.m_iCurrentIndex = 0;
                }
            }
            else
            {
                this.m_iCurrentIndex = this.NextMapIndex();
            }

            CMap cmNextMap = this.GetMap(this.m_lstMaplist[this.NextMapIndex()].MapFileName);

            if (cmNextMap != null)
            {

                this.ExecuteCommand("procon.protected.pluginconsole.write", "^bSetting playlist to: " + cmNextMap.PlayList);
                this.ExecuteCommand("procon.protected.send", "admin.setPlaylist", cmNextMap.PlayList);

                this.ExecuteCommand("procon.protected.pluginconsole.write", "^bClearing the maplist");
                this.ExecuteCommand("procon.protected.send", "mapList.clear");

                if (this.m_lstMaplist[this.NextMapIndex()].Rounds == 0)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", "^bAdding " + cmNextMap.FileName + " to the maplist");
                    this.ExecuteCommand("procon.protected.send", "mapList.append", cmNextMap.FileName);
                }
                else if (this.m_lstMaplist[this.NextMapIndex()].Rounds > 0)
                {
                    this.ExecuteCommand("procon.protected.pluginconsole.write", String.Format("^bAdding {0} to the maplist with {1} rounds", cmNextMap.FileName, this.m_lstMaplist[this.NextMapIndex()].Rounds));
                    this.ExecuteCommand("procon.protected.send", "mapList.append", cmNextMap.FileName, this.m_lstMaplist[this.NextMapIndex()].Rounds.ToString());
                }
            }
            else
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^b^1Error pulling map information for " + this.m_lstMaplist[this.NextMapIndex()]);
            }
        }

        CServerInfo m_csiLatestServerInfo = null;
        private bool m_blFinalRoundSet = false;
        public void OnServerInfo(CServerInfo csiServerInfo)
        {
            this.m_csiLatestServerInfo = csiServerInfo;

            if (this.m_csiLatestServerInfo.CurrentRound == this.m_csiLatestServerInfo.TotalRounds)
            {

                if (this.m_blFinalRoundSet == false)
                {
                    this.SetNextMap();

                    this.m_blFinalRoundSet = true;
                }
            }
            else
            {
                this.m_blFinalRoundSet = false;
            }
        }

        // Communication Events
        public void OnYelling(string strMessage, int iMessageDuration, CPlayerSubset cpsSubset)
        {

        }

        public void OnSaying(string strMessage, CPlayerSubset cpsSubset)
        {

        }

        // Level Events
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

        // Does not work in R3, never called for now.
        public void OnSupportedMaps(string strPlayList, List<string> lstSupportedMaps)
        {

        }

        public void OnPlaylistSet(string strPlaylist)
        {

        }

        public void OnListPlaylists(List<string> lstPlaylists)
        {

        }

        // Player Kick/List Events
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

        // Banning and Banlist Events
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

        // Reserved Slots Events
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

        // Maplist Events
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
            // Map successfully added, cancel the skipped map errors.
            this.m_iSkippedErrorMaps = 0;
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

        // Vars
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

    }
}