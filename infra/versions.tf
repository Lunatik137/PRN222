terraform {
  required_version = ">= 1.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = ">= 6.0"
    }
  }

  backend "s3" {
    bucket       = "dungtt112-tf-backend-bucket"
    key          = "state/dev/terraform.tfstate"
    region       = "ap-southeast-2"
    use_lockfile = false
  }
}

provider "aws" {
  region = "ap-southeast-2"
}