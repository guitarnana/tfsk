# tfsk
gitk for TFS version control

Features:
-	List of changeset
-	Show changeset message
-	Show changes in changeset
-	Diff of file.

Installation: 
After you build, the installation is under Setup\Release\Setup.msi
It basically copies 5 files – exe and its dependencies. Nothing fancy.

Usage:
Tfsk.exe -server <tfs server url> -path <file path>  –numdisplay <number of changeset showed in the list>

Option:
-server : tfs server url. 
-path: file path or directory that you want to query history. 
-numdisplay: number of changeset this showed in the list. This is the same as when you use tf history /stopafter. Default value is 100.

Example:
Tfsk.exe -server "http://yourTFSserverURL" -path "$/project/branch" –numdisplay 10

tfsk.exe -path "d:\myPath\myfile.cpp" -numdisplay 4
