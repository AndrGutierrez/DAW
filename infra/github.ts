import * as aws from "@pulumi/aws";
import * as pulumi from "@pulumi/pulumi";
import { name, tags, githubOwner, githubRepo, githubBranch, githubEnvironment } from "./config";
import { project } from "./codebuild";

// OIDC provider used by GitHub Actions to assume a short-lived role instead of
// storing long-lived AWS access keys as repository secrets.
const provider = new aws.iam.OpenIdConnectProvider(`${name}-github-oidc`, {
  url: "https://token.actions.githubusercontent.com",
  clientIdLists: ["sts.amazonaws.com"],
  thumbprintLists: [
    "6938fd4d98bab03faadb97b34396831e3780aea1",
    "1c58a3a8518e8759bf075b76b750d4f2df264fcd",
  ],
  tags,
});

// GitHub issues the OIDC `sub` either in the classic form
// (repo:owner/repo:...) or the immutable form
// (repo:owner@<ownerId>/repo@<repoId>:...). Accept both; the numeric ids are
// matched with a wildcard so the stack does not depend on them.
const subjects = [
  `repo:${githubOwner}/${githubRepo}:ref:refs/heads/${githubBranch}`,
  `repo:${githubOwner}/${githubRepo}:environment:${githubEnvironment}`,
  `repo:${githubOwner}@*/${githubRepo}@*:ref:refs/heads/${githubBranch}`,
  `repo:${githubOwner}@*/${githubRepo}@*:environment:${githubEnvironment}`,
];

export const deployRole = new aws.iam.Role(`${name}-github-deploy`, {
  assumeRolePolicy: provider.arn.apply((providerArn) =>
    JSON.stringify({
      Version: "2012-10-17",
      Statement: [
        {
          Effect: "Allow",
          Principal: { Federated: providerArn },
          Action: "sts:AssumeRoleWithWebIdentity",
          Condition: {
            StringEquals: {
              "token.actions.githubusercontent.com:aud": "sts.amazonaws.com",
            },
            StringLike: {
              "token.actions.githubusercontent.com:sub": subjects,
            },
          },
        },
      ],
    }),
  ),
  tags,
});

new aws.iam.RolePolicy(`${name}-github-deploy-policy`, {
  role: deployRole.id,
  policy: project.arn.apply((projectArn) =>
    JSON.stringify({
      Version: "2012-10-17",
      Statement: [
        {
          Effect: "Allow",
          Action: ["codebuild:StartBuild", "codebuild:BatchGetBuilds", "codebuild:StopBuild"],
          Resource: projectArn,
        },
      ],
    }),
  ),
});
