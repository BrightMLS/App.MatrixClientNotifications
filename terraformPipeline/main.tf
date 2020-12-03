provider "aws" {
  version = ">= 2.31.0"
  region  = var.aws_region_primary
  profile = var.aws_profile
}

provider "aws" {
  alias   = "code_primary"
  version = ">= 2.31.0"
  region  = var.aws_region_primary
  profile = var.aws_profile
}

provider "aws" {
  alias   = "code_secondary"
  version = ">= 2.31.0"
  region  = var.aws_region_secondary
  profile = var.aws_profile
}

provider "aws" {
  alias   = "dev_primary"
  version = ">= 2.31.0"
  region  = var.aws_region_primary
  profile = var.aws_profile_dev
}

provider "aws" {
  alias   = "dev_secondary"
  version = ">= 2.31.0"
  region  = var.aws_region_secondary
  profile = var.aws_profile_dev
}

provider "aws" {
  alias   = "tst_primary"
  version = ">= 2.31.0"
  region  = var.aws_region_primary
  profile = var.aws_profile_tst
}

provider "aws" {
  alias   = "tst_secondary"
  version = ">= 2.31.0"
  region  = var.aws_region_secondary
  profile = var.aws_profile_tst
}

provider "aws" {
  alias   = "prd_primary"
  version = ">= 2.31.0"
  region  = var.aws_region_primary
  profile = var.aws_profile_prd
}

provider "aws" {
  alias   = "prd_secondary"
  version = ">= 2.31.0"
  region  = var.aws_region_secondary
  profile = var.aws_profile_prd
}

provider "github" {
  organization = var.github_owner
}

# Base Naming setup
module "base_naming" {
  source    = "git::ssh://git@github.com/BrightMLS/common_modules_terraform.git//bright_naming_conventions?ref=develop"
  app_group = var.app_group
  env       = var.env
  ledger    = var.ledger
  site      = var.site_primary
  tier      = var.tier
}

module "secondary_base_naming" {
  source      = "git::ssh://git@github.com/BrightMLS/common_modules_terraform.git//bright_naming_conventions?ref=develop"
  base_object = module.base_naming
  site        = var.site_secondary
}

module "primary_bucket_naming" {
  source      = "git::ssh://git@github.com/BrightMLS/common_modules_terraform.git//bright_naming_conventions?ref=develop"
  base_object = module.base_naming
  type        = "s3b"
}

module "secondary_bucket_naming" {
  source      = "git::ssh://git@github.com/BrightMLS/common_modules_terraform.git//bright_naming_conventions?ref=develop"
  base_object = module.secondary_base_naming
  type        = "s3b"
}

module "pipeline_naming" {
  source      = "git::ssh://git@github.com/BrightMLS/common_modules_terraform.git//bright_naming_conventions?ref=develop"
  base_object = module.base_naming
  type        = "cpl"
}

# Create Bucket
module "primary_bucket" {
  source        = "git::ssh://git@github.com/BrightMLS/Lib.t4bi.git//terraformPipeline/code_bucket?ref=develop"
  naming_object = module.primary_bucket_naming

  force_destroy = true

  aws_remote_account_list = [
    var.aws_account_number_dev,
    var.aws_account_number_tst,
    var.aws_account_number_prd,
    var.aws_account_number_sbx
  ]
  providers = {
    aws = aws.code_primary
  }
}

# Create Bucket
module "secondary_bucket" {
  source        = "git::ssh://git@github.com/BrightMLS/Lib.t4bi.git//terraformPipeline/code_bucket?ref=develop"
  naming_object = module.secondary_bucket_naming

  force_destroy = true

  aws_remote_account_list = [
    var.aws_account_number_dev,
    var.aws_account_number_tst,
    var.aws_account_number_prd,
    var.aws_account_number_sbx
  ]
  providers = {
    aws = aws.code_secondary
  }
}

# Create Develop Pipeline
module "develop_pipeline" {
  source        = "git::ssh://git@github.com/BrightMLS/Lib.t4bi.git//terraformPipeline/applic_develop_stack?ref=develop"
  naming_object = module.pipeline_naming

  primary_s3_bucket   = module.primary_bucket.bucket
  primary_kms_key     = module.primary_bucket.kms_key
  secondary_s3_bucket = module.secondary_bucket.bucket
  secondary_kms_key   = module.secondary_bucket.kms_key
  github_oauth_token  = var.github_oauth_token
  github_owner        = var.github_owner
  github_repo         = var.github_repo
  github_branch       = "server-migration"
  pipeline_purpose    = "develop"
  build_image         = "aws/codebuild/windows-base:2019-1.0"
  build_type          = "WINDOWS_SERVER_2019_CONTAINER"
  buildspec           = "App.MatrixClientNotifications\\App.MatrixClientNotifications\\buildspec.yml"

  build_environment_variables = {
    "APPLIC_S3_BUCKET" = module.primary_bucket.bucket.id
    "NUGET_S3"         = module.primary_bucket.bucket.id
  }

  deploy_tags = {
    "app"     = "dotnet"
    "purpose" = "job"
  }

  providers = {
    aws.code      = aws.code_primary #pipeline
    aws.primary   = aws.dev_primary  #codebuild
    aws.secondary = aws.dev_secondary
  }
}

# Create Release Pipeline
module "release_pipeline" {
  source              = "git::ssh://git@github.com/BrightMLS/Lib.t4bi.git//terraformPipeline/applic_release_stack?ref=develop"
  naming_object       = module.pipeline_naming
  primary_s3_bucket   = module.primary_bucket.bucket
  primary_kms_key     = module.primary_bucket.kms_key
  secondary_s3_bucket = module.secondary_bucket.bucket
  secondary_kms_key   = module.secondary_bucket.kms_key
  github_oauth_token  = var.github_oauth_token
  github_owner        = var.github_owner
  github_repo         = var.github_repo
  github_branch       = "server-migration-release"
  pipeline_purpose    = "release"
  build_image         = "aws/codebuild/windows-base:2019-1.0"
  build_type          = "WINDOWS_SERVER_2019_CONTAINER"
  buildspec           = "App.MatrixClientNotifications\\App.MatrixClientNotifications\\buildspec.yml"
  build_environment_variables = {
    "APPLIC_S3_BUCKET" = module.primary_bucket.bucket.id
    "NUGET_S3"         = module.primary_bucket.bucket.id
  }
  deploy_tags = {
    "app"     = "dotnet"
    "purpose" = "job"
  }
  providers = {
    aws.code                 = aws.code_primary
    aws.test_primary         = aws.tst_primary
    aws.test_secondary       = aws.tst_secondary
    aws.production_primary   = aws.prd_primary
    aws.production_secondary = aws.prd_secondary
  }
}