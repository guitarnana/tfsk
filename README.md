# tfsk
gitk for TFS version control

####Features:
-	List of changeset
-	Show changeset message
-	Show changes in changeset
-	Diff of file.
-	Exclude changeset of users from showing up
-	Search commit message

####Installation: 
After you build, the installation is under Setup\Release\Setup.msi
It basically copies 5 files – exe and its dependencies. Nothing fancy.

####Usage:
tfsk.exe -path <path> [-server \<TeamProjectCollectionUrl\>] [-numdisplay \<num history\>] [-excludeUser \<ExcludeUsers\>] [-version \<versionspec\>]

**Option:**
* **server:** TFS server url. 
* **path:** File path or directory that you want to query history. 
* **numdisplay:** Number of changeset this showed in the list. This is the same as when you use tf history /stopafter. Default value is 100.
* **exclude Users:** Exclude user from showing up in changeset list. Username separated by ;
* **versionspec:** Please see [tfs version spec syntax](https://msdn.microsoft.com/en-us/library/cc31bk2e.aspx#syntax)

####Example:
tfsk.exe -server "http://yourTFSserverURL" -path "$/project/branch" –numdisplay 10

tfsk.exe -path "d:\myPath\myfile.cpp" -numdisplay 4
