# Console application to perform batch closing for old issues #

## Summary ###

Small tool which is using [Octokit.net](http://octokitnet.readthedocs.io/en/latest/) NuGet in Console application to perform batch closing of issues in the GitHub. You can define following parameters.

- **User Id** - Which identity to use when logging into GitHub
- **Password or access token** - Password or access token for the user account
- **Organization** - GitHub organization where repository is located
- **Repository** - Repository of the issue list to process
- **Ignore label** - can be set to ignore issues which have this label - notice that in initial version only one label is supported
- **Days** - How many days old issues are closed

You can define closing comment in the ClosingComment.txt located on the folder where console application is executed. This also supports 'XYZ_days' token, which will be replaced with the 'Days' value.

> It's recommended to use **Personal access tokens** as the login model. You can create these in your GitHub profile by choosing **Settings** - **Developer settings** - **Personal access tokens** (*in April 2018, structure could be changed in future*). You will need to grant pretty high permissions to be able to close issues on behalf of your account.

## Applies to ###

- GitHub issues
- Created using Visual Studio 2017

## Prerequisites ###
None

## Solution ###
Solution | Author(s)
---------|----------
GitHubBatchIssueUpdater | Vesa Juvonen

## Version history ###
Version  | Date | Comments
---------| -----| --------
1.0  | April 27th 2018 | Initial release

## Disclaimer ###
**THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.**


----------

Additional documentation hopefully coming soon - you can smile for this comment when it's 2025... 



<img src="https://telemetry.sharepointpnp.com/community-tooling/solutions/GitHubBatchIssueUpdater" />