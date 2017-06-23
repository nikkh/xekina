# xekina
xekina is a tool for jump starting your projects.
At present it's a proof of concept that aims to achieve the following high level objectives:
* Create a VSTS project based on the [GDS Agile Project Phases](https://www.gov.uk/service-manual/agile-delivery)
* Create an Azure DevTest Lab for the team
* Create environments (Azure App Service and SQL DB) for the CI/CD pipeline

This is not a single ARM template.  At present the process is implemented using a combination of configuration files, ARM Templates and a console application that strings the creation of artifacts together. 

Soon, I'll be creating a web application that enables you to specify  your desired configuration and then submit the creation to an Azure function or WebJob that will create your infrastructure.

Colleagues are thinking about dotnet core sample projects that will guide the development teams in how to develop their applciations while avoiding vendor lock-in.  

Longer term we could extend this to create components in atlassian stack or using ansible or other tools.
