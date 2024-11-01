provider "aws" {
  region = var.region
}

locals {
  environment_name = "dev"
}

module "kms" {
  source = "../../modules/kms"

  description             = "KMS for dev environment"
  deletion_window_in_days = 7
  enable_key_rotation     = false
  is_enabled              = true
  key_usage               = "ENCRYPT_DECRYPT"
  multi_region            = false
  key_statements          = [
    {
      sid = "AllowCloudWatchLogs"
      actions = [
        "kms:Encrypt*",
        "kms:Decrypt*",
        "kms:GenerateDataKey*"
      ]
      resources = ["*"]
      principals = [
        { type = "AWS", identifiers = ["*"] }
      ]
    }
  ]
  alias           = "dev-key"
  additional_tags = var.additional_tags
}

module "vpc" {
  source                = "../../modules/vpc"
  vpc_cidr              = var.vpc_cidr
  availability_zones    = var.availability_zones
  public_subnet_enabled = true
  auto_assign_public_ip = true
  additional_tags       = var.additional_tags
  environment           = local.environment_name
}

module "rds-pg" {
  source                = "../../modules/rds-pg"
  db_name               = "pinglight-${local.environment_name}-db"
  engine_version        = "15.2"
  instance_class        = "db.t3.micro"
  storage_type          = "standard"
  allocated_storage     = 20
  max_allocated_storage = 30
  publicly_accessible   = true
  subnet_ids            = module.vpc.public_subnet_id
  vpc_id                = module.vpc.vpc_id
  master_username       = "admin"
  master_userpassword   = var.rds_master_user_password
  additional_tags       = var.additional_tags
}
