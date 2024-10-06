terraform {
  backend "s3" {
    bucket = "pinglight-terraform-state-bucket"
    key    = "dev/terraform.tfstate"
    region = "eu-central-1"
  }
}
