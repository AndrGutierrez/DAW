import * as pulumi from "@pulumi/pulumi";
import * as aws from "@pulumi/aws";

// Configuration. Set values with, for example:
//
//   pulumi config set aws:region us-east-1
//   pulumi config set githubOwner my-org
//   pulumi config set githubRepo my-repo
//
// Required: githubOwner, githubRepo (and aws:region).
// Optional: projectName, environment, githubBranch, githubEnvironment,
//   ecrApiRepoName, ecrNginxRepoName, imageRetentionCount, deployPath,
//   instanceId (override the hardcoded SSM target).
//
// The EC2 host is targeted through SSM Run Command using EC2_INSTANCE_IDS,
// which defaults to the hardcoded instance id below.

const cfg = new pulumi.Config();
const awsCfg = new pulumi.Config("aws");

export const projectName = cfg.get("projectName") ?? "daw";
export const stackName = pulumi.getStack();
export const environment = cfg.get("environment") ?? stackName;

// Prefix applied to every resource name so multiple stacks can coexist.
export const name = `${projectName}-${stackName}`;

export const region = cfg.get("region") ?? awsCfg.get("region") ?? "us-east-1";

export const githubOwner = cfg.require("githubOwner");
export const githubRepo = cfg.require("githubRepo");
export const githubBranch = cfg.get("githubBranch") ?? "main";
export const githubEnvironment = cfg.get("githubEnvironment") ?? "production";

export const ecrApiRepoName = cfg.get("ecrApiRepoName") ?? `${projectName}-api`;
export const ecrNginxRepoName = cfg.get("ecrNginxRepoName") ?? `${projectName}-nginx`;
export const imageRetentionCount = cfg.getNumber("imageRetentionCount") ?? 30;

// Hardcoded EC2 instance that CodeBuild deploys to via SSM Run Command.
// Override with: pulumi config set instanceId i-...
export const instanceId = cfg.get("instanceId") ?? "i-0019eb40449414d15";

export const deployPath = cfg.get("deployPath") ?? "/opt/daw";

export const tags: Record<string, string> = {
  Project: projectName,
  Environment: environment,
  ManagedBy: "pulumi",
};
