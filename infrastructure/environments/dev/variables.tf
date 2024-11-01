variable "region" {
  default = "eu-central-1"
}

variable "vpc_cidr" {
  default = "10.20.0.0/16"
}

variable "availability_zones" {
  default = ["eu-central-1a", "eu-central-1b"]
}

variable "additional_tags" {
  type = map(string)
  default = {
    Expires    = "Never"
  }
}

variable "rds_master_user_password" {
  description = "The password for the RDS database master user"
  type        = string
  sensitive   = true
}
