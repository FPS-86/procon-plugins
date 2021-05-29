CAdminLogger.cs
Written by: [LAB]HeliMagnet
Version: 1.2
For use with: ProCon

This plugin will log SUCCESSFUL COMMANDS or "Did you mean" RESPONSES.
Upon enabling the plugin, a folder will be created in: <ProCon Root>\Plugins\Admin Logs
A file will be created inside this folder: Total Admin Log.txt	- This will contain all the commands by all admins on the server.
As each admin (with ProCon privileges) joins the server, their commands will be tracked as well. A filename will be created with their name as well (potential source of problem here - not sure what special characters in name will do - please notify of any errors).
NOw includes PRoCon interface logging.

After every level load (or if the command limit - see below - is reached), the files will be updated with any commands sent.
A typical entry in the text file may look like:
	6/11/2010 11:32:15 PM: HeliMagnet --> jsay Test
For server responses:
	6/7/2010 7:37:09 PM: HeliMagnet <-- (server) Cancelled Command
The time being the system time for the computer running ProCon.

Looking at the plugin settings and going down the options:

Commands Before Write to File: 	Any integer > 0. The lower the number, the more often the file will be updated, but more file I/O is needed

Log Commands?: 			Yes - enable the plugin. No - disable plugin (this will be needed when I fold the code into the In-game Admin plugin soon).

Manually Pick Directory?:	Change this to "Yes" to open a browser to select the folder you wish to log the files in. You can not paste the directory in on your own!

Log Files Directory:		Shows where the log files will be written to. This is only to show you where, you can't edit this (prevent errors)

Commands:			Enter them EXACTLY as they are set up in the In-game admin plugin. No margin for error here!

Response Scope:			Similar to commands, enter them exactly!