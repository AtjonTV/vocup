using System.Threading.Tasks;
using Octokit;

namespace Vocup.Util.Network
{
    public class InternetService
    {
        public async Task Start()
        {
            var github = new GitHubClient(new ProductHeaderValue(AppInfo.ProductName, AppInfo.GetVersion(3)));
            var releases = await github.Repository.Release.GetAll("daniel-lerch", "vocup");
        }
    }
}