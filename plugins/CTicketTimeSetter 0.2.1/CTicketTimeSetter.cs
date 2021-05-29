using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Timers;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
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
    public class CTicketTimeSetter : PRoConPluginAPI, IPRoConPluginInterface
    {
        private enumBoolYesNo enableOblRushMixed;
        private int obliterationCounter;
        private int obliterationTime;
        private int rushCounter;
        private int rushTime;
        private int conquestCounter;
        private int conquestTime;
        private int tdmCounter;
        private int tdmTime;
        private int eliminationCounter;
        private int eliminationTime;
        private int dominationCounter;
        private int dominationTime;
        private int squadDeathMatchCounter;
        private int squadDeathMatchTime;
        private int airSuperiorityCounter;
        private int airSuperiorityTime;

        private enumBoolYesNo enableCounterTimeRestart;
        private int gameModeCounter;
        private string presetValue;
        private string currentGameMode;

        private bool fromRoundOver;
        private bool fromRunNext;
        private bool goingNextMap;
        private int m_nextMap;
        private bool checkingCrash;
        private bool b_checked;
        private bool gettingGameMode;

        public CTicketTimeSetter()
        {
            this.enableOblRushMixed = enumBoolYesNo.No;
            this.obliterationCounter = 100;
            this.obliterationTime = 100;
            this.rushCounter = 150;
            this.rushTime = 0;
            this.conquestCounter = 100;
            this.conquestTime = 0;
            this.tdmCounter = 500;
            this.tdmTime = 0;
            this.eliminationCounter = 100;
            this.eliminationTime = 100;
            this.dominationCounter = 100;
            this.dominationTime = 0;
            this.squadDeathMatchCounter = 100;
            this.squadDeathMatchTime = 0;
            this.airSuperiorityCounter = 100;
            this.airSuperiorityTime = 0;

            this.enableCounterTimeRestart = enumBoolYesNo.No;
            this.gameModeCounter = 100;
            this.presetValue = "normal";
            this.currentGameMode = "";

            this.fromRoundOver = false;
            this.fromRunNext = false;
            this.goingNextMap = false;
            this.m_nextMap = 0;
            this.checkingCrash = false;
            this.b_checked = false;
            this.gettingGameMode = false;
        }

        public string GetPluginName()
        {
            return "Ticket Time Setter";
        }

        public string GetPluginVersion()
        {
            return "0.2.1.0";
        }

        public string GetPluginAuthor()
        {
            return "TimSad";
        }

        public string GetPluginWebsite()
        {
            return "forum.myrcon.com/showthread.php?7062";
        }

        public string GetPluginDescription()
        {
            return @"<p>The primary function of this plugin is to be able to define different values for vars.gameModeCounter and vars.roundTimeLimit between all the different game modes. When the map is changing to one of those game modes, the variables will change over to what you have them set for in the plugin settings for that particular game mode.</p><br \>
	      <p>The secondary function of this plugin is to help you manage your vars.gameModeCounter value while also retaining your desired server preset value.  It keeps the vars.gameModeCounter value from server startup then sends the vars.preset command with your desired value. Once a round ends, it instantly sends your desired vars.gameModeCounter value since it got reset to 100 from forcing the preset. Then, once the next round is well into play, it forces back your desired preset. NOTE! - If you have the Mixed Mode part of the plugin enabled, it ignores the vars.gameModeCounter setting from this part of the plugin.</p>
        ";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name, "OnServerInfo", "OnRestartLevel", "OnMaplistList", "OnMaplistGetRounds", "OnMaplistGetMapIndices", "OnEndRound", "OnRunNextLevel", "OnRoundOver", "OnLoadingLevel", "OnLevelStarted", "OnLevelLoaded");
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bTicket Time Setter ^2Enabled!");
            this.ExecuteCommand("procon.protected.tasks.add", "CTicketTimeSetterCheckCrash", "5", "30", "-1", "procon.protected.plugins.call", "CTicketTimeSetter", "checkCrash");
            this.gettingGameMode = true;
            this.ExecuteCommand("procon.protected.send", "serverInfo");
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bTicket Time Setter ^1Disabled =(");
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|Enable Mixed Control?", typeof(enumBoolYesNo), this.enableOblRushMixed));
            if (this.enableOblRushMixed == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|Obliteration Ticket Percentage", this.obliterationCounter.GetType(), this.obliterationCounter));
                lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|Obliteration Time Limit Percentage", this.obliterationTime.GetType(), this.obliterationTime));
                lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|Rush Ticket Percentage", this.rushCounter.GetType(), this.rushCounter));
                lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|Rush Time Limit Percentage", this.rushTime.GetType(), this.rushTime));
                lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|Conquest Ticket Percentage", this.conquestCounter.GetType(), this.conquestCounter));
                lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|Conquest Time Limit Percentage", this.conquestTime.GetType(), this.conquestTime));
                lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|TDM Ticket Percentage", this.tdmCounter.GetType(), this.tdmCounter));
                lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|TDM Time Limit Percentage", this.tdmTime.GetType(), this.tdmTime));
                lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|Defuse Ticket Percentage", this.eliminationCounter.GetType(), this.eliminationCounter));
                lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|Defuse Time Limit Percentage", this.eliminationTime.GetType(), this.eliminationTime));
                lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|Domination Ticket Percentage", this.dominationCounter.GetType(), this.dominationCounter));
                lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|Domination Time Limit Percentage", this.dominationTime.GetType(), this.dominationTime));
                lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|Squad Death Match Ticket Percentage", this.squadDeathMatchCounter.GetType(), this.squadDeathMatchCounter));
                lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|Squad Death Match Time Limit Percentage", this.squadDeathMatchTime.GetType(), this.squadDeathMatchTime));
                lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|Air Superiority Ticket Percentage", this.airSuperiorityCounter.GetType(), this.airSuperiorityCounter));
                lstReturn.Add(new CPluginVariable("Mixed Game Mode Control|Air Superiority Time Limit Percentage", this.airSuperiorityTime.GetType(), this.airSuperiorityTime));
            }

            lstReturn.Add(new CPluginVariable("Ticket Preset Setter|Enable Ticket Preset Setter?", typeof(enumBoolYesNo), this.enableCounterTimeRestart));
            if (this.enableCounterTimeRestart == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Ticket Preset Setter|Ticket Percentage", this.gameModeCounter.GetType(), this.gameModeCounter));
                lstReturn.Add(new CPluginVariable("Ticket Preset Setter|Preset Value", this.presetValue.GetType(), this.presetValue));
            }

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Enable Mixed Control?", typeof(enumBoolYesNo), this.enableOblRushMixed));
            if (this.enableOblRushMixed == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Obliteration Ticket Percentage", this.obliterationCounter.GetType(), this.obliterationCounter));
                lstReturn.Add(new CPluginVariable("Obliteration Time Limit Percentage", this.obliterationTime.GetType(), this.obliterationTime));
                lstReturn.Add(new CPluginVariable("Rush Ticket Percentage", this.rushCounter.GetType(), this.rushCounter));
                lstReturn.Add(new CPluginVariable("Rush Time Limit Percentage", this.rushTime.GetType(), this.rushTime));
                lstReturn.Add(new CPluginVariable("Conquest Ticket Percentage", this.conquestCounter.GetType(), this.conquestCounter));
                lstReturn.Add(new CPluginVariable("Conquest Time Limit Percentage", this.conquestTime.GetType(), this.conquestTime));
                lstReturn.Add(new CPluginVariable("TDM Ticket Percentage", this.tdmCounter.GetType(), this.tdmCounter));
                lstReturn.Add(new CPluginVariable("TDM Time Limit Percentage", this.tdmTime.GetType(), this.tdmTime));
                lstReturn.Add(new CPluginVariable("Defuse Ticket Percentage", this.eliminationCounter.GetType(), this.eliminationCounter));
                lstReturn.Add(new CPluginVariable("Defuse Time Limit Percentage", this.eliminationTime.GetType(), this.eliminationTime));
                lstReturn.Add(new CPluginVariable("Domination Ticket Percentage", this.dominationCounter.GetType(), this.dominationCounter));
                lstReturn.Add(new CPluginVariable("Domination Time Limit Percentage", this.dominationTime.GetType(), this.dominationTime));
                lstReturn.Add(new CPluginVariable("Squad Death Match Ticket Percentage", this.squadDeathMatchCounter.GetType(), this.squadDeathMatchCounter));
                lstReturn.Add(new CPluginVariable("Squad Death Match Time Limit Percentage", this.squadDeathMatchTime.GetType(), this.squadDeathMatchTime));
                lstReturn.Add(new CPluginVariable("Air Superiority Ticket Percentage", this.airSuperiorityCounter.GetType(), this.airSuperiorityCounter));
                lstReturn.Add(new CPluginVariable("Air Superiority Time Limit Percentage", this.airSuperiorityTime.GetType(), this.airSuperiorityTime));
            }

            lstReturn.Add(new CPluginVariable("Enable Ticket Preset Setter?", typeof(enumBoolYesNo), this.enableCounterTimeRestart));
            if (this.enableCounterTimeRestart == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Ticket Percentage", this.gameModeCounter.GetType(), this.gameModeCounter));
                lstReturn.Add(new CPluginVariable("Preset Value", this.presetValue.GetType(), this.presetValue));
            }

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            if (strVariable.CompareTo("Enable Mixed Control?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.enableOblRushMixed = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Obliteration Ticket Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.obliterationCounter = valueAsInt;
            }
            else if (strVariable.CompareTo("Obliteration Time Limit Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.obliterationTime = valueAsInt;
            }
            else if (strVariable.CompareTo("Rush Ticket Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.rushCounter = valueAsInt;
            }
            else if (strVariable.CompareTo("Rush Time Limit Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.rushTime = valueAsInt;
            }
            else if (strVariable.CompareTo("Conquest Ticket Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.conquestCounter = valueAsInt;
            }
            else if (strVariable.CompareTo("Conquest Time Limit Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.conquestTime = valueAsInt;
            }
            else if (strVariable.CompareTo("TDM Ticket Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.tdmCounter = valueAsInt;
            }
            else if (strVariable.CompareTo("TDM Time Limit Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.tdmTime = valueAsInt;
            }
            else if (strVariable.CompareTo("Defuse Ticket Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.eliminationCounter = valueAsInt;
            }
            else if (strVariable.CompareTo("Defuse Time Limit Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.eliminationTime = valueAsInt;
            }
            else if (strVariable.CompareTo("Domination Ticket Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.dominationCounter = valueAsInt;
            }
            else if (strVariable.CompareTo("Domination Time Limit Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.dominationTime = valueAsInt;
            }
            else if (strVariable.CompareTo("Squad Death Match Ticket Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.squadDeathMatchCounter = valueAsInt;
            }
            else if (strVariable.CompareTo("Squad Death Match Time Limit Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.squadDeathMatchTime = valueAsInt;
            }
            else if (strVariable.CompareTo("Air Superiority Ticket Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.airSuperiorityCounter = valueAsInt;
            }
            else if (strVariable.CompareTo("Air Superiority Time Limit Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.airSuperiorityTime = valueAsInt;
            }
            else if (strVariable.CompareTo("Enable Ticket Preset Setter?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.enableCounterTimeRestart = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Ticket Percentage") == 0)
            {
                int valueAsInt;
                int.TryParse(strValue, out valueAsInt);
                this.gameModeCounter = valueAsInt;
            }
            else if (strVariable.CompareTo("Preset Value") == 0)
            {
                this.presetValue = strValue;
            }
        }

        public void checkCrash()
        {
            if (this.enableCounterTimeRestart == enumBoolYesNo.Yes)
            {
                this.checkingCrash = true;
                this.ExecuteCommand("procon.protected.send", "serverInfo");
            }
        }

        public void setPresetValue()
        {
            this.ExecuteCommand("procon.protected.send", "vars.preset", this.presetValue);
        }

        public override void OnServerInfo(CServerInfo serverInfo)
        {
            if (this.gettingGameMode)
            {
                this.currentGameMode = serverInfo.GameMode;

                this.gettingGameMode = false;
            }

            if (this.checkingCrash)
            {
                if (serverInfo.ServerUptime < 60)
                    this.setPresetValue();

                this.checkingCrash = false;
            }
        }

        public override void OnRestartLevel()
        {

        }

        public override void OnMaplistList(List<MaplistEntry> lstMaplist)
        {
            if (this.goingNextMap)
            {
                this.currentGameMode = lstMaplist[this.m_nextMap].Gamemode;

                if (lstMaplist[this.m_nextMap].Gamemode == "Obliteration")
                {
                    this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.obliterationCounter.ToString());
                    this.ExecuteCommand("procon.protected.send", "vars.roundTimeLimit", this.obliterationTime.ToString());
                }
                else if (lstMaplist[this.m_nextMap].Gamemode == "RushLarge0")
                {
                    this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.rushCounter.ToString());
                    this.ExecuteCommand("procon.protected.send", "vars.roundTimeLimit", this.rushTime.ToString());
                }
                else if (lstMaplist[this.m_nextMap].Gamemode == "ConquestLarge0" || lstMaplist[this.m_nextMap].Gamemode == "ConquestSmall0")
                {
                    this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.conquestCounter.ToString());
                    this.ExecuteCommand("procon.protected.send", "vars.roundTimeLimit", this.conquestTime.ToString());
                }
                else if (lstMaplist[this.m_nextMap].Gamemode == "TeamDeathMatch0")
                {
                    this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.tdmCounter.ToString());
                    this.ExecuteCommand("procon.protected.send", "vars.roundTimeLimit", this.tdmTime.ToString());
                }
                else if (lstMaplist[this.m_nextMap].Gamemode == "Elimination0")
                {
                    this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.eliminationCounter.ToString());
                    this.ExecuteCommand("procon.protected.send", "vars.roundTimeLimit", this.eliminationTime.ToString());
                }
                else if (lstMaplist[this.m_nextMap].Gamemode == "Domination0")
                {
                    this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.dominationCounter.ToString());
                    this.ExecuteCommand("procon.protected.send", "vars.roundTimeLimit", this.dominationTime.ToString());
                }
                else if (lstMaplist[this.m_nextMap].Gamemode == "SquadDeathMatch0")
                {
                    this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.squadDeathMatchCounter.ToString());
                    this.ExecuteCommand("procon.protected.send", "vars.roundTimeLimit", this.squadDeathMatchTime.ToString());
                }
                else if (lstMaplist[this.m_nextMap].Gamemode == "AirSuperiority0")
                {
                    this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.airSuperiorityCounter.ToString());
                    this.ExecuteCommand("procon.protected.send", "vars.roundTimeLimit", this.airSuperiorityTime.ToString());
                }

                this.goingNextMap = false;
                this.fromRoundOver = false;
                this.fromRunNext = false;
            }
        }

        public override void OnMaplistGetMapIndices(int mapIndex, int nextIndex)
        {
            if (this.fromRoundOver)
            {
                if (this.goingNextMap)
                    this.m_nextMap = nextIndex;

                this.ExecuteCommand("procon.protected.send", "mapList.list");
            }
            else if (this.fromRunNext)
            {
                if (this.goingNextMap)
                    this.m_nextMap = mapIndex;

                this.ExecuteCommand("procon.protected.send", "mapList.list");
            }
        }

        public override void OnMaplistGetRounds(int currentRound, int totalRounds)
        {
            if (this.fromRoundOver)
            {
                if ((currentRound + 1) >= totalRounds)
                    this.goingNextMap = true;

                this.ExecuteCommand("procon.protected.send", "mapList.getMapIndices");
            }
            else if (this.fromRunNext)
            {
                if (currentRound == 0)
                    this.goingNextMap = true;

                this.ExecuteCommand("procon.protected.send", "mapList.getMapIndices");
            }
        }

        public override void OnRunNextLevel()
        {
            if (this.enableOblRushMixed == enumBoolYesNo.Yes)
            {
                this.fromRunNext = true;
                this.ExecuteCommand("procon.protected.send", "mapList.getRounds");
            }

            if (this.enableCounterTimeRestart == enumBoolYesNo.Yes)
            {
                if (this.enableOblRushMixed == enumBoolYesNo.No)
                    this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.gameModeCounter.ToString());

                this.ExecuteCommand("procon.protected.tasks.add", "CTicketTimeSetterSetPresetValue", "120", "1", "1", "procon.protected.plugins.call", "CTicketTimeSetter", "setPresetValue");
            }
        }

        public override void OnRoundOver(int winningTeamId)
        {
            if (this.enableOblRushMixed == enumBoolYesNo.Yes)
            {
                this.fromRoundOver = true;
                this.ExecuteCommand("procon.protected.send", "mapList.getRounds");
            }

            if (this.enableCounterTimeRestart == enumBoolYesNo.Yes)
            {
                if (this.enableOblRushMixed == enumBoolYesNo.No)
                    this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.gameModeCounter.ToString());
                else
                {
                    if (this.currentGameMode == "Obliteration")
                        this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.obliterationCounter.ToString());
                    else if (this.currentGameMode == "RushLarge0")
                        this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.rushCounter.ToString());
                    else if (this.currentGameMode == "ConquestLarge0" || this.currentGameMode == "ConquestSmall0")
                        this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.conquestCounter.ToString());
                    else if (this.currentGameMode == "TeamDeathMatch0")
                        this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.tdmCounter.ToString());
                    else if (this.currentGameMode == "Elimination0")
                        this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.eliminationCounter.ToString());
                    else if (this.currentGameMode == "Domination0")
                        this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.dominationCounter.ToString());
                    else if (this.currentGameMode == "SquadDeathMatch0")
                        this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.squadDeathMatchCounter.ToString());
                    else if (this.currentGameMode == "AirSuperiority0")
                        this.ExecuteCommand("procon.protected.send", "vars.gameModeCounter", this.airSuperiorityCounter.ToString());
                }

                this.ExecuteCommand("procon.protected.tasks.add", "CTicketTimeSetterSetPresetValue", "120", "1", "1", "procon.protected.plugins.call", "CTicketTimeSetter", "setPresetValue");
            }
        }

        public override void OnEndRound(int iWinningTeamID)
        {
            this.OnRoundOver(iWinningTeamID);
        }

        public override void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal)
        {

        }

        public override void OnLevelStarted()
        {

        }

        public override void OnLevelLoaded(string mapFileName, string gamemode, int roundsPlayed, int roundsTotal)
        {

        }

    }
}
