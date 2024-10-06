variable "vpc_cidr" {}
variable "availability_zones" {
  type = list(string)
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
