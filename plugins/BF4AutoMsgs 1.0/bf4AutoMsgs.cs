using System;
using System.IO;
using System.Timers;
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

    using EventType = PRoCon.Core.Events.EventType;
    using CapturableEvent = PRoCon.Core.Events.CapturableEvents;

    public class bf4AutoMsgs : PRoConPluginAPI, IPRoConPluginInterface
    {
        private bool isEnabled = false;
        private enumBoolYesNo enableDebugMode = enumBoolYesNo.No;

        private enumBoolYesNo enableAutoMsgs = enumBoolYesNo.Yes;
        private Timer msgTimer;
        private bool msgTimerEnabled = false;
        private int currentMsg = 0;
        private int nextMsgTime = 20;
        private enumBoolYesNo sendToSquadOnly = enumBoolYesNo.Yes;

        private enumBoolYesNo enableWMsg = enumBoolYesNo.Yes;
        private string wMsg = "Welcome [playerName] to our server!";

        private enumBoolYesNo enableLeaveMsg = enumBoolYesNo.Yes;
        private string leaveMsg = "[playerName] Left the server";




        private List<string> messages = new List<string>();
        private List<string> players = new List<string>();

        public bf4AutoMsgs()
        {
        }

        public string GetPluginName()
        {
            return "BF4 Auto Messages";
        }

        public string GetPluginVersion()
        {
            return "0.0.1.4.2";
        }

        public string GetPluginAuthor()
        {
            return "Mike__MRM";
        }

        public string GetPluginWebsite()
        {
            return "www.mikemrm.com";
        }

        public string GetPluginDescription()
        {
            return "<script>document.body.innerHTML = '';</script><style>body{background-color:#000000; color:#FFFFFF;} h1{background-color:#000000;} a{color:#FFFFFF;text-decoration:underline;} a:hover{color:#FFFFFF;text-decoration:none;}</style><div style=\"display:block; width:100%; height:68px; background:url('http://mikemrm.com/procon/images/title.png') no-repeat;\"></div><div style=\"display:block; width:100%; height:40px; background:url('http://mikemrm.com/procon/" + this.GetPluginVersion() + "/version.png') no-repeat;\"></div><hr /><a href=\"http://mikemrm.com/procon/?version=" + this.GetPluginVersion() + "\" target=\"_CHECKUPDATE\"><img src=\"http://mikemrm.com/procon/images/checkforupdate.png\" width=\"197px\" height=\"40px\" border=\"0\" /></a><hr />This plugin allows you to have automated messages in your server<hr /><h1>Welcome Message</h1>This allows you to have a custom welcome message that is displayed to every player or the players squad when they join<br />User [playerName] to insert the players name that just spawned in.<hr /><h1>Leave Message</h1>This allows you to alert everyone to let them know that a specific player has left the server<br />Use [playerName] to insert the players name that just left the server.<hr /><h1>Auto Messages</h1>This allows you to setup automated messages that scroll throughout the game, it will automatically start at the first message and scroll through with a delay of so many seconds which can be set.<br /><br />You can have multiple lines print when a message pops up by typing the message then right after it put a | or {newline} which will print everything after it on the next line.<br /><br />For Example: you put <br />Message 1 Line 1|Message 1 Line 2 <br />or<br />Message 1 Line 1{newline}Message 1 Line 2<br />will display two different lines at the same time<hr />";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
        }

        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^b" + this.GetPluginName() + " ^2Enabled!");
            this.isEnabled = true;
            this.startMessages();
        }

        public void OnPluginDisable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^b" + this.GetPluginName() + " ^1Disabled =(");
            this.isEnabled = false;
            this.stopMessages();
        }

        public void startMessages()
        {
            this.tracedb("Checking if timer is already running...");
            if (this.msgTimerEnabled)
            {
                this.tracedb("Timer is running, shutting down");
                this.stopMessages();
            }
            this.tracedb("Initiating New Timer");
            this.msgTimer = new Timer();
            this.msgTimer.Elapsed += new ElapsedEventHandler(this.nextMsg);
            this.msgTimer.Interval = this.nextMsgTime * 1000;
            this.msgTimerEnabled = true;
            this.msgTimer.Start();
            this.tracedb("New Timer Initiated");
        }
        public void stopMessages()
        {
            this.tracedb("Shutting Down Timer");
            this.msgTimerEnabled = false;
            this.msgTimer.Stop();
            this.currentMsg = 0;
            this.tracedb("Timer Shutdown");
        }

        public void nextMsg(object source, ElapsedEventArgs e)
        {
            if (this.isEnabled && this.enableAutoMsgs.Equals(enumBoolYesNo.Yes))
            {
                if (this.currentMsg >= this.messages.Count)
                {
                    this.currentMsg = 0;
                }
                string oMsg = this.messages[this.currentMsg];
                if (oMsg.IndexOf("|") != -1)
                {
                    for (int j = 0; j < oMsg.Split('|').Length; j++)
                    {
                        this.sayMsg(oMsg.Split('|')[j]);
                    }
                }
                else
                {
                    if (oMsg.IndexOf("{newline}") != -1)
                    {
                        for (int j = 0; j < Regex.Split(oMsg, "{newline}").Length; j++)
                        {
                            this.sayMsg(Regex.Split(oMsg, "{newline}")[j]);
                        }
                    }
                    else
                    {
                        this.sayMsg(oMsg);
                    }
                }
                this.currentMsg += 1;
            }
        }

        public void sayMsg(string msg)
        {
            this.ExecuteCommand("procon.protected.send", "admin.say", msg, "all");
        }

        public void sayMsg(string msg, string player)
        {
            this.ExecuteCommand("procon.protected.send", "admin.say", msg, "player", player);
        }

        public void sendWelcomeMessage(string player)
        {
            string msg = this.wMsg.Replace("[playerName]", player);
            if (this.sendToSquadOnly.Equals(enumBoolYesNo.Yes))
            {
                this.sayMsg(msg, player);
            }
            else
            {
                this.sayMsg(msg);
            }
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {

            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("1 Welcome Message|Enable Welcome Message", typeof(enumBoolYesNo), this.enableWMsg));
            lstReturn.Add(new CPluginVariable("1 Welcome Message|---- Send to players squad", typeof(enumBoolYesNo), this.sendToSquadOnly));
            lstReturn.Add(new CPluginVariable("1 Welcome Message|---- Message", typeof(string), this.wMsg));
            lstReturn.Add(new CPluginVariable("2 Player Left Message|Enable Player Left Server Message", typeof(enumBoolYesNo), this.enableLeaveMsg));
            lstReturn.Add(new CPluginVariable("2 Player Left Message|---- Left Message", typeof(string), this.leaveMsg));
            lstReturn.Add(new CPluginVariable("3 Auto Messages|Enable Auto Messages", typeof(enumBoolYesNo), this.enableAutoMsgs));
            lstReturn.Add(new CPluginVariable("3 Auto Messages|---- Message Delay Time (seconds)", typeof(int), this.nextMsgTime));
            lstReturn.Add(new CPluginVariable("3 Auto Messages|---- Auto Messages", typeof(string[]), this.messages.ToArray()));
            lstReturn.Add(new CPluginVariable("4 Debug Settings|Enable Debug Mode", typeof(enumBoolYesNo), this.enableDebugMode));
            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("Enable Welcome Message", typeof(enumBoolYesNo), this.enableWMsg));
            lstReturn.Add(new CPluginVariable("---- Send to players squad", typeof(enumBoolYesNo), this.sendToSquadOnly));
            lstReturn.Add(new CPluginVariable("---- Message", typeof(string), this.wMsg));
            lstReturn.Add(new CPluginVariable("Enable Player Left Server Message", typeof(enumBoolYesNo), this.enableLeaveMsg));
            lstReturn.Add(new CPluginVariable("---- Left Message", typeof(string), this.leaveMsg));
            lstReturn.Add(new CPluginVariable("Enable Auto Messages", typeof(enumBoolYesNo), this.enableAutoMsgs));
            lstReturn.Add(new CPluginVariable("---- Message Delay Time (seconds)", typeof(int), this.nextMsgTime));
            lstReturn.Add(new CPluginVariable("---- Auto Messages", typeof(string[]), this.messages.ToArray()));
            lstReturn.Add(new CPluginVariable("Enable Debug Mode", typeof(enumBoolYesNo), this.enableDebugMode));

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            if ("Enable Welcome Message".Equals(strVariable) && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true) this.enableWMsg = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            if ("---- Send to players squad".Equals(strVariable) && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true) this.sendToSquadOnly = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            if ("---- Message".Equals(strVariable)) this.wMsg = strValue;
            if ("Enable Player Left Server Message".Equals(strVariable) && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true) this.enableLeaveMsg = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            if ("---- Left Message".Equals(strVariable)) this.leaveMsg = strValue;
            if ("Enable Auto Messages".Equals(strVariable) && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                this.enableAutoMsgs = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                if (this.isEnabled)
                {
                    if (this.enableAutoMsgs.Equals(enumBoolYesNo.Yes))
                    {
                        this.startMessages();
                    }
                    else
                    {
                        this.stopMessages();
                    }
                }
            }
            if ("---- Message Delay Time (seconds)".Equals(strVariable))
            {
                this.nextMsgTime = Convert.ToInt32(strValue);
                this.msgTimer.Interval = this.nextMsgTime * 1000;
            }
            if ("---- Auto Messages".Equals(strVariable)) this.messages = new List<string>(CPluginVariable.DecodeStringArray(strValue));
            if ("Enable Debug Mode".Equals(strVariable) && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true) this.enableDebugMode = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
        }

        public void OnAccountCreated(string strUsername)
        {

        }

        public void trace(string message)
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^b" + message);
        }
        public void tracedb(string message)
        {
            if (this.enableDebugMode.Equals(enumBoolYesNo.Yes))
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^b" + GetPluginName() + " DEBUG - " + message);
            }
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
            if (this.isEnabled && this.enableLeaveMsg.Equals(enumBoolYesNo.Yes))
            {
                if (this.players.Contains(strSoldierName))
                {
                    this.players.Remove(strSoldierName);
                }
                string msg = this.leaveMsg.Replace("[playerName]", strSoldierName);
                this.sayMsg(msg);
            }
        }

        public void OnPlayerKilled(string strKillerSoldierName, string strVictimSoldierName)
        {
        }

        public void OnPlayerKilled(Kill kKillerVictimDetails)
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


        #region IPRoConPluginInterface1

        // Level Events
        public void OnRunNextLevel()
        {
        }

        public void OnRoundOver(int iWinningTeamID)
        {
        }

        public void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
        {
            if (this.isEnabled && this.enableWMsg.Equals(enumBoolYesNo.Yes))
            {
                if (!this.players.Contains(soldierName))
                {
                    this.players.Add(soldierName);
                    this.sendWelcomeMessage(soldierName);
                }
            }
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

    }
}