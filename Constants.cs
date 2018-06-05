using System.Collections.Generic;

namespace JiraToGitHub
{
    public static class Constants
    {
        // TODO: Fill in mappings of epics to repos
        public static readonly Dictionary<string, (string RepoOwner, string RepoName)> JiraEpicToGitHubRepo = new Dictionary<string, (string RepoOwner, string RepoName)>
        {
            { "FOO-1234", ("fooowner", "foorepo") },
        };

        // TODO: Fill in mappings of Jira and GitHub users
        public static readonly Dictionary<string, string> JiraUserToGitHubUser = new Dictionary<string, string>
        {
            { "jirauser", "githubuser" },
        };
    }
}