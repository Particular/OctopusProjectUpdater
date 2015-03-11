namespace OctopusProjectUpdater
{
    public interface ITemplateRepository
    {
        string GetTempate(string projectGroup, string fileName);
    }
}