variable "region" {
  default = "eu-central-1"
}
variable "vpc_cidr" {
  default = "10.20.0.0/16"
}
variable "availability_zones" {
  default = ["eu-central-1a", "eu-central-1b"]
}
variable "allowed_security_groups" {
  default = ["sg-0a680afd35"]
}
variable "additional_tags" {
  type = map(string)
  default = {
    Expires    = "Never"
  }
}
