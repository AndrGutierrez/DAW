import * as aws from "@pulumi/aws";
import { name, tags, ecrApiRepoName, ecrNginxRepoName, imageRetentionCount } from "./config";

function createRepository(id: string, repositoryName: string): aws.ecr.Repository {
  const repository = new aws.ecr.Repository(id, {
    name: repositoryName,
    // Mutable so a re-run of the pipeline can push the same commit SHA again.
    imageTagMutability: "MUTABLE",
    imageScanningConfiguration: { scanOnPush: true },
    forceDelete: true,
    tags: { ...tags, Name: repositoryName },
  });

  new aws.ecr.LifecyclePolicy(`${id}-lifecycle`, {
    repository: repository.name,
    policy: JSON.stringify({
      rules: [
        {
          rulePriority: 1,
          description: "Expire untagged images after 7 days",
          selection: {
            tagStatus: "untagged",
            countType: "sinceImagePushed",
            countUnit: "days",
            countNumber: 7,
          },
          action: { type: "expire" },
        },
        {
          rulePriority: 2,
          description: `Keep the ${imageRetentionCount} most recent images`,
          selection: {
            tagStatus: "any",
            countType: "imageCountMoreThan",
            countNumber: imageRetentionCount,
          },
          action: { type: "expire" },
        },
      ],
    }),
  });

  return repository;
}

export const apiRepo = createRepository(`${name}-api`, ecrApiRepoName);
export const nginxRepo = createRepository(`${name}-nginx`, ecrNginxRepoName);
