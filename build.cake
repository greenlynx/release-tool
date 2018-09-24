#addin nuget:?package=GreenLynx.ReleaseTool.Cake&version=2.1.2&loaddependencies=true
#addin nuget:?package=Cake.Git
#addin "Cake.FileHelpers"

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var gitPath = MakeAbsolute(Directory("."));
var gitName = "Dan Haughey";
var gitEmail = "dan@greenlynx.co.uk";

Task("Check-Git-Prerequisites")
    .Does(() =>
{
    if (!GitIsValidRepository(gitPath))
	{
		throw new Exception($"Directory '{gitPath}' is not a valid git repository");
	}

	var currentBranch = GitBranchCurrent(gitPath);
	if (currentBranch.FriendlyName != "master")
	{
		throw new Exception($"Directory '{gitPath}' must be on the master branch in order to prepare a release");
	}

    if (GitHasStagedChanges(gitPath))
	{
		throw new Exception($"Directory '{gitPath}' has staged git changes. Please make sure the working directory is clean before preparing a release.");
	}

    if (GitHasUncommitedChanges(gitPath))
	{
		throw new Exception($"Directory '{gitPath}' has uncommited git changes. Please make sure the working directory is clean before preparing a release.");
	}
});

Task("Prepare-Release")
	.IsDependentOn("Check-Git-Prerequisites")
    .Does(() =>
{
    PrepareRelease(new PrepareReleaseSettings { ProductName = "Release Tool", HtmlChangeLogFileName = "CHANGELOG.html" });
	
	if (GitHasUncommitedChanges(gitPath))
	{
		var version = FileReadText("./VERSION");

		if (GitTags(gitPath).Exists(x => x.FriendlyName == version))
		{
			throw new Exception($"Tag '{version}' already exists, so no tag will be created. If you want a new release, please add a change entry to LATEST-CHANGES.txt and try again.");
		}

		GitAddAll(gitPath);
		GitCommit(gitPath, gitName, gitEmail, $"Prepare {version} release");
		GitTag(gitPath, version, gitName, gitEmail, "TODO: add changelog here");

		Information("Release successfully created and tagged. Now run:");
		Information("git push --follow-tags");
	}
	else
	{
		Warning("NOTE: No changes were listed, so no release will be made, no Git tag will be created, and nothing further will be done. If you are trying to create a new release, please add a change entry to LATEST-CHANGES.txt and try again.");
	}
});

Task("Default")
    .IsDependentOn("Prepare-Release");

RunTarget(target);
