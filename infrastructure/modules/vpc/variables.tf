variable "vpc_cidr" {}

variable "availability_zones" {
  type = list(string)
}

variable "az_count" {
    description = "Number of AZs to cover in a given region"
    default = "2"
}

variable "public_subnet_enabled" {
  default = true
}

variable "auto_assign_public_ip" {
  default = true
}

variable "additional_tags" {
  type = map(string)
  default = {}
}

variable "environment" {}
