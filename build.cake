//#addin nuget:?package=GreenLynx.ReleaseTool.Cake&version=2.1.2
#addin nuget:?package=Cake.Git

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
    var releaseHistory = PrepareRelease(new PrepareReleaseSettings { ProductName = "Release Tool" });
	var version = releaseHistory.CurrentVersion;

	GitTag(gitPath, version);
	GitAddAll(gitPath);
	GitCommit(gitPath, gitName, gitEmail, $"Prepare {version} release")
	GitPush(gitPath);
});

Task("Default")
    .IsDependentOn("Prepare-Release");

RunTarget(target);
