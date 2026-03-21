terraform {
  required_version = ">= 1.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = ">= 6.0"
    }
  }

  backend "s3" {
    bucket       = "chienpq137-863700943489-us-east-1-an"
    key          = "state/dev/terraform.tfstate"
    region       = "us-east-1"
    use_lockfile = false
  }
}

provider "aws" {
  region = "us-east-1"
}