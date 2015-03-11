namespace OctopusProjectUpdater
{
    using System.Collections.Generic;

    public class CachingTemplateRepository : ITemplateRepository
    {
        readonly Dictionary<string, string> cache = new Dictionary<string, string>();
        readonly ITemplateRepository underlyingRepository;

        public CachingTemplateRepository(ITemplateRepository underlyingRepository)
        {
            this.underlyingRepository = underlyingRepository;
        }

        public string GetTempate(string projectGroup, string fileName)
        {
            var key = string.Format("{0}:{1}", projectGroup, fileName);
            string result;
            if (cache.TryGetValue(key, out result))
            {
                return result;
            }
            result = underlyingRepository.GetTempate(projectGroup, fileName);
            cache[key] = result;
            return result;
        }
    }
}