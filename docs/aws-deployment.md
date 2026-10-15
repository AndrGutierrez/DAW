# AWS deployment (CodeBuild + ECR + SSM)

This document explains how the production deployment works, and why the build
happens in **AWS CodeBuild** rather than locally or in GitHub Actions.

## Why CodeBuild?

The production host is an **ARM64 (AWS Graviton) EC2 instance** — `t4g.micro`.
Graviton instances are cheaper per unit of work than their x86 counterparts,
so the workload is worth running on ARM.

The catch: **we cannot build the images locally**. Developer machines (and the
GitHub Actions runners) are x86_64. A container image built for `linux/amd64`
will not run on the ARM host. Emulated cross-builds (`buildx --platform
linux/arm64`) are slow and add QEMU to the loop.

CodeBuild solves both problems at once:

- It runs on **native ARM64** builders
  (`aws/codebuild/amazonlinux2-aarch64-standard:3.0`), so `docker build`
  produces `linux/arm64` images with no emulation.
- It runs **inside AWS**, close to ECR, with no local toolchain or QEMU setup.

So: build ARM images in CodeBuild → push to ECR → the ARM instance pulls them.

## Architecture

```
GitHub push to main
      │
      ▼
GitHub Actions (CI)
  • unit-tests / frontend-tests / stack-tests must pass
  • deploy job assumes an AWS role via OIDC (no static keys)
      │  aws codebuild start-build
      ▼
AWS CodeBuild (ARM64, buildspec.yml)
  • docker build API + SPA  (native linux/arm64)
  • docker push → Amazon ECR
  • aws ssm send-command  ──────────────┐
      │                                  │
      ▼                                  ▼
Amazon ECR                         EC2 (t4g.micro, ARM64)
  daw-api:  <git-sha>              • docker compose pull
  daw-nginx:<git-sha>              • docker compose up -d
                                   • health check /health/ready
```

- **No S3 bucket**: the compose file and image tags are embedded (base64) into
  the SSM Run Command.
- **No CodeDeploy**: CodeBuild triggers the rollout directly through SSM Run
  Command.
- **No long-lived AWS keys**: GitHub Actions authenticates with OIDC and a
  short-lived role.

## Components

| Component | Purpose | Where |
| --- | --- | --- |
| ECR repositories `daw-api`, `daw-nginx` | Immutable images tagged by commit SHA | `infra/registry.ts` |
| CodeBuild project `daw-<stack>-build` | Native ARM64 build + ECR push + SSM deploy | `infra/codebuild.ts`, `buildspec.yml` |
| CodeStar connection | Lets CodeBuild read the GitHub repo | `infra/codebuild.ts` |
| IAM OIDC provider + deploy role | GitHub Actions → AWS without static keys | `infra/github.ts` |
| SSM Parameter Store `/daw/<stack>/...` | App secrets (DB, JWT, seed admin) | `infra/secrets.ts` |
| IAM instance role + profile | SSM agent access, ECR pull, Parameter Store read | `infra/instance.ts` |
| EC2 instance (existing) | Runs the Docker Compose stack | user-provided |
| `docker-compose.prod.yml` | Production stack (DB, API, seed, nginx) | repo root |
| `.github/workflows/ci.yml` | Test gate + deploy job | `.github/workflows/ci.yml` |

## How a deploy runs

1. Push to `main`. CI runs `unit-tests`, `frontend-tests`, `stack-tests`.
2. Only if all pass, the `deploy` job runs (guarded by
   `needs:` + `if: push && refs/heads/main`). It assumes the AWS role via OIDC
   and calls `aws codebuild start-build --source-version <sha>`.
3. CodeBuild (ARM64) runs `buildspec.yml`:
   - `pre_build`: logs in to ECR, computes `API_IMAGE`/`NGINX_IMAGE` tagged with
     the commit SHA.
   - `build`: `docker build` for the API and the SPA (native `linux/arm64`).
   - `post_build`: pushes both images to ECR, then base64-encodes the compose
     file + image tags into an SSM Run Command and waits for it to finish.
4. On the EC2 instance, SSM writes `/opt/daw/current/{docker-compose.prod.yml,
   image.env}`, logs in to ECR, `docker compose pull && up -d`, and waits for
   `http://127.0.0.1:8080/health/ready`.

## Host specifics (important)

The EC2 host already runs a **system nginx** (Ubuntu package) on ports 80/443
for another site (`cs.andresgutierrez.me`), so our stack **cannot bind port 80**
directly. The production compose publishes the app's nginx on **host port 8080**:

```yaml
# docker-compose.prod.yml
nginx:
  ports:
    - "${APP_HTTP_PORT:-8080}:80"
```

The host's system nginx then reverse-proxies **port 80 → 127.0.0.1:8080** via
`nginx/host-daw.conf` (installed at `/etc/nginx/sites-available/daw`). That file
is a `default_server` for the `daw.andresgutierez.me` host (and any unmatched
host), so CloudFront can use the standard **HTTP port 80** origin. The
`cs.andresgutierrez.me` site is unaffected.

CloudFront (`daw.andresgutierez.me`) therefore uses an **HTTP origin on port
80**. The origin request policy must forward `CloudFront-Forwarded-Proto`
(e.g. `Managed-AllViewerAndCloudFrontHeaders-2022-06`); the host nginx passes it
through, the container nginx turns it into `X-Forwarded-Proto: https`, and the
API trusts the Docker network for that header (`Program.cs`) — required for the
cookie-based session to accept HTTPS.

## Runtime configuration on the instance

- Secrets live in **SSM Parameter Store** (`/daw/<stack>/...`) and are rendered
  once into `/opt/daw/.env` (mode 600) on the instance. The deploy step reads
  them with `--env-file /opt/daw/.env`.
- Per-deploy values (`API_IMAGE`, `NGINX_IMAGE`, `ECR_REGISTRY`, `AWS_REGION`)
  are written to `/opt/daw/current/image.env`.
- The instance needs: SSM agent, an instance profile with
  `AmazonSSMManagedInstanceCore` + ECR read + Parameter Store read, Docker with
  the Compose plugin, and the AWS CLI (for `ecr get-login-password`).

## Provisioning (Pulumi, TypeScript)

```bash
cd infra
npm install
pulumi stack init dev
pulumi config set aws:region us-east-1
pulumi config set githubOwner <owner>
pulumi config set githubRepo <repo>
# secrets used to seed /opt/daw/.env:
pulumi config set --secret postgresPassword  "$(python3 -c 'import secrets,base64;print(base64.b64encode(secrets.token_bytes(24)).decode())')"
pulumi config set --secret jwtKey            "$(python3 -c 'import secrets,base64;print(base64.b64encode(secrets.token_bytes(48)).decode())')"
pulumi config set --secret seedAdminPassword "ChangeMe!123"
pulumi up
pulumi stack output githubActions   # values to put in GitHub
```

Then:
1. Authorize the CodeStar connection (Console → Developer Tools → Connections).
2. Attach the instance profile to the existing EC2 instance:
   `aws ec2 associate-iam-instance-profile --instance-id <id> --iam-instance-profile Name=<profile>`.
3. In GitHub (Settings → Secrets and variables → Actions):
   - variable `CODEBUILD_PROJECT` = the `codebuildProjectName` output
   - secret `AWS_DEPLOY_ROLE_ARN` = the `githubActionsRoleArn` output
   - environment `production`

## Troubleshooting (issues already hit)

- **`Not authorized to perform sts:AssumeRoleWithWebIdentity`** — GitHub now
  issues an *immutable* OIDC subject, e.g.
  `repo:owner@<ownerId>/repo@<repoId>:environment:production`. The trust policy
  must allow it. `infra/github.ts` accepts both the classic
  (`repo:owner/repo:...`) and immutable (`repo:owner@*/repo@*:...`) forms.
- **`set: Illegal option -o pipefail`** — SSM `AWS-RunShellScript` executes on
  the instance with `sh` (dash), which does not support `pipefail`. The deploy
  script uses `set -eu` only.
- **Health check returns 404 / deploy fails after `up`** — a system nginx owns
  port 80 on the host, so the app's nginx container cannot bind it. The stack
  uses host port `8080` (`APP_HTTP_PORT`).
- **`429 Data limit exceeded` pulling `public.ecr.aws/...`** — anonymous public
  ECR pulls are rate/volume limited. The frontend uses Docker Hub
  `nginx:alpine`; if you switch to public ECR, authenticate
  (`aws ecr-public get-login-password | docker login public.ecr.aws`).
- **`Invalid service role` / CodeBuild project creation** — the CodeBuild
  service role needs `codeconnections:GetConnectionToken`/`UseConnection` on the
  CodeStar connection, and the connection must be `AVAILABLE`.

## Operating

- Build logs: CloudWatch Logs group `/aws/codebuild/daw-<stack>-build`.
- Deploy artifacts on the instance: `/opt/daw/current` (compose + `image.env`),
  `/opt/daw/.env` (secrets).
- Manual deploy: `aws codebuild start-build --project-name daw-<stack>-build
  --source-version <sha> --environment-variables-override name=IMAGE_TAG,value=<sha>,type=PLAINTEXT`.
- Roll back: re-run CodeBuild with a previous commit SHA (images are immutable,
  tagged by SHA).
