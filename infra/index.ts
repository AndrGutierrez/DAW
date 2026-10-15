import * as pulumi from "@pulumi/pulumi";
import { region, deployPath } from "./config";
import { apiRepo, nginxRepo } from "./registry";
import { project, connection } from "./codebuild";
import { deployRole } from "./github";
import { instanceProfileName } from "./instance";
import { parameterPrefix } from "./secrets";

export const apiRepositoryUrl = apiRepo.repositoryUrl;
export const nginxRepositoryUrl = nginxRepo.repositoryUrl;

export const codebuildProjectName = project.name;
export const codebuildConnectionArn = connection.arn;

export const githubActionsRoleArn = deployRole.arn;
export const appDeployPath = deployPath;
export const appInstanceProfileName = instanceProfileName;
export const secretParameterPrefix = parameterPrefix;

// Everything to configure in GitHub: Settings > Secrets and variables > Actions.
// Copy each field to the destination encoded in its name.
export const githubActions = pulumi.all([project.name, deployRole.arn, region]).apply(([projectName, roleArn, awsRegion]) => ({
  createEnvironment: "production",
  repositoryVariable_CODEBUILD_PROJECT: projectName,
  repositorySecret_AWS_DEPLOY_ROLE_ARN: roleArn,
  note_AWS_REGION_is_hardcoded_in_workflow: awsRegion,
}));
