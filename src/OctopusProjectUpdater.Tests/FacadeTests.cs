namespace OctopusProjectUpdater.Tests
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using Octopus.Client;

    [TestFixture]
    public class FacadeTests
    {
        [Test]
        public void It_can_create_and_update_a_project_and_project_group()
        {
            var octopusApiKey = Environment.GetEnvironmentVariable("OCTOPUS_API_KEY");
            var facade = new Facade(octopusApiKey, new TeamCityArtifactTemplateRepository());
            facade.CreateProject("Test1", "Testing", "Test1.Octo");

            var factory = new OctopusClientFactory();
            var client = factory.CreateClient(new OctopusServerEndpoint("http://deploy.particular.net", octopusApiKey));
            var repo = new OctopusRepository(client);

            var project = repo.Projects.FindByName("Test1.Octo");
            Assert.IsNotNull(project);
            var proecess = repo.DeploymentProcesses.Get(project.DeploymentProcessId);
            try
            {
                Assert.IsTrue(project.IncludedLibraryVariableSetIds.Contains("LibraryVariableSets-33"));
                Assert.IsTrue(proecess.Steps.Any(x => x.Name == "Deploy"));

                facade = new Facade(octopusApiKey, new FakeTemplateRepository());
                facade.UpdateProject("Test1.Octo");
                project = repo.Projects.FindByName("Test1.Octo");
                Assert.IsNotNull(project);
                proecess = repo.DeploymentProcesses.Get(project.DeploymentProcessId);

                Assert.IsFalse(project.IncludedLibraryVariableSetIds.Contains("LibraryVariableSets-33"));
                Assert.IsFalse(proecess.Steps.Any(x => x.Name == "Deploy"));

                repo = new OctopusRepository(client);
                facade = new Facade(octopusApiKey, new TeamCityArtifactTemplateRepository());
                facade.UpdateAllProjects("Testing");

                project = repo.Projects.FindByName("Test1.Octo");
                Assert.IsNotNull(project);
                proecess = repo.DeploymentProcesses.Get(project.DeploymentProcessId);

                Assert.IsTrue(project.IncludedLibraryVariableSetIds.Contains("LibraryVariableSets-33"));
                Assert.IsTrue(proecess.Steps.Any(x => x.Name == "Deploy"));
            }
            finally
            {
                repo.Projects.Delete(project);
            }
        }

        private class FakeTemplateRepository : ITemplateRepository
        {
            public string GetTempate(string projectGroup, string fileName)
            {
                if (fileName.Equals("Project.json", StringComparison.OrdinalIgnoreCase))
                {
                    return @"{
  ""IncludedLibraryVariableSetIds"": [
    ""LibraryVariableSets-36"",
    ""LibraryVariableSets-35"",
	""LibraryVariableSets-65"",
	""LibraryVariableSets-67""
  ],
  ""DefaultToSkipIfAlreadyInstalled"": false,
  ""VersioningStrategy"": {
    ""DonorPackageStepId"": null,
    ""Template"": ""#{Octopus.Version.LastMajor}.#{Octopus.Version.LastMinor}.#{Octopus.Version.NextPatch}""
  },
  ""Name"": ""%OCTO_PROJECT_NAME%"",
  ""Description"": """",
  ""IsDisabled"": false,
  ""ProjectGroupId"": ""ProjectGroups-131"",
  ""LifecycleId"": ""lifecycle-ProjectGroups-99"",
}";
                }
                return @"{
  ""Steps"": [
    {
      ""Id"": ""674e1a63-42cf-440e-83be-f80f58ef85a1"",
      ""Name"": ""Notify of draft"",
      ""RequiresPackagesToBeAcquired"": false,
      ""Properties"": {
        ""Octopus.Action.TargetRoles"": ""tentacle""
      },
      ""Condition"": ""Success"",
      ""Actions"": [
        {
          ""Id"": ""578efb8c-8303-4c1f-88a9-f3cee9e14e0a"",
          ""Name"": ""Notify of draft"",
          ""ActionType"": ""Octopus.Script"",
          ""Environments"": [
            ""Environments-65""
          ],
          ""Properties"": {
            ""Octopus.Action.Script.ScriptBody"": ""$message = if ($OctopusParameters['HipChatMessage']) { $OctopusParameters['HipChatMessage'] } else { \""(successful) %PROJECT_NAME% [v$($OctopusParameters['Octopus.Release.Number'])] deployed to $($OctopusParameters['Octopus.Environment.Name'])  on $($OctopusParameters['Octopus.Machine.Name'])\"" } \n#---------\n$apitoken = $OctopusParameters['HipChatAuthToken']\n$roomid = $OctopusParameters['HipChatRoomId']\n$from = $OctopusParameters['HipChatFrom']\n$colour = $OctopusParameters['HipChatColor']\n\nTry \n{\n\t#Do the HTTP POST to HipChat\n\t$post = \""auth_token=$apitoken&room_id=$roomid&from=$from&color=$colour&message=$message&notify=1&message_format=text\""\n\t$webRequest = [System.Net.WebRequest]::Create(\""https://api.hipchat.com/v1/rooms/message\"")\n\t$webRequest.ContentType = \""application/x-www-form-urlencoded\""\n\t$postStr = [System.Text.Encoding]::UTF8.GetBytes($post)\n\t$webrequest.ContentLength = $postStr.Length\n\t$webRequest.Method = \""POST\""\n\t$requestStream = $webRequest.GetRequestStream()\n\t$requestStream.Write($postStr, 0,$postStr.length)\n\t$requestStream.Close()\n\t\n\t[System.Net.WebResponse] $resp = $webRequest.GetResponse();\n\t$rs = $resp.GetResponseStream();\n\t[System.IO.StreamReader] $sr = New-Object System.IO.StreamReader -argumentList $rs;\n\t$sr.ReadToEnd();\t\t\t\t\t\n}\ncatch [Exception] {\n\t\""Woah!, wasn't expecting to get this exception. `r`n $_.Exception.ToString()\""\n}"",
            ""Octopus.Action.Template.Id"": ""ActionTemplates-2"",
            ""Octopus.Action.Template.Version"": ""0"",
            ""HipChatFrom"": ""Octopus Deploy"",
            ""HipChatColor"": ""green"",
            ""HipChatRoomId"": ""#{HipChatEngineeringID}"",
            ""HipChatAuthToken"": ""#{HipChatAPIV1}"",
            ""HipChatMessage"": ""New draft release of %PROJECT_NAME% at https://github.com/Particular/%PROJECT_NAME%/releases""
          },
          ""SensitiveProperties"": {}
        }
      ],
      ""SensitiveProperties"": {}
    },
    {
      ""Id"": ""b6145394-b751-4d2e-8a92-1b82f1a3fc9a"",
      ""Name"": ""Verify draft"",
      ""RequiresPackagesToBeAcquired"": false,
      ""Properties"": {
        ""Octopus.Action.TargetRoles"": """"
      },
      ""Condition"": ""Success"",
      ""Actions"": [
        {
          ""Id"": ""737fa883-457b-4821-805a-ade60e58e181"",
          ""Name"": ""Verify draft"",
          ""ActionType"": ""Octopus.Manual"",
          ""Environments"": [
            ""Environments-65""
          ],
          ""Properties"": {
            ""Octopus.Action.Manual.ResponsibleTeamIds"": ""teams-everyone"",
            ""Octopus.Action.Manual.Instructions"": ""Please verify the draft release notes of %PROJECT_NAME% at https://github.com/Particular/%PROJECT_NAME%/releases""
          },
          ""SensitiveProperties"": {}
        }
      ],
      ""SensitiveProperties"": {}
    }
  ]
}";
            }
        }
    }
}
