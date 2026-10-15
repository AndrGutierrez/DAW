import * as aws from "@pulumi/aws";
import * as pulumi from "@pulumi/pulumi";
import {
  name,
  tags,
  region,
  githubOwner,
  githubRepo,
  githubBranch,
  deployPath,
  instanceId,
} from "./config";
import { apiRepo, nginxRepo } from "./registry";

const current = aws.getCallerIdentityOutput({});

// CodeStar connection to GitHub. It is created in PENDING state and must be
// authorized once in the AWS console (Developer Tools > Connections) before
// CodeBuild can read the repository.
export const connection = new aws.codestarconnections.Connection(`${name}-github`, {
  providerType: "GitHub",
  tags,
});

export const logGroup = new aws.cloudwatch.LogGroup(`${name}-build-logs`, {
  name: `/aws/codebuild/${name}-build`,
  retentionInDays: 30,
  tags,
});

const role = new aws.iam.Role(`${name}-codebuild-role`, {
  assumeRolePolicy: JSON.stringify({
    Version: "2012-10-17",
    Statement: [
      {
        Effect: "Allow",
        Principal: { Service: "codebuild.amazonaws.com" },
        Action: "sts:AssumeRole",
      },
    ],
  }),
  tags,
});

// Scope SSM SendCommand to the specific instance when instanceId is known,
// otherwise to any instance in the account (buildspec discovers the target).
const instanceResourceSuffix = instanceId ? `instance/${instanceId}` : "instance/*";

new aws.iam.RolePolicy(`${name}-codebuild-policy`, {
  role: role.id,
  policy: pulumi
    .all([apiRepo.arn, nginxRepo.arn, connection.arn, current.accountId])
    .apply(([apiArn, nginxArn, connectionArn, accountId]) =>
      JSON.stringify({
        Version: "2012-10-17",
        Statement: [
          {
            Effect: "Allow",
            Action: ["logs:CreateLogGroup", "logs:CreateLogStream", "logs:PutLogEvents"],
            Resource: [
              `arn:aws:logs:${region}:${accountId}:log-group:/aws/codebuild/${name}-build`,
              `arn:aws:logs:${region}:${accountId}:log-group:/aws/codebuild/${name}-build:*`,
            ],
          },
          { Effect: "Allow", Action: ["ecr:GetAuthorizationToken"], Resource: "*" },
          {
            Effect: "Allow",
            Action: [
              "ecr:BatchCheckLayerAvailability",
              "ecr:BatchGetImage",
              "ecr:GetDownloadUrlForLayer",
              "ecr:PutImage",
              "ecr:InitiateLayerUpload",
              "ecr:UploadLayerPart",
              "ecr:CompleteLayerUpload",
            ],
            Resource: [apiArn, nginxArn],
          },
          {
            Effect: "Allow",
            Action: [
              "codestar-connections:UseConnection",
              "codeconnections:UseConnection",
              "codeconnections:GetConnection",
              "codeconnections:GetConnectionToken",
            ],
            Resource: connectionArn,
          },
          {
            Effect: "Allow",
            Action: ["ssm:SendCommand"],
            Resource: [
              `arn:aws:ssm:${region}::document/AWS-RunShellScript`,
              `arn:aws:ec2:${region}:${accountId}:${instanceResourceSuffix}`,
            ],
          },
          {
            Effect: "Allow",
            Action: [
              "ssm:GetCommandInvocation",
              "ssm:ListCommandInvocations",
              "ssm:ListCommands",
              "ssm:DescribeInstanceInformation",
            ],
            Resource: "*",
          },
        ],
      }),
    ),
});

const environmentVariables = [
  { name: "DEPLOY_PATH", value: deployPath, type: "PLAINTEXT" as const },
];

// Optional pin. When unset, buildspec.yml discovers the SSM-managed instance.
if (instanceId) {
  environmentVariables.push({ name: "EC2_INSTANCE_IDS", value: instanceId, type: "PLAINTEXT" as const });
}

export const project = new aws.codebuild.Project(`${name}-build`, {
  name: `${name}-build`,
  description: `Build ARM64 images and deploy ${name}`,
  serviceRole: role.arn,
  buildTimeout: 60,
  queuedTimeout: 30,
  artifacts: { type: "NO_ARTIFACTS" },
  environment: {
    type: "ARM_CONTAINER",
    image: "aws/codebuild/amazonlinux2-aarch64-standard:3.0",
    computeType: "BUILD_GENERAL1_SMALL",
    privilegedMode: true,
    environmentVariables,
  },
  logsConfig: {
    cloudwatchLogs: {
      groupName: logGroup.name,
      status: "ENABLED",
    },
  },
  source: {
    type: "GITHUB",
    location: `https://github.com/${githubOwner}/${githubRepo}.git`,
    buildspec: "buildspec.yml",
    gitSubmodulesConfig: { fetchSubmodules: false },
    auth: { type: "CODECONNECTIONS", resource: connection.arn },
  },
  sourceVersion: `refs/heads/${githubBranch}`,
  tags,
});
