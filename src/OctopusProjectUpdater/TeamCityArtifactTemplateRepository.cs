namespace OctopusProjectUpdater
{
    using System.Net.Http;

    public class TeamCityArtifactTemplateRepository : ITemplateRepository
    {
        const string ArtifactUrlTemplate = "http://builds.particular.net/guestAuth/app/rest/builds/buildType:Tooling_DeploymentProcess_{0},branch:master,status:SUCCESS/artifacts/content/{1}";

        public string GetTempate(string projectGroup, string fileName)
        {
            var artifactUrl = string.Format(ArtifactUrlTemplate, projectGroup, fileName);
            var httpClient = new HttpClient();
            var templateJson = httpClient.GetStringAsync(artifactUrl).Result;
            return templateJson;
        }
    }
}