using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Text.RegularExpressions;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;
using PRoCon.Core.Plugin.Commands;

namespace PRoConEvents
{
    public class TeamSwapQueue : PRoConPluginAPI, IPRoConPluginInterface
    {
        #region Variables

        //Instantiate variables
        private List<string> team1Queue;
        private List<string> team2Queue;
        private List<string> team3Queue;
        private List<string> team4Queue;
        private List<string> inGameMessages;
        private CPlayerSubset team1Subset;
        private CPlayerSubset team2Subset;
        private CPlayerSubset team3Subset;
        private CPlayerSubset team4Subset;
        private PlayerInformationDictionary playerList;
        private int infoMessageInterval;
        private bool playersUpdated;
        private bool updatingPlayers;
        private bool teamsUpdated;
        private bool updatingTeams;
        private bool pluginEnabled;
        private string switchCommand;
        private string cancelCommand;
        private string infoMessage;
        private enumBoolYesNo enableForceSwitch;
        private enumBoolYesNo enableDebugging;
        private enumBoolYesNo enableInfoMessage;

        #endregion

        #region Initialization

        //Constructor -- Initialize variables
        public TeamSwapQueue()
        {
            this.enableForceSwitch = enumBoolYesNo.Yes;
            this.switchCommand = "@switchme";
            this.cancelCommand = "@cancelswitch";
            this.enableDebugging = enumBoolYesNo.No;

            this.enableInfoMessage = enumBoolYesNo.No;
            this.infoMessage = "This server is running Team Swap Queue. Type @switchme to queue for a team switch!";
            this.infoMessageInterval = 300;

            this.pluginEnabled = false;

            this.team1Queue = new List<string>();
            this.team2Queue = new List<string>();
            this.team3Queue = new List<string>();
            this.team4Queue = new List<string>();

            this.buildTeams();

            this.playerList = new PlayerInformationDictionary();
            this.playerList.ExecuteCommand += new PlayerInformationDictionary.ExecuteCommandHandler(playerList_ExecuteCommand);
            this.playerList.debugWrite += new PlayerInformationDictionary.debugWriteHandler(playerList_debugWrite);
            this.playerList.processMessage += new PlayerInformationDictionary.ProcessMessageHandler(playerList_processMessage);

            this.inGameMessages = new List<string>();
            this.initMessages();
        }

        /// <summary>
        /// Retrieves plugin name
        /// </summary>
        /// <returns>string pluginName</returns>
        public string GetPluginName()
        {
            return "Team Swap Queue";
        }

        /// <summary>
        ///Retrieves plugin version
        /// </summary>
        /// <returns>string pluginVersion</returns>
        public string GetPluginVersion()
        {
            return "0.1.5";
        }

        /// <summary>
        /// Retrieves plugin author
        /// </summary>
        /// <returns>string pluginAuthor</returns>
        public string GetPluginAuthor()
        {
            return "Archangel-HER0-";
        }

        /// <summary>
        /// Retrieves plugin website
        /// </summary>
        /// <returns>string pluginWebsite</returns>
        public string GetPluginWebsite()
        {
            return "www.hnvclan.com";
        }

        /// <summary>
        /// Retrieves plugin description
        /// </summary>
        /// <returns>string pluginDescription</returns>
        public string GetPluginDescription()
        {
            return @"
                <h2>Description</h2>
                    <p>This plugin allows players to queue to switch teams when a slot on the opposing team opens up. Using this tool, players will not have to continually watch the scoreboard to see when they can change, but instead can rely on the server to switch them over when there is an opening. Additionally, players being told they cannot switch teams so often (due to autobalance or otherwise) may still join their friends on the opposing team.</p>
                    <h2>In-Game Commands</h2>
                        <blockquote><h4>@switchme <optional: int teamId></h4> Allows the player to add themself to a team switch queue.</blockquote>
                        <blockquote><h4>@cancelswitch</h4> Allows the player to cancel their request to switch teams.</blockquote>
                    <h2>Settings</h2>
                        <blockquote><h4>Enable Force Switching?</h4> When enabled, will switch queued players immediately when they can be moved. This will slay the player in the process, but will not add to their deaths.</blockquote>
                        <blockquote><h4>Switch Command</h4> The command used to add a player to the team switch queue.</blockquote>
                        <blockquote><h4>Cancel Command</h4> The command used to cancel a player's team switch request.</blockquote>
                        <blockquote><h4>Enable Scrolling Info Message?</h4> When enabled, will display a scrolling message at a specified interval (default 300 seconds).</blockquote>
                        <blockquote><h4>Info Message</h4> The message that will be displayed.</blockquote>
                        <blockquote><h4>Interval (seconds)</h4> The interval (in seconds) for displaying the Info Message. Minimum of 30 seconds.</blockquote>
                        <blockquote><h4>Enable Debugging?</h4> When enabled, will display debugging info in the plugin console.</blockquote>
                ";
        }

        /// <summary>
        /// Registers necessary events with PRoCon
        /// </summary>
        /// <param name="strHostName"></param>
        /// <param name="strPort"></param>
        /// <param name="strPRoConVersion"></param>
        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name, "OnGlobalChat", "OnListPlayers", "OnPlayerTeamChange", "OnPlayerMovedByAdmin", "OnRoundOver", "OnPlayerKilled", "OnPlayerSpawned", "OnLeave", "OnVersion");
        }

        /// <summary>
        /// Announces plugin enable
        /// </summary>
        public void OnPluginEnable()
        {
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bTeam Swap Queue ^2Enabled!");
            this.pluginEnabled = true;
            this.updateInfoMessage();
            this.updatePlayerList();
            this.buildTeams();
        }

        /// <summary>
        /// Announces plugin disable
        /// </summary>
        public void OnPluginDisable()
        {
            this.pluginEnabled = false;
            this.ExecuteCommand("procon.protected.tasks.remove", "TeamSwitchQueue");
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bTeam Swap Queue ^1Disabled!");
        }

        /// <summary>
        /// Retrieves list of displayed plugin variables
        /// </summary>
        /// <returns>List of displayed plugin variables</returns>
        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Settings|Enable Force Switching?", typeof(enumBoolYesNo), enableForceSwitch));
            lstReturn.Add(new CPluginVariable("Settings|Enable Info Message?", typeof(enumBoolYesNo), enableInfoMessage));
            if (enableInfoMessage == enumBoolYesNo.Yes)
            {
                lstReturn.Add(new CPluginVariable("Settings|Info Message", infoMessage.GetType(), infoMessage));
                lstReturn.Add(new CPluginVariable("Settings|Interval (seconds)", infoMessageInterval.GetType(), infoMessageInterval));
            }
            lstReturn.Add(new CPluginVariable("In-Game Commands|Switch Command", switchCommand.GetType(), switchCommand));
            lstReturn.Add(new CPluginVariable("In-Game Commands|Cancel Command", cancelCommand.GetType(), cancelCommand));
            lstReturn.Add(new CPluginVariable("Debugging|Enable Debugging?", typeof(enumBoolYesNo), enableDebugging));

            return lstReturn;
        }

        /// <summary>
        /// Retrieves full list of plugin variables
        /// </summary>
        /// <returns>Full list of plugin variables</returns>
        public List<CPluginVariable> GetPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("Enable Force Switching?", typeof(enumBoolYesNo), enableForceSwitch));
            lstReturn.Add(new CPluginVariable("Enable Info Message?", typeof(enumBoolYesNo), enableInfoMessage));
            lstReturn.Add(new CPluginVariable("Info Message", infoMessage.GetType(), infoMessage));
            lstReturn.Add(new CPluginVariable("Interval (seconds)", infoMessageInterval.GetType(), infoMessageInterval));
            lstReturn.Add(new CPluginVariable("Switch Command", switchCommand.GetType(), switchCommand));
            lstReturn.Add(new CPluginVariable("Cancel Command", cancelCommand.GetType(), cancelCommand));
            lstReturn.Add(new CPluginVariable("Enable Debugging?", typeof(enumBoolYesNo), enableDebugging));

            return lstReturn;
        }

        /// <summary>
        /// Handles changing of plugin variables
        /// </summary>
        /// <param name="strVariable">Variable name</param>
        /// <param name="strValue">New Value</param>
        public void SetPluginVariable(string strVariable, string strValue)
        {
            int iTimeSeconds = infoMessageInterval;

            if (strVariable.CompareTo("Enable Force Switching?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                enableForceSwitch = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
            else if (strVariable.CompareTo("Enable Info Message?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                enableInfoMessage = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                updateInfoMessage();
            }
            else if (strVariable.CompareTo("Info Message") == 0)
            {
                if (strValue != "")
                {
                    infoMessage = strValue;
                }
                updateInfoMessage();
            }
            else if (strVariable.CompareTo("Interval (seconds)") == 0 && int.TryParse(strValue, out iTimeSeconds) == true)
            {
                if (iTimeSeconds >= 30)
                {
                    infoMessageInterval = iTimeSeconds;
                }

                updateInfoMessage();
            }
            else if (strVariable.CompareTo("Switch Command") == 0)
            {
                if (strValue == "")
                {
                    switchCommand = "@switchme";
                }
                else
                {
                    switchCommand = strValue;
                }
            }
            else if (strVariable.CompareTo("Cancel Command") == 0)
            {
                if (strValue == "")
                {
                    cancelCommand = "@cancelswitch";
                }
                else
                {
                    cancelCommand = strValue;
                }
            }
            else if (strVariable.CompareTo("Enable Debugging?") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
            {
                enableDebugging = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
            }
        }

        /// <summary>
        /// Makes list of In-Game Messages
        /// </summary>
        private void initMessages()
        {
            inGameMessages.Add("You have been added to the queue to join %1%.");
            inGameMessages.Add("You are currently #%1% in queue");
            inGameMessages.Add("You have been removed from Team %1%'s queue");
            inGameMessages.Add("A position has opened!");
            inGameMessages.Add("A position has opened! You will be moved on next death!");
            inGameMessages.Add("Moving you now!");
            inGameMessages.Add("A player from the other team is also in queue. Swapping places now!");
            inGameMessages.Add("You are already in queue for Team %1%. You must leave this queue (%2%) before you may join a different one.)");
            inGameMessages.Add("You are already on Team %1%!");
            inGameMessages.Add("You were not in any queues.");
        }

        private void updateInfoMessage()
        {
            this.ExecuteCommand("procon.protected.tasks.remove", "TeamSwitchQueue");

            if (this.pluginEnabled && (this.enableInfoMessage == enumBoolYesNo.Yes))
            {
                this.ExecuteCommand("procon.protected.tasks.add", "TeamSwitchQueue", this.infoMessageInterval.ToString(), this.infoMessageInterval.ToString(), "-1", "procon.protected.send", "admin.say", infoMessage, "all");
            }
        }

        #endregion

        /// <summary>
        /// Fills in information and displays in-game messages
        /// </summary>
        /// <param name="messageLine">Index of the desired message</param>
        /// <param name="items">Extra pieces of information</param>
        private void processMessage(int messageLine, params object[] items)
        {
            string target = items[0].ToString();
            string message = inGameMessages[messageLine];

            for (int i = 0; i < items.Length; i++)
            {
                message = message.Replace("%" + i.ToString() + "%", items[i].ToString());
            }

            this.ExecuteCommand("procon.protected.send", "admin.say", message, "player", target);
        }

        /// <summary>
        /// Processes a switch request
        /// </summary>
        /// <param name="player">Player Name</param>
        /// <param name="request">Request and arguments</param>
        private void processRequest(string player, string request)
        {
            debugWrite("processRequest(): " + player + " requested team change!");

            string[] args = request.Split(' ');

            updatePlayerList();

            List<List<string>> queues = new List<List<string>>();
            queues.Add(team1Queue);
            queues.Add(team2Queue);
            queues.Add(team3Queue);
            queues.Add(team4Queue);

            if (this.playerList[player].QueuedForTeam != 0)
            {
                processMessage(7, player, this.playerList[player].QueuedForTeam, cancelCommand);
            }
            if (this.playerList[player].QueuedForTeam == 0)
            {
                if (args.Length == 1)
                {
                    int targetTeam = 0;
                    int sourceTeam = playerList[player].basicInfo.TeamID;

                    switch (sourceTeam)
                    {
                        case 1:
                            targetTeam = 2;
                            break;
                        case 2:
                            targetTeam = 1;
                            break;
                        case 3:
                            targetTeam = 1;
                            break;
                        case 4:
                            targetTeam = 1;
                            break;
                        default:
                            debugWrite("processRequest(): " + player + " has invalid Team ID: " + sourceTeam);
                            break;
                    }

                    addToQueue(player, targetTeam);
                }
                else if (args.Length == 2)
                {
                    int targetTeam = Convert.ToInt32(args[1]);

                    bool alreadyOnTeam = false;

                    foreach (PlayerInfo soldier in playerList)
                    {
                        if (soldier.SoldierName.CompareTo(player) == 0)
                        {
                            alreadyOnTeam = true;
                            break;
                        }
                    }

                    if (!alreadyOnTeam)
                    {
                        debugWrite("processRequest(): Adding " + player + " to queue!");
                        addToQueue(player, targetTeam);
                    }
                    else
                    {
                        processMessage(8, player, targetTeam);
                        debugWrite("processRequest(): Not adding " + player + " to queue - they are already on team " + targetTeam);
                    }
                }
                else
                {
                    debugWrite("processRequest(): Incorrect number of arguments");
                }
            }
        }

        /// <summary>
        /// Processes a cancel switch request
        /// </summary>
        /// <param name="player">Player Name</param>
        private void processCancel(string player)
        {
            if (this.playerList[player].QueuedForTeam != 0)
            {
                if (team1Queue.Contains(player))
                {
                    removeFromQueue(player, 1);
                    this.processMessage(2, player, 1);
                }
                else if (team2Queue.Contains(player))
                {
                    removeFromQueue(player, 2);
                    this.processMessage(2, player, 2);
                }
                else if (team3Queue.Contains(player))
                {
                    removeFromQueue(player, 3);
                    this.processMessage(2, player, 3);
                }
                else if (team4Queue.Contains(player))
                {
                    removeFromQueue(player, 4);
                    this.processMessage(2, player, 4);
                }
                else if (this.playerList[player].MoveLocation != null)
                {
                    this.processMessage(2, player, this.playerList[player].MoveLocation.TeamID);
                }
                this.playerList[player].MoveLocation = null;
                this.playerList[player].QueuedForTeam = 0;
            }
            else
            {
                debugWrite("processCancel(): " + player + " was not in a queue");
                processMessage(9, player);
            }
        }

        /// <summary>
        /// Adds a player to be switched to another team
        /// </summary>
        /// <param name="player">Player Name</param>
        /// <param name="targetTeam">Target Team ID</param>
        private void addToQueue(string player, int targetTeam)
        {
            debugWrite("addToQueue(): Team only version called");
            if (targetTeam == 1)
            {
                playerList[player].EndTarget = team1Subset;
                playerList[player].QueuedForTeam = targetTeam;
                processMessage(0, player, "Team 1");
                team1Queue.Add(player);
            }
            else if (targetTeam == 2)
            {
                playerList[player].EndTarget = team2Subset;
                playerList[player].QueuedForTeam = targetTeam;
                processMessage(0, player, "Team 2");
                team2Queue.Add(player);
            }
            processMessage(1, player, getQueuePosition(player));
            checkQueues();
        }

        /// <summary>
        /// Adds a player to a queue with a specific team and squad in mind
        /// </summary>
        /// <param name="player">Player name</param>
        /// <param name="targetTeam">Target Team ID</param>
        /// <param name="targetSquad">Target Squad ID</param>
        private void addToQueue(string player, int targetTeam, int targetSquad)
        {
            debugWrite("addToQueue(): Team and Squad version called");
        }

        /// <summary>
        /// Removes a player from queue
        /// </summary>
        /// <param name="player">Player name</param>
        /// <param name="targetTeam">Team queued for</param>
        private void removeFromQueue(string player, int targetTeam)
        {
            debugWrite("removeFromQueue(): Removing " + player + " from team " + targetTeam + "'s queue");
            switch (targetTeam)
            {
                case 1:
                    team1Queue.Remove(player);
                    break;
                case 2:
                    team2Queue.Remove(player);
                    break;
                case 3:
                    team3Queue.Remove(player);
                    break;
                case 4:
                    team4Queue.Remove(player);
                    break;
                default:
                    debugWrite("removeFromQueue(): Invalid team passed to removeFromQueue()");
                    break;
            }
            if (!(targetTeam == 0))
            {
                playerList[player].EndTarget = null;
                updateQueue(targetTeam);
            }
        }

        /// <summary>
        /// Get the player's queue position
        /// </summary>
        /// <param name="player">Player's name</param>
        /// <returns>Player's position in queue</returns>
        private int getQueuePosition(string player)
        {
            if (team1Queue.Contains(player))
            {
                return (team1Queue.IndexOf(player) + 1);
            }
            if (team2Queue.Contains(player))
            {
                return (team2Queue.IndexOf(player) + 1);
            }
            if (team3Queue.Contains(player))
            {
                return (team3Queue.IndexOf(player) + 1);
            }
            if (team4Queue.Contains(player))
            {
                return (team4Queue.IndexOf(player) + 1);
            }

            return 0;
        }

        /// <summary>
        /// Update members of the queue when their position changes
        /// </summary>
        /// <param name="TeamID">ID of the queue's target team</param>
        public void updateQueue(int TeamID)
        {
            if (TeamID == 1)
            {
                for (int i = 0; i < team1Queue.Count; i++)
                {
                    processMessage(1, team1Queue[i], i);
                }
            }
            else if (TeamID == 2)
            {
                for (int i = 0; i < team2Queue.Count; i++)
                {
                    processMessage(1, team2Queue[i], getQueuePosition(team2Queue[i]));
                }
            }
            else if (TeamID == 3)
            {
                for (int i = 0; i < team3Queue.Count; i++)
                {
                    processMessage(1, team3Queue[i], getQueuePosition(team3Queue[i]));
                }
            }
            else if (TeamID == 4)
            {
                for (int i = 0; i < team4Queue.Count; i++)
                {
                    processMessage(1, team4Queue[i], getQueuePosition(team4Queue[i]));
                }
            }
            else
            {
                debugWrite("updateQueue(): Could not update queue - Invalid Team ID");
            }
            checkQueues();
        }

        /// <summary>
        /// Checks whether the next player in queue can be switched
        /// </summary>
        private void checkQueues()
        {
            if (!playersUpdated || !teamsUpdated)
            {
                if (!playersUpdated)
                {
                    debugWrite("checkQueues(): Updating player list");
                    updatePlayerList();
                }
                if (!teamsUpdated)
                {
                    debugWrite("checkQueues(): Building teams");
                    buildTeams();
                }
            }
            else
            {
                playersUpdated = false;
                teamsUpdated = false;
                if (team1Queue.Count > 0)
                {
                    foreach (PlayerInfo player in playerList)
                    {
                        debugWrite("checkQueues(): Checking for " + player.SoldierName + " in Queue 1");
                        if (player.SoldierName.CompareTo(team1Queue[0]) == 0)
                        {
                            debugWrite("checkQueues(): Found player " + player.SoldierName + " at front of Team 1's queue");
                            checkTeams(player.basicInfo.TeamID, team1Subset);
                            break;
                        }
                    }
                }
                if (team2Queue.Count > 0)
                {
                    foreach (PlayerInfo player in playerList)
                    {
                        if (player.SoldierName.CompareTo(team2Queue[0]) == 0)
                        {
                            debugWrite("checkQueues(): Found player " + player.SoldierName + " at front of Team 2's queue");
                            checkTeams(player.basicInfo.TeamID, team2Subset);
                            break;
                        }
                    }
                }
                if (team3Queue.Count > 0)
                {
                    foreach (PlayerInfo player in playerList)
                    {
                        if (player.SoldierName.CompareTo(team3Queue[0]) == 0)
                        {
                            debugWrite("checkQueues(): Found player " + player.SoldierName + " at front of Team 3's queue");
                            checkTeams(player.basicInfo.TeamID, team3Subset);
                            break;
                        }
                    }
                }
                if (team4Queue.Count > 0)
                {
                    foreach (PlayerInfo player in playerList)
                    {
                        if (player.SoldierName.CompareTo(team4Queue[0]) == 0)
                        {
                            debugWrite("checkQueues(): Found player " + player.SoldierName + " at front of Team 4's queue");
                            checkTeams(player.basicInfo.TeamID, team4Subset);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks for openings on a target team
        /// </summary>
        /// <param name="sourceTeam">Team the player is trying to switch from</param>
        /// <param name="targetTeam">Team to look for openings on</param>
        private void checkTeams(int sourceTeam, CPlayerSubset targetTeam)
        {
            debugWrite("checkTeams(): Function Called");

            List<List<string>> queues = new List<List<string>>();
            queues.Add(team1Queue);
            queues.Add(team2Queue);
            queues.Add(team3Queue);
            queues.Add(team4Queue);
            if (targetTeam != null)
            {
                debugWrite("checkTeams(): Almost checking team sizes");
                if (this.playerList.TeamSize(sourceTeam) > this.playerList.TeamSize(targetTeam))
                {
                    debugWrite("checkTeams(): Calling switchTeam(" + queues[targetTeam.TeamID - 1][0] + ", " + targetTeam + ")");
                    switchTeam(queues[targetTeam.TeamID - 1][0], this.GetMoveDestination(targetTeam));
                    removeFromQueue(queues[targetTeam.TeamID - 1][0], targetTeam.TeamID);
                }
            }
            else
            {
                debugWrite("checkTeams(): Null targetTeam value");
            }
        }

        /// <summary>
        /// Sends command to switch a player's team
        /// </summary>
        /// <param name="player">Target player's name</param>
        /// <param name="targetSubset">Subset that the player should be switched to</param>
        private void switchTeam(string player, CPlayerSubset targetTeam)
        {
            debugWrite("switchTeam() called with arguments player: " + player + " - targetTeam: " + targetTeam);
            if (enableForceSwitch == enumBoolYesNo.Yes || playerList[player].IsAlive == false)
            {
                playerList.ForceMovePlayer(player, targetTeam);
                //processMessage(3, player);
            }
            else
            {
                playerList.QueueMovePlayer(player, targetTeam);
                processMessage(4, player);
            }
            removeFromQueue(player, targetTeam.TeamID);
        }


        /// <summary>
        /// Updates the list of players on a specified team
        /// </summary>
        /// <param name="TeamID"></param>
        private void buildTeams()
        {
            updatingTeams = true;
            for (int i = 1; i <= 4; i++)
            {
                this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "team", i.ToString());
            }
        }

        /// <summary>
        /// Updates the full list of players
        /// </summary>
        private void updatePlayerList()
        {
            updatingPlayers = true;
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
        }

        private CPlayerSubset GetMoveDestination(CPlayerSubset subset)
        {
            CPlayerSubset moveDestination = new CPlayerSubset(CPlayerSubset.PlayerSubsetType.None, subset.TeamID, 0);
            return moveDestination;
        }

        /// <summary>
        /// Checks chat messages for team switch requests
        /// </summary>
        /// <param name="speaker">Player name</param>
        /// <param name="message">Chat Message</param>
        public override void OnGlobalChat(string speaker, string message)
        {
            updatePlayerList();

            if (message.StartsWith(switchCommand))
            {
                processRequest(speaker, message);
            }
            else if (message.StartsWith(cancelCommand))
            {
                processCancel(speaker);
            }
            else if (message.StartsWith("@myteam"))
            {
                foreach (PlayerInfo player in playerList)
                {
                    if (player.SoldierName.CompareTo(speaker) == 0)
                    {
                        debugWrite(player.SoldierName + " is on " + player.basicInfo.TeamID);
                    }
                }
            }
        }

        /// <summary>
        /// Checks team switch events to see if a spot has been opened
        /// </summary>
        /// <param name="soldierName">Name of switched player</param>
        /// <param name="teamId">Team ID</param>
        /// <param name="squadId">Squad ID</param>
        public override void OnPlayerTeamChange(string soldierName, int teamId, int squadId)
        {
            debugWrite("OnPlayerTeamChange(): " + soldierName + " moved to Team " + teamId + "; Squad " + squadId);
            checkQueues();
        }

        /// <summary>
        /// Checks player killed events to see if the dead player should be switched
        /// </summary>
        /// <param name="kKillerVictimDetails">Details of the kill event</param>
        public override void OnPlayerKilled(Kill kKillerVictimDetails)
        {
            this.playerList[kKillerVictimDetails.Victim.SoldierName].IsAlive = false;
            this.playerList.MoveQueuedPlayer(kKillerVictimDetails.Victim.SoldierName);
        }

        public override void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
        {
            this.playerList[soldierName].IsAlive = true;
        }

        /// <summary>
        /// Checks the list of players to find out what team a given player is on
        /// </summary>
        /// <param name="players">List of players</param>
        /// <param name="subset">Subset of players</param>
        public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset)
        {
            if (subset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                foreach (CPlayerInfo player in players)
                {
                    this.playerList.UpdatePlayer(player.SoldierName, player);
                }

                foreach (PlayerInfo storedPlayer in playerList)
                {
                    bool isInList = false;

                    foreach (CPlayerInfo playerInfo in players)
                    {
                        if (string.Compare(storedPlayer.SoldierName, playerInfo.SoldierName) == 0)
                        {
                            isInList = true;
                            break;
                        }
                    }

                    if (!isInList)
                    {
                        this.playerList.RemovePlayer(storedPlayer.SoldierName);
                    }
                }

                if ((!playersUpdated) && updatingPlayers)
                {
                    playersUpdated = true;
                    updatingPlayers = false;
                    checkQueues();
                }
            }
            else if (subset.Subset == CPlayerSubset.PlayerSubsetType.Team)
            {
                switch (subset.TeamID)
                {
                    case 1:
                        team1Subset = subset;
                        break;
                    case 2:
                        team2Subset = subset;
                        break;
                    case 3:
                        team3Subset = subset;
                        break;
                    case 4:
                        team4Subset = subset;
                        if (!teamsUpdated && updatingTeams)
                        {
                            teamsUpdated = true;
                            updatingTeams = false;
                            checkQueues();
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Removes a player from any queues and updates player and team lists
        /// </summary>
        /// <param name="playerInfo">Info for the player leaving the server</param>
        public override void OnPlayerLeft(CPlayerInfo playerInfo)
        {
            if (team1Queue.Contains(playerInfo.SoldierName))
            {
                removeFromQueue(playerInfo.SoldierName, 1);
            }
            if (team2Queue.Contains(playerInfo.SoldierName))
            {
                removeFromQueue(playerInfo.SoldierName, 2);
            }
            if (team3Queue.Contains(playerInfo.SoldierName))
            {
                removeFromQueue(playerInfo.SoldierName, 3);
            }
            if (team4Queue.Contains(playerInfo.SoldierName))
            {
                removeFromQueue(playerInfo.SoldierName, 4);
            }
            checkQueues();
        }

        /// <summary>
        /// Write debug messages to console when debugging mode is active
        /// </summary>
        /// <param name="message">Debugging message</param>
        private void debugWrite(string message)
        {
            if (enableDebugging == enumBoolYesNo.Yes)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "^bTeam Switch (Debug): " + message);
            }
        }

        private void playerList_ExecuteCommand(params string[] words)
        {
            this.ExecuteCommand(words);
        }

        private void playerList_processMessage(int messageLine, params object[] items)
        {
            this.processMessage(messageLine, items);
        }

        private void playerList_debugWrite(string message)
        {
            this.debugWrite(message);
        }

        #region Internal Player Info

        internal class PlayerInformationDictionary : KeyedCollection<string, PlayerInfo>
        {
            public delegate void ProcessMessageHandler(int messageLine, params object[] items);
            public event ProcessMessageHandler processMessage;

            public delegate void ExecuteCommandHandler(params string[] words);
            public event ExecuteCommandHandler ExecuteCommand;

            public delegate void debugWriteHandler(string message);
            public event debugWriteHandler debugWrite;

            public PlayerInformationDictionary()
            {
            }

            protected override string GetKeyForItem(PlayerInfo item)
            {
                return item.SoldierName;
            }

            public List<string> GetSoldierNameKeys()
            {
                List<string> soldierNames = new List<string>();

                foreach (PlayerInfo player in this)
                {
                    soldierNames.Add(player.SoldierName);
                }

                return soldierNames;
            }

            public void QueueMovePlayer(string targetSoldierName, CPlayerSubset newLocation)
            {
                debugWrite("QueueMovePlayer(): Function Called");
                if (this.Contains(targetSoldierName))
                {
                    this[targetSoldierName].MoveLocation = newLocation;
                }
            }

            public void ForceMovePlayer(string targetSoldierName, CPlayerSubset newLocation)
            {
                debugWrite("ForceMovePlayer(): Function Called");
                if (this.Contains(targetSoldierName))
                {
                    this[targetSoldierName].MoveLocation = newLocation;
                }

                MoveQueuedPlayer(targetSoldierName);
            }

            public void MoveQueuedPlayer(string targetSoldierName)
            {
                debugWrite("MoveQueuedPlayer(): Function Called");
                if (this.Contains(targetSoldierName))
                {
                    if (this[targetSoldierName].MoveLocation != null)
                    {
                        this.processMessage(5, targetSoldierName);
                        this.ExecuteCommand("procon.protected.send", "admin.movePlayer", targetSoldierName, this[targetSoldierName].MoveLocation.TeamID.ToString(), this[targetSoldierName].MoveLocation.SquadID.ToString(), "true");

                        this[targetSoldierName].MoveLocation = null;
                        this[targetSoldierName].QueuedForTeam = 0;
                    }
                }
            }

            public void UpdatePlayer(string soldierName, CPlayerInfo basicInformation)
            {
                if (this.Contains(soldierName))
                {
                    if (basicInformation != null)
                    {
                        this[soldierName].basicInfo = basicInformation;
                    }
                }
                else
                {
                    this.Add(new PlayerInfo(basicInformation));
                }
            }

            public int TeamSize(CPlayerSubset subset)
            {
                debugWrite("TeamSize(): Subset version called");
                int count = 0;

                foreach (PlayerInfo player in this)
                {
                    if (player.basicInfo.TeamID == subset.TeamID)
                    {
                        count++;
                    }
                }

                return count;
            }

            public int TeamSize(int TeamID)
            {
                debugWrite("TeamSize(): Integer version called");
                int count = 0;

                foreach (PlayerInfo player in this)
                {
                    if (player.basicInfo.TeamID == TeamID)
                    {
                        count++;
                    }
                }

                return count;
            }

            public void RemovePlayer(string soldierName)
            {
                if (String.Compare(soldierName, "Server", true) != 0 && this.Contains(soldierName))
                {
                    this.Remove(soldierName);
                }
            }
        }

        internal class PlayerInfo
        {
            private CPlayerInfo basicInformation;

            public CPlayerInfo basicInfo
            {
                get
                {
                    return basicInformation;
                }

                set
                {
                    this.basicInformation = value;
                }
            }

            public CPlayerSubset moveLocation
            {
                get
                {
                    return this.moveLocation;
                }

                set
                {
                    this.moveLocation = value;
                }
            }

            public string SoldierName
            {
                get
                {
                    string name = String.Empty;

                    if (this.basicInfo != null)
                    {
                        name = this.basicInfo.SoldierName;
                    }

                    return name;
                }
            }

            private CPlayerSubset targetLocation;
            public CPlayerSubset MoveLocation
            {
                get
                {
                    return this.targetLocation;
                }

                set
                {
                    this.targetLocation = value;
                }
            }

            private CPlayerSubset eventualTarget;
            public CPlayerSubset EndTarget
            {
                get
                {
                    return this.eventualTarget;
                }
                set
                {
                    this.eventualTarget = value;
                }
            }

            private int queuedForTeam;
            public int QueuedForTeam
            {
                get
                {
                    return this.queuedForTeam;
                }
                set
                {
                    this.queuedForTeam = value;
                }
            }

            private bool isAlive;
            public bool IsAlive
            {
                get
                {
                    return this.isAlive;
                }
                set
                {
                    this.isAlive = value;
                }
            }

            private int newTeam;
            public int NewTeam
            {
                get
                {
                    return this.newTeam;
                }
                set
                {
                    this.newTeam = value;
                }
            }

            public PlayerInfo(CPlayerInfo basicInformation)
            {
                this.basicInformation = basicInformation;
                this.queuedForTeam = 0;
                this.isAlive = false;
            }
        }

        #endregion
    }
}