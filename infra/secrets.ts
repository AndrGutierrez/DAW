import * as aws from "@pulumi/aws";
import * as pulumi from "@pulumi/pulumi";
import { name, tags } from "./config";

const cfg = new pulumi.Config();

// Application secrets live in SSM Parameter Store (SecureString). The instance
// reads them once at bootstrap to render /opt/daw/.env; the build itself only
// needs image tags. Set the secrets with, e.g.:
//   pulumi config set --secret postgresPassword "$(openssl rand -base64 24)"
//   pulumi config set --secret jwtKey "$(openssl rand -base64 48)"
//   pulumi config set --secret seedAdminPassword "$(openssl rand -base64 12)Aa1!"
export const parameterPrefix = `/daw/${pulumi.getStack()}`;

const postgresDb = cfg.get("postgresDb") ?? "daw";
const postgresUser = cfg.get("postgresUser") ?? "daw";
const seedAdminUsername = cfg.get("seedAdminUsername") ?? "admin";
const seedAdminEmail = cfg.get("seedAdminEmail") ?? "admin@daw.local";
const bcvAutoSync = cfg.get("bcvAutoSync") ?? "true";

const postgresPassword = cfg.requireSecret("postgresPassword");
const jwtKey = cfg.requireSecret("jwtKey");
const seedAdminPassword = cfg.requireSecret("seedAdminPassword");

export type AppSecretEntry = {
  key: string;
  name: string;
  secure: boolean;
  param: aws.ssm.Parameter;
};

function makeParam(
  id: string,
  key: string,
  suffix: string,
  value: pulumi.Input<string>,
  secure: boolean,
): AppSecretEntry {
  const paramName = `${parameterPrefix}/${suffix}`;
  const param = new aws.ssm.Parameter(id, {
    name: paramName,
    type: secure ? "SecureString" : "String",
    value,
    overwrite: true,
    tags: { ...tags, Name: paramName },
  });
  return { key, name: paramName, secure, param };
}

export const appSecretEntries: AppSecretEntry[] = [
  makeParam(`${name}-param-pg-db`, "POSTGRES_DB", "postgres/db", postgresDb, false),
  makeParam(`${name}-param-pg-user`, "POSTGRES_USER", "postgres/user", postgresUser, false),
  makeParam(`${name}-param-pg-pass`, "POSTGRES_PASSWORD", "postgres/password", postgresPassword, true),
  makeParam(`${name}-param-jwt`, "JWT_KEY", "jwt/key", jwtKey, true),
  makeParam(`${name}-param-admin-user`, "SEED_ADMIN_USERNAME", "seed/admin/username", seedAdminUsername, false),
  makeParam(`${name}-param-admin-email`, "SEED_ADMIN_EMAIL", "seed/admin/email", seedAdminEmail, false),
  makeParam(`${name}-param-admin-pass`, "SEED_ADMIN_PASSWORD", "seed/admin/password", seedAdminPassword, true),
  makeParam(`${name}-param-bcv`, "BCV_AUTO_SYNC", "bcv/auto-sync", bcvAutoSync, false),
];

export const parameterArns: pulumi.Output<string[]> = pulumi.all(
  appSecretEntries.map((entry) => entry.param.arn),
);
