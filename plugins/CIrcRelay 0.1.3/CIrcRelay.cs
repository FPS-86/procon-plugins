/* BF3 IRC Relay
 * Created by: B-man
 * Website: http://www.tchalo.com
 * IRC Server: irc.tchalo.net
 * */

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using System.Runtime.Serialization;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Collections;
using System.Globalization;
using System.IO;
//using System.Reflection;
using System.Net.Sockets;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Events;
using PRoCon.Core.Players;

using Meebey.SmartIrc4net;

namespace PRoConEvents
{
    public class CIrcRelay : PRoConPluginAPI, IPRoConPluginInterface
    {
        #region [Global Variables]
        public Thread ircThread;
        public IrcClient ircClient;

        private string ircServer;
        private int ircPort;
        private string ircChannel;
        private string ircNick;
        private string ircPassword;

        private bool autoReconnect;
        private bool autoRejoin;
        private bool autoRetry;
        private bool autoRejoinOnKick;

        private enumBoolYesNo relayChatToGame = enumBoolYesNo.Yes;
        private enumBoolYesNo relayJoinsQuits = enumBoolYesNo.Yes;
        private enumBoolYesNo relayPlayerDeaths = enumBoolYesNo.No;

        //Irc special characters
        public const char ircColor = '\x03';
        public const char ircBold = '\x02';
        public const char ircUnderline = '\x1F';
        public const char ircItalic = '\x16';
        public const char ircNormal = '\x0F';

        private int winningTeam;
        private CServerInfo currentServerInfo;
        private List<CPlayerInfo> currentPlayers;
        #endregion

        public CIrcRelay()
        {
            this.ircServer = "irc.tchalo.net";
            this.ircPort = 6667;
            this.ircChannel = "#bf3";
            this.ircNick = "BF3Bot";
            this.ircPassword = "";

            this.autoReconnect = true;
            this.autoRejoin = true;
            this.autoRetry = true;
            this.autoRejoinOnKick = true;
        }

        #region [Plugin Methods]
        public string GetPluginName()
        {
            return "IRC Relay BF3";
        }

        public string GetPluginVersion()
        {
            return "0.1.3";
        }

        public string GetPluginAuthor()
        {
            return "B-Man";
        }

        public string GetPluginWebsite()
        {
            return "tchalo.com";
        }

        public string GetPluginDescription()
        {
            return @"
                <h2>Description</h2>
                    <p>Relays information from BF3 to IRC and IRC to BF3</p>

                <h2>Commands</h2>
                    <blockquote><h4>!info</h4>List server information in IRC</blockquote>
                    <blockquote><h4>!players</h4>List the players connected to the server</blockquote>

                <h2>Settings</h2>
                    <h3>IRC settings</h3>
                        <blockquote><h4>IRC Server</h4>IRC Server Host Address</blockquote> 
                        <blockquote><h4>IRC Port</h4>IRC Server Port Number, Usually 6667</blockquote>
                        <blockquote><h4>IRC Password</h4>Optional: The password to connect to the IRC server</blockquote> 
                        <blockquote><h4>Channel</h4>The IRC channel the bot should join</blockquote>
                        <blockquote><h4>Nickname</h4>The name of the bot in IRC</blockquote> 
                        
                    <h3>Miscellaneous</h3>
                        <blockquote><h4>Relay IRC chat to game</h4>Relays all chat in the IRC channel to the in-game chat</blockquote>  
                ";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            this.RegisterEvents(this.GetType().Name, "OnPlayerJoin", "OnPlayerLeft", "OnGlobalChat", "OnRoundOverTeamScores", "OnRoundOver", "OnLevelLoaded", "OnServerInfo", "OnListPlayers");
        }

        public void OnPluginEnable()
        {
            this.ircClient = new IrcClient();

            //Seperate thread to keep GUI happy
            this.ircThread = new Thread(new ThreadStart(delegate
            {
                // Attach event handlers
                this.ircClient.OnConnected += new EventHandler(ircClient_OnConnected);
                this.ircClient.OnChannelMessage += new IrcEventHandler(ircClient_OnChannelMessage);

                this.ircClient.AutoReconnect = this.autoReconnect;
                this.ircClient.AutoRejoin = this.autoRejoin;
                this.ircClient.AutoRetry = this.autoRetry;
                this.ircClient.AutoNickHandling = true;
                this.ircClient.AutoRejoinOnKick = this.autoRejoinOnKick;
                this.ircClient.ActiveChannelSyncing = true;

                // Attempt to connect to the IRC server
                try { this.ircClient.Connect(this.ircServer, this.ircPort); }
                catch (Exception) { }
            }));
            try { this.ircThread.Start(); }
            catch (ThreadAbortException) { Thread.ResetAbort(); }
            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bIRC Relay BF3 ^2Enabled!");
        }

        public void OnPluginDisable()
        {
            if (ircClient.IsConnected)
            {
                ircClient.AutoReconnect = false;
                this.ircClient.RfcQuit("BF3 IRC Bot. By B-Man. (TCHalo)");
            }

            this.ExecuteCommand("procon.protected.pluginconsole.write", "^bIRC Relay BF3 ^1Disabled =(");
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();

            lstReturn.Add(new CPluginVariable("IRC Server", ircServer.GetType(), ircServer));
            lstReturn.Add(new CPluginVariable("IRC Port", ircPort.GetType(), ircPort));
            lstReturn.Add(new CPluginVariable("IRC Password", ircPassword.GetType(), ircPassword));
            lstReturn.Add(new CPluginVariable("Channel", ircChannel.GetType(), ircChannel));
            lstReturn.Add(new CPluginVariable("Nickname", ircNick.GetType(), ircNick));
            lstReturn.Add(new CPluginVariable("Relay IRC chat to game?", typeof(enumBoolYesNo), relayChatToGame));

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            return GetDisplayPluginVariables();
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            int intValue = -1;

            switch (strVariable)
            {
                case "IRC Server":
                    {
                        //Gotta fix this to properly change servers
                        ircServer = strValue;
                        reconnectIrcClient();
                        break;
                    }
                case "IRC Port":
                    {
                        if (Int32.TryParse(strValue, out intValue))
                            ircPort = intValue;
                        reconnectIrcClient();
                        break;
                    }
                case "IRC Password":
                    {
                        ircPassword = strValue;
                        reconnectIrcClient();
                        break;
                    }
                case "Channel":
                    {
                        if (ircClient.IsConnected && ircClient.JoinedChannels[0] != strValue)
                        {
                            ircClient.RfcPart(ircClient.JoinedChannels[0]);
                            ircClient.RfcJoin(strValue);
                        }
                        ircChannel = strValue;
                        break;
                    }
                case "Nickname":
                    {
                        if (ircClient.IsConnected && ircNick != strValue)
                        {
                            ircClient.RfcNick(strValue);
                        }
                        ircNick = strValue;
                        break;
                    }
                case "Relay IRC chat to game?":
                    {
                        relayChatToGame = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
                        break;
                    }
            }
        }
        #endregion

        #region [IRC Events]
        void ircClient_OnChannelMessage(object sender, IrcEventArgs e)
        {
            if (currentServerInfo != null && e.Data.MessageArray.Length > 0 && e.Data.MessageArray[0].ToLower() == "!info")
            {
                CMap map = GetMapByFilename(currentServerInfo.Map);
                SendIrcMessage("* [Server Name] " + ircColor + "5" + currentServerInfo.ServerName + ircNormal + " [Map]" + ircColor + "5 " + map.PublicLevelName + ircNormal + " [Game Mode] " + ircColor + "5" + GetFormattedGameMode(currentServerInfo.GameMode) + ircNormal + " [Round]" + ircColor + "5 " + (currentServerInfo.CurrentRound + 1) + "/" + currentServerInfo.TotalRounds + ircNormal + " [Players]" + ircColor + "5 " + currentServerInfo.PlayerCount + "/" + currentServerInfo.MaxPlayerCount);
            }
            else if (currentServerInfo != null && e.Data.MessageArray.Length > 0 && e.Data.MessageArray[0].ToLower() == "!players")
            {
                int[] teams = { 0, 1, 2, 3, 4 };
                string teamMsg = ircColor + "3** Team Colors: " + ircNormal;
                foreach (int i in teams)
                {
                    teamMsg += IrcColorizeStringByTeamId(i, GetTeamName(i)) + " ";
                }
                SendIrcMessage(teamMsg.Trim());

                if (currentPlayers != null)
                {
                    List<CPlayerInfo> sortedPlayers = currentPlayers;
                    sortedPlayers.Sort(new SortPlayersByTeam());
                    string message = "* (" + currentServerInfo.PlayerCount + "/" + currentServerInfo.MaxPlayerCount + ") Players online: ";
                    foreach (CPlayerInfo cpi in sortedPlayers)
                    {
                        message += IrcColorizeNameByTeam(cpi.SoldierName) + ", ";
                    }
                    message = message.Trim().Remove(message.Length - 2);
                    SendIrcMessage(message);
                }
            }
            else
            {
                //Send message to server
                if (e.Data.Nick != ircClient.Nickname && this.relayChatToGame == enumBoolYesNo.Yes)
                    this.ExecuteCommand("procon.protected.send", "admin.say", "[IRC]" + e.Data.Nick + ": " + e.Data.Message, "all");
            }
        }

        void ircClient_OnConnected(object sender, EventArgs e)
        {
            if (ircPassword.Length == 0)
                this.ircClient.Login(this.ircNick, this.ircNick, 0, this.ircNick);
            else
                this.ircClient.Login(this.ircNick, this.ircNick, 0, this.ircNick, ircPassword);

            this.ircClient.RfcJoin(this.ircChannel);
            this.ircClient.Listen();
            ircClient.AutoReconnect = this.autoReconnect;
        }
        #endregion

        #region [Method Overrides]
        public override void OnPlayerJoin(string soldierName)
        {
            SendIrcMessage(soldierName + " has joined the game");
        }
        public override void OnPlayerLeft(CPlayerInfo playerInfo)
        {
            SendIrcMessage(playerInfo.SoldierName + " has left the game");
        }
        public override void OnGlobalChat(string speaker, string message)
        {
            if (speaker != "Server")
            {
                string ircMessage = "[" + IrcColorizeNameByTeam(speaker) + "] " + message;
                SendIrcMessage(ircMessage);
            }
        }

        public override void OnRoundOverTeamScores(List<TeamScore> teamScores)
        {
            foreach (TeamScore ts in teamScores)
            {
                int score = ts.Score;
                if (score == 1)         //Losing team always has 1 ticket left... this fixes that.
                    score = 0;
                if (currentServerInfo.GameMode.ToLower().Contains("squad"))
                    SendIrcMessage(ircColor + "3Squad " + IrcColorizeStringByTeamId(ts.TeamID, GetTeamName(ts.TeamID)) + ": " + score + " tickets");
                else
                    SendIrcMessage(ircColor + "3Team " + IrcColorizeStringByTeamId(ts.TeamID, GetTeamName(ts.TeamID)) + ": " + score + " tickets left");
            }
        }
        public override void OnRoundOver(int winningTeamId)
        {
            TimeSpan rndTime = TimeSpan.FromSeconds(currentServerInfo.RoundTime);
            SendIrcMessage(ircColor + "3*** Round Over! Winning Team: " + IrcColorizeStringByTeamId(winningTeamId, GetTeamName(winningTeamId)) + ircColor + "3 (Round Time: " + rndTime.Minutes + ":" + rndTime.Seconds.ToString("00") + ")");
            this.winningTeam = winningTeamId;
        }
        public override void OnServerInfo(CServerInfo serverInfo)
        {
            currentServerInfo = serverInfo;             //Update the currentServerInfo to the latest server Info
        }
        public override void OnLevelLoaded(string mapFileName, string Gamemode, int roundsPlayed, int roundsTotal)
        {
            CMap map = GetMapByFilename(mapFileName);
            SendIrcMessage(ircColor + "3*** Round Started *** " + ircNormal + " [Map]" + ircColor + "5 " + map.PublicLevelName + ircNormal + " [Game Mode] " + ircColor + "5" + GetFormattedGameMode(Gamemode) + ircNormal + " [Round]" + ircColor + "5 " + roundsPlayed + "/" + roundsTotal + ircNormal + " [Players]" + ircColor + "5 " + currentServerInfo.PlayerCount + "/" + currentServerInfo.MaxPlayerCount);
        }
        public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset)
        {
            currentPlayers = players;
        }
        #endregion

        #region [Private Methods]
        private void SendIrcMessage(string ircMessage)
        {
            ircClient.SendMessage(SendType.Message, ircClient.JoinedChannels[0], ircMessage);
        }
        private CPlayerInfo FindPlayerInfo(string soldierName)
        {
            foreach (CPlayerInfo cpi in currentPlayers)
            {
                if (cpi.SoldierName.ToLower() == soldierName.ToLower())
                    return cpi;
            }
            return null;
        }
        private string GetTeamName(string soldierName)
        {
            CPlayerInfo player = FindPlayerInfo(soldierName);
            return GetTeamName(player);
        }
        private string GetTeamName(CPlayerInfo playerInfo)
        {
            return GetTeamName(playerInfo.TeamID);
        }
        private string GetTeamName(int teamID)
        {
            CMap map = GetMapByFilename(currentServerInfo.Map);
            if (currentServerInfo.GameMode.ToLower().Contains("squad"))
            {
                if (teamID == 0)
                    return "Neutral";
                if (teamID == 1)
                    return "Alpha";
                if (teamID == 2)
                    return "Bravo";
                if (teamID == 3)
                    return "Charlie";
                if (teamID == 4)
                    return "Delta";
            }
            else
            {
                foreach (CTeamName ctn in map.TeamNames)
                {
                    if (ctn.Playlist == map.PlayList && ctn.TeamID == teamID)
                        return this.GetLocalized(ctn.LocalizationKey, ctn.LocalizationKey);
                }
            }
            return "N/A";
        }
        private string IrcColorizeStringByTeamId(int teamID, string input)
        {
            int temp;
            if (int.TryParse(input[0].ToString(), out temp))        //Fix for users that start with a number..
            {
                input = " " + input;
            }
            if (teamID == 0)
                return ircColor + "11" + input + ircNormal;
            if (teamID == 1)
                return ircColor + "12" + input + ircNormal;
            if (teamID == 2)
                return ircColor + "4" + input + ircNormal;
            if (teamID == 3)
                return ircColor + "6" + input + ircNormal;
            if (teamID == 4)
                return ircColor + "7" + input + ircNormal;

            return input;
        }
        private string IrcColorizeNameByTeam(string soldierName)
        {
            CPlayerInfo player = FindPlayerInfo(soldierName);
            if (player == null)
            {
                return "";
            }
            return IrcColorizeStringByTeamId(player.TeamID, player.SoldierName);
        }
        private string GetFormattedGameMode(string gameMode)
        {
            if (gameMode == "SquadDeathMatch0")
                return "Squad Death Match";
            if (gameMode == "ConquestSmall0")
                return "Conquest Small";
            if (gameMode == "ConquestLarge0")
                return "Conquest Large";
            if (gameMode == "TeamDeathMatch0")
                return "Team Death Match";
            if (gameMode == "SquadRush0")
                return "Squad Rush";
            if (gameMode == "RushLarge0")
                return "Rush";
            return gameMode;
        }
        private void reconnectIrcClient()
        {
            this.OnPluginDisable();
            this.OnPluginEnable();
        }
        #endregion
    }

    #region [IComparer Classes]
    public class SortPlayersByTeam : IComparer<CPlayerInfo>
    {
        int IComparer<CPlayerInfo>.Compare(CPlayerInfo a, CPlayerInfo b) //implement Compare
        {
            if (a.TeamID < b.TeamID)
                return -1; //normally greater than = 1
            if (a.TeamID > b.TeamID)
                return 1; // normally smaller than = -1
            else
                return 0; // equal
        }
    }
    #endregion
}

/* Meeby IRC
 * http://www.meebey.net/projects/smartirc4net/
 */
#region [Meeby IRC Bot Classes (Yes, Its Ugly..)]

namespace Meebey.SmartIrc4net
{

    /// <summary>
    ///
    /// </summary>
    public enum Priority
    {
        Low,
        BelowMedium,
        Medium,
        AboveMedium,
        High,
        Critical
    }

    /// <summary>
    ///
    /// </summary>
    public enum SendType
    {
        Message,
        Action,
        Notice,
        CtcpReply,
        CtcpRequest
    }

    /// <summary>
    ///
    /// </summary>
    public enum ReceiveType
    {
        Info,
        Login,
        Motd,
        List,
        Join,
        Kick,
        Part,
        Invite,
        Quit,
        Who,
        WhoIs,
        WhoWas,
        Name,
        Topic,
        BanList,
        NickChange,
        TopicChange,
        UserMode,
        UserModeChange,
        ChannelMode,
        ChannelModeChange,
        ChannelMessage,
        ChannelAction,
        ChannelNotice,
        QueryMessage,
        QueryAction,
        QueryNotice,
        CtcpReply,
        CtcpRequest,
        Error,
        ErrorMessage,
        Unknown
    }

    /// <summary>
    ///
    /// </summary>
    public enum ReplyCode : int
    {
        Null = 000,
        Welcome = 001,
        YourHost = 002,
        Created = 003,
        MyInfo = 004,
        Bounce = 005,
        TraceLink = 200,
        TraceConnecting = 201,
        TraceHandshake = 202,
        TraceUnknown = 203,
        TraceOperator = 204,
        TraceUser = 205,
        TraceServer = 206,
        TraceService = 207,
        TraceNewType = 208,
        TraceClass = 209,
        TraceReconnect = 210,
        StatsLinkInfo = 211,
        StatsCommands = 212,
        EndOfStats = 219,
        UserModeIs = 221,
        ServiceList = 234,
        ServiceListEnd = 235,
        StatsUptime = 242,
        StatsOLine = 243,
        LuserClient = 251,
        LuserOp = 252,
        LuserUnknown = 253,
        LuserChannels = 254,
        LuserMe = 255,
        AdminMe = 256,
        AdminLocation1 = 257,
        AdminLocation2 = 258,
        AdminEmail = 259,
        TraceLog = 261,
        TraceEnd = 262,
        TryAgain = 263,
        Away = 301,
        UserHost = 302,
        IsOn = 303,
        UnAway = 305,
        NowAway = 306,
        WhoIsUser = 311,
        WhoIsServer = 312,
        WhoIsOperator = 313,
        WhoWasUser = 314,
        EndOfWho = 315,
        WhoIsIdle = 317,
        EndOfWhoIs = 318,
        WhoIsChannels = 319,
        ListStart = 321,
        List = 322,
        ListEnd = 323,
        ChannelModeIs = 324,
        UniqueOpIs = 325,
        NoTopic = 331,
        Topic = 332,
        Inviting = 341,
        Summoning = 342,
        InviteList = 346,
        EndOfInviteList = 347,
        ExceptionList = 348,
        EndOfExceptionList = 349,
        Version = 351,
        WhoReply = 352,
        NamesReply = 353,
        Links = 364,
        EndOfLinks = 365,
        EndOfNames = 366,
        BanList = 367,
        EndOfBanList = 368,
        EndOfWhoWas = 369,
        Info = 371,
        Motd = 372,
        EndOfInfo = 374,
        MotdStart = 375,
        EndOfMotd = 376,
        YouAreOper = 381,
        Rehashing = 382,
        YouAreService = 383,
        Time = 391,
        UsersStart = 392,
        Users = 393,
        EndOfUsers = 394,
        NoUsers = 395,
        ErrorNoSuchNickname = 401,
        ErrorNoSuchServer = 402,
        ErrorNoSuchChannel = 403,
        ErrorCannotSendToChannel = 404,
        ErrorTooManyChannels = 405,
        ErrorWasNoSuchNickname = 406,
        ErrorTooManyTargets = 407,
        ErrorNoSuchService = 408,
        ErrorNoOrigin = 409,
        ErrorNoRecipient = 411,
        ErrorNoTextToSend = 412,
        ErrorNoTopLevel = 413,
        ErrorWildTopLevel = 414,
        ErrorBadMask = 415,
        ErrorUnknownCommand = 421,
        ErrorNoMotd = 422,
        ErrorNoAdminInfo = 423,
        ErrorFileError = 424,
        ErrorNoNicknameGiven = 431,
        ErrorErroneusNickname = 432,
        ErrorNicknameInUse = 433,
        ErrorNicknameCollision = 436,
        ErrorUnavailableResource = 437,
        ErrorUserNotInChannel = 441,
        ErrorNotOnChannel = 442,
        ErrorUserOnChannel = 443,
        ErrorNoLogin = 444,
        ErrorSummonDisabled = 445,
        ErrorUsersDisabled = 446,
        ErrorNotRegistered = 451,
        ErrorNeedMoreParams = 461,
        ErrorAlreadyRegistered = 462,
        ErrorNoPermissionForHost = 463,
        ErrorPasswordMismatch = 464,
        ErrorYouAreBannedCreep = 465,
        ErrorYouWillBeBanned = 466,
        ErrorKeySet = 467,
        ErrorChannelIsFull = 471,
        ErrorUnknownMode = 472,
        ErrorInviteOnlyChannel = 473,
        ErrorBannedFromChannel = 474,
        ErrorBadChannelKey = 475,
        ErrorBadChannelMask = 476,
        ErrorNoChannelModes = 477,
        ErrorBanListFull = 478,
        ErrorNoPrivileges = 481,
        ErrorChannelOpPrivilegesNeeded = 482,
        ErrorCannotKillServer = 483,
        ErrorRestricted = 484,
        ErrorUniqueOpPrivilegesNeeded = 485,
        ErrorNoOperHost = 491,
        ErrorUserModeUnknownFlag = 501,
        ErrorUsersDoNotMatch = 502
    }
}
namespace Meebey.SmartIrc4net
{
    /// <summary>
    ///
    /// </summary>
    /// <threadsafety static="true" instance="true" />
    public class IrcEventArgs : EventArgs
    {
        private readonly IrcMessageData _Data;

        /// <summary>
        /// 
        /// </summary>
        public IrcMessageData Data
        {
            get
            {
                return _Data;
            }
        }

        internal IrcEventArgs(IrcMessageData data)
        {
            _Data = data;
        }
    }
}
namespace Meebey.SmartIrc4net
{
    /// <threadsafety static="true" instance="true" />
    [Serializable()]
    public class SmartIrc4netException : ApplicationException
    {
        public SmartIrc4netException()
            : base()
        {
        }

        public SmartIrc4netException(string message)
            : base(message)
        {
        }

        public SmartIrc4netException(string message, Exception e)
            : base(message, e)
        {
        }

        protected SmartIrc4netException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <threadsafety static="true" instance="true" />
    [Serializable()]
    public class ConnectionException : SmartIrc4netException
    {
        public ConnectionException()
            : base()
        {
        }

        public ConnectionException(string message)
            : base(message)
        {
        }

        public ConnectionException(string message, Exception e)
            : base(message, e)
        {
        }

        protected ConnectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <threadsafety static="true" instance="true" />
    [Serializable()]
    public class CouldNotConnectException : ConnectionException
    {
        public CouldNotConnectException()
            : base()
        {
        }

        public CouldNotConnectException(string message)
            : base(message)
        {
        }

        public CouldNotConnectException(string message, Exception e)
            : base(message, e)
        {
        }

        protected CouldNotConnectException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <threadsafety static="true" instance="true" />
    [Serializable()]
    public class NotConnectedException : ConnectionException
    {
        public NotConnectedException()
            : base()
        {
        }

        public NotConnectedException(string message)
            : base(message)
        {
        }

        public NotConnectedException(string message, Exception e)
            : base(message, e)
        {
        }

        protected NotConnectedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <threadsafety static="true" instance="true" />
    [Serializable()]
    public class AlreadyConnectedException : ConnectionException
    {
        public AlreadyConnectedException()
            : base()
        {
        }

        public AlreadyConnectedException(string message)
            : base(message)
        {
        }

        public AlreadyConnectedException(string message, Exception e)
            : base(message, e)
        {
        }

        protected AlreadyConnectedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// This layer is an event driven high-level API with all features you could need for IRC programming.
    /// </summary>
    /// <threadsafety static="true" instance="true" />
    public class IrcClient : IrcCommands
    {
        private string _Nickname = string.Empty;
        private string[] _NicknameList;
        private int _CurrentNickname;
        private string _Realname = string.Empty;
        private string _Usermode = string.Empty;
        private int _IUsermode;
        private string _Username = string.Empty;
        private string _Password = string.Empty;
        private bool _IsAway;
        private string _CtcpVersion;
        private bool _ActiveChannelSyncing;
        private bool _PassiveChannelSyncing;
        private bool _AutoJoinOnInvite;
        private bool _AutoRejoin;
        private StringDictionary _AutoRejoinChannels = new StringDictionary();
        private bool _AutoRejoinChannelsWithKeys;
        private bool _AutoRejoinOnKick;
        private bool _AutoRelogin;
        private bool _AutoNickHandling = true;
        private bool _SupportNonRfc;
        private bool _SupportNonRfcLocked;
        private StringCollection _Motd = new StringCollection();
        private bool _MotdReceived;
        private Array _ReplyCodes = Enum.GetValues(typeof(ReplyCode));
        private StringCollection _JoinedChannels = new StringCollection();
        private Hashtable _Channels = Hashtable.Synchronized(new Hashtable(new CaseInsensitiveHashCodeProvider(), new CaseInsensitiveComparer()));
        private Hashtable _IrcUsers = Hashtable.Synchronized(new Hashtable(new CaseInsensitiveHashCodeProvider(), new CaseInsensitiveComparer()));
        private List<ChannelInfo> _ChannelList;
        private Object _ChannelListSyncRoot = new Object();
        private AutoResetEvent _ChannelListReceivedEvent;
        private List<WhoInfo> _WhoList;
        private Object _WhoListSyncRoot = new Object();
        private AutoResetEvent _WhoListReceivedEvent;
        private List<BanInfo> _BanList;
        private Object _BanListSyncRoot = new Object();
        private AutoResetEvent _BanListReceivedEvent;
        private static Regex _ReplyCodeRegex = new Regex("^:[^ ]+? ([0-9]{3}) .+$", RegexOptions.Compiled);
        private static Regex _PingRegex = new Regex("^PING :.*", RegexOptions.Compiled);
        private static Regex _ErrorRegex = new Regex("^ERROR :.*", RegexOptions.Compiled);
        private static Regex _ActionRegex = new Regex("^:.*? PRIVMSG (.).* :" + "\x1" + "ACTION .*" + "\x1" + "$", RegexOptions.Compiled);
        private static Regex _CtcpRequestRegex = new Regex("^:.*? PRIVMSG .* :" + "\x1" + ".*" + "\x1" + "$", RegexOptions.Compiled);
        private static Regex _MessageRegex = new Regex("^:.*? PRIVMSG (.).* :.*$", RegexOptions.Compiled);
        private static Regex _CtcpReplyRegex = new Regex("^:.*? NOTICE .* :" + "\x1" + ".*" + "\x1" + "$", RegexOptions.Compiled);
        private static Regex _NoticeRegex = new Regex("^:.*? NOTICE (.).* :.*$", RegexOptions.Compiled);
        private static Regex _InviteRegex = new Regex("^:.*? INVITE .* .*$", RegexOptions.Compiled);
        private static Regex _JoinRegex = new Regex("^:.*? JOIN .*$", RegexOptions.Compiled);
        private static Regex _TopicRegex = new Regex("^:.*? TOPIC .* :.*$", RegexOptions.Compiled);
        private static Regex _NickRegex = new Regex("^:.*? NICK .*$", RegexOptions.Compiled);
        private static Regex _KickRegex = new Regex("^:.*? KICK .* .*$", RegexOptions.Compiled);
        private static Regex _PartRegex = new Regex("^:.*? PART .*$", RegexOptions.Compiled);
        private static Regex _ModeRegex = new Regex("^:.*? MODE (.*) .*$", RegexOptions.Compiled);
        private static Regex _QuitRegex = new Regex("^:.*? QUIT :.*$", RegexOptions.Compiled);

        public event EventHandler OnRegistered;
        public event PingEventHandler OnPing;
        public event PongEventHandler OnPong;
        public event IrcEventHandler OnRawMessage;
        public event ErrorEventHandler OnError;
        public event IrcEventHandler OnErrorMessage;
        public event JoinEventHandler OnJoin;
        public event NamesEventHandler OnNames;
        public event ListEventHandler OnList;
        public event PartEventHandler OnPart;
        public event QuitEventHandler OnQuit;
        public event KickEventHandler OnKick;
        public event AwayEventHandler OnAway;
        public event IrcEventHandler OnUnAway;
        public event IrcEventHandler OnNowAway;
        public event InviteEventHandler OnInvite;
        public event BanEventHandler OnBan;
        public event UnbanEventHandler OnUnban;
        public event OpEventHandler OnOp;
        public event DeopEventHandler OnDeop;
        public event HalfopEventHandler OnHalfop;
        public event DehalfopEventHandler OnDehalfop;
        public event VoiceEventHandler OnVoice;
        public event DevoiceEventHandler OnDevoice;
        public event WhoEventHandler OnWho;
        public event MotdEventHandler OnMotd;
        public event TopicEventHandler OnTopic;
        public event TopicChangeEventHandler OnTopicChange;
        public event NickChangeEventHandler OnNickChange;
        public event IrcEventHandler OnModeChange;
        public event IrcEventHandler OnUserModeChange;
        public event IrcEventHandler OnChannelModeChange;
        public event IrcEventHandler OnChannelMessage;
        public event ActionEventHandler OnChannelAction;
        public event IrcEventHandler OnChannelNotice;
        public event IrcEventHandler OnChannelActiveSynced;
        public event IrcEventHandler OnChannelPassiveSynced;
        public event IrcEventHandler OnQueryMessage;
        public event ActionEventHandler OnQueryAction;
        public event IrcEventHandler OnQueryNotice;
        public event CtcpEventHandler OnCtcpRequest;
        public event CtcpEventHandler OnCtcpReply;

        /// <summary>
        /// Enables/disables the active channel sync feature.
        /// Default: false
        /// </summary>
        public bool ActiveChannelSyncing
        {
            get
            {
                return _ActiveChannelSyncing;
            }
            set
            {
#if LOG4NET
                if (value) {
                    Logger.ChannelSyncing.Info("Active channel syncing enabled");
                } else {
                    Logger.ChannelSyncing.Info("Active channel syncing disabled");
                }
#endif
                _ActiveChannelSyncing = value;
            }
        }

        /// <summary>
        /// Enables/disables the passive channel sync feature. Not implemented yet!
        /// </summary>
        public bool PassiveChannelSyncing
        {
            get
            {
                return _PassiveChannelSyncing;
            }
            /*
            set {
#if LOG4NET
                if (value) {
                    Logger.ChannelSyncing.Info("Passive channel syncing enabled");
                } else {
                    Logger.ChannelSyncing.Info("Passive channel syncing disabled");
                }
#endif
                _PassiveChannelSyncing = value;
            }
            */
        }

        /// <summary>
        /// Sets the ctcp version that should be replied on ctcp version request.
        /// </summary>
        public string CtcpVersion
        {
            get
            {
                return _CtcpVersion;
            }
            set
            {
                _CtcpVersion = value;
            }
        }

        /// <summary>
        /// Enables/disables auto joining of channels when invited.
        /// Default: false
        /// </summary>
        public bool AutoJoinOnInvite
        {
            get
            {
                return _AutoJoinOnInvite;
            }
            set
            {
#if LOG4NET
                if (value) {
                    Logger.ChannelSyncing.Info("AutoJoinOnInvite enabled");
                } else {
                    Logger.ChannelSyncing.Info("AutoJoinOnInvite disabled");
                }
#endif
                _AutoJoinOnInvite = value;
            }
        }

        /// <summary>
        /// Enables/disables automatic rejoining of channels when a connection to the server is lost.
        /// Default: false
        /// </summary>
        public bool AutoRejoin
        {
            get
            {
                return _AutoRejoin;
            }
            set
            {
#if LOG4NET
                if (value) {
                    Logger.ChannelSyncing.Info("AutoRejoin enabled");
                } else {
                    Logger.ChannelSyncing.Info("AutoRejoin disabled");
                }
#endif
                _AutoRejoin = value;
            }
        }

        /// <summary>
        /// Enables/disables auto rejoining of channels when kicked.
        /// Default: false
        /// </summary>
        public bool AutoRejoinOnKick
        {
            get
            {
                return _AutoRejoinOnKick;
            }
            set
            {
#if LOG4NET
                if (value) {
                    Logger.ChannelSyncing.Info("AutoRejoinOnKick enabled");
                } else {
                    Logger.ChannelSyncing.Info("AutoRejoinOnKick disabled");
                }
#endif
                _AutoRejoinOnKick = value;
            }
        }

        /// <summary>
        /// Enables/disables auto relogin to the server after a reconnect.
        /// Default: false
        /// </summary>
        public bool AutoRelogin
        {
            get
            {
                return _AutoRelogin;
            }
            set
            {
#if LOG4NET
                if (value) {
                    Logger.ChannelSyncing.Info("AutoRelogin enabled");
                } else {
                    Logger.ChannelSyncing.Info("AutoRelogin disabled");
                }
#endif
                _AutoRelogin = value;
            }
        }

        /// <summary>
        /// Enables/disables auto nick handling on nick collisions
        /// Default: true
        /// </summary>
        public bool AutoNickHandling
        {
            get
            {
                return _AutoNickHandling;
            }
            set
            {
#if LOG4NET
                if (value) {
                    Logger.ChannelSyncing.Info("AutoNickHandling enabled");
                } else {
                    Logger.ChannelSyncing.Info("AutoNickHandling disabled");
                }
#endif
                _AutoNickHandling = value;
            }
        }

        /// <summary>
        /// Enables/disables support for non rfc features.
        /// Default: false
        /// </summary>
        public bool SupportNonRfc
        {
            get
            {
                return _SupportNonRfc;
            }
            set
            {
                if (_SupportNonRfcLocked)
                {
                    return;
                }
#if LOG4NET
                
                if (value) {
                    Logger.ChannelSyncing.Info("SupportNonRfc enabled");
                } else {
                    Logger.ChannelSyncing.Info("SupportNonRfc disabled");
                }
#endif
                _SupportNonRfc = value;
            }
        }

        /// <summary>
        /// Gets the nickname of us.
        /// </summary>
        public string Nickname
        {
            get
            {
                return _Nickname;
            }
        }

        /// <summary>
        /// Gets the list of nicknames of us.
        /// </summary>
        public string[] NicknameList
        {
            get
            {
                return _NicknameList;
            }
        }

        /// <summary>
        /// Gets the supposed real name of us.
        /// </summary>
        public string Realname
        {
            get
            {
                return _Realname;
            }
        }

        /// <summary>
        /// Gets the username for the server.
        /// </summary>
        /// <remarks>
        /// System username is set by default 
        /// </remarks>
        public string Username
        {
            get
            {
                return _Username;
            }
        }

        /// <summary>
        /// Gets the alphanumeric mode mask of us.
        /// </summary>
        public string Usermode
        {
            get
            {
                return _Usermode;
            }
        }

        /// <summary>
        /// Gets the numeric mode mask of us.
        /// </summary>
        public int IUsermode
        {
            get
            {
                return _IUsermode;
            }
        }

        /// <summary>
        /// Returns if we are away on this connection
        /// </summary>
        public bool IsAway
        {
            get
            {
                return _IsAway;
            }
        }

        /// <summary>
        /// Gets the password for the server.
        /// </summary>
        public string Password
        {
            get
            {
                return _Password;
            }
        }

        /// <summary>
        /// Gets the list of channels we are joined.
        /// </summary>
        public StringCollection JoinedChannels
        {
            get
            {
                return _JoinedChannels;
            }
        }

        /// <summary>
        /// Gets the server message of the day.
        /// </summary>
        public StringCollection Motd
        {
            get
            {
                return _Motd;
            }
        }

        public object BanListSyncRoot
        {
            get
            {
                return _BanListSyncRoot;
            }
        }

        /// <summary>
        /// This class manages the connection server and provides access to all the objects needed to send and receive messages.
        /// </summary>
        public IrcClient()
        {
#if LOG4NET
            Logger.Main.Debug("IrcClient created");
#endif
            OnReadLine += new ReadLineEventHandler(_Worker);
            OnDisconnected += new EventHandler(_OnDisconnected);
            OnConnectionError += new EventHandler(_OnConnectionError);
        }

#if LOG4NET
        ~IrcClient()
        {
            Logger.Main.Debug("IrcClient destroyed");
        }
#endif

        /// <summary>
        /// Connection parameters required to establish an server connection.
        /// </summary>
        /// <param name="addresslist">The list of server hostnames.</param>
        /// <param name="port">The TCP port the server listens on.</param>
        public new void Connect(string[] addresslist, int port)
        {
            _SupportNonRfcLocked = true;
            base.Connect(addresslist, port);
        }

        /// <overloads>
        /// Reconnects to the current server.
        /// </overloads>
        /// <param name="login">If the login data should be sent, after successful connect.</param>
        /// <param name="channels">If the channels should be rejoined, after successful connect.</param>
        public void Reconnect(bool login, bool channels)
        {
            if (channels)
            {
                _StoreChannelsToRejoin();
            }
            base.Reconnect();
            if (login)
            {
                //reset the nick to the original nicklist
                _CurrentNickname = 0;
                Login(_NicknameList, Realname, IUsermode, Username, Password);
            }
            if (channels)
            {
                _RejoinChannels();
            }
        }

        /// <param name="login">If the login data should be sent, after successful connect.</param>
        public void Reconnect(bool login)
        {
            Reconnect(login, true);
        }

        /// <summary>
        /// Login parameters required identify with server connection
        /// </summary>
        /// <remark>Login is used at the beginning of connection to specify the username, hostname and realname of a new user.</remark>
        /// <param name="nicklist">The users list of 'nick' names which may NOT contain spaces</param>
        /// <param name="realname">The users 'real' name which may contain space characters</param>
        /// <param name="usermode">A numeric mode parameter.  
        ///   <remark>
        ///     Set to 0 to recieve wallops and be invisible. 
        ///     Set to 4 to be invisible and not receive wallops.
        ///   </remark>
        /// </param>
        /// <param name="username">The user's machine logon name</param>        
        /// <param name="password">The optional password can and MUST be set before any attempt to register
        ///  the connection is made.</param>        
        public void Login(string[] nicklist, string realname, int usermode, string username, string password)
        {
#if LOG4NET
            Logger.Connection.Info("logging in");
#endif
            _NicknameList = (string[])nicklist.Clone();
            // here we set the nickname which we will try first
            _Nickname = _NicknameList[0].Replace(" ", "");
            _Realname = realname;
            _IUsermode = usermode;

            if (username != null && username.Length > 0)
            {
                _Username = username.Replace(" ", "");
            }
            else
            {
                _Username = Environment.UserName.Replace(" ", "");
            }

            if (password != null && password.Length > 0)
            {
                _Password = password;
                RfcPass(Password, Priority.Critical);
            }

            RfcNick(Nickname, Priority.Critical);
            RfcUser(Username, IUsermode, Realname, Priority.Critical);
        }

        /// <summary>
        /// Login parameters required identify with server connection
        /// </summary>
        /// <remark>Login is used at the beginning of connection to specify the username, hostname and realname of a new user.</remark>
        /// <param name="nicklist">The users list of 'nick' names which may NOT contain spaces</param>
        /// <param name="realname">The users 'real' name which may contain space characters</param>
        /// <param name="usermode">A numeric mode parameter.  
        /// Set to 0 to recieve wallops and be invisible. 
        /// Set to 4 to be invisible and not receive wallops.</param>        
        /// <param name="username">The user's machine logon name</param>        
        public void Login(string[] nicklist, string realname, int usermode, string username)
        {
            Login(nicklist, realname, usermode, username, "");
        }

        /// <summary>
        /// Login parameters required identify with server connection
        /// </summary>
        /// <remark>Login is used at the beginning of connection to specify the username, hostname and realname of a new user.</remark>
        /// <param name="nicklist">The users list of 'nick' names which may NOT contain spaces</param>
        /// <param name="realname">The users 'real' name which may contain space characters</param>
        /// <param name="usermode">A numeric mode parameter.  
        /// Set to 0 to recieve wallops and be invisible. 
        /// Set to 4 to be invisible and not receive wallops.</param>        
        public void Login(string[] nicklist, string realname, int usermode)
        {
            Login(nicklist, realname, usermode, "", "");
        }

        /// <summary>
        /// Login parameters required identify with server connection
        /// </summary>
        /// <remark>Login is used at the beginning of connection to specify the username, hostname and realname of a new user.</remark>
        /// <param name="nicklist">The users list of 'nick' names which may NOT contain spaces</param>
        /// <param name="realname">The users 'real' name which may contain space characters</param> 
        public void Login(string[] nicklist, string realname)
        {
            Login(nicklist, realname, 0, "", "");
        }

        /// <summary>
        /// Login parameters required identify with server connection
        /// </summary>
        /// <remark>Login is used at the beginning of connection to specify the username, hostname and realname of a new user.</remark>
        /// <param name="nick">The users 'nick' name which may NOT contain spaces</param>
        /// <param name="realname">The users 'real' name which may contain space characters</param>
        /// <param name="usermode">A numeric mode parameter.  
        /// Set to 0 to recieve wallops and be invisible. 
        /// Set to 4 to be invisible and not receive wallops.</param>        
        /// <param name="username">The user's machine logon name</param>        
        /// <param name="password">The optional password can and MUST be set before any attempt to register
        ///  the connection is made.</param>   
        public void Login(string nick, string realname, int usermode, string username, string password)
        {
            Login(new string[] { nick, nick + "_", nick + "__" }, realname, usermode, username, password);
        }

        /// <summary>
        /// Login parameters required identify with server connection
        /// </summary>
        /// <remark>Login is used at the beginning of connection to specify the username, hostname and realname of a new user.</remark>
        /// <param name="nick">The users 'nick' name which may NOT contain spaces</param>
        /// <param name="realname">The users 'real' name which may contain space characters</param>
        /// <param name="usermode">A numeric mode parameter.  
        /// Set to 0 to recieve wallops and be invisible. 
        /// Set to 4 to be invisible and not receive wallops.</param>        
        /// <param name="username">The user's machine logon name</param>        
        public void Login(string nick, string realname, int usermode, string username)
        {
            Login(new string[] { nick, nick + "_", nick + "__" }, realname, usermode, username, "");
        }

        /// <summary>
        /// Login parameters required identify with server connection
        /// </summary>
        /// <remark>Login is used at the beginning of connection to specify the username, hostname and realname of a new user.</remark>
        /// <param name="nick">The users 'nick' name which may NOT contain spaces</param>
        /// <param name="realname">The users 'real' name which may contain space characters</param>
        /// <param name="usermode">A numeric mode parameter.  
        /// Set to 0 to recieve wallops and be invisible. 
        /// Set to 4 to be invisible and not receive wallops.</param>        
        public void Login(string nick, string realname, int usermode)
        {
            Login(new string[] { nick, nick + "_", nick + "__" }, realname, usermode, "", "");
        }

        /// <summary>
        /// Login parameters required identify with server connection
        /// </summary>
        /// <remark>Login is used at the beginning of connection to specify the username, hostname and realname of a new user.</remark>
        /// <param name="nick">The users 'nick' name which may NOT contain spaces</param>
        /// <param name="realname">The users 'real' name which may contain space characters</param>
        public void Login(string nick, string realname)
        {
            Login(new string[] { nick, nick + "_", nick + "__" }, realname, 0, "", "");
        }

        /// <summary>
        /// Determine if a specifier nickname is you
        /// </summary>
        /// <param name="nickname">The users 'nick' name which may NOT contain spaces</param>
        /// <returns>True if nickname belongs to you</returns>
        public bool IsMe(string nickname)
        {
            return (Nickname == nickname);
        }

        /// <summary>
        /// Determines if your nickname can be found in a specified channel
        /// </summary>
        /// <param name="channelname">The name of the channel you wish to query</param>
        /// <returns>True if you are found in channel</returns>
        public bool IsJoined(string channelname)
        {
            return IsJoined(channelname, Nickname);
        }

        /// <summary>
        /// Determine if a specified nickname can be found in a specified channel
        /// </summary>
        /// <param name="channelname">The name of the channel you wish to query</param>
        /// <param name="nickname">The users 'nick' name which may NOT contain spaces</param>
        /// <returns>True if nickname is found in channel</returns>
        public bool IsJoined(string channelname, string nickname)
        {
            if (channelname == null)
            {
                throw new System.ArgumentNullException("channelname");
            }

            if (nickname == null)
            {
                throw new System.ArgumentNullException("nickname");
            }

            Channel channel = GetChannel(channelname);
            if (channel != null &&
                channel.UnsafeUsers != null &&
                channel.UnsafeUsers.ContainsKey(nickname))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns user information
        /// </summary>
        /// <param name="nickname">The users 'nick' name which may NOT contain spaces</param>
        /// <returns>IrcUser object of requested nickname</returns>
        public IrcUser GetIrcUser(string nickname)
        {
            if (nickname == null)
            {
                throw new System.ArgumentNullException("nickname");
            }

            return (IrcUser)_IrcUsers[nickname];
        }

        /// <summary>
        /// Returns extended user information including channel information
        /// </summary>
        /// <param name="channelname">The name of the channel you wish to query</param>
        /// <param name="nickname">The users 'nick' name which may NOT contain spaces</param>
        /// <returns>ChannelUser object of requested channelname/nickname</returns>
        public ChannelUser GetChannelUser(string channelname, string nickname)
        {
            if (channelname == null)
            {
                throw new System.ArgumentNullException("channel");
            }

            if (nickname == null)
            {
                throw new System.ArgumentNullException("nickname");
            }

            Channel channel = GetChannel(channelname);
            if (channel != null)
            {
                return (ChannelUser)channel.UnsafeUsers[nickname];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channelname">The name of the channel you wish to query</param>
        /// <returns>Channel object of requested channel</returns>
        public Channel GetChannel(string channelname)
        {
            if (channelname == null)
            {
                throw new System.ArgumentNullException("channelname");
            }

            return (Channel)_Channels[channelname];
        }

        /// <summary>
        /// Gets a list of all joined channels on server
        /// </summary>
        /// <returns>String array of all joined channel names</returns>
        public string[] GetChannels()
        {
            string[] channels = new string[_Channels.Values.Count];
            int i = 0;
            foreach (Channel channel in _Channels.Values)
            {
                channels[i++] = channel.Name;
            }

            return channels;
        }

        /// <summary>
        /// Fetches a fresh list of all available channels that match the passed mask
        /// </summary>
        /// <returns>List of ListInfo</returns>
        public IList<ChannelInfo> GetChannelList(string mask)
        {
            List<ChannelInfo> list = new List<ChannelInfo>();
            lock (_ChannelListSyncRoot)
            {
                _ChannelList = list;
                _ChannelListReceivedEvent = new AutoResetEvent(false);

                // request list
                RfcList(mask);
                // wait till we have the complete list
                _ChannelListReceivedEvent.WaitOne();

                _ChannelListReceivedEvent = null;
                _ChannelList = null;
            }

            return list;
        }

        /// <summary>
        /// Fetches a fresh list of users that matches the passed mask
        /// </summary>
        /// <returns>List of ListInfo</returns>
        public IList<WhoInfo> GetWhoList(string mask)
        {
            List<WhoInfo> list = new List<WhoInfo>();
            lock (_WhoListSyncRoot)
            {
                _WhoList = list;
                _WhoListReceivedEvent = new AutoResetEvent(false);

                // request list
                RfcWho(mask);
                // wait till we have the complete list
                _WhoListReceivedEvent.WaitOne();

                _WhoListReceivedEvent = null;
                _WhoList = null;
            }

            return list;
        }

        /// <summary>
        /// Fetches a fresh ban list of the specified channel
        /// </summary>
        /// <returns>List of ListInfo</returns>
        public IList<BanInfo> GetBanList(string channel)
        {
            List<BanInfo> list = new List<BanInfo>();
            lock (_BanListSyncRoot)
            {
                _BanList = list;
                _BanListReceivedEvent = new AutoResetEvent(false);

                // request list
                Ban(channel);
                // wait till we have the complete list
                _BanListReceivedEvent.WaitOne();

                _BanListReceivedEvent = null;
                _BanList = null;
            }

            return list;
        }

        public IrcMessageData MessageParser(string rawline)
        {
            string line;
            string[] linear;
            string messagecode;
            string from;
            string nick = null;
            string ident = null;
            string host = null;
            string channel = null;
            string message = null;
            ReceiveType type;
            ReplyCode replycode;
            int exclamationpos;
            int atpos;
            int colonpos;

            if (rawline[0] == ':')
            {
                line = rawline.Substring(1);
            }
            else
            {
                line = rawline;
            }

            linear = line.Split(new char[] { ' ' });

            // conform to RFC 2812
            from = linear[0];
            messagecode = linear[1];
            exclamationpos = from.IndexOf("!");
            atpos = from.IndexOf("@");
            colonpos = line.IndexOf(" :");
            if (colonpos != -1)
            {
                // we want the exact position of ":" not beginning from the space
                colonpos += 1;
            }
            if (exclamationpos != -1)
            {
                nick = from.Substring(0, exclamationpos);
            }
            if ((atpos != -1) &&
                (exclamationpos != -1))
            {
                ident = from.Substring(exclamationpos + 1, (atpos - exclamationpos) - 1);
            }
            if (atpos != -1)
            {
                host = from.Substring(atpos + 1);
            }

            try
            {
                replycode = (ReplyCode)int.Parse(messagecode);
            }
            catch (FormatException)
            {
                replycode = ReplyCode.Null;
            }
            type = _GetMessageType(rawline);
            if (colonpos != -1)
            {
                message = line.Substring(colonpos + 1);
            }

            switch (type)
            {
                case ReceiveType.Join:
                case ReceiveType.Kick:
                case ReceiveType.Part:
                case ReceiveType.TopicChange:
                case ReceiveType.ChannelModeChange:
                case ReceiveType.ChannelMessage:
                case ReceiveType.ChannelAction:
                case ReceiveType.ChannelNotice:
                    channel = linear[2];
                    break;
                case ReceiveType.Who:
                case ReceiveType.Topic:
                case ReceiveType.Invite:
                case ReceiveType.BanList:
                case ReceiveType.ChannelMode:
                    channel = linear[3];
                    break;
                case ReceiveType.Name:
                    channel = linear[4];
                    break;
            }

            switch (replycode)
            {
                case ReplyCode.List:
                case ReplyCode.ListEnd:
                case ReplyCode.ErrorNoChannelModes:
                    channel = linear[3];
                    break;
            }

            if ((channel != null) &&
                (channel[0] == ':'))
            {
                channel = channel.Substring(1);
            }

            IrcMessageData data;
            data = new IrcMessageData(this, from, nick, ident, host, channel, message, rawline, type, replycode);
#if LOG4NET
            Logger.MessageParser.Debug("IrcMessageData "+
                                       "nick: '"+data.Nick+"' "+
                                       "ident: '"+data.Ident+"' "+
                                       "host: '"+data.Host+"' "+
                                       "type: '"+data.Type.ToString()+"' "+
                                       "from: '"+data.From+"' "+
                                       "channel: '"+data.Channel+"' "+
                                       "message: '"+data.Message+"' "
                                       );
#endif
            return data;
        }

        protected virtual IrcUser CreateIrcUser(string nickname)
        {
            return new IrcUser(nickname, this);
        }

        protected virtual Channel CreateChannel(string name)
        {
            if (_SupportNonRfc)
            {
                return new NonRfcChannel(name);
            }
            else
            {
                return new Channel(name);
            }
        }

        protected virtual ChannelUser CreateChannelUser(string channel, IrcUser ircUser)
        {
            if (_SupportNonRfc)
            {
                return new NonRfcChannelUser(channel, ircUser);
            }
            else
            {
                return new ChannelUser(channel, ircUser);
            }
        }

        private void _Worker(object sender, ReadLineEventArgs e)
        {
            // lets see if we have events or internal messagehandler for it
            _HandleEvents(MessageParser(e.Line));
        }

        private void _OnDisconnected(object sender, EventArgs e)
        {
            if (AutoRejoin)
            {
                _StoreChannelsToRejoin();
            }
            _SyncingCleanup();
        }

        private void _OnConnectionError(object sender, EventArgs e)
        {
            try
            {
                // AutoReconnect is handled in IrcConnection._OnConnectionError
                if (AutoReconnect && AutoRelogin)
                {
                    Login(_NicknameList, Realname, IUsermode, Username, Password);
                }
                if (AutoReconnect && AutoRejoin)
                {
                    _RejoinChannels();
                }
            }
            catch (NotConnectedException)
            {
                // HACK: this is hacky, we don't know if the Reconnect was actually successful
                // means sending IRC commands without a connection throws NotConnectedExceptions 
            }
        }

        private void _StoreChannelsToRejoin()
        {
#if LOG4NET
            Logger.Connection.Info("Storing channels for rejoin...");
#endif
            _AutoRejoinChannels.Clear();
            if (ActiveChannelSyncing || PassiveChannelSyncing)
            {
                // store the key using channel sync
                foreach (Channel channel in _Channels.Values)
                {
                    if (channel.Key.Length > 0)
                    {
                        _AutoRejoinChannels.Add(channel.Name, channel.Key);
                        _AutoRejoinChannelsWithKeys = true;
                    }
                    else
                    {
                        _AutoRejoinChannels.Add(channel.Name, "nokey");
                    }
                }
            }
            else
            {
                foreach (string channel in _JoinedChannels)
                {
                    _AutoRejoinChannels.Add(channel, "nokey");
                }
            }
        }

        private void _RejoinChannels()
        {
#if LOG4NET
            Logger.Connection.Info("Rejoining channels...");
#endif
            int chan_count = _AutoRejoinChannels.Count;

            string[] names = new string[chan_count];
            _AutoRejoinChannels.Keys.CopyTo(names, 0);

            if (_AutoRejoinChannelsWithKeys)
            {
                string[] keys = new string[chan_count];
                _AutoRejoinChannels.Values.CopyTo(keys, 0);

                RfcJoin(names, keys, Priority.High);
            }
            else
            {
                RfcJoin(names, Priority.High);
            }

            _AutoRejoinChannelsWithKeys = false;
            _AutoRejoinChannels.Clear();
        }

        private void _SyncingCleanup()
        {
            // lets clean it baby, powered by Mr. Proper
#if LOG4NET
            Logger.ChannelSyncing.Debug("Mr. Proper action, cleaning good...");
#endif
            _JoinedChannels.Clear();
            if (ActiveChannelSyncing)
            {
                _Channels.Clear();
                _IrcUsers.Clear();
            }

            _IsAway = false;

            _MotdReceived = false;
            _Motd.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        private string _NextNickname()
        {
            _CurrentNickname++;
            //if we reach the end stay there
            if (_CurrentNickname >= _NicknameList.Length)
            {
                _CurrentNickname--;
            }
            return NicknameList[_CurrentNickname];
        }

        private ReceiveType _GetMessageType(string rawline)
        {
            Match found = _ReplyCodeRegex.Match(rawline);
            if (found.Success)
            {
                string code = found.Groups[1].Value;
                ReplyCode replycode = (ReplyCode)int.Parse(code);

                // check if this replycode is known in the RFC
                if (Array.IndexOf(_ReplyCodes, replycode) == -1)
                {
#if LOG4NET
                    Logger.MessageTypes.Warn("This IRC server ("+Address+") doesn't conform to the RFC 2812! ignoring unrecognized replycode '"+replycode+"'");
#endif
                    return ReceiveType.Unknown;
                }

                switch (replycode)
                {
                    case ReplyCode.Welcome:
                    case ReplyCode.YourHost:
                    case ReplyCode.Created:
                    case ReplyCode.MyInfo:
                    case ReplyCode.Bounce:
                        return ReceiveType.Login;
                    case ReplyCode.LuserClient:
                    case ReplyCode.LuserOp:
                    case ReplyCode.LuserUnknown:
                    case ReplyCode.LuserMe:
                    case ReplyCode.LuserChannels:
                        return ReceiveType.Info;
                    case ReplyCode.MotdStart:
                    case ReplyCode.Motd:
                    case ReplyCode.EndOfMotd:
                        return ReceiveType.Motd;
                    case ReplyCode.NamesReply:
                    case ReplyCode.EndOfNames:
                        return ReceiveType.Name;
                    case ReplyCode.WhoReply:
                    case ReplyCode.EndOfWho:
                        return ReceiveType.Who;
                    case ReplyCode.ListStart:
                    case ReplyCode.List:
                    case ReplyCode.ListEnd:
                        return ReceiveType.List;
                    case ReplyCode.BanList:
                    case ReplyCode.EndOfBanList:
                        return ReceiveType.BanList;
                    case ReplyCode.Topic:
                    case ReplyCode.NoTopic:
                        return ReceiveType.Topic;
                    case ReplyCode.WhoIsUser:
                    case ReplyCode.WhoIsServer:
                    case ReplyCode.WhoIsOperator:
                    case ReplyCode.WhoIsIdle:
                    case ReplyCode.WhoIsChannels:
                    case ReplyCode.EndOfWhoIs:
                        return ReceiveType.WhoIs;
                    case ReplyCode.WhoWasUser:
                    case ReplyCode.EndOfWhoWas:
                        return ReceiveType.WhoWas;
                    case ReplyCode.UserModeIs:
                        return ReceiveType.UserMode;
                    case ReplyCode.ChannelModeIs:
                        return ReceiveType.ChannelMode;
                    default:
                        if (((int)replycode >= 400) &&
                            ((int)replycode <= 599))
                        {
                            return ReceiveType.ErrorMessage;
                        }
                        else
                        {
#if LOG4NET
                            Logger.MessageTypes.Warn("replycode unknown ("+code+"): \""+rawline+"\"");
#endif
                            return ReceiveType.Unknown;
                        }
                }
            }

            found = _PingRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Unknown;
            }

            found = _ErrorRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Error;
            }

            found = _ActionRegex.Match(rawline);
            if (found.Success)
            {
                switch (found.Groups[1].Value)
                {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return ReceiveType.ChannelAction;
                    default:
                        return ReceiveType.QueryAction;
                }
            }

            found = _CtcpRequestRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.CtcpRequest;
            }

            found = _MessageRegex.Match(rawline);
            if (found.Success)
            {
                switch (found.Groups[1].Value)
                {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return ReceiveType.ChannelMessage;
                    default:
                        return ReceiveType.QueryMessage;
                }
            }

            found = _CtcpReplyRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.CtcpReply;
            }

            found = _NoticeRegex.Match(rawline);
            if (found.Success)
            {
                switch (found.Groups[1].Value)
                {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return ReceiveType.ChannelNotice;
                    default:
                        return ReceiveType.QueryNotice;
                }
            }

            found = _InviteRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Invite;
            }

            found = _JoinRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Join;
            }

            found = _TopicRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.TopicChange;
            }

            found = _NickRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.NickChange;
            }

            found = _KickRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Kick;
            }

            found = _PartRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Part;
            }

            found = _ModeRegex.Match(rawline);
            if (found.Success)
            {
                if (found.Groups[1].Value == _Nickname)
                {
                    return ReceiveType.UserModeChange;
                }
                else
                {
                    return ReceiveType.ChannelModeChange;
                }
            }

            found = _QuitRegex.Match(rawline);
            if (found.Success)
            {
                return ReceiveType.Quit;
            }

#if LOG4NET
            Logger.MessageTypes.Warn("messagetype unknown: \""+rawline+"\"");
#endif
            return ReceiveType.Unknown;
        }

        private void _HandleEvents(IrcMessageData ircdata)
        {
            if (OnRawMessage != null)
            {
                OnRawMessage(this, new IrcEventArgs(ircdata));
            }

            string code;
            // special IRC messages
            code = ircdata.RawMessageArray[0];
            switch (code)
            {
                case "PING":
                    _Event_PING(ircdata);
                    break;
                case "ERROR":
                    _Event_ERROR(ircdata);
                    break;
            }

            code = ircdata.RawMessageArray[1];
            switch (code)
            {
                case "PRIVMSG":
                    _Event_PRIVMSG(ircdata);
                    break;
                case "NOTICE":
                    _Event_NOTICE(ircdata);
                    break;
                case "JOIN":
                    _Event_JOIN(ircdata);
                    break;
                case "PART":
                    _Event_PART(ircdata);
                    break;
                case "KICK":
                    _Event_KICK(ircdata);
                    break;
                case "QUIT":
                    _Event_QUIT(ircdata);
                    break;
                case "TOPIC":
                    _Event_TOPIC(ircdata);
                    break;
                case "NICK":
                    _Event_NICK(ircdata);
                    break;
                case "INVITE":
                    _Event_INVITE(ircdata);
                    break;
                case "MODE":
                    _Event_MODE(ircdata);
                    break;
                case "PONG":
                    _Event_PONG(ircdata);
                    break;
            }

            if (ircdata.ReplyCode != ReplyCode.Null)
            {
                switch (ircdata.ReplyCode)
                {
                    case ReplyCode.Welcome:
                        _Event_RPL_WELCOME(ircdata);
                        break;
                    case ReplyCode.Topic:
                        _Event_RPL_TOPIC(ircdata);
                        break;
                    case ReplyCode.NoTopic:
                        _Event_RPL_NOTOPIC(ircdata);
                        break;
                    case ReplyCode.NamesReply:
                        _Event_RPL_NAMREPLY(ircdata);
                        break;
                    case ReplyCode.EndOfNames:
                        _Event_RPL_ENDOFNAMES(ircdata);
                        break;
                    case ReplyCode.List:
                        _Event_RPL_LIST(ircdata);
                        break;
                    case ReplyCode.ListEnd:
                        _Event_RPL_LISTEND(ircdata);
                        break;
                    case ReplyCode.WhoReply:
                        _Event_RPL_WHOREPLY(ircdata);
                        break;
                    case ReplyCode.EndOfWho:
                        _Event_RPL_ENDOFWHO(ircdata);
                        break;
                    case ReplyCode.ChannelModeIs:
                        _Event_RPL_CHANNELMODEIS(ircdata);
                        break;
                    case ReplyCode.BanList:
                        _Event_RPL_BANLIST(ircdata);
                        break;
                    case ReplyCode.EndOfBanList:
                        _Event_RPL_ENDOFBANLIST(ircdata);
                        break;
                    case ReplyCode.ErrorNoChannelModes:
                        _Event_ERR_NOCHANMODES(ircdata);
                        break;
                    case ReplyCode.Motd:
                        _Event_RPL_MOTD(ircdata);
                        break;
                    case ReplyCode.EndOfMotd:
                        _Event_RPL_ENDOFMOTD(ircdata);
                        break;
                    case ReplyCode.Away:
                        _Event_RPL_AWAY(ircdata);
                        break;
                    case ReplyCode.UnAway:
                        _Event_RPL_UNAWAY(ircdata);
                        break;
                    case ReplyCode.NowAway:
                        _Event_RPL_NOWAWAY(ircdata);
                        break;
                    case ReplyCode.TryAgain:
                        _Event_RPL_TRYAGAIN(ircdata);
                        break;
                    case ReplyCode.ErrorNicknameInUse:
                        _Event_ERR_NICKNAMEINUSE(ircdata);
                        break;
                }
            }

            if (ircdata.Type == ReceiveType.ErrorMessage)
            {
                _Event_ERR(ircdata);
            }
        }

        /// <summary>
        /// Removes a specified user from all channel lists
        /// </summary>
        /// <param name="nickname">The users 'nick' name which may NOT contain spaces</param>
        private bool _RemoveIrcUser(string nickname)
        {
            if (GetIrcUser(nickname).JoinedChannels.Length == 0)
            {
                // he is nowhere else, lets kill him
                _IrcUsers.Remove(nickname);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes a specified user from a specified channel list
        /// </summary>
        /// <param name="channelname">The name of the channel you wish to query</param>
        /// <param name="nickname">The users 'nick' name which may NOT contain spaces</param>
        private void _RemoveChannelUser(string channelname, string nickname)
        {
            Channel chan = GetChannel(channelname);
            chan.UnsafeUsers.Remove(nickname);
            chan.UnsafeOps.Remove(nickname);
            chan.UnsafeVoices.Remove(nickname);
            if (SupportNonRfc)
            {
                NonRfcChannel nchan = (NonRfcChannel)chan;
                nchan.UnsafeHalfops.Remove(nickname);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ircdata">Message data containing channel mode information</param>
        /// <param name="mode">Channel mode</param>
        /// <param name="parameter">List of supplied paramaters</param>
        private void _InterpretChannelMode(IrcMessageData ircdata, string mode, string parameter)
        {
            string[] parameters = parameter.Split(new char[] { ' ' });
            bool add = false;
            bool remove = false;
            int modelength = mode.Length;
            string temp;
            Channel channel = null;
            if (ActiveChannelSyncing)
            {
                channel = GetChannel(ircdata.Channel);
            }
            IEnumerator parametersEnumerator = parameters.GetEnumerator();
            // bring the enumerator to the 1. element
            parametersEnumerator.MoveNext();
            for (int i = 0; i < modelength; i++)
            {
                switch (mode[i])
                {
                    case '-':
                        add = false;
                        remove = true;
                        break;
                    case '+':
                        add = true;
                        remove = false;
                        break;
                    case 'o':
                        temp = (string)parametersEnumerator.Current;
                        parametersEnumerator.MoveNext();

                        if (add)
                        {
                            if (ActiveChannelSyncing)
                            {
                                // sanity check
                                if (GetChannelUser(ircdata.Channel, temp) != null)
                                {
                                    // update the op list
                                    try
                                    {
                                        channel.UnsafeOps.Add(temp, GetIrcUser(temp));
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("added op: "+temp+" to: "+ircdata.Channel);
#endif
                                    }
                                    catch (ArgumentException)
                                    {
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("duplicate op: "+temp+" in: "+ircdata.Channel+" not added");
#endif
                                    }

                                    // update the user op status
                                    ChannelUser cuser = GetChannelUser(ircdata.Channel, temp);
                                    cuser.IsOp = true;
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("set op status: " + temp + " for: "+ircdata.Channel);
#endif
                                }
                                else
                                {
#if LOG4NET
                                    Logger.ChannelSyncing.Error("_InterpretChannelMode(): GetChannelUser(" + ircdata.Channel + "," + temp + ") returned null! Ignoring...");
#endif
                                }
                            }

                            if (OnOp != null)
                            {
                                OnOp(this, new OpEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing)
                            {
                                // sanity check
                                if (GetChannelUser(ircdata.Channel, temp) != null)
                                {
                                    // update the op list
                                    channel.UnsafeOps.Remove(temp);
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("removed op: "+temp+" from: "+ircdata.Channel);
#endif
                                    // update the user op status
                                    ChannelUser cuser = GetChannelUser(ircdata.Channel, temp);
                                    cuser.IsOp = false;
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("unset op status: " + temp + " for: "+ircdata.Channel);
#endif
                                }
                                else
                                {
#if LOG4NET
                                    Logger.ChannelSyncing.Error("_InterpretChannelMode(): GetChannelUser(" + ircdata.Channel + "," + temp + ") returned null! Ignoring...");
#endif
                                }
                            }

                            if (OnDeop != null)
                            {
                                OnDeop(this, new DeopEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                        }
                        break;
                    case 'h':
                        if (SupportNonRfc)
                        {
                            temp = (string)parametersEnumerator.Current;
                            parametersEnumerator.MoveNext();

                            if (add)
                            {
                                if (ActiveChannelSyncing)
                                {
                                    // sanity check
                                    if (GetChannelUser(ircdata.Channel, temp) != null)
                                    {
                                        // update the halfop list
                                        try
                                        {
                                            ((NonRfcChannel)channel).UnsafeHalfops.Add(temp, GetIrcUser(temp));
#if LOG4NET
                                            Logger.ChannelSyncing.Debug("added halfop: "+temp+" to: "+ircdata.Channel);
#endif
                                        }
                                        catch (ArgumentException)
                                        {
#if LOG4NET
                                            Logger.ChannelSyncing.Debug("duplicate halfop: "+temp+" in: "+ircdata.Channel+" not added");
#endif
                                        }

                                        // update the user halfop status
                                        NonRfcChannelUser cuser = (NonRfcChannelUser)GetChannelUser(ircdata.Channel, temp);
                                        cuser.IsHalfop = true;
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("set halfop status: " + temp + " for: "+ircdata.Channel);
#endif
                                    }
                                    else
                                    {
#if LOG4NET
                                        Logger.ChannelSyncing.Error("_InterpretChannelMode(): GetChannelUser(" + ircdata.Channel + "," + temp + ") returned null! Ignoring...");
#endif
                                    }
                                }

                                if (OnHalfop != null)
                                {
                                    OnHalfop(this, new HalfopEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                                }
                            }
                            if (remove)
                            {
                                if (ActiveChannelSyncing)
                                {
                                    // sanity check
                                    if (GetChannelUser(ircdata.Channel, temp) != null)
                                    {
                                        // update the halfop list
                                        ((NonRfcChannel)channel).UnsafeHalfops.Remove(temp);
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("removed halfop: "+temp+" from: "+ircdata.Channel);
#endif
                                        // update the user halfop status
                                        NonRfcChannelUser cuser = (NonRfcChannelUser)GetChannelUser(ircdata.Channel, temp);
                                        cuser.IsHalfop = false;
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("unset halfop status: " + temp + " for: "+ircdata.Channel);
#endif
                                    }
                                    else
                                    {
#if LOG4NET
                                        Logger.ChannelSyncing.Error("_InterpretChannelMode(): GetChannelUser(" + ircdata.Channel + "," + temp + ") returned null! Ignoring...");
#endif
                                    }
                                }

                                if (OnDehalfop != null)
                                {
                                    OnDehalfop(this, new DehalfopEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                                }
                            }
                        }
                        break;
                    case 'v':
                        temp = (string)parametersEnumerator.Current;
                        parametersEnumerator.MoveNext();

                        if (add)
                        {
                            if (ActiveChannelSyncing)
                            {
                                // sanity check
                                if (GetChannelUser(ircdata.Channel, temp) != null)
                                {
                                    // update the voice list
                                    try
                                    {
                                        channel.UnsafeVoices.Add(temp, GetIrcUser(temp));
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("added voice: "+temp+" to: "+ircdata.Channel);
#endif
                                    }
                                    catch (ArgumentException)
                                    {
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("duplicate voice: "+temp+" in: "+ircdata.Channel+" not added");
#endif
                                    }

                                    // update the user voice status
                                    ChannelUser cuser = GetChannelUser(ircdata.Channel, temp);
                                    cuser.IsVoice = true;
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("set voice status: " + temp + " for: "+ircdata.Channel);
#endif
                                }
                                else
                                {
#if LOG4NET
                                    Logger.ChannelSyncing.Error("_InterpretChannelMode(): GetChannelUser(" + ircdata.Channel + "," + temp + ") returned null! Ignoring...");
#endif
                                }
                            }

                            if (OnVoice != null)
                            {
                                OnVoice(this, new VoiceEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing)
                            {
                                // sanity check
                                if (GetChannelUser(ircdata.Channel, temp) != null)
                                {
                                    // update the voice list
                                    channel.UnsafeVoices.Remove(temp);
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("removed voice: "+temp+" from: "+ircdata.Channel);
#endif
                                    // update the user voice status
                                    ChannelUser cuser = GetChannelUser(ircdata.Channel, temp);
                                    cuser.IsVoice = false;
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("unset voice status: " + temp + " for: "+ircdata.Channel);
#endif
                                }
                                else
                                {
#if LOG4NET
                                    Logger.ChannelSyncing.Error("_InterpretChannelMode(): GetChannelUser(" + ircdata.Channel + "," + temp + ") returned null! Ignoring...");
#endif
                                }
                            }

                            if (OnDevoice != null)
                            {
                                OnDevoice(this, new DevoiceEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                        }
                        break;
                    case 'b':
                        temp = (string)parametersEnumerator.Current;
                        parametersEnumerator.MoveNext();
                        if (add)
                        {
                            if (ActiveChannelSyncing)
                            {
                                try
                                {
                                    channel.Bans.Add(temp);
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("added ban: "+temp+" to: "+ircdata.Channel);
#endif
                                }
                                catch (ArgumentException)
                                {
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("duplicate ban: "+temp+" in: "+ircdata.Channel+" not added");
#endif
                                }
                            }
                            if (OnBan != null)
                            {
                                OnBan(this, new BanEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing)
                            {
                                channel.Bans.Remove(temp);
#if LOG4NET
                                Logger.ChannelSyncing.Debug("removed ban: "+temp+" from: "+ircdata.Channel);
#endif
                            }
                            if (OnUnban != null)
                            {
                                OnUnban(this, new UnbanEventArgs(ircdata, ircdata.Channel, ircdata.Nick, temp));
                            }
                        }
                        break;
                    case 'l':
                        temp = (string)parametersEnumerator.Current;
                        parametersEnumerator.MoveNext();
                        if (add)
                        {
                            if (ActiveChannelSyncing)
                            {
                                try
                                {
                                    channel.UserLimit = int.Parse(temp);
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("stored user limit for: "+ircdata.Channel);
#endif
                                }
                                catch (FormatException)
                                {
#if LOG4NET
                                    Logger.ChannelSyncing.Error("could not parse user limit: "+temp+" channel: "+ircdata.Channel);
#endif
                                }
                            }
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing)
                            {
                                channel.UserLimit = 0;
#if LOG4NET
                                Logger.ChannelSyncing.Debug("removed user limit for: "+ircdata.Channel);
#endif
                            }
                        }
                        break;
                    case 'k':
                        temp = (string)parametersEnumerator.Current;
                        parametersEnumerator.MoveNext();
                        if (add)
                        {
                            if (ActiveChannelSyncing)
                            {
                                channel.Key = temp;
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("stored channel key for: "+ircdata.Channel);
#endif
                            }
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing)
                            {
                                channel.Key = "";
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("removed channel key for: "+ircdata.Channel);
#endif
                            }
                        }
                        break;
                    default:
                        if (add)
                        {
                            if (ActiveChannelSyncing)
                            {
                                if (channel.Mode.IndexOf(mode[i]) == -1)
                                {
                                    channel.Mode += mode[i];
#if LOG4NET
                                        Logger.ChannelSyncing.Debug("added channel mode ("+mode[i]+") for: "+ircdata.Channel);
#endif
                                }
                            }
                        }
                        if (remove)
                        {
                            if (ActiveChannelSyncing)
                            {
                                channel.Mode = channel.Mode.Replace(mode[i].ToString(), String.Empty);
#if LOG4NET
                                    Logger.ChannelSyncing.Debug("removed channel mode ("+mode[i]+") for: "+ircdata.Channel);
#endif
                            }
                        }
                        break;
                }
            }
        }

        #region Internal Messagehandlers
        /// <summary>
        /// Event handler for ping messages
        /// </summary>
        /// <param name="ircdata">Message data containing ping information</param>
        private void _Event_PING(IrcMessageData ircdata)
        {
            string server = ircdata.RawMessageArray[1].Substring(1);
#if LOG4NET
            Logger.Connection.Debug("Ping? Pong!");
#endif
            RfcPong(server, Priority.Critical);

            if (OnPing != null)
            {
                OnPing(this, new PingEventArgs(ircdata, server));
            }
        }

        /// <summary>
        /// Event handler for PONG messages
        /// </summary>
        /// <param name="ircdata">Message data containing pong information</param>
        private void _Event_PONG(IrcMessageData ircdata)
        {
            if (OnPong != null)
            {
                OnPong(this, new PongEventArgs(ircdata, ircdata.Irc.Lag));
            }
        }

        /// <summary>
        /// Event handler for error messages
        /// </summary>
        /// <param name="ircdata">Message data containing error information</param>
        private void _Event_ERROR(IrcMessageData ircdata)
        {
            string message = ircdata.Message;
#if LOG4NET
            Logger.Connection.Info("received ERROR from IRC server");
#endif

            if (OnError != null)
            {
                OnError(this, new ErrorEventArgs(ircdata, message));
            }
        }

        /// <summary>
        /// Event handler for join messages
        /// </summary>
        /// <param name="ircdata">Message data containing join information</param>
        private void _Event_JOIN(IrcMessageData ircdata)
        {
            string who = ircdata.Nick;
            string channelname = ircdata.Channel;

            if (IsMe(who))
            {
                _JoinedChannels.Add(channelname);
            }

            if (ActiveChannelSyncing)
            {
                Channel channel;
                if (IsMe(who))
                {
                    // we joined the channel
#if LOG4NET
                    Logger.ChannelSyncing.Debug("joining channel: "+channelname);
#endif
                    channel = CreateChannel(channelname);
                    _Channels.Add(channelname, channel);
                    // request channel mode
                    RfcMode(channelname);
                    // request wholist
                    RfcWho(channelname);
                    // request banlist
                    Ban(channelname);
                }
                else
                {
                    // someone else joined the channel
                    // request the who data
                    RfcWho(who);
                }

#if LOG4NET
                Logger.ChannelSyncing.Debug(who+" joins channel: "+channelname);
#endif
                channel = GetChannel(channelname);
                IrcUser ircuser = GetIrcUser(who);

                if (ircuser == null)
                {
                    ircuser = new IrcUser(who, this);
                    ircuser.Ident = ircdata.Ident;
                    ircuser.Host = ircdata.Host;
                    _IrcUsers.Add(who, ircuser);
                }

                ChannelUser channeluser = CreateChannelUser(channelname, ircuser);
                channel.UnsafeUsers.Add(who, channeluser);
            }

            if (OnJoin != null)
            {
                OnJoin(this, new JoinEventArgs(ircdata, channelname, who));
            }
        }

        /// <summary>
        /// Event handler for part messages
        /// </summary>
        /// <param name="ircdata">Message data containing part information</param>
        private void _Event_PART(IrcMessageData ircdata)
        {
            string who = ircdata.Nick;
            string channel = ircdata.Channel;
            string partmessage = ircdata.Message;

            if (IsMe(who))
            {
                _JoinedChannels.Remove(channel);
            }

            if (ActiveChannelSyncing)
            {
                if (IsMe(who))
                {
#if LOG4NET
                    Logger.ChannelSyncing.Debug("parting channel: "+channel);
#endif
                    _Channels.Remove(channel);
                }
                else
                {
#if LOG4NET
                    Logger.ChannelSyncing.Debug(who+" parts channel: "+channel);
#endif
                    _RemoveChannelUser(channel, who);
                    _RemoveIrcUser(who);
                }
            }

            if (OnPart != null)
            {
                OnPart(this, new PartEventArgs(ircdata, channel, who, partmessage));
            }
        }

        /// <summary>
        /// Event handler for kick messages
        /// </summary>
        /// <param name="ircdata">Message data containing kick information</param>
        private void _Event_KICK(IrcMessageData ircdata)
        {
            string channelname = ircdata.Channel;
            string who = ircdata.Nick;
            string whom = ircdata.RawMessageArray[3];
            string reason = ircdata.Message;
            bool isme = IsMe(whom);

            if (isme)
            {
                _JoinedChannels.Remove(channelname);
            }

            if (ActiveChannelSyncing)
            {
                if (isme)
                {
                    Channel channel = GetChannel(channelname);
                    _Channels.Remove(channelname);
                    if (_AutoRejoinOnKick)
                    {
                        RfcJoin(channel.Name, channel.Key);
                    }
                }
                else
                {
                    _RemoveChannelUser(channelname, whom);
                    _RemoveIrcUser(whom);
                }
            }
            else
            {
                if (isme && AutoRejoinOnKick)
                {
                    RfcJoin(channelname);
                }
            }

            if (OnKick != null)
            {
                OnKick(this, new KickEventArgs(ircdata, channelname, who, whom, reason));
            }
        }

        /// <summary>
        /// Event handler for quit messages
        /// </summary>
        /// <param name="ircdata">Message data containing quit information</param>
        private void _Event_QUIT(IrcMessageData ircdata)
        {
            string who = ircdata.Nick;
            string reason = ircdata.Message;

            // no need to handle if we quit, disconnect event will take care

            if (ActiveChannelSyncing)
            {
                // sanity checks, freshirc is very broken about RFC
                IrcUser user = GetIrcUser(who);
                if (user != null)
                {
                    string[] joined_channels = user.JoinedChannels;
                    if (joined_channels != null)
                    {
                        foreach (string channel in joined_channels)
                        {
                            _RemoveChannelUser(channel, who);
                        }
                        _RemoveIrcUser(who);
#if LOG4NET
                    } else {
                        Logger.ChannelSyncing.Error("user.JoinedChannels (for: '"+who+"') returned null in _Event_QUIT! Ignoring...");
#endif
                    }
#if LOG4NET
                } else {
                    Logger.ChannelSyncing.Error("GetIrcUser("+who+") returned null in _Event_QUIT! Ignoring...");
#endif
                }
            }

            if (OnQuit != null)
            {
                OnQuit(this, new QuitEventArgs(ircdata, who, reason));
            }
        }

        /// <summary>
        /// Event handler for private messages
        /// </summary>
        /// <param name="ircdata">Message data containing private message information</param>
        private void _Event_PRIVMSG(IrcMessageData ircdata)
        {
            if (ircdata.Type == ReceiveType.CtcpRequest)
            {
                if (ircdata.Message.StartsWith("\x1" + "PING"))
                {
                    if (ircdata.Message.Length > 7)
                    {
                        SendMessage(SendType.CtcpReply, ircdata.Nick, "PING " + ircdata.Message.Substring(6, (ircdata.Message.Length - 7)));
                    }
                    else
                    {
                        SendMessage(SendType.CtcpReply, ircdata.Nick, "PING");
                    }
                }
                else if (ircdata.Message.StartsWith("\x1" + "VERSION"))
                {
                    string versionstring;
                    if (_CtcpVersion == null)
                    {
                        versionstring = VersionString;
                    }
                    else
                    {
                        versionstring = _CtcpVersion;
                    }
                    SendMessage(SendType.CtcpReply, ircdata.Nick, "VERSION " + versionstring);
                }
                else if (ircdata.Message.StartsWith("\x1" + "CLIENTINFO"))
                {
                    SendMessage(SendType.CtcpReply, ircdata.Nick, "CLIENTINFO PING VERSION CLIENTINFO");
                }
            }

            switch (ircdata.Type)
            {
                case ReceiveType.ChannelMessage:
                    if (OnChannelMessage != null)
                    {
                        OnChannelMessage(this, new IrcEventArgs(ircdata));
                    }
                    break;
                case ReceiveType.ChannelAction:
                    if (OnChannelAction != null)
                    {
                        string action = ircdata.Message.Substring(8, ircdata.Message.Length - 9);
                        OnChannelAction(this, new ActionEventArgs(ircdata, action));
                    }
                    break;
                case ReceiveType.QueryMessage:
                    if (OnQueryMessage != null)
                    {
                        OnQueryMessage(this, new IrcEventArgs(ircdata));
                    }
                    break;
                case ReceiveType.QueryAction:
                    if (OnQueryAction != null)
                    {
                        string action = ircdata.Message.Substring(8, ircdata.Message.Length - 9);
                        OnQueryAction(this, new ActionEventArgs(ircdata, action));
                    }
                    break;
                case ReceiveType.CtcpRequest:
                    if (OnCtcpRequest != null)
                    {
                        int space_pos = ircdata.Message.IndexOf(' ');
                        string cmd = "";
                        string param = "";
                        if (space_pos != -1)
                        {
                            cmd = ircdata.Message.Substring(1, space_pos - 1);
                            param = ircdata.Message.Substring(space_pos + 1,
                                        ircdata.Message.Length - space_pos - 2);
                        }
                        else
                        {
                            cmd = ircdata.Message.Substring(1, ircdata.Message.Length - 2);
                        }
                        OnCtcpRequest(this, new CtcpEventArgs(ircdata, cmd, param));
                    }
                    break;
            }
        }

        /// <summary>
        /// Event handler for notice messages
        /// </summary>
        /// <param name="ircdata">Message data containing notice information</param>
        private void _Event_NOTICE(IrcMessageData ircdata)
        {
            switch (ircdata.Type)
            {
                case ReceiveType.ChannelNotice:
                    if (OnChannelNotice != null)
                    {
                        OnChannelNotice(this, new IrcEventArgs(ircdata));
                    }
                    break;
                case ReceiveType.QueryNotice:
                    if (OnQueryNotice != null)
                    {
                        OnQueryNotice(this, new IrcEventArgs(ircdata));
                    }
                    break;
                case ReceiveType.CtcpReply:
                    if (OnCtcpReply != null)
                    {
                        int space_pos = ircdata.Message.IndexOf(' ');
                        string cmd = "";
                        string param = "";
                        if (space_pos != -1)
                        {
                            cmd = ircdata.Message.Substring(1, space_pos - 1);
                            param = ircdata.Message.Substring(space_pos + 1,
                                        ircdata.Message.Length - space_pos - 2);
                        }
                        else
                        {
                            cmd = ircdata.Message.Substring(1, ircdata.Message.Length - 2);
                        }
                        OnCtcpReply(this, new CtcpEventArgs(ircdata, cmd, param));
                    }
                    break;
            }
        }

        /// <summary>
        /// Event handler for topic messages
        /// </summary>
        /// <param name="ircdata">Message data containing topic information</param>
        private void _Event_TOPIC(IrcMessageData ircdata)
        {
            string who = ircdata.Nick;
            string channel = ircdata.Channel;
            string newtopic = ircdata.Message;

            if (ActiveChannelSyncing &&
                IsJoined(channel))
            {
                GetChannel(channel).Topic = newtopic;
#if LOG4NET
                Logger.ChannelSyncing.Debug("stored topic for channel: "+channel);
#endif
            }

            if (OnTopicChange != null)
            {
                OnTopicChange(this, new TopicChangeEventArgs(ircdata, channel, who, newtopic));
            }
        }

        /// <summary>
        /// Event handler for nickname messages
        /// </summary>
        /// <param name="ircdata">Message data containing nickname information</param>
        private void _Event_NICK(IrcMessageData ircdata)
        {
            string oldnickname = ircdata.Nick;
            //string newnickname = ircdata.Message;
            // the colon in the NICK message is optional, thus we can't rely on Message
            string newnickname = ircdata.RawMessageArray[2];

            // so let's strip the colon if it's there
            if (newnickname.StartsWith(":"))
            {
                newnickname = newnickname.Substring(1);
            }

            if (IsMe(ircdata.Nick))
            {
                // nickname change is your own
                _Nickname = newnickname;
            }

            if (ActiveChannelSyncing)
            {
                IrcUser ircuser = GetIrcUser(oldnickname);

                // if we don't have any info about him, don't update him!
                // (only queries or ourself in no channels)
                if (ircuser != null)
                {
                    string[] joinedchannels = ircuser.JoinedChannels;

                    // update his nickname
                    ircuser.Nick = newnickname;
                    // remove the old entry 
                    // remove first to avoid duplication, Foo -> foo
                    _IrcUsers.Remove(oldnickname);
                    // add him as new entry and new nickname as key
                    _IrcUsers.Add(newnickname, ircuser);
#if LOG4NET
                    Logger.ChannelSyncing.Debug("updated nickname of: "+oldnickname+" to: "+newnickname);
#endif
                    // now the same for all channels he is joined
                    Channel channel;
                    ChannelUser channeluser;
                    foreach (string channelname in joinedchannels)
                    {
                        channel = GetChannel(channelname);
                        channeluser = GetChannelUser(channelname, oldnickname);
                        // remove first to avoid duplication, Foo -> foo
                        channel.UnsafeUsers.Remove(oldnickname);
                        channel.UnsafeUsers.Add(newnickname, channeluser);
                        if (channeluser.IsOp)
                        {
                            channel.UnsafeOps.Remove(oldnickname);
                            channel.UnsafeOps.Add(newnickname, channeluser);
                        }
                        if (SupportNonRfc && ((NonRfcChannelUser)channeluser).IsHalfop)
                        {
                            NonRfcChannel nchannel = (NonRfcChannel)channel;
                            nchannel.UnsafeHalfops.Remove(oldnickname);
                            nchannel.UnsafeHalfops.Add(newnickname, channeluser);
                        }
                        if (channeluser.IsVoice)
                        {
                            channel.UnsafeVoices.Remove(oldnickname);
                            channel.UnsafeVoices.Add(newnickname, channeluser);
                        }
                    }
                }
            }

            if (OnNickChange != null)
            {
                OnNickChange(this, new NickChangeEventArgs(ircdata, oldnickname, newnickname));
            }
        }

        /// <summary>
        /// Event handler for invite messages
        /// </summary>
        /// <param name="ircdata">Message data containing invite information</param>
        private void _Event_INVITE(IrcMessageData ircdata)
        {
            string channel = ircdata.Channel;
            string inviter = ircdata.Nick;

            if (AutoJoinOnInvite)
            {
                if (channel.Trim() != "0")
                {
                    RfcJoin(channel);
                }
            }

            if (OnInvite != null)
            {
                OnInvite(this, new InviteEventArgs(ircdata, channel, inviter));
            }
        }

        /// <summary>
        /// Event handler for mode messages
        /// </summary>
        /// <param name="ircdata">Message data containing mode information</param>
        private void _Event_MODE(IrcMessageData ircdata)
        {
            if (IsMe(ircdata.RawMessageArray[2]))
            {
                // my user mode changed
                _Usermode = ircdata.RawMessageArray[3].Substring(1);
            }
            else
            {
                // channel mode changed
                string mode = ircdata.RawMessageArray[3];
                string parameter = String.Join(" ", ircdata.RawMessageArray, 4, ircdata.RawMessageArray.Length - 4);
                _InterpretChannelMode(ircdata, mode, parameter);
            }

            if ((ircdata.Type == ReceiveType.UserModeChange) &&
                (OnUserModeChange != null))
            {
                OnUserModeChange(this, new IrcEventArgs(ircdata));
            }

            if ((ircdata.Type == ReceiveType.ChannelModeChange) &&
                (OnChannelModeChange != null))
            {
                OnChannelModeChange(this, new IrcEventArgs(ircdata));
            }

            if (OnModeChange != null)
            {
                OnModeChange(this, new IrcEventArgs(ircdata));
            }
        }


        /// <summary>
        /// Event handler for channel mode reply messages
        /// </summary>
        /// <param name="ircdata">Message data containing reply information</param>
        private void _Event_RPL_CHANNELMODEIS(IrcMessageData ircdata)
        {
            if (ActiveChannelSyncing &&
                IsJoined(ircdata.Channel))
            {
                // reset stored mode first, as this is the complete mode
                Channel chan = GetChannel(ircdata.Channel);
                chan.Mode = String.Empty;
                string mode = ircdata.RawMessageArray[4];
                string parameter = String.Join(" ", ircdata.RawMessageArray, 5, ircdata.RawMessageArray.Length - 5);
                _InterpretChannelMode(ircdata, mode, parameter);
            }
        }

        /// <summary>
        /// Event handler for welcome reply messages
        /// </summary>
        /// <remark>
        /// Upon success, the client will receive an RPL_WELCOME (for users) or
        /// RPL_YOURESERVICE (for services) message indicating that the
        /// connection is now registered and known the to the entire IRC network.
        /// The reply message MUST contain the full client identifier upon which
        /// it was registered.
        /// </remark>
        /// <param name="ircdata">Message data containing reply information</param>
        private void _Event_RPL_WELCOME(IrcMessageData ircdata)
        {
            // updating our nickname, that we got (maybe cutted...)
            _Nickname = ircdata.RawMessageArray[2];

            if (OnRegistered != null)
            {
                OnRegistered(this, EventArgs.Empty);
            }
        }

        private void _Event_RPL_TOPIC(IrcMessageData ircdata)
        {
            string topic = ircdata.Message;
            string channel = ircdata.Channel;

            if (ActiveChannelSyncing &&
                IsJoined(channel))
            {
                GetChannel(channel).Topic = topic;
#if LOG4NET
                Logger.ChannelSyncing.Debug("stored topic for channel: "+channel);
#endif
            }

            if (OnTopic != null)
            {
                OnTopic(this, new TopicEventArgs(ircdata, channel, topic));
            }
        }

        private void _Event_RPL_NOTOPIC(IrcMessageData ircdata)
        {
            string channel = ircdata.Channel;

            if (ActiveChannelSyncing &&
                IsJoined(channel))
            {
                GetChannel(channel).Topic = "";
#if LOG4NET
                Logger.ChannelSyncing.Debug("stored empty topic for channel: "+channel);
#endif
            }

            if (OnTopic != null)
            {
                OnTopic(this, new TopicEventArgs(ircdata, channel, ""));
            }
        }

        private void _Event_RPL_NAMREPLY(IrcMessageData ircdata)
        {
            string channelname = ircdata.Channel;
            string[] userlist = ircdata.MessageArray;
            if (ActiveChannelSyncing &&
                IsJoined(channelname))
            {
                string nickname;
                bool op;
                bool halfop;
                bool voice;
                foreach (string user in userlist)
                {
                    if (user.Length <= 0)
                    {
                        continue;
                    }

                    op = false;
                    halfop = false;
                    voice = false;
                    switch (user[0])
                    {
                        case '@':
                            op = true;
                            nickname = user.Substring(1);
                            break;
                        case '+':
                            voice = true;
                            nickname = user.Substring(1);
                            break;
                        // RFC VIOLATION
                        // some IRC network do this and break our channel sync...
                        case '&':
                            nickname = user.Substring(1);
                            break;
                        case '%':
                            halfop = true;
                            nickname = user.Substring(1);
                            break;
                        case '~':
                            nickname = user.Substring(1);
                            break;
                        default:
                            nickname = user;
                            break;
                    }

                    IrcUser ircuser = GetIrcUser(nickname);
                    ChannelUser channeluser = GetChannelUser(channelname, nickname);

                    if (ircuser == null)
                    {
#if LOG4NET
                        Logger.ChannelSyncing.Debug("creating IrcUser: "+nickname+" because he doesn't exist yet");
#endif
                        ircuser = new IrcUser(nickname, this);
                        _IrcUsers.Add(nickname, ircuser);
                    }

                    if (channeluser == null)
                    {
#if LOG4NET
                        Logger.ChannelSyncing.Debug("creating ChannelUser: "+nickname+" for Channel: "+channelname+" because he doesn't exist yet");
#endif

                        channeluser = CreateChannelUser(channelname, ircuser);
                        Channel channel = GetChannel(channelname);

                        channel.UnsafeUsers.Add(nickname, channeluser);
                        if (op)
                        {
                            channel.UnsafeOps.Add(nickname, channeluser);
#if LOG4NET
                            Logger.ChannelSyncing.Debug("added op: "+nickname+" to: "+channelname);
#endif
                        }
                        if (SupportNonRfc && halfop)
                        {
                            ((NonRfcChannel)channel).UnsafeHalfops.Add(nickname, channeluser);
#if LOG4NET
                            Logger.ChannelSyncing.Debug("added halfop: "+nickname+" to: "+channelname);
#endif
                        }
                        if (voice)
                        {
                            channel.UnsafeVoices.Add(nickname, channeluser);
#if LOG4NET
                            Logger.ChannelSyncing.Debug("added voice: "+nickname+" to: "+channelname);
#endif
                        }
                    }

                    channeluser.IsOp = op;
                    channeluser.IsVoice = voice;
                    if (SupportNonRfc)
                    {
                        ((NonRfcChannelUser)channeluser).IsHalfop = halfop;
                    }
                }
            }

            if (OnNames != null)
            {
                OnNames(this, new NamesEventArgs(ircdata, channelname, userlist));
            }
        }

        private void _Event_RPL_LIST(IrcMessageData ircdata)
        {
            string channelName = ircdata.Channel;
            int userCount = Int32.Parse(ircdata.RawMessageArray[4]);
            string topic = ircdata.Message;

            ChannelInfo info = null;
            if (OnList != null || _ChannelList != null)
            {
                info = new ChannelInfo(channelName, userCount, topic);
            }

            if (_ChannelList != null)
            {
                _ChannelList.Add(info);
            }

            if (OnList != null)
            {
                OnList(this, new ListEventArgs(ircdata, info));
            }
        }

        private void _Event_RPL_LISTEND(IrcMessageData ircdata)
        {
            if (_ChannelListReceivedEvent != null)
            {
                _ChannelListReceivedEvent.Set();
            }
        }

        private void _Event_RPL_TRYAGAIN(IrcMessageData ircdata)
        {
            if (_ChannelListReceivedEvent != null)
            {
                _ChannelListReceivedEvent.Set();
            }
        }

        /*
        // BUG: RFC2812 says LIST and WHO might return ERR_TOOMANYMATCHES which
        // is not defined :(
        private void _Event_ERR_TOOMANYMATCHES(IrcMessageData ircdata)
        {
            if (_ListInfosReceivedEvent != null) {
                _ListInfosReceivedEvent.Set();
            }
        }
        */

        private void _Event_RPL_ENDOFNAMES(IrcMessageData ircdata)
        {
            string channelname = ircdata.RawMessageArray[3];
            if (ActiveChannelSyncing &&
                IsJoined(channelname))
            {
#if LOG4NET
                Logger.ChannelSyncing.Debug("passive synced: "+channelname);
#endif
                if (OnChannelPassiveSynced != null)
                {
                    OnChannelPassiveSynced(this, new IrcEventArgs(ircdata));
                }
            }
        }

        private void _Event_RPL_AWAY(IrcMessageData ircdata)
        {
            string who = ircdata.RawMessageArray[3];
            string awaymessage = ircdata.Message;

            if (ActiveChannelSyncing)
            {
                IrcUser ircuser = GetIrcUser(who);
                if (ircuser != null)
                {
#if LOG4NET
                    Logger.ChannelSyncing.Debug("setting away flag for user: "+who);
#endif
                    ircuser.IsAway = true;
                }
            }

            if (OnAway != null)
            {
                OnAway(this, new AwayEventArgs(ircdata, who, awaymessage));
            }
        }

        private void _Event_RPL_UNAWAY(IrcMessageData ircdata)
        {
            _IsAway = false;

            if (OnUnAway != null)
            {
                OnUnAway(this, new IrcEventArgs(ircdata));
            }
        }

        private void _Event_RPL_NOWAWAY(IrcMessageData ircdata)
        {
            _IsAway = true;

            if (OnNowAway != null)
            {
                OnNowAway(this, new IrcEventArgs(ircdata));
            }
        }

        private void _Event_RPL_WHOREPLY(IrcMessageData ircdata)
        {
            WhoInfo info = WhoInfo.Parse(ircdata);
            string channel = info.Channel;
            string nick = info.Nick;

            if (_WhoList != null)
            {
                _WhoList.Add(info);
            }

            if (ActiveChannelSyncing &&
                IsJoined(channel))
            {
                // checking the irc and channel user I only do for sanity!
                // according to RFC they must be known to us already via RPL_NAMREPLY
                // psyBNC is not very correct with this... maybe other bouncers too
                IrcUser ircuser = GetIrcUser(nick);
                ChannelUser channeluser = GetChannelUser(channel, nick);
#if LOG4NET
                if (ircuser == null) {
                    Logger.ChannelSyncing.Error("GetIrcUser("+nick+") returned null in _Event_WHOREPLY! Ignoring...");
                }
#endif

#if LOG4NET
                if (channeluser == null) {
                    Logger.ChannelSyncing.Error("GetChannelUser("+nick+") returned null in _Event_WHOREPLY! Ignoring...");
                }
#endif

                if (ircuser != null)
                {
#if LOG4NET
                    Logger.ChannelSyncing.Debug("updating userinfo (from whoreply) for user: "+nick+" channel: "+channel);
#endif

                    ircuser.Ident = info.Ident;
                    ircuser.Host = info.Host;
                    ircuser.Server = info.Server;
                    ircuser.Nick = info.Nick;
                    ircuser.HopCount = info.HopCount;
                    ircuser.Realname = info.Realname;
                    ircuser.IsAway = info.IsAway;
                    ircuser.IsIrcOp = info.IsIrcOp;

                    switch (channel[0])
                    {
                        case '#':
                        case '!':
                        case '&':
                        case '+':
                            // this channel may not be where we are joined!
                            // see RFC 1459 and RFC 2812, it must return a channelname
                            // we use this channel info when possible...
                            if (channeluser != null)
                            {
                                channeluser.IsOp = info.IsOp;
                                channeluser.IsVoice = info.IsVoice;
                            }
                            break;
                    }
                }
            }

            if (OnWho != null)
            {
                OnWho(this, new WhoEventArgs(ircdata, info));
            }
        }

        private void _Event_RPL_ENDOFWHO(IrcMessageData ircdata)
        {
            if (_WhoListReceivedEvent != null)
            {
                _WhoListReceivedEvent.Set();
            }
        }

        private void _Event_RPL_MOTD(IrcMessageData ircdata)
        {
            if (!_MotdReceived)
            {
                _Motd.Add(ircdata.Message);
            }

            if (OnMotd != null)
            {
                OnMotd(this, new MotdEventArgs(ircdata, ircdata.Message));
            }
        }

        private void _Event_RPL_ENDOFMOTD(IrcMessageData ircdata)
        {
            _MotdReceived = true;
        }

        private void _Event_RPL_BANLIST(IrcMessageData ircdata)
        {
            string channelname = ircdata.Channel;

            BanInfo info = BanInfo.Parse(ircdata);
            if (_BanList != null)
            {
                _BanList.Add(info);
            }

            if (ActiveChannelSyncing &&
                IsJoined(channelname))
            {
                Channel channel = GetChannel(channelname);
                if (channel.IsSycned)
                {
                    return;
                }

                channel.Bans.Add(info.Mask);
            }
        }

        private void _Event_RPL_ENDOFBANLIST(IrcMessageData ircdata)
        {
            string channelname = ircdata.Channel;

            if (_BanListReceivedEvent != null)
            {
                _BanListReceivedEvent.Set();
            }

            if (ActiveChannelSyncing &&
                IsJoined(channelname))
            {
                Channel channel = GetChannel(channelname);
                if (channel.IsSycned)
                {
                    // only fire the event once
                    return;
                }

                channel.ActiveSyncStop = DateTime.Now;
                channel.IsSycned = true;
#if LOG4NET
                Logger.ChannelSyncing.Debug("active synced: "+channelname+
                    " (in "+channel.ActiveSyncTime.TotalSeconds+" sec)");
#endif
                if (OnChannelActiveSynced != null)
                {
                    OnChannelActiveSynced(this, new IrcEventArgs(ircdata));
                }
            }
        }

        // MODE +b might return ERR_NOCHANMODES for mode-less channels (like +chan) 
        private void _Event_ERR_NOCHANMODES(IrcMessageData ircdata)
        {
            string channelname = ircdata.RawMessageArray[3];
            if (ActiveChannelSyncing &&
                IsJoined(channelname))
            {
                Channel channel = GetChannel(channelname);
                if (channel.IsSycned)
                {
                    // only fire the event once
                    return;
                }

                channel.ActiveSyncStop = DateTime.Now;
                channel.IsSycned = true;
#if LOG4NET
                Logger.ChannelSyncing.Debug("active synced: "+channelname+
                    " (in "+channel.ActiveSyncTime.TotalSeconds+" sec)");
#endif
                if (OnChannelActiveSynced != null)
                {
                    OnChannelActiveSynced(this, new IrcEventArgs(ircdata));
                }
            }
        }

        private void _Event_ERR(IrcMessageData ircdata)
        {
            if (OnErrorMessage != null)
            {
                OnErrorMessage(this, new IrcEventArgs(ircdata));
            }
        }

        private void _Event_ERR_NICKNAMEINUSE(IrcMessageData ircdata)
        {
#if LOG4NET
            Logger.Connection.Warn("nickname collision detected, changing nickname");
#endif
            if (!AutoNickHandling)
            {
                return;
            }

            string nickname;
            // if a nicklist has been given loop through the nicknames
            // if the upper limit of this list has been reached and still no nickname has registered
            // then generate a random nick
            if (_CurrentNickname == NicknameList.Length - 1)
            {
                Random rand = new Random();
                int number = rand.Next(999);
                if (Nickname.Length > 5)
                {
                    nickname = Nickname.Substring(0, 5) + number;
                }
                else
                {
                    nickname = Nickname.Substring(0, Nickname.Length - 1) + number;
                }
            }
            else
            {
                nickname = _NextNickname();
            }
            // change the nickname
            RfcNick(nickname, Priority.Critical);
        }
        #endregion
    }
}
namespace Meebey.SmartIrc4net
{
    public class BanInfo
    {
        private string f_Channel;
        private string f_Mask;

        public string Channel
        {
            get
            {
                return f_Channel;
            }
        }

        public string Mask
        {
            get
            {
                return f_Mask;
            }
        }

        private BanInfo()
        {
        }

        public static BanInfo Parse(IrcMessageData data)
        {
            BanInfo info = new BanInfo();
            // :magnet.oftc.net 367 meebey #smuxi test!test@test meebey!~meebey@e176002059.adsl.alicedsl.de 1216309801..
            info.f_Channel = data.RawMessageArray[3];
            info.f_Mask = data.RawMessageArray[4];
            return info;
        }
    }
}
namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// 
    /// </summary>
    /// <threadsafety static="true" instance="true" />
    public class Channel
    {
        private string _Name;
        private string _Key = String.Empty;
        private Hashtable _Users = Hashtable.Synchronized(new Hashtable(new CaseInsensitiveHashCodeProvider(), new CaseInsensitiveComparer()));
        private Hashtable _Ops = Hashtable.Synchronized(new Hashtable(new CaseInsensitiveHashCodeProvider(), new CaseInsensitiveComparer()));
        private Hashtable _Voices = Hashtable.Synchronized(new Hashtable(new CaseInsensitiveHashCodeProvider(), new CaseInsensitiveComparer()));
        private StringCollection _Bans = new StringCollection();
        private string _Topic = String.Empty;
        private int _UserLimit;
        private string _Mode = String.Empty;
        private DateTime _ActiveSyncStart;
        private DateTime _ActiveSyncStop;
        private TimeSpan _ActiveSyncTime;
        private bool _IsSycned;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"> </param>
        internal Channel(string name)
        {
            _Name = name;
            _ActiveSyncStart = DateTime.Now;
        }

#if LOG4NET
        ~Channel()
        {
            Logger.ChannelSyncing.Debug("Channel ("+Name+") destroyed");
        }
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        public string Name
        {
            get
            {
                return _Name;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        public string Key
        {
            get
            {
                return _Key;
            }
            set
            {
                _Key = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        public Hashtable Users
        {
            get
            {
                return (Hashtable)_Users.Clone();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        internal Hashtable UnsafeUsers
        {
            get
            {
                return _Users;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        public Hashtable Ops
        {
            get
            {
                return (Hashtable)_Ops.Clone();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        internal Hashtable UnsafeOps
        {
            get
            {
                return _Ops;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        public Hashtable Voices
        {
            get
            {
                return (Hashtable)_Voices.Clone();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        internal Hashtable UnsafeVoices
        {
            get
            {
                return _Voices;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        public StringCollection Bans
        {
            get
            {
                return _Bans;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        public string Topic
        {
            get
            {
                return _Topic;
            }
            set
            {
                _Topic = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        public int UserLimit
        {
            get
            {
                return _UserLimit;
            }
            set
            {
                _UserLimit = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        public string Mode
        {
            get
            {
                return _Mode;
            }
            set
            {
                _Mode = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        public DateTime ActiveSyncStart
        {
            get
            {
                return _ActiveSyncStart;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        public DateTime ActiveSyncStop
        {
            get
            {
                return _ActiveSyncStop;
            }
            set
            {
                _ActiveSyncStop = value;
                _ActiveSyncTime = _ActiveSyncStop.Subtract(_ActiveSyncStart);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        public TimeSpan ActiveSyncTime
        {
            get
            {
                return _ActiveSyncTime;
            }
        }

        public bool IsSycned
        {
            get
            {
                return _IsSycned;
            }
            set
            {
                _IsSycned = value;
            }
        }
    }
}
namespace Meebey.SmartIrc4net
{
    public class ChannelInfo
    {
        private string f_Channel;
        private int f_UserCount;
        private string f_Topic;

        public string Channel
        {
            get
            {
                return f_Channel;
            }
        }

        public int UserCount
        {
            get
            {
                return f_UserCount;
            }
        }

        public string Topic
        {
            get
            {
                return f_Topic;
            }
        }

        internal ChannelInfo(string channel, int userCount, string topic)
        {
            f_Channel = channel;
            f_UserCount = userCount;
            f_Topic = topic;
        }
    }
}
namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// This class manages the information of a user within a channel.
    /// </summary>
    /// <remarks>
    /// only used with channel sync
    /// </remarks>
    /// <threadsafety static="true" instance="true" />
    public class ChannelUser
    {
        private string _Channel;
        private IrcUser _IrcUser;
        private bool _IsOp;
        private bool _IsVoice;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"> </param>
        /// <param name="ircuser"> </param>
        internal ChannelUser(string channel, IrcUser ircuser)
        {
            _Channel = channel;
            _IrcUser = ircuser;
        }

#if LOG4NET
        ~ChannelUser()
        {
            Logger.ChannelSyncing.Debug("ChannelUser ("+Channel+":"+IrcUser.Nick+") destroyed");
        }
#endif

        /// <summary>
        /// Gets the channel name
        /// </summary>
        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        /// <summary>
        /// Gets the server operator status of the user
        /// </summary>
        public bool IsIrcOp
        {
            get
            {
                return _IrcUser.IsIrcOp;
            }
        }

        /// <summary>
        /// Gets or sets the op flag of the user (+o)
        /// </summary>
        /// <remarks>
        /// only used with channel sync
        /// </remarks>
        public bool IsOp
        {
            get
            {
                return _IsOp;
            }
            set
            {
                _IsOp = value;
            }
        }

        /// <summary>
        /// Gets or sets the voice flag of the user (+v)
        /// </summary>
        /// <remarks>
        /// only used with channel sync
        /// </remarks>
        public bool IsVoice
        {
            get
            {
                return _IsVoice;
            }
            set
            {
                _IsVoice = value;
            }
        }

        /// <summary>
        /// Gets the away status of the user
        /// </summary>
        public bool IsAway
        {
            get
            {
                return _IrcUser.IsAway;
            }
        }

        /// <summary>
        /// Gets the underlaying IrcUser object
        /// </summary>
        public IrcUser IrcUser
        {
            get
            {
                return _IrcUser;
            }
        }

        /// <summary>
        /// Gets the nickname of the user
        /// </summary>
        public string Nick
        {
            get
            {
                return _IrcUser.Nick;
            }
        }

        /// <summary>
        /// Gets the identity (username) of the user, which is used by some IRC networks for authentication.
        /// </summary>
        public string Ident
        {
            get
            {
                return _IrcUser.Ident;
            }
        }

        /// <summary>
        /// Gets the hostname of the user,
        /// </summary>
        public string Host
        {
            get
            {
                return _IrcUser.Host;
            }
        }

        /// <summary>
        /// Gets the supposed real name of the user.
        /// </summary>
        public string Realname
        {
            get
            {
                return _IrcUser.Realname;
            }
        }

        /// <summary>
        /// Gets the server the user is connected to.
        /// </summary>
        /// <value> </value>
        public string Server
        {
            get
            {
                return _IrcUser.Server;
            }
        }

        /// <summary>
        /// Gets or sets the count of hops between you and the user's server
        /// </summary>
        public int HopCount
        {
            get
            {
                return _IrcUser.HopCount;
            }
        }

        /// <summary>
        /// Gets the list of channels the user has joined
        /// </summary>
        public string[] JoinedChannels
        {
            get
            {
                return _IrcUser.JoinedChannels;
            }
        }
    }
}
namespace Meebey.SmartIrc4net
{
    public delegate void IrcEventHandler(object sender, IrcEventArgs e);
    public delegate void CtcpEventHandler(object sender, CtcpEventArgs e);
    public delegate void ActionEventHandler(object sender, ActionEventArgs e);
    public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);
    public delegate void PingEventHandler(object sender, PingEventArgs e);
    public delegate void KickEventHandler(object sender, KickEventArgs e);
    public delegate void JoinEventHandler(object sender, JoinEventArgs e);
    public delegate void NamesEventHandler(object sender, NamesEventArgs e);
    public delegate void ListEventHandler(object sender, ListEventArgs e);
}
namespace Meebey.SmartIrc4net
{
    /// <summary>
    ///
    /// </summary>
    public class ActionEventArgs : CtcpEventArgs
    {
        private string _ActionMessage;

        public string ActionMessage
        {
            get
            {
                return _ActionMessage;
            }
        }

        internal ActionEventArgs(IrcMessageData data, string actionmsg)
            : base(data, "ACTION", actionmsg)
        {
            _ActionMessage = actionmsg;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class CtcpEventArgs : IrcEventArgs
    {
        private string _CtcpCommand;
        private string _CtcpParameter;

        public string CtcpCommand
        {
            get
            {
                return _CtcpCommand;
            }
        }

        public string CtcpParameter
        {
            get
            {
                return _CtcpParameter;
            }
        }

        internal CtcpEventArgs(IrcMessageData data, string ctcpcmd, string ctcpparam)
            : base(data)
        {
            _CtcpCommand = ctcpcmd;
            _CtcpParameter = ctcpparam;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class ErrorEventArgs : IrcEventArgs
    {
        private string _ErrorMessage;

        public string ErrorMessage
        {
            get
            {
                return _ErrorMessage;
            }
        }

        internal ErrorEventArgs(IrcMessageData data, string errormsg)
            : base(data)
        {
            _ErrorMessage = errormsg;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class MotdEventArgs : IrcEventArgs
    {
        private string _MotdMessage;

        public string MotdMessage
        {
            get
            {
                return _MotdMessage;
            }
        }

        internal MotdEventArgs(IrcMessageData data, string motdmsg)
            : base(data)
        {
            _MotdMessage = motdmsg;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class PingEventArgs : IrcEventArgs
    {
        private string _PingData;

        public string PingData
        {
            get
            {
                return _PingData;
            }
        }

        internal PingEventArgs(IrcMessageData data, string pingdata)
            : base(data)
        {
            _PingData = pingdata;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class PongEventArgs : IrcEventArgs
    {
        private TimeSpan _Lag;

        public TimeSpan Lag
        {
            get
            {
                return _Lag;
            }
        }

        internal PongEventArgs(IrcMessageData data, TimeSpan lag)
            : base(data)
        {
            _Lag = lag;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class KickEventArgs : IrcEventArgs
    {
        private string _Channel;
        private string _Who;
        private string _Whom;
        private string _KickReason;

        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        public string Who
        {
            get
            {
                return _Who;
            }
        }

        public string Whom
        {
            get
            {
                return _Whom;
            }
        }

        public string KickReason
        {
            get
            {
                return _KickReason;
            }
        }

        internal KickEventArgs(IrcMessageData data, string channel, string who, string whom, string kickreason)
            : base(data)
        {
            _Channel = channel;
            _Who = who;
            _Whom = whom;
            _KickReason = kickreason;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class JoinEventArgs : IrcEventArgs
    {
        private string _Channel;
        private string _Who;

        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        public string Who
        {
            get
            {
                return _Who;
            }
        }

        internal JoinEventArgs(IrcMessageData data, string channel, string who)
            : base(data)
        {
            _Channel = channel;
            _Who = who;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class NamesEventArgs : IrcEventArgs
    {
        private string _Channel;
        private string[] _UserList;

        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        public string[] UserList
        {
            get
            {
                return _UserList;
            }
        }

        internal NamesEventArgs(IrcMessageData data, string channel, string[] userlist)
            : base(data)
        {
            _Channel = channel;
            _UserList = userlist;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class ListEventArgs : IrcEventArgs
    {
        private ChannelInfo f_ListInfo;

        public ChannelInfo ListInfo
        {
            get
            {
                return f_ListInfo;
            }
        }

        internal ListEventArgs(IrcMessageData data, ChannelInfo listInfo)
            : base(data)
        {
            f_ListInfo = listInfo;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class InviteEventArgs : IrcEventArgs
    {
        private string _Channel;
        private string _Who;

        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        public string Who
        {
            get
            {
                return _Who;
            }
        }

        internal InviteEventArgs(IrcMessageData data, string channel, string who)
            : base(data)
        {
            _Channel = channel;
            _Who = who;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class PartEventArgs : IrcEventArgs
    {
        private string _Channel;
        private string _Who;
        private string _PartMessage;

        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        public string Who
        {
            get
            {
                return _Who;
            }
        }

        public string PartMessage
        {
            get
            {
                return _PartMessage;
            }
        }

        internal PartEventArgs(IrcMessageData data, string channel, string who, string partmessage)
            : base(data)
        {
            _Channel = channel;
            _Who = who;
            _PartMessage = partmessage;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class WhoEventArgs : IrcEventArgs
    {
        private WhoInfo f_WhoInfo;

        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public string Channel
        {
            get
            {
                return f_WhoInfo.Channel;
            }
        }

        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public string Nick
        {
            get
            {
                return f_WhoInfo.Nick;
            }
        }

        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public string Ident
        {
            get
            {
                return f_WhoInfo.Ident;
            }
        }

        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public string Host
        {
            get
            {
                return f_WhoInfo.Host;
            }
        }

        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public string Realname
        {
            get
            {
                return f_WhoInfo.Realname;
            }
        }

        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public bool IsAway
        {
            get
            {
                return f_WhoInfo.IsAway;
            }
        }

        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public bool IsOp
        {
            get
            {
                return f_WhoInfo.IsOp;
            }
        }

        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public bool IsVoice
        {
            get
            {
                return f_WhoInfo.IsVoice;
            }
        }

        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public bool IsIrcOp
        {
            get
            {
                return f_WhoInfo.IsIrcOp;
            }
        }

        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public string Server
        {
            get
            {
                return f_WhoInfo.Server;
            }
        }

        [Obsolete("Use WhoEventArgs.WhoInfo instead.")]
        public int HopCount
        {
            get
            {
                return f_WhoInfo.HopCount;
            }
        }

        public WhoInfo WhoInfo
        {
            get
            {
                return f_WhoInfo;
            }
        }

        internal WhoEventArgs(IrcMessageData data, WhoInfo whoInfo)
            : base(data)
        {
            f_WhoInfo = whoInfo;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class QuitEventArgs : IrcEventArgs
    {
        private string _Who;
        private string _QuitMessage;

        public string Who
        {
            get
            {
                return _Who;
            }
        }

        public string QuitMessage
        {
            get
            {
                return _QuitMessage;
            }
        }

        internal QuitEventArgs(IrcMessageData data, string who, string quitmessage)
            : base(data)
        {
            _Who = who;
            _QuitMessage = quitmessage;
        }
    }


    /// <summary>
    ///
    /// </summary>
    public class AwayEventArgs : IrcEventArgs
    {
        private string _Who;
        private string _AwayMessage;

        public string Who
        {
            get
            {
                return _Who;
            }
        }

        public string AwayMessage
        {
            get
            {
                return _AwayMessage;
            }
        }

        internal AwayEventArgs(IrcMessageData data, string who, string awaymessage)
            : base(data)
        {
            _Who = who;
            _AwayMessage = awaymessage;
        }
    }
    /// <summary>
    ///
    /// </summary>
    public class NickChangeEventArgs : IrcEventArgs
    {
        private string _OldNickname;
        private string _NewNickname;

        public string OldNickname
        {
            get
            {
                return _OldNickname;
            }
        }

        public string NewNickname
        {
            get
            {
                return _NewNickname;
            }
        }

        internal NickChangeEventArgs(IrcMessageData data, string oldnick, string newnick)
            : base(data)
        {
            _OldNickname = oldnick;
            _NewNickname = newnick;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class TopicEventArgs : IrcEventArgs
    {
        private string _Channel;
        private string _Topic;

        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        public string Topic
        {
            get
            {
                return _Topic;
            }
        }

        internal TopicEventArgs(IrcMessageData data, string channel, string topic)
            : base(data)
        {
            _Channel = channel;
            _Topic = topic;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class TopicChangeEventArgs : IrcEventArgs
    {
        private string _Channel;
        private string _Who;
        private string _NewTopic;

        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        public string Who
        {
            get
            {
                return _Who;
            }
        }

        public string NewTopic
        {
            get
            {
                return _NewTopic;
            }
        }

        internal TopicChangeEventArgs(IrcMessageData data, string channel, string who, string newtopic)
            : base(data)
        {
            _Channel = channel;
            _Who = who;
            _NewTopic = newtopic;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class BanEventArgs : IrcEventArgs
    {
        private string _Channel;
        private string _Who;
        private string _Hostmask;

        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        public string Who
        {
            get
            {
                return _Who;
            }
        }

        public string Hostmask
        {
            get
            {
                return _Hostmask;
            }
        }

        internal BanEventArgs(IrcMessageData data, string channel, string who, string hostmask)
            : base(data)
        {
            _Channel = channel;
            _Who = who;
            _Hostmask = hostmask;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class UnbanEventArgs : IrcEventArgs
    {
        private string _Channel;
        private string _Who;
        private string _Hostmask;

        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        public string Who
        {
            get
            {
                return _Who;
            }
        }

        public string Hostmask
        {
            get
            {
                return _Hostmask;
            }
        }

        internal UnbanEventArgs(IrcMessageData data, string channel, string who, string hostmask)
            : base(data)
        {
            _Channel = channel;
            _Who = who;
            _Hostmask = hostmask;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class OpEventArgs : IrcEventArgs
    {
        private string _Channel;
        private string _Who;
        private string _Whom;

        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        public string Who
        {
            get
            {
                return _Who;
            }
        }

        public string Whom
        {
            get
            {
                return _Whom;
            }
        }

        internal OpEventArgs(IrcMessageData data, string channel, string who, string whom)
            : base(data)
        {
            _Channel = channel;
            _Who = who;
            _Whom = whom;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class DeopEventArgs : IrcEventArgs
    {
        private string _Channel;
        private string _Who;
        private string _Whom;

        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        public string Who
        {
            get
            {
                return _Who;
            }
        }

        public string Whom
        {
            get
            {
                return _Whom;
            }
        }

        internal DeopEventArgs(IrcMessageData data, string channel, string who, string whom)
            : base(data)
        {
            _Channel = channel;
            _Who = who;
            _Whom = whom;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class HalfopEventArgs : IrcEventArgs
    {
        private string _Channel;
        private string _Who;
        private string _Whom;

        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        public string Who
        {
            get
            {
                return _Who;
            }
        }

        public string Whom
        {
            get
            {
                return _Whom;
            }
        }

        internal HalfopEventArgs(IrcMessageData data, string channel, string who, string whom)
            : base(data)
        {
            _Channel = channel;
            _Who = who;
            _Whom = whom;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class DehalfopEventArgs : IrcEventArgs
    {
        private string _Channel;
        private string _Who;
        private string _Whom;

        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        public string Who
        {
            get
            {
                return _Who;
            }
        }

        public string Whom
        {
            get
            {
                return _Whom;
            }
        }

        internal DehalfopEventArgs(IrcMessageData data, string channel, string who, string whom)
            : base(data)
        {
            _Channel = channel;
            _Who = who;
            _Whom = whom;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class VoiceEventArgs : IrcEventArgs
    {
        private string _Channel;
        private string _Who;
        private string _Whom;

        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        public string Who
        {
            get
            {
                return _Who;
            }
        }

        public string Whom
        {
            get
            {
                return _Whom;
            }
        }

        internal VoiceEventArgs(IrcMessageData data, string channel, string who, string whom)
            : base(data)
        {
            _Channel = channel;
            _Who = who;
            _Whom = whom;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class DevoiceEventArgs : IrcEventArgs
    {
        private string _Channel;
        private string _Who;
        private string _Whom;

        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        public string Who
        {
            get
            {
                return _Who;
            }
        }

        public string Whom
        {
            get
            {
                return _Whom;
            }
        }

        internal DevoiceEventArgs(IrcMessageData data, string channel, string who, string whom)
            : base(data)
        {
            _Channel = channel;
            _Who = who;
            _Whom = whom;
        }
    }

}
namespace Meebey.SmartIrc4net
{
    public delegate void PartEventHandler(object sender, PartEventArgs e);
    public delegate void InviteEventHandler(object sender, InviteEventArgs e);
    public delegate void OpEventHandler(object sender, OpEventArgs e);
    public delegate void DeopEventHandler(object sender, DeopEventArgs e);
    public delegate void HalfopEventHandler(object sender, HalfopEventArgs e);
    public delegate void DehalfopEventHandler(object sender, DehalfopEventArgs e);
    public delegate void VoiceEventHandler(object sender, VoiceEventArgs e);
    public delegate void DevoiceEventHandler(object sender, DevoiceEventArgs e);
    public delegate void BanEventHandler(object sender, BanEventArgs e);
    public delegate void UnbanEventHandler(object sender, UnbanEventArgs e);
    public delegate void TopicEventHandler(object sender, TopicEventArgs e);
    public delegate void TopicChangeEventHandler(object sender, TopicChangeEventArgs e);
    public delegate void NickChangeEventHandler(object sender, NickChangeEventArgs e);
    public delegate void QuitEventHandler(object sender, QuitEventArgs e);
    public delegate void AwayEventHandler(object sender, AwayEventArgs e);
    public delegate void WhoEventHandler(object sender, WhoEventArgs e);
    public delegate void MotdEventHandler(object sender, MotdEventArgs e);
    public delegate void PongEventHandler(object sender, PongEventArgs e);
}
namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// This class contains an IRC message in a parsed form
    /// </summary>
    /// <threadsafety static="true" instance="true" />
    public class IrcMessageData
    {
        private IrcClient _Irc;
        private string _From;
        private string _Nick;
        private string _Ident;
        private string _Host;
        private string _Channel;
        private string _Message;
        private string[] _MessageArray;
        private string _RawMessage;
        private string[] _RawMessageArray;
        private ReceiveType _Type;
        private ReplyCode _ReplyCode;

        /// <summary>
        /// Gets the IrcClient object the message originated from
        /// </summary>
        public IrcClient Irc
        {
            get
            {
                return _Irc;
            }
        }

        /// <summary>
        /// Gets the combined nickname, identity and hostname of the user that sent the message
        /// </summary>
        /// <example>
        /// nick!ident@host
        /// </example>
        public string From
        {
            get
            {
                return _From;
            }
        }

        /// <summary>
        /// Gets the nickname of the user that sent the message
        /// </summary>
        public string Nick
        {
            get
            {
                return _Nick;
            }
        }

        /// <summary>
        /// Gets the identity (username) of the user that sent the message
        /// </summary>
        public string Ident
        {
            get
            {
                return _Ident;
            }
        }

        /// <summary>
        /// Gets the hostname of the user that sent the message
        /// </summary>
        public string Host
        {
            get
            {
                return _Host;
            }
        }

        /// <summary>
        /// Gets the channel the message originated from
        /// </summary>
        public string Channel
        {
            get
            {
                return _Channel;
            }
        }

        /// <summary>
        /// Gets the message
        /// </summary>
        public string Message
        {
            get
            {
                return _Message;
            }
        }

        /// <summary>
        /// Gets the message as an array of strings (splitted by space)
        /// </summary>
        public string[] MessageArray
        {
            get
            {
                return _MessageArray;
            }
        }

        /// <summary>
        /// Gets the raw message sent by the server
        /// </summary>
        public string RawMessage
        {
            get
            {
                return _RawMessage;
            }
        }

        /// <summary>
        /// Gets the raw message sent by the server as array of strings (splitted by space)
        /// </summary>
        public string[] RawMessageArray
        {
            get
            {
                return _RawMessageArray;
            }
        }

        /// <summary>
        /// Gets the message type
        /// </summary>
        public ReceiveType Type
        {
            get
            {
                return _Type;
            }
        }

        /// <summary>
        /// Gets the message reply code
        /// </summary>
        public ReplyCode ReplyCode
        {
            get
            {
                return _ReplyCode;
            }
        }

        /// <summary>
        /// Constructor to create an instace of IrcMessageData
        /// </summary>
        /// <param name="ircclient">IrcClient the message originated from</param>
        /// <param name="from">combined nickname, identity and host of the user that sent the message (nick!ident@host)</param>
        /// <param name="nick">nickname of the user that sent the message</param>
        /// <param name="ident">identity (username) of the userthat sent the message</param>
        /// <param name="host">hostname of the user that sent the message</param>
        /// <param name="channel">channel the message originated from</param>
        /// <param name="message">message</param>
        /// <param name="rawmessage">raw message sent by the server</param>
        /// <param name="type">message type</param>
        /// <param name="replycode">message reply code</param>
        public IrcMessageData(IrcClient ircclient, string from, string nick, string ident, string host, string channel, string message, string rawmessage, ReceiveType type, ReplyCode replycode)
        {
            _Irc = ircclient;
            _RawMessage = rawmessage;
            _RawMessageArray = rawmessage.Split(new char[] { ' ' });
            _Type = type;
            _ReplyCode = replycode;
            _From = from;
            _Nick = nick;
            _Ident = ident;
            _Host = host;
            _Channel = channel;
            if (message != null)
            {
                // message is optional
                _Message = message;
                _MessageArray = message.Split(new char[] { ' ' });
            }
        }
    }
}
namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// This class manages the user information.
    /// </summary>
    /// <remarks>
    /// only used with channel sync
    /// <seealso cref="IrcClient.ActiveChannelSyncing">
    ///   IrcClient.ActiveChannelSyncing
    /// </seealso>
    /// </remarks>
    /// <threadsafety static="true" instance="true" />
    public class IrcUser
    {
        private IrcClient _IrcClient;
        private string _Nick = null;
        private string _Ident = null;
        private string _Host = null;
        private string _Realname = null;
        private bool _IsIrcOp = false;
        private bool _IsAway = false;
        private string _Server = null;
        private int _HopCount = -1;

        internal IrcUser(string nickname, IrcClient ircclient)
        {
            _IrcClient = ircclient;
            _Nick = nickname;
        }

#if LOG4NET
        ~IrcUser()
        {
            Logger.ChannelSyncing.Debug("IrcUser ("+Nick+") destroyed");
        }
#endif

        /// <summary>
        /// Gets or sets the nickname of the user.
        /// </summary>
        /// <remarks>
        /// Do _not_ set this value, it will break channel sync!
        /// </remarks>
        public string Nick
        {
            get
            {
                return _Nick;
            }
            set
            {
                _Nick = value;
            }
        }

        /// <summary>
        /// Gets or sets the identity (username) of the user which is used by some IRC networks for authentication. 
        /// </summary>
        /// <remarks>
        /// Do _not_ set this value, it will break channel sync!
        /// </remarks>
        public string Ident
        {
            get
            {
                return _Ident;
            }
            set
            {
                _Ident = value;
            }
        }

        /// <summary>
        /// Gets or sets the hostname of the user. 
        /// </summary>
        /// <remarks>
        /// Do _not_ set this value, it will break channel sync!
        /// </remarks>
        public string Host
        {
            get
            {
                return _Host;
            }
            set
            {
                _Host = value;
            }
        }

        /// <summary>
        /// Gets or sets the supposed real name of the user.
        /// </summary>
        /// <remarks>
        /// Do _not_ set this value, it will break channel sync!
        /// </remarks>
        public string Realname
        {
            get
            {
                return _Realname;
            }
            set
            {
                _Realname = value;
            }
        }

        /// <summary>
        /// Gets or sets the server operator status of the user
        /// </summary>
        /// <remarks>
        /// Do _not_ set this value, it will break channel sync!
        /// </remarks>
        public bool IsIrcOp
        {
            get
            {
                return _IsIrcOp;
            }
            set
            {
                _IsIrcOp = value;
            }
        }

        /// <summary>
        /// Gets or sets away status of the user
        /// </summary>
        /// <remarks>
        /// Do _not_ set this value, it will break channel sync!
        /// </remarks>
        public bool IsAway
        {
            get
            {
                return _IsAway;
            }
            set
            {
                _IsAway = value;
            }
        }

        /// <summary>
        /// Gets or sets the server the user is connected to
        /// </summary>
        /// <remarks>
        /// Do _not_ set this value, it will break channel sync!
        /// </remarks>
        public string Server
        {
            get
            {
                return _Server;
            }
            set
            {
                _Server = value;
            }
        }

        /// <summary>
        /// Gets or sets the count of hops between you and the user's server
        /// </summary>
        /// <remarks>
        /// Do _not_ set this value, it will break channel sync!
        /// </remarks>
        public int HopCount
        {
            get
            {
                return _HopCount;
            }
            set
            {
                _HopCount = value;
            }
        }

        /// <summary>
        /// Gets the list of channels the user has joined
        /// </summary>
        public string[] JoinedChannels
        {
            get
            {
                Channel channel;
                string[] result;
                string[] channels = _IrcClient.GetChannels();
                StringCollection joinedchannels = new StringCollection();
                foreach (string channelname in channels)
                {
                    channel = _IrcClient.GetChannel(channelname);
                    if (channel.UnsafeUsers.ContainsKey(_Nick))
                    {
                        joinedchannels.Add(channelname);
                    }
                }

                result = new string[joinedchannels.Count];
                joinedchannels.CopyTo(result, 0);
                return result;
                //return joinedchannels;
            }
        }
    }
}
namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// 
    /// </summary>
    /// <threadsafety static="true" instance="true" />
    public class NonRfcChannel : Channel
    {
        private Hashtable _Halfops = Hashtable.Synchronized(new Hashtable(new CaseInsensitiveHashCodeProvider(), new CaseInsensitiveComparer()));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"> </param>
        internal NonRfcChannel(string name)
            : base(name)
        {
        }

#if LOG4NET
        ~NonRfcChannel()
        {
            Logger.ChannelSyncing.Debug("NonRfcChannel ("+Name+") destroyed");
        }
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        public Hashtable Halfops
        {
            get
            {
                return (Hashtable)_Halfops.Clone();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        internal Hashtable UnsafeHalfops
        {
            get
            {
                return _Halfops;
            }
        }
    }
}
namespace Meebey.SmartIrc4net
{
    /// <summary>
    ///
    /// </summary>
    /// <threadsafety static="true" instance="true" />
    public class NonRfcChannelUser : ChannelUser
    {
        private bool _IsHalfop;
        private bool _IsOwner;
        private bool _IsAdmin;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"> </param>
        /// <param name="ircuser"> </param>
        internal NonRfcChannelUser(string channel, IrcUser ircuser)
            : base(channel, ircuser)
        {
        }

#if LOG4NET
        ~NonRfcChannelUser()
        {
            Logger.ChannelSyncing.Debug("NonRfcChannelUser ("+Channel+":"+IrcUser.Nick+") destroyed");
        }
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <value> </value>
        public bool IsHalfop
        {
            get
            {
                return _IsHalfop;
            }
            set
            {
                _IsHalfop = value;
            }
        }
    }
}
namespace Meebey.SmartIrc4net
{
    public class WhoInfo
    {
        private string f_Channel;
        private string f_Ident;
        private string f_Host;
        private string f_Server;
        private string f_Nick;
        private int f_HopCount;
        private string f_Realname;
        private bool f_IsAway;
        private bool f_IsOp;
        private bool f_IsVoice;
        private bool f_IsIrcOp;

        public string Channel
        {
            get
            {
                return f_Channel;
            }
        }

        public string Ident
        {
            get
            {
                return f_Ident;
            }
        }

        public string Host
        {
            get
            {
                return f_Host;
            }
        }

        public string Server
        {
            get
            {
                return f_Server;
            }
        }

        public string Nick
        {
            get
            {
                return f_Nick;
            }
        }

        public int HopCount
        {
            get
            {
                return f_HopCount;
            }
        }

        public string Realname
        {
            get
            {
                return f_Realname;
            }
        }

        public bool IsAway
        {
            get
            {
                return f_IsAway;
            }
        }

        public bool IsOp
        {
            get
            {
                return f_IsOp;
            }
        }

        public bool IsVoice
        {
            get
            {
                return f_IsVoice;
            }
        }

        public bool IsIrcOp
        {
            get
            {
                return f_IsIrcOp;
            }
        }

        private WhoInfo()
        {
        }

        public static WhoInfo Parse(IrcMessageData data)
        {
            WhoInfo whoInfo = new WhoInfo();
            // :fu-berlin.de 352 meebey_ * ~meebey e176002059.adsl.alicedsl.de fu-berlin.de meebey_ H :0 Mirco Bauer..
            whoInfo.f_Channel = data.RawMessageArray[3];
            whoInfo.f_Ident = data.RawMessageArray[4];
            whoInfo.f_Host = data.RawMessageArray[5];
            whoInfo.f_Server = data.RawMessageArray[6];
            whoInfo.f_Nick = data.RawMessageArray[7];
            // skip hop count
            whoInfo.f_Realname = String.Join(" ", data.MessageArray, 1, data.MessageArray.Length - 1);

            int hopcount = 0;
            string hopcountStr = data.MessageArray[0];
            try
            {
                hopcount = int.Parse(hopcountStr);
            }
            catch (FormatException ex)
            {
#if LOG4NET
                Logger.MessageParser.Warn("Parse(): couldn't parse (as int): '" + hopcountStr + "'", ex);
#endif
            }

            string usermode = data.RawMessageArray[8];
            bool op = false;
            bool voice = false;
            bool ircop = false;
            bool away = false;
            int usermodelength = usermode.Length;
            for (int i = 0; i < usermodelength; i++)
            {
                switch (usermode[i])
                {
                    case 'H':
                        away = false;
                        break;
                    case 'G':
                        away = true;
                        break;
                    case '@':
                        op = true;
                        break;
                    case '+':
                        voice = true;
                        break;
                    case '*':
                        ircop = true;
                        break;
                }
            }
            whoInfo.f_IsAway = away;
            whoInfo.f_IsOp = op;
            whoInfo.f_IsVoice = voice;
            whoInfo.f_IsIrcOp = ircop;

            return whoInfo;
        }
    }
}
namespace Meebey.SmartIrc4net
{
    /// <summary>
    ///
    /// </summary>
    /// <threadsafety static="true" instance="true" />
    public class IrcCommands : IrcConnection
    {
        private int _MaxModeChanges = 3;

        protected int MaxModeChanges
        {
            get
            {
                return _MaxModeChanges;
            }
            set
            {
                _MaxModeChanges = value;
            }
        }

#if LOG4NET
        public IrcCommands()
        {
            Logger.Main.Debug("IrcCommands created");
        }
#endif

#if LOG4NET
        ~IrcCommands()
        {
            Logger.Main.Debug("IrcCommands destroyed");
        }
#endif

        // API commands
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="destination"></param>
        /// <param name="message"></param>
        /// <param name="priority"></param>
        public void SendMessage(SendType type, string destination, string message, Priority priority)
        {
            switch (type)
            {
                case SendType.Message:
                    RfcPrivmsg(destination, message, priority);
                    break;
                case SendType.Action:
                    RfcPrivmsg(destination, "\x1" + "ACTION " + message + "\x1", priority);
                    break;
                case SendType.Notice:
                    RfcNotice(destination, message, priority);
                    break;
                case SendType.CtcpRequest:
                    RfcPrivmsg(destination, "\x1" + message + "\x1", priority);
                    break;
                case SendType.CtcpReply:
                    RfcNotice(destination, "\x1" + message + "\x1", priority);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="destination"></param>
        /// <param name="message"></param>
        public void SendMessage(SendType type, string destination, string message)
        {
            SendMessage(type, destination, message, Priority.Medium);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="message"></param>
        /// <param name="priority"></param>
        public void SendReply(IrcMessageData data, string message, Priority priority)
        {
            switch (data.Type)
            {
                case ReceiveType.ChannelMessage:
                    SendMessage(SendType.Message, data.Channel, message, priority);
                    break;
                case ReceiveType.QueryMessage:
                    SendMessage(SendType.Message, data.Nick, message, priority);
                    break;
                case ReceiveType.QueryNotice:
                    SendMessage(SendType.Notice, data.Nick, message, priority);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="message"></param>
        public void SendReply(IrcMessageData data, string message)
        {
            SendReply(data, message, Priority.Medium);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nickname"></param>
        /// <param name="priority"></param>
        public void Op(string channel, string nickname, Priority priority)
        {
            WriteLine(Rfc2812.Mode(channel, "+o " + nickname), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nickname"></param>
        /*
        public void Op(string channel, string[] nicknames)
        {
            if (nicknames == null) {
                throw new ArgumentNullException("nicknames");
            }
            
            string[] modes = new string[nicknames.Length];
            for (int i = 0; i < nicknames.Length; i++) {
                modes[i] = "+o";
            }
            WriteLine(Rfc2812.Mode(channel, modes, nicknames));
        }
        */

        public void Op(string channel, string nickname)
        {
            WriteLine(Rfc2812.Mode(channel, "+o " + nickname));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nickname"></param>
        /// <param name="priority"></param>
        public void Deop(string channel, string nickname, Priority priority)
        {
            WriteLine(Rfc2812.Mode(channel, "-o " + nickname), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nickname"></param>
        public void Deop(string channel, string nickname)
        {
            WriteLine(Rfc2812.Mode(channel, "-o " + nickname));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nickname"></param>
        /// <param name="priority"></param>
        public void Voice(string channel, string nickname, Priority priority)
        {
            WriteLine(Rfc2812.Mode(channel, "+v " + nickname), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nickname"></param>
        public void Voice(string channel, string nickname)
        {
            WriteLine(Rfc2812.Mode(channel, "+v " + nickname));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nickname"></param>
        /// <param name="priority"></param>
        public void Devoice(string channel, string nickname, Priority priority)
        {
            WriteLine(Rfc2812.Mode(channel, "-v " + nickname), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nickname"></param>
        public void Devoice(string channel, string nickname)
        {
            WriteLine(Rfc2812.Mode(channel, "-v " + nickname));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="priority"></param>
        public void Ban(string channel, Priority priority)
        {
            WriteLine(Rfc2812.Mode(channel, "+b"), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        public void Ban(string channel)
        {
            WriteLine(Rfc2812.Mode(channel, "+b"));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="hostmask"></param>
        /// <param name="priority"></param>
        public void Ban(string channel, string hostmask, Priority priority)
        {
            WriteLine(Rfc2812.Mode(channel, "+b " + hostmask), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="hostmask"></param>
        public void Ban(string channel, string hostmask)
        {
            WriteLine(Rfc2812.Mode(channel, "+b " + hostmask));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="hostmask"></param>
        /// <param name="priority"></param>
        public void Unban(string channel, string hostmask, Priority priority)
        {
            WriteLine(Rfc2812.Mode(channel, "-b " + hostmask), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="hostmask"></param>
        public void Unban(string channel, string hostmask)
        {
            WriteLine(Rfc2812.Mode(channel, "-b " + hostmask));
        }

        // non-RFC commands
        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nickname"></param>
        /// <param name="priority"></param>
        public void Halfop(string channel, string nickname, Priority priority)
        {
            WriteLine(Rfc2812.Mode(channel, "+h " + nickname), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nickname"></param>
        public void Dehalfop(string channel, string nickname)
        {
            WriteLine(Rfc2812.Mode(channel, "-h " + nickname));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nickname"></param>
        /*
        public void Mode(string target, string[] newModes, string[] newModeParameters)
        {
            int modeLines = (int) Math.Ceiling(newModes.Length / (double) _MaxModeChanges);
            for (int i = 0; i < modeLines; i++) {
                int chunkOffset = i * _MaxModeChanges;
                string[] newModeChunks = new string[_MaxModeChanges];
                string[] newModeParameterChunks = new string[_MaxModeChanges];
                for (int j = 0; j < _MaxModeChanges; j++) {
                    newModeChunks[j] = newModes[chunkOffset + j];
                    newModeParameterChunks[j] = newModeParameterChunks[chunkOffset + j];
                }
                WriteLine(Rfc2812.Mode(target, newModeChunks, newModeParameterChunks));
            }
        }
        */

        #region RFC commands
        /// <summary>
        /// 
        /// </summary>
        /// <param name="password"></param>
        /// <param name="priority"></param>
        public void RfcPass(string password, Priority priority)
        {
            WriteLine(Rfc2812.Pass(password), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="password"></param>
        public void RfcPass(string password)
        {
            WriteLine(Rfc2812.Pass(password));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="usermode"></param>
        /// <param name="realname"></param>
        /// <param name="priority"></param>
        public void RfcUser(string username, int usermode, string realname, Priority priority)
        {
            WriteLine(Rfc2812.User(username, usermode, realname), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="usermode"></param>
        /// <param name="realname"></param>
        public void RfcUser(string username, int usermode, string realname)
        {
            WriteLine(Rfc2812.User(username, usermode, realname));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="password"></param>
        /// <param name="priority"></param>
        public void RfcOper(string name, string password, Priority priority)
        {
            WriteLine(Rfc2812.Oper(name, password), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="password"></param>
        public void RfcOper(string name, string password)
        {
            WriteLine(Rfc2812.Oper(name, password));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="message"></param>
        /// <param name="priority"></param>
        public void RfcPrivmsg(string destination, string message, Priority priority)
        {
            WriteLine(Rfc2812.Privmsg(destination, message), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="message"></param>
        public void RfcPrivmsg(string destination, string message)
        {
            WriteLine(Rfc2812.Privmsg(destination, message));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="message"></param>
        /// <param name="priority"></param>
        public void RfcNotice(string destination, string message, Priority priority)
        {
            WriteLine(Rfc2812.Notice(destination, message), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="message"></param>
        public void RfcNotice(string destination, string message)
        {
            WriteLine(Rfc2812.Notice(destination, message));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="priority"></param>
        public void RfcJoin(string channel, Priority priority)
        {
            WriteLine(Rfc2812.Join(channel), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        public void RfcJoin(string channel)
        {
            WriteLine(Rfc2812.Join(channel));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="priority"></param>
        public void RfcJoin(string[] channels, Priority priority)
        {
            WriteLine(Rfc2812.Join(channels), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        public void RfcJoin(string[] channels)
        {
            WriteLine(Rfc2812.Join(channels));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="key"></param>
        /// <param name="priority"></param>
        public void RfcJoin(string channel, string key, Priority priority)
        {
            WriteLine(Rfc2812.Join(channel, key), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="key"></param>
        public void RfcJoin(string channel, string key)
        {
            WriteLine(Rfc2812.Join(channel, key));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="keys"></param>
        /// <param name="priority"></param>
        public void RfcJoin(string[] channels, string[] keys, Priority priority)
        {
            WriteLine(Rfc2812.Join(channels, keys), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="keys"></param>
        public void RfcJoin(string[] channels, string[] keys)
        {
            WriteLine(Rfc2812.Join(channels, keys));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="priority"></param>
        public void RfcPart(string channel, Priority priority)
        {
            WriteLine(Rfc2812.Part(channel), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        public void RfcPart(string channel)
        {
            WriteLine(Rfc2812.Part(channel));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="priority"></param>
        public void RfcPart(string[] channels, Priority priority)
        {
            WriteLine(Rfc2812.Part(channels), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        public void RfcPart(string[] channels)
        {
            WriteLine(Rfc2812.Part(channels));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="partmessage"></param>
        /// <param name="priority"></param>
        public void RfcPart(string channel, string partmessage, Priority priority)
        {
            WriteLine(Rfc2812.Part(channel, partmessage), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="partmessage"></param>
        public void RfcPart(string channel, string partmessage)
        {
            WriteLine(Rfc2812.Part(channel, partmessage));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="partmessage"></param>
        /// <param name="priority"></param>
        public void RfcPart(string[] channels, string partmessage, Priority priority)
        {
            WriteLine(Rfc2812.Part(channels, partmessage), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="partmessage"></param>
        public void RfcPart(string[] channels, string partmessage)
        {
            WriteLine(Rfc2812.Part(channels, partmessage));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nickname"></param>
        /// <param name="priority"></param>
        public void RfcKick(string channel, string nickname, Priority priority)
        {
            WriteLine(Rfc2812.Kick(channel, nickname), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nickname"></param>
        public void RfcKick(string channel, string nickname)
        {
            WriteLine(Rfc2812.Kick(channel, nickname));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="nickname"></param>
        /// <param name="priority"></param>
        public void RfcKick(string[] channels, string nickname, Priority priority)
        {
            WriteLine(Rfc2812.Kick(channels, nickname), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="nickname"></param>
        public void RfcKick(string[] channels, string nickname)
        {
            WriteLine(Rfc2812.Kick(channels, nickname));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nicknames"></param>
        /// <param name="priority"></param>
        public void RfcKick(string channel, string[] nicknames, Priority priority)
        {
            WriteLine(Rfc2812.Kick(channel, nicknames), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nicknames"></param>
        public void RfcKick(string channel, string[] nicknames)
        {
            WriteLine(Rfc2812.Kick(channel, nicknames));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="nicknames"></param>
        /// <param name="priority"></param>
        public void RfcKick(string[] channels, string[] nicknames, Priority priority)
        {
            WriteLine(Rfc2812.Kick(channels, nicknames), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="nicknames"></param>
        public void RfcKick(string[] channels, string[] nicknames)
        {
            WriteLine(Rfc2812.Kick(channels, nicknames));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nickname"></param>
        /// <param name="comment"></param>
        /// <param name="priority"></param>
        public void RfcKick(string channel, string nickname, string comment, Priority priority)
        {
            WriteLine(Rfc2812.Kick(channel, nickname, comment), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nickname"></param>
        /// <param name="comment"></param>
        public void RfcKick(string channel, string nickname, string comment)
        {
            WriteLine(Rfc2812.Kick(channel, nickname, comment));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="nickname"></param>
        /// <param name="comment"></param>
        /// <param name="priority"></param>
        public void RfcKick(string[] channels, string nickname, string comment, Priority priority)
        {
            WriteLine(Rfc2812.Kick(channels, nickname, comment), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="nickname"></param>
        /// <param name="comment"></param>
        public void RfcKick(string[] channels, string nickname, string comment)
        {
            WriteLine(Rfc2812.Kick(channels, nickname, comment));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nicknames"></param>
        /// <param name="comment"></param>
        /// <param name="priority"></param>
        public void RfcKick(string channel, string[] nicknames, string comment, Priority priority)
        {
            WriteLine(Rfc2812.Kick(channel, nicknames, comment), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="nicknames"></param>
        /// <param name="comment"></param>
        public void RfcKick(string channel, string[] nicknames, string comment)
        {
            WriteLine(Rfc2812.Kick(channel, nicknames, comment));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="nicknames"></param>
        /// <param name="comment"></param>
        /// <param name="priority"></param>
        public void RfcKick(string[] channels, string[] nicknames, string comment, Priority priority)
        {
            WriteLine(Rfc2812.Kick(channels, nicknames, comment), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="nicknames"></param>
        /// <param name="comment"></param>
        public void RfcKick(string[] channels, string[] nicknames, string comment)
        {
            WriteLine(Rfc2812.Kick(channels, nicknames, comment));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        public void RfcMotd(Priority priority)
        {
            WriteLine(Rfc2812.Motd(), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RfcMotd()
        {
            WriteLine(Rfc2812.Motd());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        public void RfcMotd(string target, Priority priority)
        {
            WriteLine(Rfc2812.Motd(target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        public void RfcMotd(string target)
        {
            WriteLine(Rfc2812.Motd(target));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        [Obsolete("use RfcLusers(Priority) instead")]
        public void RfcLuser(Priority priority)
        {
            RfcLusers(priority);
        }

        public void RfcLusers(Priority priority)
        {
            WriteLine(Rfc2812.Lusers(), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        [Obsolete("use RfcLusers() instead")]
        public void RfcLuser()
        {
            RfcLusers();
        }

        public void RfcLusers()
        {
            WriteLine(Rfc2812.Lusers());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="priority"></param>
        [Obsolete("use RfcLusers(string, Priority) instead")]
        public void RfcLuser(string mask, Priority priority)
        {
            RfcLusers(mask, priority);
        }

        public void RfcLusers(string mask, Priority priority)
        {
            WriteLine(Rfc2812.Lusers(mask), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask"></param>
        [Obsolete("use RfcLusers(string) instead")]
        public void RfcLuser(string mask)
        {
            RfcLusers(mask);
        }

        public void RfcLusers(string mask)
        {
            WriteLine(Rfc2812.Lusers(mask));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        [Obsolete("use RfcLusers(string, string, Priority) instead")]
        public void RfcLuser(string mask, string target, Priority priority)
        {
            RfcLusers(mask, target, priority);
        }

        public void RfcLusers(string mask, string target, Priority priority)
        {
            WriteLine(Rfc2812.Lusers(mask, target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="target"></param>
        [Obsolete("use RfcLusers(string, string) instead")]
        public void RfcLuser(string mask, string target)
        {
            RfcLusers(mask, target);
        }

        public void RfcLusers(string mask, string target)
        {
            WriteLine(Rfc2812.Lusers(mask, target));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        public void RfcVersion(Priority priority)
        {
            WriteLine(Rfc2812.Version(), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RfcVersion()
        {
            WriteLine(Rfc2812.Version());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        public void RfcVersion(string target, Priority priority)
        {
            WriteLine(Rfc2812.Version(target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        public void RfcVersion(string target)
        {
            WriteLine(Rfc2812.Version(target));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        public void RfcStats(Priority priority)
        {
            WriteLine(Rfc2812.Stats(), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RfcStats()
        {
            WriteLine(Rfc2812.Stats());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="priority"></param>
        public void RfcStats(string query, Priority priority)
        {
            WriteLine(Rfc2812.Stats(query), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        public void RfcStats(string query)
        {
            WriteLine(Rfc2812.Stats(query));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        public void RfcStats(string query, string target, Priority priority)
        {
            WriteLine(Rfc2812.Stats(query, target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query"></param>
        /// <param name="target"></param>
        public void RfcStats(string query, string target)
        {
            WriteLine(Rfc2812.Stats(query, target));
        }

        /// <summary>
        /// 
        /// </summary>
        public void RfcLinks()
        {
            WriteLine(Rfc2812.Links());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="servermask"></param>
        /// <param name="priority"></param>
        public void RfcLinks(string servermask, Priority priority)
        {
            WriteLine(Rfc2812.Links(servermask), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="servermask"></param>
        public void RfcLinks(string servermask)
        {
            WriteLine(Rfc2812.Links(servermask));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteserver"></param>
        /// <param name="servermask"></param>
        /// <param name="priority"></param>
        public void RfcLinks(string remoteserver, string servermask, Priority priority)
        {
            WriteLine(Rfc2812.Links(remoteserver, servermask), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteserver"></param>
        /// <param name="servermask"></param>
        public void RfcLinks(string remoteserver, string servermask)
        {
            WriteLine(Rfc2812.Links(remoteserver, servermask));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        public void RfcTime(Priority priority)
        {
            WriteLine(Rfc2812.Time(), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RfcTime()
        {
            WriteLine(Rfc2812.Time());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        public void RfcTime(string target, Priority priority)
        {
            WriteLine(Rfc2812.Time(target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        public void RfcTime(string target)
        {
            WriteLine(Rfc2812.Time(target));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetserver"></param>
        /// <param name="port"></param>
        /// <param name="priority"></param>
        public void RfcConnect(string targetserver, string port, Priority priority)
        {
            WriteLine(Rfc2812.Connect(targetserver, port), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetserver"></param>
        /// <param name="port"></param>
        public void RfcConnect(string targetserver, string port)
        {
            WriteLine(Rfc2812.Connect(targetserver, port));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetserver"></param>
        /// <param name="port"></param>
        /// <param name="remoteserver"></param>
        /// <param name="priority"></param>
        public void RfcConnect(string targetserver, string port, string remoteserver, Priority priority)
        {
            WriteLine(Rfc2812.Connect(targetserver, port, remoteserver), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetserver"></param>
        /// <param name="port"></param>
        /// <param name="remoteserver"></param>
        public void RfcConnect(string targetserver, string port, string remoteserver)
        {
            WriteLine(Rfc2812.Connect(targetserver, port, remoteserver));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        public void RfcTrace(Priority priority)
        {
            WriteLine(Rfc2812.Trace(), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RfcTrace()
        {
            WriteLine(Rfc2812.Trace());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        public void RfcTrace(string target, Priority priority)
        {
            WriteLine(Rfc2812.Trace(target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        public void RfcTrace(string target)
        {
            WriteLine(Rfc2812.Trace(target));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        public void RfcAdmin(Priority priority)
        {
            WriteLine(Rfc2812.Admin(), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RfcAdmin()
        {
            WriteLine(Rfc2812.Admin());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        public void RfcAdmin(string target, Priority priority)
        {
            WriteLine(Rfc2812.Admin(target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        public void RfcAdmin(string target)
        {
            WriteLine(Rfc2812.Admin(target));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        public void RfcInfo(Priority priority)
        {
            WriteLine(Rfc2812.Info(), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RfcInfo()
        {
            WriteLine(Rfc2812.Info());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        public void RfcInfo(string target, Priority priority)
        {
            WriteLine(Rfc2812.Info(target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        public void RfcInfo(string target)
        {
            WriteLine(Rfc2812.Info(target));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        public void RfcServlist(Priority priority)
        {
            WriteLine(Rfc2812.Servlist(), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RfcServlist()
        {
            WriteLine(Rfc2812.Servlist());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="priority"></param>
        public void RfcServlist(string mask, Priority priority)
        {
            WriteLine(Rfc2812.Servlist(mask), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask"></param>
        public void RfcServlist(string mask)
        {
            WriteLine(Rfc2812.Servlist(mask));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="type"></param>
        /// <param name="priority"></param>
        public void RfcServlist(string mask, string type, Priority priority)
        {
            WriteLine(Rfc2812.Servlist(mask, type), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="type"></param>
        public void RfcServlist(string mask, string type)
        {
            WriteLine(Rfc2812.Servlist(mask, type));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="servicename"></param>
        /// <param name="servicetext"></param>
        /// <param name="priority"></param>
        public void RfcSquery(string servicename, string servicetext, Priority priority)
        {
            WriteLine(Rfc2812.Squery(servicename, servicetext), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="servicename"></param>
        /// <param name="servicetext"></param>
        public void RfcSquery(string servicename, string servicetext)
        {
            WriteLine(Rfc2812.Squery(servicename, servicetext));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="priority"></param>
        public void RfcList(string channel, Priority priority)
        {
            WriteLine(Rfc2812.List(channel), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        public void RfcList(string channel)
        {
            WriteLine(Rfc2812.List(channel));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="priority"></param>
        public void RfcList(string[] channels, Priority priority)
        {
            WriteLine(Rfc2812.List(channels), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        public void RfcList(string[] channels)
        {
            WriteLine(Rfc2812.List(channels));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        public void RfcList(string channel, string target, Priority priority)
        {
            WriteLine(Rfc2812.List(channel, target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="target"></param>
        public void RfcList(string channel, string target)
        {
            WriteLine(Rfc2812.List(channel, target));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        public void RfcList(string[] channels, string target, Priority priority)
        {
            WriteLine(Rfc2812.List(channels, target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="target"></param>
        public void RfcList(string[] channels, string target)
        {
            WriteLine(Rfc2812.List(channels, target));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="priority"></param>
        public void RfcNames(string channel, Priority priority)
        {
            WriteLine(Rfc2812.Names(channel), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        public void RfcNames(string channel)
        {
            WriteLine(Rfc2812.Names(channel));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="priority"></param>
        public void RfcNames(string[] channels, Priority priority)
        {
            WriteLine(Rfc2812.Names(channels), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        public void RfcNames(string[] channels)
        {
            WriteLine(Rfc2812.Names(channels));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        public void RfcNames(string channel, string target, Priority priority)
        {
            WriteLine(Rfc2812.Names(channel, target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="target"></param>
        public void RfcNames(string channel, string target)
        {
            WriteLine(Rfc2812.Names(channel, target));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        public void RfcNames(string[] channels, string target, Priority priority)
        {
            WriteLine(Rfc2812.Names(channels, target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="target"></param>
        public void RfcNames(string[] channels, string target)
        {
            WriteLine(Rfc2812.Names(channels, target));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="priority"></param>
        public void RfcTopic(string channel, Priority priority)
        {
            WriteLine(Rfc2812.Topic(channel), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        public void RfcTopic(string channel)
        {
            WriteLine(Rfc2812.Topic(channel));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="newtopic"></param>
        /// <param name="priority"></param>
        public void RfcTopic(string channel, string newtopic, Priority priority)
        {
            WriteLine(Rfc2812.Topic(channel, newtopic), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="newtopic"></param>
        public void RfcTopic(string channel, string newtopic)
        {
            WriteLine(Rfc2812.Topic(channel, newtopic));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        public void RfcMode(string target, Priority priority)
        {
            WriteLine(Rfc2812.Mode(target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        public void RfcMode(string target)
        {
            WriteLine(Rfc2812.Mode(target));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="newmode"></param>
        /// <param name="priority"></param>
        public void RfcMode(string target, string newmode, Priority priority)
        {
            WriteLine(Rfc2812.Mode(target, newmode), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="newmode"></param>
        public void RfcMode(string target, string newmode)
        {
            WriteLine(Rfc2812.Mode(target, newmode));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="distribution"></param>
        /// <param name="info"></param>
        /// <param name="priority"></param>
        public void RfcService(string nickname, string distribution, string info, Priority priority)
        {
            WriteLine(Rfc2812.Service(nickname, distribution, info), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="distribution"></param>
        /// <param name="info"></param>
        public void RfcService(string nickname, string distribution, string info)
        {
            WriteLine(Rfc2812.Service(nickname, distribution, info));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="channel"></param>
        /// <param name="priority"></param>
        public void RfcInvite(string nickname, string channel, Priority priority)
        {
            WriteLine(Rfc2812.Invite(nickname, channel), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="channel"></param>
        public void RfcInvite(string nickname, string channel)
        {
            WriteLine(Rfc2812.Invite(nickname, channel));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newnickname"></param>
        /// <param name="priority"></param>
        public void RfcNick(string newnickname, Priority priority)
        {
            WriteLine(Rfc2812.Nick(newnickname), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newnickname"></param>
        public void RfcNick(string newnickname)
        {
            WriteLine(Rfc2812.Nick(newnickname));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        public void RfcWho(Priority priority)
        {
            WriteLine(Rfc2812.Who(), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RfcWho()
        {
            WriteLine(Rfc2812.Who());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="priority"></param>
        public void RfcWho(string mask, Priority priority)
        {
            WriteLine(Rfc2812.Who(mask), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask"></param>
        public void RfcWho(string mask)
        {
            WriteLine(Rfc2812.Who(mask));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="ircop"></param>
        /// <param name="priority"></param>
        public void RfcWho(string mask, bool ircop, Priority priority)
        {
            WriteLine(Rfc2812.Who(mask, ircop), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="ircop"></param>
        public void RfcWho(string mask, bool ircop)
        {
            WriteLine(Rfc2812.Who(mask, ircop));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="priority"></param>
        public void RfcWhois(string mask, Priority priority)
        {
            WriteLine(Rfc2812.Whois(mask), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mask"></param>
        public void RfcWhois(string mask)
        {
            WriteLine(Rfc2812.Whois(mask));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="masks"></param>
        /// <param name="priority"></param>
        public void RfcWhois(string[] masks, Priority priority)
        {
            WriteLine(Rfc2812.Whois(masks), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="masks"></param>
        public void RfcWhois(string[] masks)
        {
            WriteLine(Rfc2812.Whois(masks));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="mask"></param>
        /// <param name="priority"></param>
        public void RfcWhois(string target, string mask, Priority priority)
        {
            WriteLine(Rfc2812.Whois(target, mask), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="mask"></param>
        public void RfcWhois(string target, string mask)
        {
            WriteLine(Rfc2812.Whois(target, mask));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="masks"></param>
        /// <param name="priority"></param>
        public void RfcWhois(string target, string[] masks, Priority priority)
        {
            WriteLine(Rfc2812.Whois(target, masks), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="masks"></param>
        public void RfcWhois(string target, string[] masks)
        {
            WriteLine(Rfc2812.Whois(target, masks));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="priority"></param>
        public void RfcWhowas(string nickname, Priority priority)
        {
            WriteLine(Rfc2812.Whowas(nickname), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        public void RfcWhowas(string nickname)
        {
            WriteLine(Rfc2812.Whowas(nickname));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nicknames"></param>
        /// <param name="priority"></param>
        public void RfcWhowas(string[] nicknames, Priority priority)
        {
            WriteLine(Rfc2812.Whowas(nicknames), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nicknames"></param>
        public void RfcWhowas(string[] nicknames)
        {
            WriteLine(Rfc2812.Whowas(nicknames));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="count"></param>
        /// <param name="priority"></param>
        public void RfcWhowas(string nickname, string count, Priority priority)
        {
            WriteLine(Rfc2812.Whowas(nickname, count), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="count"></param>
        public void RfcWhowas(string nickname, string count)
        {
            WriteLine(Rfc2812.Whowas(nickname, count));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nicknames"></param>
        /// <param name="count"></param>
        /// <param name="priority"></param>
        public void RfcWhowas(string[] nicknames, string count, Priority priority)
        {
            WriteLine(Rfc2812.Whowas(nicknames, count), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nicknames"></param>
        /// <param name="count"></param>
        public void RfcWhowas(string[] nicknames, string count)
        {
            WriteLine(Rfc2812.Whowas(nicknames, count));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="count"></param>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        public void RfcWhowas(string nickname, string count, string target, Priority priority)
        {
            WriteLine(Rfc2812.Whowas(nickname, count, target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="count"></param>
        /// <param name="target"></param>
        public void RfcWhowas(string nickname, string count, string target)
        {
            WriteLine(Rfc2812.Whowas(nickname, count, target));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nicknames"></param>
        /// <param name="count"></param>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        public void RfcWhowas(string[] nicknames, string count, string target, Priority priority)
        {
            WriteLine(Rfc2812.Whowas(nicknames, count, target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nicknames"></param>
        /// <param name="count"></param>
        /// <param name="target"></param>
        public void RfcWhowas(string[] nicknames, string count, string target)
        {
            WriteLine(Rfc2812.Whowas(nicknames, count, target));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="comment"></param>
        /// <param name="priority"></param>
        public void RfcKill(string nickname, string comment, Priority priority)
        {
            WriteLine(Rfc2812.Kill(nickname, comment), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="comment"></param>
        public void RfcKill(string nickname, string comment)
        {
            WriteLine(Rfc2812.Kill(nickname, comment));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="priority"></param>
        public void RfcPing(string server, Priority priority)
        {
            WriteLine(Rfc2812.Ping(server), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="server"></param>
        public void RfcPing(string server)
        {
            WriteLine(Rfc2812.Ping(server));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="server2"></param>
        /// <param name="priority"></param>
        public void RfcPing(string server, string server2, Priority priority)
        {
            WriteLine(Rfc2812.Ping(server, server2), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="server2"></param>
        public void RfcPing(string server, string server2)
        {
            WriteLine(Rfc2812.Ping(server, server2));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="priority"></param>
        public void RfcPong(string server, Priority priority)
        {
            WriteLine(Rfc2812.Pong(server), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="server"></param>
        public void RfcPong(string server)
        {
            WriteLine(Rfc2812.Pong(server));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="server2"></param>
        /// <param name="priority"></param>
        public void RfcPong(string server, string server2, Priority priority)
        {
            WriteLine(Rfc2812.Pong(server, server2), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="server2"></param>
        public void RfcPong(string server, string server2)
        {
            WriteLine(Rfc2812.Pong(server, server2));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        public void RfcAway(Priority priority)
        {
            WriteLine(Rfc2812.Away(), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RfcAway()
        {
            WriteLine(Rfc2812.Away());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="awaytext"></param>
        /// <param name="priority"></param>
        public void RfcAway(string awaytext, Priority priority)
        {
            WriteLine(Rfc2812.Away(awaytext), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="awaytext"></param>
        public void RfcAway(string awaytext)
        {
            WriteLine(Rfc2812.Away(awaytext));
        }

        /// <summary>
        /// 
        /// </summary>
        public void RfcRehash()
        {
            WriteLine(Rfc2812.Rehash());
        }

        /// <summary>
        /// 
        /// </summary>
        public void RfcDie()
        {
            WriteLine(Rfc2812.Die());
        }

        /// <summary>
        /// 
        /// </summary>
        public void RfcRestart()
        {
            WriteLine(Rfc2812.Restart());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="priority"></param>
        public void RfcSummon(string user, Priority priority)
        {
            WriteLine(Rfc2812.Summon(user), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        public void RfcSummon(string user)
        {
            WriteLine(Rfc2812.Summon(user));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        public void RfcSummon(string user, string target, Priority priority)
        {
            WriteLine(Rfc2812.Summon(user, target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        public void RfcSummon(string user, string target)
        {
            WriteLine(Rfc2812.Summon(user, target));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="channel"></param>
        /// <param name="priority"></param>
        public void RfcSummon(string user, string target, string channel, Priority priority)
        {
            WriteLine(Rfc2812.Summon(user, target, channel), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="channel"></param>
        public void RfcSummon(string user, string target, string channel)
        {
            WriteLine(Rfc2812.Summon(user, target, channel));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        public void RfcUsers(Priority priority)
        {
            WriteLine(Rfc2812.Users(), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RfcUsers()
        {
            WriteLine(Rfc2812.Users());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="priority"></param>
        public void RfcUsers(string target, Priority priority)
        {
            WriteLine(Rfc2812.Users(target), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        public void RfcUsers(string target)
        {
            WriteLine(Rfc2812.Users(target));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="wallopstext"></param>
        /// <param name="priority"></param>
        public void RfcWallops(string wallopstext, Priority priority)
        {
            WriteLine(Rfc2812.Wallops(wallopstext), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="wallopstext"></param>
        public void RfcWallops(string wallopstext)
        {
            WriteLine(Rfc2812.Wallops(wallopstext));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="priority"></param>
        public void RfcUserhost(string nickname, Priority priority)
        {
            WriteLine(Rfc2812.Userhost(nickname), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        public void RfcUserhost(string nickname)
        {
            WriteLine(Rfc2812.Userhost(nickname));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nicknames"></param>
        /// <param name="priority"></param>
        public void RfcUserhost(string[] nicknames, Priority priority)
        {
            WriteLine(Rfc2812.Userhost(nicknames), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nicknames"></param>
        public void RfcUserhost(string[] nicknames)
        {
            WriteLine(Rfc2812.Userhost(nicknames));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        /// <param name="priority"></param>
        public void RfcIson(string nickname, Priority priority)
        {
            WriteLine(Rfc2812.Ison(nickname), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nickname"></param>
        public void RfcIson(string nickname)
        {
            WriteLine(Rfc2812.Ison(nickname));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nicknames"></param>
        /// <param name="priority"></param>
        public void RfcIson(string[] nicknames, Priority priority)
        {
            WriteLine(Rfc2812.Ison(nicknames), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nicknames"></param>
        public void RfcIson(string[] nicknames)
        {
            WriteLine(Rfc2812.Ison(nicknames));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priority"></param>
        public void RfcQuit(Priority priority)
        {
            WriteLine(Rfc2812.Quit(), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        public void RfcQuit()
        {
            WriteLine(Rfc2812.Quit());
        }

        public void RfcQuit(string quitmessage, Priority priority)
        {
            WriteLine(Rfc2812.Quit(quitmessage), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="quitmessage"></param>
        public void RfcQuit(string quitmessage)
        {
            WriteLine(Rfc2812.Quit(quitmessage));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="comment"></param>
        /// <param name="priority"></param>
        public void RfcSquit(string server, string comment, Priority priority)
        {
            WriteLine(Rfc2812.Squit(server, comment), priority);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="comment"></param>
        public void RfcSquit(string server, string comment)
        {
            WriteLine(Rfc2812.Squit(server, comment));
        }
        #endregion
    }
}
namespace Meebey.SmartIrc4net
{
    /// <summary>
    ///
    /// </summary>
    /// <threadsafety static="true" instance="true" />
    public sealed class Rfc2812
    {
        // nickname   =  ( letter / special ) *8( letter / digit / special / "-" )
        // letter     =  %x41-5A / %x61-7A       ; A-Z / a-z
        // digit      =  %x30-39                 ; 0-9
        // special    =  %x5B-60 / %x7B-7D
        //                  ; "[", "]", "\", "`", "_", "^", "{", "|", "}"
        private static Regex _NicknameRegex = new Regex(@"^[A-Za-z\[\]\\`_^{|}][A-Za-z0-9\[\]\\`_\-^{|}]+$", RegexOptions.Compiled);

        private Rfc2812()
        {
        }

        /// <summary>
        /// Checks if the passed nickname is valid according to the RFC
        ///
        /// Use with caution, many IRC servers are not conform with this!
        /// </summary>
        public static bool IsValidNickname(string nickname)
        {
            if ((nickname != null) &&
                (nickname.Length > 0) &&
                (_NicknameRegex.Match(nickname).Success))
            {
                return true;
            }

            return false;
        }

        public static string Pass(string password)
        {
            return "PASS " + password;
        }

        public static string Nick(string nickname)
        {
            return "NICK " + nickname;
        }

        public static string User(string username, int usermode, string realname)
        {
            return "USER " + username + " " + usermode.ToString() + " * :" + realname;
        }

        public static string Oper(string name, string password)
        {
            return "OPER " + name + " " + password;
        }

        public static string Privmsg(string destination, string message)
        {
            return "PRIVMSG " + destination + " :" + message;
        }

        public static string Notice(string destination, string message)
        {
            return "NOTICE " + destination + " :" + message;
        }

        public static string Join(string channel)
        {
            return "JOIN " + channel;
        }

        public static string Join(string[] channels)
        {
            string channellist = String.Join(",", channels);
            return "JOIN " + channellist;
        }

        public static string Join(string channel, string key)
        {
            return "JOIN " + channel + " " + key;
        }

        public static string Join(string[] channels, string[] keys)
        {
            string channellist = String.Join(",", channels);
            string keylist = String.Join(",", keys);
            return "JOIN " + channellist + " " + keylist;
        }

        public static string Part(string channel)
        {
            return "PART " + channel;
        }

        public static string Part(string[] channels)
        {
            string channellist = String.Join(",", channels);
            return "PART " + channellist;
        }

        public static string Part(string channel, string partmessage)
        {
            return "PART " + channel + " :" + partmessage;
        }

        public static string Part(string[] channels, string partmessage)
        {
            string channellist = String.Join(",", channels);
            return "PART " + channellist + " :" + partmessage;
        }

        public static string Kick(string channel, string nickname)
        {
            return "KICK " + channel + " " + nickname;
        }

        public static string Kick(string channel, string nickname, string comment)
        {
            return "KICK " + channel + " " + nickname + " :" + comment;
        }

        public static string Kick(string[] channels, string nickname)
        {
            string channellist = String.Join(",", channels);
            return "KICK " + channellist + " " + nickname;
        }

        public static string Kick(string[] channels, string nickname, string comment)
        {
            string channellist = String.Join(",", channels);
            return "KICK " + channellist + " " + nickname + " :" + comment;
        }

        public static string Kick(string channel, string[] nicknames)
        {
            string nicknamelist = String.Join(",", nicknames);
            return "KICK " + channel + " " + nicknamelist;
        }

        public static string Kick(string channel, string[] nicknames, string comment)
        {
            string nicknamelist = String.Join(",", nicknames);
            return "KICK " + channel + " " + nicknamelist + " :" + comment;
        }

        public static string Kick(string[] channels, string[] nicknames)
        {
            string channellist = String.Join(",", channels);
            string nicknamelist = String.Join(",", nicknames);
            return "KICK " + channellist + " " + nicknamelist;
        }

        public static string Kick(string[] channels, string[] nicknames, string comment)
        {
            string channellist = String.Join(",", channels);
            string nicknamelist = String.Join(",", nicknames);
            return "KICK " + channellist + " " + nicknamelist + " :" + comment;
        }

        public static string Motd()
        {
            return "MOTD";
        }

        public static string Motd(string target)
        {
            return "MOTD " + target;
        }

        [Obsolete("use Lusers() method instead")]
        public static string Luser()
        {
            return Lusers();
        }

        public static string Lusers()
        {
            return "LUSERS";
        }

        [Obsolete("use Lusers(string) method instead")]
        public static string Luser(string mask)
        {
            return Lusers(mask);
        }

        public static string Lusers(string mask)
        {
            return "LUSER " + mask;
        }

        [Obsolete("use Lusers(string, string) method instead")]
        public static string Luser(string mask, string target)
        {
            return Lusers(mask, target);
        }

        public static string Lusers(string mask, string target)
        {
            return "LUSER " + mask + " " + target;
        }

        public static string Version()
        {
            return "VERSION";
        }

        public static string Version(string target)
        {
            return "VERSION " + target;
        }

        public static string Stats()
        {
            return "STATS";
        }

        public static string Stats(string query)
        {
            return "STATS " + query;
        }

        public static string Stats(string query, string target)
        {
            return "STATS " + query + " " + target;
        }

        public static string Links()
        {
            return "LINKS";
        }

        public static string Links(string servermask)
        {
            return "LINKS " + servermask;
        }

        public static string Links(string remoteserver, string servermask)
        {
            return "LINKS " + remoteserver + " " + servermask;
        }

        public static string Time()
        {
            return "TIME";
        }

        public static string Time(string target)
        {
            return "TIME " + target;
        }

        public static string Connect(string targetserver, string port)
        {
            return "CONNECT " + targetserver + " " + port;
        }

        public static string Connect(string targetserver, string port, string remoteserver)
        {
            return "CONNECT " + targetserver + " " + port + " " + remoteserver;
        }

        public static string Trace()
        {
            return "TRACE";
        }

        public static string Trace(string target)
        {
            return "TRACE " + target;
        }

        public static string Admin()
        {
            return "ADMIN";
        }

        public static string Admin(string target)
        {
            return "ADMIN " + target;
        }

        public static string Info()
        {
            return "INFO";
        }

        public static string Info(string target)
        {
            return "INFO " + target;
        }

        public static string Servlist()
        {
            return "SERVLIST";
        }

        public static string Servlist(string mask)
        {
            return "SERVLIST " + mask;
        }

        public static string Servlist(string mask, string type)
        {
            return "SERVLIST " + mask + " " + type;
        }

        public static string Squery(string servicename, string servicetext)
        {
            return "SQUERY " + servicename + " :" + servicetext;
        }

        public static string List()
        {
            return "LIST";
        }

        public static string List(string channel)
        {
            return "LIST " + channel;
        }

        public static string List(string[] channels)
        {
            string channellist = String.Join(",", channels);
            return "LIST " + channellist;
        }

        public static string List(string channel, string target)
        {
            return "LIST " + channel + " " + target;
        }

        public static string List(string[] channels, string target)
        {
            string channellist = String.Join(",", channels);
            return "LIST " + channellist + " " + target;
        }

        public static string Names()
        {
            return "NAMES";
        }

        public static string Names(string channel)
        {
            return "NAMES " + channel;
        }

        public static string Names(string[] channels)
        {
            string channellist = String.Join(",", channels);
            return "NAMES " + channellist;
        }

        public static string Names(string channel, string target)
        {
            return "NAMES " + channel + " " + target;
        }

        public static string Names(string[] channels, string target)
        {
            string channellist = String.Join(",", channels);
            return "NAMES " + channellist + " " + target;
        }

        public static string Topic(string channel)
        {
            return "TOPIC " + channel;
        }

        public static string Topic(string channel, string newtopic)
        {
            return "TOPIC " + channel + " :" + newtopic;
        }

        public static string Mode(string target)
        {
            return "MODE " + target;
        }

        public static string Mode(string target, string newmode)
        {
            return "MODE " + target + " " + newmode;
        }

        /*
        public static string Mode(string target, string[] newModes, string[] newModeParameters)
        {
            if (newModes == null) {
                throw new ArgumentNullException("newModes");
            }
            if (newModeParameters == null) {
                throw new ArgumentNullException("newModeParameters");
            }
            if (newModes.Length != newModeParameters.Length) {
                throw new ArgumentException("newModes and modeParameters must have the same size.");
            }
            
            StringBuilder newMode = new StringBuilder(newModes.Length);
            StringBuilder newModeParameter = new StringBuilder();
            // as per RFC 3.2.3, maximum is 3 modes changes at once
            int maxModeChanges = 3;
            if (newModes.Length > maxModeChanges) {
                throw new ArgumentOutOfRangeException(
                    "newModes.Length",
                    newModes.Length,
                    String.Format("Mode change list is too large (> {0}).", maxModeChanges)
                );
            }
            
            for (int i = 0; i < newModes.Length; i += maxModeChanges) {
                for (int j = 0; j < maxModeChanges; j++) {
                    newMode.Append(newModes[i + j]);
                }
                
                for (int j = 0; j < maxModeChanges; j++) {
                    newModeParameter.Append(newModeParameters[i + j]);
                }
            }
            newMode.Append(" ");
            newMode.Append(newModeParameter.ToString());
            
            return Mode(target, newMode.ToString());
        }
        */

        public static string Service(string nickname, string distribution, string info)
        {
            return "SERVICE " + nickname + " * " + distribution + " * * :" + info;
        }

        public static string Invite(string nickname, string channel)
        {
            return "INVITE " + nickname + " " + channel;
        }

        public static string Who()
        {
            return "WHO";
        }

        public static string Who(string mask)
        {
            return "WHO " + mask;
        }

        public static string Who(string mask, bool ircop)
        {
            if (ircop)
            {
                return "WHO " + mask + " o";
            }
            else
            {
                return "WHO " + mask;
            }
        }

        public static string Whois(string mask)
        {
            return "WHOIS " + mask;
        }

        public static string Whois(string[] masks)
        {
            string masklist = String.Join(",", masks);
            return "WHOIS " + masklist;
        }

        public static string Whois(string target, string mask)
        {
            return "WHOIS " + target + " " + mask;
        }

        public static string Whois(string target, string[] masks)
        {
            string masklist = String.Join(",", masks);
            return "WHOIS " + target + " " + masklist;
        }

        public static string Whowas(string nickname)
        {
            return "WHOWAS " + nickname;
        }

        public static string Whowas(string[] nicknames)
        {
            string nicknamelist = String.Join(",", nicknames);
            return "WHOWAS " + nicknamelist;
        }

        public static string Whowas(string nickname, string count)
        {
            return "WHOWAS " + nickname + " " + count + " ";
        }

        public static string Whowas(string[] nicknames, string count)
        {
            string nicknamelist = String.Join(",", nicknames);
            return "WHOWAS " + nicknamelist + " " + count + " ";
        }

        public static string Whowas(string nickname, string count, string target)
        {
            return "WHOWAS " + nickname + " " + count + " " + target;
        }

        public static string Whowas(string[] nicknames, string count, string target)
        {
            string nicknamelist = String.Join(",", nicknames);
            return "WHOWAS " + nicknamelist + " " + count + " " + target;
        }

        public static string Kill(string nickname, string comment)
        {
            return "KILL " + nickname + " :" + comment;
        }

        public static string Ping(string server)
        {
            return "PING " + server;
        }

        public static string Ping(string server, string server2)
        {
            return "PING " + server + " " + server2;
        }

        public static string Pong(string server)
        {
            return "PONG " + server;
        }

        public static string Pong(string server, string server2)
        {
            return "PONG " + server + " " + server2;
        }

        public static string Error(string errormessage)
        {
            return "ERROR :" + errormessage;
        }

        public static string Away()
        {
            return "AWAY";
        }

        public static string Away(string awaytext)
        {
            return "AWAY :" + awaytext;
        }

        public static string Rehash()
        {
            return "REHASH";
        }

        public static string Die()
        {
            return "DIE";
        }

        public static string Restart()
        {
            return "RESTART";
        }

        public static string Summon(string user)
        {
            return "SUMMON " + user;
        }

        public static string Summon(string user, string target)
        {
            return "SUMMON " + user + " " + target;
        }

        public static string Summon(string user, string target, string channel)
        {
            return "SUMMON " + user + " " + target + " " + channel;
        }

        public static string Users()
        {
            return "USERS";
        }

        public static string Users(string target)
        {
            return "USERS " + target;
        }

        public static string Wallops(string wallopstext)
        {
            return "WALLOPS :" + wallopstext;
        }

        public static string Userhost(string nickname)
        {
            return "USERHOST " + nickname;
        }

        public static string Userhost(string[] nicknames)
        {
            string nicknamelist = String.Join(" ", nicknames);
            return "USERHOST " + nicknamelist;
        }

        public static string Ison(string nickname)
        {
            return "ISON " + nickname;
        }

        public static string Ison(string[] nicknames)
        {
            string nicknamelist = String.Join(" ", nicknames);
            return "ISON " + nicknamelist;
        }

        public static string Quit()
        {
            return "QUIT";
        }

        public static string Quit(string quitmessage)
        {
            return "QUIT :" + quitmessage;
        }

        public static string Squit(string server, string comment)
        {
            return "SQUIT " + server + " :" + comment;
        }
    }
}
namespace Meebey.SmartIrc4net
{
    public delegate void ReadLineEventHandler(object sender, ReadLineEventArgs e);
    public delegate void WriteLineEventHandler(object sender, WriteLineEventArgs e);
    public delegate void AutoConnectErrorEventHandler(object sender, AutoConnectErrorEventArgs e);
}
namespace Meebey.SmartIrc4net
{
    /// <summary>
    ///
    /// </summary>
    public class ReadLineEventArgs : EventArgs
    {
        private string _Line;

        public string Line
        {
            get
            {
                return _Line;
            }
        }

        internal ReadLineEventArgs(string line)
        {
            _Line = line;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class WriteLineEventArgs : EventArgs
    {
        private string _Line;

        public string Line
        {
            get
            {
                return _Line;
            }
        }

        internal WriteLineEventArgs(string line)
        {
            _Line = line;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class AutoConnectErrorEventArgs : EventArgs
    {
        private Exception _Exception;
        private string _Address;
        private int _Port;

        public Exception Exception
        {
            get
            {
                return _Exception;
            }
        }

        public string Address
        {
            get
            {
                return _Address;
            }
        }

        public int Port
        {
            get
            {
                return _Port;
            }
        }

        internal AutoConnectErrorEventArgs(string address, int port, Exception ex)
        {
            _Address = address;
            _Port = port;
            _Exception = ex;
        }
    }
}
namespace Meebey.SmartIrc4net
{
    /// <summary>
    /// 
    /// </summary>
    /// <threadsafety static="true" instance="true" />
    public class IrcConnection
    {
        private string _VersionNumber;
        private string _VersionString;
        private string[] _AddressList = { "localhost" };
        private int _CurrentAddress;
        private int _Port;
#if NET_2_0        
        private bool             _UseSsl;
#endif
        private StreamReader _Reader;
        private StreamWriter _Writer;
        private ReadThread _ReadThread;
        private WriteThread _WriteThread;
        private IdleWorkerThread _IdleWorkerThread;
        private IrcTcpClient _TcpClient;
        private Hashtable _SendBuffer = Hashtable.Synchronized(new Hashtable());
        private int _SendDelay = 200;
        private bool _IsRegistered;
        private bool _IsConnected;
        private bool _IsConnectionError;
        private bool _IsDisconnecting;
        private int _ConnectTries;
        private bool _AutoRetry;
        private int _AutoRetryDelay = 30;
        private bool _AutoReconnect;
        private Encoding _Encoding = Encoding.Default;
        private int _SocketReceiveTimeout = 600;
        private int _SocketSendTimeout = 600;
        private int _IdleWorkerInterval = 60;
        private int _PingInterval = 60;
        private int _PingTimeout = 300;
        private DateTime _LastPingSent;
        private DateTime _LastPongReceived;
        private TimeSpan _Lag;

        /// <event cref="OnReadLine">
        /// Raised when a \r\n terminated line is read from the socket
        /// </event>
        public event ReadLineEventHandler OnReadLine;
        /// <event cref="OnWriteLine">
        /// Raised when a \r\n terminated line is written to the socket
        /// </event>
        public event WriteLineEventHandler OnWriteLine;
        /// <event cref="OnConnect">
        /// Raised before the connect attempt
        /// </event>
        public event EventHandler OnConnecting;
        /// <event cref="OnConnect">
        /// Raised on successful connect
        /// </event>
        public event EventHandler OnConnected;
        /// <event cref="OnConnect">
        /// Raised before the connection is closed
        /// </event>
        public event EventHandler OnDisconnecting;
        /// <event cref="OnConnect">
        /// Raised when the connection is closed
        /// </event>
        public event EventHandler OnDisconnected;
        /// <event cref="OnConnectionError">
        /// Raised when the connection got into an error state
        /// </event>
        public event EventHandler OnConnectionError;
        /// <event cref="AutoConnectErrorEventHandler">
        /// Raised when the connection got into an error state during auto connect loop
        /// </event>
        public event AutoConnectErrorEventHandler OnAutoConnectError;

        /// <summary>
        /// When a connection error is detected this property will return true
        /// </summary>
        protected bool IsConnectionError
        {
            get
            {
                lock (this)
                {
                    return _IsConnectionError;
                }
            }
            set
            {
                lock (this)
                {
                    _IsConnectionError = value;
                }
            }
        }

        protected bool IsDisconnecting
        {
            get
            {
                lock (this)
                {
                    return _IsDisconnecting;
                }
            }
            set
            {
                lock (this)
                {
                    _IsDisconnecting = value;
                }
            }
        }

        /// <summary>
        /// Gets the current address of the connection
        /// </summary>
        public string Address
        {
            get
            {
                return _AddressList[_CurrentAddress];
            }
        }

        /// <summary>
        /// Gets the address list of the connection
        /// </summary>
        public string[] AddressList
        {
            get
            {
                return _AddressList;
            }
        }

        /// <summary>
        /// Gets the used port of the connection
        /// </summary>
        public int Port
        {
            get
            {
                return _Port;
            }
        }

        /// <summary>
        /// By default nothing is done when the library looses the connection
        /// to the server.
        /// Default: false
        /// </summary>
        /// <value>
        /// true, if the library should reconnect on lost connections
        /// false, if the library should not take care of it
        /// </value>
        public bool AutoReconnect
        {
            get
            {
                return _AutoReconnect;
            }
            set
            {
#if LOG4NET
                if (value) {
                    Logger.Connection.Info("AutoReconnect enabled");
                } else {
                    Logger.Connection.Info("AutoReconnect disabled");
                }
#endif
                _AutoReconnect = value;
            }
        }

        /// <summary>
        /// If the library should retry to connect when the connection fails.
        /// Default: false
        /// </summary>
        /// <value>
        /// true, if the library should retry to connect
        /// false, if the library should not retry
        /// </value>
        public bool AutoRetry
        {
            get
            {
                return _AutoRetry;
            }
            set
            {
#if LOG4NET
                if (value) {
                    Logger.Connection.Info("AutoRetry enabled");
                } else {
                    Logger.Connection.Info("AutoRetry disabled");
                }
#endif
                _AutoRetry = value;
            }
        }

        /// <summary>
        /// Delay between retry attempts in Connect() in seconds.
        /// Default: 30
        /// </summary>
        public int AutoRetryDelay
        {
            get
            {
                return _AutoRetryDelay;
            }
            set
            {
                _AutoRetryDelay = value;
            }
        }

        /// <summary>
        /// To prevent flooding the IRC server, it's required to delay each
        /// message, given in milliseconds.
        /// Default: 200
        /// </summary>
        public int SendDelay
        {
            get
            {
                return _SendDelay;
            }
            set
            {
                _SendDelay = value;
            }
        }

        /// <summary>
        /// On successful registration on the IRC network, this is set to true.
        /// </summary>
        public bool IsRegistered
        {
            get
            {
                return _IsRegistered;
            }
        }

        /// <summary>
        /// On successful connect to the IRC server, this is set to true.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return _IsConnected;
            }
        }

        /// <summary>
        /// Gets the SmartIrc4net version number
        /// </summary>
        public string VersionNumber
        {
            get
            {
                return _VersionNumber;
            }
        }

        /// <summary>
        /// Gets the full SmartIrc4net version string
        /// </summary>
        public string VersionString
        {
            get
            {
                return _VersionString;
            }
        }

        /// <summary>
        /// Encoding which is used for reading and writing to the socket
        /// Default: encoding of the system
        /// </summary>
        public Encoding Encoding
        {
            get
            {
                return _Encoding;
            }
            set
            {
                _Encoding = value;
            }
        }

#if NET_2_0        
        /// <summary>
        /// Enables/disables using SSL for the connection
        /// Default: false
        /// </summary>
        public bool UseSsl {
            get {
                return _UseSsl;
            }
            set {
                _UseSsl = value;
            }
        }
#endif

        /// <summary>
        /// Timeout in seconds for receiving data from the socket
        /// Default: 600
        /// </summary>
        public int SocketReceiveTimeout
        {
            get
            {
                return _SocketReceiveTimeout;
            }
            set
            {
                _SocketReceiveTimeout = value;
            }
        }

        /// <summary>
        /// Timeout in seconds for sending data to the socket
        /// Default: 600
        /// </summary>
        public int SocketSendTimeout
        {
            get
            {
                return _SocketSendTimeout;
            }
            set
            {
                _SocketSendTimeout = value;
            }
        }

        /// <summary>
        /// Interval in seconds to run the idle worker
        /// Default: 60
        /// </summary>
        public int IdleWorkerInterval
        {
            get
            {
                return _IdleWorkerInterval;
            }
            set
            {
                _IdleWorkerInterval = value;
            }
        }

        /// <summary>
        /// Interval in seconds to send a PING
        /// Default: 60
        /// </summary>
        public int PingInterval
        {
            get
            {
                return _PingInterval;
            }
            set
            {
                _PingInterval = value;
            }
        }

        /// <summary>
        /// Timeout in seconds for server response to a PING
        /// Default: 600
        /// </summary>
        public int PingTimeout
        {
            get
            {
                return _PingTimeout;
            }
            set
            {
                _PingTimeout = value;
            }
        }

        /// <summary>
        /// Latency between client and the server
        /// </summary>
        public TimeSpan Lag
        {
            get
            {
                if (_LastPingSent > _LastPongReceived)
                {
                    // there is an outstanding ping, thus we don't have a current lag value
                    return DateTime.Now - _LastPingSent;
                }

                return _Lag;
            }
        }

        /// <summary>
        /// Initializes the message queues, read and write thread
        /// </summary>
        public IrcConnection()
        {
#if LOG4NET
            Logger.Init();
            Logger.Main.Debug("IrcConnection created");
#endif
            _SendBuffer[Priority.High] = Queue.Synchronized(new Queue());
            _SendBuffer[Priority.AboveMedium] = Queue.Synchronized(new Queue());
            _SendBuffer[Priority.Medium] = Queue.Synchronized(new Queue());
            _SendBuffer[Priority.BelowMedium] = Queue.Synchronized(new Queue());
            _SendBuffer[Priority.Low] = Queue.Synchronized(new Queue());

            // setup own callbacks
            OnReadLine += new ReadLineEventHandler(_SimpleParser);
            OnConnectionError += new EventHandler(_OnConnectionError);

            _ReadThread = new ReadThread(this);
            _WriteThread = new WriteThread(this);
            _IdleWorkerThread = new IdleWorkerThread(this);

            //Assembly assm = Assembly.GetAssembly(this.GetType());
            //AssemblyName assm_name = assm.GetName(false);

            //AssemblyProductAttribute pr = (AssemblyProductAttribute)assm.GetCustomAttributes(typeof(AssemblyProductAttribute), false)[0];

            //_VersionNumber = assm_name.Version.ToString();
            //_VersionString = pr.Product+" "+_VersionNumber;
        }

#if LOG4NET
        ~IrcConnection()
        {
            Logger.Main.Debug("IrcConnection destroyed");
        }
#endif

        /// <overloads>this method has 2 overloads</overloads>
        /// <summary>
        /// Connects to the specified server and port, when the connection fails
        /// the next server in the list will be used.
        /// </summary>
        /// <param name="addresslist">List of servers to connect to</param>
        /// <param name="port">Portnumber to connect to</param>
        /// <exception cref="CouldNotConnectException">The connection failed</exception>
        /// <exception cref="AlreadyConnectedException">If there is already an active connection</exception>
        public void Connect(string[] addresslist, int port)
        {
            if (_IsConnected)
            {
                throw new AlreadyConnectedException("Already connected to: " + Address + ":" + Port);
            }

            _ConnectTries++;
#if LOG4NET
            Logger.Connection.Info(String.Format("connecting... (attempt: {0})",
                                                 _ConnectTries));
#endif
            _AddressList = (string[])addresslist.Clone();
            _Port = port;

            if (OnConnecting != null)
            {
                OnConnecting(this, EventArgs.Empty);
            }
            try
            {
                System.Net.IPAddress ip = System.Net.Dns.Resolve(Address).AddressList[0];
                _TcpClient = new IrcTcpClient();
                _TcpClient.NoDelay = true;
                _TcpClient.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                // set timeout, after this the connection will be aborted
                _TcpClient.ReceiveTimeout = _SocketReceiveTimeout * 1000;
                _TcpClient.SendTimeout = _SocketSendTimeout * 1000;
                _TcpClient.Connect(ip, port);

                Stream stream = _TcpClient.GetStream();
#if NET_2_0
                if (_UseSsl) {
                    SslStream sslStream = new SslStream(stream, false, delegate {
                        return true;
                    });
                    sslStream.AuthenticateAsClient(Address);
                    stream = sslStream;
                }
#endif
                _Reader = new StreamReader(stream, _Encoding);
                _Writer = new StreamWriter(stream, _Encoding);

                if (_Encoding.GetPreamble().Length > 0)
                {
                    // HACK: we have an encoding that has some kind of preamble
                    // like UTF-8 has a BOM, this will confuse the IRCd!
                    // Thus we send a \r\n so the IRCd can safely ignore that
                    // garbage.
                    _Writer.WriteLine();
                }

                // Connection was succeful, reseting the connect counter
                _ConnectTries = 0;

                // updating the connection error state, so connecting is possible again
                IsConnectionError = false;
                _IsConnected = true;

                // lets power up our threads
                _ReadThread.Start();
                _WriteThread.Start();
                _IdleWorkerThread.Start();

#if LOG4NET
                Logger.Connection.Info("connected");
#endif
                if (OnConnected != null)
                {
                    OnConnected(this, EventArgs.Empty);
                }
            }
            catch (Exception e)
            {
                if (_Reader != null)
                {
                    try
                    {
                        _Reader.Close();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
                if (_Writer != null)
                {
                    try
                    {
                        _Writer.Close();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
                if (_TcpClient != null)
                {
                    _TcpClient.Close();
                }
                _IsConnected = false;
                IsConnectionError = true;

#if LOG4NET
                Logger.Connection.Info("connection failed: "+e.Message);
#endif
                if (_AutoRetry &&
                    _ConnectTries <= 3)
                {
                    if (OnAutoConnectError != null)
                    {
                        OnAutoConnectError(this, new AutoConnectErrorEventArgs(Address, Port, e));
                    }
#if LOG4NET
                    Logger.Connection.Debug("delaying new connect attempt for "+_AutoRetryDelay+" sec");
#endif
                    Thread.Sleep(_AutoRetryDelay * 1000);
                    _NextAddress();
                    Connect(_AddressList, _Port);
                }
                else
                {
                    throw new CouldNotConnectException("Could not connect to: " + Address + ":" + Port + " " + e.Message, e);
                }
            }
        }

        /// <summary>
        /// Connects to the specified server and port.
        /// </summary>
        /// <param name="address">Server address to connect to</param>
        /// <param name="port">Port number to connect to</param>
        public void Connect(string address, int port)
        {
            Connect(new string[] { address }, port);
        }

        /// <summary>
        /// Reconnects to the server
        /// </summary>
        /// <exception cref="NotConnectedException">
        /// If there was no active connection
        /// </exception>
        /// <exception cref="CouldNotConnectException">
        /// The connection failed
        /// </exception>
        /// <exception cref="AlreadyConnectedException">
        /// If there is already an active connection
        /// </exception>
        public void Reconnect()
        {
#if LOG4NET
            Logger.Connection.Info("reconnecting...");
#endif
            Disconnect();
            Connect(_AddressList, _Port);
        }

        /// <summary>
        /// Disconnects from the server
        /// </summary>
        /// <exception cref="NotConnectedException">
        /// If there was no active connection
        /// </exception>
        public void Disconnect()
        {
            if (!IsConnected)
            {
                throw new NotConnectedException("The connection could not be disconnected because there is no active connection");
            }

#if LOG4NET
            Logger.Connection.Info("disconnecting...");
#endif
            if (OnDisconnecting != null)
            {
                OnDisconnecting(this, EventArgs.Empty);
            }

            IsDisconnecting = true;

            _ReadThread.Stop();
            _WriteThread.Stop();
            _TcpClient.Close();
            _IsConnected = false;
            _IsRegistered = false;

            IsDisconnecting = false;

            if (OnDisconnected != null)
            {
                OnDisconnected(this, EventArgs.Empty);
            }

#if LOG4NET
            Logger.Connection.Info("disconnected");
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blocking"></param>
        public void Listen(bool blocking)
        {
            if (blocking)
            {
                while (IsConnected)
                {
                    ReadLine(true);
                }
            }
            else
            {
                while (ReadLine(false).Length > 0)
                {
                    // loop as long as we receive messages
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Listen()
        {
            Listen(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blocking"></param>
        public void ListenOnce(bool blocking)
        {
            ReadLine(blocking);
        }

        /// <summary>
        /// 
        /// </summary>
        public void ListenOnce()
        {
            ListenOnce(true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blocking"></param>
        /// <returns></returns>
        public string ReadLine(bool blocking)
        {
            string data = "";
            if (blocking)
            {
                // block till the queue has data, but bail out on connection error
                while (IsConnected &&
                       !IsConnectionError &&
                       _ReadThread.Queue.Count == 0)
                {
                    Thread.Sleep(10);
                }
            }

            if (IsConnected &&
                _ReadThread.Queue.Count > 0)
            {
                data = (string)(_ReadThread.Queue.Dequeue());
            }

            if (data != null && data.Length > 0)
            {
#if LOG4NET
                Logger.Queue.Debug("read: \""+data+"\"");
#endif
                if (OnReadLine != null)
                {
                    OnReadLine(this, new ReadLineEventArgs(data));
                }
            }

            if (IsConnectionError &&
                !IsDisconnecting &&
                OnConnectionError != null)
            {
                OnConnectionError(this, EventArgs.Empty);
            }

            return data;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="priority"></param>
        public void WriteLine(string data, Priority priority)
        {
            if (priority == Priority.Critical)
            {
                if (!IsConnected)
                {
                    throw new NotConnectedException();
                }

                _WriteLine(data);
            }
            else
            {
                ((Queue)_SendBuffer[priority]).Enqueue(data);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void WriteLine(string data)
        {
            WriteLine(data, Priority.Medium);
        }

        private bool _WriteLine(string data)
        {
            if (IsConnected)
            {
                try
                {
                    _Writer.Write(data + "\r\n");
                    _Writer.Flush();
                }
                catch (IOException)
                {
#if LOG4NET
                    Logger.Socket.Warn("sending data failed, connection lost");
#endif
                    IsConnectionError = true;
                    return false;
                }
                catch (ObjectDisposedException)
                {
#if LOG4NET
                    Logger.Socket.Warn("sending data failed (stream error), connection lost");
#endif
                    IsConnectionError = true;
                    return false;
                }

#if LOG4NET
                Logger.Socket.Debug("sent: \""+data+"\"");
#endif
                if (OnWriteLine != null)
                {
                    OnWriteLine(this, new WriteLineEventArgs(data));
                }
                return true;
            }

            return false;
        }

        private void _NextAddress()
        {
            _CurrentAddress++;
            if (_CurrentAddress >= _AddressList.Length)
            {
                _CurrentAddress = 0;
            }
#if LOG4NET
            Logger.Connection.Info("set server to: "+Address);
#endif
        }

        private void _SimpleParser(object sender, ReadLineEventArgs args)
        {
            string rawline = args.Line;
            string[] rawlineex = rawline.Split(new char[] { ' ' });
            string messagecode = "";

            if (rawline[0] == ':')
            {
                messagecode = rawlineex[1];

                ReplyCode replycode = ReplyCode.Null;
                try
                {
                    replycode = (ReplyCode)int.Parse(messagecode);
                }
                catch (FormatException)
                {
                }

                if (replycode != ReplyCode.Null)
                {
                    switch (replycode)
                    {
                        case ReplyCode.Welcome:
                            _IsRegistered = true;
#if LOG4NET
                            Logger.Connection.Info("logged in");
#endif
                            break;
                    }
                }
                else
                {
                    switch (rawlineex[1])
                    {
                        case "PONG":
                            DateTime now = DateTime.Now;
                            _LastPongReceived = now;
                            _Lag = now - _LastPingSent;

#if LOG4NET
                            Logger.Connection.Debug("PONG received, took: " + _Lag.TotalMilliseconds + " ms");
#endif
                            break;
                    }
                }
            }
            else
            {
                messagecode = rawlineex[0];
                switch (messagecode)
                {
                    case "ERROR":
                        // FIXME: handle server errors differently than connection errors!
                        //IsConnectionError = true;
                        break;
                }
            }
        }

        private void _OnConnectionError(object sender, EventArgs e)
        {
            try
            {
                if (AutoReconnect)
                {
                    // lets try to recover the connection
                    Reconnect();
                }
                else
                {
                    // make sure we clean up
                    Disconnect();
                }
            }
            catch (ConnectionException)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private class ReadThread
        {
#if LOG4NET
            private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
            private IrcConnection _Connection;
            private Thread _Thread;
            private Queue _Queue = Queue.Synchronized(new Queue());

            public Queue Queue
            {
                get
                {
                    return _Queue;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="connection"></param>
            public ReadThread(IrcConnection connection)
            {
                _Connection = connection;
            }

            /// <summary>
            /// 
            /// </summary>
            public void Start()
            {
                _Thread = new Thread(new ThreadStart(_Worker));
                _Thread.Name = "ReadThread (" + _Connection.Address + ":" + _Connection.Port + ")";
                _Thread.IsBackground = true;
                _Thread.Start();
            }

            /// <summary>
            /// 
            /// </summary>
            public void Stop()
            {
#if LOG4NET
                _Logger.Debug("Stop()");
#endif

#if LOG4NET
                _Logger.Debug("Stop(): aborting thread...");
#endif
                _Thread.Abort();
                // make sure we close the stream after the thread is gone, else
                // the thread will think the connection is broken!
#if LOG4NET
                _Logger.Debug("Stop(): joining thread...");
#endif
                _Thread.Join();

#if LOG4NET
                _Logger.Debug("Stop(): closing reader...");
#endif
                try
                {
                    _Connection._Reader.Close();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            private void _Worker()
            {
#if LOG4NET
                Logger.Socket.Debug("ReadThread started");
#endif
                try
                {
                    string data = "";
                    try
                    {
                        while (_Connection.IsConnected &&
                               ((data = _Connection._Reader.ReadLine()) != null))
                        {
                            _Queue.Enqueue(data);
#if LOG4NET
                            Logger.Socket.Debug("received: \""+data+"\"");
#endif
                        }
                    }
                    catch (IOException e)
                    {
#if LOG4NET
                        Logger.Socket.Warn("IOException: "+e.Message);
#endif
                    }
                    finally
                    {
#if LOG4NET
                        Logger.Socket.Warn("connection lost");
#endif
                        // only flag this as connection error if we are not
                        // cleanly disconnecting
                        if (!_Connection.IsDisconnecting)
                        {
                            _Connection.IsConnectionError = true;
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
#if LOG4NET
                    Logger.Socket.Debug("ReadThread aborted");
#endif
                }
                catch (Exception ex)
                {
#if LOG4NET
                    Logger.Socket.Error(ex);
#endif
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private class WriteThread
        {
            private IrcConnection _Connection;
            private Thread _Thread;
            private int _HighCount;
            private int _AboveMediumCount;
            private int _MediumCount;
            private int _BelowMediumCount;
            private int _LowCount;
            private int _AboveMediumSentCount;
            private int _MediumSentCount;
            private int _BelowMediumSentCount;
            private int _AboveMediumThresholdCount = 4;
            private int _MediumThresholdCount = 2;
            private int _BelowMediumThresholdCount = 1;
            private int _BurstCount;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="connection"></param>
            public WriteThread(IrcConnection connection)
            {
                _Connection = connection;
            }

            /// <summary>
            /// 
            /// </summary>
            public void Start()
            {
                _Thread = new Thread(new ThreadStart(_Worker));
                _Thread.Name = "WriteThread (" + _Connection.Address + ":" + _Connection.Port + ")";
                _Thread.IsBackground = true;
                _Thread.Start();
            }

            /// <summary>
            /// 
            /// </summary>
            public void Stop()
            {
#if LOG4NET
                Logger.Connection.Debug("Stopping WriteThread...");
#endif

                _Thread.Abort();
                // make sure we close the stream after the thread is gone, else
                // the thread will think the connection is broken!
                _Thread.Join();

                try
                {
                    _Connection._Writer.Close();
                }
                catch (ObjectDisposedException)
                {
                }
            }

            private void _Worker()
            {
#if LOG4NET
                Logger.Socket.Debug("WriteThread started");
#endif
                try
                {
                    try
                    {
                        while (_Connection.IsConnected)
                        {
                            _CheckBuffer();
                            Thread.Sleep(_Connection._SendDelay);
                        }
                    }
                    catch (IOException e)
                    {
#if LOG4NET
                        Logger.Socket.Warn("IOException: " + e.Message);
#endif
                    }
                    finally
                    {
#if LOG4NET
                        Logger.Socket.Warn("connection lost");
#endif
                        // only flag this as connection error if we are not
                        // cleanly disconnecting
                        if (!_Connection.IsDisconnecting)
                        {
                            _Connection.IsConnectionError = true;
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
#if LOG4NET
                    Logger.Socket.Debug("WriteThread aborted");
#endif
                }
                catch (Exception ex)
                {
#if LOG4NET
                    Logger.Socket.Error(ex);
#endif
                }
            }

            #region WARNING: complex scheduler, don't even think about changing it!
            // WARNING: complex scheduler, don't even think about changing it!
            private void _CheckBuffer()
            {
                // only send data if we are succefully registered on the IRC network
                if (!_Connection._IsRegistered)
                {
                    return;
                }

                _HighCount = ((Queue)_Connection._SendBuffer[Priority.High]).Count;
                _AboveMediumCount = ((Queue)_Connection._SendBuffer[Priority.AboveMedium]).Count;
                _MediumCount = ((Queue)_Connection._SendBuffer[Priority.Medium]).Count;
                _BelowMediumCount = ((Queue)_Connection._SendBuffer[Priority.BelowMedium]).Count;
                _LowCount = ((Queue)_Connection._SendBuffer[Priority.Low]).Count;

                if (_CheckHighBuffer() &&
                    _CheckAboveMediumBuffer() &&
                    _CheckMediumBuffer() &&
                    _CheckBelowMediumBuffer() &&
                    _CheckLowBuffer())
                {
                    // everything is sent, resetting all counters
                    _AboveMediumSentCount = 0;
                    _MediumSentCount = 0;
                    _BelowMediumSentCount = 0;
                    _BurstCount = 0;
                }

                if (_BurstCount < 3)
                {
                    _BurstCount++;
                    //_CheckBuffer();
                }
            }

            private bool _CheckHighBuffer()
            {
                if (_HighCount > 0)
                {
                    string data = (string)((Queue)_Connection._SendBuffer[Priority.High]).Dequeue();
                    if (_Connection._WriteLine(data) == false)
                    {
#if LOG4NET
                        Logger.Queue.Warn("Sending data was not sucessful, data is requeued!");
#endif
                        ((Queue)_Connection._SendBuffer[Priority.High]).Enqueue(data);
                    }

                    if (_HighCount > 1)
                    {
                        // there is more data to send
                        return false;
                    }
                }

                return true;
            }

            private bool _CheckAboveMediumBuffer()
            {
                if ((_AboveMediumCount > 0) &&
                    (_AboveMediumSentCount < _AboveMediumThresholdCount))
                {
                    string data = (string)((Queue)_Connection._SendBuffer[Priority.AboveMedium]).Dequeue();
                    if (_Connection._WriteLine(data) == false)
                    {
#if LOG4NET
                        Logger.Queue.Warn("Sending data was not sucessful, data is requeued!");
#endif
                        ((Queue)_Connection._SendBuffer[Priority.AboveMedium]).Enqueue(data);
                    }
                    _AboveMediumSentCount++;

                    if (_AboveMediumSentCount < _AboveMediumThresholdCount)
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool _CheckMediumBuffer()
            {
                if ((_MediumCount > 0) &&
                    (_MediumSentCount < _MediumThresholdCount))
                {
                    string data = (string)((Queue)_Connection._SendBuffer[Priority.Medium]).Dequeue();
                    if (_Connection._WriteLine(data) == false)
                    {
#if LOG4NET
                        Logger.Queue.Warn("Sending data was not sucessful, data is requeued!");
#endif
                        ((Queue)_Connection._SendBuffer[Priority.Medium]).Enqueue(data);
                    }
                    _MediumSentCount++;

                    if (_MediumSentCount < _MediumThresholdCount)
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool _CheckBelowMediumBuffer()
            {
                if ((_BelowMediumCount > 0) &&
                    (_BelowMediumSentCount < _BelowMediumThresholdCount))
                {
                    string data = (string)((Queue)_Connection._SendBuffer[Priority.BelowMedium]).Dequeue();
                    if (_Connection._WriteLine(data) == false)
                    {
#if LOG4NET
                        Logger.Queue.Warn("Sending data was not sucessful, data is requeued!");
#endif
                        ((Queue)_Connection._SendBuffer[Priority.BelowMedium]).Enqueue(data);
                    }
                    _BelowMediumSentCount++;

                    if (_BelowMediumSentCount < _BelowMediumThresholdCount)
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool _CheckLowBuffer()
            {
                if (_LowCount > 0)
                {
                    if ((_HighCount > 0) ||
                        (_AboveMediumCount > 0) ||
                        (_MediumCount > 0) ||
                        (_BelowMediumCount > 0))
                    {
                        return true;
                    }

                    string data = (string)((Queue)_Connection._SendBuffer[Priority.Low]).Dequeue();
                    if (_Connection._WriteLine(data) == false)
                    {
#if LOG4NET
                        Logger.Queue.Warn("Sending data was not sucessful, data is requeued!");
#endif
                        ((Queue)_Connection._SendBuffer[Priority.Low]).Enqueue(data);
                    }

                    if (_LowCount > 1)
                    {
                        return false;
                    }
                }

                return true;
            }
            // END OF WARNING, below this you can read/change again ;)
            #endregion
        }

        /// <summary>
        /// 
        /// </summary>
        private class IdleWorkerThread
        {
            private IrcConnection _Connection;
            private Thread _Thread;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="connection"></param>
            public IdleWorkerThread(IrcConnection connection)
            {
                _Connection = connection;
            }

            /// <summary>
            /// 
            /// </summary>
            public void Start()
            {
                DateTime now = DateTime.Now;
                _Connection._LastPingSent = now;
                _Connection._LastPongReceived = now;

                _Thread = new Thread(new ThreadStart(_Worker));
                _Thread.Name = "IdleWorkerThread (" + _Connection.Address + ":" + _Connection.Port + ")";
                _Thread.IsBackground = true;
                _Thread.Start();
            }

            /// <summary>
            /// 
            /// </summary>
            public void Stop()
            {
                _Thread.Abort();
            }

            private void _Worker()
            {
#if LOG4NET
                Logger.Socket.Debug("IdleWorkerThread started");
#endif
                try
                {
                    while (_Connection.IsConnected)
                    {
                        Thread.Sleep(_Connection._IdleWorkerInterval);

                        // only send active pings if we are registered
                        if (!_Connection.IsRegistered)
                        {
                            continue;
                        }

                        DateTime now = DateTime.Now;
                        int last_ping_sent = (int)(now - _Connection._LastPingSent).TotalSeconds;
                        int last_pong_rcvd = (int)(now - _Connection._LastPongReceived).TotalSeconds;
                        // determins if the resoponse time is ok
                        if (last_ping_sent < _Connection._PingTimeout)
                        {
                            if (_Connection._LastPingSent > _Connection._LastPongReceived)
                            {
                                // there is a pending ping request, we have to wait
                                continue;
                            }

                            // determines if it need to send another ping yet
                            if (last_pong_rcvd > _Connection._PingInterval)
                            {
                                _Connection.WriteLine(Rfc2812.Ping(_Connection.Address), Priority.Critical);
                                _Connection._LastPingSent = now;
                                //_Connection._LastPongReceived = now;
                            } // else connection is fine, just continue
                        }
                        else
                        {
                            if (_Connection.IsDisconnecting)
                            {
                                break;
                            }
#if LOG4NET
                            Logger.Socket.Warn("ping timeout, connection lost");
#endif
                            // only flag this as connection error if we are not
                            // cleanly disconnecting
                            _Connection.IsConnectionError = true;
                            break;
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
#if LOG4NET
                    Logger.Socket.Debug("IdleWorkerThread aborted");
#endif
                }
                catch (Exception ex)
                {
#if LOG4NET
                    Logger.Socket.Error(ex);
#endif
                }
            }
        }
    }
}
namespace Meebey.SmartIrc4net
{
    /// <summary>
    ///
    /// </summary>
    /// <threadsafety static="true" instance="true" />
    internal class IrcTcpClient : TcpClient
    {
        public Socket Socket
        {
            get
            {
                return Client;
            }
        }
    }
}
#endregion