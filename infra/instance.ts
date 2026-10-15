import * as aws from "@pulumi/aws";
import * as pulumi from "@pulumi/pulumi";
import { name, tags } from "./config";
import { parameterArns } from "./secrets";

// IAM role + instance profile for the existing EC2 host. Attach it with:
//   aws ec2 associate-iam-instance-profile \
//     --instance-id <id> --iam-instance-profile Name=<profile> --region <region>
const role = new aws.iam.Role(`${name}-instance-role`, {
  assumeRolePolicy: JSON.stringify({
    Version: "2012-10-17",
    Statement: [
      {
        Effect: "Allow",
        Principal: { Service: "ec2.amazonaws.com" },
        Action: "sts:AssumeRole",
      },
    ],
  }),
  tags,
});

// SSM agent (Session Manager + Run Command) and ECR pull.
new aws.iam.RolePolicyAttachment(`${name}-instance-ssm`, {
  role: role.name,
  policyArn: "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore",
});

new aws.iam.RolePolicyAttachment(`${name}-instance-ecr`, {
  role: role.name,
  policyArn: "arn:aws:iam::aws:policy/AmazonEC2ContainerRegistryReadOnly",
});

// Read the application secrets from SSM Parameter Store.
new aws.iam.RolePolicy(`${name}-instance-policy`, {
  role: role.id,
  policy: parameterArns.apply((arns) =>
    JSON.stringify({
      Version: "2012-10-17",
      Statement: [
        {
          Effect: "Allow",
          Action: ["ssm:GetParameter", "ssm:GetParameters"],
          Resource: arns,
        },
      ],
    }),
  ),
});

export const instanceProfile = new aws.iam.InstanceProfile(`${name}-instance-profile`, {
  role: role.name,
  tags,
});

export const instanceProfileName = instanceProfile.name;
