
namespace OctopusProjectUpdater
{
    using System.Linq;
    using System;
    using Newtonsoft.Json;
    using Octopus.Client;
    using Octopus.Client.Model;

    public class Facade
    {
        const string OctopusUrl = "http://deploy.particular.net";

        string octopusApiKey;
        readonly ITemplateRepository templateRepository;

        public Facade(string octopusApiKey, ITemplateRepository templateRepository)
        {
            this.octopusApiKey = octopusApiKey;
            this.templateRepository = templateRepository;
        }

        public void CreateProject(string projectName, string projectGroup, string octopusProjectName)
        {
            var factory = new OctopusClientFactory();
            var client = factory.CreateClient(new OctopusServerEndpoint(OctopusUrl, octopusApiKey));
            var repo = new OctopusRepository(client);

            var group = repo.ProjectGroups.FindByName(projectGroup);
            if (group == null)
            {
                throw new InvalidOperationException("Project group does not exist " + projectGroup);
            }

            var createdProject = CreateProjectResource(templateRepository.GetTempate(projectGroup, "Project.json"), group.Id, projectName, octopusProjectName, repo);
            UpdateProjectVariables(projectName, repo, createdProject);
            UpdateProcessResource(templateRepository.GetTempate(projectGroup, "DeploymentProcess.json"), projectName, createdProject, repo);
        }

        static void UpdateProjectVariables(string projectName, OctopusRepository repo, ProjectResource createdProject)
        {
            var variables = repo.VariableSets.Get(createdProject.VariableSetId);
            variables.Variables.Add(new VariableResource()
            {
                Name = "CanonicalProjectName",
                IsEditable = false,
                Value = projectName
            });
            repo.VariableSets.Modify(variables);
        }

        static ProjectResource CreateProjectResource(string projectTemplateJson, string groupId, string canonicalProjectName, string octopusProjectName, OctopusRepository repo)
        {
            projectTemplateJson = FillPlaceholders(projectTemplateJson, canonicalProjectName, octopusProjectName);
            var newProject = JsonConvert.DeserializeObject<ProjectResource>(projectTemplateJson);
            newProject.ProjectGroupId = groupId;
            return repo.Projects.Create(newProject);
        }

        public void UpdateAllProjects(string projectGroup)
        {
            var factory = new OctopusClientFactory();
            var client = factory.CreateClient(new OctopusServerEndpoint(OctopusUrl, octopusApiKey));
            var repo = new OctopusRepository(client);
           
            var group = repo.ProjectGroups.FindByName(projectGroup);
            if (group == null)
            {
                throw new InvalidOperationException("Project group does not exist " + projectGroup);
            }
            var projects = repo.Projects.FindMany(x => x.ProjectGroupId == group.Id);

            foreach (var project in projects)
            {
                Update(repo, project, group);
            }
        }

        public void UpdateProject(string octopusProjectName)
        {
            var factory = new OctopusClientFactory();
            var client = factory.CreateClient(new OctopusServerEndpoint(OctopusUrl, octopusApiKey));
            var repo = new OctopusRepository(client);
            var project = repo.Projects.FindByName(octopusProjectName);
            var group = repo.ProjectGroups.Get(project.ProjectGroupId);

            Update(repo, project, group);
        }

        void Update(OctopusRepository repo, ProjectResource project, ProjectGroupResource group)
        {
            var variables = repo.VariableSets.Get(project.VariableSetId);

            var canonicalProjectName = variables.Variables.First(x => x.Name == "CanonicalProjectName").Value;

            UpdateProjectResource(templateRepository.GetTempate(group.Name, "Project.json"), canonicalProjectName, project, repo);
            UpdateProcessResource(templateRepository.GetTempate(group.Name, "DeploymentProcess.json"), canonicalProjectName, project, repo);
        }

        static void UpdateProjectResource(string projectTemplateJson, string canonicalProjectName, ProjectResource project, OctopusRepository repo)
        {
            projectTemplateJson = FillPlaceholders(projectTemplateJson, canonicalProjectName, project.Name);
            var newProject = JsonConvert.DeserializeObject<ProjectResource>(projectTemplateJson);
            newProject.Id = project.Id;
            newProject.Links = project.Links;
            repo.Projects.Modify(newProject);
        }

        static void UpdateProcessResource(string processTemplateJson, string canonicalProjectName, ProjectResource project, OctopusRepository repo)
        {
            var oldProcess = repo.DeploymentProcesses.Get(project.DeploymentProcessId);
            processTemplateJson = FillPlaceholders(processTemplateJson, canonicalProjectName, project.Name);
            var newProcess = JsonConvert.DeserializeObject<DeploymentProcessResource>(processTemplateJson);
            newProcess.Id = project.DeploymentProcessId;
            newProcess.Version = oldProcess.Version;
            newProcess.Links = oldProcess.Links;
            repo.DeploymentProcesses.Modify(newProcess);
        }

        static string FillPlaceholders(string templateJson, string canonicalProjectName, string octopusProjectName)
        {
            return templateJson
                .Replace("%PROJECT_NAME%", canonicalProjectName)
                .Replace("%OCTO_PROJECT_NAME%", octopusProjectName);
        }
    }
}
